using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core.Cooldowns;
using BotCore.Permissions;

namespace BotCore.Commands;

// The ICommand interface ensures that all commands have the necessary data for control functions; command strings and aliases, cooldown details, permissions levels, etc.
internal interface ICommand
{
    string CommandString { get; init; }                 // The string that invokes the command; 'uptime' for !uptime, etc.
    string[]? CommandAliases { get; set; }              // Alternate strings that can be used to invoke the command.

    bool IsMutable { get; init; }                       // Whether a command can be edited or removed.

    CooldownType CooldownType { get; init; }            // The base cooldown category the command belongs to.
    TimeSpan? CooldownOverride { get; }                 // Optional per-command cooldown override. Currently not configurable; reserved for future extension.
    PermissionsLevel RequiredPermissions { get; set; }   // Permissions level required for a user to invoke the command.
}
