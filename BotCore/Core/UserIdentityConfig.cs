using BotCore.Configuration;
using BotCore.Core.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core;
internal class UserIdentityConfig : ISettingsConfig
{
    public static string Filename { get; } = "user_identity.json";
    public Dictionary<ProviderID, string> PlatformIdentity {  get; init; }
}
