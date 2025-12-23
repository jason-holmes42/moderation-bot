using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;

namespace BotCore.Commands;
internal class FilterCommand : ICommand
{
    public string commandString { get; set; } = "filter";
    public string[]? commandAliases { get; set; } = [];

    public async Task ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        // !filter <on/off/add/remove/update> <filteredString> [<punishment>]
        // token[0] = filter
        string subcommand = tokens[1];      // token[1] = <on/off/add/remove/update>
        string filteredString = tokens[2];  // token[2] = filteredString
        string? punishment = tokens[3];      // token[3] = punishment. punishment may be null in cases of `!filter remove filteredString` since filteredStrings are unique.

        // implement the filter command
        throw new NotImplementedException();
    }
}
