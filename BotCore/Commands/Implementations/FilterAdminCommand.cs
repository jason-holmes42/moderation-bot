using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core.Cooldowns;
using BotCore.Core.Messaging;
using BotCore.Filtering;
using BotCore.Permissions;
using static System.Collections.Specialized.BitVector32;

namespace BotCore.Commands;
internal class FilterAdminCommand : ICoreCommand
{
    public string CommandString { get; init; } = "filter";
    public string[]? CommandAliases { get; set; } = [];
    public bool IsMutable { get; init; } = false;

    public CooldownType CooldownType { get; init; } = CooldownType.None;
    public TimeSpan? CooldownOverride { get; } = null;

    public PermissionsLevel RequiredPermissions { get; set; } = PermissionsLevel.Moderator;

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

    FilterService _filterService;

    public FilterAdminCommand(FilterService filterService)
    {
        _filterService = filterService;
    }

    public async virtual Task<string> ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        // Parse the tokens into usable details
        if (!TryParseFilterArgs(tokens, out FilterCommandArgs args, out string error))
        {
            return $"Error: {error}";
        }

        // Switch on the action enum to issue requests to the FilterService.
        switch (args.filterAction)
        {
            case FilterCommandAction.On:
                _filterService.ToggleFilter(true);
                return $"Filter activated.";

            case FilterCommandAction.Off:
                _filterService.ToggleFilter(false);
                return $"Filter deactivated.";

            case FilterCommandAction.Add:
                _filterService.AddFilterRule(messageData, args.filterPhrase, args.filterPunishment);
                await _filterService.StoreFilterConfig();
                break;

            case FilterCommandAction.Remove:
                _filterService.RemoveFilterRule(messageData, args.filterPhrase);
                await _filterService.StoreFilterConfig();
                break;

            case FilterCommandAction.Update:
                _filterService.UpdateFilterRule(messageData, args.filterPhrase, args.filterPunishment);
                await _filterService.StoreFilterConfig(); 
                break;

            default:
                // Should never get here because TryParseFilterArgs should fail and return if there's any issue.
                return "Error: Filter command parsing unsuccessful.";
        }

        return null;
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
