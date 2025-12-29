using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotCore.Filtering;
public class FilterRule
{
    public string filterPhrase { get; init; }
    public PunishmentType punishType { get; set; }

    [JsonIgnore]
    public Regex regexPattern { get; init; }

    public FilterRule(string filterPhrase, PunishmentType punishType)
    {
        this.filterPhrase = filterPhrase;
        this.punishType = punishType;
        this.regexPattern = new Regex(filterPhrase, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
