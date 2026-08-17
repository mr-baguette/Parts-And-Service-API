using MG.GIF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PnSAPI.AssetLoading
{
    public static partial class AssetLoader
    {
        /// <summary>Struct to contain frame data from a gif</summary>
        public struct GifFrame
        {
            /// <summary>The texture as Texture2D</summary>
            public Texture2D Texture;
            /// <summary>Delay to next frame in seconds</summary>
            public float Delay; // Frame delay in seconds
        }
        /// <summary>Struct to contain gif frames and total duration</summary>
        public struct LoadedGif
        {
            /// <summary>List of all frames</summary>
            public List<GifFrame> Frames;
            /// <summary>The total duration of the gif</summary>
            public float TotalDuration;
            /// <summary>Helper to detect if the gif loaded correctly</summary>
            public bool IsValid => Frames != null && Frames.Count > 0;
        }
        internal class AudioHandle
        {
            public AudioClip Clip { get; private set; }

            // State flags
            public bool IsReady { get; private set; }
            public bool IsError { get; private set; }

            public string ClipName { get; private set; }

            public AudioHandle(string name)
            {
                ClipName = name;
                IsReady = false;
                IsError = false;
            }

            // Called by the Coroutine when finished
            public void CompleteLoad(AudioClip loadedClip)
            {
                if (loadedClip != null)
                {
                    Clip = loadedClip;
                    IsReady = true;
                }
                else
                {
                    IsError = true;
                }
            }

            public void Release()
            {
                // If you do object pooling or memory management, do it here.
                // Otherwise, Unity handles Garbage Collection for destroyed GameObjects.
                if (Clip != null)
                {
                    UnityEngine.Object.Destroy(Clip);
                    Clip = null;
                }
            }
        }

        internal class DecodedAudioData
        {
            public string name;
            public float[] samples;
            public int channels;
            public int sampleRate;
        }
    }
}
