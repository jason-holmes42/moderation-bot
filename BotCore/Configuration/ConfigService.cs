using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BotCore.Core;
using BotCore.Filtering;
using BotCore.Commands;
using BotCore.Permissions;
using System.Reflection.Metadata;

namespace BotCore.Configuration;

// Responsible for loading bot-specific information from storage and saving to storage. Currently designed for simple JSON storage, but can be converted for database storage without affecting other modules.
internal class ConfigService
{
    static readonly string _configDirectoryName = "config";
    static readonly string _backupDirectoryName = "backup";

    // Create and cache the Json Serializer Options object used for all serialization/deserialization needs.
    public static readonly JsonSerializerOptions jsonOptions =
        new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }      // Converts enums from integers to strings and vice-versa.
        };

    // Generalized retrieval/storage functions to centralize error-handling, mitigate serialization/deserialization reuse, and make future replacement straightforward.
    public static async Task<T> RetrieveConfigAsync<T>(UserContext internalUser) where T : ISettingsConfig
    {
        string filepath = GetFilePath(internalUser, T.Filename);

        // Since the config files will not be especially large, we can skip StreamReader for this
        // Check if a config file exists. If not, return a value to the caller to indicate the service needs to generate one.
        if (!File.Exists(filepath))
        {
            Console.WriteLine($"File at {filepath} does not exist. Generating default config for {T.Filename}.");
            return default;
        }

        // Otherwise, collect the data from the file and deserialize it.
        try
        {
            string jsonData = await File.ReadAllTextAsync(filepath);

            if (string.IsNullOrWhiteSpace(jsonData))
            {
                Console.WriteLine($"Error: JSON loading failed. Generating default config for {T.Filename}.");
                return default;
            }

            return JsonSerializer.Deserialize<T>(jsonData, jsonOptions);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Config file {T.Filename} contains invalid JSON: {ex.Message}");
            string backupLoc = BackupFile(internalUser, filepath);
            Console.WriteLine($"Storing invalid file to {backupLoc}. Generating default config for {T.Filename}.");
            return default;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Could not read config file {T.Filename}: {ex.Message}");
            string backupLoc = BackupFile(internalUser, filepath);
            Console.WriteLine($"Storing invalid file to {backupLoc}. Generating default config for {T.Filename}.");
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error loading {T.Filename} config: {ex.Message}");
            string backupLoc = BackupFile(internalUser, filepath);
            Console.WriteLine($"Storing invalid file to {backupLoc}. Generating default config for {T.Filename}.");
            return default;
        }
    }

    public static async Task StoreConfigAsync<T>(UserContext internalUser, T config) where T : ISettingsConfig
    {
        try
        {
            // Serialize the config object to JSON. The options ensure that enums convert to legible strings and that the file includes indentation so it isn't just one long nightmare JSON string.
            string json = JsonSerializer.Serialize(config, jsonOptions);
            await File.WriteAllTextAsync(GetFilePath(internalUser, T.Filename), json);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error while serializing {T.Filename} to JSON: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error while writing {T.Filename} to file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error encountered while storing {T.Filename}: {ex.Message}");
        }
    }

    // Smooth out the process of relative filepaths and avoid having to Path.Combine everywhere
    private static string GetFilePath(UserContext internalUser, string filename)
    {
        string configDir = Path.Combine(AppContext.BaseDirectory, _configDirectoryName, internalUser.InternalUser);
        Directory.CreateDirectory(configDir);       // Safely create the directory referenced above if it does not exist.

        return Path.Combine(configDir, filename);
    }

    // Store invalid configuration files in a backup location for reference.
    private static string BackupFile(UserContext internalUser, string filepath)
    {
        // The backup directory should be a subdirectory of the config directory, so make it if it doesn't exist.
        string backupDir = Path.Combine(AppContext.BaseDirectory, _configDirectoryName, internalUser.InternalUser, _backupDirectoryName);
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
