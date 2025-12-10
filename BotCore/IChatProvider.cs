using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore
{
    public interface IChatProvider
    {
        Task SendMessage(string message);               // Stub for sending messages to the given platform. Not (meaningfully) implemented in this project.

        event Action<ChatMessage> OnMessageReceived;    // Event for handling incoming data
    }
}
