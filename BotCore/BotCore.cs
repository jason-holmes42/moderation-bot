using BotCore.Core;
using BotCore.Filtering;
using BotCore.Commands;
using BotCore.Permissions;
using BotCore.Core.Messaging;
using BotCore.Core.Providers;
using BotCore.Core.Cooldowns;
using BotCore.Core.Time;

namespace BotCore;

// Core functionality for the moderation bot.
public class BotCore
{
    UserContext _userContext;

    BotTimeProvider _timeProvider;
    CooldownTracker _cooldownTracker;

    PermissionsService _permissionsService;
    FilterService _filterService;
    CommandService _commandService;

    Dictionary<ChatEndpoint, IChatProvider> _chatProviders = new();

    BotCore(
        UserContext userContext,
        BotTimeProvider timeProvider, CooldownTracker cooldownTracker,
        PermissionsService permissionsService, FilterService filterService, CommandService commandService)
    {
        _userContext = userContext;

        _timeProvider = timeProvider;
        _cooldownTracker = cooldownTracker;

        _permissionsService = permissionsService;
        _filterService = filterService;
        _commandService = commandService;
        _commandService.InitializeCommands(ProviderQuery);
    }

    public static async Task<BotCore> CreateAsync(string internalUser)
    {
        // Establish the UserContext to identify this bot's user.
        UserContext userContext = new UserContext(internalUser);

        userContext.SetIdentity(new ChatEndpoint(ProviderID.ChatReplay, "PJDiCesare"));

        // Instantiate services.
        BotTimeProvider timeProvider = new BotTimeProvider();
        CooldownTracker cooldownTracker = new CooldownTracker(timeProvider);

        PermissionsService permissionsService = await PermissionsService.CreateAsync(userContext);
        FilterService filterService = await FilterService.CreateAsync(userContext, permissionsService);
        CommandService commandService = await CommandService.CreateAsync(userContext, filterService, permissionsService, cooldownTracker);

        return new BotCore(userContext, timeProvider, cooldownTracker, permissionsService, filterService, commandService);
    }

    public async Task ProcessMessage(MessageContext message)
    {        
        // Send message through filtering, apply any necessary reaction information
        _filterService.Evaluate(message);
        
        // If the filter identified a needed moderation action, send it to the provider for processing.
        if (message.ModAction != null)
        {
            _chatProviders[message.Endpoint].IssuePunishment(message.ModAction);
        }

        // Assess message for commands, process any identified commands
        await _commandService.Evaluate(message);
        if (message.ReactionString != null)
        {
            _chatProviders[message.Endpoint].PostMessage(message.ReactionString);
        }

        // Send MessageContext to UI for display
    }

    // Delegate query function fed to services to allow them to safely make information requests of the provider without giving them direct access.
    async Task<QueryResult> ProviderQuery(QueryRequest request)
    {
        // Switch on the request's QueryType enum, which can only contain implemented query types.
        switch (request.QueryType)
        {
            case QueryType.Uptime:
                return await _chatProviders[request.Endpoint].QueryUptimeAsync();
            default:
                return QueryResult.Failure("Invalid query type.");
        }
    }

    // Simple chat provider registry functions; to be enhanced with collision checks.
    public void RegisterProvider(IChatProvider provider)
    {
        _userContext.SetIdentity(provider.ChannelIdentity);
        _chatProviders[provider.ChannelIdentity] = provider;
    }

    public void UnregisterProvider(IChatProvider provider)
    {
        _userContext.RemoveIdentity(provider.ChannelIdentity);
        _chatProviders.Remove(provider.ChannelIdentity);
    }
}