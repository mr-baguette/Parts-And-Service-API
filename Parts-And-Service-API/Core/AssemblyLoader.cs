using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PnSAPI.Core
{
    /// <summary>
    /// Static class used for loading api mods / assemblies
    /// </summary>
    public static class AssemblyLoader
    {
        /// <summary>
        /// A list of all currently loaded mods.
        /// </summary>
        internal static Dictionary<string, ModBase> LoadedMods { get; } = new();

        private static bool _hasLoaded = false;

        /*
         * Load order:
         * 1) Scan directory for dll files
         * 2) Load assemblies into memory
         * 3) Patch monobehaviour types for IL2CPP compatability and register them (done for user-experience)
         * 4) Load patched versions that are in memory
         * 5) Instantiate each class with ModBase inheritance
         * 6) Call Load() and let them do their thing
        */
        /// <summary>
        /// Loads all classes with ModBase inheritance inside a directory.
        /// Only intended for use at startup by BepInEx plugin.
        /// </summary>
        /// <param name="directory"></param>
        internal static void Initialize(string directory)
        {
            if (_hasLoaded) throw new InvalidOperationException("Initialize may not be called more than once per session");
            _hasLoaded = true;
            //Create If it doesn't exist
            if (!Directory.Exists(directory)) 
                Directory.CreateDirectory(directory);
            
            //Check all library files
            foreach (var file in Directory.GetFiles(directory, "*.dll"))
            {
                try
                {
                    var assembly = MonoBehaviourRegisterer.PatchAndLoadAssembly(Path.Combine(directory, file));
                    foreach(var type in assembly.GetTypes())
                    {
                        if (typeof(ModBase).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            var mod = (ModBase)Activator.CreateInstance(type);
                            BepInExPlugin.BepInExAdapter.LogInfo($"[AssemblyLoader] Loading {mod.Name} from assembly {file}");
                            mod.Setup();
                            mod.Load();
                            LoadedMods.Add(mod.Name, mod);
                        }
                    }
                }
                catch (Exception e)
                {
                    BepInExPlugin.BepInExAdapter.LogError($"Failed loading assembly {file} \n{e}");
                }
            }
        }
        internal static void Update()
        {
            foreach(var mod in LoadedMods.Values)
            {
                mod.Update();
            }
        }
        internal static void LateUpdate()
        {
            foreach (var mod in LoadedMods.Values)
            {
                mod.LateUpdate();
            }
        }
        /// <summary>
        /// Get a ModBase based on a key in the APIs loaded mod list
        /// </summary>
        /// <param name="key"></param>
        /// <returns>ModBase if found. If not, returns null</returns>
        public static ModBase GetLoadedMod(string key)
        {
            if (LoadedMods.ContainsKey(key))
                return LoadedMods[key];
            return null;
        }
        /// <summary>
        /// Get a ModBase based on a key in the APIs loaded mod list
        /// </summary>
        /// <returns>ModBase if found. If not, returns null</returns>
        public static ModBase GetLoadedMod(Type modType)
        {
            // Checks if the loaded mod's instance type inherits from or matches modType
            return LoadedMods.Values.FirstOrDefault(m => m.GetType().IsAssignableTo(modType));
        }
        /// <summary>
        /// Generic type used to get a specific mod 
        /// </summary>
        /// <returns>ModBase if found. If not, returns null</returns>
        public static  T GetLoadedMod<T>() where T : ModBase
        {
            return LoadedMods.OfType<T>().FirstOrDefault();
        }
    }
}
