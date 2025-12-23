using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;

namespace BotCore.Filtering;
public class FilterRule
{
    public string filterPhrase { get; init; }
    public PunishmentType punishType { get; set; }

    public FilterRule(string filterPhrase, PunishmentType punishType)
    {
        this.filterPhrase = filterPhrase;
        this.punishType = punishType;
    }
}
