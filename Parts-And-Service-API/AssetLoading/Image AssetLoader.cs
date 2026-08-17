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
        /// Loads a 24-bit or 32-bit BMP file as a Texture2D. Uses an internal coroutine to await file load.
        /// </summary>
        public static void LoadBmp(ResourceType resource, float loadTimeout = 10f, ushort decodesPerFrame = 1, Action<Texture2D> onComplete = null)
        {
            Coroutines.Run(LoadBmpAsync(resource, Assembly.GetCallingAssembly(), loadTimeout, onComplete));
        }
        private static IEnumerator LoadBmpAsync(ResourceType resource, Assembly callingAssembly, float loadTimeout = 10f, Action<Texture2D> onComplete = null)
        {
            byte[] fileData = null;
            bool loaded = false;
            Action<byte[]> bytesLoaded = (data) => { fileData = data; loaded = true; };
            ByteLoader.LoadBytes(resource, bytesLoaded, callingAssembly);

            float maxTime = Time.unscaledTime + loadTimeout;
            while (!loaded)
            {
                yield return null;
                if (Time.unscaledTime > maxTime) { throw new TimeoutException($"File {resource.path} took too long to load"); }
            }

            if (fileData == null) yield break;

            // Validate BMP Magic Number ("BM")
            if (fileData[0] != 0x42 || fileData[1] != 0x4D)
            {
                BepInExAdapter.LogError($"[AssetLoader] File '{resource.path}' is not a valid BMP.");
                yield break;
            }

            int dataOffset = BitConverter.ToInt32(fileData, 10);
            int width = BitConverter.ToInt32(fileData, 18);
            int height = BitConverter.ToInt32(fileData, 22);
            short bitsPerPixel = BitConverter.ToInt16(fileData, 28);

            if (bitsPerPixel != 24 && bitsPerPixel != 32)
            {
                BepInExAdapter.LogError($"[AssetLoader] Unsupported BMP bit depth: {bitsPerPixel}. Please use 24-bit or 32-bit BMP.");
                yield break;
            }

            int rowLength = Mathf.FloorToInt((bitsPerPixel * width + 31) / 32) * 4;
            int bytesPerPixel = bitsPerPixel / 8;

            Color32[] pixels = new Color32[width * height];

            // BMP stores pixels bottom-to-top, matching Unity's texture space
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = dataOffset + (y * rowLength) + (x * bytesPerPixel);

                    byte b = fileData[index];
                    byte g = fileData[index + 1];
                    byte r = fileData[index + 2];
                    byte a = (bytesPerPixel == 4) ? fileData[index + 3] : (byte)255;

                    pixels[y * width + x] = new Color32(r, g, b, a);
                }
            }


            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();

            BepInExAdapter.LogInfo($"[AssetLoader] Successfully loaded BMP: '{resource.path}' ({width}x{height})");
            onComplete.Invoke(tex);
        }
        /// <summary>
        /// Loads a PNG file as a 2D texture. Uses an internal coroutine to await file load.
        /// </summary>
        /// <returns></returns>
        public static void LoadPng(ResourceType resource, float loadTimeout = 10f, ushort decodesPerFrame = 1, Action<Texture2D> onComplete = null)
        {
            Coroutines.Run(LoadPngAsync(resource, Assembly.GetCallingAssembly(), loadTimeout, onComplete));
        }
        private static IEnumerator LoadPngAsync(ResourceType resource, Assembly callingAssembly, float loadTimeout = 10f, Action<Texture2D> onComplete = null)
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

            // 3. Print File Signature
            string magicBytes = $"{data[0]:X2} {data[1]:X2} {data[2]:X2} {data[3]:X2}";

            // A true PNG MUST start with: 89 50 4E 47
            if (data[0] != 0x89 || data[1] != 0x50 || data[2] != 0x4E || data[3] != 0x47)
            {
                BepInExAdapter.LogError($"[AssetLoader] '{resource.path}' is NOT a PNG! Its header is {magicBytes}.");
                yield break; // Stop here before StbImageSharp crashes
            }

            // Load
            StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromMemory(data, StbImageSharp.ColorComponents.RedGreenBlueAlpha);

            // Create texture
            Texture2D tex = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false);
            tex.LoadRawTextureData(image.Data);
            tex.Apply();

            onComplete.Invoke(tex);
        }
    }
}
