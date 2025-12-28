using BotCore.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;
internal class CommandSettings
{
    public char commandChar { get; set; } = '!';        // Character required to precede all commands.
    public PermissionsLevel cooldownExemptionLevel { get; set; } = PermissionsLevel.Moderator;      // Minimum level required to circumvent cooldown restrictions
    // custom cooldowns for cooldown types, etc.
}
