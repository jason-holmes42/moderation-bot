using BotCore.Core;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace ChatReplayProvider;

// A Chat Provider for treating saved chat log files as if live chat streams. Only designed to support Twitch VOD chat replays which contain all of the necessary content and timing data.
public class ChatReplayProvider : IChatProvider
{
    int timeElapsed; 

    public event Action<MessageContext>? OnMessageReceived;

    // Temporary display test, to be replaced with actual functionality once complete.
    public async Task StartAsync()
    {
        Console.WriteLine("Loading data...");
        using StreamReader r = new StreamReader("replayLogs/Full Log.json");
        string jsonData = r.ReadToEnd();

        Console.WriteLine("Begin Playback");
        List<ChatMessage> commentData = ParseData(jsonData);
        await PlaybackData(commentData);
    }

    // Convert incoming JSON data into an array of ChatMessages for sending to OnMessageReceived.
    List<ChatMessage> ParseData(string raw)
    {
        TwitchJSONData? DataJSON = JsonSerializer.Deserialize<TwitchJSONData>(raw);     // Convert the raw JSON string into meaningful data
        // TODO: Deserialization error testing, try/catch
        if (DataJSON == null)
        {
            Console.WriteLine("JSON deserialization failed; object not populated.");
            return null;
        }
        
        List<ChatMessage> messageData = new List<ChatMessage> { };

        // Convert every entry in JSON 'comments' into a ChatMessage, stored in the messageData list
        foreach (TwitchCommentsData entry in DataJSON.comments)
        {
            messageData.Add(new ChatMessage(entry.commenter.display_name, entry.message.body, entry.content_offset_seconds, ConvertTimestamp(entry.content_offset_seconds)));
        }

        // Return a list bearing ChatMessages containing the JSON data
        return messageData;
    }

    // Handle the timing of each message by iterating through the list of messages received from ParseData
    async Task PlaybackData(List<ChatMessage> replayData)
    {
        timeElapsed = 0;

        // Initial delay to match up with the first message's delay.
        await Task.Delay(replayData[0].offsetSeconds * 1000);

        for (int i = 0; i < replayData.Count; i++)
        {
            timeElapsed += replayData[i].offsetSeconds - timeElapsed;

            MessageReceived(replayData[i]);

            if (i + 1 < replayData.Count)
            {
                // Handle the wait until the next message using the next message's offset time.
                await Task.Delay((replayData[i + 1].offsetSeconds - timeElapsed) * 1000);

            } else
            {
                Console.WriteLine("Playback finished.");
            }
        }
    }

    // Convert the content_offset_seconds information into a time-since-VOD-began timestamp
    string ConvertTimestamp(int offsetSeconds)
    {
        TimeSpan elapsedTime = TimeSpan.FromSeconds(offsetSeconds);
        string timeString;

        // Add hour display only if elapsedTime is over an hour (3600 seconds) to keep the first hour of timestamps a little cleaner.
        timeString = offsetSeconds >= 3600 ? elapsedTime.ToString(@"hh\:mm\:ss") : elapsedTime.ToString(@"mm\:ss");

        return timeString;
    }

    // Invoke the OnMessageReceived event
    void MessageReceived(ChatMessage message)
    {
        MessageContext messageData = new MessageContext(message, this);
        OnMessageReceived?.Invoke(messageData);
    }

    // Send a message from the bot to the platform in question. Not implemented in this version; stub only.
    public void SendMessage(string outMessage)
    {
        Console.WriteLine("Sending: " + outMessage);
    }

    // ======= API FUNCTIONS =======

    // Request the duration that the stream has been live from the platform.
    public async Task<TimeSpan> QueryUptimeAsync()
    {
        // Since ChatReplay is not live, we will use the timeElapsed parameter from the Playback function to mimic the typical result.
        return TimeSpan.FromSeconds(timeElapsed);
    }

}