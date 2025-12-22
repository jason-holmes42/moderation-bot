using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore;
public class FilterRule
{
    public string filterPhrase { get; init; }
    public ReactionType reactionType { get; set; }
}
