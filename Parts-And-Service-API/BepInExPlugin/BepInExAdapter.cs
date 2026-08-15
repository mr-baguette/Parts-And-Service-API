using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using PnSAPI.Config;
using PnSAPI.Core;
using PnSAPI.Coroutining;
using System.Collections.Concurrent;
using UnityEngine;

namespace PnSAPI.BepInExPlugin
{
    [BepInPlugin("com.oui_baguette1.PnSAPI", "Parts And Services API", "0.0.1")]
    internal class BepInExAdapter : BasePlugin
    {
        private static FileSystemWatcher watcher;
        public static BepInEx.Logging.ManualLogSource ModLogger { get; private set; }
        private RuntimeUnityEvents runtimeUnityEvents;
        private static readonly ConcurrentQueue<string> _changedFiles = new();
        public override void Load()
        {
            ModLogger = BepInEx.Logging.Logger.CreateLogSource("P&S API");
            AssemblyLoader.Initialize(Path.Combine(Paths.GameRootPath, "API mods"));

            watcher = new FileSystemWatcher
            {
                Path = Path.Combine(Paths.ConfigPath, "PnSAPI"),
                Filter = "*.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true // Watcher won't fire without this!
            };
            watcher.Changed += (s, e) => _changedFiles.Enqueue(e.Name);
            watcher.Created += (s, e) => _changedFiles.Enqueue(e.Name);
            watcher.Renamed += (s, e) => _changedFiles.Enqueue(e.Name);
            watcher.EnableRaisingEvents = true;

            ClassInjector.RegisterTypeInIl2Cpp<RuntimeUnityEvents>();
            ClassInjector.RegisterTypeInIl2Cpp<Coroutines>();

            GameObject go = new("Parts And Service API");
            GameObject.DontDestroyOnLoad(go);
            runtimeUnityEvents = go.AddComponent<RuntimeUnityEvents>();
            go.AddComponent<Coroutines>();
        }
        public static void LogDebug(string msg)
        {
            ModLogger.LogDebug(msg);
        }
        public static void LogInfo(string msg)
        {
            ModLogger.LogInfo(msg);
        }
        public static void LogWarning(string msg)
        {
            ModLogger.LogWarning(msg);
        }
        public static void LogError(string msg)
        {
            ModLogger.LogError(msg);
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            // Get "Author.ModName" from "Author.ModName.json"
            string fileWithoutExt = Path.GetFileNameWithoutExtension(e.Name);

            // Extract mod name if using "Author.ModName" naming format
            string[] parts = fileWithoutExt.Split('.');
            string modName = parts.Length > 1 ? parts[1] : parts[0];

            ModBase mod = AssemblyLoader.GetLoadedMod(modName);
            if (mod != null)
            {
                LogInfo($"Reloading config for: {mod.Name}");
                ConfigurationManager.LoadConfig(mod);
            }
        }
        internal static void Update()
        {
            while (_changedFiles.TryDequeue(out string fileName))
            {
                System.Console.WriteLine($"[PnSAPI-Watcher] Main-thread received change: {fileName}");

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
    }
}
