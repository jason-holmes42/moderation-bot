using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;
using BotCore.Filtering;
using BotCore.Permissions;
using static System.Collections.Specialized.BitVector32;

namespace BotCore.Commands;
internal class FilterAdminCommand : ICoreCommand
{
    public string commandString { get; init; } = "filter";
    public string[]? commandAliases { get; set; } = [];
    public bool isMutable { get; init; } = false;

    public CooldownType cooldownType { get; init; } = CooldownType.None;
    public TimeSpan? cooldownOverride { get; } = null;

    public PermissionsLevel requiredPermissions { get; set; } = PermissionsLevel.Moderator;

    enum FilterCommandAction
    {
        On,
        Off,
        Add,
        Remove,
        Update
    }

    class FilterCommandArgs()
    {
        public FilterCommandAction filterAction { get; init; }
        public string filterPhrase { get; init; } = string.Empty;
        public PunishmentType filterPunishment { get; init; }
    }

    FilterService filterService;

    public FilterAdminCommand(FilterService filterService)
    {
        this.filterService = filterService;
    }

    public async Task ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        // Parse the tokens into usable details
        if (!TryParseFilterArgs(tokens, out FilterCommandArgs args, out string error))
        {
            Console.WriteLine($"Error: {error}");
            return;
        }

        // Switch on the action enum to issue requests to the FilterService.
        switch (args.filterAction)
        {
            case FilterCommandAction.On:
                await filterService.ToggleFilter(true);
                break;

            case FilterCommandAction.Off:
                await filterService.ToggleFilter(false);
                break;

            case FilterCommandAction.Add:
                filterService.AddFilterRule(args.filterPhrase, args.filterPunishment);
                await filterService.StoreFilterConfig();
                break;

            case FilterCommandAction.Remove:
                filterService.RemoveFilterRule(args.filterPhrase);
                await filterService.StoreFilterConfig();
                break;

            case FilterCommandAction.Update:
                filterService.UpdateFilterRule(args.filterPhrase, args.filterPunishment);
                await filterService.StoreFilterConfig();
                break;

            default:
                // Should never get here because TryParseFilterArgs should fail and return if there's any issue.
                Console.WriteLine("Error: Filter command parsing unsuccessful.");
                break;
        }
    }

    // TryParse functions to keep core processing clean.
    static bool TryParseFilterArgs(string[] tokens, out FilterCommandArgs args, out string error)
    {
        // !filter <on/off/add/remove/update> <filteredString> <punishment>
        // token[0] = filter
        // token[1] = <on/off/add/remove/update>
        // token[2] = filteredString
        // token[3] = action to take upon detection. May be null in cases of `!filter remove filteredString` since filteredStrings are unique.

        FilterCommandAction action;
        PunishmentType reaction = PunishmentType.Timeout;

        args = null!;
        error = string.Empty;


        if (tokens.Length < 2)
        {
            error = "Missing filter action.";
            return false;
        }

        // Parse the subcommand into an action enum for switching. A success outputs the result into 'action' so we only need to act on a failure.
        if (!TryParseFilterAction(tokens[1], out action))
        {
            error = "Filter action not recognized.";
            return false;
        }

        if (tokens.Length < 3)
        {
            error = "Missing filter phrase.";
            return false;
        }

        // Check to see if a punishment is present. If so, parse it.
        if (tokens.Length > 3)
        {
            if (!TryParseFilterPunishment(tokens[3], out reaction))
            {
                error = "Punishment type not recognized.";
                return false;
            }
        }

        // Construct the args and send them back for processing.
        args = new FilterCommandArgs
        {
            filterAction = action,
            filterPhrase = tokens[2],
            filterPunishment = reaction
        };

        return true;
    }

    static bool TryParseFilterAction(string token, out FilterCommandAction action)
    {
        return Enum.TryParse(token, ignoreCase: true, out action);
    }

    static bool TryParseFilterPunishment(string token, out PunishmentType reaction)
    {
        return Enum.TryParse(token, ignoreCase: true, out reaction);
    }
}
