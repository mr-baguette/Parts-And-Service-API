using BepInEx.Logging;
using PnSAPI.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace PnSAPI.Core
{
    /// <summary>
    /// Base class used for creating a mod with the Parts And Service API. Inherit from this to start modding!
    /// </summary>
    public abstract class ModBase
    {
        /// <summary>
        /// Name of the mod. Used for logging and mod discovery. Once set, shouldn't be changed in future updates.
        /// Is by default the name of the class.
        /// </summary>
        public virtual string Name => GetType().Name;
        /// <summary>Version number</summary>
        public virtual string Version => "1.0.0";
        /// <summary>
        /// Displayed to show who the mod was created by. Shouldn't be changed in future updates.
        /// Is by default unknown.
        /// </summary>
        public virtual string Author => "Unknown";
        /// <summary>BepInEx log source for the mod</summary>
        public ManualLogSource Log { get; private set; }
        internal void Setup()
        {
            Log = BepInEx.Logging.Logger.CreateLogSource(Name);
        }
        /// <summary>
        /// Override this to set up load logic. Executed in BepInEx's loading stage
        /// </summary>
        public virtual void Load() { }
        /// <summary>
        /// Used for cleanup and saving logic. Executed in BepInEx's unload stage.
        /// </summary>
        public virtual void Unload() { }
        /// <summary>
        /// Override to get access to frame updates. Called by Unity's message system, and executes once before every frame. See unity docs: 
        /// <seealso href="http://docs.unity3d.com/6000.4/Documentation/ScriptReference/MonoBehaviour.html/">Monobehaviour.Update()</seealso>
        /// </summary>
        public virtual void Update() { }
        /// <summary>
        /// Override to get access to late updates. Called by Unity's message system, and executes once before every frame, but after the all Update() methods have finished.See unity docs: 
        /// <seealso href="https://docs.unity3d.com/6000.4/Documentation/ScriptReference/MonoBehaviour.LateUpdate.html">Monobehaviour.Update()</seealso>
        /// </summary>
        public virtual void LateUpdate() { }

        /// <summary>Logs a debug message to the BepInEx terminal under the APIs name. Debug needs to be enabled in BepInEx config to see.</summary>
        public void LogDebug(string message) => Log.LogDebug($"[{Name}] {message}");
        /// <summary>Logs a message to the BepInEx terminal under the APIs name. Most commonly used for telling user information about what it's doing.</summary>
        public void LogInfo(string message) => Log.LogInfo($"[{Name}] {message}");
        /// <summary>Logs a warning to the BepInEx terminal under the APIs name. Use this if something when wrong but not critical to the operation of your mod.</summary>
        public void LogWarning(string message) => Log.LogWarning($"[{Name}] {message}");
        /// <summary>Logs to the BepInEx terminal under the APIs name. Use this if something critical went wrong.</summary>
        public void LogError(string message) => Log.LogError($"[{Name}] {message}");

        /// <summary>
        /// Binds a ConfigEntry class to its respective entry in the config files
        /// </summary>
        /// <typeparam name="T">The data type you want to store.</typeparam>
        /// <param name="identifier">Value used to identify which entry this belongs to.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="description">What the users see when editing config.</param>
        /// <returns></returns>
        public ConfigEntry<T> BindConfig<T>(string identifier, T defaultValue, string description = "")
        {
            return ConfigurationManager.Bind(this, identifier, defaultValue, description);
        }
    }
}
