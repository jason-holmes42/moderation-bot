using Xunit;
using Moq;
using System.ComponentModel.Design;
using ChatModerationBot;
using ChatModerationBot.Commands;
using ChatModerationBot.Filtering;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Core.Cooldowns;
using ChatModerationBot.Permissions;
using ChatModerationBot.Core.Time;
using ChatModerationBot.Commands.Implementations;
using ChatModerationBot.Core;
using System.Runtime;
using static UnitTesting.TestFactories; // GenerateMessage(string message) generates a default MessageContext containing `message` as the content of the message.

namespace UnitTesting;

// Testing for command execution; identify commands, route correctly, admin functions work as intended.
public class CommandExecutionTests
{
    Mock<PermissionsService> _grantPermissionMock;
    Mock<PermissionsService> _rejectPermissionMock;

    Mock<CooldownTracker> _offCooldownMock;
    Mock<CooldownTracker> _onCooldownMock;

    public CommandExecutionTests()
    {
        // Generate and configure mocks used in tests
        bool testOnly = true;   // Adding a boolean as a parameter instructs Moq to try and use a matching constructor; in this case, the testOnly constructor will be used.
        BotTimeProvider timeProvider = new();

        _grantPermissionMock = new(testOnly, null, null);   // The PermissionsService test constructor accepts optional nullable parameters which Moq needs in order to identify the constructor to use.
        _rejectPermissionMock = new(testOnly, null, null);

        _offCooldownMock = new(timeProvider);
        _onCooldownMock = new(timeProvider);

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

        // Is off cooldown and can be used
        _offCooldownMock
            .Setup(cooldownMock => cooldownMock.IsOffCooldown(
                // ICommand command
                It.IsAny<ICommand>()
                )
            )
            .Returns(true);

        // Is on cooldown and not available
        _onCooldownMock
            .Setup(cooldownMock => cooldownMock.IsOffCooldown(
                // ICommand command
                It.IsAny<ICommand>()
                )
            )
            .Returns(false);
}

    // Dummy ProviderQuery function for command testing.
    public static async Task<QueryResult> ProviderQuery(QueryRequest request)
    {
        return QueryResult.Failure("Unit test successfully queried provider.");
    }


    // ===== Test Cases =====


    // 1. Evaluate message to identify commands

