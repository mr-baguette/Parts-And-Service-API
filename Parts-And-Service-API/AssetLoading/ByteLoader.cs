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
                    BepInExPlugin.BepInExAdapter.LogError($"[AssetLoader] Could not find embedded resource: '{resourceName}'");
                    return null;
                }

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return data;
            }
        }
        /// <summary>
        /// Reads byte data from file on disk using the specified path
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
        /// Reads file from internet url asynchronously. Will dumb raw data so if not a file, could look like garbage
        /// </summary>
        public static async void GetResourceBytesFromUrl(string url, Action<byte[]> onSuccess)
        {
            byte[] bytes = await Client.GetByteArrayAsync(url);

            // Queue the callback to be invoked later on the main thread
            MainThreadExecutionQueue.Enqueue(() => onSuccess?.Invoke(bytes));
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
