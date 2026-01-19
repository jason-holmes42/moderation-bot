using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Permissions;

// PermissionsLevel indicates the user's registered permissions and the minimum level required to access a command / be exempt from the filter.
internal enum PermissionsLevel
{
    // Enum values are assigned specific levels to ensure hierarchal processing consistency.
    None = 0,
    Regular = 1,
    Moderator = 2,
    Admin = 3,
    Broadcaster = 4
}
