using Xunit;
using Moq;
using BotCore.Filtering;
using BotCore.Commands.Implementations;
using BotCore.Core.Messaging;
using BotCore.Core.Providers;
using BotCore.Permissions;
using static UnitTesting.TestFactories;
using FilterCommandAction = BotCore.Commands.Implementations.FilterAdminCommand.FilterCommandAction;

namespace UnitTesting;
public class FilterAdminTests
{
    // !filter <action> <filterString> <punishment>
    // 1. Accurately parse arguments

    [Theory]
    [MemberData(nameof(FilterActionTests))]
    internal void FilterAdmin_CorrectlyParses_FilterAction(string input, bool expectedResult, FilterCommandAction? expectedAction)
    {
        // Arrange
        FilterService filterService = new(testOnly: true);
        FilterAdminCommand adminCommand = new(filterService);
        MessageContext messageContext = GenerateMessage($"!filter {input}");
        string[] tokens = CommandServiceTokenize(messageContext.Message);

        // Act
        bool wasSuccessful = FilterAdminCommand.TryParseFilterArgs(tokens, out FilterAdminCommand.FilterCommandArgs args, out string error);

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
        if (expectedAction == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.NotNull(args);
            Assert.Equal(expectedAction, args.filterAction);
        }
    }
    public static IEnumerable<object[]> FilterActionTests()
    {
        // Valid actions: on, off, add, remove, update
        // Every test data entry contains valid arguments outside of the one being tested to ensure consistency.
        yield return new object[] { "add string ban", true, FilterCommandAction.Add };
        yield return new object[] { "remove string", true, FilterCommandAction.Remove };
        yield return new object[] { "update string ban", true, FilterCommandAction.Update };
        yield return new object[] { "on", true, FilterCommandAction.On };
        yield return new object[] { "off", true, FilterCommandAction.Off };
        yield return new object[] { "off no other tokens matter", true, FilterCommandAction.Off };

        // Invalid action
        yield return new object[] { "else string ban", false, null };

        // Nothing / too short
        yield return new object[] { "  string ban", false, null };
        yield return new object[] { "", false, null };
    }

    [Theory]
    [InlineData("add standardPhrase ban", true, "standardPhrase")]  // Standard string
    [InlineData("add \\bregex\\b ban", true, "\\bregex\\b")]        // Regex pattern
    [InlineData("add", false, null)]                                // Too short
    public void FilterAdmin_CorrectlyParses_FilterPhrase(string input, bool expectedResult, string? expectedPhrase)
    {
        // Arrange
        FilterService filterService = new(testOnly: true);
        FilterAdminCommand adminCommand = new(filterService);
        MessageContext messageContext = GenerateMessage($"!filter {input}");
        string[] tokens = CommandServiceTokenize(messageContext.Message);

        // Act
        bool wasSuccessful = FilterAdminCommand.TryParseFilterArgs(tokens, out FilterAdminCommand.FilterCommandArgs args, out string error);

        // Assert
        //Assert.Equal(expectedResult, wasSuccessful);
        if (expectedPhrase == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.NotNull(args);
            Assert.Equal(expectedPhrase, args.filterPhrase);
        }
    }

    [Theory]
    [MemberData(nameof(FilterPunishmentTests))]
    public void FilterAdmin_CorrectlyParses_PunishmentType(string input, bool expectedResult, PunishmentType? expectedPunishment)
    {
        // Arrange
        FilterService filterService = new(testOnly: true);
        FilterAdminCommand adminCommand = new(filterService);
        MessageContext messageContext = GenerateMessage($"!filter {input}");
        string[] tokens = CommandServiceTokenize(messageContext.Message);

        // Act
        bool wasSuccessful = FilterAdminCommand.TryParseFilterArgs(tokens, out FilterAdminCommand.FilterCommandArgs args, out string error);

        // Assert
        Assert.Equal(expectedResult, wasSuccessful);
        if (expectedPunishment == null)
        {
            Assert.Null(args);
        }
        else
        {
            Assert.NotNull(args);
            Assert.Equal(expectedPunishment, args.filterPunishment);
        }
    }
    public static IEnumerable<object[]> FilterPunishmentTests()
    {
        // Valid punishments: ban, timeout, warning, (assumed)
        yield return new object[] { "add phrase ban", true, PunishmentType.Ban };
        yield return new object[] { "add phrase timeout", true, PunishmentType.Timeout };
        yield return new object[] { "add phrase warning", true, PunishmentType.Warning };
        yield return new object[] { "add phrase", true, PunishmentType.Timeout };

        // Invalid punishment
        yield return new object[] { "add phrase exile", false, null };

        // Missing tokens
        yield return new object[] { "add", false, null };
    }

    
    // 2. Accurately route commands to respective functions


    [Theory]
    [MemberData(nameof(RoutingTests))]
    public async Task FilterAdmin_CorrectlyRoutes_ToFilterService(string commandInput, string testInput, PunishmentType? expectedPunishment)
    {
        // Arrange

        // Rejecting all exemption permissions allows us to test without building out a full, valid permissions service.
        Mock<PermissionsService> rejectExemptionPermissions = new(true, null, null);
        rejectExemptionPermissions
            .Setup(permissionsMock => permissionsMock.HasPermission(
                It.IsAny<ProviderID>(),
                It.IsAny<string>(),
                It.IsAny<PermissionsLevel>()
                )
            )
            .Returns(false);

        FilterService filterService = new(testOnly: true, permissionsService: rejectExemptionPermissions.Object);
        FilterAdminCommand adminCommand = new(filterService);

        MessageContext commandContext = GenerateMessage($"!filter {commandInput}");
        filterService.AddFilterRule(commandContext, "existing", PunishmentType.Ban);
        string[] tokens = CommandServiceTokenize(commandContext.Message);

        MessageContext testMessageContext = GenerateMessage(testInput);

        // Act
        await adminCommand.ExecuteAsync(commandContext, tokens);    // Execute the command
        filterService.Evaluate(testMessageContext);                 // Evaluate against the resulting phrase list

        // Assert
        if (expectedPunishment == null)
        {
            // No punishment triggered.
            Assert.Null(testMessageContext.ModAction);
        }
        else
        {
            // Punishment triggered.
            Assert.NotNull(testMessageContext.ModAction);
            Assert.Equal(expectedPunishment, testMessageContext.ModAction.Punishment);
        }

    }
    public static IEnumerable<object[]> RoutingTests()
    {
        // Valid routes: toggle filter (on, off), add filter rule, remove filter rule, update filter rule
        yield return new object[] { "add phrase ban", "This uses the new test phrase to trigger a ban.", PunishmentType.Ban };
        yield return new object[] { "remove existing", "This message uses the existing filtered string but does not trigger a punishment.", null };
        yield return new object[] { "update existing timeout", "This message uses the updated existing filter and receives the updated punishment.", PunishmentType.Timeout };
        yield return new object[] { "off", "This message uses the existing filtered phrase, but does not receive a punishment because the filter has been toggled off.", null };

        // Invalid routes cannot be reached because they fail to correctly construct arguments, resulting in being rejected by earlier processes.
    }
}