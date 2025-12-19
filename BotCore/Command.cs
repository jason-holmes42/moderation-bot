using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore;
internal class Command
{
    public static void Evaluate(ChatMessage messageData)
    {
        // Trim any whitespace characters for simple tokenization.
        string input = messageData.message.Trim();

        // Establish the command character that should precede commands. Custom command characters would be defined here.
        char commandChar = '!';

        // If the first character of the trimmed string is not the command character, skip. 
        if (input[0] != commandChar)
        {
            return;
        }

        // Elsewise, tokenize and parse as a command.
        string[] tokens = Tokenize(input);
        Console.WriteLine($"Command identified: {String.Join(", ", tokens)}");
    }

    // Convert incoming string into a collection of actionable tokens delimited by the space (' ') character.
    static string[] Tokenize(string input)
    {
        // The input will already be trimmed at front and back, so we just need to trim the command character as well so that the first token is the command string or alias.
        // Since this is a simple string manipulation, we can chain them into a single return statement and split the line to make it easier to read.
        return input
            .Substring(1)                                           // Remove the command character
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);     // Split the string on ' ' characters into a string array of tokens
    }
}