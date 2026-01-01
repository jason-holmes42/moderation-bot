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
    FilterService filterService;
    CommandService commandService;
    PermissionsService permissionsService;
    BotTimeProvider timeProvider;
    CooldownTracker cooldownTracker;

    Dictionary<ChatEndpoint, IChatProvider> chatProviders;

    public async Task Initialize(string broadcaster)
    {
        timeProvider = new BotTimeProvider();
        cooldownTracker = new CooldownTracker(timeProvider);

        permissionsService = await PermissionsService.CreateAsync(broadcaster);
        filterService = await FilterService.CreateAsync(permissionsService);
        commandService = await CommandService.CreateAsync(filterService, permissionsService, cooldownTracker);

        chatProviders = new Dictionary<ChatEndpoint, IChatProvider>();
    }

    public event Action<string>? OnMessageSent;
    Action<string>? onMessageSentHandler;

    // public event Func<string>? OnQuery;
    // Action<string>? onQueryHandler;

    public async Task ProcessMessage(MessageContext message)
    {        
        // Send message through filtering, apply any necessary reaction information
        filterService.Evaluate(message);
        
        // If the filter identified a needed moderation action, send it to the provider for processing.
        if (message.modAction != null)
        {
            chatProviders[message.Endpoint].IssuePunishment(message.modAction);
        }

        // Assess message for commands, process any identified commands
        await commandService.Evaluate(message);
        if (message.reactionString != null)
        {
            chatProviders[message.Endpoint].PostMessage(message.reactionString);
        }

        // Send MessageContext to UI for display
    }

    // Simple registry functions; to be enhanced with collision checks.
    public void RegisterProvider(IChatProvider provider)
    {
        chatProviders[provider.channelIdentity] = provider;
    }

    public void UnregisterProvider(IChatProvider provider)
    {
        chatProviders.Remove(provider.channelIdentity);
    }

    // When a message needs to be issued to a chat provider--like a ban message or a command reaction message--this is what to send. For now, it will just send a string to be displayed, but later it will send commands.
    public void SendMessage(string message)
    {
        Console.WriteLine($"SENDING: {message}");
        //OnMessageSent?.Invoke(message);
    }
}