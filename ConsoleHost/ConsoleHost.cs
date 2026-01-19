namespace ConsoleHost;
using ChatModerationBot;
using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatReplayProvider;
using System.Linq.Expressions;

// The console host and driver for the bot. Used to display incoming data once it has been converted. Primarily for early testing and designed to be supplanted by a more feature-rich WPF console display.
internal class ConsoleHost
{
    IChatProvider _chatProvider;
    BotCore _botCore;

    // These are temporary variables to stand-in for selection systems to be implemented later.
    string _fullLogFilepath = "replayLogs/Full Log.json";
    string _userIdentity = "testuser";

    static async Task Main(string[] args)
    {
        ConsoleHost consoleHost = new ConsoleHost();
        
        await consoleHost.Run();        
    }

    async Task Run()
    {
        // Initialize chat provider. This is where you'd insert both Chat Provider and Broadcaster selection logic, but for now we're only using ChatReplayProvider.
        ChatReplayProvider chatReplay = await ChatReplayProvider.CreateAsync(_fullLogFilepath);
        _chatProvider = chatReplay;

        // Initialize bot. Each user gets an individual bot instance that centralizes and handles their processing across whatever platforms they're using.
        _botCore = await BotCore.CreateAsync(_userIdentity);
        _botCore.RegisterProvider(_chatProvider);

        // Register cross-communication events
        chatReplay.OnMessageReceived += OnMessageReceived;

        // Enter processing loop
        await chatReplay.StartAsync();
    }

    // Display the message in the console, then send it to BotCore for processing.
    async void OnMessageReceived(MessageContext data)
    {
        // Display message first to accurately reflect what a live chat would look like
        ShowMessage(data);

        // send to bot for processing (filtering, command reactions, etc.). Error handling since async void can be a little risky.
        try
        {
            await _botCore.ProcessMessage(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }

    }

    // Take a ChatMessage and display it in the console.
    void ShowMessage(MessageContext data)
    {
        Console.WriteLine($"[{data.Timestamp}] {data.Username}: {data.Message}");
    }
}
