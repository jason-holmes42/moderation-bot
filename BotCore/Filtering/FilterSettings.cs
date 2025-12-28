using BotCore.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Filtering;
internal class FilterSettings
{
    public bool filterStatus { get; set; } = true;      // Whether the filter is currently active or not.
    public PermissionsLevel filterExemptionLevel { get; set; } = PermissionsLevel.Moderator;        // Minimum level required to be exempt from filter
}
