using Mono.Cecil;
using PnSAPI.BepInExPlugin;
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
        /// <param name="resource"></param>
        /// <param name="loadTimeout"></param>
        /// <param name="decodesPerFrame"></param>
        /// <param name="onComplete"></param>
        public static void LoadImage(ResourceType resource, float loadTimeout = 10f, ushort decodesPerFrame = 1, Action<Texture2D> onComplete = null)
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
                if (Time.unscaledTime > maxTime) { throw new TimeoutException($"File {resource.path} took too long to load"); }
            }

            // 2. Validate Size
            if (data.Length < 8)
            {
                BepInExAdapter.LogError($"[AssetLoader] The file '{resource.path}' is only {data.Length} bytes long! It is corrupted or empty.");
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
