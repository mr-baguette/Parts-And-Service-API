using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using PnSAPI.Core;
using PnSAPI.Coroutining;
using PnSAPI.Config;
using UnityEngine;

namespace PnSAPI.BepInExPlugin
{
    [BepInPlugin("com.oui_baguette1.PnSAPI", "Parts And Services API", "0.0.1")]
    internal class BepInExAdapter : BasePlugin
    {
        public static BepInEx.Logging.ManualLogSource ModLogger { get; private set; }
        private RuntimeUnityEvents runtimeUnityEvents;
        public override void Load()
        {
            ModLogger = BepInEx.Logging.Logger.CreateLogSource("P&S API");
            AssemblyLoader.Initialize(Path.Combine(Paths.GameRootPath, "API mods"));

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = Path.Combine(Paths.ConfigPath, "PnSAPI");
            watcher.Changed += OnChanged;

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
            {
                return;
            }
            string file = e.Name;
            LogInfo($"Config changed: {file}");
            int start = file.IndexOf(".");
            int end = file.LastIndexOf(".");
            string ModName = file.Substring(start, end - start);
            LogInfo(ModName);
            ConfigurationManager.LoadConfig(AssemblyLoader.GetLoadedMod(ModName));
        }
    }
}
