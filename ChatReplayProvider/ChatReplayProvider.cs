using ChatModerationBot.Core.Messaging;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Filtering;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace ChatReplayProvider;

// A Chat Provider for treating saved chat log files as if live chat streams. Only designed to support Twitch VOD chat replays which contain all of the necessary content and timing data.
public class ChatReplayProvider : IChatProvider
{
    private static readonly ProviderID _platform = ProviderID.ChatReplay;
    private static readonly string _filepath = "replayLogs/";

    public ChatEndpoint ChannelIdentity { get; init; }      // Used to identify this specific provider for message routing from BotCore

    int _timeElapsed;            // Used to keep track of the seconds elapsed as provided by replay log's offset seconds
    DateTime _replayStart;       // Used to track time since replay began for dynamic timestamp creation for PostMessage

    List<ChatMessage> _commentData;

    public event Action<MessageContext>? OnMessageReceived;

    public ChatReplayProvider(string broadcaster, TwitchJSONData jsonData)
    {
        ChannelIdentity = new ChatEndpoint(_platform, broadcaster);
        _commentData = ParseComments(jsonData);
    }

    // Load data from file. Async due to the size of files potentially taking some time / allowing for loading from alternate sources.
    public static async Task<ChatReplayProvider> CreateAsync(string platformIdentity, string filename)
    {
        string filepath = $"{_filepath}{filename}";
        TwitchJSONData jsonData = await ParseData(filepath);

        return new ChatReplayProvider(platformIdentity, jsonData);
    }

    // Temporary display test, to be replaced with actual functionality once complete.
    public async Task StartAsync(CancellationToken cancelToken)
    {
        Console.WriteLine("Begin Playback");
        try { await PlaybackData(_commentData, cancelToken); }
        catch (OperationCanceledException) { Console.WriteLine("Playback cancelled."); }
    }

    // Parse the selected chat replay log file into Twitch JSON data transfer objects (DTOs).
    static async Task<TwitchJSONData> ParseData(string filepath)
    {
        Console.WriteLine("Loading data...");

        // Convert the raw JSON string into meaningful data
        try
        {
            using StreamReader r = new StreamReader(filepath);
            string raw = await r.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(raw))
            {
                Console.WriteLine("Error: JSON loading failed; chat replay object not populated.");
                return default;
            }

            return JsonSerializer.Deserialize<TwitchJSONData>(raw);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Chat replay file contains invalid JSON: {ex.Message}");
            return default;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Could not read chat replay file at {filepath}: {ex.Message}");
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error loading {filepath} chat replay file: {ex.Message}");
            return default;
        }
    }

    // Take the raw JSON data and extract the comments as a list of ChatMessage objects for sending to OnMessageReceived
    List<ChatMessage> ParseComments(TwitchJSONData DataJSON)
    {   
        List<ChatMessage> messageData = new List<ChatMessage> { };

        // Convert every entry in JSON 'comments' into a ChatMessage, stored in the messageData list
        foreach (TwitchCommentsData entry in DataJSON.comments)
        {
            messageData.Add(new ChatMessage(entry.commenter.display_name, entry.message.body, entry.message.user_color, entry.content_offset_seconds, ConvertTimestamp(entry.content_offset_seconds)));
        }

        // Return a list bearing ChatMessages containing the JSON data
        return messageData;
    }

    // Handle the timing of each message by iterating through the list of messages received from ParseData
    async Task PlaybackData(List<ChatMessage> replayData, CancellationToken cancelToken)
    {
        _timeElapsed = 0;    // For tracking against log-provided offsetSeconds
        _replayStart = DateTime.Now; // For tracking against real time for use with PostMessage function

        // Initial delay to match up with the first message's delay.
        await Task.Delay(replayData[0].OffsetSeconds * 1000, cancelToken);

        while (!cancelToken.IsCancellationRequested)
        {
            for (int i = 0; i < replayData.Count; i++)
            {
                _timeElapsed += replayData[i].OffsetSeconds - _timeElapsed;

                MessageReceived(replayData[i]);

                if (i + 1 < replayData.Count)
                {
                    // Handle the wait until the next message using the next message's offset time.
                    await Task.Delay((replayData[i + 1].OffsetSeconds - _timeElapsed) * 1000, cancelToken);
                }
                else
                {
                    Console.WriteLine("Playback finished.");
                }
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
        MessageContext messageData = new MessageContext(message, ChannelIdentity);
        OnMessageReceived?.Invoke(messageData);
    }

    // Send a message from the bot to the platform in question. For ChatReplay, it processes the outgoing message as if it were an actual message within the log, allowing the bot to react accordingly.
    public void PostMessage(string username, string outMessage)
    {
        int secondsSinceStart = (int)(DateTime.Now - _replayStart).TotalSeconds;
        ChatMessage chatMessage = new ChatMessage(username, outMessage, "#FF0000", secondsSinceStart, ConvertTimestamp(secondsSinceStart));
        MessageReceived(chatMessage);
    }

    public void PostMessage(string outMessage)
    {
        PostMessage("ModerationBot", outMessage);
    }

    // Issue a punishment according to provided ModerationAction information using provider-specific functions.
    public void IssuePunishment(ModerationAction modAction)
    {
        // Issue punishment
        // Log reason
        Console.WriteLine($"Punishing {modAction.TargetUser} with {modAction.Punishment.ToString().ToUpper()}. REASON: {modAction.Reason}");
    }

    // ======= API FUNCTIONS =======

    // Request the duration that the stream has been live from the platform.
    public async Task<QueryResult> QueryUptimeAsync()
    {
        // Since ChatReplay is not live, we will use the timeElapsed parameter from the Playback function to mimic the typical result.
        return QueryResult.Success(TimeSpan.FromSeconds(_timeElapsed));
    }
}