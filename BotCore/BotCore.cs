using ChatModerationBot.Core;
using ChatModerationBot.Filtering;
using ChatModerationBot.Commands;
using ChatModerationBot.Permissions;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Core.Cooldowns;
using ChatModerationBot.Core.Time;
using ChatModerationBot.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatModerationBot;

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

    public event Action<MessageContext>? OnMessageProcessed;

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

        // Retrieve any previously-stored identities for the user from storage
        UserIdentityConfig userIdentity = await ConfigService.RetrieveConfigAsync<UserIdentityConfig>(userContext);
        if (userIdentity != null) userContext.LoadIdentities(userIdentity.PlatformIdentities);

        userContext.SetIdentity(new ChatEndpoint(ProviderID.ChatReplay, "PJDiCesare"));

        // Instantiate services.
        BotTimeProvider timeProvider = new BotTimeProvider();
        CooldownTracker cooldownTracker = new CooldownTracker(timeProvider);

        PermissionsService permissionsService = await PermissionsService.CreateAsync(userContext);
        FilterService filterService = await FilterService.CreateAsync(userContext, permissionsService);
        CommandService commandService = await CommandService.CreateAsync(userContext, filterService, permissionsService, cooldownTracker);

        return new BotCore(userContext, timeProvider, cooldownTracker, permissionsService, filterService, commandService);
    }

    public async Task ProcessMessage(MessageContext messageData)
    {
        // Send message through filtering, apply any necessary reaction information
        _filterService.Evaluate(messageData);
        
        // If the filter identified a needed moderation action, send it to the provider for processing.
        if (messageData.ModAction != null)
        {
            _chatProviders[messageData.Endpoint].IssuePunishment(messageData.ModAction);
            OnMessageProcessed?.Invoke(messageData);    // Send the message for display.
            return;     // Do not process any other requests.
        }

        // Assess message for commands, process any identified commands
        await _commandService.Evaluate(messageData);

        // Message processing is now complete, so send the MessageContext to UI for display
        OnMessageProcessed?.Invoke(messageData);

        // If there are any commands to process, do so.
        if (messageData.ReactionString != null)
        {
            _chatProviders[messageData.Endpoint].PostMessage(messageData.ReactionString);
        }
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
    public async Task RegisterProvider(IChatProvider provider)
    {
        // Registering a provider requires specifying an identity on that platform and, if you desire bot functionality, the appropriate authentication.
        // This renders provider registration and platform identity registration distinct but inseparable.

        // Register both the user's identity on the platform and the provider for that identity as an active provider
        _userContext.SetIdentity(provider.ChannelIdentity);
        _chatProviders[provider.ChannelIdentity] = provider;

        // Subscribe to provider's OnMessageReceived.
        provider.OnMessageReceived += async message => { await ProcessMessage(message); };

        // Save updates to the user's identity registry.
        await ConfigService.StoreConfigAsync(_userContext, new UserIdentityConfig(_userContext.GetAllIdentities()));
    }

    public async Task UnregisterProvider(IChatProvider provider)
    {
        // Unregister both the user's identity on the platform and the provider for that identity as an active provider
        _userContext.RemoveIdentity(provider.ChannelIdentity);
        _chatProviders.Remove(provider.ChannelIdentity);

        // Unsubscribe to provider's OnMessageReceived.
        provider.OnMessageReceived -= async message => { await ProcessMessage(message); };

        // Save updates to the user's identity registry.
        await ConfigService.StoreConfigAsync(_userContext, new UserIdentityConfig(_userContext.GetAllIdentities()));
    }
}