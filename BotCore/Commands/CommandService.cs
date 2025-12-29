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

namespace BotCore.Commands;
internal class CommandService
{
    CommandSettings settings;
    Dictionary<string, ICommand> commandRegistry = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase); // Necessary for case-insensitive command lookups

    FilterService filterService;
    PermissionsService permissionsService;
    CooldownTracker cooldownTracker;

    private CommandService(CommandConfig config, FilterService filterService, PermissionsService permissionsService, CooldownTracker cooldownTracker)
    {
        // Cache settings and service providers
        this.settings = config.commandSettings;
        this.filterService = filterService;
        this.permissionsService = permissionsService;
        this.cooldownTracker = cooldownTracker;

        // Register core commands -- commands that all bots may access.
        RegisterCommandInternal(new UptimeCommand());
        RegisterCommandInternal(new FilterAdminCommand(filterService));
        RegisterCommandInternal(new CommandsAdminCommand(this));
        RegisterCommandInternal(new PermissionsAdminCommand(permissionsService));

        // Register custom commands -- commands unique to this configuration of the bot.
        foreach (CustomCommandDefinition customCommand in config.customCommands) RegisterCommandInternal(customCommand);
    }

    public static async Task<CommandService> CreateAsync(FilterService filterService, PermissionsService permissionsService, CooldownTracker cooldownTracker)
    {
        // Retrieve command config from storage. Failing that, generate a new one and save it to storage.
        CommandConfig config;
        config = await ConfigService.RetrieveCommandConfig();
        if (config == null) config = await GenerateDefaultConfig();

        // Pass the config (which contains settings and custom commands) and filter service to the constructor
        return new CommandService(config, filterService, permissionsService, cooldownTracker);
    }

    public async Task Evaluate(MessageContext messageData)
    {
        // Trim any whitespace characters for simple tokenization.
        string input = messageData.message.Trim();

        // If the first character of the trimmed string is not the command character, no need to parse. 
        if (input[0] != settings.commandChar)
        {
            return;
        }

        // Elsewise, tokenize by space character ' ' and parse as a command.
        string[] tokens = TokenizeCommand(' ', input);
        messageData.reactionType = ReactionType.Command;
        messageData.reactionString = $"Command {tokens[0]} identified!";
        
        // Check registered commands for a match and, upon success, execute the command.
        if (commandRegistry.TryGetValue(tokens[0], out ICommand command))
        {
            // Check if the command is on cooldown and, if it is, skip processing. However, if the user has cooldown-exemption permissions, carry on as normal.
            if (!cooldownTracker.IsOffCooldown(command) && !permissionsService.HasPermission(messageData.username, settings.cooldownExemptionLevel))
            {
                Console.WriteLine($"{settings.commandChar}{command.commandString} identified but skipped due to cooldown.");        // Debug-only message
                return;
            }

            // Check if user has permission to use the command. If not, skip processing.
            if (!permissionsService.HasPermission(messageData.username, command.requiredPermissions))
            {
                Console.WriteLine($"{messageData.username} lacks permission for {settings.commandChar}{command.commandString}: {permissionsService.GetPermissionsLevel(messageData.username)} vs {command.requiredPermissions}."); // Debug-only message
                return;
            }

            // If it's a core command and user has permission, execute its functionality and trigger the cooldown.
            if (command is ICoreCommand executable)
            {
                await executable.ExecuteAsync(messageData, tokens);
                cooldownTracker.StartCooldown(command.commandString);
            }
            // Otherwise if it's a custom command and user has permission, issue its response and trigger the cooldown.
            else if (command is CustomCommandDefinition custom)
            {
                Console.WriteLine(custom.commandResponse);
                cooldownTracker.StartCooldown(command.commandString);
            }
            // This should never execute because all commands fit into one of the above categories.
            else Console.WriteLine($"Unable to identify {settings.commandChar}{command.commandString} command type.");
        }
        // If a message starts with a command character but is not a valid command, there is no need to respond.
    }

    // RegisterCommand handles the user-facing aspects of registration, calling RegisterCommandInternal to handle the rest. These functions are distinct from UpdateCommand to minimize potential user issues.
    public void RegisterCommand(ICommand command)
    {
        if (RegisterCommandInternal(command))
        {
            Console.WriteLine($"Command added for {settings.commandChar}{command.commandString}.");
        }
        else Console.WriteLine($"{settings.commandChar}{command.commandString} already exists.");
    }

    // RegisterCommandInternal acts as the single authoritative path for command registration, verifying and registering incoming commands, with success or failure being reported to the caller. Validation is performed by the CommandAdminCommand prior to reaching this point.
    bool RegisterCommandInternal(ICommand command)
    {
        // Identify whether a command exists or not. Since core commands are registered during setup, this prevents users from adding custom commands using the same strings.

        // If it doesn't exist already, add it.
        if (!commandRegistry.ContainsKey(command.commandString))
        {
            commandRegistry.Add(command.commandString, command);
            return true;
        }
        else return false;
    }

    // RemoveCommand removes eligible commands while protecting immutable commands (which typically represent core functionality).
    public void UnregisterCommand(string commandString)
    {
        // Locate the command in the command registry.
        if (commandRegistry.TryGetValue(commandString, out ICommand registeredCommand))
        {
            // If the command is present, identify whether it is mutable. If it is not mutable, the user may not remove it.
            if (!registeredCommand.isMutable)
            {
                Console.WriteLine($"{settings.commandChar}{registeredCommand.commandString} may not be removed.");
                return;
            }

            // Otherwise, remove the registered command.
            commandRegistry.Remove(commandString);
            Console.WriteLine($"{settings.commandChar}{commandString} command removed.");
        }
        else Console.WriteLine($"No command for {settings.commandChar}{commandString} found.");
    }

    // UpdateCommand updates eligible commands while protecting immutable commands (which typically represent core functionality). The passed command argument should be the new version of the command.
    public void UpdateCommand(ICommand command)
    {
        // Locate the command in the command registry.
        if (commandRegistry.TryGetValue(command.commandString, out ICommand registeredCommand))
        {
            // If the command is present, identify whether it is mutable. If it is not mutable, the user may not change it.
            // By checking the currently registered command for mutability instead of the incoming command, it prevents updates from trying to trick the registry into allowing it to change a mutable command.
            if (!registeredCommand.isMutable)
            {
                Console.WriteLine($"{settings.commandChar}{registeredCommand.commandString} may not be changed.");
                return;
            }

            // Otherwise, update the registered command.
            commandRegistry[command.commandString] = command;
            Console.WriteLine($"{settings.commandChar}{command.commandString} command updated.");
        }
        else Console.WriteLine($"No command for {settings.commandChar}{command.commandString} found.");
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
        List<CustomCommandDefinition> customCommands = commandRegistry
            .Values
            .OfType<CustomCommandDefinition>()
            .ToList();

        // Future extensions of mutable command types will need to extract their commands separately, or adjust the above LINQ statement to handle it accordingly.

        await ConfigService.StoreCommandConfig(new CommandConfig(settings, customCommands));
    }

    static async Task<CommandConfig> GenerateDefaultConfig()
    {
        // Construct the default config
        CommandSettings commandSettings = new CommandSettings()
        {
            commandChar = ' '
        };

        List<CustomCommandDefinition> customCommands = new List<CustomCommandDefinition>();

        CommandConfig config = new CommandConfig(commandSettings, customCommands);

        // Store it for future retrieval
        await ConfigService.StoreCommandConfig(config);

        // Send it back for use
        return config;
    }
}