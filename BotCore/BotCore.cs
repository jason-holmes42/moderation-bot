namespace BotCore;

// Core functionality for the moderation bot.
public class BotCore
{
    FilterService? filterService;
    CommandService? commandService;
    ConfigService? configService;

    public async void Initialize()
    {
        filterService = await FilterService.CreateAsync();
        commandService = await CommandService.CreateAsync();
        configService = await ConfigService.CreateAsync();
    }

    public event Action<string>? OnMessageSent;
    public void ProcessMessage(MessageContext message)
    {
        // Identify the user's permissions level (if any)
        
        // Send message through filtering, apply any necessary reaction information
        filterService.Evaluate(message);
        
        // Based on the filter's evaluation, identify whether the message needs to be punished.
        if (message.reactionType != ReactionType.None)  // Since commands have not yet been processed, it will be none unless a punishment is needed.
        {
            // Punish. For now, we'll just send a message for display.
            SendMessage(message.reactionString);
        }

        // Assess message for commands, process any identified commands
        commandService.Evaluate(message);
        if (message.reactionType == ReactionType.Command)
        {
            SendMessage(message.reactionString);
        }
    }

    // When a message needs to be issued to a chat provider--like a ban message or a command reaction message--this is what to send. For now, it will just send a string to be displayed, but later it will send commands.
    public void SendMessage(string message)
    {
        OnMessageSent?.Invoke(message);
    }
}