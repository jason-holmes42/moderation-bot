using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Core.Time;
internal class BotTimeProvider : ITimeProvider
{
    public DateTime Now
    {
        get { return DateTime.Now; }
    }
}
