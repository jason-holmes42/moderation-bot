using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;
public class CommandDefinition
{
    public string commandString { get; init; }
    public string reactionString { get; set; }

    public CommandDefinition(string commandString, string reactionString)
    {
        this.commandString = commandString;
        this.reactionString = reactionString;
    }
}
