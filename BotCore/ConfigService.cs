using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BotCore;

// Responsible for loading bot-specific information from storage and saving to storage. Currently designed for simple JSON storage, but can be converted for database storage without affecting other modules.
internal class ConfigService
{
    static readonly string filterRulesFilename = "filterRules.json";
    static readonly string customCommandsFilename = "customCommands.json";
    static readonly string defaultSettingsFilename = "defaultSettings.json";
    static readonly string configDirectoryName = "config";

    // Create and cache the Json Serializer Options object used for all serialization/deserialization needs.
    private static readonly JsonSerializerOptions jsonOptions =
        new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }      // Converts enums from integers to strings and vice-versa.
        };

    public static async Task<IEnumerable<FilterRule>> RetrieveFilterRules()
    {
        List<FilterRule> filterRules = new List<FilterRule>();

        // read and deserialize from JSON file. Since the config files will not be especially large, we can skip StreamReader for this.
        string jsonData = File.ReadAllText(GetFilePath(filterRulesFilename));
        // TODO: Deserialization error testing, try/catch
        if (jsonData == null)
        {
            Console.WriteLine("JSON deserialization failed; object not populated.");
            return null;
        }

        // Deserialize from JSON into a List of FilterRule objects. Options ensure that the reactionType strings from the JSON convert properly.
        filterRules = JsonSerializer.Deserialize<List<FilterRule>>(jsonData, jsonOptions);

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

    public static async Task<int> RetrieveDefaultSettings(string setting, int defaultValue)
    {
        // read and deserialize from JSON file
    }

    public static async Task StoreFilterRules(IEnumerable<FilterRule> filterRules)
    {
        List<FilterRule> rulesToStore = filterRules.ToList();

        // Serialize the List of FilterRule objects to JSON. The options ensure both that the reactionType enum converts to legible strings and that the file includes indentation so it isn't just one long nightmare JSON string.
        string json = JsonSerializer.Serialize(rulesToStore, jsonOptions);
        File.WriteAllText(GetFilePath(filterRulesFilename), json);
        Console.WriteLine($"Saved filter rules to {GetFilePath(filterRulesFilename)}");

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

    public static async Task StoreDefaultSettings(string setting, int updateValue)
    {
        // serialize and save to JSON file
    }

    // Smooth out the process of relative filepaths and avoid having to Path.Combine in every function.
    private static string GetFilePath(string filename)
    {
        string configDir = Path.Combine(AppContext.BaseDirectory, configDirectoryName);
        Directory.CreateDirectory(configDir);       // Safely create the directory referenced above if it does not exist.

        return Path.Combine(configDir, filename);
    }
}
