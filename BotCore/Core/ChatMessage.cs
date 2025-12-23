using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core;

// Defines a data container for incoming chat messages for use by the bot. This is a record of the incoming message; MessageContext contains additional data for how the bot is handling the message.
public class ChatMessage
{
    public string username { get; set; }
    public string message { get; init; }
    public int offsetSeconds { get; init; }
    public string timestamp { get; init; }

    public ChatMessage(string user, string msg, int offset = 0, string time = "")
    {
        username = user;
        message = msg;
        offsetSeconds = offset;
        timestamp = time;
    }
}
