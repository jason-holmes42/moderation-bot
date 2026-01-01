using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BotCore.Filtering;
using BotCore.Commands;
using BotCore.Permissions;
using System.Reflection.Metadata;

namespace BotCore.Configuration;

// Responsible for loading bot-specific information from storage and saving to storage. Currently designed for simple JSON storage, but can be converted for database storage without affecting other modules.
internal class ConfigService
{
    static readonly string filterConfigFilename = "filterConfig.json";
    static readonly string commandConfigFilename = "commandConfig.json";
    static readonly string permissionsConfigFilename = "permissionsConfig.json";
    static readonly string configDirectoryName = "config";
    static readonly string backupDirectoryName = "backup";

    // Create and cache the Json Serializer Options object used for all serialization/deserialization needs.
    private static readonly JsonSerializerOptions jsonOptions =
        new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }      // Converts enums from integers to strings and vice-versa.
        };

    // Service-specific retrieval and storage functions. They are split to allow for each service to make requests to ConfigService without needing to know the implementation or storage details.
    public static Task<FilterConfig> RetrieveFilterConfig()
    {
        return RetrieveConfigAsync<FilterConfig>(filterConfigFilename);
    }

    public static Task<CommandConfig> RetrieveCommandConfig()
    {
        return RetrieveConfigAsync<CommandConfig>(commandConfigFilename);
    }

    public static Task<PermissionsConfig> RetrievePermissionsConfig()
    {
        return RetrieveConfigAsync<PermissionsConfig>(permissionsConfigFilename);
    }

    public static Task StoreFilterConfig(FilterConfig filterConfig)
    {
        return StoreConfigAsync<FilterConfig>(filterConfig, filterConfigFilename);
    }

    public static Task StoreCommandConfig(CommandConfig commandConfig)
    {
        return StoreConfigAsync<CommandConfig>(commandConfig, commandConfigFilename);
    }

    public static Task StorePermissionsConfig(PermissionsConfig permissionConfig)
    {
        return StoreConfigAsync<PermissionsConfig>(permissionConfig, permissionsConfigFilename);
    }

    // Generalized retrieval/storage functions to centralize error-handling, mitigate serialization/deserialization reuse, and make future replacement straightforward.
    private static async Task<T> RetrieveConfigAsync<T>(string filename)
    {
        string filepath = GetFilePath(filename);

        // Since the config files will not be especially large, we can skip StreamReader for this
        // Check if a config file exists. If not, return a value to the caller to indicate the service needs to generate one.
        if (!File.Exists(filepath))
        {
            Console.WriteLine($"File at {filepath} does not exist. Generating default config.");
            return default;
        }

        // Otherwise, collect the data from the file and deserialize it.
        try
        {
            string jsonData = await File.ReadAllTextAsync(filepath);

            if (string.IsNullOrWhiteSpace(jsonData))
            {
                Console.WriteLine("Error: JSON loading failed. Generating default config.");
                return default;
            }

            return JsonSerializer.Deserialize<T>(jsonData, jsonOptions);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Config file {filename} contains invalid JSON: {ex.Message}");
            string backupLoc = BackupFile(filepath);
            Console.WriteLine($"Storing invalid file to {backupLoc}. Generating default config.");
            return default;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Could not read config file {filename}: {ex.Message}");
            string backupLoc = BackupFile(filepath);
            Console.WriteLine($"Storing invalid file to {backupLoc}. Generating default config.");
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error loading {filename} config: {ex.Message}");
            string backupLoc = BackupFile(filepath);
            Console.WriteLine($"Storing invalid file to {backupLoc}. Generating default config.");
            return default;
        }
    }

    private static async Task StoreConfigAsync<T>(T config, string filename)
    {
        try
        {
            // Serialize the config object to JSON. The options ensure that enums convert to legible strings and that the file includes indentation so it isn't just one long nightmare JSON string.
            string json = JsonSerializer.Serialize(config, jsonOptions);
            await File.WriteAllTextAsync(GetFilePath(filename), json);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error while serializing {filename} to JSON: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error while writing {filename} to file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error encountered while storing {filename}: {ex.Message}");
        }
    }

    // Smooth out the process of relative filepaths and avoid having to Path.Combine everywhere
    private static string GetFilePath(string filename)
    {
        string configDir = Path.Combine(AppContext.BaseDirectory, configDirectoryName);
        Directory.CreateDirectory(configDir);       // Safely create the directory referenced above if it does not exist.

        return Path.Combine(configDir, filename);
    }

    // Store invalid configuration files in a backup location for reference.
    private static string BackupFile(string filepath)
    {
        // The backup directory should be a subdirectory of the config directory, so make it if it doesn't exist.
        string backupDir = Path.Combine(AppContext.BaseDirectory, configDirectoryName, backupDirectoryName);
        Directory.CreateDirectory(backupDir);

        // Append the current datetime to the filename, add .bak, mark it for the backup directory
        string filename = Path.GetFileName(filepath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupFilename = $"{filename}.{timestamp}.bak";
        string backupFilepath = Path.Combine(backupDir, backupFilename);

        // Perform the copy
        File.Copy(filepath, backupFilepath);

        // Return the path for exception handling
        return backupFilepath;
    }
}
