using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;
using BotCore.Commands.Implementations;
using BotCore.Filtering;
using BotCore.Configuration;

namespace BotCore.Commands;
internal class CommandService
{
    CommandSettings settings;
    Dictionary<string, ICommand> coreCommands;
    Dictionary<string, CustomCommand> customCommands;

    FilterService filterService;

    private CommandService(CommandSettings settings, Dictionary<string, CustomCommand> customCommands, FilterService filterService)
    {
        // Commands all bots may access.
        coreCommands = new Dictionary<string, ICommand>
        {
            {"uptime", new UptimeCommand() },
            {"filter", new FilterCommand(filterService) }
        };

        this.settings = settings;
        this.customCommands = customCommands;
        this.filterService = filterService;
    }

    public static async Task<CommandService> CreateAsync(FilterService filterService)
    {
        // Retrieve command config from storage. Failing that, generate a new one and save it to storage.
        CommandConfig config;
        config = await ConfigService.RetrieveCommandConfig();
        if (config == null) config = await GenerateDefaultConfig();

        // Create an easily-matchable dictionary out of the custom commands
        Dictionary<string, CustomCommand> storedCommands = new Dictionary<string, CustomCommand>();

        foreach (CustomCommand command in config.customCommands) storedCommands.Add(command.commandString, command);

        // Pass the settings, custom commands, and filter service to the constructor
        return new CommandService(config.commandSettings, storedCommands, filterService);
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
        Console.WriteLine($"Command identified: {String.Join(", ", tokens)}");
        
        // Check registered core commands for a match
        if (coreCommands.TryGetValue(tokens[0], out ICommand coreCommand))
        {
            await coreCommand.ExecuteAsync(messageData, tokens);
        }

        // Check registered custom commands for a match
        if (customCommands.TryGetValue(tokens[0], out CustomCommand customCommand))
        {
            Console.WriteLine(customCommand.reactionString);
        }

    }

    // Custom command management functions. Add and Update could be safely combined, but keeping them distinct will help users keep the impact of accidental commands minimal.
    public async Task AddCustomCommand(string commandString, string reactionString)
    {
        // The command string's tokens will be parsed by the time it arrives here, so there is no need to do so again.

        // Add the new CustomCommand to the phrase dictionary
        if (customCommands.TryGetValue(commandString, out CustomCommand customCommand))
        {
            Console.WriteLine($"{settings.commandChar}{commandString} already exists.");
        }
        else
        {
            customCommands.Add(commandString, new CustomCommand(commandString, reactionString));
            Console.WriteLine($"Command added for {settings.commandChar}{commandString}.");
        }

        // Save the updated dictionary.
        await StoreCommandConfig();
    }

    public async Task RemoveCustomCommand(string commandString)
    {
        // The command string's tokens will be parsed by the time it arrives here, so there is no need to do so again.

        // Locate the commandString from the phrase dictionary and remove it.
        if (customCommands.ContainsKey(commandString))
        {
            customCommands.Remove(commandString);
            Console.WriteLine($"{settings.commandChar}{commandString} command removed.");
        }
        else Console.WriteLine($"No command for {settings.commandChar}{commandString} found.");

        // Save the updated dictionary.
        await StoreCommandConfig();
    }

    public async Task UpdateCustomCommand(string commandString, string updatedReaction)
    {
        // The command string's tokens will be parsed by the time it arrives here, so there is no need to do so again.

        // Locate the commandString in the customCommands dictionary and, if it exists, update it.
        if (customCommands.TryGetValue(commandString, out CustomCommand customCommand))
        {
            customCommand.reactionString = updatedReaction;
            Console.WriteLine($"{settings.commandChar}{commandString} command updated.");
        }
        else Console.WriteLine($"{settings.commandChar}{commandString} command not found.");

        // Save the updated dictionary.
        await StoreCommandConfig();
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
    async Task StoreCommandConfig()
    {
        await ConfigService.StoreCommandConfig(new CommandConfig(settings, customCommands.Values.ToList()));
    }

    static async Task<CommandConfig> GenerateDefaultConfig()
    {
        CommandSettings commandSettings = new CommandSettings()
        {
            // globalCooldown = 3
            commandChar = ' '
        };

        List<CustomCommand> customCommands = new List<CustomCommand>();

        CommandConfig config = new CommandConfig(commandSettings, customCommands);

        await ConfigService.StoreCommandConfig(config);

        return config;
    }
}