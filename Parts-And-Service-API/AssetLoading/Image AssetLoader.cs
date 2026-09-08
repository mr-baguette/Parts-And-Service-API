using Mono.Cecil;
using PnSAPI.BepInExPlugin;
using PnSAPI.Core;
using PnSAPI.Coroutining;
using System.Collections;
using System.Reflection;
using UnityEngine;


namespace PnSAPI.AssetLoading
{
    public static partial class AssetLoader
    {
        /// <summary>
        /// Loads an image an any of these formats:
        /// JPEG, PNG, BMP, PSD, TGA, HDR
        /// </summary>
        /// <param name="resource">The resource where the image should be loaded from</param>
        /// <param name="loadTimeout">How long to wait before giving up on loading</param>
        /// <param name="onComplete">Callback to pass loaded image back to your code</param>
        public static void LoadImage(ResourceType resource, float loadTimeout = 10f, Action<Texture2D> onComplete = null)
        {
            Coroutines.Run(LoadImageAsync(resource, Assembly.GetCallingAssembly(), loadTimeout, onComplete));
        }
        private static IEnumerator LoadImageAsync(ResourceType resource, Assembly callingAssembly, float loadTimeout = 10f, Action<Texture2D> onComplete = null)
        {
            byte[] data = null;
            bool loaded = false;
            Action<byte[]> bytesLoaded = (fileData) => { data = fileData; loaded = true; };
            ByteLoader.LoadBytes(resource, bytesLoaded, callingAssembly);

            float maxTime = Time.unscaledTime + loadTimeout;
            while (!loaded)
            {
                yield return null;
                if (Time.unscaledTime > maxTime)
                {
                    PnSAPIBridge.LogError("Error while loading file: " + new TimeoutException($"File {resource.path} took too long to load"), "ImageLoader");
                    onComplete.Invoke(null);
                    yield break;
                }
            }

            // 2. Validate Size
            if (data.Length < 8)
            {
                PnSAPIBridge.LogError($"[AssetLoader] The file '{resource.path}' is only {data.Length} bytes long! It is corrupted or empty.", "ImageLoader");
                yield break;
            }

            // Load
            StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromMemory(data, StbImageSharp.ColorComponents.RedGreenBlueAlpha);

            // Create texture
            Texture2D tex = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false);
            tex.LoadRawTextureData(image.Data);
            tex.Apply();

            onComplete.Invoke(tex);
        }
        /// <summary>Checks a file's 'magic bytes' and returns true if it matches that of a JPG</summary>
        /// <param name="data">Image data to check</param>
        /// <returns>true if an actual Jpeg</returns>
        public static bool IsJPG(byte[] data)
        {
            string magicBytes = $"{data[0]:X2} {data[1]:X2} {data[2]:X2}";

            // A true PNG MUST start with: FF D8 FF
            if (data[0] != 0xFF || data[1] != 0xD8 || data[2] != 0xFF)
            {
                return false;
            }
            return true;
        }
    }
}
