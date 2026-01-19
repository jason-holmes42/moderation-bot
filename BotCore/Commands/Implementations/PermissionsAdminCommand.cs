using ChatModerationBot.Core.Cooldowns;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Filtering;
using ChatModerationBot.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Commands.Implementations;
internal class PermissionsAdminCommand : ICoreCommand
{
    public string CommandString { get; init; } = "permissions";
    public string[]? CommandAliases { get; set; } = [];
    public bool IsMutable { get; init; } = false;

    public CooldownType CooldownType { get; init; } = CooldownType.None;
    public TimeSpan? CooldownOverride { get; } = null;

    public PermissionsLevel RequiredPermissions { get; set; } = PermissionsLevel.Moderator;

    PermissionsService _permissionsService;

    internal enum PermissionsCommandAction
    {
        Set,
        Remove
    }

    internal class PermissionsCommandArgs()
    {
        public PermissionsCommandAction permissionsAction{ get; init; }
        public string targetUser { get; init; } = string.Empty;
        public PermissionsLevel targetLevel { get; init; }
    }

    public PermissionsAdminCommand(PermissionsService permissionsService)
    {
        _permissionsService = permissionsService;
    }

    public async virtual Task<string> ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        // Parse the tokens into usable details
        if (!TryParseCommandArgs(tokens, out PermissionsCommandArgs args, out string error))
        {
            return $"Error: {error}";
        }

        // Switch on the action enum to issue requests to the PermissionsService.
        switch (args.permissionsAction)
        {
            case PermissionsCommandAction.Set:
                _permissionsService.SetPermissions(messageData, args.targetUser, args.targetLevel);
                await _permissionsService.StorePermissionsConfig();
                break;

            case PermissionsCommandAction.Remove:
                _permissionsService.RemovePermissions(messageData, args.targetUser);
                await _permissionsService.StorePermissionsConfig();
                break;

            default:
                // Should never get here because TryParseCommandArgs should fail and return if there's any issue.
                return "Error: Permissions command parsing unsuccessful.";
        }

        return null;
    }

    internal static bool TryParseCommandArgs(string[] tokens, out PermissionsCommandArgs args, out string error)
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

        // If the action is set to remove, the target permissions level is irrelevant.
        if (action == PermissionsCommandAction.Remove)
        {
            args = new PermissionsCommandArgs { permissionsAction = action, targetUser = tokens[2] };
            return true;
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
        else
        {
            error = $"No permissions level indicated.";
            return false;
        }

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
