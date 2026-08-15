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

            string fileName = $"{modInstance.Author}.{modInstance.Name}.json";
            string folderPath = Path.Combine(Paths.ConfigPath, "PnSAPI");
            string filePath = Path.Combine(folderPath, fileName);

            Directory.CreateDirectory(folderPath);

            if (!entries.ContainsKey(fileName))
            {
                entries[fileName] = new List<ConfigEntryBase>();
            }

            // Build rich JSON entries with metadata
            var jsonOutput = new Dictionary<string, object>();
            foreach (var entry in entries[fileName])
            {
                jsonOutput[entry.Identifier] = new
                {
                    Value = entry.BoxedValue,
                    DefaultValue = entry.BoxedDefaultValue,
                    Type = entry.SettingType.FullName ?? entry.SettingType.Name,
                    Description = entry.Description
                };
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(jsonOutput, options);
            File.WriteAllText(filePath, json);
        }

        internal static void LoadConfig(ModBase modInstance)
        {
            if (modInstance == null) return;

            string fileName = $"{modInstance.Author}.{modInstance.Name}.json";
            string filePath = Path.Combine(Paths.ConfigPath, "PnSAPI", fileName);

            if (!File.Exists(filePath) || !entries.ContainsKey(fileName))
                return;

            string json = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    json = File.ReadAllText(filePath);
                    break;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }

            if (string.IsNullOrEmpty(json)) return;

            bool needsRewrite = false;

            try
            {
                using var doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                foreach (var entry in entries[fileName])
                {
                    if (root.TryGetProperty(entry.Identifier, out JsonElement entryElement))
                    {
                        // 1. Type validation
                        string storedType = null;
                        if (entryElement.TryGetProperty("Type", out JsonElement typeProp))
                        {
                            storedType = typeProp.GetString();
                        }

                        string expectedFullName = entry.SettingType.FullName ?? entry.SettingType.Name;
                        string expectedName = entry.SettingType.Name;

                        bool typeMatches = !string.IsNullOrEmpty(storedType) &&
                            (storedType.Equals(expectedFullName, StringComparison.OrdinalIgnoreCase) ||
                             storedType.Equals(expectedName, StringComparison.OrdinalIgnoreCase));

                        if (!typeMatches)
                        {
                            Console.WriteLine($"[PnSAPI-ConfigError] Type mismatch in '{entry.Identifier}'! Expected '{expectedFullName}', found '{storedType}'. Resetting to default.");
                            entry.ResetToDefault();
                            needsRewrite = true;
                            continue;
                        }

                        // 2. Value validation & extraction
                        if (entryElement.TryGetProperty("Value", out JsonElement valueProp))
                        {
                            try
                            {
                                entry.BoxedValue = GetElementValue(valueProp, entry.SettingType);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[PnSAPI-ConfigError] Invalid value for '{entry.Identifier}' ({ex.Message}). Resetting to default.");
                                entry.ResetToDefault();
                                needsRewrite = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[PnSAPI-ConfigError] Missing 'Value' field in '{entry.Identifier}'. Resetting to default.");
                            entry.ResetToDefault();
                            needsRewrite = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PnSAPI-ConfigError] Corrupted JSON in '{fileName}' ({ex.Message}). Resetting all entries to default.");
                foreach (var entry in entries[fileName])
                {
                    entry.ResetToDefault();
                }
                needsRewrite = true;
            }

            // If any type mismatches, missing values, or corrupted entries occurred, rewrite a clean file back to disk
            if (needsRewrite)
            {
                SaveConfig(modInstance);
            }
        }

        private static object GetElementValue(JsonElement element, Type targetType)
        {
            if (targetType == typeof(bool)) return element.GetBoolean();
            if (targetType == typeof(int)) return element.GetInt32();
            if (targetType == typeof(float)) return element.GetSingle();
            if (targetType == typeof(double)) return element.GetDouble();
            if (targetType == typeof(string)) return element.GetString();

            return JsonSerializer.Deserialize(element.GetRawText(), targetType);
        }
        internal static ConfigEntry<T> Bind<T>(ModBase modInstance, string identifier, T defaultValue, string description = "")
        {
            if (modInstance == null) throw new ArgumentNullException(nameof(modInstance));

            string fileName = $"{modInstance.Author}.{modInstance.Name}.json";
            string filePath = Path.Combine(Paths.ConfigPath, "PnSAPI", fileName);

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

            // 4. If file exists on disk, load user values. Otherwise, save.
            if (File.Exists(filePath))
            {
                LoadConfig(modInstance);

                SaveConfig(modInstance);
            }
            else
            {
                SaveConfig(modInstance);
            }

            return newEntry;
        }
    }
}
