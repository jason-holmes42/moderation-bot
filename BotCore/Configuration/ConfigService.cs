using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BotCore.Filtering;
using BotCore.Commands;

namespace BotCore.Configuration;

// Responsible for loading bot-specific information from storage and saving to storage. Currently designed for simple JSON storage, but can be converted for database storage without affecting other modules.
internal class ConfigService
{
    Dictionary<string, int> defaultSettings;

    static readonly string filterConfigFilename = "filterConfig.json";
    static readonly string commandConfigFilename = "commandConfig.json";
    static readonly string configDirectoryName = "config";

    // Create and cache the Json Serializer Options object used for all serialization/deserialization needs.
    private static readonly JsonSerializerOptions jsonOptions =
        new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }      // Converts enums from integers to strings and vice-versa.
        };

    public static async Task<FilterConfig> RetrieveFilterConfig()
    {
        FilterConfig config = new FilterConfig();

        // read and deserialize from JSON file
        string jsonData = GetJSONData(filterConfigFilename);

        if (jsonData == null) return null;

        // Deserialize from JSON into a List of FilterRule objects. Options ensure that the reactionType strings from the JSON convert properly.
        config = JsonSerializer.Deserialize<FilterConfig>(jsonData, jsonOptions);

        return config;
    }

    // Stub function for future Custom Commands feature
    public static async Task<IEnumerable<CustomCommand>> RetrieveCommandConfig()
    {
        List<CustomCommand> commandsList = new List<CustomCommand>();

        // read and deserialize from JSON file

        return commandsList;
    }

    // Stub function for future Permissions feature.
    public static async void RetrievePermissionsConfig()
    {
        // read and deserialize from JSON file
    }

    public static async Task StoreFilterConfig(FilterConfig filterConfig)
    {
        // List<FilterRule> rulesToStore = filterConfig.filterRules.ToList();

        // Serialize the List of FilterRule objects to JSON. The options ensure both that the reactionType enum converts to legible strings and that the file includes indentation so it isn't just one long nightmare JSON string.
        string json = JsonSerializer.Serialize(filterConfig, jsonOptions);
        File.WriteAllText(GetFilePath(filterConfigFilename), json);

        // serialize and save to JSON file
    }

    // Stub function for future Custom Commands feature
    public static async Task StoreCommandConfig(IEnumerable<CustomCommand> customCommands)
    {
        List<CustomCommand> commandsToStore = customCommands.ToList();

        // serialize and save to JSON file
    }

    // Stub function for future Permissions feature.
    public static async Task StorePermissionsConfig()
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

    private static string GetJSONData(string filename)
    {
        // Since the config files will not be especially large, we can skip StreamReader for this
        if (!File.Exists(GetFilePath(filename)))
        {
            Console.WriteLine($"File at {GetFilePath(filename)} does not exist. Creating default file.");
            File.AppendAllText(GetFilePath(filename), """[]""");
            return """[]""";
        }
        
        string jsonData = File.ReadAllText(GetFilePath(filename));
        // TODO: Deserialization error testing, try/catch
        if (jsonData == null | jsonData == "")
        {
            Console.WriteLine("JSON deserialization failed; object not populated.");
            return null;
        }

        return jsonData;
    }
}
