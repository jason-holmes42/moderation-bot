using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;
public class CustomCommand
{
    public string commandString { get; init; }
    public string reactionString { get; set; }

    public CustomCommand(string commandString, string reactionString)
    {
        this.commandString = commandString;
        this.reactionString = reactionString;
    }
}
