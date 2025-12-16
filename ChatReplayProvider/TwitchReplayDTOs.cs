using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatReplayProvider
{    
    // A JSON receptacle class for incoming Twitch data. Only suitable for Video-on-Demand (VOD) replay chat logs used by Chat Replay.
    public class TwitchJSONData
    {
        // The VOD replay chat log root/top-level JSON structure:

        // FileInfo - Version and creation information; not relevant.
        // streamer - Streamer information; name, login, id. Not currently relevant, but might be useful for expanded functionality.
        // clipper - null; only used for clips, not full VOD replays. Not relevant.
        // video - VOD details; title, description (written by streamer), ID, length in seconds, game, chapter information. Not relevant.
        public List<TwitchCommentsData> comments { get; init; }   // All comments are stored here.
        // embeddedData - null; not relevant.
    }

    // Receptacle for the nested comments data from the JSON file.
    public class TwitchCommentsData
    {
        // The 'comments' object is a simple time-sorted (by content_offset_seconds) list of all comments throughout the VOD.

        // _id - unique ID (UID) string for the comment. Not currently relevant.
        // created_at - ISO8601 date/time for when the comment was added to the generated log file. Not relevant.
        // channel_id - the channel ID from which the chat originated. Should be the same as the streamer ID. Not relevant.
        // content_type - "video"; not relevant
        // content_id - UID for the VOD; not currently relevant
        public int content_offset_seconds { get; init; }      // The offset (in seconds) from the start of the VOD that the chat message occurred. Necessary for proper timing.
        public TwitchCommenterData commenter { get; init; }   // Information about the commenter. Necessary.
        public TwitchMessageData message { get; init; }       // Information about the message. Necessary.

    }

    // Receptacle for the nested commenter information within the comments data.
    public class TwitchCommenterData
    {
        public string display_name { get; init; }               // The display name of the commenter. Necessary for proper display and for issuing reactions / punishments.
        // _id - UID for the commenter. Not currently relevant.
        // name - The commenter's Twitch-public name string; same as display_name, but lowercase. Not currently relevant.
        // bio - The commenter's self-written biography. Not relevant.
        // created_at - ISO8601 date/time for the commenter's account creation date. Not relevant.
        // updated_at - ISO8601 date/time for the last time some element of the commenter's profile details were updated. Not relevant.
        // logo - A URL to a PNG of the commenter's Twitch profile picture on the Twitch content delivery network (CDN). Not currently relevant, but may be useful if a more robust visual style is desired.
    }

    // Receptacle for the nested message information within the comments data.
    public class TwitchMessageData
    {
        public string body { get; init; }                       // The contents of the chat message
        // bits_spent - The amount of bits spent in the message. Not currently relevant, but some chats allow users to spend bits to highlight their message, so might be useful for expansion.
        // fragments - An object containing the body of the message divided into multiple objects by every emoticon used. Sub-objects include the 'text' for the fragment and 'emoticon' which is an object containing 'emoticon_id' the UID for the Twitch emoticon.
                // Not currently relevant, but will be if rendering the chat as it is seen on Twitch is wanted.
        // user_badges - User Badges display small icons before the user's name on Twitch denoting various statuses; Moderator, VIP, subscriber, etc. Contains a sub-object with a "_id" unique string identifier for the type of badge and a 'version' int.
                // Not currently relevant, but will be if rendering the chat as it is seen on Twitch is wanted.
        // user_color - hex color (e.g. #8A2BE2) value of the user's display name. Not currently relevant, but will be if rendering the chat as it is seen on Twitch is wanted.
        // emoticons - Null unless the message contains emoticons. Contains sub-objects indicating '_id' for the UID of the emoticon, 'begin' for the string index of where the emoticon text begins, and 'end' for the string index of where the emoticon code text.
                // Not currently relevant, but will be if rendering the chat as it is seen on Twitch is wanted.
    }
}
