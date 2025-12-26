using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core;

// CooldownType is utilized by all commands as a classification of cooldown type that should be used.
internal enum CooldownType
{
    None,               // For administration commands typically restricted by permissions levels, such as !filter
    CoreCommand,        // For commands tied to core functionality, such as !uptime
    CustomCommand,      // For user-defined custom commands
    API                 // For commands which result in an external API call. Currently not implemented; reserved for future extension.
}
