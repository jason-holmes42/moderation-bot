using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core.Providers;

// An ID enum for each supported platform (via provider type). These are just examples of what sorts of providers could be utilized; only ChatReplay is implemented.
public enum ProviderID
{
    ChatReplay,
    Twitch,
    Youtube,
    Niconico,
    Discord
}
