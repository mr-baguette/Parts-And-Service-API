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
        public class Gif
        {
            public List<Texture2D> Frames;
            public List<float> DelaysInSeconds;
        }
        private struct RawFrameData
        {
            public int Width;
            public int Height;
            public float DelayInSeconds;
            public Color32[] Pixels;
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
    }
}
