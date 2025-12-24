using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotCore.Core;
using BotCore.Configuration;

namespace BotCore.Filtering;

internal class FilterService
{
    Dictionary<string, FilterRule> phraseDictionary;
    FilterSettings settings;
    bool activeState = true;

    private FilterService(FilterSettings settings, Dictionary<string, FilterRule> filterPhrases)
    {
        this.settings = settings;
        this.phraseDictionary = filterPhrases;
    }

    public static async Task<FilterService> CreateAsync()
    {
        // Retrieve filter rules from storage
        FilterConfig config = await ConfigService.RetrieveFilterConfig();

        // Create an easily-matchable dictionary out of the filter rules
        Dictionary<string, FilterRule> filteredPhrases = new Dictionary<string, FilterRule>();

        foreach (FilterRule rule in config.filterRules) filteredPhrases.Add(rule.filterPhrase, rule);

        // Pass the new phrase dictionary to the constructor.
        return new FilterService(config.filterSettings, filteredPhrases);
    }

    public void Evaluate(MessageContext messageData)
    {
        // If the filter has been disabled, do not evaluate a message.
        if (!activeState) return;

        // Assess message body for filtered phrases
        foreach (var rule in phraseDictionary.Values)
        {
            if (rule.regexPattern.IsMatch(messageData.message))
            {
                // If message contains a filtered phrase, mark its reaction type and reaction string. Based on this, the punishment will be triggered elsewhere.
                // The cast from PunishmentType to ReactionType is safe without TryParse or other protections because PunishmentType is a subset of ReactionType.
                messageData.reactionType = (ReactionType) Enum.Parse(typeof(ReactionType), rule.punishType.ToString(), ignoreCase: true);
                messageData.reactionString = $"{messageData.reactionType.ToString().ToUpper()} {messageData.username} REASON: Matched '{rule.filterPhrase}' filter.";

                // An expansion of the Punishment functionality would want to be called directly here.
            }
        }
    }

    // Switch the filter from on to off or vice versa.
    public async Task ToggleFilter(bool newState)
    {
        activeState = newState;                 // Active state of the filter
        Console.WriteLine($"Filter {(newState ? "" : "de")}activated.");
        settings.filterStatus = activeState;    // Default state of the filter
        await StoreFilterConfig();
    }

    // Rule management functions. Add and Update could be safely combined, but keeping them distinct will help users keep the impact of accidental commands minimal.
    public async Task AddFilterRule(string filteredPhrase, PunishmentType reaction)
    {
        // The command string's tokens will be parsed by FilterCommand, so there is no need to parse them here.

        // Add the new FilterRule to the phrase dictionary
        if (phraseDictionary.ContainsKey(filteredPhrase))
        {
            Console.WriteLine($"{filteredPhrase} is already marked for {phraseDictionary[filteredPhrase].punishType.ToString().ToUpper()}! You can change the punishment type with `!filter update <phrase> <new punishment>`.");
        }
        else
        {
            phraseDictionary.Add(filteredPhrase, new FilterRule(filteredPhrase, reaction));
            Console.WriteLine($"Filter added for {filteredPhrase} with punishment of {phraseDictionary[filteredPhrase].punishType.ToString().ToUpper()}.");
        }

        // Save the updated dictionary.
        await StoreFilterConfig();
    }

    public async Task RemoveFilterRule(string filteredPhrase)
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

        // Save the updated dictionary.
        await StoreFilterConfig();
    }

    public async Task UpdateFilterRule(string filteredPhrase, PunishmentType updatedReaction)
    {
        // The command string's tokens will be parsed and verified by FilterCommand, so there is no need to parse them here.

        // Locate the filteredPhrase from the phrase dictionary and, if it exists, update it.
        if (phraseDictionary.ContainsKey(filteredPhrase))
        {
            if (phraseDictionary[filteredPhrase].punishType == updatedReaction)
            {
                Console.WriteLine($"{filteredPhrase} is already marked for {phraseDictionary[filteredPhrase].punishType.ToString().ToUpper()}!");
            }
            else
            {
                phraseDictionary[filteredPhrase].punishType = updatedReaction;
                Console.WriteLine($"{filteredPhrase} filter updated to apply {phraseDictionary[filteredPhrase].punishType.ToString().ToUpper()}.");
            }
        }
        else Console.WriteLine($"{filteredPhrase} filter not found. You can add a new phrase to the filter by using `!filter add <phrase> <punishment>`.");

        // Save the updated dictionary.
        await StoreFilterConfig();
    }

    // Handle converting the phrase dictionary to a List of FilterRules and sending it off for storage.
    async Task StoreFilterConfig()
    {
        await ConfigService.StoreFilterConfig(new FilterConfig(settings, phraseDictionary.Values.ToList()));
    }
}
