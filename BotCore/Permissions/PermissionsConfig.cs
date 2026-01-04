using BotCore.Configuration;
using BotCore.Core.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Permissions;
internal class PermissionsConfig : ISettingsConfig
{
    public static string Filename { get; } = "permissions.json";
    public PermissionsSettings PermissionsSettings { get; set; }
    public Dictionary<ProviderID, Dictionary<string, PermissionsLevel>> PermissionsList {  get; set; }

    public PermissionsConfig(PermissionsSettings permissionsSettings, Dictionary<ProviderID, Dictionary<string, PermissionsLevel>> permissionsList)
    {
        PermissionsSettings = permissionsSettings;
        PermissionsList = permissionsList;
    }
}
