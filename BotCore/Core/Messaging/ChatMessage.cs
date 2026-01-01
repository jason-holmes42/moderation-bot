using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core.Messaging;

// Defines a data container for incoming chat messages for use by the bot. This is a record of the incoming message; MessageContext contains additional data for how the bot is handling the message.
public class ChatMessage
{
    public string Username { get; set; }
    public string Message { get; init; }
    public int OffsetSeconds { get; init; }
    public string Timestamp { get; init; }

    public ChatMessage(string user, string msg, int offset = 0, string time = "")
    {
        Username = user;
        Message = msg;
        OffsetSeconds = offset;
        Timestamp = time;
    }
}
