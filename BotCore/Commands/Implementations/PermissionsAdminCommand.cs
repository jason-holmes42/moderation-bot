using BotCore.Core;
using BotCore.Filtering;
using BotCore.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands.Implementations;
internal class PermissionsAdminCommand : ICoreCommand
{
    public string commandString { get; init; } = "permissions";
    public string[]? commandAliases { get; set; } = [];
    public bool isMutable { get; init; } = false;

    public CooldownType cooldownType { get; init; } = CooldownType.None;
    public TimeSpan? cooldownOverride { get; } = null;

    public PermissionsLevel requiredPermissions { get; set; } = PermissionsLevel.Moderator;

    PermissionsService permissionsService;

    enum PermissionsCommandAction
    {
        Set,
        Remove
    }

    class PermissionsCommandArgs()
    {
        public PermissionsCommandAction permissionsAction{ get; init; }
        public string targetUser { get; init; } = string.Empty;
        public PermissionsLevel targetLevel { get; init; }
    }

    public PermissionsAdminCommand(PermissionsService permissionsService)
    {
        this.permissionsService = permissionsService;
    }

    public async Task ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        // Parse the tokens into usable details
        if (!TryParseCommandArgs(tokens, out PermissionsCommandArgs args, out string error))
        {
            Console.WriteLine($"Error: {error}");
            return;
        }

        // Switch on the action enum to issue requests to the PermissionsService.
        switch (args.permissionsAction)
        {
            case PermissionsCommandAction.Set:
                permissionsService.SetPermissions(messageData.username, args.targetUser, args.targetLevel);
                await permissionsService.StorePermissionsConfig();
                break;

            case PermissionsCommandAction.Remove:
                permissionsService.RemovePermissions(messageData.username, args.targetUser);
                await permissionsService.StorePermissionsConfig();
                break;

            default:
                // Should never get here because TryParseCommandArgs should fail and return if there's any issue.
                Console.WriteLine("Error: Permissions command parsing unsuccessful.");
                break;
        }
    }

    static bool TryParseCommandArgs(string[] tokens, out PermissionsCommandArgs args, out string error)
    {
        // !permissions <set/remove> <targetUser> [permissionsLevel]
        // tokens[0] = permissions
        // tokens[1] = <set/remove>
        // tokens[2] = <targetUser>
        // tokens[3] = [permissionsLevel]

        PermissionsCommandAction action;

        args = null!;
        error = string.Empty;
        PermissionsLevel targetLevel = PermissionsLevel.None;

        if (tokens.Length < 2)
        {
            error = "Missing permissions action.";
            return false;
        }

        // Parse the subcommand into an action enum for switching. A success outputs the result into 'action' so we only need to act on a failure.
        if (!TryParseCommandAction(tokens[1], out action))
        {
            error = "Permissions action not recognized.";
            return false;
        }

        if (tokens.Length < 3)
        {
            error = "Missing target user.";
            return false;
        }

        // Check to see if a target permissions level is present. If so, parse it.
        if (tokens.Length > 3)
        {
            if (!TryParsePermissionsLevel(tokens[3], out targetLevel))
            {
                error = $"Permissions level {tokens[3]} not recognized.";
                return false;
            }
        }
        // If the target level is not present, then !permissions remove <targetUser> is being used and the default level of None should stand.

        // Construct the args and send them back for processing.
        args = new PermissionsCommandArgs
        {
            permissionsAction = action,
            targetUser = tokens[2],
            targetLevel = targetLevel
        };

        return true;
    }

    static bool TryParseCommandAction(string token, out PermissionsCommandAction action)
    {
        return Enum.TryParse(token, ignoreCase: true, out action);
    }

    static bool TryParsePermissionsLevel(string token, out PermissionsLevel targetLevel)
    {
        return Enum.TryParse(token, ignoreCase: true, out targetLevel);
    }
}
