using BepInEx;
using PnSAPI.AssetLoading;
using PnSAPI.Config;
using PnSAPI.Core;
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
                endTime = Time.timeAsDouble + 3;
            }
        }
    }
    public class TestComponent : UnityEngine.MonoBehaviour
    {
        // The injected (IntPtr ptr) constructor will be called behind the scenes by IL2CPP!
        private void Start()
        {
            ResourceType resource = new("API mods/Chica-Dance.gif", LoadType.Disk );
            AssetLoader.LoadGifAsync(resource, onComplete: (gif) =>
            {
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from disk, and dimensions {gif.Frames[0].Texture.width}x{gif.Frames[0].Texture.height}");
            });
            resource = new("PnSAPITest.Chica-Dance.gif", LoadType.Embedded);
            AssetLoader.LoadGifAsync(resource, onComplete: (gif) =>
            {
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from embedded resource, and dimensions {gif.Frames[0].Texture.width}x{gif.Frames[0].Texture.height}");
            });
            resource = new("https://upload.wikimedia.org/wikipedia/commons/2/2c/Rotating_earth_%28large%29.gif", LoadType.Online);
            AssetLoader.LoadGifAsync(resource, onComplete: (gif) =>
            {
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from internet, and dimensions {gif.Frames[0].Texture.width}x{gif.Frames[0].Texture.height}");
            });
        }
        private void Update()
        {
            //APITest.Instance.LogInfo("TestComponent Start() executed successfully!");
        }
    }
}
