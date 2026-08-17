using MG.GIF;
using PnSAPI.BepInExPlugin;
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
                    BepInExAdapter.LogError($"File {resource.path} took too long to load.");
                    onComplete?.Invoke(null);
                    yield break;
                }
                yield return null;
            }

            if (gifData == null || gifData.Length == 0)
            {
                BepInExAdapter.LogError($"[Mod] Loaded byte data for {resource.path} was empty.");
                onComplete?.Invoke(null);
                yield break;
            }

            // 2. Offload mgGif CPU decoding to a background thread
            Task<RawFrameData[]> decodeTask = Task.Run(() => DecodeGifBytes(gifData));

            // 3. Yield wait in the Coroutine until background thread finishes
            while (!decodeTask.IsCompleted)
            {
                yield return null;
            }

            if (decodeTask.IsFaulted || decodeTask.Result == null || decodeTask.Result.Length == 0)
            {
                BepInExAdapter.LogError($"[Mod] Failed to decode GIF: {decodeTask.Exception?.InnerException?.Message}");
                onComplete?.Invoke(null);
                yield break;
            }

            RawFrameData[] rawFrames = decodeTask.Result;

            // 4. Create Texture2D instances GUARANTEED on Unity's Main Thread
            var textures = new List<Texture2D>(rawFrames.Length);
            var delays = new List<float>(rawFrames.Length);

            for (int i = 0; i < rawFrames.Length; i++)
            {
                var frame = rawFrames[i];

                Texture2D texture = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                texture.SetPixels32(frame.Pixels);
                texture.Apply();

                textures.Add(texture);
                delays.Add(frame.DelayInSeconds);
            }

            onComplete?.Invoke(new Gif { Frames = textures, DelaysInSeconds = delays });
        }

        // Background thread helper (100% thread safe - no Unity objects used here)
        private static RawFrameData[] DecodeGifBytes(byte[] gifBytes)
        {
            using var decoder = new Decoder(gifBytes);
            var frameList = new List<RawFrameData>();

            MG.GIF.Image img;
            while ((img = decoder.NextImage()) != null)
            {
                frameList.Add(new RawFrameData
                {
                    Width = img.Width,
                    Height = img.Height,
                    DelayInSeconds = img.Delay / 1000f,
                    Pixels = img.RawImage
                });
            }

            return frameList.ToArray();
        }
    }
}
