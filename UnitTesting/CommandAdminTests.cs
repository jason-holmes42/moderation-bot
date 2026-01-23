using Xunit;
using Moq;
using ChatModerationBot.Commands;
using ChatModerationBot.Commands.Implementations;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Permissions;
using static UnitTesting.TestFactories;
using CommandsAdminAction = ChatModerationBot.Commands.Implementations.CommandsAdminCommand.CommandsAdminAction;
using ChatModerationBot.Core;

namespace UnitTesting;
public class CommandAdminTests
{
    // !command <action> <commandString> <commandResponse>
    [Theory]
    [MemberData(nameof(CommandActionTests))]
    internal void CommandAdmin_CorrectlyParses_CommandAction(string input, bool expectedResult, CommandsAdminAction? expectedAction)
    {
        // Arrange
        CommandService commandService = new(testOnly: true);
        CommandsAdminCommand adminCommand = new(commandService);
        MessageContext messageContext = GenerateMessage($"!command {input}");

        // Act
        bool wasSuccessful = CommandsAdminCommand.TryParseCommandArgs(messageContext.Message, out CommandsAdminCommand.CommandsAdminArgs args, out string error);

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
        if (expectedAction == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.NotNull(args);
            Assert.Equal(expectedAction, args.commandAction);
        }
    }
    public static IEnumerable<object[]> CommandActionTests()
    {
        // Valid actions: add, remove, update
        // Every test data entry contains valid arguments outside of the one being tested to ensure consistency.
        yield return new object[] { "add custom Custom command response.", true, CommandsAdminAction.Add };
        yield return new object[] { "remove custom", true, CommandsAdminAction.Remove };
        yield return new object[] { "update custom New custom response.", true, CommandsAdminAction.Update };

        // Invalid action
        yield return new object[] { "else custom Custom command response.", false, null };

        // Nothing / too short
        yield return new object[] { "  custom Custom response.", false, null };
        yield return new object[] { "", false, null };
    }

    [Theory]
    [InlineData("add custom Custom command response", true, "custom")]  // Valid command phrase
    [InlineData("add", false, null)] // Too short
    public void CommandAdmin_CorrectlyParses_CommandPhrase(string input, bool expectedResult, string? expectedPhrase)
    {
        // Arrange
        CommandService commandService = new(testOnly: true);
        CommandsAdminCommand adminCommand = new(commandService);
        MessageContext messageContext = GenerateMessage($"!command {input}");

        // Act
        bool wasSuccessful = CommandsAdminCommand.TryParseCommandArgs(messageContext.Message, out CommandsAdminCommand.CommandsAdminArgs args, out string error);

        // Assert
        //Assert.Equal(expectedResult, wasSuccessful);
        if (expectedPhrase == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.Equal(expectedResult, wasSuccessful);
            Assert.Equal(expectedPhrase, args.commandPhrase);
        }
    }

    [Theory]
    [MemberData(nameof(CommandResponseTests))]
    public void CommandAdmin_CorrectlyParses_CommandResponse(string input, bool expectedResult, string? expectedResponse)
    {
        // Arrange
        CommandService commandService = new(testOnly: true);
        CommandsAdminCommand adminCommand = new(commandService);
        MessageContext messageContext = GenerateMessage($"!command {input}");

        // Act
        bool wasSuccessful = CommandsAdminCommand.TryParseCommandArgs(messageContext.Message, out CommandsAdminCommand.CommandsAdminArgs args, out string error);

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
        if (expectedResponse == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.NotNull(args);
            Assert.Equal(expectedResponse, args.commandResponse);
        }
    }
    public static IEnumerable<object[]> CommandResponseTests()
    {
        // Valid responses can be anything.
        yield return new object[] { "add custom Custom command response.", true, "Custom command response." };

        // There are no invalid responses.

        // Missing tokens
        yield return new object[] { "add", false, null };
        yield return new object[] { "add custom", false, null };
    }


    // 2. Accurately route commands to respective functions


    [Theory]
    [MemberData(nameof(RoutingTests))]
    public async Task CommandAdmin_CorrectlyRoutes_ToCommandService(string commandInput, string testInput, string? expectedResponse)
    {
        // Arrange

        // Granting permissions allows us to test without building out a full, valid permissions service.
        Mock<PermissionsService> grantExemptionPermissions = new(true, null, null);
        grantExemptionPermissions
            .Setup(permissionsMock => permissionsMock.HasPermission(
                It.IsAny<ProviderID>(),
                It.IsAny<string>(),
                It.IsAny<PermissionsLevel>()
                )
            )
            .Returns(true);

        CommandService commandService = new(testOnly: true, permissionsService: grantExemptionPermissions.Object);
        commandService.RegisterCommandInternal(new CustomCommandDefinition("existing", "This is an existing command."));

        CommandsAdminCommand adminCommand = new(commandService);

        MessageContext commandContext = GenerateMessage($"!command {commandInput}");
        string[] tokens = CommandServiceTokenize(commandContext.Message);   // The CommandsAdminCommand re-parses the message string from the provided MessageContext, but still accepts tokens to ensure compatibility across all command implementations.

        MessageContext testMessageContext = GenerateMessage(testInput);

        // Act
        await adminCommand.ExecuteAsync(commandContext, tokens);    // Execute the command
        await commandService.Evaluate(testMessageContext);          // Evaluate against the resulting command registry

        // Assert
        if (expectedResponse == null)
        {
            // No command identified, no command response.
            Assert.Equal(ReactionType.None, testMessageContext.ReactionType);
            Assert.Null(testMessageContext.ReactionString);
        }
        else
        {
            // Command identified.
            Assert.Equal(ReactionType.ValidCommand, testMessageContext.ReactionType);
            Assert.Equal(expectedResponse, testMessageContext.ReactionString);
        }

    }
    public static IEnumerable<object[]> RoutingTests()
    {
        // Valid routes: add command, remove command, update command
        yield return new object[] { "add custom Command should successfully respond.", "!custom", "Command should successfully respond." };
        yield return new object[] { "remove existing", "!existing", null };
        yield return new object[] { "update existing This is an updated response for the existing command.", "!existing", "This is an updated response for the existing command." };

        // Invalid routes cannot be reached because they fail to correctly construct arguments, resulting in being rejected by earlier processes.
    }
}
