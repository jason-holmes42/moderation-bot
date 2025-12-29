using BotCore.Core.Cooldowns;
using BotCore.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BotCore.Commands;

// Custom commands are user-defined commands that can be accessed by chat members from within the chat to produce a static response from the moderation bot.
internal class CustomCommandDefinition : ICommand
{
    public string commandString { get; init; }
    public string[]? commandAliases { get; set; }
    [JsonIgnore]public bool isMutable { get; init; } = true;

    [JsonIgnore]public CooldownType cooldownType { get; init; } = CooldownType.CustomCommand;
    [JsonIgnore]public TimeSpan? cooldownOverride { get; } = null;

    public PermissionsLevel requiredPermissions { get; set; } = PermissionsLevel.None;

    public string commandResponse { get; set; }

    public CustomCommandDefinition(string commandString, string commandResponse)
    {
        this.commandString = commandString;
        this.commandResponse = commandResponse;
    }
}
