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
    }
}
