using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore;

// Responsible for loading bot-specific information from storage and saving to storage. Currently designed for simple JSON storage, but can be converted for database storage without affecting other modules.
internal class ConfigService
{
    static string filepath = "config/";

    public static async Task<IEnumerable<FilterRule>> RetrieveFilterRules()
    {
        List<FilterRule> filterRules = new List<FilterRule>();

        // read and deserialize from JSON file

        return filterRules;
    }

    // Stub function for future Custom Commands feature
    public static async Task<IEnumerable<CustomCommand>> RetrieveCustomCommands()
    {
        List<CustomCommand> commandsList = new List<CustomCommand>();

        // read and deserialize from JSON file

        return commandsList;
    }

    // Stub function for future Permissions feature.
    public static async void RetrievePermissionsList()
    {
        // read and deserialize from JSON file
    }

    public static async Task StoreFilterRules(IEnumerable<FilterRule> filterRules)
    {
        List<FilterRule> rulesToStore = filterRules.ToList();

        // serialize and save to JSON file
    }

    // Stub function for future Custom Commands feature
    public static async Task StoreCustomCommands(IEnumerable<CustomCommand> customCommands)
    {
        List<CustomCommand> commandsToStore = customCommands.ToList();

        // serialize and save to JSON file
    }

    // Stub function for future Permissions feature.
    public static async Task StorePermissionsList()
    {
        // serialize and save to JSON file
    }
}
