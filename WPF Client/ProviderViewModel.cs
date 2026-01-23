using ChatModerationBot;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WPFClient.Commands;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFClient;
public class ProviderViewModel
{
    public ProviderID Provider { get; set; }
    public string UserIdentity { get; set; }

    public ObservableCollection<MessageItem> MessageItems { get; } = new();


    IChatProvider _chatProvider;
    CancellationTokenSource _cancelTokenSource = new();
    BotCore _botCore;

    public ProviderViewModel(BotCore botCore, string userIdentity, ProviderID platform, IChatProvider chatProvider)
    {
        UserIdentity = userIdentity;
        Provider = platform;

        _botCore = botCore;
        _chatProvider = chatProvider;
    }

    public static async Task<ProviderViewModel> CreateAsync(BotCore botCore, string userIdentity, ProviderID platform, string fileToLoad = "")
    {
        // Instantiate the relevant provider
        IChatProvider chatProvider = null;

        switch (platform)
        {
            case ProviderID.ChatReplay:
                chatProvider = await ChatReplayProvider.ChatReplayProvider.CreateAsync(userIdentity, fileToLoad!);
                break;
            default:
                // This case should never occur due to protections further up the processing chain. If it somehow does, I want everything to stop so it can be resolved.
                throw new InvalidOperationException($"Invalid ProviderID: {platform}");
        }

        // Register that provider with the bot
        await botCore.RegisterProvider(chatProvider);

        // Finish setup
        return new ProviderViewModel(botCore, userIdentity, platform, chatProvider!);
    }

    async void DisplayMessage(MessageContext messageData)
    {
        MessageItem entry = new(user: messageData.Username, message: messageData.Message, timestamp: messageData.Timestamp);

        if (messageData.ModAction != null)
        {
            entry.BotReaction = $"{messageData.ModAction.Punishment} on {messageData.ModAction.TargetUser}. Reason: {messageData.ModAction.Reason}";
        } 
        else if (messageData.ReactionType != ChatModerationBot.Core.ReactionType.None)
        {
            entry.BotReaction = $"Valid command identified. Response: {messageData.ReactionString}";
        }

        MessageItems.Add(entry);

        // Temporarily display incoming message
        Console.WriteLine($"[{messageData.Timestamp}] {messageData.Username}: {messageData.Message}");
    }

    // Chat input for presentation demonstration only.
    public void SendMessage(string message)
    {
        (_chatProvider as ChatReplayProvider.ChatReplayProvider).PostMessage(UserIdentity, message);
    }

    public void Cleanup()
    {
        _cancelTokenSource.Cancel();    // Send a cancellation request event to the token, which the provider is monitoring to know when it should stop
        UnregisterProvider();
    }

    // Unregister provider and unsubscribe from processed messages
    async Task UnregisterProvider()
    {
        _botCore.OnMessageProcessed -= DisplayMessage;
        await _botCore.UnregisterProvider(_chatProvider);
    }

    // Initiate playback
    public async Task StartAsync(string fileToLoad = "")
    {
        // Subscribe to the bot's message processed event to receive messages from the playback once they finish processing
        _botCore.OnMessageProcessed += DisplayMessage;

        // Initiate playback
        await _chatProvider.StartAsync(_cancelTokenSource.Token);
    }
}
