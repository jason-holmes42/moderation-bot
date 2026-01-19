using Xunit;
using Moq;
using ChatModerationBot.Commands;
using ChatModerationBot.Commands.Implementations;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Permissions;
using static UnitTesting.TestFactories;
using PermissionsCommandAction = ChatModerationBot.Commands.Implementations.PermissionsAdminCommand.PermissionsCommandAction;
using ChatModerationBot.Core;
using System.Reflection.Emit;

namespace UnitTesting;
public class PermissionsAdminTests
{
    // !permissions <action> <targetUser>
    [Theory]
    [MemberData(nameof(PermissionsActionTests))]
    internal void PermissionsAdmin_CorrectlyParses_PermissionsAction(string input, bool expectedResult, PermissionsCommandAction? expectedAction)
    {
        // Arrange
        PermissionsService commandService = new(testOnly: true);
        PermissionsAdminCommand adminPermissions = new(commandService);
        MessageContext messageContext = GenerateMessage($"!permissions {input}");
        string[] tokens = CommandServiceTokenize(messageContext.Message);

        // Act
        bool wasSuccessful = PermissionsAdminCommand.TryParseCommandArgs(tokens, out PermissionsAdminCommand.PermissionsCommandArgs args, out string error);

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
        if (expectedAction == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.NotNull(args);
            Assert.Equal(expectedAction, args.permissionsAction);
        }
    }
    public static IEnumerable<object[]> PermissionsActionTests()
    {
        // Valid actions: set, remove
        // Every test data entry contains valid arguments outside of the one being tested to ensure consistency.
        yield return new object[] { "set TestUser moderator", true, PermissionsCommandAction.Set };
        yield return new object[] { "remove TestUser", true, PermissionsCommandAction.Remove };

        // Invalid action
        yield return new object[] { "add TestUser moderator", false, null };

        // Nothing / too short
        yield return new object[] { "  TestUser moderator", false, null };
        yield return new object[] { "", false, null };
    }

    [Theory]
    [InlineData("set TestUser moderator", true, "TestUser")]  // Valid user included
    [InlineData("add", false, null)] // Too short
    public void PermissionsAdmin_CorrectlyParses_TargetUser(string input, bool expectedResult, string? expectedUser)
    {
        // Arrange
        PermissionsService commandService = new(testOnly: true);
        PermissionsAdminCommand adminPermissions = new(commandService);
        MessageContext messageContext = GenerateMessage($"!permissions {input}");
        string[] tokens = CommandServiceTokenize(messageContext.Message);

        // Act
        bool wasSuccessful = PermissionsAdminCommand.TryParseCommandArgs(tokens, out PermissionsAdminCommand.PermissionsCommandArgs args, out string error);

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
        if (expectedUser == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.Equal(expectedResult, wasSuccessful);
            Assert.Equal(expectedUser, args.targetUser);
        }
    }

    [Theory]
    [MemberData(nameof(PermissionsResponseTests))]
    internal void PermissionsAdmin_CorrectlyParses_PermissionsLevel(string input, bool expectedResult, PermissionsLevel? expectedLevel)
    {
        // Arrange
        PermissionsService commandService = new(testOnly: true);
        PermissionsAdminCommand adminPermissions = new(commandService);
        MessageContext messageContext = GenerateMessage($"!permissions {input}");
        string[] tokens = CommandServiceTokenize(messageContext.Message);

        // Act
        bool wasSuccessful = PermissionsAdminCommand.TryParseCommandArgs(tokens, out PermissionsAdminCommand.PermissionsCommandArgs args, out string error);

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
        if (expectedLevel == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.NotNull(args);
            Assert.Equal(expectedLevel, args.targetLevel);
        }
    }
    public static IEnumerable<object[]> PermissionsResponseTests()
    {
        // Valid permissions levels
        foreach (PermissionsLevel level in Enum.GetValues<PermissionsLevel>())
        {
            yield return new object[] { $"set TestUser {level.ToString().ToLower()}", true, level };
        }

        // Invalid permissions levels
        yield return new object[] { $"set TestUser VIP", false, null };

        // Missing tokens
        yield return new object[] { "set TestUser", false, null };
        yield return new object[] { "remove", false, null };
    }


    // 2. Accurately route commands to respective functions


    [Theory]
    [MemberData(nameof(RoutingTests))]
    internal async Task PermissionsAdmin_CorrectlyRoutes_ToPermissionsService(string commandInput, string testUser, PermissionsLevel expectedResult)
    {
        // Arrange

        // To ensure the routing is not rejected, the command issued needs to come from someone with adequate permissions.
        PermissionsConfig testConfig = new PermissionsConfig(
            new PermissionsSettings(),
            new Dictionary<ProviderID, Dictionary<string, PermissionsLevel>>
                {
                    { ProviderID.ChatReplay, new Dictionary<string, PermissionsLevel>() }
                }
            );
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("TestAdmin", PermissionsLevel.Admin);
        testConfig.PermissionsList[ProviderID.ChatReplay].Add("ExistingUser", PermissionsLevel.Moderator);

        PermissionsService permissionsService = new(testOnly: true, permissionsConfig: testConfig);

        PermissionsAdminCommand adminPermissions = new(permissionsService);

        MessageContext commandContext = GenerateMessage($"!permissions {commandInput}", username: "TestAdmin");
        string[] tokens = CommandServiceTokenize(commandContext.Message);

        // Act
        await adminPermissions.ExecuteAsync(commandContext, tokens);    // Execute the command
        PermissionsLevel resultingLevel = permissionsService.GetPermissionsLevel(ProviderID.ChatReplay, testUser); // Check whether an update occurred.

        // Assert
        Assert.Equal(expectedResult, resultingLevel);
    }
    public static IEnumerable<object[]> RoutingTests()
    {
        // Valid routes: set permissions, remove permissions
        yield return new object[] { "set TestUser moderator", "TestUser", PermissionsLevel.Moderator};
        yield return new object[] { "remove ExistingUser", "ExistingUser", PermissionsLevel.None};

        // Invalid routes cannot be reached because they fail to correctly construct arguments, resulting in being rejected by earlier processes.
    }
}