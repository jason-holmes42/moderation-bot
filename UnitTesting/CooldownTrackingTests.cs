using Xunit;
using Moq;
using BotCore.Core.Cooldowns;
using BotCore.Commands;
using BotCore.Commands.Implementations;
using BotCore.Permissions;

namespace UnitTesting;
public class CooldownTrackingTests
{
    CustomCommandDefinition _customCommand;
    UptimeCommand _uptimeCommand;
    PermissionsAdminCommand _permCommand;
    CustomCommandDefinition _overrideCommand;

    public CooldownTrackingTests()
    {
        // Commands from different cooldown categories for use in tests. Their contents are irrelevant.
        _customCommand  = new("honk", "Honk!");
        _uptimeCommand  = new(CommandExecutionTests.ProviderQuery);
    }

    [Fact]
    public void CooldownTracker_AccuratelyRelaysStatus()
    {
        // Arrange
        TestTimeProvider timeProvider = new(new DateTime(1985, 10, 26, 1, 20, 00));
        CooldownTracker cooldownTracker = new(timeProvider);

        // Act
        cooldownTracker.StartCooldown(_customCommand.CommandString);

        // Assert
        Assert.True(cooldownTracker.IsOffCooldown(_uptimeCommand));
        Assert.False(cooldownTracker.IsOffCooldown(_customCommand));
    }

    [Fact]
    public void CooldownTracker_AccuratelyRelaysStatus_AfterEnd()
    {
        // Arrange
        TestTimeProvider timeProvider = new(new DateTime(1985, 10, 26, 1, 35, 00));
        CooldownTracker cooldownTracker = new(timeProvider);

        // Act
        cooldownTracker.StartCooldown(_customCommand.CommandString);            // Cooldown entries are tracked by command string
        bool getOnCooldown = !cooldownTracker.IsOffCooldown(_customCommand);    // Should be true if the command is put on cooldown
        timeProvider.CurrentTime += TimeSpan.FromSeconds(15.5);                 // Custom command category cooldowns are 15 seconds by default.
        bool getOffCooldown = cooldownTracker.IsOffCooldown(_customCommand);    // Should be true since 15.5 seconds have passed.

        // Assert
        Assert.True(getOnCooldown);
        Assert.True(getOffCooldown);
    }

    [Theory]
    [MemberData(nameof(CategoryTests))]
    internal void CooldownTracker_AccuratelyTracks_CategoryCooldowns(ICommand command, int expectedCooldownDuration)
    {
        // Arrange
        TestTimeProvider timeProvider = new(new DateTime(1955, 11, 12, 10, 04, 00));
        CooldownTracker cooldownTracker = new(timeProvider);

        // Act
        cooldownTracker.StartCooldown(command.CommandString);
        // OnCooldown is meaningless if there is no cooldown, so disregard this result in that case.
        bool getOnCooldown = expectedCooldownDuration == 0 ? true : !cooldownTracker.IsOffCooldown(command);
        timeProvider.CurrentTime += TimeSpan.FromSeconds(expectedCooldownDuration + 0.5);
        bool getOffCooldown = cooldownTracker.IsOffCooldown(command);

        // Assert
        Assert.True(getOnCooldown);
        Assert.True(getOffCooldown);
    }
    public static IEnumerable<object[]> CategoryTests()
    {
        return new[]
        {
            new object[] { new PermissionsAdminCommand(new PermissionsService(testOnly: true)), 0 },
            new object[] { new UptimeCommand(CommandExecutionTests.ProviderQuery), 5 },
            new object[] { new CustomCommandDefinition("honk", "Honk!"), 15 }
        };
    }

    [Fact]
    public void CooldownTracker_AccuratelyTracks_OverrideCooldowns()
    {
        // Arrange
        TestTimeProvider timeProvider = new(new DateTime(1985, 10, 26, 1, 24, 00));
        CooldownTracker cooldownTracker = new(timeProvider);
        TimeSpan overrideCooldown = TimeSpan.FromSeconds(3);
        CustomCommandDefinition overrideCommand = new("override", "This command has an override cooldown.", cooldownOverride: overrideCooldown);

        // Act
        cooldownTracker.StartCooldown(overrideCommand.CommandString);
        timeProvider.CurrentTime += TimeSpan.FromSeconds(overrideCooldown.TotalSeconds - 0.1);   // 2.9 seconds should result in the command still being on cooldown
        bool getOnCooldown = !cooldownTracker.IsOffCooldown(overrideCommand);
        timeProvider.CurrentTime += TimeSpan.FromSeconds(0.2);                                   // 3.1 seconds should result in the command being off cooldown again
        bool getOffCooldown = cooldownTracker.IsOffCooldown(overrideCommand);

        // Assert
        Assert.True(getOnCooldown);
        Assert.True(getOffCooldown);
    }

    [Fact]
    public void CooldownTracker_AccuratelyTracks_CooldownsIndependently()
    {
        // Arrange
        TestTimeProvider timeProvider = new(new DateTime(2015, 10, 21, 4, 29, 00));
        CooldownTracker cooldownTracker = new(timeProvider);

        // Act
        cooldownTracker.StartCooldown(_customCommand.CommandString);
        cooldownTracker.StartCooldown(_uptimeCommand.CommandString);
        timeProvider.CurrentTime += TimeSpan.FromSeconds(5.5);      // Uptime should be off cooldown, but not the custom command.
        bool uptimeOffCooldown = cooldownTracker.IsOffCooldown(_uptimeCommand);
        bool customOnCooldown = !cooldownTracker.IsOffCooldown(_customCommand);

        // Assert
        Assert.True(uptimeOffCooldown);
        Assert.True(customOnCooldown);
    }
}
