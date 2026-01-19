using ChatModerationBot.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Filtering;
internal class FilterSettings
{
    public bool FilterEnabled { get; set; } = true;      // Whether the filter is currently active or not.
    public PermissionsLevel FilterExemptionLevel { get; set; } = PermissionsLevel.Moderator;        // Minimum level required to be exempt from filter
}
