using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PnSAPI.Coroutining
{
    /// <summary>
    /// A simple class that let's you easily run IEnumerator methods without the hassle of Il2Cpp.
    /// </summary>
    public class Coroutines : MonoBehaviour
    {
        /// <summary>Interger pointer constructor for Il2Cpp</summary>
        public Coroutines(System.IntPtr ptr) : base(ptr) { }
        /// <summary>
        /// Instance used for instance access. Rarely used since all methods are static
        /// </summary>
        public static Coroutines Instance;

        private class CoroutineData
        {
            public IEnumerator Enumerator;
            public float WaitTimer;
            public CoroutineData Waitaction;
            public Action OnComplete;
        }

        private static readonly List<CoroutineData> _activeCoroutines = new List<CoroutineData>();
        private static readonly List<CoroutineData> _toAdd = new List<CoroutineData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        /// <summary>
        /// Runs the provided coroutine and returns a handle to access it. You can optionally provide an onComplete callback that will be invoked when the coroutine finishes.
        /// </summary>
        /// <param name="coroutine">The coroutine to run.</param>
        /// <param name="onComplete">An optional callback to invoke when the coroutine completes.</param>
        /// <returns>A handle to the running coroutine.</returns>
        [HideFromIl2Cpp]
        public static CoroutineHandle Run(IEnumerator coroutine, Action onComplete = null)
        {
            if (coroutine == null) return null;

            _activeCoroutines.Add(new CoroutineData
            {
                Enumerator = coroutine,
                WaitTimer = 0f,
                OnComplete = onComplete
            });

            return new CoroutineHandle(coroutine);
        }
        /// <summary>
        /// Stops a coroutine based on the provided handle. If the handle is null or the coroutine has already completed, this method does nothing.
        /// Note that this requires storing a reference to the handle, because calling the method again will just make a seperate, different instance.
        /// </summary>
        /// <param name="handle"></param>
        [HideFromIl2Cpp]
        public static void Stop(CoroutineHandle handle)
        {
            if (handle != null)
            {
                _activeCoroutines.RemoveAll(c => c.Enumerator == handle.Enumerator);
            }
        }
        /// <summary>
        /// Clears all active coroutines. Use with caution, as running coroutines will be abruptly stopped and will not be able to clean up (if end of the coroutine does that).
        /// </summary>
        [HideFromIl2Cpp]
        public static void StopAll()
        {
            _activeCoroutines.Clear();
        }

        private void Update()
        {
            if (_toAdd.Count > 0)
            {
                _activeCoroutines.AddRange(_toAdd);
                _toAdd.Clear();
            }
            for (int i = _activeCoroutines.Count - 1; i >= 0; i--)
            {

                CoroutineData data = _activeCoroutines[i];

                if (data.WaitTimer > 0f)
                {
                    data.WaitTimer -= Time.deltaTime;
                    continue;
                }
                if (data.Waitaction != null) 
                {
                    if (!_activeCoroutines.Contains(data.Waitaction)) data.Waitaction = null;
                    else continue;
                }
                try
                {
                    // Advance the coroutine
                    if (!data.Enumerator.MoveNext())
                    {

                        try { data.OnComplete?.Invoke(); }
                        catch (Exception ex) { BepInExPlugin.BepInExAdapter.LogError($"[Coroutines] OnComplete error: {ex}"); }

                        _activeCoroutines.RemoveAt(i);
                        continue;
                    }

                    // yield logic
                    object yieldedItem = data.Enumerator.Current;

                    if (yieldedItem is WaitForSeconds customWait)
                    {
                        data.Waitaction = StartInternal(Wait(customWait.Duration));
                    }
                    else if (yieldedItem is float waitFloat) data.WaitTimer = waitFloat;
                    else if (yieldedItem is int waitInt) data.WaitTimer = waitInt;
                    else if (yieldedItem == null) { }
                    else if (yieldedItem is IEnumerator coroutine) data.Waitaction = StartInternal(coroutine);
                    else if (yieldedItem is CoroutineHandle handle)
                    {
                        data.Waitaction = _activeCoroutines.FirstOrDefault(c => c.Enumerator == handle.Enumerator)
                                       ?? _toAdd.FirstOrDefault(c => c.Enumerator == handle.Enumerator);
                    }
                    else if (yieldedItem is Func<bool> predicate) data.Waitaction = StartInternal(WaitUntil(predicate));
                    else
                    {
                        BepInExPlugin.BepInExAdapter.LogWarning($"[Coroutines] Yielded unknown object: {yieldedItem.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    BepInExPlugin.BepInExAdapter.LogError($"[Coroutines] Crash at yield! Removing coroutine: {ex}");
                    _activeCoroutines.RemoveAt(i);
                }
            }
        }
        [HideFromIl2Cpp]
        private static CoroutineData StartInternal(IEnumerator coroutine, Action onComplete = null)
        {
            var data = new CoroutineData
            {
                Enumerator = coroutine,
                OnComplete = onComplete
            };
            _toAdd.Add(data);
            return data;
        }
        #region yield coroutines
        IEnumerator Wait(double waitTime)
        {
            double endTime = Time.timeAsDouble + waitTime;
            while (Time.timeAsDouble < endTime)
            {
                yield return null;
            }
            yield break;
        }
        IEnumerator WaitUntil(Func<bool> predicate)
        {
            while (predicate.Invoke())
            {
                yield return null;
            }
            yield break;
        }
        #endregion
    }

    /// <summary>
    /// A simple wrapper so you can stop specific coroutines later
    /// </summary>
    public class CoroutineHandle
    {
        /// <summary>Used for seeing what coroutines is in this handle.</summary>
        public IEnumerator Enumerator { get; private set; }
        /// <summary>Constructor for this class</summary>
        public CoroutineHandle(IEnumerator enumerator)
        {
            Enumerator = enumerator;
        }
    }
    /// <summary>
    /// A simple class to replace unity's advanced WaitForSeconds, which won't work as easily on the managed side.
    /// Yielding a new WaitForSeconds will start a timer that will count down using Time.deltaTime, and the coroutine will continue once the timer reaches zero.
    /// </summary>
    public class WaitForSeconds
    {
        /// <summary>Duration to wait</summary>
        public float Duration { get; }
        /// <summary>Simple constructor making a coroutine wait.</summary>
        public WaitForSeconds(float duration)
        {
            Duration = duration;
        }
    }
}
