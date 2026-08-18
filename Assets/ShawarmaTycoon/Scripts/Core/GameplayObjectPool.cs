using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Scene-scoped pool for reusable, short-lived gameplay visuals. Phase 2 starts with
    /// carried and station item stacks; customers, money and VFX can join the
    /// same service without another pooling implementation.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameplayObjectPool : MonoBehaviour
    {
        public static GameplayObjectPool Instance { get; private set; }

        private readonly Dictionary<string, Stack<GameObject>> available = new(StringComparer.Ordinal);
        private readonly Dictionary<int, string> keysByInstance = new();
        private Transform inactiveRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            inactiveRoot = new GameObject("Pooled Objects").transform;
            inactiveRoot.SetParent(transform, false);
            inactiveRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            available.Clear();
            keysByInstance.Clear();
        }

        public static GameObject Rent(string key, Transform parent, Func<GameObject> factory)
        {
            if (Instance == null) return factory?.Invoke();
            return Instance.RentInternal(key, parent, factory);
        }

        public static void Release(GameObject instance)
        {
            if (instance == null) return;
            if (Instance == null || !Instance.ReleaseInternal(instance)) Destroy(instance);
        }

        private GameObject RentInternal(string key, Transform parent, Func<GameObject> factory)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Pool key is required.", nameof(key));

            GameObject instance = null;
            if (available.TryGetValue(key, out Stack<GameObject> stack))
            {
                while (stack.Count > 0 && instance == null) instance = stack.Pop();
            }

            if (instance == null)
            {
                instance = factory?.Invoke();
                if (instance == null) return null;
                keysByInstance[instance.GetInstanceID()] = key;
            }

            instance.transform.SetParent(parent, false);
            instance.SetActive(true);
            return instance;
        }

        private bool ReleaseInternal(GameObject instance)
        {
            if (!keysByInstance.TryGetValue(instance.GetInstanceID(), out string key)) return false;
            if (!available.TryGetValue(key, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                available[key] = stack;
            }

            instance.SetActive(false);
            instance.transform.SetParent(inactiveRoot, false);
            stack.Push(instance);
            return true;
        }
    }
}


