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
    Dictionary<string, FilterRule> filteredPhrases;
    FilterSettings settings;
    bool activeState = true;

    private FilterService(FilterSettings settings, Dictionary<string, FilterRule> filterPhrases)
    {
        this.settings = settings;
        this.filteredPhrases = filterPhrases;
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
        foreach (var pattern in filteredPhrases)
        {
            if (Regex.IsMatch(messageData.message, pattern.Key, RegexOptions.IgnoreCase))
            {
                // If message contains a filtered phrase, mark its reaction type and reaction string. Based on this, the punishment will be triggered elsewhere.
                // The cast from PunishmentType to ReactionType is safe without TryParse or other protections because PunishmentType is a subset of ReactionType.
                messageData.reactionType = (ReactionType) Enum.Parse(typeof(ReactionType), pattern.Value.punishType.ToString(), ignoreCase: true);
                messageData.reactionString = $"{messageData.reactionType.ToString().ToUpper()} {messageData.username} REASON: Matched '{pattern.Key}' filter.";

                // An expansion of the Punishment functionality would want to be called directly here.
            }
        }
    }

    // Switch the filter from on to off or vice versa.
    public void ToggleFilter()
    {
        activeState = !activeState;
        settings.filterStatus = activeState;
        StoreFilterConfig();
    }

    // Rule management functions. Add and Update could be safely combined, but keeping them distinct will help users keep the impact of accidental commands minimal.
    public void AddFilterRule(string[] tokens)
    {
        // !filter add <filteredPhrase> [<reaction>]
        // Construct the FilterRule to be added
        string filteredPhrase = tokens[2];
        PunishmentType reaction;

        // Since specifying the reaction is optional and we need to validate the input anyhow, we'll use TryParse.
        // On a success, reaction will already hold the new value, so there's no special need to assign it.
        // On a failure (if there's no match, or if there's no argument at all), assign Timeout as a default punishment.
        if (tokens.Length <= 3) reaction = PunishmentType.Timeout;
        else reaction = Enum.TryParse(tokens[3], ignoreCase: true, out reaction) ? reaction : PunishmentType.Timeout;

        // Add the new FilterRule to the phrase dictionary
        if (filteredPhrases.ContainsKey(filteredPhrase))
        {
            Console.WriteLine($"{filteredPhrase} is already marked for {filteredPhrases[filteredPhrase].punishType.ToString().ToUpper()}! You can change the punishment type with `!filter update <phrase> <new punishment>`.");
        }
        else
        {
            filteredPhrases.Add(filteredPhrase, new FilterRule(filteredPhrase, reaction));
            Console.WriteLine($"Filter added for {filteredPhrase} with punishment of {filteredPhrases[filteredPhrase].punishType.ToString().ToUpper()}.");
        }

        // Save the updated dictionary.
        StoreFilterConfig();
    }

    public void RemoveFilterRule(string[] tokens)
    {
        // !filter remove <filteredPhrase>
        string filteredPhrase = tokens[2];

        // Locate the filteredPhrase from the phrase dictionary and remove it.
        if (filteredPhrases.ContainsKey(filteredPhrase))
        {
            filteredPhrases.Remove(filteredPhrase);
            Console.WriteLine($"{filteredPhrase} filter removed.");
        }
        else
        {
            Console.WriteLine($"No filter for {filteredPhrase} found.");
        }

        // Save the updated dictionary.
        StoreFilterConfig();
    }

    public void UpdateFilterRule(string[] tokens)
    {
        // !filter update <filteredPhrase> <newPunishment>
        string filteredPhrase = tokens[2];

        // Verify the new punishment type
        PunishmentType reaction;
        Enum.TryParse(tokens[3], ignoreCase: true, out reaction);     // No default results here; if there is an error in the new punishment type, leave the current type as it is.

        // Locate the filteredPhrase from the phrase dictionary and, if it exists, update it.
        if (filteredPhrases.ContainsKey(filteredPhrase))
        {
            if (filteredPhrases[filteredPhrase].punishType == reaction)
            {
                Console.WriteLine($"{filteredPhrase} is already marked for {filteredPhrases[filteredPhrase].punishType.ToString().ToUpper()}!");
            }
            else
            {
                filteredPhrases[filteredPhrase].punishType = reaction;
                Console.WriteLine($"{filteredPhrase} filter updated to apply {filteredPhrases[filteredPhrase].punishType.ToString().ToUpper()}.");
            }
        }
        else Console.WriteLine($"{filteredPhrase} filter not found. You can add a new phrase to the filter by using `!filter add <phrase> <punishment>`.");

        // Save the updated dictionary.
        StoreFilterConfig();
    }

    // Handle converting the phrase dictionary to a List of FilterRules and sending it off for storage.
    async void StoreFilterConfig()
    {
        await ConfigService.StoreFilterConfig(new FilterConfig(settings, filteredPhrases.Values.ToList()));
    }
}
