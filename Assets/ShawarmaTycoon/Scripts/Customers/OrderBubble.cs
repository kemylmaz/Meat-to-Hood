using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// The speech bubble over a customer showing what they are waiting for, one
    /// small model of each thing with a count beside it.
    ///
    /// The queue used to be a row of identical people with nothing to say what any
    /// of them wanted, so an empty fridge stalled the line for no visible reason.
    /// </summary>
    public sealed class OrderBubble : MonoBehaviour
    {
        private static readonly Color BubbleColor = new(0.99f, 0.97f, 0.92f);
        private static readonly Color BubbleEdge = new(0.36f, 0.28f, 0.24f);

        /// <summary>
        /// Width per item line, and the height of the card behind them.
        ///
        /// These were half this size, chosen so a three item order did not cover
        /// the people either side of it in the queue. That made the bubble
        /// unreadable, which loses the point of having one - the whole job of a
        /// bubble is to be read from wherever the player happens to be standing.
        /// The overlap is handled by lifting bubbles clear of each other instead,
        /// see <see cref="Create"/>.
        /// </summary>
        private const float LineWidth = 0.62f;

        private const float CardHeight = 0.56f;

        /// <summary>Portion size inside the card, and the count text beside it.</summary>
        private const float IconScale = 0.78f;
        private const float LabelSize = 0.038f;

        private readonly List<GameObject> lineVisuals = new();
        private Transform content;
        private GameObject panel;
        private GameObject tail;

        /// <summary>
        /// Hung over a customer's head. <paramref name="lift"/> stacks bubbles at
        /// different heights along a queue: at full size neighbouring cards would
        /// otherwise overlap, and staggering them costs nothing while shrinking
        /// them costs the reading.
        /// </summary>
        public static OrderBubble Create(Transform customer, float headHeight, float lift = 0f)
        {
            GameObject bubble = new("Sipariş Balonu");
            bubble.transform.SetParent(customer, false);
            bubble.transform.localPosition = new Vector3(0f, headHeight + lift, 0f);
            // Tilted the way every other world label is, so it reads square-on to
            // the isometric rig instead of edge-on.
            bubble.transform.localEulerAngles = new Vector3(55f, 0f, 0f);
            return bubble.AddComponent<OrderBubble>();
        }

        public void Show(CustomerOrder order)
        {
            Clear();
            if (order == null || order.LineCount == 0) return;

            int lines = order.LineCount;
            float width = LineWidth * lines + 0.10f;

            panel = PrototypeVisuals.CreatePrimitive("Balon", PrimitiveType.Cube, transform,
                Vector3.zero, new Vector3(width, CardHeight, 0.05f), BubbleColor);
            PrototypeVisuals.CreatePrimitive("Kenar", PrimitiveType.Cube, transform,
                new Vector3(0f, 0f, 0.03f), new Vector3(width + 0.06f, CardHeight + 0.06f, 0.04f), BubbleEdge);
            tail = PrototypeVisuals.CreatePrimitive("Kuyruk", PrimitiveType.Cube, transform,
                new Vector3(0f, -CardHeight * 0.66f, 0f), new Vector3(0.14f, 0.21f, 0.05f), BubbleColor);

            GameObject contentRoot = new("İçerik");
            contentRoot.transform.SetParent(transform, false);
            contentRoot.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            content = contentRoot.transform;

            int slot = 0;
            for (int i = 0; i < CustomerOrder.DisplayOrder.Length; i++)
            {
                ItemType type = CustomerOrder.DisplayOrder[i];
                int count = order.CountOf(type);
                if (count <= 0) continue;

                float x = (slot - (lines - 1) * 0.5f) * LineWidth;
                slot++;

                GameObject icon = PrototypeVisuals.CreateItemVisual(
                    type, content, new Vector3(x - 0.11f, 0.03f, 0f), IconScale);
                lineVisuals.Add(icon);

                TextMesh countLabel = new GameObject("Adet").AddComponent<TextMesh>();
                countLabel.transform.SetParent(content, false);
                countLabel.transform.localPosition = new Vector3(x + 0.17f, -0.03f, -0.02f);
                countLabel.text = "x" + count;
                countLabel.anchor = TextAnchor.MiddleCenter;
                countLabel.alignment = TextAlignment.Center;
                countLabel.characterSize = LabelSize;
                countLabel.fontSize = 64;
                countLabel.fontStyle = FontStyle.Bold;
                countLabel.color = new Color(0.24f, 0.16f, 0.12f);
                lineVisuals.Add(countLabel.gameObject);
            }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        public void Hide() => Clear();

        private void Clear()
        {
            for (int i = 0; i < lineVisuals.Count; i++)
                if (lineVisuals[i] != null) Destroy(lineVisuals[i]);
            lineVisuals.Clear();

            if (content != null) Destroy(content.gameObject);
            if (panel != null) Destroy(panel);
            if (tail != null) Destroy(tail);
            foreach (Transform child in transform)
                if (child.name == "Kenar") Destroy(child.gameObject);
            content = null;
            panel = null;
            tail = null;
        }
    }
}
