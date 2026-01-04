using BotCore.Core.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core;

// Each BotCore instance represents a single user of the bot. However, that user may have a variety of identities across platforms. UserContext exists to store and orchestrate these distinctions.
internal class UserContext
{
    public string InternalUser { get; init; }
    Dictionary<ProviderID, string> _platformIdentities = new();

    public UserContext(string internalUser)
    {
        InternalUser = internalUser;
    }

    public string? GetIdentity(ProviderID platform)
    {
        return _platformIdentities.TryGetValue(platform, out string? identity) ? identity : null;
    }

    public void SetIdentity(ChatEndpoint platformIdentity)
    {
        _platformIdentities[platformIdentity.Platform] = platformIdentity.ChannelID;
    }

    public void RemoveIdentity(ChatEndpoint platformIdentity)
    {
        _platformIdentities.Remove(platformIdentity.Platform);
    }
}