namespace ConsoleHost;
using BotCore;
using ChatReplayProvider;

// The console host and driver for the bot. Used to display incoming data once it has been converted. Primarily for early testing and designed to be supplanted by a more feature-rich WPF console display.
internal class ConsoleHost
{
    IChatProvider chatProvider;
    BotCore botCore;

    static async Task Main(string[] args)
    {
        ConsoleHost consoleHost = new ConsoleHost();
        
        await consoleHost.Run();        
    }

    async Task Run()
    {
        // Initialize bot
        botCore = InitializeBot();

        // Initialize chat provider. This is where you'd insert Chat Provider selection logic
        ChatReplayProvider chatReplay = new ChatReplayProvider();

        // Register cross-communication events
        chatReplay.OnMessageReceived += ProcessMessage;
        botCore.OnMessageSent += chatReplay.SendMessage;

        // Enter processing loop
        await chatReplay.StartAsync();
    }

    // Instantiate a BotCore object and return it. Separated as its own function for future-proofing.
    BotCore InitializeBot()
    {
        BotCore botCore = new BotCore();
        botCore.Initialize();
        return botCore;
    }

    // Send the message to the bot for processing, then display it
    void ProcessMessage(MessageContext data)
    {
        // Display message first to accurately reflect what a live chat would look like
        ShowMessage(data);

        // send to bot for processing (filtering, command reactions, etc.)
        botCore.ProcessMessage(data);
    }

    // Take a ChatMessage and display it in the console.
    void ShowMessage(MessageContext data)
    {
        Console.WriteLine($"[{data.timestamp}] {data.username}: {data.message}");
    }
}
