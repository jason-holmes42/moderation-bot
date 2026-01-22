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
    public Color UserColor { get; init; }
    public string ChatMessage { get; init; }
    public string BotReaction { get; set; } = "";

    public MessageItem(string user, string message, string timestamp, Color? color = null)
    {
        Username = user;
        UserColor = color ?? Color.White;
        ChatMessage = $"[{timestamp}] {user}: {message}";
    }
}
