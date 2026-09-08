using PnSAPI.BepInExPlugin;
using PnSAPI.Core;
using PnSAPI.Coroutining;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using static PnSAPI.AssetLoading.AssetLoader;

namespace PnSAPI.AssetLoading
{
    /// <summary>
    /// Helper class to load certain file types like some image and sound formats based on the ResourceType struct
    /// </summary>
    public static partial class AssetLoader
    {
        /// <summary>
        /// Loads a GIF from a resource and parses it into a struct of Unity Texture2Ds.
        /// WARNING: Call this during initialization/loading screens to prevent gameplay long loading times
        /// Change timeout if loading large files from internet or user are having timeout exceptions
        /// Uses an internal coroutine to await file load and space out decoding
        /// </summary>
        /// <param name="resource">The resource to load</param>
        /// <param name="loadTimeout">Specifies how long to wait before giving up on file load. May need to be changed if using internet download</param>
        /// <param name="onComplete">Callback to recieve loaded texture</param>
        public static void LoadGif(ResourceType resource, float loadTimeout = 10f, Action<Gif> onComplete = null)
        {
            Coroutines.Run(LoadGifCoroutine(resource, Assembly.GetCallingAssembly(), loadTimeout, onComplete));
        }
        private static IEnumerator LoadGifCoroutine(ResourceType resource, Assembly callingAssembly, float loadTimeout, Action<Gif> onComplete)
        {
            // 1. Load Bytes
            byte[] gifData = null;
            bool loaded = false;

            ByteLoader.LoadBytes(resource, (data) =>
            {
                gifData = data;
                loaded = true;
            }, callingAssembly);

            float maxTime = Time.unscaledTime + loadTimeout;
            while (!loaded)
            {
                if (Time.unscaledTime > maxTime)
                {
                    PnSAPIBridge.LogError($"File {resource.path} took too long to load.", "GifLoader");
                    onComplete?.Invoke(null);
                    yield break;
                }
                yield return null;
            }

            if (gifData == null || gifData.Length == 0)
            {
                PnSAPIBridge.LogError($"[Mod] Loaded byte data for {resource.path} was empty.", "GifLoader");
                onComplete?.Invoke(null);
                yield break;
            }

            // 2. Offload UniGif LZW parsing & delta compositing to a background thread
            Task<UniGif.DecodedFrame[]> decodeTask = Task.Run(() => UniGif.DecodeThreadSafe(gifData));

            // 3. Yield wait until background decoding finishes
            while (!decodeTask.IsCompleted)
            {
                yield return null;
            }

            if (decodeTask.IsFaulted || decodeTask.Result == null || decodeTask.Result.Length == 0)
            {
                PnSAPIBridge.LogError($"[Mod] UniGif background decode failed: {decodeTask.Exception?.InnerException?.Message}", "GifLoader");
                onComplete?.Invoke(null);
                yield break;
            }

            UniGif.DecodedFrame[] rawFrames = decodeTask.Result;

            // 4. Instantiate Texture2D objects on the Main Thread
            var textures = new List<Texture2D>(rawFrames.Length);
            var delays = new List<float>(rawFrames.Length);

            for (int i = 0; i < rawFrames.Length; i++)
            {
                var frame = rawFrames[i];

                Texture2D texture = new Texture2D(frame.width, frame.height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };

                texture.SetPixels32(frame.pixels);
                texture.Apply();

                textures.Add(texture);
                delays.Add(frame.delaySec);
            }

            onComplete?.Invoke(new Gif { Frames = textures, DelaysInSeconds = delays });
        }
    }
}
