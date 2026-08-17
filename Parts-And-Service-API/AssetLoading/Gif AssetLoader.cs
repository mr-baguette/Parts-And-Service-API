using PnSAPI.BepInExPlugin;
using PnSAPI.Coroutining;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using UnityEngine;

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
        /// <param name="loadTimeout">Specifies how long to waitbefore giving up on file load. May need to be changed if using internet download</param>
        /// <param name="decodesPerFrame">Specifies how many frames should be decoded every game frame. Use 1/default if you're preloading</param>
        /// <param name="onComplete">Callback to recieve loaded texture</param>
        public static void LoadGif(ResourceType resource, float loadTimeout = 10f, ushort decodesPerFrame = 1, Action<LoadedGif> onComplete = null)
        {
            Coroutines.Run(LoadGifAsync(resource, Assembly.GetCallingAssembly(), loadTimeout, decodesPerFrame, onComplete));
        }
        private static IEnumerator LoadGifAsync(ResourceType resource, Assembly callingAssembly, float loadTimeout = 10f,ushort decodesPerFrame = 1, Action<LoadedGif> onComplete = null)
        {
            BepInExAdapter.LogInfo($"[GifLoader] Starting async load for {resource.path} from {resource.loadType.ToString()}...");

            LoadedGif result = new LoadedGif
            {
                Frames = new List<GifFrame>(),
                TotalDuration = 0f
            };
            byte[] gifData = null;
            bool loaded = false;
            Action<byte[]> bytesLoaded = (data) => { gifData = data; loaded = true; };
            ByteLoader.LoadBytes(resource, bytesLoaded, callingAssembly);

            float maxTime = Time.unscaledTime + loadTimeout;
            while (!loaded) 
            { 
                yield return null; 
                if (Time.unscaledTime > maxTime) { throw new TimeoutException($"File {resource.path} took too long to load"); }
            }

            if (gifData == null || gifData.Length == 0)
            {
                BepInExAdapter.LogError($"[GifLoader] Failed to load bytes for: {resource.path}");
                yield break;
            }

            

            using (var decoder = new MG.GIF.Decoder(gifData))
            {
                var img = decoder.NextImage();
                int decodecounter = 1;

                while (img != null)
                {
                    float frameDelay = img.Delay / 1000f;

                    Texture2D tex = img.CreateTexture();
                    tex.hideFlags = HideFlags.HideAndDontSave;
                    UnityEngine.Object.DontDestroyOnLoad(tex);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;

                    result.Frames.Add(new GifFrame
                    {
                        Texture = tex,
                        Delay = frameDelay
                    });

                    result.TotalDuration += frameDelay;

                    img = decoder.NextImage();
                    decodecounter++;

                    // Pause execution here until the next frame. 
                    // This prevents the game from freezing during big GIFs
                    if (decodecounter >= decodesPerFrame) yield return null;
                }
            }
            BepInExAdapter.LogInfo($"[GifLoader] Finished async load! Frames: {result.Frames.Count}");

            // Fire the callback if someone was waiting for it to finish
            onComplete?.Invoke(result);
        }
    }
}
