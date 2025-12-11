namespace ConsoleHost;
using BotCore;
using ChatReplayProvider;

// The console host and driver for the bot. Used to display incoming data once it has been converted. Primarily for early testing and designed to be supplanted by a more feature-rich WPF console display.
internal class ConsoleHost
{
    static void Main(string[] args)
    {
        // Initialize bot

        // Initialize chat provider
        ChatReplayProvider chatReplay = new ChatReplayProvider();
        chatReplay.OnMessageReceived += ShowMessage;
        chatReplay.ProvideTestChat();

        // Enter processing loop

    }

    // Take a ChatMessage and display it in the console.
    static void ShowMessage(ChatMessage data)
    {
        Console.WriteLine(data.username + ": " + data.message);
    }
}
