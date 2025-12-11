using BotCore;
using System.Text.Json;

namespace ChatReplayProvider;

// A Chat Provider for treating saved chat log files as if live chat streams. Only designed to support Twitch VOD chat replays which contain all of the necessary content and timing data.
public class ChatReplayProvider : IChatProvider
{
    public event Action<ChatMessage> OnMessageReceived;

    // Send a message from the bot to the platform in question. Not implemented in this version; stub only.
    public async Task SendMessage(string message)
    {
        Console.WriteLine("Sending: " + message);
    }

    // Convert incoming JSON data into a ChatMessage for sending to OnMessageReceived.
    void ParseData(string raw)
    {
        
        OnMessageReceived?.Invoke(new ChatMessage());
    }
}
