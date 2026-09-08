using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PnSAPI.Core;
using PnSAPI.AssetLoading;
#pragma warning disable CS1591
namespace PnSAPI.BepInExPlugin
{
    public class RuntimeUnityEvents : MonoBehaviour
    {
        public RuntimeUnityEvents(System.IntPtr ptr) : base(ptr) { }
        void Update()
        {
            PnSAPIBridge.Update();
            ByteLoader.UpdateMainThreadQueue();
            AssetLoader.UpdateMainThreadQueue();
            AssemblyLoader.Update();
        }
        void LateUpdate()
        {
            AssemblyLoader.LateUpdate();
        }
    }
}
