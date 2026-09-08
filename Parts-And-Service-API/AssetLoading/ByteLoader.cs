using PnSAPI.BepInExPlugin;
using PnSAPI.Core;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace PnSAPI.AssetLoading
{
    /// <summary>Class that loads a file's binaries</summary>
    public static class ByteLoader
    {
        /// <summary>
        /// Loads bytes from ResourceType
        /// </summary>
        /// <param name="type">The resource to load</param>
        /// <param name="callback">Action to call upon succesful byte load</param>
        /// <param name="callingAssembly">Must pass the assembly that calls it for embedded resource loading. Can be ignored for disk or web loading</param>
        public static void LoadBytes(ResourceType type, Action<byte[]> callback, Assembly callingAssembly)
        {
            switch (type.loadType)
            {
                case LoadType.Embedded:
                    callback.Invoke(GetResourceBytesFromEmbedded(type.path, callingAssembly));
                    break;
                case LoadType.Disk:
                    GetResourceBytesFromDisk(type.path, callback);
                    break;
                case LoadType.Online:
                    GetResourceBytesFromUrl(type.path, callback);
                    break;
            }
        }
        /// <summary>Loads an embedded file from an assembly.</summary>
        /// <returns>File Data in byte array format</returns>
        public static byte[] GetResourceBytesFromEmbedded(string resourceName, Assembly assembly)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    PnSAPIBridge.LogError($"Could not find embedded resource: '{resourceName}'", "AssetLoader");
                    return null;
                }

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return data;
            }
        }
        /// <summary>
        /// Reads byte data from file on disk using the specified path
        /// The path must be relative to the game's root folder (and using "/" instead of "\")
        /// </summary>
        /// <exception cref="ArgumentException"> File contains invalid characters</exception>
        /// <exception cref="FileLoadException"> File had a length of 0 or returned null</exception>
        public static async void GetResourceBytesFromDisk(string filePath, Action<byte[]> onSuccess)
        {
            char[] invalidChars = Path.GetInvalidPathChars();
            foreach (char c in invalidChars)
            {
                if(filePath.Contains(c)) throw new ArgumentException($"File path contains invalid character {c}");
            }
            byte[] data = await File.ReadAllBytesAsync(filePath);
            if (data == null || data.Length == 0)
            {
                throw new FileLoadException($"[AssetLoader] File has no data or failed to load: '{filePath}'");
            }
            MainThreadExecutionQueue.Enqueue(() => onSuccess?.Invoke(data));
        }
        private static readonly HttpClient Client = new HttpClient();

        private static readonly ConcurrentQueue<Action> MainThreadExecutionQueue = new ConcurrentQueue<Action>();
        /// <summary>
        /// Reads file from internet url asynchronously. Will dump raw data so if not a file, so could look like garbage
        /// </summary>
        /// <param name="url">Link to the resource to be loaded</param>
        /// <param name="onSuccess">Callback to return data once bytes are loaded</param>
        /// <param name="onError">Callback for when an error occurs and can't continue</param>
        public static async void GetResourceBytesFromUrl(string url, Action<byte[]> onSuccess, Action<Exception> onError = null)
        {
            try
            {
                Client.DefaultRequestHeaders.UserAgent.ParseAdd("Parts and Service API/1.0");
                byte[] bytes = await Client.GetByteArrayAsync(url);
                onSuccess?.Invoke(bytes);
            }
            catch (Exception ex)
            {
                // Safely pass the exception to the caller instead of letting it crash the process
                if (onError != null)
                {
                    onError.Invoke(ex);
                }
                else
                {
                    PnSAPIBridge.LogInfo($"Download failed: {ex.Message}", "ByteLoader");
                }
            }
        }
        internal static void UpdateMainThreadQueue()
        {
            while (MainThreadExecutionQueue.TryDequeue(out var action))
            {
                action.Invoke(); // Runs safely on the main thread
            }
        }
    }
}
