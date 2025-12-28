using BotCore.Configuration;
using BotCore.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Permissions;
internal class PermissionsService
{
    PermissionsSettings permissionsSettings;
    Dictionary<string, PermissionsLevel> permissionsList;

    PermissionsService(PermissionsConfig permissionsConfig)
    {
        this.permissionsSettings = permissionsConfig.permissionsSettings;
        this.permissionsList = permissionsConfig.permissionsList;
    }

    public static async Task<PermissionsService> CreateAsync()
    {
        // Retrieve config from storage and, failing that, generate a new one.
        PermissionsConfig permissionsConfig;
        permissionsConfig = await ConfigService.RetrievePermissionsConfig();
        if (permissionsConfig == null) permissionsConfig = await GenerateDefaultConfig();

        return new PermissionsService(permissionsConfig);
    }

    // public GetPermissionsLevel(string user)

    // public HasPermission(string user, PermissionsLevel requiredLevel)

    // public AddPermission(string user, PermissionsLevel level)

    // public RemovePermission(string user)

    // public UpdatePermission(string user, PermissionsLevel level)

    // Handle converting current settings and permissions state into PermissionsConfig and sending off for storage.
    async Task StorePermissionsConfig()
    {
        await ConfigService.StorePermissionsConfig(new PermissionsConfig(permissionsSettings, permissionsList));
    }

    // Generate a default config file if one does not exist.
    static async Task<PermissionsConfig> GenerateDefaultConfig()
    {
        // Construct the default config
        PermissionsSettings permissionsSettings = new PermissionsSettings();
        Dictionary<string, PermissionsLevel> permissionsList = new Dictionary<string, PermissionsLevel>();

        PermissionsConfig permissionsConfig = new PermissionsConfig(permissionsSettings, permissionsList);

        // Store it for future retrieval
        await ConfigService.StorePermissionsConfig(permissionsConfig);

        // Send it back for use
        return permissionsConfig;
    }
}
