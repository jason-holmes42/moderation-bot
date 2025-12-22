using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore;

// Responsible for loading bot-specific information from storage and saving to storage. Currently designed for simple JSON storage, but can be converted for database storage without affecting other modules.
internal class ConfigService
{
    string filepath = "config/";

    public IEnumerable<FilterRule> RetrieveFilterRules()
    {
        List<FilterRule> filterRules = new List<FilterRule>();

        // read and deserialize from JSON file

        return filterRules;
    }

    // Stub function for future Custom Commands feature
    public IEnumerable<CustomCommand> RetrieveCustomCommands()
    {
        List<CustomCommand> commandsList = new List<CustomCommand>();

        // read and deserialize from JSON file

        return commandsList;
    }

    // Stub function for future Permissions feature.
    public void RetrievePermissionsList()
    {
        // read and deserialize from JSON file
    }

    public void StoreFilterRules(IEnumerable<FilterRule> filterRules)
    {
        List<FilterRule> rulesToStore = filterRules.ToList();

        // serialize and save to JSON file
    }

    // Stub function for future Custom Commands feature
    public void StoreCustomCommands(IEnumerable<CustomCommand> customCommands)
    {
        List<CustomCommand> commandsToStore = customCommands.ToList();

        // serialize and save to JSON file
    }

    // Stub function for future Permissions feature.
    public void StorePermissionsList()
    {
        // serialize and save to JSON file
    }
}
