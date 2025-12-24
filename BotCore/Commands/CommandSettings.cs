using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;
internal class CommandSettings
{
    // int globalCooldown { get; set; } = 3;      // Cooldown applied to all non-exempt commands
    public char commandChar { get; set; } = '!';        // Character required to precede all commands.
}
