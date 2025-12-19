using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotCore;

internal class Filter
{
    public static void Evaluate(ChatMessage messageData)
    {
        // Test phrases
        string[] filteredPhrases =
        [
            "honk",
            "evoker",
            "\bsummon\b"
        ];

        // Assess message body for filtered phrases
        foreach (string pattern in filteredPhrases)
        {
            if (Regex.IsMatch(messageData.message, pattern, RegexOptions.IgnoreCase))
            {
                // If message contains a filtered phrase, mark its reaction type and reaction string. Based on this, the punishment will be triggered elsewhere.
                // Different reaction severities will be handled in a future commit. For now, we'll just mark them as ban.
                messageData.reactionType = ReactionType.Ban;
                messageData.reactionString = $"BAN {messageData.username} REASON: Matched '{pattern}' filter.";
            }
        }
    }
}
