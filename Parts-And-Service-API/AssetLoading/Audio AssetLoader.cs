using NVorbis;
using PnSAPI.BepInExPlugin;
using PnSAPI.Coroutining;
using System.Reflection;
using System.Collections;
using UnityEngine;

namespace PnSAPI.AssetLoading
{
    public static partial class AssetLoader
    {
        private static Dictionary<string, AudioHandle> audioCache = new Dictionary<string, AudioHandle>();

        /// <summary>
        /// Loads an ogg (vorbis) audio file on a background thread
        /// </summary>
        public static void LoadOgg(ResourceType resource, float loadTimeout = 10f, ushort decodesPerFrame = 1, Action<AudioClip> onComplete = null)
        {
            Coroutines.Run(LoadOggAudioAsync(resource, Assembly.GetCallingAssembly(), loadTimeout, onComplete));
        }
        private static IEnumerator LoadOggAudioAsync(ResourceType resource, Assembly callingAssembly, float loadTimeout = 10f, Action<AudioClip> onComplete = null)
        {
            BepInExAdapter.LogInfo($"[SmartAudioLoader] Starting async load for: {resource.path}");
            string clipName = Path.GetFileNameWithoutExtension(resource.path);
            AudioHandle newHandle = new AudioHandle(clipName);

            byte[] fileBytes = null;
            bool loaded = false;
            Action<byte[]> bytesLoaded = (fileData) => { fileBytes = fileData; loaded = true; };
            ByteLoader.LoadBytes(resource, bytesLoaded, callingAssembly);

            float maxTime = Time.unscaledTime + loadTimeout;
            while (!loaded)
            {
                yield return null;
                if (Time.unscaledTime > maxTime) { throw new TimeoutException($"File {resource.path} took too long to load"); }
            }

            // Fire off the background thread
            ThreadPool.QueueUserWorkItem(_ =>
            {
                float[] pcmSamples = null;
                int channels = 0;
                int sampleRate = 0;
                int totalRead = 0;
                bool threadSuccess = false;

                try
                {
                    using (var ms = new MemoryStream(fileBytes))
                    using (var reader = new VorbisReader(ms))
                    {
                        channels = reader.Channels;
                        sampleRate = reader.SampleRate;

                        long totalFloats = reader.TotalSamples * channels;
                        pcmSamples = new float[totalFloats];

                        totalRead = reader.ReadSamples(pcmSamples, 0, (int)totalFloats);
                        threadSuccess = totalRead > 0;
                    }
                }
                catch (Exception ex)
                {
                    BepInExAdapter.LogError($"[SmartAudioLoader] Background decode failed: {ex}");
                }

                // --- THE MAGIC BRIDGE ---
                // Instead of relying on a fragile coroutine yield, we drop the results
                // directly into the native Update() queue to be processed on the next frame.
                MainThreadExecutionQueue.Enqueue(() =>
                {
                    AudioClip clip = null;
                    if (threadSuccess && pcmSamples != null)
                    {
                        try
                        {
                            clip = AudioClip.Create(clipName, totalRead / channels, channels, sampleRate, false);
                            clip.SetData(pcmSamples, 0);
                            BepInExAdapter.LogInfo($"[SmartAudioLoader] Successfully built AudioClip '{clipName}' on Main Thread!");
                        }
                        catch (Exception ex)
                        {
                            BepInExAdapter.LogError($"[SmartAudioLoader] AudioClip creation failed: {ex}");
                        }
                    }

                    newHandle.CompleteLoad(clip);
                    onComplete?.Invoke(newHandle.Clip);
                });
            });
        }
    } 
}
