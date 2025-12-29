using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCore.Commands;
using BotCore.Core.Time;

namespace BotCore.Core.Cooldowns;
internal class CooldownTracker
{
    Dictionary<string, DateTime> cooldownRegistry = new Dictionary<string, DateTime>();
    ITimeProvider timeProvider;

    // This dictionary dictates the cooldown of each command by the cooldown type defined in the command itself.
    static readonly Dictionary<CooldownType, TimeSpan> typeCooldowns = new Dictionary<CooldownType, TimeSpan>()
    {
        { CooldownType.None, TimeSpan.FromSeconds(0) },             // Used for admin commands usually restricted by permissions
        { CooldownType.CoreCommand, TimeSpan.FromSeconds(5)  },     // Used for functional commands such as !uptime
        { CooldownType.CustomCommand, TimeSpan.FromSeconds(15)  },  // Used for user-defined response commands.
        { CooldownType.API, TimeSpan.FromSeconds(30)  }             // Used for commands resulting in external API calls. Not currently implemented; reserved for future extension.
    };

    public CooldownTracker(ITimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    // Function that only answers the question "Is this item on cooldown?"
    public bool IsOffCooldown(ICommand command)
    {
        // CooldownTracker uses lazy query tracking, meaning it only updates when asked.
        DateTime lastUsedTimestamp;

        // If the entry is registered, check when it was last used.
        if (cooldownRegistry.TryGetValue(command.commandString, out lastUsedTimestamp))
        {
            DateTime currentTime = timeProvider.Now;

            // If the command has defined a custom cooldown override, use that as the cooldown. Otherwise, base the cooldown on the registered cooldown type.
            TimeSpan cooldown = command.cooldownOverride ?? typeCooldowns[command.cooldownType];

            // If it has been beyond the cooldown time, it's ready for use and should be removed from the registry.   
            if (currentTime - lastUsedTimestamp >= cooldown)
            {
                cooldownRegistry.Remove(command.commandString);
                return true;
            }
            // Otherwise it isn't off cooldown yet.
            else return false;
        }
        // If the entry is not registered, then it isn't on cooldown so it can be used.
        else return true;
    }

    // Function that triggers a cooldown, usually simultaneously with a command being issued.
    public void StartCooldown(string entry)
    {
        // Certain users may be immune to cooldown restrictions but their command uses should still trigger that cooldown for others, so it is possible that an entry may already exist.
        // By assigning the entry's timestamp in this manner, it will automatically add a new entry or update an existing entry without issue.
        cooldownRegistry[entry] = timeProvider.Now;
    }
}
