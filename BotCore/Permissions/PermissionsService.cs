using BotCore.Configuration;
using BotCore.Core.Messaging;
using BotCore.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Permissions;
internal class PermissionsService
{
    PermissionsSettings _permissionsSettings;
    Dictionary<string, PermissionsLevel> _permissionsList;

    PermissionsService(PermissionsConfig permissionsConfig, string broadcaster)
    {
        _permissionsSettings = permissionsConfig.PermissionsSettings;
        _permissionsList = new Dictionary<string, PermissionsLevel>(permissionsConfig.PermissionsList, StringComparer.OrdinalIgnoreCase);   // Necessary for case-insensitive username lookups

        // The broadcaster should always have Broadcaster-level permissions, so confirm their presence in the permissions list.
        if (!_permissionsList.TryGetValue(broadcaster, out PermissionsLevel broadcasterPerms) || broadcasterPerms < PermissionsLevel.Broadcaster)
        {
            // If they are not present or their permissions are not Broadcaster, correct them.
            _permissionsList[broadcaster] = PermissionsLevel.Broadcaster;
        }
    }

    public static async Task<PermissionsService> CreateAsync(string broadcaster)
    {
        // Retrieve config from storage and, failing that, generate a new one.
        PermissionsConfig permissionsConfig;
        permissionsConfig = await ConfigService.RetrievePermissionsConfig();
        if (permissionsConfig == null) permissionsConfig = await GenerateDefaultConfig();

        return new PermissionsService(permissionsConfig, broadcaster);
    }

    // Answer what a given user's registered permissions level is.
    public PermissionsLevel GetPermissionsLevel(string user)
    {
        // If the user has a registered permissions level, return it. Otherwise, indicate none. A ternary conditional could be used here, but this is more legible.
        if (_permissionsList.TryGetValue(user, out PermissionsLevel registeredLevel))
        {
            return registeredLevel;
        }
        else return PermissionsLevel.None;
    }

    // Answer whether a given user has registered permissions at or above the required level.
    public bool HasPermission(string user, PermissionsLevel requiredLevel)
    {
        return GetPermissionsLevel(user) >= requiredLevel;
    }

    // Permissions management functions. Add/Update distinctions, while useful for commands and filters, are not relevant here.
    public void SetPermissions(MessageContext messageData, string targetUser, PermissionsLevel targetLevel)
    {
        // A user must have higher permissions than both the target user and the target permissions level.
        // e.g., Broadcasters can edit all permissions (except their own) and cannot elevate anyone to broadcaster, admins can edit moderator and regular permissions and cannot elevate anyone to admin, and so on.
        // Broadcasters are automatically given a permissions level higher than admins. This allows them to manage their registered administrators without special case logic, being able to demote themselves, add other broadcasters, etc.

        PermissionsLevel userPermissions = GetPermissionsLevel(messageData.Username);
        PermissionsLevel targetPermissions = GetPermissionsLevel(targetUser);

        if (userPermissions > targetPermissions && userPermissions > targetLevel)
        {
            // PermissionsLevel.None is functionally equivalent to having no registered permissions at all, so if that's the target use the RemovePermissions function for consistency and config cleanliness.
            if (targetLevel == PermissionsLevel.None)
            {
                RemovePermissions(messageData, targetUser);
                return;
            }

            _permissionsList[targetUser] = targetLevel;    // This will safely add new entries or update existing entries.
            messageData.ReactionString = $"{targetUser}'s permissions set to {targetLevel.ToString().ToUpper()}.";
            return;
        }
        else
        {
            messageData.ReactionString = $"{messageData.Username}'s permissions level is not high enough to set {targetUser}'s permissions to {targetLevel.ToString().ToUpper()}.";
            return;
        }
    }

    // Having a separate syntax explicitly for removal simplifies the user experience.
    public void RemovePermissions(MessageContext messageData, string targetUser)
    {
        // Confirm whether the targetUser has permissions registered.
        PermissionsLevel targetPermissions;

        if (!_permissionsList.TryGetValue(targetUser, out targetPermissions))
        {
            messageData.ReactionString = $"{targetUser} does not have any registered permissions.";
            return;
        }

        // The initiating user must have higher permissions than the target user to remove their permissions.
        PermissionsLevel userPermissions = GetPermissionsLevel(messageData.Username);
        if (userPermissions > targetPermissions)
        {
            _permissionsList.Remove(targetUser);
            messageData.ReactionString = $"{targetUser}'s permissions removed.";
            return;
        }
        else
        {
            messageData.ReactionString = $"{messageData.Username}'s permissions level not high enough to remove {targetUser}'s permissions.";
            return;
        }

    }

    // Handle converting current settings and permissions state into PermissionsConfig and sending off for storage.
    public async Task StorePermissionsConfig()
    {
        await ConfigService.StorePermissionsConfig(new PermissionsConfig(_permissionsSettings, _permissionsList));
    }

    // Generate a default config file if one does not exist.
    static async Task<PermissionsConfig> GenerateDefaultConfig()
    {
        // Construct the default config
        PermissionsSettings permissionsSettings = new PermissionsSettings();
        Dictionary<string, PermissionsLevel> permissionsList = new Dictionary<string, PermissionsLevel>(StringComparer.OrdinalIgnoreCase); // Necessary for case-insensitive username lookups

        PermissionsConfig permissionsConfig = new PermissionsConfig(permissionsSettings, permissionsList);

        // Store it for future retrieval
        await ConfigService.StorePermissionsConfig(permissionsConfig);

        // Send it back for use
        return permissionsConfig;
    }
}
