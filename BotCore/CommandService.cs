using BotCore.commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore;
internal class CommandService
{
    // Commands all bots may access. customCommands will need to be built in the constructor due to loading requirement.
    Dictionary<string, ICommand> coreCommands = new Dictionary<string, ICommand>
    {
        { "uptime", new UptimeCommand() }
    };

    private CommandService()
    {
        // Dictionary customCommands = new Dictionary<string, CustomCommand>();
        // foreach (CustomCommand command in storedCommands) customCommands.Add(command.commandString, command);
    }

    public static async Task<CommandService> CreateAsync()
    {
        // List<CustomCommand> storedCommands = await ConfigService.RetrieveCustomCommands

        // return new CommandService(storedCommands);
        return new CommandService();
    }

    public async Task Evaluate(MessageContext messageData)
    {
        // Trim any whitespace characters for simple tokenization.
        string input = messageData.message.Trim();

        // Establish the command character that should precede commands. Custom command characters would be defined here.
        char commandChar = '!';

        // If the first character of the trimmed string is not the command character, skip. 
        if (input[0] != commandChar)
        {
            return;
        }

        // Elsewise, tokenize by space character ' ' and parse as a command.
        string[] tokens = TokenizeCommand(' ', input);
        Console.WriteLine($"Command identified: {String.Join(", ", tokens)}");
        
        // Check registered core commands for a match
        if (coreCommands.TryGetValue(tokens[0], out ICommand command))
        {
            if (command != null)
            {
                await command.ExecuteAsync(messageData, tokens);
            }
        }
        // Check registered custom commands for a match

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
}