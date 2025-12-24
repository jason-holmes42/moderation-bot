using BotCore.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands.Implementations;
internal class CommandsAdminCommand : ICommand
{
    public string commandString { get; init; } = "command";
    public string[]? commandAliases { get; set; } = [];

    public Task ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        throw new NotImplementedException();
    }
}