    [Fact]
    public async Task CommandService_IdentifiesCoreCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        commandService.InitializeCommands(ProviderQuery);
        MessageContext messageContext = GenerateMessage("!uptime");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.True(messageContext.ReactionType == ChatModerationBot.Core.ReactionType.ValidCommand);
    }

    [Fact]
    public async Task CommandService_IdentifiesCustomCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        commandService.RegisterCommandInternal(new CustomCommandDefinition("honk", "Honk!"));
        MessageContext messageContext = GenerateMessage("!honk");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.True(messageContext.ReactionString == "Honk!");
    }

    [Fact]
    public async Task CommandService_IgnoresFakeCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("!obviouslyFakeCommand");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.True(messageContext.ReactionType == ChatModerationBot.Core.ReactionType.None);
    }

    [Theory]
    [InlineData("!uptime", true)]
    [InlineData("!uptime is the correct format.", true)]
    [InlineData("You can use !uptime to check the stream's uptime", false)]
    [InlineData("!honk", true)]
    [InlineData("!honk honk", true)]
    [InlineData("$honk", false)]
    [InlineData("Honk !honk honk !honk!", false)]
    public async Task CommandService_RespectsUsageFormat(string inputMessage, bool expectedResult)
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object, cooldownTracker: _offCooldownMock.Object);
        commandService.InitializeCommands(ProviderQuery);
        commandService.RegisterCommandInternal(new CustomCommandDefinition("honk", "Honk!"));
        MessageContext messageContext = GenerateMessage(inputMessage);

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.Equal(expectedResult, messageContext.ReactionType == ChatModerationBot.Core.ReactionType.ValidCommand);
    }

    [Fact]
    public async Task CommandService_IdentifiesCommands_WithCustomCharacter()
    {
        // Arrange
        CommandSettings commandSettings = new CommandSettings() { CommandChar = '$' };
        CommandConfig commandConfig = new CommandConfig(commandSettings, new List<CustomCommandDefinition>());
        CommandService commandService = new CommandService(testOnly: true, config: commandConfig, permissionsService: _grantPermissionMock.Object);
        commandService.InitializeCommands(ProviderQuery);
        MessageContext messageContext = GenerateMessage("$uptime");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.Equal(ChatModerationBot.Core.ReactionType.ValidCommand, messageContext.ReactionType);
    }

    [Theory]
    [InlineData("!uptime", true)]
    [InlineData("!UPTIME", true)]
    [InlineData("!upTIME", true)]
    [InlineData("!UPtime", true)]
    public async Task CommandService_IdentifiesCommands_RegardlessOfCase(string commandInput, bool expectedResult)
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        commandService.InitializeCommands(ProviderQuery);
        MessageContext messageContext = GenerateMessage(commandInput);

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.Equal(messageContext.ReactionType == ChatModerationBot.Core.ReactionType.ValidCommand, expectedResult);
    }

    // 2. Route identified commands to respective destinations

    [Theory]
    [ClassData(typeof(CommandRoutingData))]
    internal async Task CommandService_CorrectlyRoutesCommands(string messageInput, string expectedResponse, ICommand commandBeingTested)
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object, cooldownTracker: _offCooldownMock.Object);
        commandService.RegisterCommandInternal(commandBeingTested);
        MessageContext messageContext = GenerateMessage(messageInput);

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.Equal(expectedResponse, messageContext.ReactionString);
    }

    // 3. Custom commands admin functions. Only tests whether the functions work once called; parsing the commands correctly belongs to the admin command itself and will get separate tests.

    [Fact]
    public void CommandService_RegisterCommand_RegistersCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("!honk");

        // Act
        bool wasSuccessful = commandService.RegisterCommand(messageContext, new CustomCommandDefinition("honk", "Honk!"));

        // Assert
        Assert.True(wasSuccessful);
    }

    [Theory]
    [InlineData("honk", "Existing command conflict!")]  // Existing command conflict
    [InlineData("uptime", "Core command conflict!")]    // Core command conflict
    public void CommandService_RegisterCommand_RejectsConflicts(string commandString, string commandResponse)
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        commandService.InitializeCommands(ProviderQuery);
        commandService.RegisterCommandInternal(new CustomCommandDefinition("honk", "Honk!"));
        MessageContext messageContext = GenerateMessage($"!command add {commandString} {commandResponse}");

        // Act
        bool wasSuccessful = commandService.RegisterCommand(messageContext, new CustomCommandDefinition(commandString, commandResponse));

        // Assert
        Assert.False(wasSuccessful);
    }

    [Fact]
    public void CommandService_UnregisterCommand_UnregistersCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        commandService.RegisterCommandInternal(new CustomCommandDefinition("honk", "Honk!"));
        MessageContext messageContext = GenerateMessage("!honk");

        // Act
        bool wasSuccessful = commandService.UnregisterCommand(messageContext, "honk");

        // Assert
        Assert.True(wasSuccessful);
        Assert.Equal(ReactionType.None, messageContext.ReactionType);
    }

    [Fact]
    public void CommandService_UnegisterCommand_HandlesMissingCommand()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        MessageContext messageContext = GenerateMessage($"!command remove hronk");

        // Act
        bool wasSuccessful = commandService.UnregisterCommand(messageContext, "hronk");

        // Assert
        Assert.False(wasSuccessful);
    }

    [Fact]
    public async Task CommandService_UpdateCommand_UpdatesCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        CustomCommandDefinition customCommand = new CustomCommandDefinition("honk", "Honk!");
        commandService.RegisterCommandInternal(customCommand);
        MessageContext messageContext = GenerateMessage("!honk");

        // Act
        customCommand.CommandResponse = "Honk honk!";
        bool wasSuccessful = commandService.UpdateCommand(messageContext, customCommand);

        // Assert
        await commandService.Evaluate(messageContext);
        Assert.True(wasSuccessful);
        Assert.Equal("Honk honk!", messageContext.ReactionString);
    }

    [Theory]
    [InlineData("hronk", "No such command conflict!")]  // No such command conflict
    [InlineData("uptime", "Core command conflict!")]    // Core command conflict
    public void CommandService_UpdateCommand_HandlesConflicts(string commandString, string newCommandResponse)
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        CustomCommandDefinition unregisteredCustomCommand = new(commandString, newCommandResponse);
        commandService.InitializeCommands(ProviderQuery);
        MessageContext messageContext = GenerateMessage($"!{commandString}");

        // Act
        bool wasSuccessful = commandService.UpdateCommand(messageContext, unregisteredCustomCommand);

        // Assert
        Assert.False(wasSuccessful);
    }

    // 4. Permissions

    [Fact]
    public async Task CommandService_RunsCommand_IfUserHasPermission()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object, cooldownTracker: _offCooldownMock.Object);
        Mock<CommandsAdminCommand> _mockCommandAdmin = new(commandService);

        // ExecuteAsync is only called once a command passes both the permissions and the cooldown checks, so it can only return "!command" if the user has permission.
        _mockCommandAdmin.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!command");
        commandService.RegisterCommandInternal(_mockCommandAdmin.Object);
        MessageContext messageContext = GenerateMessage("!command add honk Honk!");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.Equal("!command", messageContext.ReactionString);
    }

    [Fact]
    public async Task CommandService_DoesNotRunCommand_IfUserLacksPermission()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _rejectPermissionMock.Object, cooldownTracker: _offCooldownMock.Object);
        Mock<CommandsAdminCommand> _mockCommandAdmin = new(commandService);

        // ExecuteAsync is only called once a command passes both the permissions and the cooldown checks, so it can only return "!command" if the user has permission.
        _mockCommandAdmin.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!command");
        commandService.RegisterCommandInternal(_mockCommandAdmin.Object);
        MessageContext messageContext = GenerateMessage("!command add honk Honk!");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.True(string.IsNullOrEmpty(messageContext.ReactionString));
    }

    // 5. Cooldowns

    [Fact]
    public async Task CommandService_RunsCommand_IfNotOnCooldown()
    {
        // Arrange
        // A user with sufficient permissions is exempt from the cooldown check, so a standard permissions mock lacks the granularity to demonstrate the functionality.
        // "--Incoming Test User--" is the user ascribed by GenerateMessage and the default exemption level is Moderator, so we set them to a level lower than that to ensure they respect the cooldown.
        // Uptime has a default permissions requirement of None, so permissions will not otherwise interfere with processing.
        Dictionary<string, PermissionsLevel> permissionsList = new() { { "--Incoming Test User--", PermissionsLevel.Regular } };
        Dictionary<ProviderID, Dictionary<string, PermissionsLevel>> perPlatformPermissions = new() { { ProviderID.ChatReplay, permissionsList } };
        PermissionsConfig permConfig = new(new PermissionsSettings(), perPlatformPermissions);
        PermissionsService permService = new PermissionsService(testOnly: true, permissionsConfig: permConfig);

        CommandService commandService = new CommandService(testOnly: true, permissionsService: permService, cooldownTracker: _offCooldownMock.Object);
        Mock<UptimeCommand> _mockUptime = new(ProviderQuery);

        // ExecuteAsync is only called once a command passes both the permissions and the cooldown checks, so it can only return "!uptime" if the command is not on cooldown.
        _mockUptime.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!uptime");
        commandService.RegisterCommandInternal(_mockUptime.Object);
        MessageContext messageContext = GenerateMessage("!uptime");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.Equal("!uptime", messageContext.ReactionString);
    }

    [Fact]
    public async Task CommandService_DoesNotRunCommand_IfOnCooldown()
    {
        // Arrange
        // A user with sufficient permissions is exempt from the cooldown check, so a standard permissions mock lacks the granularity to demonstrate the functionality.
        // "--Incoming Test User--" is the user ascribed by GenerateMessage and the default exemption level is Moderator, so we set them to a level lower than that to ensure they respect the cooldown.
        // Uptime has a default permissions requirement of None, so permissions will not otherwise interfere with processing.
        Dictionary<string, PermissionsLevel> permissionsList = new() { { "--Incoming Test User--", PermissionsLevel.Regular } };
        Dictionary<ProviderID, Dictionary<string, PermissionsLevel>> perPlatformPermissions = new() { { ProviderID.ChatReplay, permissionsList } };
        PermissionsConfig permConfig = new(new PermissionsSettings(), perPlatformPermissions);
        PermissionsService permService = new PermissionsService(testOnly: true, permissionsConfig: permConfig);

        CommandService commandService = new CommandService(testOnly: true, permissionsService: permService, cooldownTracker: _onCooldownMock.Object);
        Mock<UptimeCommand> _mockUptime = new(ProviderQuery);

        // ExecuteAsync is only called once a command passes both the permissions and the cooldown checks, so it can only return "!uptime" if the command is not on cooldown.
        _mockUptime.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!uptime");
        commandService.RegisterCommandInternal(_mockUptime.Object);
        MessageContext messageContext = GenerateMessage("!uptime");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.True(string.IsNullOrEmpty(messageContext.ReactionString));
    }

    [Fact]
    public async Task CommandService_RunsCommands_AnytimeWithExemptionPermission()
    {
        // Arrange
        // By giving CommandService a grant permission mock, when it checks if the user is exempt from permissions, it will show yes. So even if the command is on cooldown, it should still process.
        // Uptime has a default permissions requirement of None, so permissions will not otherwise interfere with processing.
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object, cooldownTracker: _onCooldownMock.Object);
        Mock<UptimeCommand> _mockUptime = new(ProviderQuery);

        // ExecuteAsync is only called once a command passes both the permissions and the cooldown checks, so it can only return "!uptime" if the command is processed.
        _mockUptime.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!uptime");
        commandService.RegisterCommandInternal(_mockUptime.Object);
        MessageContext messageContext = GenerateMessage("!uptime");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.Equal("!uptime", messageContext.ReactionString);
    }
}

