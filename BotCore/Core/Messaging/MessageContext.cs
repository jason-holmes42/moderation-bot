using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatModerationBot.Core.Providers;
using ChatModerationBot.Filtering;

namespace ChatModerationBot.Core.Messaging;

// Defines a data container including the original ChatMessage that was received as well as other information useful for bot functions.
public class MessageContext
{
    public ChatMessage ChatMessage { get; init; }
    public IChatProvider Provider { get; init; }
    public ChatEndpoint Endpoint { get; init; }

    // Permissions level
    public ModerationAction? ModAction { get; set; }
    public ReactionType ReactionType { get; set; }
    public string? ReactionString { get; set; }

    // Passthrough properties to make referencing ChatMessage data less of a pain
    public string Message => ChatMessage.Message;
    public string Username => ChatMessage.Username;
    public string Timestamp => ChatMessage.Timestamp;

    public MessageContext(ChatMessage messageData, ChatEndpoint endpoint)
    {
        ChatMessage = messageData;
        ReactionType = ReactionType.None;
        Endpoint = endpoint;
    }
}