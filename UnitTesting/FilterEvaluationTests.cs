using Xunit;
using Moq;
using ChatModerationBot.Filtering;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Permissions;
using ChatModerationBot.Core.Time;
using static UnitTesting.TestFactories; // GenerateMessage(string message) generates a default MessageContext containing `message` as the content of the message.

namespace UnitTesting;
public class FilterEvaluationTests
{
    Mock<PermissionsService> _grantPermissionMock;
    Mock<PermissionsService> _rejectPermissionMock;

    public FilterEvaluationTests()
    {
        // Generate and configure mocks used in tests
        bool testOnly = true;   // Adding a boolean as a parameter instructs Moq to try and use a matching constructor; in this case, the testOnly constructor will be used.
        BotTimeProvider timeProvider = new();

        _grantPermissionMock = new(testOnly, null, null);   // The PermissionsService test constructor accepts optional nullable parameters which Moq needs in order to identify the constructor to use.
        _rejectPermissionMock = new(testOnly, null, null);

        // User has adequate permission
        _grantPermissionMock
            .Setup(permissionsMock => permissionsMock.HasPermission(
                // ProviderID platform, string user, PermissionsLevel requiredLevel
                It.IsAny<ProviderID>(),
                It.IsAny<string>(),
                It.IsAny<PermissionsLevel>()
                )
            )
            .Returns(true);

        // User lacks adequate permission
        _rejectPermissionMock
            .Setup(permissionsMock => permissionsMock.HasPermission(
                // ProviderID platform, string user, PermissionsLevel requiredLevel
                It.IsAny<ProviderID>(),
                It.IsAny<string>(),
                It.IsAny<PermissionsLevel>()
                )
            )
            .Returns(false);
    }


    // ===== Test Cases =====