// ClassData for the command routing test
internal class CommandRoutingData : TheoryData<string, string, ICommand>
{   
    public CommandRoutingData()
    {
        // Configure core command mocks
        Mock<UptimeCommand> _mockUptime = new(CommandExecutionTests.ProviderQuery);
        Mock<CommandsAdminCommand> _mockCommandAdmin = new(new CommandService(testOnly: true));
        Mock<FilterAdminCommand> _mockFilterAdmin = new(new FilterService(testOnly: true));
        Mock<PermissionsAdminCommand> _mockPermsAdmin = new(new PermissionsService(testOnly: true));

        _mockUptime.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!uptime");
        _mockCommandAdmin.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!command");
        _mockPermsAdmin.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!permissions");
        _mockFilterAdmin.Setup(mockCommand => mockCommand.ExecuteAsync(It.IsAny<MessageContext>(), It.IsAny<string[]>())).ReturnsAsync("!filter");

        // Add core commands to the theory data.
        Add("!uptime", "!uptime", _mockUptime.Object);
        Add("!command add honk Honk!", "!command", _mockCommandAdmin.Object);
        Add("!permissions set TestGoose moderator", "!permissions", _mockPermsAdmin.Object);
        Add("!filter add quack ban", "!filter", _mockFilterAdmin.Object);

        // Add custom commands to the theory data.
        Add("!honk", "Honk!", new CustomCommandDefinition("honk", "Honk!"));
    }
}