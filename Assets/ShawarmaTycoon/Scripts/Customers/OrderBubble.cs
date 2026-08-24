using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// A compact, camera-facing order badge: the food itself sits in a warm round
    /// well and a large plain number beside it says how many. The icon carries the
    /// meaning first, matching the visual reference without another text-heavy card.
    /// </summary>
    public sealed class OrderBubble : MonoBehaviour
    {
        private static readonly Color BubbleColor = UI.UITheme.CreamLight;

        private const float SlotWidth = 0.82f;
        private const float CardHeight = 0.58f;
        private const float IconWellDiameter = 0.46f;
        private const float IconScale = 0.92f;
        private const float LabelSize = 0.072f;

        private Transform content;
        private GameObject panel;
        private GameObject tail;

        /// <summary>
        /// Hangs the badge close to the customer's head. Its world rotation is
        /// corrected every frame, because inheriting a walking customer's yaw
        /// turns an otherwise readable order edge-on to the isometric camera.
        /// </summary>
        public static OrderBubble Create(Transform customer, float headHeight, float lift = 0f)
        {
            GameObject bubble = new("Sipariş Balonu");
            bubble.transform.SetParent(customer, false);
            bubble.transform.localPosition = new Vector3(0f, headHeight + lift, 0f);
            bubble.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            return bubble.AddComponent<OrderBubble>();
        }

        public void Show(CustomerOrder order)
        {
            Clear();
            if (order == null || order.LineCount == 0) return;

            int lines = order.LineCount;
            float width = SlotWidth * lines + 0.08f;

            panel = new GameObject("Balon");
            panel.transform.SetParent(transform, false);
            BuildPillLayer(panel.transform, "Gölge", width + 0.07f, CardHeight + 0.07f,
                0.035f, UI.UITheme.DropShadow);
            BuildPillLayer(panel.transform, "Kart", width, CardHeight,
                0f, BubbleColor);

            tail = new GameObject("Kuyruk");
            tail.transform.SetParent(transform, false);
            PrototypeVisuals.CreatePrimitive("Kuyruk Gölgesi", PrimitiveType.Cube, tail.transform,
                new Vector3(0.025f, -CardHeight * 0.61f - 0.025f, 0.035f),
                new Vector3(0.10f, 0.18f, 0.045f), UI.UITheme.DropShadow);
            PrototypeVisuals.CreatePrimitive("Kuyruk Gövdesi", PrimitiveType.Cube, tail.transform,
                new Vector3(0f, -CardHeight * 0.61f, 0f),
                new Vector3(0.085f, 0.17f, 0.045f), BubbleColor);

            GameObject contentRoot = new("İçerik");
            contentRoot.transform.SetParent(transform, false);
            contentRoot.transform.localPosition = new Vector3(0f, 0f, -0.055f);
            content = contentRoot.transform;

            int slot = 0;
            for (int i = 0; i < CustomerOrder.DisplayOrder.Length; i++)
            {
                ItemType type = CustomerOrder.DisplayOrder[i];
                int count = order.CountOf(type);
                if (count <= 0) continue;

                float x = (slot - (lines - 1) * 0.5f) * SlotWidth;
                slot++;

                PrototypeVisuals.CreatePrimitive("İkon Halkası", PrimitiveType.Cylinder, content,
                    new Vector3(x - 0.17f, 0f, 0.012f),
                    new Vector3(IconWellDiameter, 0.024f, IconWellDiameter),
                    UI.UITheme.Mustard, new Vector3(90f, 0f, 0f));
                PrototypeVisuals.CreatePrimitive("İkon Zemini", PrimitiveType.Cylinder, content,
                    new Vector3(x - 0.17f, 0f, -0.015f),
                    new Vector3(IconWellDiameter - 0.075f, 0.025f, IconWellDiameter - 0.075f),
                    UI.UITheme.CounterPaper, new Vector3(90f, 0f, 0f));

                PrototypeVisuals.CreateItemVisual(
                    type, content, new Vector3(x - 0.17f, 0.035f, -0.065f), IconScale);

                TextMesh countLabel = new GameObject("Adet").AddComponent<TextMesh>();
                countLabel.transform.SetParent(content, false);
                countLabel.transform.localPosition = new Vector3(x + 0.23f, -0.015f, -0.075f);
                countLabel.text = count.ToString();
                countLabel.anchor = TextAnchor.MiddleCenter;
                countLabel.alignment = TextAlignment.Center;
                countLabel.font = UI.UITheme.DisplayFont;
                countLabel.fontSize = 64;
                countLabel.characterSize = LabelSize;
                countLabel.fontStyle = FontStyle.Bold;
                countLabel.color = UI.UITheme.Ink;
                Renderer textRenderer = countLabel.GetComponent<Renderer>();
                if (textRenderer != null && countLabel.font != null)
                    textRenderer.sharedMaterial = countLabel.font.material;
            }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        private static void BuildPillLayer(
            Transform parent, string name, float width, float height, float z, Color color)
        {
            float bodyWidth = Mathf.Max(0.02f, width - height);
            PrototypeVisuals.CreatePrimitive(name + " Orta", PrimitiveType.Cube, parent,
                new Vector3(0f, 0f, z), new Vector3(bodyWidth, height, 0.04f), color);

            float capX = bodyWidth * 0.5f;
            PrototypeVisuals.CreatePrimitive(name + " Sol", PrimitiveType.Cylinder, parent,
                new Vector3(-capX, 0f, z), new Vector3(height, 0.02f, height), color,
                new Vector3(90f, 0f, 0f));
            PrototypeVisuals.CreatePrimitive(name + " Sağ", PrimitiveType.Cylinder, parent,
                new Vector3(capX, 0f, z), new Vector3(height, 0.02f, height), color,
                new Vector3(90f, 0f, 0f));
        }

        public void Hide() => Clear();

        private void LateUpdate() => transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        private void Clear()
        {
            if (content != null) Destroy(content.gameObject);
            if (panel != null) Destroy(panel);
            if (tail != null) Destroy(tail);
            content = null;
            panel = null;
            tail = null;
        }
    }
}
