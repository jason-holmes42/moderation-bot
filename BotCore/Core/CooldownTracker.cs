using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core;
internal class CooldownTracker
{
    Dictionary<string, DateTime> cooldownRegistry = new Dictionary<string, DateTime>();
    ITimeProvider timeProvider;

    TimeSpan cooldown = TimeSpan.FromSeconds(3);       // Stub cooldown duration of 3 seconds.

    public CooldownTracker(ITimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    // Function that only answers the question "Is this item on cooldown?"
    public bool IsOffCooldown(string entry)
    {
        // CooldownTracker uses lazy query tracking, meaning it only updates when asked.
        DateTime lastUsedTimestamp;

        // If the entry is registered, check when it was last used.
        if (cooldownRegistry.TryGetValue(entry, out lastUsedTimestamp))
        {
            // If it has been beyond the cooldown time, it's ready for use and should be removed from the registry.
            DateTime currentTime = timeProvider.Now;
            
            if (currentTime - lastUsedTimestamp >= cooldown)
            {
                cooldownRegistry.Remove(entry);
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
