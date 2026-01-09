using Xunit;
using Moq;
using System.ComponentModel.Design;
using BotCore;
using BotCore.Commands;
using BotCore.Filtering;
using BotCore.Core.Messaging;
using BotCore.Core.Providers;
using BotCore.Core.Cooldowns;
using BotCore.Permissions;
using BotCore.Core.Time;
using BotCore.Commands.Implementations;
using BotCore.Core;
using System.Runtime;

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

    // Shorthand functions to make writing tests simpler.
    MessageContext GenerateMessage(string messageContent)
    {
        return new MessageContext(
            new ChatMessage(user: "--Incoming Test User--", msg: messageContent),
            new ChatEndpoint(platform: ProviderID.ChatReplay, channelID: "--Unit Test Provider--")
        );
    }
    // Dummy ProviderQuery function for command testing.
    public static async Task<QueryResult> ProviderQuery(QueryRequest request)
    {
        return QueryResult.Failure("Unit test successfully queried provider.");
    }


    // ===== Test Cases =====


    // 1. Evaluate message to identify commands
    // Identify a message contains a core command in the usage format
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
        Assert.True(messageContext.ReactionType == BotCore.Core.ReactionType.Command);
    }

    // Identify a message contains a custom command in the usage format
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

    // Do not evaluate a command that does not exist.
    [Fact]
    public async Task CommandService_IgnoresFakeCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("!obviouslyFakeCommand");

        // Act
        await commandService.Evaluate(messageContext);

        // Assert
        Assert.True(messageContext.ReactionType == BotCore.Core.ReactionType.None);
    }

    // Do not evaluate a command outside usage format. The usage format requires the command phrase be the first token of the message, preceded by the command character. e.g., !uptime, !command add honk Honk!
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
        Assert.Equal(expectedResult, messageContext.ReactionType == BotCore.Core.ReactionType.Command);
    }

    // Evaluate commands using the specified command character, whatever it may be. This test ensures the feature remains supported.
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
        Assert.Equal(BotCore.Core.ReactionType.Command, messageContext.ReactionType);
    }

    // Command string evaluation is case-insensitive.
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
        Assert.Equal(messageContext.ReactionType == BotCore.Core.ReactionType.Command, expectedResult);
    }

    // 2. Route identified commands to respective destinations
    // Correctly call commands
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
    // RegisterCommand works
    [Fact]
    public void CommandService_RegisterCommand_RegistersCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        MessageContext messageContext = GenerateMessage("!honk");

        // Act
        commandService.RegisterCommand(messageContext, new CustomCommandDefinition("honk", "Honk!"));

        // Assert
        Assert.Equal($"Command added for `!honk`.", messageContext.ReactionString);
    }

    // RegisterCommand rejects registration when a conflict arises.
    [Theory]
    [InlineData("honk", "Existing command conflict!")]
    [InlineData("uptime", "Core command conflict!")]
    public void CommandService_RegisterCommand_RejectsConflicts(string commandString, string commandResponse)
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        commandService.InitializeCommands(ProviderQuery);
        commandService.RegisterCommandInternal(new CustomCommandDefinition("honk", "Honk!"));
        MessageContext messageContext = GenerateMessage($"!command add {commandString} {commandResponse}");

        // Act
        commandService.RegisterCommand(messageContext, new CustomCommandDefinition(commandString, commandResponse));

        // Assert
        Assert.Equal($"Command `!{commandString}` already exists.", messageContext.ReactionString);
    }

    // UnregisterCommand works
    [Fact]
    public void CommandService_UnregisterCommand_UnregistersCommands()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        commandService.RegisterCommandInternal(new CustomCommandDefinition("honk", "Honk!"));
        MessageContext messageContext = GenerateMessage("!honk");

        // Act
        commandService.UnregisterCommand(messageContext, "honk");

        // Assert
        Assert.Equal(ReactionType.None, messageContext.ReactionType);
    }

    // UnregisterCommand gracefully handles attempting to remove a command that does not exist.
    [Fact]
    public void CommandService_UnegisterCommand_HandlesMissingCommand()
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        MessageContext messageContext = GenerateMessage($"!command remove hronk");

        // Act
        commandService.UnregisterCommand(messageContext, "hronk");

        // Assert
        Assert.Equal($"No command for `!hronk` found.", messageContext.ReactionString);
    }

    // UpdateCommand works
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
        commandService.UpdateCommand(messageContext, customCommand);

        // Assert
        await commandService.Evaluate(messageContext);
        Assert.Equal("Honk honk!", messageContext.ReactionString);
    }

    // UpdateCommand gracefully handles conflicts
    [Theory]
    [InlineData("hronk", "No such command conflict!")]
    [InlineData("uptime", "Core command conflict!")]
    public void CommandService_UpdateCommand_HandlesConflicts(string commandString, string newCommandResponse)
    {
        // Arrange
        CommandService commandService = new CommandService(testOnly: true, permissionsService: _grantPermissionMock.Object);
        CustomCommandDefinition customCommand = new CustomCommandDefinition("honk", "Honk!");
        commandService.RegisterCommandInternal(customCommand);
        MessageContext messageContext = GenerateMessage("!honk");

        // Act
        customCommand.CommandResponse = newCommandResponse;
        commandService.UpdateCommand(messageContext, customCommand);

        // Assert
        Assert.NotEqual($"Command `!{commandString}` updated.", messageContext.ReactionString);
    }

    // 4. Permissions
    // Command runs if user has permission
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

    // Command does not run if user does not have permission
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

    // Command runs if it is not on cooldown
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

    // Command does not run if it is on cooldown
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

    // Command runs even if it is on cooldown if user has sufficient permissions
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