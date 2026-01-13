using Xunit;
using Moq;
using BotCore.Commands;
using BotCore.Core.Providers;
using BotCore.Permissions;
using BotCore.Commands.Implementations;
using BotCore.Core;
using BotCore.Core.Messaging;
using static UnitTesting.TestFactories;

namespace UnitTesting;
public class PermissionsTests
{
    // Shorthand function to smooth out test writing.
    PermissionsConfig GenerateTestConfig()
    {
        return new PermissionsConfig(
            new PermissionsSettings(),
            new Dictionary<ProviderID, Dictionary<string, PermissionsLevel>>
                {
                    { ProviderID.ChatReplay, new Dictionary<string, PermissionsLevel>() }
                }
            );
    }

    // 1. Accurately relay permissions level

    [Theory]
    [InlineData(PermissionsLevel.Moderator)]
    [InlineData(PermissionsLevel.Admin)]
    [InlineData(PermissionsLevel.None)]
    internal void PermissionsService_RelaysPermissions_Accurately(PermissionsLevel level)
    {
        // Arrange
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestUser", level);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);

        // Act
        PermissionsLevel result = permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, "TestUser");

        // Assert
        Assert.Equal(level, result);
    }

    [Fact]
    public void PermissionsService_ComparesNames_IgnoringCase()
    {
        // Arrange
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestUser", PermissionsLevel.Moderator);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);

        // Act
        PermissionsLevel result = permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, "testuser");

        // Assert
        Assert.Equal(PermissionsLevel.Moderator, result);
    }

    [Fact]
    public void PermissionsService_BroadcasterAlwaysBroadcaster()
    {
        // Arrange
        // UserContext carries the bot user's identity for each relevant platform and is passed to the PermissionsService during construction.tly.
        UserContext testUserContext = new("TestUserName");
        string broadcasterName = "BroadcasterName";
        string unregisteredUserName = "UnregisteredUser";
        testUserContext.SetIdentity(new ChatEndpoint(ProviderID.ChatReplay, broadcasterName));

        // PermissionsService derives the broadcaster's identity from the UserContext it receives rather than registering their permissions explicitly
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: GenerateTestConfig(), userContext: testUserContext);

        // Act
        PermissionsLevel unregisteredUserLevel = permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, unregisteredUserName);
        PermissionsLevel broadcasterUserLevel = permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, broadcasterName);

        // Assert
        Assert.Equal(PermissionsLevel.None, unregisteredUserLevel);
        Assert.Equal(PermissionsLevel.Broadcaster, broadcasterUserLevel);
    }

    // Broadcaster > Admin > Moderator > Regular > None
    // There are a lot of different possible combinations here and they should all be accurately enforced, so a slightly more complex test is required.
    [Theory]
    [MemberData(nameof(PermissionsComparisons))]
    internal void PermissionsService_EnforcesPermissionsHierarchy(PermissionsLevel userLevel, PermissionsLevel requiredLevel, bool expectedResult)
    {
        // Arrange
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestUser", userLevel);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);

        // Act
        bool hasPermission = permissionsService.HasPermission(ProviderID.ChatReplay, "TestUser", requiredLevel);

        // Assert
        Assert.Equal(expectedResult, hasPermission);
    }
    public static IEnumerable<object[]> PermissionsComparisons()
    {
        // A nested loop can be written in a more compact way, but I find this more legible.
        foreach (PermissionsLevel userLevel in Enum.GetValues<PermissionsLevel>())
        {
            foreach (PermissionsLevel requiredLevel in Enum.GetValues<PermissionsLevel>())
            {
                yield return new object[]
                {
                    userLevel,
                    requiredLevel,
                    userLevel >= requiredLevel
                };
            }
        }
    }

    [Fact]
    public void PermissionsService_UnregisteredUsersHaveNone()
    {
        // Arrange
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: GenerateTestConfig());

        // Act
        PermissionsLevel userLevel = permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, "TestUser");
        bool hasPermission = permissionsService.HasPermission(ProviderID.ChatReplay, "TestUser", PermissionsLevel.Regular);

        // Assert
        Assert.Equal(PermissionsLevel.None, userLevel);
        Assert.False(hasPermission);
    }

    [Fact]
    internal void PermissionsService_RelaysPermissions_BasedOnPlatform()
    {
        // Arrange
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.Niconico] = new Dictionary<string, PermissionsLevel>();   // No users registered.
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestUser", PermissionsLevel.Moderator);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);

        // Act
        PermissionsLevel replayPermissions = permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, "TestUser");
        PermissionsLevel niconicoPermissions = permissionsService.GetPermissionsLevel(ProviderID.Niconico, "TestUser");

        // Assert
        Assert.Equal(PermissionsLevel.Moderator, replayPermissions);
        Assert.Equal(PermissionsLevel.None, niconicoPermissions);
    }

    // 2. Admin commands

    [Fact]
    public void PermissionsService_SetPermissions_SetsPermissions()
    {
        // Arrange
        // SetPermissions requires that the user's permissions level is greater than both the target user's current level and the intended new level.
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestAdmin", PermissionsLevel.Admin);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);
        MessageContext messageContext = GenerateMessage("test message containing the admin command call", username: "TestAdmin");

        // Act
        bool wasSuccessful = permissionsService.SetPermissions(messageContext, "TestUser", PermissionsLevel.Moderator);

        // Assert
        Assert.True(wasSuccessful);
        Assert.Equal(PermissionsLevel.Moderator, permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, "TestUser"));
    }

    [Theory]
    [MemberData(nameof(SetPermissionsComparisons))]
    internal void PermissionsService_SetPermissions_RequiresUserBeHigherThanTarget(PermissionsLevel userLevel, PermissionsLevel targetUserLevel, PermissionsLevel newTargetLevel, bool expectedResult)
    {
        // Arrange
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestAdmin", userLevel);
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestUser", targetUserLevel);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);
        MessageContext messageContext = GenerateMessage("test message containing the admin command call", username: "TestAdmin");

        // Act
        bool wasSuccessful = permissionsService.SetPermissions(messageContext, "TestUser", newTargetLevel);

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
    }
    public static IEnumerable<object[]> SetPermissionsComparisons()
    {
        // A nested loop can be written in a more compact way, but I find this more legible.
        foreach (PermissionsLevel userLevel in Enum.GetValues<PermissionsLevel>())
        {
            foreach (PermissionsLevel targetUserLevel in Enum.GetValues<PermissionsLevel>())
            {
                foreach (PermissionsLevel newTargetLevel in Enum.GetValues<PermissionsLevel>())
                {
                    // There's no value in checking the case where the target user's current level is the same as the new level.
                    if (newTargetLevel == targetUserLevel) continue;

                    yield return new object[]
                    {
                    userLevel,
                    targetUserLevel,
                    newTargetLevel,
                    userLevel > targetUserLevel && userLevel > newTargetLevel
                    };
                }
            }
        }
    }

    [Fact]
    public void PermissionsService_RemovePermissions_RemovesPermissions()
    {
        // Arrange
        // RemovePermissions requires that the user's permissions level is greater than the target user's permissions level.
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestAdmin", PermissionsLevel.Admin);
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestUser", PermissionsLevel.Moderator);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);
        MessageContext messageContext = GenerateMessage("test message containing the admin command call", username: "TestAdmin");

        // Act
        bool wasSuccessful = permissionsService.SetPermissions(messageContext, "TestUser", PermissionsLevel.None);

        // Assert
        Assert.True(wasSuccessful);
        Assert.Equal(PermissionsLevel.None, permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, "TestUser"));
    }

    [Fact]
    public void PermissionsService_RemovePermissions_HandlesUnregisteredUser()
    {
        // Arrange
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestAdmin", PermissionsLevel.Admin);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);
        MessageContext messageContext = GenerateMessage("test message containing the admin command call", username: "TestAdmin");

        // Act
        bool wasSuccessful = permissionsService.RemovePermissions(messageContext, "TestUser");

        // Assert
        Assert.True(wasSuccessful);
        Assert.Equal(PermissionsLevel.None, permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, "TestUser"));
    }

    [Theory]
    [MemberData(nameof(RemovePermissionsComparisons))]
    internal void PermissionsService_RemovePermissions_RequiresUserBeHigherThanTarget(PermissionsLevel userLevel, PermissionsLevel targetUserLevel, bool expectedResult)
    {
        // Arrange
        PermissionsConfig testConfig = GenerateTestConfig();
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestAdmin", userLevel);
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestUser", targetUserLevel);
        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);
        MessageContext messageContext = GenerateMessage("test message containing the admin command call", username: "TestAdmin");

        // Act
        bool wasSuccessful = permissionsService.RemovePermissions(messageContext, "TestUser");

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
    }
    public static IEnumerable<object[]> RemovePermissionsComparisons()
    {
        // A nested loop can be written in a more compact way, but I find this more legible.
        foreach (PermissionsLevel userLevel in Enum.GetValues<PermissionsLevel>())
        {
            foreach (PermissionsLevel targetUserLevel in Enum.GetValues<PermissionsLevel>())
            {
                // The test to confirm safe handling of attempting to remove a target's permissions when they have none is handled elsewhere and has no value in this test.
                if (targetUserLevel == PermissionsLevel.None) continue;

                yield return new object[]
                {
                userLevel,
                targetUserLevel,
                userLevel > targetUserLevel
                };
            }
        }
    }
}
