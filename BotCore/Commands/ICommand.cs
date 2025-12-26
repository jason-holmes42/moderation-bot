using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;

namespace BotCore.Commands;

// The ICommand interface ensures that all commands have the necessary data for control functions; command strings and aliases, cooldown details, permissions levels, etc.
internal interface ICommand
{
    string commandString { get; init; }                 // The string that invokes the command; 'uptime' for !uptime, etc.
    string[]? commandAliases { get; set; }              // Alternate strings that can be used to invoke the command.

    bool isMutable { get; init; }                       // Whether a command can be edited or removed.

    CooldownType cooldownType { get; init; }            // The base cooldown category the command belongs to.
    TimeSpan? cooldownOverride { get; }                 // Optional per-command cooldown override. Currently not configurable; reserved for future extension.
    // PermissionLevel? requiredPermission { get; set; }// Permissions level required for a user to invoke the command.
}
