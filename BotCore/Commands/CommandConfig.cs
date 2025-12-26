using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;
internal class CommandConfig
{
    public List<CustomCommandDefinition> customCommands { get; init; }
    public CommandSettings commandSettings {  get; init; }

    public CommandConfig(CommandSettings commandSettings, List<CustomCommandDefinition> customCommands)
    {
        this.customCommands = customCommands;
        this.commandSettings = commandSettings;
    }
}
