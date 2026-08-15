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
        ConfigEntry<bool> entry;
        public override void Load()
        {
            Instance = this;

            entry = BindConfig<bool>("test", false, "Testing to see if config works");

            var go = new UnityEngine.GameObject("FrameworkTestObject");
            go.AddComponent<TestComponent>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
        public override void Update()
        {
            //LogInfo("Update called");
            LogInfo($"Value is {entry.Get()}");
        }
    }
    public class TestComponent : UnityEngine.MonoBehaviour
    {
        // The injected (IntPtr ptr) constructor will be called behind the scenes by IL2CPP!
        private void Update()
        {
            APITest.Instance.LogInfo("TestComponent Start() executed successfully!");
        }
    }
}
