using ChatModerationBot.Configuration;
using ChatModerationBot.Core.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Core;
internal class UserIdentityConfig : ISettingsConfig
{
    public static string Filename { get; } = "user_identity.json";
    public Dictionary<ProviderID, string> PlatformIdentities {  get; init; }

    public UserIdentityConfig(Dictionary<ProviderID, string> platformIdentities)
    {
        PlatformIdentities = platformIdentities;
    }
}
