using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core;
using BotCore.Core.Cooldowns;
using BotCore.Core.Messaging;
using BotCore.Core.Providers;
using BotCore.Permissions;

namespace BotCore.Commands.Implementations;
internal class UptimeCommand : ICoreCommand
{
    public string commandString { get; init; } = "uptime";
    public string[]? commandAliases { get; set; } = [];
    public bool isMutable { get; init; } = false;

    public CooldownType cooldownType { get; init; } = CooldownType.CoreCommand;
    public TimeSpan? cooldownOverride { get; } = null;

    public PermissionsLevel requiredPermissions { get; set; } = PermissionsLevel.None;

    Func<QueryRequest, Task<QueryResult>> _providerQuery;

    public UptimeCommand(Func<QueryRequest, Task<QueryResult>> providerQuery)
    {
        _providerQuery = providerQuery;
    }

    public async Task<string> ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        // Use the providerQuery delegate to request uptime information from the provider.
        QueryResult result = await _providerQuery(new QueryRequest(messageData.Endpoint, QueryType.Uptime));
        
        // Confirm the API request was successful and the result is populated.
        if (!result.isSuccessful)
        {
            return $"Unable to acquire current uptime: {result.error}";
        }

        // Convert the result to the format we want.
        TimeSpan uptime = (TimeSpan)result.value!;

        // Add hour display only if elapsedTime is over an hour (3600 seconds) to keep the first hour of timestamps a little cleaner.
        string uptimeString = uptime.TotalSeconds >= 3600 ? uptime.ToString(@"hh\:mm\:ss") : uptime.ToString(@"mm\:ss");

        return $"Replay has been live for {uptimeString}.";
    }
}