    [Fact]
    public void FilterService_IdentifiesProhibitedPhrases()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);    // A permissions rejection in the filter just indicates the user is not exempt from the filter.
        MessageContext messageContext = GenerateMessage("test");
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);    // AddFilterRule requires a messageContext under production circumstances to know where to respond. It is meaningless in this context.

        // Act
        filterService.Evaluate(messageContext);

        // Assert
        Assert.NotNull(messageContext.ModAction);   // A ModAction only exists if a filtered phrase is identified.
        Assert.Equal(PunishmentType.Warning, messageContext.ModAction.Punishment);  // The punishment is the one listed.
    }

    [Fact]
    public void FilterService_IgnoresInnocentMessages()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("This message does not contain a prohibited phrase.");
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);
        filterService.AddFilterRule(messageContext, "exam", PunishmentType.Timeout);
        filterService.AddFilterRule(messageContext, "honk", PunishmentType.Ban);

        // Act
        filterService.Evaluate(messageContext);

        // Assert
        Assert.Null(messageContext.ModAction);       // A ModAction only exists if a filtered phrase is identified.
    }

    [Theory]
    // [InlineData(null)]   -- A null message cannot exist due to invariant enforcement at the initial ChatMessage construction stage, so a null message is impossible.
    [InlineData("")]
    [InlineData(" ")]
    public void FilterService_HandlesEmptyWhitespace(string message)
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage(message);
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);

        // Act
        filterService.Evaluate(messageContext);

        // Assert
        Assert.Null(messageContext.ModAction);       // A ModAction only exists if a filtered phrase is identified. This confirms both that the message is evaluated and does not false-positive.
    }

    [Fact]
    public void FilterService_MatchesPhrases_RegardlessOfCase()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("TEST");
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);

        // Act
        filterService.Evaluate(messageContext);

        // Assert
        Assert.NotNull(messageContext.ModAction);
        Assert.Equal(PunishmentType.Warning, messageContext.ModAction.Punishment);
    }

    [Theory]
    [InlineData("This test message contains multiple prohibited phrases. Honk!", PunishmentType.Timeout)]
    [InlineData("When this test message's quest is complete, it should result in a ban.", PunishmentType.Ban)]
    public void FilterService_MatchesMultiplePhrases_AppliesStrongestPunishment(string message, PunishmentType expectedPunishment)
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage(message);
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);
        filterService.AddFilterRule(messageContext, "honk", PunishmentType.Timeout);
        filterService.AddFilterRule(messageContext, "quest", PunishmentType.Ban);

        // Act
        filterService.Evaluate(messageContext);

        // Assert
        Assert.NotNull(messageContext.ModAction);
        Assert.Equal(expectedPunishment, messageContext.ModAction.Punishment);
    }

    [Fact]
    public void FilterService_IdentifiesRegexPhrases()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext prohibitedMessage = GenerateMessage("This is a test message containing a prohibited phrase.");
        MessageContext innocentMessage = GenerateMessage("If the regex evaluates correctly, the word 'testing' should not trigger the filter.");
        filterService.AddFilterRule(innocentMessage, "\\btest\\b", PunishmentType.Warning);

        // Act
        filterService.Evaluate(prohibitedMessage);
        filterService.Evaluate(innocentMessage);

        // Assert
        Assert.NotNull(prohibitedMessage.ModAction);
        Assert.Equal(PunishmentType.Warning, prohibitedMessage.ModAction.Punishment);
        Assert.Null(innocentMessage.ModAction);
    }

    [Fact]
    public void FilterService_ExemptsUsers_WithAdequatePermissions()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _grantPermissionMock.Object);    // Permissions being granted mean that the user is treated as exempt from the filter.
        MessageContext messageContext = GenerateMessage("test");
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);

        // Act
        filterService.Evaluate(messageContext);

        // Assert
        Assert.Null(messageContext.ModAction);
    }

    // Filter admin commands.

    [Fact]
    public void FilterService_AddFilterRule_AddsNewFilter()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("A test message to trigger the filter and reveal the filter is active.");

        // Act
        bool wasSuccessful = filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);
        filterService.Evaluate(messageContext);

        // Assert
        Assert.True(wasSuccessful);
        Assert.NotNull(messageContext.ModAction);
    }

    [Fact]
    public void FilterService_AddFilterRule_RejectsConflicts()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("any message");
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);

        // Act
        bool wasSuccessful = filterService.AddFilterRule(messageContext, "test", PunishmentType.Ban);    // AddFilterRule updates the context's ReactionString directly.

        // Assert
        Assert.False(wasSuccessful);
    }

    [Fact]
    public void FilterService_RemoveFilterRule_RemovesExistingFilter()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("A test message to trigger the filter and reveal the filter is active.");
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);

        // Act
        bool wasSuccessful = filterService.RemoveFilterRule(messageContext, "test");
        filterService.Evaluate(messageContext);

        // Assert
        Assert.True(wasSuccessful);
        Assert.Null(messageContext.ModAction);
    }

    [Fact]
    public void FilterService_RemoveFilterRule_HandlesNoMatchingFilter()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("any message");
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);

        // Act
        bool wasSuccessful = filterService.RemoveFilterRule(messageContext, "honk");

        // Assert
        Assert.False(wasSuccessful);
    }

    [Fact]
    public void FilterService_UpdateFilterRule_UpdatesFilters()
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("Test message to trigger filter with updated rule.");
        filterService.AddFilterRule(messageContext, "test", PunishmentType.Warning);

        // Act
        bool wasSuccessful = filterService.UpdateFilterRule(messageContext, "test", PunishmentType.Ban);
        filterService.Evaluate(messageContext);

        // Assert
        Assert.True(wasSuccessful);
        Assert.NotNull(messageContext.ModAction);
        Assert.Equal(PunishmentType.Ban, messageContext.ModAction.Punishment);
    }

    [Theory]
    [InlineData("honk")]    // Phrase already exists.
    [InlineData("test")]    // Phrase not registered.

    public void FilterService_UpdateFilterRule_RejectsConflicts(string filter)
    {
        // Arrange
        FilterService filterService = new(testOnly: true, permissionsService: _rejectPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("any message");
        filterService.AddFilterRule(messageContext, "honk", PunishmentType.Warning);

        // Act
        bool wasSuccessful = filterService.UpdateFilterRule(messageContext, filter, PunishmentType.Warning);

        // Assert
        Assert.False(wasSuccessful);
    }
}