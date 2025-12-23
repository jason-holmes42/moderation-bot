using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;

namespace BotCore.Commands;
internal interface ICommand
{
    string commandString { get; set; }                  // The string that requests the command; 'uptime' for !uptime, etc.
    string[]? commandAliases { get; set; }
    // PermissionLevel? permissions { get; set; }     // Permissions level required
    Task ExecuteAsync(MessageContext messageData, string[] tokens);     // Asynchronous processing of command event. Execution logic; what does the command do?
}
