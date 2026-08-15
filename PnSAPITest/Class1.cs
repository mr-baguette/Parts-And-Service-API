using BepInEx;
using PnSAPI.Core;
using PnSAPI.Config;
using UnityEngine;

namespace PnSAPITest
{
    public class APITest : ModBase
    {
        public static APITest Instance;
        public override string Name => "API test";
        public override string Author => "BurgerLover17";
        ConfigEntry<bool> entryBool;
        ConfigEntry<int> entryInt;
        ConfigEntry<float> entryFloat;
        public override void Load()
        {
            Instance = this;

            entryBool = BindConfig<bool>("testBool", false, "Testing to see if config works");
            entryInt = BindConfig<int>("testInt", 0, "Testing to see if config works for integers");
            entryFloat = BindConfig<float>("testFloat", 0f, "Testing to see if config works for floats");

            var go = new UnityEngine.GameObject("FrameworkTestObject");
            go.AddComponent<TestComponent>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
        double endTime = 0;
        public override void Update()
        {
            if (Time.timeAsDouble > endTime)
            {
                LogInfo($"Value bool is {entryBool.Get()}");
                LogInfo($"Value int is {entryInt.Get()}");
                LogInfo($"Value float is {entryFloat.Get()}");
                endTime = Time.timeAsDouble + 1;
            }
        }
    }
    public class TestComponent : UnityEngine.MonoBehaviour
    {
        // The injected (IntPtr ptr) constructor will be called behind the scenes by IL2CPP!
        private void Update()
        {
            //APITest.Instance.LogInfo("TestComponent Start() executed successfully!");
        }
    }
}
