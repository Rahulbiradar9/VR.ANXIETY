using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows background threads (e.g. WebSocket callbacks) to queue work
/// that must be executed on the Unity main thread.
/// 
/// Usage:  UnityMainThreadDispatcher.Enqueue(() => SomeUnityCall());
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly Queue<Action> _queue = new Queue<Action>();
    private static readonly object _lock = new object();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                // Auto-create if not present in the scene
                var go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// Queue an action to be executed on the main thread next Update().
    /// Safe to call from any thread.
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_lock)
        {
            _queue.Enqueue(action);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        while (true)
        {
            Action action = null;
            lock (_lock)
            {
                if (_queue.Count == 0) break;
                action = _queue.Dequeue();
            }
            action?.Invoke();
        }
    }
}
