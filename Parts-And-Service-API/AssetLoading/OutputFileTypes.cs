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
        public struct GifFrame
        {
            public Texture2D Texture;
            public float Delay; // Frame delay in seconds
        }
        public struct LoadedGif
        {
            public List<GifFrame> Frames;
            public float TotalDuration;
            public bool IsValid => Frames != null && Frames.Count > 0;
        }
    }
}
