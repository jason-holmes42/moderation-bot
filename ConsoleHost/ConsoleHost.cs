namespace ConsoleHost;
using BotCore;
using BotCore.Core;
using ChatReplayProvider;
using System.Linq.Expressions;

// The console host and driver for the bot. Used to display incoming data once it has been converted. Primarily for early testing and designed to be supplanted by a more feature-rich WPF console display.
internal class ConsoleHost
{
    IChatProvider chatProvider;
    BotCore botCore;

    string fullLogFilepath = "replayLogs/Full Log.json";

    static async Task Main(string[] args)
    {
        ConsoleHost consoleHost = new ConsoleHost();
        
        await consoleHost.Run();        
    }

    async Task Run()
    {
        // Initialize chat provider. This is where you'd insert Chat Provider selection logic, but for now we're only using ChatReplayProvider.
        ChatReplayProvider chatReplay = await ChatReplayProvider.CreateAsync(fullLogFilepath);
        chatProvider = chatReplay;

        // Initialize bot
        botCore = await InitializeBot();

        // Register cross-communication events
        chatReplay.OnMessageReceived += OnMessageReceived;
        botCore.OnMessageSent += chatReplay.SendMessage;

        // Enter processing loop
        await chatReplay.StartAsync();
    }

    // Instantiate a BotCore object and return it. Separated as its own function for future-proofing.
    async Task<BotCore> InitializeBot()
    {
        BotCore botCore = new BotCore();
        await botCore.Initialize(chatProvider.channelIdentity);
        return botCore;
    }

    // Display the message in the console, then send it to BotCore for processing.
    async void OnMessageReceived(MessageContext data)
    {
        // Display message first to accurately reflect what a live chat would look like
        ShowMessage(data);

        // send to bot for processing (filtering, command reactions, etc.). Error handling since async void can be a little risky.
        try
        {
            await botCore.ProcessMessage(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }

    }

    // Take a ChatMessage and display it in the console.
    void ShowMessage(MessageContext data)
    {
        Console.WriteLine($"[{data.timestamp}] {data.username}: {data.message}");
    }
}
