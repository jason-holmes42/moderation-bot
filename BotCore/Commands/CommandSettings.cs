using ChatModerationBot.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Commands;
internal class CommandSettings
{
    public char CommandChar { get; set; } = '!';        // Character required to precede all commands.
    public PermissionsLevel CooldownExemptionLevel { get; set; } = PermissionsLevel.Moderator;      // Minimum level required to circumvent cooldown restrictions
    // custom cooldowns for cooldown types, etc.
}
