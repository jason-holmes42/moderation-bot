using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Core.Providers;

// QueryType is a curated list of queries that the bot's services can make to the providers. As new capabilities are added, they gain an entry in the enum.
public enum QueryType
{
    Uptime
    // ViewerCount,
    // StreamTitle,
    // StreamCategory,
    // IsLive
    // FollowAge
    // SubscriptionStatus
    // etc.
}
