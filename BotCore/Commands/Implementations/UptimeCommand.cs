using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;

namespace BotCore.Commands.Implementations;
internal class UptimeCommand : ICommand
{
    public string commandString { get; set; } = "uptime";
    public string[]? commandAliases { get; set; } = [];

    public async Task ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        TimeSpan uptime = await messageData.Provider.QueryUptimeAsync();

        messageData.reactionType = ReactionType.Command;

        // Add hour display only if elapsedTime is over an hour (3600 seconds) to keep the first hour of timestamps a little cleaner.
        string uptimeString = uptime.TotalSeconds >= 3600 ? uptime.ToString(@"hh\:mm\:ss") : uptime.ToString(@"mm\:ss");

        messageData.reactionString = $"Replay has been live for {uptimeString}.";
    }
}
