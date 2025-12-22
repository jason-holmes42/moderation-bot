using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotCore;

internal class FilterService
{
    Dictionary<string, ReactionType> filteredPhrases;

    private FilterService(Dictionary<string, ReactionType> filterRules)
    {
        filteredPhrases = filterRules;
    }

    public static async Task<FilterService> CreateAsync()
    {
        // Test phrases
        Dictionary<string, ReactionType> filteredPhrases = new Dictionary<string, ReactionType>();

        filteredPhrases.Add("honk", ReactionType.Ban);
        filteredPhrases.Add("\\bevoker\\b", ReactionType.Timeout);
        filteredPhrases.Add("summon", ReactionType.Ban);

        return new FilterService(filteredPhrases);
    }

    public void Evaluate(MessageContext messageData)
    {
        // Assess message body for filtered phrases
        foreach (var pattern in filteredPhrases)
        {
            if (Regex.IsMatch(messageData.message, pattern.Key, RegexOptions.IgnoreCase))
            {
                // If message contains a filtered phrase, mark its reaction type and reaction string. Based on this, the punishment will be triggered elsewhere.
                messageData.reactionType = pattern.Value;
                messageData.reactionString = $"{messageData.reactionType.ToString().ToUpper()} {messageData.username} REASON: Matched '{pattern.Key}' filter.";
            }
        }
    }
}
