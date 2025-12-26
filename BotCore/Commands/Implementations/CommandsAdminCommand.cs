using BotCore.Core;
using BotCore.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace BotCore.Commands.Implementations;
internal class CommandsAdminCommand : ICoreCommand
{
    public string commandString { get; init; } = "command";
    public string[]? commandAliases { get; set; } = [];
    public bool isMutable { get; init; } = false;

    enum CommandsAdminAction
    {
        Add,
        Remove,
        Update
    }

    class CommandsAdminArgs()
    {
        public CommandsAdminAction commandAction { get; init; }
        public string commandPhrase { get; init; }
        public string commandResponse { get; init; }
    }

    CommandService commandService;

    public CommandsAdminCommand(CommandService commandService)
    {
        this.commandService = commandService;
    }

    public async Task ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        // Parse the tokens into usable details
        if (!TryParseCommandArgs(messageData.message, out CommandsAdminArgs args, out string error))
        {
            Console.WriteLine($"Error: {error}");
            return;
        }

        // Switch on the action enum to issue requests to the commandService.
        switch (args.commandAction)
        {
            case CommandsAdminAction.Add:
                commandService.RegisterCommand(new CustomCommandDefinition(args.commandPhrase, args.commandResponse));
                await commandService.StoreCommandConfig();
                break;

            case CommandsAdminAction.Remove:
                commandService.UnregisterCommand(args.commandPhrase);
                await commandService.StoreCommandConfig();
                break;

            case CommandsAdminAction.Update:
                commandService.UpdateCommand(new CustomCommandDefinition(args.commandPhrase, args.commandResponse));
                await commandService.StoreCommandConfig();
                break;

            default:
                // Should never get here because TryParseCommandArgs should fail and return if there's any issue.
                Console.WriteLine("Error: Custom command parsing unsuccessful.");
                break;
        }
    }

    // TryParse functions to keep core processing clean.
    static bool TryParseCommandArgs(string message, out CommandsAdminArgs args, out string error)
    {
        // !command <add/remove/update> <commandString> <reactionString>
        // This command wants a different arrangement of tokens than other commands, so it must do its own tokenizing.

        string[] tokens = message
            .Substring(1)       // Remove the command character.
            .Split(' ', count: 4, StringSplitOptions.RemoveEmptyEntries);   // Split into 4 units; the first three are subcommands, while the 4th is the full command response.

        CommandsAdminAction action;
        string commandString;
        string commandResponse;

        args = null!;
        error = string.Empty;

        if (tokens.Length < 2)
        {
            error = "Error: Missing command action.";
            return false;
        }

        // Parse the subcommand into an action enum for switching. A success outputs the result into 'action' so we only need to act on a failure.
        if (!TryParseCommandAction(tokens[1], out action))
        {
            error = "Error: Command action not recognized. Try 'add', 'update', or 'remove'.";
            return false;
        }

        if (tokens.Length < 3)
        {
            error = "Error: Missing command phrase.";
            return false;
        }

        // Collect the commandString
        commandString = tokens[2];

        if (tokens.Length < 4 && action != CommandsAdminAction.Remove)
        {
            error = "Error: Missing command response.";
            return false;
        }

        // Collect the commandResponse if present.
        commandResponse = tokens.Length > 3 ? tokens[3] : "";

        // Construct the args and send them back for processing.
        args = new CommandsAdminArgs
        {
            commandAction = action,
            commandPhrase = commandString,
            commandResponse = commandResponse
        };

        return true;
    }

    static bool TryParseCommandAction(string token, out CommandsAdminAction action)
    {
        return Enum.TryParse(token, ignoreCase: true, out action);
    }
}
