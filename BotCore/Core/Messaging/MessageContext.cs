using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core.Providers;
using BotCore.Filtering;

namespace BotCore.Core.Messaging;

// Defines a data container including the original ChatMessage that was received as well as other information useful for bot functions.
public class MessageContext
{
    public ChatMessage Message { get; init; }
    public IChatProvider Provider { get; init; }
    public ChatEndpoint Endpoint { get; init; }

    // Permissions level
    public ModerationAction? modAction { get; set; }
    public ReactionType reactionType { get; set; }
    public string? reactionString { get; set; }

    // Passthrough properties to make referencing ChatMessage data less of a pain
    public string message => Message.message;
    public string username => Message.username;
    public string timestamp => Message.timestamp;

    public MessageContext(ChatMessage messageData, IChatProvider chatProvider, ChatEndpoint endpoint)
    {
        Message = messageData;
        Provider = chatProvider;
        reactionType = ReactionType.None;
        Endpoint = endpoint;
    }
}