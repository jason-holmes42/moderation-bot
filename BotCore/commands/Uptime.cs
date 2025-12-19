using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.commands;
internal class Uptime : ICommand
{
    public string commandString { get; set; } = "uptime";
    public string[]? commandAliases { get; set; } = [];

    public async Task ExecuteAsync()
    {

    }
}
