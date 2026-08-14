using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace PnSAPI.Core
{
    public abstract class ModBase
    {
        public virtual string Name => GetType().Name;
        public virtual string Version => "1.0.0";
        public virtual string Author => "Unknown";
        public ManualLogSource Log { get; private set; }
        internal void Setup()
        {
            Log = BepInEx.Logging.Logger.CreateLogSource(Name);
        }
        public virtual void Load() { }
        public virtual void Unload() { }
        public virtual void Update() { }
        public virtual void LateUpdate() { }

        /// <summary>Logs a debug message to the BepInEx terminal under the APIs name. Debug needs to be enabled in BepInEx config to see.</summary>
        public void LogDebug(string message) => Log.LogDebug($"[{Name}] {message}");
        /// <summary>Logs a message to the BepInEx terminal under the APIs name. Most commonly used for telling user information about what it's doing.</summary>
        public void LogInfo(string message) => Log.LogInfo($"[{Name}] {message}");
        /// <summary>Logs a warning to the BepInEx terminal under the APIs name. Use this if something when wrong but not critical to the operation of your mod.</summary>
        public void LogWarning(string message) => Log.LogWarning($"[{Name}] {message}");
        /// <summary>Logs to the BepInEx terminal under the APIs name. Use this if something critical went wrong.</summary>
        public void LogError(string message) => Log.LogError($"[{Name}] {message}");
    }
}
