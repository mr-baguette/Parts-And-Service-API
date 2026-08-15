using BepInEx;
using System.Text.Json;
using PnSAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnSAPI.Config
{
    internal static class ConfigurationManager
    {
        // Maps a Mod's file name to its list of config entries
        internal static Dictionary<string, List<ConfigEntryBase>> entries = new();

        internal static ConfigEntry<T> GetEntry<T>(ModBase modInstance, string identifier)
        {
            List<ConfigEntryBase> configEntries = entries[$"{modInstance.Author}.{modInstance.Name}.json"];
            foreach (var entry in configEntries)
            {
                if (entry.Identifier == identifier && entry is ConfigEntry<T> typedEntry)
                {
                    return typedEntry;
                }
            }
            return null;
        }
        internal static ConfigEntry<T> GetEntry<T>(List<ConfigEntryBase> configEntries, string identifier)
        {
            foreach (var entry in configEntries)
            {
                if (entry.Identifier == identifier && entry is ConfigEntry<T> typedEntry)
                {
                    return typedEntry;
                }
            }
            return null;
        }

        internal static void SaveConfig(ModBase modInstance)
        {
            if (modInstance == null) return;

            // Generate the file name (e.g., "Author.ModName.json")
            // Note: Adjust the property names based on how your ModBase stores its metadata
            string fileName = $"{modInstance.Author}.{modInstance.Name}.json";
            string folderPath = Path.Combine(Paths.ConfigPath, "PnSAPI");
            string filePath = Path.Combine(folderPath, fileName);

            Directory.CreateDirectory(folderPath);

            // Ensure we have entries tracked for this file
            if (!entries.ContainsKey(fileName))
            {
                entries[fileName] = new List<ConfigEntryBase>();
            }

            // Create a temporary dictionary just for serialization to make the JSON look clean
            var jsonOutput = new Dictionary<string, object>();
            foreach (var entry in entries[fileName])
            {
                jsonOutput[entry.Identifier] = entry.BoxedValue;
            }

            // Write to disk
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(jsonOutput, options);

            File.WriteAllText(filePath, json);
        }

        // Optional: Call this when a mod initializes to load their settings from disk
        internal static void LoadConfig(ModBase modInstance)
        {
            string fileName = $"{modInstance.Author}.{modInstance.Name}.json";
            string filePath = Path.Combine(Paths.ConfigPath, "PnSAPI", fileName);

            if (!File.Exists(filePath) || !entries.ContainsKey(fileName))
                return;

            string json = File.ReadAllText(filePath);
            var savedData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (savedData == null) return;

            // Apply saved values back to the tracked entries
            foreach (var entry in entries[fileName])
            {
                if (savedData.TryGetValue(entry.Identifier, out JsonElement savedValue))
                {
                    try
                    {
                        // Convert the raw JSON object back into the correct C# type
                        entry.BoxedValue = Convert.ChangeType(savedValue, entry.SettingType);
                    }
                    catch (Exception ex)
                    {
                        // Handle type mismatch if the user messed up the JSON manually
                        Console.WriteLine($"[PnSAPI] Failed to load config '{entry.Identifier}': {ex.Message}");
                    }
                }
            }
        }
        internal static ConfigEntry<T> Bind<T>(ModBase modInstance, string identifier, T defaultValue, string description = "")
        {
            if (modInstance == null) throw new ArgumentNullException(nameof(modInstance));

            string fileName = $"{modInstance.Author}.{modInstance.Name}.json";

            // 1. Ensure this mod has a list in our dictionary
            if (!entries.ContainsKey(fileName))
            {
                entries[fileName] = new List<ConfigEntryBase>();
            }

            // 2. If the mod already bound this exact setting, just return it
            var existingEntry = GetEntry<T>(entries[fileName], identifier);
            if (existingEntry != null)
            {
                return existingEntry;
            }

            // 3. Create the new entry with the default value
            var newEntry = new ConfigEntry<T>(modInstance, identifier, defaultValue, description);
            entries[fileName].Add(newEntry);

            // 4. Load the config from disk to overwrite the default value if the user changed it in the JSON file
            LoadConfig(modInstance);

            // 5. Save the config to disk. This ensures that if it's a brand new setting, 
            // it gets written to the JSON file so the user can see it and edit it later.
            SaveConfig(modInstance);

            return newEntry;
        }
    }
}
