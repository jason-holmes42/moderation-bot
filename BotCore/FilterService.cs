using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotCore;

internal class FilterService
{
    List<FilterRule> filterRules;
    Dictionary<string, FilterRule> filteredPhrases;

    private FilterService(Dictionary<string, FilterRule> filterPhrases, List<FilterRule> filterRules)
    {
        this.filteredPhrases = filterPhrases;
        this.filterRules = filterRules;

        // ConfigService.StoreFilterRules(this.filterRules);
    }

    public static async Task<FilterService> CreateAsync()
    {
        // Test phrases
        List<FilterRule> filterRules = (await ConfigService.RetrieveFilterRules()).ToList();

        // Create a matchable dictionary out of the filter rules
        Dictionary<string, FilterRule> filteredPhrases = new Dictionary<string, FilterRule>();

        foreach (FilterRule rule in filterRules) filteredPhrases.Add(rule.filterPhrase, rule);

        // Pass both the match dictionary and the list of rules to the constructor.
        return new FilterService(filteredPhrases, filterRules);
    }

    public void Evaluate(MessageContext messageData)
    {
        // Assess message body for filtered phrases
        foreach (var pattern in filteredPhrases)
        {
            if (Regex.IsMatch(messageData.message, pattern.Key, RegexOptions.IgnoreCase))
            {
                // If message contains a filtered phrase, mark its reaction type and reaction string. Based on this, the punishment will be triggered elsewhere.
                messageData.reactionType = pattern.Value.reactionType;
                messageData.reactionString = $"{messageData.reactionType.ToString().ToUpper()} {messageData.username} REASON: Matched '{pattern.Key}' filter.";
            }
        }
    }
}
