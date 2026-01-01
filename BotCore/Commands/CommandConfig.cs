using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Commands;
internal class CommandConfig
{
    public List<CustomCommandDefinition> CustomCommands { get; init; }
    public CommandSettings CommandSettings {  get; init; }

    public CommandConfig(CommandSettings commandSettings, List<CustomCommandDefinition> customCommands)
    {
        CustomCommands = customCommands;
        CommandSettings = commandSettings;
    }
}
