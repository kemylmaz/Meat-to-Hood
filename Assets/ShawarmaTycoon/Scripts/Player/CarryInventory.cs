using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class CarryInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 12;
        [SerializeField, Min(1)] private int maxVisibleItems = 12;
        [SerializeField] private Transform stackRoot;
        [SerializeField] private GameObject rawMeatPrefab;
        [SerializeField] private GameObject cookedMeatPrefab;
        [SerializeField] private GameObject slicedMeatPrefab;
        [SerializeField] private GameObject wrapPrefab;

        private readonly List<GameObject> visuals = new();
        private TextMesh capacityLabel;
        private int baseCapacity;

        public ItemType HeldType { get; private set; } = ItemType.None;
        public int Count { get; private set; }
        public int Capacity => capacity;
        public event Action Changed;

        private void Awake()
        {
            if (stackRoot == null)
            {
                GameObject root = new("Carry Stack");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = new Vector3(0f, 1.15f, -0.28f);
                stackRoot = root.transform;
            }

            capacityLabel = PrototypeVisuals.CreateLabel("MAX", transform, new Vector3(0f, 2.75f, 0f), 0.14f);
            capacityLabel.color = PrototypeVisuals.Red;
            capacityLabel.gameObject.SetActive(false);
        }

        public void Configure(int newCapacity)
        {
            capacity = Mathf.Max(1, newCapacity);
            baseCapacity = capacity;
            RefreshVisuals();
        }

        public void SetCapacityUpgradeLevel(int level)
        {
            if (baseCapacity <= 0) baseCapacity = capacity;
            capacity = baseCapacity + Mathf.Max(0, level) * 3;
            RefreshVisuals();
        }

        public void SetStackAnchor(Transform anchor)
        {
            if (anchor == null || stackRoot == null)
                return;

            stackRoot.SetParent(anchor, false);
            stackRoot.localPosition = Vector3.zero;
            stackRoot.localRotation = Quaternion.identity;
            stackRoot.localScale = Vector3.one;
        }

        public bool CanAccept(ItemType type)
        {
            return type != ItemType.None && Count < capacity && (Count == 0 || HeldType == type);
        }

        public bool TryAdd(ItemType type, int amount = 1)
        {
            if (amount <= 0 || !CanAccept(type)) return false;
            int accepted = Mathf.Min(amount, capacity - Count);
            if (Count == 0) HeldType = type;
            Count += accepted;
            RefreshVisuals();
            return accepted > 0;
        }

        public bool TryRemove(ItemType type, int amount = 1)
        {
            if (amount <= 0 || Count <= 0 || HeldType != type) return false;
            Count -= Mathf.Min(amount, Count);
            if (Count == 0) HeldType = ItemType.None;
            RefreshVisuals();
            return true;
        }

        public int Clear()
        {
            int removed = Count;
            Count = 0;
            HeldType = ItemType.None;
            RefreshVisuals();
            return removed;
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < visuals.Count; i++)
                if (visuals[i] != null) Destroy(visuals[i]);
            visuals.Clear();

            int visibleCount = Mathf.Min(Count, maxVisibleItems);
            for (int i = 0; i < visibleCount; i++)
            {
                GameObject prefab = PrefabFor(HeldType);
                GameObject visual;
                if (prefab != null)
                {
                    visual = Instantiate(prefab, stackRoot);
                    visual.transform.localPosition = Vector3.up * (i * 0.16f);
                    visual.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    visual = PrototypeVisuals.CreateItemVisual(HeldType, stackRoot, Vector3.up * (i * 0.16f));
                }

                foreach (Collider collider in visual.GetComponentsInChildren<Collider>())
                    collider.enabled = false;
                visuals.Add(visual);
            }

            Changed?.Invoke();
            if (capacityLabel != null) capacityLabel.gameObject.SetActive(Count >= capacity);
        }

        private GameObject PrefabFor(ItemType type)
        {
            return type switch
            {
                ItemType.RawMeat => rawMeatPrefab,
                ItemType.CookedMeat => cookedMeatPrefab,
                ItemType.SlicedMeat => slicedMeatPrefab,
                ItemType.Wrap => wrapPrefab,
                _ => null
            };
        }
    }
}
