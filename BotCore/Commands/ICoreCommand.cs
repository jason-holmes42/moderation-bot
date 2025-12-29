using BotCore.Core.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;

// Core commands use logic and produces behaviors, requiring unique processing relative to custom commands.
internal interface ICoreCommand : ICommand
{
    public Task ExecuteAsync(MessageContext messageData, string[] tokens);     // Asynchronous processing of command event. Execution logic; what does the command do?
}
