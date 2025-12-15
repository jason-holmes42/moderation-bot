using BotCore;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace ChatReplayProvider;

// A JSON receptacle class for incoming Twitch data. Only suitable for VOD replay chat logs used by Chat Replay.
public class TwitchJSONData
{
    public TwitchCommenterData commenter { get; init; }
    public TwitchMessageData message { get; init; }
    public string created_at { get; init; }
    public int content_offset_seconds {  get; init; }
}

// Receptacle for the nested commenter information within the Twitch data stream.
public class TwitchCommenterData
{
    public string display_name { get; init; }
}

// Receptacle for the nested message information within the Twitch data stream.
public class TwitchMessageData
{
    public string body { get; init; }
}

// A Chat Provider for treating saved chat log files as if live chat streams. Only designed to support Twitch VOD chat replays which contain all of the necessary content and timing data.
public class ChatReplayProvider : IChatProvider
{
    public event Action<ChatMessage>? OnMessageReceived;

    // Temporary display test, to be replaced with actual functionality once complete.
    public async Task StartAsync()
    {
        Console.WriteLine("Initiating Test Chat");
        string[] testMessages =
        [
            """{"commenter": {"display_name": "TestUser1"}, "message": {"body": "Hello, world!"}, "created_at": "2025-12-11T13:01:05Z", "content_offset_seconds":5}""",
            """{"commenter": {"display_name": "TestUser1"}, "message": {"body": "This is the second test message."}, "created_at": "2025-12-11T13:01:10Z", "content_offset_seconds":10}""",
            """{"commenter": {"display_name": "TestUser1"}, "message": {"body": "These are for display testing only, so nothing interesting will be contained."}, "created_at": "2025-12-11T13:01:15Z", "content_offset_seconds":15}""",
            """{"commenter": {"display_name": "TestUser1"}, "message": {"body": "The full test should take just under 30 seconds in total."}, "created_at": "2025-12-11T13:01:20Z", "content_offset_seconds":20}""",
            """{"commenter": {"display_name": "TestUser1"}, "message": {"body": "Test complete; thank you!"}, "created_at": "2025-12-11T13:01:25Z", "content_offset_seconds":25}""",
        ];

        string[] testMessages2 =
        [
            """{"_id": "QKi1FrPMaxhGXw","created_at": "2025-10-06T04:32:28.9705118Z","channel_id": "22379976","content_type": "video","content_id": "2584527176","content_offset_seconds": 4,"commenter": {"display_name": "x10Power","_id": "16282597","name": "x10power","bio": "Hello my name is Nick and I welcome you to my humble stream. I am an admin for a amazing gaming team know as Lzuruha! Which you can find more information about underneath with the Lzuruha tab. Hope you all enjoy yourself while you are watching and chatting!","created_at": "2010-10-03T21:45:16.274387Z","updated_at": "2025-10-04T20:32:21.010076Z","logo": "https://static-cdn.jtvnw.net/jtv_user_pictures/3133c9fd-77b1-4a1f-b38a-6d479f2c89b7-profile_image-300x300.png"},"message": {"body": "I want to get the FFT Remake","bits_spent": 0,"fragments": [{"text": "I want to get the FFT Remake","emoticon": null}],"user_badges": [{"_id": "bits","version": "100"}],"user_color": "#800909","emoticons": []}}""",
            """{"_id": "V7G1FrPMaxibEQ","created_at": "2025-10-06T04:32:28.9705123Z","channel_id": "22379976","content_type": "video","content_id": "2584527176","content_offset_seconds": 8,"commenter": {"display_name": "Greffery","_id": "91554149","name": "greffery","bio": "Come in, kick up your feet and relax.  I mostly play RPGs and I sprinkle in some randomizers, MMOs, and speedruns when my hands decide to be agreeable.  ","created_at": "2015-05-21T02:09:55.493792Z","updated_at": "2025-10-04T15:43:18.664189Z","logo": "https://static-cdn.jtvnw.net/jtv_user_pictures/greffery-profile_image-01af9cb5b9f35d3a-300x300.png"},"message": {"body": "Less talk more coin toss summons","bits_spent": 0,"fragments": [{"text": "Less talk more coin toss summons","emoticon": null}],"user_badges": [],"user_color": "#1E90FF","emoticons": []}}""",
            """{"_id": "xLW1FrPMaxgomQ","created_at": "2025-10-06T04:32:28.9705129Z","channel_id": "22379976","content_type": "video","content_id": "2584527176","content_offset_seconds": 12,"commenter": {"display_name": "x10Power","_id": "16282597","name": "x10power","bio": "Hello my name is Nick and I welcome you to my humble stream. I am an admin for a amazing gaming team know as Lzuruha! Which you can find more information about underneath with the Lzuruha tab. Hope you all enjoy yourself while you are watching and chatting!","created_at": "2010-10-03T21:45:16.274387Z","updated_at": "2025-10-04T20:32:21.010076Z","logo": "https://static-cdn.jtvnw.net/jtv_user_pictures/3133c9fd-77b1-4a1f-b38a-6d479f2c89b7-profile_image-300x300.png"},"message": {"body": "Still deciding on PC or Switch 2","bits_spent": 0,"fragments": [{"text": "Still deciding on PC or Switch 2","emoticon": null}],"user_badges": [{"_id": "bits","version": "100"}],"user_color": "#800909","emoticons": []}}"""
        ];

        ChatMessage tempMessage;
        int timeElapsed = 0;
        
        Console.WriteLine("Begin Playback");
        for (int i = 0; i < testMessages.Length; i++)
        {
            tempMessage = ParseData(testMessages[i]);                               // Convert the raw string data into ChatMessage
            MessageReceived(tempMessage);                                           // Fire OnMessageReceived event
            await Task.Delay((tempMessage.offsetSeconds - timeElapsed) * 1000);     // Delay wants times in milliseconds but we're measuring by seconds, so multiply by 1000
            timeElapsed += tempMessage.offsetSeconds - timeElapsed;                 // Update playback time elapsed for future message timing
        }

        Console.WriteLine("Attempting Test 2");
        timeElapsed = 0;
        for (int i = 0; i < testMessages2.Length; i++)
        {
            tempMessage = ParseData(testMessages2[i]);
            MessageReceived(tempMessage);
            await Task.Delay((tempMessage.offsetSeconds - timeElapsed) * 1000);
            timeElapsed += tempMessage.offsetSeconds - timeElapsed;
        }
    }

    // Convert incoming JSON data into a ChatMessage for sending to OnMessageReceived.
    ChatMessage ParseData(string raw)
    {
        // Convert the raw JSON string into meaningful data
        TwitchJSONData? DataJSON = JsonSerializer.Deserialize<TwitchJSONData>(raw);
        DateTime JSONTime = DateTime.Parse(DataJSON.created_at, null, System.Globalization.DateTimeStyles.RoundtripKind);
        // TODO: Deserialization error testing, try/catch
        
        // Return a ChatMessage bearing the JSON data
        return new ChatMessage(DataJSON.commenter.display_name, DataJSON.message.body, JSONTime, DataJSON.content_offset_seconds);
    }

    // Invoke the OnMessageReceived event
    void MessageReceived(ChatMessage message)
    {
        OnMessageReceived?.Invoke(message);
    }

    // Send a message from the bot to the platform in question. Not implemented in this version; stub only.
    public async Task SendMessage(string outMessage)
    {
        Console.WriteLine("Sending: " + outMessage);
    }

}