using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnSAPI.AssetLoading
{
    public static partial class AssetLoader
    {
        private static readonly ConcurrentQueue<Action> MainThreadExecutionQueue = new ConcurrentQueue<Action>();
        // Queue system so type building can happen on main thread
        internal static void UpdateMainThreadQueue()
        {
            while (MainThreadExecutionQueue.TryDequeue(out var action))
            {
                action.Invoke(); // Runs safely on the main thread
            }
        }
    }
}
