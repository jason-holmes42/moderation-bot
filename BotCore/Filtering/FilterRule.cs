using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatModerationBot.Filtering;
public class FilterRule
{
    public string FilterPhrase { get; init; }
    public PunishmentType PunishType { get; set; }

    [JsonIgnore]
    public Regex RegexPattern { get; init; }

    public FilterRule(string filterPhrase, PunishmentType punishType)
    {
        FilterPhrase = filterPhrase;
        PunishType = punishType;
        RegexPattern = new Regex(filterPhrase, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
