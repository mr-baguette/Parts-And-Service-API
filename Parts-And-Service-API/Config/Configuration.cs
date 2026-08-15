using BepInEx.Unity.IL2CPP;
using PnSAPI.Core;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static UnityEngine.UI.GridLayoutGroup;

namespace PnSAPI.Config
{
    /// <summary>
    /// Class to create config in json format
    /// </summary>
    public class ConfigEntry<T> : ConfigEntryBase
    {
        /// <summary>Loaded value.</summary>
        public T Value { get; private set; } // Changed to public getter so users can read it easily
        public T DefaultValue { get; private set; }

        internal override object BoxedValue
        {
            get => Value;
            set => Value = (T)value;
        }
        internal override object BoxedDefaultValue => DefaultValue;
        public override void ResetToDefault()
        {
            Value = DefaultValue;
        }
        internal ConfigEntry(ModBase owner, string identifier, T defaultValue, string description = "")
        {
            Owner = owner;
            Identifier = identifier;
            Description = description;
            Value = defaultValue;
            SettingType = typeof(T);
        }
        /// <summary>Gets the configuration value.</summary>
        /// <param name="forceDiskRead">Forces the loader to read from file. Not reccomended to use, as it blocks up main thread and is handled automatically using system events.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public T Get(bool forceDiskRead = false)
        {
            if (forceDiskRead && Owner != null)
            {
                ConfigurationManager.LoadConfig(Owner);
            }

            return Value;
        }
        /// <summary>Saves value to disk.</summary>
        /// <param name="value">Value to save</param>
        public void Set(T value)
        {
            Value = value;

            if (Owner != null)
            {
                ConfigurationManager.SaveConfig(Owner);
            }
        }
    }

#pragma warning disable CS1591
    public abstract class ConfigEntryBase
    {
        public ModBase Owner { get; protected set; }
        /// <summary>
        /// Used for Identifying the configuration entry. Multiple instances of this class with the same Identifier will read the same value, but might conflict on other values.
        /// Not reccomended to use same identifier more than once.
        /// </summary>
        public string Identifier { get; protected set; }

        /// <summary>Description that is used so users can see what this value does.</summary>
        public string Description { get; protected set; }

        public Type SettingType { get; protected set; }

        // Untyped getter/setter for serialization and file saving
        internal abstract object BoxedValue { get; set; }
        internal abstract object BoxedDefaultValue { get; }
        public abstract void ResetToDefault();
    }
#pragma warning restore CS1591
    /// <summary>helper to get mod who called this</summary>
    internal static class ModResolver
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ModBase ResolveCallingMod()
        {
            var stackTrace = new StackTrace();

            foreach (StackFrame frame in stackTrace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                Type declaringType = method?.DeclaringType;

                if (declaringType == null || declaringType.Assembly == typeof(ModResolver).Assembly)
                    continue; // Skip internal PnSAPI frames

                if (typeof(ModBase).IsAssignableFrom(declaringType))
                {
                    return AssemblyLoader.GetLoadedMod(declaringType);
                }
            }

            throw new TypeLoadException("Executing mod not found in call stack while resolving configuration.");
        }
    }
}
