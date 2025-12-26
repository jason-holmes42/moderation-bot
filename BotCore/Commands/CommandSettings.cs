using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;
internal class CommandSettings
{
    public char commandChar { get; set; } = '!';        // Character required to precede all commands.
    // custom cooldowns for cooldown types, etc.
}
