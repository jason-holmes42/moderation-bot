using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore;

// Interface for providers of incoming data streams from chats. Chat Providers are responsible for converting incoming platform-specific data to internal ChatMessage data and sending it on for BotCore to process.
// Additionally, they convert any outgoing communications to platform-specific commands.
public interface IChatProvider
{
    Task StartAsync();                              // The asynchronous processing of incoming messages.

    event Action<MessageContext> OnMessageReceived; // Event for handling incoming data

    void SendMessage(string message);               // Stub for sending messages to the given platform. Not (meaningfully) implemented in this project.

    // API Functions - requesting information from the platform.

    TimeSpan QueryUptimeAsync();                    // Get the duration that the stream has been live
}
