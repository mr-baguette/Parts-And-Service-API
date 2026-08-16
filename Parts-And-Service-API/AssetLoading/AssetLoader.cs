using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using PnSAPI.BepInExPlugin;
using MG.GIF;
using PnSAPI.Coroutining;

namespace PnSAPI.AssetLoading
{
    public static partial class AssetLoader
    {
        /// <summary>
        /// Loads a GIF from a resource and parses it into a struct of Unity Texture2Ds.
        /// WARNING: Call this during initialization/loading screens to prevent gameplay long loading times or stuttering!
        /// Change timeout if loading large files from internet or user are having timeout exceptions
        /// </summary>
        public static void LoadGif(ResourceType resource, float timeout = 10f, Action<LoadedGif> onComplete = null)
        {
            Coroutines.Run(LoadGifAsync(resource, Assembly.GetCallingAssembly(), onComplete, timeout));
        }
        private static IEnumerator LoadGifAsync(ResourceType resource, Assembly callingAssembly, Action<LoadedGif> onComplete = null, float timeout = 10f)
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

            float maxTime = Time.unscaledTime + 10f;
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

                    // THE MAGIC TRICK: 
                    // Pause execution here until the next frame. 
                    // This prevents the game from freezing during big GIFs!
                    yield return null;
                }
            }
            BepInExAdapter.LogInfo($"[GifLoader] Finished async load! Frames: {result.Frames.Count}");

            // Fire the callback if someone was waiting for it to finish
            onComplete?.Invoke(result);
        }
    }
}
