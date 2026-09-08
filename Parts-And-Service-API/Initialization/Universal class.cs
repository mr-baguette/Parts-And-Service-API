using Il2CppInterop.Runtime.Injection;
using PnSAPI.BepInExPlugin;
using PnSAPI.Config;
using PnSAPI.Coroutining;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

namespace PnSAPI.Core
{
    internal enum LogLevel { Debug, Info, Warning, Error }

    internal static class PnSAPIBridge
    {
        private static FileSystemWatcher _watcher;
        private static readonly ConcurrentQueue<string> _changedFiles = new();

        // Loader-agnostic log delegate: (message, sourceTag, level)
        internal static Action<string, string, LogLevel> LogHandler { get; set; }

        internal static void Initialize(string gameRootPath, string configPath)
        {
            // 1. Initialize Assembly Loader
            string modsPath = Path.Combine(gameRootPath, "API mods");
            AssemblyLoader.Initialize(modsPath);

            // 2. Setup File System Watcher
            string apiConfigPath = Path.Combine(configPath, "PnSAPI");
            Directory.CreateDirectory(apiConfigPath);

            _watcher = new FileSystemWatcher
            {
                Path = apiConfigPath,
                Filter = "*.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Changed += (s, e) => _changedFiles.Enqueue(e.Name);
            _watcher.Created += (s, e) => _changedFiles.Enqueue(e.Name);
            _watcher.Renamed += (s, e) => _changedFiles.Enqueue(e.Name);

            // 3. Register IL2CPP Types
            ClassInjector.RegisterTypeInIl2Cpp<RuntimeUnityEvents>();
            ClassInjector.RegisterTypeInIl2Cpp<Coroutines>();

            // 4. Instantiate API Unity Engine Object
            GameObject go = new("Parts And Service API");
            UnityEngine.Object.DontDestroyOnLoad(go);

            go.AddComponent<RuntimeUnityEvents>();
            go.AddComponent<Coroutines>();

            LogInfo("Parts And Service API initialized successfully.", "PnSAPI");
        }

        // Call this from RuntimeUnityEvents.Update() on the main thread
        internal static void Update()
        {
            while (_changedFiles.TryDequeue(out string fileName))
            {
                LogInfo($"Main-thread received change: {fileName}", "ConfigWatcher");

                string fileWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                if (string.IsNullOrEmpty(fileWithoutExt)) continue;

                string[] parts = fileWithoutExt.Split('.');
                string modName = parts.Length > 1 ? parts[1] : parts[0];

                ModBase mod = AssemblyLoader.GetLoadedMod(modName);
                if (mod != null)
                {
                    ConfigurationManager.LoadConfig(mod);
                }
            }
        }

        #region Universal Logging Methods
        internal static void LogDebug(string msg, string source = "PnSAPI") => LogHandler?.Invoke(msg, source, LogLevel.Debug);
        internal static void LogInfo(string msg, string source = "PnSAPI") => LogHandler?.Invoke(msg, source, LogLevel.Info);
        internal static void LogWarning(string msg, string source = "PnSAPI") => LogHandler?.Invoke(msg, source, LogLevel.Warning);
        internal static void LogError(string msg, string source = "PnSAPI") => LogHandler?.Invoke(msg, source, LogLevel.Error);
        #endregion
    }
}
