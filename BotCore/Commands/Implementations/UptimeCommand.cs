using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatModerationBot.Core;
using ChatModerationBot.Core.Cooldowns;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Permissions;

namespace ChatModerationBot.Commands.Implementations;
internal class UptimeCommand : ICoreCommand
{
    public string CommandString { get; init; } = "uptime";
    public string[]? CommandAliases { get; set; } = [];
    public bool IsMutable { get; init; } = false;

    public CooldownType CooldownType { get; init; } = CooldownType.CoreCommand;
    public TimeSpan? CooldownOverride { get; } = null;

    public PermissionsLevel RequiredPermissions { get; set; } = PermissionsLevel.None;

    Func<QueryRequest, Task<QueryResult>> _providerQuery;

    public UptimeCommand(Func<QueryRequest, Task<QueryResult>> providerQuery)
    {
        _providerQuery = providerQuery;
    }

    public async virtual Task<string> ExecuteAsync(MessageContext messageData, string[] tokens)
    {
        // Use the providerQuery delegate to request uptime information from the provider.
        QueryResult result = await _providerQuery(new QueryRequest(messageData.Endpoint, QueryType.Uptime));
        
        // Confirm the API request was successful and the result is populated.
        if (!result.IsSuccessful)
        {
            return $"Unable to acquire current uptime: {result.Error}";
        }

        // Convert the result to the format we want.
        TimeSpan uptime = (TimeSpan)result.Value!;

        // Add hour display only if elapsedTime is over an hour (3600 seconds) to keep the first hour of timestamps a little cleaner.
        string uptimeString = uptime.TotalSeconds >= 3600 ? uptime.ToString(@"hh\:mm\:ss") : uptime.ToString(@"mm\:ss");

        return $"Replay has been live for {uptimeString}.";
    }
}
