using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore
{
    // Defines a data container for incoming chat messages for use by the bot. ChatProviders convert incoming data into a ChatMessage before passing it on to the BotCore for processing.
    public class ChatMessage
    {
        public string username;
        public string message;
        public DateTime timestamp;
    }
}
