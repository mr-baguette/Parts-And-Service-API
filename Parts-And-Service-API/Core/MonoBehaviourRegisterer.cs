using BepInEx;
using Il2CppInterop.Runtime.Injection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using PnSAPI.Core;

// Alias OpCodes just to be safe
using OpCodes = Mono.Cecil.Cil.OpCodes;

internal static class MonoBehaviourRegisterer
{
    /// <summary>
    /// Patches monobehaviour types so they have an IntPtr constructor for Il2cpp and then load assemblies
    /// </summary>
    internal static System.Reflection.Assembly PatchAndLoadAssembly(string filePath)
    {
        byte[] dllBytes = File.ReadAllBytes(filePath);

        using (var stream = new MemoryStream(dllBytes))
        using (var assemblyDef = AssemblyDefinition.ReadAssembly(stream))
        {
            bool modified = false;
            var module = assemblyDef.MainModule;

            var monoBehaviourTypes = module.Types.Where(t =>
                t.IsClass && !t.IsAbstract && IsSubclassOfMonoBehaviour(t)).ToList();

            foreach (var typeDef in monoBehaviourTypes)
            {
                if (!HasIntPtrConstructor(typeDef))
                {
                    InjectIntPtrConstructor(module, typeDef);
                    modified = true;
                }
            }

            if (modified)
            {
                using (var outStream = new MemoryStream())
                {
                    assemblyDef.Write(outStream);
                    dllBytes = outStream.ToArray(); // Overwrite byte array with patched bytes
                }
            }
        }

        // 1. Load patched bytes directly from RAM
        var loadedAssembly = System.Reflection.Assembly.Load(dllBytes);

        // 2. THE MISSING STEP: Register all MonoBehaviours with IL2CPP!
        foreach (var type in loadedAssembly.GetTypes())
        {
            if (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type) && !type.IsAbstract)
            {
                ClassInjector.RegisterTypeInIl2Cpp(type);
                // Log it so you can verify it's happening
                PnSAPIBridge.LogInfo($"Registered {type.Name} in IL2CPP Domain.", "MonoBehaviourRegisterer");
            }
        }

        return loadedAssembly;
    }

    private static bool IsSubclassOfMonoBehaviour(TypeDefinition type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.FullName == "UnityEngine.MonoBehaviour") return true;
            try { current = current.Resolve()?.BaseType; }
            catch { break; }
        }
        return false;
    }

    private static bool HasIntPtrConstructor(TypeDefinition type)
    {
        return type.Methods.Any(m => m.IsConstructor &&
            m.Parameters.Count == 1 &&
            m.Parameters[0].ParameterType.FullName == "System.IntPtr");
    }

    private static void InjectIntPtrConstructor(ModuleDefinition module, TypeDefinition type)
    {
        TypeReference intPtrRef = module.ImportReference(typeof(IntPtr));
        TypeReference monoBehaviourRef = module.ImportReference(typeof(UnityEngine.MonoBehaviour));

        MethodDefinition ctor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);

        ctor.Parameters.Add(new ParameterDefinition("ptr", ParameterAttributes.None, intPtrRef));

        MethodReference parentCtor = new MethodReference(".ctor", module.TypeSystem.Void, monoBehaviourRef)
        {
            HasThis = true
        };
        parentCtor.Parameters.Add(new ParameterDefinition("ptr", ParameterAttributes.None, intPtrRef));

        ILProcessor il = ctor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, parentCtor);
        il.Emit(OpCodes.Ret);

        type.Methods.Add(ctor);
    }
}