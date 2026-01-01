using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Permissions;
internal class PermissionsConfig
{
    public PermissionsSettings PermissionsSettings { get; set; }
    public Dictionary<string, PermissionsLevel> PermissionsList {  get; set; }

    public PermissionsConfig(PermissionsSettings permissionsSettings, Dictionary<string, PermissionsLevel> permissionsList)
    {
        PermissionsSettings = permissionsSettings;
        PermissionsList = permissionsList;
    }
}
