using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;
using BotCore.Commands.Implementations;
using BotCore.Filtering;
using BotCore.Configuration;
using BotCore.Permissions;
using BotCore.Core.Messaging;
using BotCore.Core.Cooldowns;
using BotCore.Core.Providers;

namespace BotCore.Commands;
internal class CommandService
{
    UserContext _userContext;
    CommandSettings _settings;
    List<CustomCommandDefinition> _customCommands;

    Dictionary<string, ICommand> _commandRegistry = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase); // Necessary for case-insensitive command lookups

    FilterService _filterService;
    PermissionsService _permissionsService;
    CooldownTracker _cooldownTracker;


    private CommandService(
        UserContext userContext,
        CommandConfig config, 
        FilterService filterService, 
        PermissionsService permissionsService, 
        CooldownTracker cooldownTracker)
    {
        // Cache settings and service providers
        _userContext = userContext;
        _settings = config.CommandSettings;
        _customCommands = config.CustomCommands;

        _filterService = filterService;
        _permissionsService = permissionsService;
        _cooldownTracker = cooldownTracker;
    }

    public static async Task<CommandService> CreateAsync(
        UserContext userContext,
        FilterService filterService, 
        PermissionsService permissionsService, 
        CooldownTracker cooldownTracker)
    {
        // Retrieve command config from storage. Failing that, generate a new one and save it to storage.
        CommandConfig config;
        config = await ConfigService.RetrieveConfigAsync<CommandConfig>(userContext);
        if (config == null) config = await GenerateDefaultConfig();

        // Pass the config (which contains settings and custom commands) and filter service to the constructor
        return new CommandService(userContext, config, filterService, permissionsService, cooldownTracker);
    }

    // InitializeCommands allows BotCore to pass stateful delegates directly to the commands by initializing the commands for the service after the service's configuration has been retrieved.
    public void InitializeCommands(Func<QueryRequest, Task<QueryResult>> providerQuery)
    {
        // Register core commands -- commands that all bots may access.
        RegisterCommandInternal(new UptimeCommand(providerQuery));
        RegisterCommandInternal(new FilterAdminCommand(_filterService));
        RegisterCommandInternal(new CommandsAdminCommand(this));
        RegisterCommandInternal(new PermissionsAdminCommand(_permissionsService));

        // Register custom commands -- commands unique to this configuration of the bot.
        foreach (CustomCommandDefinition customCommand in _customCommands) RegisterCommandInternal(customCommand);
    }

    public async Task Evaluate(MessageContext messageData)
    {
        // Trim any whitespace characters for simple tokenization.
        string input = messageData.Message.Trim();

        // If the first character of the trimmed string is not the command character, no need to parse. 
        if (input[0] != _settings.CommandChar)
        {
            return;
        }

        // Elsewise, tokenize by space character ' ' and parse as a command.
        string[] tokens = TokenizeCommand(' ', input);
        messageData.ReactionType = ReactionType.Command;
        
        // Check registered commands for a match and, upon success, execute the command.
        if (_commandRegistry.TryGetValue(tokens[0], out ICommand command))
        {
            // Check if the command is on cooldown and, if it is, skip processing. However, if the user has cooldown-exemption permissions, carry on as normal.
            if (!_cooldownTracker.IsOffCooldown(command) && !_permissionsService.HasPermission(messageData.Endpoint.Platform, messageData.Username, _settings.CooldownExemptionLevel))
            {
                Console.WriteLine($"DEBUG: {_settings.CommandChar}{command.CommandString} identified but skipped due to cooldown.");        // Debug-only message
                return;
            }

            // Check if user has permission to use the command. If not, skip processing.
            if (!_permissionsService.HasPermission(messageData.Endpoint.Platform, messageData.Username, command.RequiredPermissions))
            {
                Console.WriteLine($"DEBUG: {messageData.Username} lacks permission for {_settings.CommandChar}{command.CommandString}: {_permissionsService.GetPermissionsLevel(messageData.Endpoint.Platform, messageData.Username)} vs {command.RequiredPermissions}."); // Debug-only message
                return;
            }

            // If it's a core command and user has permission, execute its functionality and trigger the cooldown.
            if (command is ICoreCommand executable)
            {
                string response = await executable.ExecuteAsync(messageData, tokens);
                if (response != null) messageData.ReactionString = response;
                _cooldownTracker.StartCooldown(command.CommandString);
            }
            // Otherwise if it's a custom command and user has permission, issue its response and trigger the cooldown.
            else if (command is CustomCommandDefinition custom)
            {
                messageData.ReactionString = custom.CommandResponse;
                _cooldownTracker.StartCooldown(command.CommandString);
            }
            // This should never execute because all commands fit into one of the above categories.
            else messageData.ReactionString = $"Unable to identify `{_settings.CommandChar}{command.CommandString}` command type.";
        }
        // If a message starts with a command character but is not a valid command, there is no need to respond.
    }

    // RegisterCommand handles the user-facing aspects of registration, calling RegisterCommandInternal to handle the rest. These functions are distinct from UpdateCommand to minimize potential user issues.
    public void RegisterCommand(MessageContext messageData, ICommand command)
    {
        if (RegisterCommandInternal(command))
        {
            messageData.ReactionString = $"Command added for `{_settings.CommandChar}{command.CommandString}`.";
        }
        else messageData.ReactionString = $"Command `{_settings.CommandChar}{command.CommandString}` already exists.";
    }

    // RegisterCommandInternal acts as the single authoritative path for command registration, verifying and registering incoming commands, with success or failure being reported to the caller. Validation is performed by the CommandAdminCommand prior to reaching this point.
    bool RegisterCommandInternal(ICommand command)
    {
        // Identify whether a command exists or not. Since core commands are registered during setup, this prevents users from adding custom commands using the same strings.

        // If it doesn't exist already, add it.
        if (!_commandRegistry.ContainsKey(command.CommandString))
        {
            _commandRegistry.Add(command.CommandString, command);
            return true;
        }
        else return false;
    }

    // RemoveCommand removes eligible commands while protecting immutable commands (which typically represent core functionality).
    public void UnregisterCommand(MessageContext messageData, string commandString)
    {
        // Locate the command in the command registry.
        if (_commandRegistry.TryGetValue(commandString, out ICommand registeredCommand))
        {
            // If the command is present, identify whether it is mutable. If it is not mutable, the user may not remove it.
            if (!registeredCommand.IsMutable)
            {
                messageData.ReactionString = $"Command `{_settings.CommandChar}{registeredCommand.CommandString}` may not be removed.";
                return;
            }

            // Otherwise, remove the registered command.
            _commandRegistry.Remove(commandString);
            messageData.ReactionString = $"Command `{_settings.CommandChar}{commandString}` removed.";
        }
        else messageData.ReactionString = $"No command for `{_settings.CommandChar}{commandString}` found.";
    }

    // UpdateCommand updates eligible commands while protecting immutable commands (which typically represent core functionality). The passed command argument should be the new version of the command.
    public void UpdateCommand(MessageContext messageData, ICommand command)
    {
        // Locate the command in the command registry.
        if (_commandRegistry.TryGetValue(command.CommandString, out ICommand registeredCommand))
        {
            // If the command is present, identify whether it is mutable. If it is not mutable, the user may not change it.
            // By checking the currently registered command for mutability instead of the incoming command, it prevents updates from trying to trick the registry into allowing it to change a mutable command.
            if (!registeredCommand.IsMutable)
            {
                messageData.ReactionString = $"Command `{_settings.CommandChar}{registeredCommand.CommandString}` may not be changed.";
                return;
            }

            // Otherwise, update the registered command.
            _commandRegistry[command.CommandString] = command;
            messageData.ReactionString = $"Command `{_settings.CommandChar}{command.CommandString}` updated.";
        }
        else messageData.ReactionString = $"No command for `{_settings.CommandChar}{command.CommandString}` found.";
    }

    // Convert incoming string (previously identified as a command) into a collection of actionable tokens delimited by the splitChar character.
    private static string[] TokenizeCommand(char splitChar, string input)
    {
        // The input will already be trimmed at front and back, so we just need to trim the command character as well so that the first token is the command string or alias.
        // Since this is a simple string manipulation, we can chain them into a single return statement and split the line to make it easier to read.
        return input
            .Substring(1)                                           // Remove the command character
            .Split(splitChar, StringSplitOptions.RemoveEmptyEntries);     // Split the string on ' ' characters into a string array of tokens
    }

    // Handle sending the command settings and custom commands as a CommandConfig off for storage.
    public async Task StoreCommandConfig()
    {
        // Since the commandRegistry contains both core commands (which should not be stored) and custom commands (which should), we first need to extract the custom commands from the command registry.
        List<CustomCommandDefinition> customCommands = _commandRegistry
            .Values
            .OfType<CustomCommandDefinition>()
            .ToList();

        // Future extensions of mutable command types will need to extract their commands separately, or adjust the above LINQ statement to handle it accordingly.

        await ConfigService.StoreConfigAsync(_userContext, new CommandConfig(_settings, customCommands));
    }

    static async Task<CommandConfig> GenerateDefaultConfig()
    {
        // Construct the default config
        CommandSettings commandSettings = new CommandSettings()
        {
            CommandChar = '!'
        };

        List<CustomCommandDefinition> customCommands = new List<CustomCommandDefinition>();

        CommandConfig config = new CommandConfig(commandSettings, customCommands);

        // Send it back for use
        return config;
    }
}