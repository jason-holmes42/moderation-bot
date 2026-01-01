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
    BotTimeProvider _timeProvider;
    CooldownTracker _cooldownTracker;

    PermissionsService _permissionsService;
    FilterService _filterService;
    CommandService _commandService;

    Dictionary<ChatEndpoint, IChatProvider> _chatProviders;

    BotCore(
        BotTimeProvider timeProvider,
        CooldownTracker cooldownTracker,
        PermissionsService permissionsService,
        FilterService filterService,
        CommandService commandService)
    {
        _timeProvider = timeProvider;
        _cooldownTracker = cooldownTracker;

        _permissionsService = permissionsService;
        _filterService = filterService;
        _commandService = commandService;

        _chatProviders = new Dictionary<ChatEndpoint, IChatProvider>();
    }

    public static async Task<BotCore> CreateAsync(ChatEndpoint channelIdentity)
    {
        BotTimeProvider timeProvider = new BotTimeProvider();
        CooldownTracker cooldownTracker = new CooldownTracker(timeProvider);

        PermissionsService permissionsService = await PermissionsService.CreateAsync(channelIdentity.channelID);
        FilterService filterService = await FilterService.CreateAsync(permissionsService);
        CommandService commandService = await CommandService.CreateAsync(filterService, permissionsService, cooldownTracker);

        return new BotCore(timeProvider, cooldownTracker, permissionsService, filterService, commandService);
    }

    public event Action<string>? OnMessageSent;
    Action<string>? onMessageSentHandler;

    // public event Func<string>? OnQuery;
    // Action<string>? onQueryHandler;

    public async Task ProcessMessage(MessageContext message)
    {        
        // Send message through filtering, apply any necessary reaction information
        _filterService.Evaluate(message);
        
        // If the filter identified a needed moderation action, send it to the provider for processing.
        if (message.modAction != null)
        {
            _chatProviders[message.Endpoint].IssuePunishment(message.modAction);
        }

        // Assess message for commands, process any identified commands
        await _commandService.Evaluate(message);
        if (message.reactionString != null)
        {
            _chatProviders[message.Endpoint].PostMessage(message.reactionString);
        }

        // Send MessageContext to UI for display
    }

    // Simple registry functions; to be enhanced with collision checks.
    public void RegisterProvider(IChatProvider provider)
    {
        _chatProviders[provider.channelIdentity] = provider;
    }

    public void UnregisterProvider(IChatProvider provider)
    {
        _chatProviders.Remove(provider.channelIdentity);
    }

    // When a message needs to be issued to a chat provider--like a ban message or a command reaction message--this is what to send. For now, it will just send a string to be displayed, but later it will send commands.
    public void SendMessage(string message)
    {
        Console.WriteLine($"SENDING: {message}");
        //OnMessageSent?.Invoke(message);
    }
}