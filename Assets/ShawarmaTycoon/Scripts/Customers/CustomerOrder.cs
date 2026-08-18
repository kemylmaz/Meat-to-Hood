using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// What one customer came in for. Small and mutable on purpose: an order that
    /// cannot be filled gets trimmed rather than blocking the line forever, and
    /// the bubble over the customer's head redraws from the same object.
    /// </summary>
    public sealed class CustomerOrder
    {
        private readonly Dictionary<ItemType, int> lines = new();

        /// <summary>The order as it stands, in a stable order for display.</summary>
        public static readonly ItemType[] DisplayOrder =
        {
            ItemType.Wrap, ItemType.Drink, ItemType.Dessert
        };

        public int LineCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < DisplayOrder.Length; i++)
                    if (CountOf(DisplayOrder[i]) > 0) count++;
                return count;
            }
        }

        public int TotalItems
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<ItemType, int> line in lines) total += line.Value;
                return total;
            }
        }

        public int CountOf(ItemType type) => lines.TryGetValue(type, out int count) ? count : 0;

        public void Add(ItemType type, int count)
        {
            if (type == ItemType.None || count <= 0) return;
            lines[type] = CountOf(type) + count;
        }

        public void Remove(ItemType type)
        {
            lines.Remove(type);
        }

        public void Clear() => lines.Clear();

        /// <summary>
        /// Drops the lines <paramref name="isStocked"/> says cannot be filled, and
        /// answers whether anything was given up. The wrap is never dropped - a
        /// customer who wanted lunch still wants lunch - so an order always keeps
        /// something to serve.
        /// </summary>
        public bool TrimUnavailableExtras(Func<ItemType, bool> isStocked)
        {
            bool trimmed = false;
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                ItemType type = DisplayOrder[i];
                if (type == ItemType.Wrap || CountOf(type) <= 0 || isStocked(type)) continue;
                Remove(type);
                trimmed = true;
            }
            return trimmed;
        }

        /// <summary>What the order is worth, relative to a plain wrap.</summary>
        public float ValueMultiplier
        {
            get
            {
                float value = 0f;
                foreach (KeyValuePair<ItemType, int> line in lines)
                    value += line.Value * PriceOf(line.Key);
                return Mathf.Max(1f, value);
            }
        }

        /// <summary>
        /// Relative worth of each thing sold. A drink is nearly free to keep and
        /// nearly pure margin; dessert sits between the two.
        /// </summary>
        public static float PriceOf(ItemType type) => type switch
        {
            ItemType.Wrap => 1f,
            ItemType.Dessert => 0.7f,
            ItemType.Drink => 0.45f,
            _ => 0f
        };
    }
}
