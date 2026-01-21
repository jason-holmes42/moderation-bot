using ChatModerationBot.Configuration;
using ChatModerationBot.Core;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Permissions;
internal class PermissionsService
{
    UserContext _userContext;

    PermissionsSettings _permissionsSettings;
    Dictionary<ProviderID, Dictionary<string, PermissionsLevel>> _permissionsList;

    PermissionsService(UserContext userContext, PermissionsConfig permissionsConfig)
    {
        _userContext = userContext;

        _permissionsSettings = permissionsConfig.PermissionsSettings;
        _permissionsList = permissionsConfig.PermissionsList
            .ToDictionary(
                KeyValuePair => KeyValuePair.Key,
                KeyValuePair => new Dictionary<string, PermissionsLevel>(KeyValuePair.Value, StringComparer.OrdinalIgnoreCase)  // Necessary for case-insensitive username lookups
            );
    }

    public static async Task<PermissionsService> CreateAsync(UserContext userContext)
    {
        // Retrieve config from storage and, failing that, generate a new one.
        PermissionsConfig permissionsConfig;
        permissionsConfig = await ConfigService.RetrieveConfigAsync<PermissionsConfig>(userContext);
        if (permissionsConfig == null) permissionsConfig = await GenerateDefaultConfig(userContext);

        return new PermissionsService(userContext, permissionsConfig);
    }

    // Internal, test-only constructor. Accepts whatever the test provides it and generates defaults for everything else, then chains into the normal constructor to centralize initialization.
    internal PermissionsService(bool testOnly, UserContext? userContext = null, PermissionsConfig? permissionsConfig = null)
        : this(
            userContext ?? new UserContext("__TESTUSER__"),
            permissionsConfig ?? GenerateDefaultConfig(userContext).GetAwaiter().GetResult()
        )
    { }

    // Answer what a given user's registered permissions level is.
    public PermissionsLevel GetPermissionsLevel(ProviderID platform, string user)
    {
        // To ensure the broadcaster has Broadcaster level permissions across all platforms, compare the user against the broadcaster's registered platform identity.
        if (IsBroadcaster(platform, user)) return PermissionsLevel.Broadcaster;

        // If the user has a registered permissions level, return it. Otherwise, indicate none. A ternary conditional could be used here, but this is more legible.
        if (_permissionsList[platform].TryGetValue(user, out PermissionsLevel registeredLevel))
        {
            return registeredLevel;
        }
        else return PermissionsLevel.None;
    }

    // Answer whether a given user has registered permissions at or above the required level.
    public virtual bool HasPermission(ProviderID platform, string user, PermissionsLevel requiredLevel)
    {
        return GetPermissionsLevel(platform, user) >= requiredLevel;
    }

    // Permissions management functions. Add/Update distinctions, while useful for commands and filters, are not relevant here.
    public bool SetPermissions(MessageContext messageData, string targetUser, PermissionsLevel targetLevel)
    {
        // A user must have higher permissions than both the target user and the target permissions level.
        // e.g., Broadcasters can edit all permissions (except their own) and cannot elevate anyone to broadcaster, admins can edit moderator and regular permissions and cannot elevate anyone to admin, and so on.
        // Broadcasters are automatically given a permissions level higher than admins. This allows them to manage their registered administrators without special case logic, being able to demote themselves, add other broadcasters, etc.

        PermissionsLevel userPermissions = GetPermissionsLevel(messageData.Endpoint.Platform, messageData.Username);
        PermissionsLevel targetPermissions = GetPermissionsLevel(messageData.Endpoint.Platform, targetUser);

        if (userPermissions > targetPermissions && userPermissions > targetLevel)
        {
            // PermissionsLevel.None is functionally equivalent to having no registered permissions at all, so if that's the target use the RemovePermissions function for consistency and config cleanliness.
            if (targetLevel == PermissionsLevel.None)
            {
                return RemovePermissions(messageData, targetUser);
            }

            _permissionsList[messageData.Endpoint.Platform][targetUser] = targetLevel;    // This will safely add new entries or update existing entries.
            messageData.ReactionString = $"{targetUser}'s permissions set to {targetLevel.ToString().ToUpper()}.";
            return true;
        }
        else
        {
            messageData.ReactionString = $"{messageData.Username}'s permissions level is not high enough to set {targetUser}'s permissions to {targetLevel.ToString().ToUpper()}.";
            return false;
        }
    }

    // Having a separate syntax explicitly for removal simplifies the user experience.
    public bool RemovePermissions(MessageContext messageData, string targetUser)
    {
        // Confirm whether the targetUser has permissions registered.
        PermissionsLevel targetPermissions;

        if (!_permissionsList[messageData.Endpoint.Platform].TryGetValue(targetUser, out targetPermissions))
        {
            messageData.ReactionString = $"{targetUser} does not have any registered permissions.";
            return true;
        }

        // The initiating user must have higher permissions than the target user to remove their permissions.
        PermissionsLevel userPermissions = GetPermissionsLevel(messageData.Endpoint.Platform, messageData.Username);
        if (userPermissions > targetPermissions)
        {
            _permissionsList[messageData.Endpoint.Platform].Remove(targetUser);
            messageData.ReactionString = $"{targetUser}'s permissions removed.";
            return true;
        }
        else
        {
            messageData.ReactionString = $"{messageData.Username}'s permissions level not high enough to remove {targetUser}'s permissions.";
            return false;
        }
    }

    // Compares the provided username against the bot's registered identity for the provided platform to determine whether the person in question is the broadcaster or not.
    bool IsBroadcaster(ProviderID platform, string username)
    {
        // Retrieve the broadcaster's identity for the platform in question
        string userPlatformIdentity = _userContext.GetIdentity(platform)!;
        if (string.IsNullOrEmpty(userPlatformIdentity)) return false;   // If the platform somehow does not have a user identity registered, assume the answer is no.

        // Compare the username in question against the derived broadcaster identity and return the result.
        return userPlatformIdentity.Equals(username, StringComparison.OrdinalIgnoreCase);
    }

    // Handle converting current settings and permissions state into PermissionsConfig and sending off for storage.
    public async Task StorePermissionsConfig()
    {
        await ConfigService.StoreConfigAsync(_userContext, new PermissionsConfig(_permissionsSettings, _permissionsList));
    }

    // Generate a default config file if one does not exist.
    static async Task<PermissionsConfig> GenerateDefaultConfig(UserContext userContext)
    {
        // Construct the default config
        PermissionsSettings permissionsSettings = new PermissionsSettings();
        Dictionary<ProviderID, Dictionary<string, PermissionsLevel>> permissionsList = new();

        foreach(ProviderID platform in userContext.GetAllIdentities().Keys)
        {
            permissionsList.Add(platform, new Dictionary<string, PermissionsLevel>());
        }

        PermissionsConfig permissionsConfig = new PermissionsConfig(permissionsSettings, permissionsList);

        // Send it back for use
        return permissionsConfig;
    }
}
