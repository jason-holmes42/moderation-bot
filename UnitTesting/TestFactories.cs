using BotCore.Core.Messaging;
using BotCore.Core.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting;

// A library of shorthand functions used to make writing tests simpler
public static class TestFactories
{
    // Create a default MessageContext object
    public static MessageContext GenerateMessage(string messageContent, string username = "--Incoming Test User--")
    {
        return new MessageContext(
            new ChatMessage(user: username, msg: messageContent),
            new ChatEndpoint(platform: ProviderID.ChatReplay, channelID: "--Unit Test Provider--")
        );
    }

    // Tokenize an input string in the same manner as the CommandService would prior to passing the tokens to the command.
    public static string[] CommandServiceTokenize(string input)
    {
        return input
            .Substring(1)                                           // Remove the command character
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);     // Split the string on ' ' characters into a string array of tokens
    }
}
