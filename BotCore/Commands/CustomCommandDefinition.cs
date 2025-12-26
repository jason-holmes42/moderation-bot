using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;

// Custom commands are user-defined commands that can be accessed by chat members from within the chat to produce a static response from the moderation bot.
internal class CustomCommandDefinition : ICommand
{
    public string commandString { get; init; }
    public string[]? commandAliases { get; set; }
    public bool isMutable { get; init; } = true;

    public string commandResponse { get; set; }

    public CustomCommandDefinition(string commandString, string commandResponse)
    {
        this.commandString = commandString;
        this.commandResponse = commandResponse;
    }
}
