using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PnSAPI.AssetLoading
{
    /// <summary>Simple class holding frames and delays</summary>
    public class Gif
    {
        /// <summary>List of frames decoded. MUST BE DESTROYED TO PREVENT MEMORY LEAKAGE</summary>
        public List<Texture2D> Frames;
        /// <summary>The delay until the next frame should show up.</summary>
        public List<float> DelaysInSeconds;
    }
#pragma warning disable CS0649
    internal struct RawFrameData
    {
        public int Width;
        public int Height;
        public float DelayInSeconds;
        public Color32[] Pixels;
    }
#pragma warning restore CS0649
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
}
