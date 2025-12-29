using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Filtering;
public enum PunishmentType
{
    // PunishmentTypes are given explicit integer values for comparison when prioritizing the severity of the punishment and should be assigned consistent with this intent, rather than implicit assumptions.
    Warning = 0,
    Timeout = 1,
    Ban = 2
}
