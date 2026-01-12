using BotCore.Core.Messaging;
using BotCore.Core.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting;

// A library of shorthand functions used to make writing tests simpler
public static class MessageContextFactory
{
    public static MessageContext GenerateMessage(string messageContent)
    {
        return new MessageContext(
            new ChatMessage(user: "--Incoming Test User--", msg: messageContent),
            new ChatEndpoint(platform: ProviderID.ChatReplay, channelID: "--Unit Test Provider--")
        );
    }
}
