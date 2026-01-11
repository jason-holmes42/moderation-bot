using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotCore.Core;
using BotCore.Configuration;
using BotCore.Permissions;
using BotCore.Core.Messaging;
using BotCore.Core.Providers;
using System.Net;

namespace BotCore.Filtering;

internal class FilterService
{
    UserContext _userContext;

    PermissionsService _permissionsService;

    Dictionary<string, FilterRule> _phraseDictionary;
    FilterSettings _settings;

    // readonly Func<query, response> _providerQuery;

    private FilterService(
        UserContext userContext,
        FilterConfig config,
        PermissionsService permissionsService)
    {
        _userContext = userContext;

        // Create an easily-matchable dictionary out of the filter rules
        Dictionary<string, FilterRule> filteredPhrases = new Dictionary<string, FilterRule>();

        foreach (FilterRule rule in config.FilterRules) filteredPhrases.Add(rule.FilterPhrase, rule);

        _settings = config.FilterSettings;
        _phraseDictionary = filteredPhrases;
        _permissionsService = permissionsService;
    }

    public static async Task<FilterService> CreateAsync(UserContext userContext, PermissionsService permissionsService)
    {
        // Retrieve filter config from storage. Failing that, generate a new one and save it to storage.
        FilterConfig config;
        config = await ConfigService.RetrieveConfigAsync<FilterConfig>(userContext);
        if (config == null) config = await GenerateDefaultConfig();
        
        // Pass the new phrase dictionary to the constructor.
        return new FilterService(userContext, config, permissionsService);
    }
    
    // Internal, test-only constructor. Accepts whatever the test provides it and generates defaults for everything else, then chains into the normal constructor to centralize initialization.
    internal FilterService(
        bool testOnly,
        UserContext? userContext = null,
        FilterConfig? filterConfig = null,
        PermissionsService? permissionsService = null)
        : this(
            userContext ?? new UserContext("__TESTUSER__"),
            filterConfig ?? GenerateDefaultConfig().GetAwaiter().GetResult(),
            permissionsService ?? new PermissionsService(testOnly: true)
        )
    { }

    public void Evaluate(MessageContext messageData)
    {
        // If the filter has been disabled, do not evaluate a message.
        if (!_settings.FilterEnabled) return;

        // In the case of multiple matches in a single message, the strongest punishment should be prioritized.
        PunishmentType? strongestPunishment = null;

        // Assess message body for filtered phrases
        foreach (var rule in _phraseDictionary.Values)
        {
            if (rule.RegexPattern.IsMatch(messageData.Message))
            {
                // If message contains a filtered phrase, first check to see whether the user's permissions exempt them from the filter.
                if (_permissionsService.HasPermission(messageData.Endpoint.Platform, messageData.Username, _settings.FilterExemptionLevel)) return;

                // If not exempt, mark the message's reaction type and reaction string. Based on this, the punishment will be triggered elsewhere.
                if (strongestPunishment == null || rule.PunishType > strongestPunishment)
                {
                    strongestPunishment = rule.PunishType;

                    if (messageData.ModAction == null) messageData.ModAction = new ModerationAction();

                    messageData.ModAction.TargetUser = messageData.Username;
                    messageData.ModAction.Punishment = rule.PunishType;
                    messageData.ModAction.Reason = $"Matched '{rule.FilterPhrase}' filter.";
                }

                // An expansion of the Punishment functionality would want to be called directly here.
            }
        }
    }

    // Switch the filter from on to off or vice versa.
    public void ToggleFilter(bool newState)
    {
        _settings.FilterEnabled = newState;    // Default state of the filter
    }

    // Rule management functions. Add and Update could be safely combined, but keeping them distinct will help users keep the impact of accidental commands minimal.
    public void AddFilterRule(MessageContext messageData, string filteredPhrase, PunishmentType reaction)
    {
        // The command string's tokens will be parsed by FilterCommand, so there is no need to parse them here.

        // Add the new FilterRule to the phrase dictionary
        if (_phraseDictionary.TryGetValue(filteredPhrase, out FilterRule filterRule))
        {
            messageData.ReactionString = $"That phrase is already marked for {filterRule.PunishType.ToString().ToUpper()}.";
        }
        else
        {
            _phraseDictionary.Add(filteredPhrase, new FilterRule(filteredPhrase, reaction));
            messageData.ReactionString = $"Filter added with punishment of {_phraseDictionary[filteredPhrase].PunishType.ToString().ToUpper()}.";
        }
    }

    public void RemoveFilterRule(MessageContext messageData, string filteredPhrase)
    {
        // The command string's tokens will be parsed by FilterCommand, so there is no need to parse them here.

        // Locate the filteredPhrase from the phrase dictionary and remove it.
        if (_phraseDictionary.ContainsKey(filteredPhrase))
        {
            _phraseDictionary.Remove(filteredPhrase);
            messageData.ReactionString = $"{filteredPhrase} filter removed.";
        }
        else
        {
            messageData.ReactionString = $"No filter for {filteredPhrase} found.";
        }
    }

    public void UpdateFilterRule(MessageContext messageData, string filteredPhrase, PunishmentType updatedReaction)
    {
        // The command string's tokens will be parsed and verified by FilterCommand, so there is no need to parse them here.

        // Locate the filteredPhrase from the phrase dictionary and, if it exists, update it.
        if (_phraseDictionary.TryGetValue(filteredPhrase, out FilterRule filterRule))
        {
            if (filterRule.PunishType == updatedReaction)
            {
                messageData.ReactionString = $"That phrase is already marked for {filterRule.PunishType.ToString().ToUpper()}!";
            }
            else
            {
                filterRule.PunishType = updatedReaction;
                messageData.ReactionString = $"Filter updated to apply {filterRule.PunishType.ToString().ToUpper()}.";
            }
        }
        else messageData.ReactionString = $"{filteredPhrase} filter not found.";
    }

    // Handle converting the phrase dictionary to a List of FilterRules and sending it off for storage.
    public async Task StoreFilterConfig()
    {
        await ConfigService.StoreConfigAsync(_userContext, new FilterConfig(_settings, _phraseDictionary.Values.ToList()));
    }

    static async Task<FilterConfig> GenerateDefaultConfig()
    {
        // Construct the default config
        FilterSettings filterSettings = new FilterSettings()
        {
            FilterEnabled = true,
            FilterExemptionLevel = PermissionsLevel.Moderator
        };

        List<FilterRule> filterRules = new List<FilterRule>();
        FilterConfig config = new FilterConfig(filterSettings, filterRules);

        // Send it back for use
        return config;
    }
}