using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Permissions;
internal class PermissionsConfig
{
    public PermissionsSettings permissionsSettings { get; set; }
    public Dictionary<string, PermissionsLevel> permissionsList {  get; set; }

    public PermissionsConfig(PermissionsSettings permissionsSettings, Dictionary<string, PermissionsLevel> permissionsList)
    {
        this.permissionsSettings = permissionsSettings;
        this.permissionsList = permissionsList;
    }
}
