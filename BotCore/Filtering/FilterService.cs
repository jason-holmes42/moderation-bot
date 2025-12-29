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

namespace BotCore.Filtering;

internal class FilterService
{
    PermissionsService permissionsService;

    Dictionary<string, FilterRule> phraseDictionary;
    FilterSettings settings;

    // Explicit conversion dictionary to avoid messy, unsafe enum casts or Enum.Parse usages
    static readonly Dictionary<PunishmentType, ReactionType> PunishmentToReaction = new Dictionary<PunishmentType, ReactionType>()
    {
        { PunishmentType.Warning, ReactionType.Warning },
        { PunishmentType.Timeout, ReactionType.Timeout },
        { PunishmentType.Ban, ReactionType.Ban },
    };

    private FilterService(FilterSettings settings, Dictionary<string, FilterRule> filterPhrases, PermissionsService permissionsService)
    {
        this.settings = settings;
        this.phraseDictionary = filterPhrases;
        this.permissionsService = permissionsService;
    }

    public static async Task<FilterService> CreateAsync(PermissionsService permissionsService)
    {
        // Retrieve filter config from storage. Failing that, generate a new one and save it to storage.
        FilterConfig config;
        config = await ConfigService.RetrieveFilterConfig();
        if (config == null) config = await GenerateDefaultConfig();
        
        // Create an easily-matchable dictionary out of the filter rules
        Dictionary<string, FilterRule> filteredPhrases = new Dictionary<string, FilterRule>();

        foreach (FilterRule rule in config.filterRules) filteredPhrases.Add(rule.filterPhrase, rule);

        // Pass the new phrase dictionary to the constructor.
        return new FilterService(config.filterSettings, filteredPhrases, permissionsService);
    }

    public void Evaluate(MessageContext messageData)
    {
        // If the filter has been disabled, do not evaluate a message.
        if (!settings.filterEnabled) return;

        // In the case of multiple matches in a single message, the strongest punishment should be prioritized.
        PunishmentType? strongestPunishment = null;

        // Assess message body for filtered phrases
        foreach (var rule in phraseDictionary.Values)
        {
            if (rule.regexPattern.IsMatch(messageData.message))
            {
                // If message contains a filtered phrase, first check to see whether the user's permissions exempt them from the filter.
                if (permissionsService.HasPermission(messageData.username, settings.filterExemptionLevel)) return;

                // If not exempt, mark the message's reaction type and reaction string. Based on this, the punishment will be triggered elsewhere.
                if (strongestPunishment == null || rule.punishType > strongestPunishment)
                {
                    strongestPunishment = rule.punishType;
                    messageData.reactionType = PunishmentToReaction[rule.punishType];
                    messageData.reactionString = $"{messageData.reactionType.ToString().ToUpper()} {messageData.username} REASON: Matched '{rule.filterPhrase}' filter.";
                }

                // An expansion of the Punishment functionality would want to be called directly here.
            }
        }
    }

    // Switch the filter from on to off or vice versa.
    public async Task ToggleFilter(bool newState)
    {
        settings.filterEnabled = newState;    // Default state of the filter
        Console.WriteLine($"Filter {(newState ? "" : "de")}activated.");
    }

    // Rule management functions. Add and Update could be safely combined, but keeping them distinct will help users keep the impact of accidental commands minimal.
    public void AddFilterRule(string filteredPhrase, PunishmentType reaction)
    {
        // The command string's tokens will be parsed by FilterCommand, so there is no need to parse them here.

        // Add the new FilterRule to the phrase dictionary
        if (phraseDictionary.TryGetValue(filteredPhrase, out FilterRule filterRule))
        {
            Console.WriteLine($"{filteredPhrase} is already marked for {filterRule.punishType.ToString().ToUpper()}.");
        }
        else
        {
            phraseDictionary.Add(filteredPhrase, new FilterRule(filteredPhrase, reaction));
            Console.WriteLine($"Filter added for {filteredPhrase} with punishment of {phraseDictionary[filteredPhrase].punishType.ToString().ToUpper()}.");
        }
    }

    public void RemoveFilterRule(string filteredPhrase)
    {
        // The command string's tokens will be parsed by FilterCommand, so there is no need to parse them here.

        // Locate the filteredPhrase from the phrase dictionary and remove it.
        if (phraseDictionary.ContainsKey(filteredPhrase))
        {
            phraseDictionary.Remove(filteredPhrase);
            Console.WriteLine($"{filteredPhrase} filter removed.");
        }
        else
        {
            Console.WriteLine($"No filter for {filteredPhrase} found.");
        }
    }

    public void UpdateFilterRule(string filteredPhrase, PunishmentType updatedReaction)
    {
        // The command string's tokens will be parsed and verified by FilterCommand, so there is no need to parse them here.

        // Locate the filteredPhrase from the phrase dictionary and, if it exists, update it.
        if (phraseDictionary.TryGetValue(filteredPhrase, out FilterRule filterRule))
        {
            if (filterRule.punishType == updatedReaction)
            {
                Console.WriteLine($"{filteredPhrase} is already marked for {filterRule.punishType.ToString().ToUpper()}!");
            }
            else
            {
                filterRule.punishType = updatedReaction;
                Console.WriteLine($"{filteredPhrase} filter updated to apply {filterRule.punishType.ToString().ToUpper()}.");
            }
        }
        else Console.WriteLine($"{filteredPhrase} filter not found.");
    }

    // Handle converting the phrase dictionary to a List of FilterRules and sending it off for storage.
    public async Task StoreFilterConfig()
    {
        await ConfigService.StoreFilterConfig(new FilterConfig(settings, phraseDictionary.Values.ToList()));
    }

    static async Task<FilterConfig> GenerateDefaultConfig()
    {
        // Construct the default config
        FilterSettings filterSettings = new FilterSettings()
        {
            filterEnabled = true
        };

        List<FilterRule> filterRules = new List<FilterRule>();
        FilterConfig config = new FilterConfig(filterSettings, filterRules);

        // Store it for future retrieval
        await ConfigService.StoreFilterConfig(config);

        // Send it back for use
        return config;
    }
}