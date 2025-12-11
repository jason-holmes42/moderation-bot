namespace ConsoleHost;
using BotCore;

// The console host and driver for the bot. Used to display incoming data once it has been converted. Primarily for early testing and designed to be supplanted by a more feature-rich WPF console display.
internal class ConsoleHost
{
    static void Main(string[] args)
    {
        // Initialize bot
        // Initialize chat provider
        // Enter processing loop

        // Temporary display test, to be transferred to Chat Provider and eventually replaced with actual functionality.
        ChatMessage[] testMessages = new ChatMessage[5];

        testMessages[0] = new ChatMessage("TestUser1", "Hello, world!", new DateTime(2025, 12, 11, 13, 01, 05));
        testMessages[1] = new ChatMessage("TestUser1", "This is the second test message.", new DateTime(2025, 12, 11, 13, 01, 10));
        testMessages[2] = new ChatMessage("TestUser1", "These are for display testing only, so nothing interesting will be contained", new DateTime(2025, 12, 11, 13, 01, 15));
        testMessages[3] = new ChatMessage("TestUser1", "The full test should take just under 30 seconds in total.", new DateTime(2025, 12, 11, 13, 01, 20));
        testMessages[4] = new ChatMessage("TestUser1", "Test complete; thank you!", new DateTime(2025, 12, 11, 13, 01, 25));

        DateTime currentTime = new DateTime(2025, 12, 11, 13, 01, 00);

        foreach (ChatMessage entry in testMessages)
        {
            ShowMessage(entry);
            Thread.Sleep(entry.timestamp - currentTime);
            currentTime = entry.timestamp;
        }
    }

    // Take a ChatMessage and display it in the console.
    static void ShowMessage(ChatMessage data)
    {
        Console.WriteLine(data.username + ": " + data.message);
    }
}
