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
    public static class AssemblyLoader
    {
        public static List<ModBase> LoadedMods { get; } = new();

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
        /// Loads all classes with ModBase inheritance inside a directory
        /// </summary>
        /// <param name="directory"></param>
        public static void Initialize(string directory)
        {
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
                            mod.Load();
                            LoadedMods.Add(mod);
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
            foreach(var mod in LoadedMods)
            {
                mod.Update();
            }
        }
        internal static void LateUpdate()
        {
            foreach (var mod in LoadedMods)
            {
                mod.LateUpdate();
            }
        }
    }
}
