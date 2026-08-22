using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class CarryInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 12;
        /// <summary>Dirty plates ride in their own slot, outside the one-type rule.</summary>
        [SerializeField, Min(1)] private int trashCapacity = 4;
        [SerializeField, Min(1)] private int maxVisibleItems = 12;
        [SerializeField] private Transform stackRoot;
        [SerializeField] private GameObject rawMeatPrefab;
        [SerializeField] private GameObject cookedMeatPrefab;
        [SerializeField] private GameObject slicedMeatPrefab;
        [SerializeField] private GameObject wrapPrefab;

        private readonly List<GameObject> visuals = new();
        private TextMesh capacityLabel;
        private FeedbackPulse stackPulse;
        private int baseCapacity;

        public ItemType HeldType { get; private set; } = ItemType.None;
        public int Count { get; private set; }
        public int Capacity => capacity;
        public int TrashCount { get; private set; }
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

            stackPulse = stackRoot.GetComponent<FeedbackPulse>() ?? stackRoot.gameObject.AddComponent<FeedbackPulse>();

            capacityLabel = PrototypeVisuals.CreateCozyBadge(
                "DOLU", transform, new Vector3(0f, 2.34f, 0f), 1.02f,
                UI.UITheme.Mustard, UI.UITheme.Ink);
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

        /// <summary>
        /// Dirty plates are exempt from the one-kind-at-a-time rule. Sharing the
        /// stack with stock meant a player carrying wraps could not clear a table,
        /// so a backed up line and a full dining room deadlocked each other and
        /// the only way out was binning a batch of finished food.
        /// </summary>
        public bool CanAccept(ItemType type)
        {
            if (type == ItemType.None) return false;
            if (type == ItemType.Trash) return TrashCount < trashCapacity;
            return Count < capacity && (Count == 0 || HeldType == type);
        }

        public bool TryAdd(ItemType type, int amount = 1)
        {
            if (amount <= 0 || !CanAccept(type)) return false;

            if (type == ItemType.Trash)
            {
                TrashCount += Mathf.Min(amount, trashCapacity - TrashCount);
                RefreshVisuals();
                return true;
            }

            int accepted = Mathf.Min(amount, capacity - Count);
            if (Count == 0) HeldType = type;
            Count += accepted;
            RefreshVisuals();
            return accepted > 0;
        }

        public bool TryRemove(ItemType type, int amount = 1)
        {
            if (amount <= 0) return false;

            if (type == ItemType.Trash)
            {
                if (TrashCount <= 0) return false;
                TrashCount -= Mathf.Min(amount, TrashCount);
                RefreshVisuals();
                return true;
            }

            if (Count <= 0 || HeldType != type) return false;
            Count -= Mathf.Min(amount, Count);
            if (Count == 0) HeldType = ItemType.None;
            RefreshVisuals();
            return true;
        }

        /// <summary>Drops the stock being carried. Leaves the trash slot alone.</summary>
        public int Clear()
        {
            int removed = Count;
            Count = 0;
            HeldType = ItemType.None;
            RefreshVisuals();
            return removed;
        }

        public int ClearTrash()
        {
            int removed = TrashCount;
            TrashCount = 0;
            RefreshVisuals();
            return removed;
        }

        private void RefreshVisuals()
        {
            ReleaseVisuals(visuals);

            int visibleCount = Mathf.Min(Count, maxVisibleItems);
            // Pitched off the item itself: a column of dirty plates sits nearly
            // flush while a column of drinks needs a cup's height between them.
            float step = PrototypeVisuals.StackStep(HeldType);
            for (int i = 0; i < visibleCount; i++)
            {
                GameObject prefab = PrefabFor(HeldType);
                GameObject visual;
                if (prefab != null)
                {
                    string key = $"carry.prefab.{prefab.GetInstanceID()}";
                    visual = GameplayObjectPool.Rent(key, stackRoot, () => Instantiate(prefab, stackRoot));
                    visual.transform.localPosition = Vector3.up * (i * 0.16f);
                    visual.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    ItemType type = HeldType;
                    string key = $"carry.item.{type}";
                    visual = GameplayObjectPool.Rent(key, stackRoot,
                        () => PrototypeVisuals.CreateItemVisual(type, stackRoot, Vector3.zero));
                    visual.transform.localPosition = Vector3.up * (i * step);
                }

                foreach (Collider collider in visual.GetComponentsInChildren<Collider>())
                    collider.enabled = false;
                visuals.Add(visual);
            }

            // Plates ride on top of the stock, so both loads read at a glance.
            float plateStep = PrototypeVisuals.StackStep(ItemType.Trash);
            float plateBase = visibleCount * step + 0.05f;
            for (int i = 0; i < TrashCount; i++)
            {
                GameObject plate = GameplayObjectPool.Rent("carry.item.Trash", stackRoot,
                    () => PrototypeVisuals.CreateItemVisual(ItemType.Trash, stackRoot, Vector3.zero));
                plate.transform.localPosition = Vector3.up * (plateBase + i * plateStep);
                foreach (Collider collider in plate.GetComponentsInChildren<Collider>())
                    collider.enabled = false;
                visuals.Add(plate);
            }

            Changed?.Invoke();
            if (Application.isPlaying && (Count > 0 || TrashCount > 0))
                stackPulse?.Kick();
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

        private static void ReleaseVisuals(List<GameObject> pooledVisuals)
        {
            for (int i = 0; i < pooledVisuals.Count; i++)
                GameplayObjectPool.Release(pooledVisuals[i]);
            pooledVisuals.Clear();
        }
    }
}
