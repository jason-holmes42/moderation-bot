using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Core.Messaging;
using BotCore.Filtering;

namespace BotCore.Core.Providers;

// Interface for providers of incoming data streams from chats. Chat Providers are responsible for converting incoming platform-specific data to internal ChatMessage data and sending it on for BotCore to process.
// Additionally, they convert any outgoing communications to platform-specific commands.
public interface IChatProvider
{
    ChatEndpoint channelIdentity { get; init; }         // Routing endpoint containing the platformID and channelID

    Task StartAsync();                                  // The asynchronous processing of incoming messages.

    event Action<MessageContext> OnMessageReceived;     // Event for handling incoming data

    void PostMessage(string message);                   // Function for posting messages into the chat on the given platform. Not implemented for any live platforms on this project.

    void IssuePunishment(ModerationAction modAction);   // Function for issuing punishments to a user based on a moderation decision by the bot

    // API Functions - requesting information from the platform.

    public Task<QueryResult> QueryUptimeAsync();        // Get the duration that the stream has been live
}