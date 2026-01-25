using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFClient;
public class MessageItem
{
    public string Username { get; init; }
    public string UserColor { get; init; }
    public string Message { get; init; }
    public string Timestamp { get; init; }
    public string BotReaction { get; set; } = "";

    public MessageItem(string user, string message, string timestamp, string color)
    {
        Username = user;
        Message = message;
        Timestamp = timestamp;
        UserColor = color;
    }
}
