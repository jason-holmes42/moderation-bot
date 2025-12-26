using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core;

// This will allow for robust testing of time-based decision-making, such as with CooldownTracker.
internal interface ITimeProvider
{
    public DateTime Now { get; }
}
