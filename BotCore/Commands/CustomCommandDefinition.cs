using ChatModerationBot.Core.Cooldowns;
using ChatModerationBot.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChatModerationBot.Commands;

// Custom commands are user-defined commands that can be accessed by chat members from within the chat to produce a static response from the moderation bot.
internal class CustomCommandDefinition : ICommand
{
    public string CommandString { get; init; }
    public string[]? CommandAliases { get; set; }
    [JsonIgnore]public bool IsMutable { get; init; } = true;

    [JsonIgnore]public CooldownType CooldownType { get; init; } = CooldownType.CustomCommand;
    [JsonIgnore]public TimeSpan? CooldownOverride { get; } = null;

    public PermissionsLevel RequiredPermissions { get; set; } = PermissionsLevel.None;

    public string CommandResponse { get; set; }

    public CustomCommandDefinition(string commandString, string commandResponse, TimeSpan? cooldownOverride = null)
    {
        CommandString = commandString;
        CommandResponse = commandResponse;
        CooldownOverride = cooldownOverride;
    }
}
