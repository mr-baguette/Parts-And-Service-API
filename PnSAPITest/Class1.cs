using BepInEx;
using PnSAPI.AssetLoading;
using PnSAPI.Config;
using PnSAPI.Core;
using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using PnSAPI.Coroutining;

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
            /*ResourceType resource = new("API mods/Chica-Dance.gif", LoadType.Disk );
            AssetLoader.LoadGif(resource, onComplete: (gif) =>
            {
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from disk, and dimensions {gif.Frames[0].width}x{gif.Frames[0].height}");
            });
            resource = new("PnSAPITest.Chica-Dance.gif", LoadType.Embedded);
            AssetLoader.LoadGif(resource, onComplete: (gif) =>
            {
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from embedded resource, and dimensions {gif.Frames[0].width} x {gif.Frames[0].height}");
            });
            resource = new("https://upload.wikimedia.org/wikipedia/commons/2/2c/Rotating_earth_%28large%29.gif", LoadType.Online);
            AssetLoader.LoadGif(resource, onComplete: (gif) =>
            {
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from internet, and dimensions {gif.Frames[0].width} x {gif.Frames[0].height}");
            });*/
            Coroutines.Run(GifLoadTest());
        }
        private void Update()
        {
            //APITest.Instance.LogInfo("TestComponent Start() executed successfully!");
        }
        IEnumerator GifLoadTest()
        {
            yield return 5; //wait for main menu to show
            //disk gif
            ResourceType resource = new("API mods/Chica-Dance.gif", LoadType.Disk);
            bool loaded = false;
            Gif loadedGif = null;
            AssetLoader.LoadGif(resource, onComplete: (gif) =>
            {
                loadedGif = gif;
                loaded = true;
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from disk, and dimensions {gif.Frames[0].width}x{gif.Frames[0].height}, delay is {gif.DelaysInSeconds[0]}");
            });
            Func<bool> WaitCondition = () => loaded;
            yield return WaitCondition;
            if (loadedGif == null) APITest.Instance.LogError("Gif failed to load");
            GameObject canvasObj = new GameObject("gif stuff");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.sortingOrder = 500;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            GameObject imageObj = new("Gif");
            imageObj.transform.parent = canvasObj.transform;
            RawImage image = imageObj.AddComponent<RawImage>();
            RectTransform rect = imageObj.GetComponent<RectTransform>();

            rect.offsetMax = new Vector2(-20, -20);
            rect.offsetMin = new Vector2(20, 20);
            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            APITest.Instance.LogInfo("Showing gif");
            for (int i = 0; i < loadedGif.Frames.Count; i++)
            {
                APITest.Instance.LogInfo($"Showing frame {i} on gif");
                image.texture = loadedGif.Frames[i];
                APITest.Instance.LogInfo($"Waiting {loadedGif.DelaysInSeconds[i]} seconds for next frame");
                yield return loadedGif.DelaysInSeconds[i]; //delay
            }
            
            //embedded gif
            resource = new("PnSAPITest.Chica-Dance.gif", LoadType.Embedded);
            loaded = false;
            loadedGif = null;
            AssetLoader.LoadGif(resource, onComplete: (gif) =>
            {
                loadedGif = gif;
                loaded = true;
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from embedded resource, and dimensions {gif.Frames[0].width}x{gif.Frames[0].height}, delay is {gif.DelaysInSeconds[0]}");
            });
            yield return WaitCondition;
            for (int i = 0; i < loadedGif.Frames.Count; i++)
            {
                APITest.Instance.LogInfo($"Showing frame {i} on gif");
                image.texture = loadedGif.Frames[i];
                APITest.Instance.LogInfo($"Waiting {loadedGif.DelaysInSeconds[i]} seconds for next frame");
                yield return loadedGif.DelaysInSeconds[i]; //delay
            }

            //internet resource
            resource = new("https://upload.wikimedia.org/wikipedia/commons/2/2c/Rotating_earth_%28large%29.gif", LoadType.Online);
            loaded = false;
            loadedGif = null;
            AssetLoader.LoadGif(resource, onComplete: (gif) =>
            {
                loadedGif = gif;
                loaded = true;
                APITest.Instance.LogInfo($"Gif loaded with {gif.Frames.Count} frames from internet, and dimensions {gif.Frames[0].width}x{gif.Frames[0].height}, delay is {gif.DelaysInSeconds[0]}");
            });
            yield return WaitCondition;
            for (int i = 0; i < loadedGif.Frames.Count; i++)
            {
                APITest.Instance.LogInfo($"Showing frame {i} on gif");
                image.texture = loadedGif.Frames[i];
                APITest.Instance.LogInfo($"Waiting {loadedGif.DelaysInSeconds[i]} seconds for next frame");
                yield return loadedGif.DelaysInSeconds[i]; //delay
            }
            yield break;
        }
    }
}
