using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core.Providers;

// An endpoint identifier for specific channels on specific platforms. Used by BotCore to route outgoing requests to the correct providers.
public class ChatEndpoint
{
    public ProviderID platform { get; init; }   // Which platform (e.g., ChatReplay, Twitch, Youtube, etc.)
    public string channelID { get; init; }  // Which channel / stream on that platform.

    public ChatEndpoint(ProviderID platform, string channelID)
    {
        this.platform = platform;
        this.channelID = channelID;
    }
}
