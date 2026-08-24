using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Two related pieces of world money language. Collected takings use a small
    /// physical receipt card; purchases use an open, ground-painted name and
    /// price framed by white corners. They share the banknote colours and amount
    /// update path, but never share the opaque receipt background.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldCashMarker : MonoBehaviour
    {
        private static readonly Color Card = new(1f, 0.94f, 0.77f);
        private static readonly Color Ink = new(0.25f, 0.14f, 0.10f);
        private static readonly Color Leaf = new(0.28f, 0.68f, 0.31f);
        private static readonly Color LeafDark = new(0.12f, 0.43f, 0.20f);
        private static readonly Color Gold = new(1f, 0.76f, 0.22f);

        private Transform cardPivot;
        private TextMesh amountLabel;
        private TextMesh amountShadowLabel;
        private TextMesh titleLabel;
        private Vector3 cardRestPosition;
        private bool groundedPurchase;
        private float punch;
        private float phase;

        public string AmountText => amountLabel != null ? amountLabel.text : string.Empty;
        public string TitleText => titleLabel != null ? titleLabel.text : string.Empty;
        public bool IsGroundedPurchase => groundedPurchase;

        public static WorldCashMarker Create(Transform parent)
        {
            GameObject marker = new("Para Kartı");
            marker.transform.SetParent(parent, false);
            WorldCashMarker result = marker.AddComponent<WorldCashMarker>();
            result.BuildReceiptVisuals();
            return result;
        }

        /// <summary>
        /// Creates the transparent build-price treatment used by purchase pads:
        /// no disc, card, shadow panel or arrow—only corners, object name, a
        /// small banknote and the price sitting just above the floor.
        /// </summary>
        public static WorldCashMarker CreatePurchase(Transform parent, string displayName)
        {
            GameObject marker = new("Satın Alma Göstergesi");
            marker.transform.SetParent(parent, false);
            WorldCashMarker result = marker.AddComponent<WorldCashMarker>();
            result.groundedPurchase = true;
            result.BuildPurchaseVisuals(string.IsNullOrWhiteSpace(displayName)
                ? parent.name
                : displayName);
            return result;
        }

        public void SetAmount(int amount, bool animate = true)
        {
            if (amountLabel == null) return;
            string value = amount > 0
                ? groundedPurchase ? amount.ToString() : "₺" + amount
                : string.Empty;
            amountLabel.text = value;
            if (amountShadowLabel != null) amountShadowLabel.text = value;
            if (animate) punch = amount > 0 ? 0.12f : 0f;
        }

        private void BuildReceiptVisuals()
        {
            phase = Mathf.Abs(transform.parent != null
                ? transform.parent.GetInstanceID() * 0.0137f
                : GetInstanceID() * 0.0137f);

            // Four quiet corner brackets make the collection footprint readable
            // without turning every table into another glowing UI panel.
            Transform brackets = new GameObject("Toplama Köşeleri").transform;
            brackets.SetParent(transform, false);
            BuildBracket(brackets, -0.43f, -0.32f, 1f, 1f);
            BuildBracket(brackets, 0.43f, -0.32f, -1f, 1f);
            BuildBracket(brackets, -0.43f, 0.32f, 1f, -1f);
            BuildBracket(brackets, 0.43f, 0.32f, -1f, -1f);

            cardPivot = new GameObject("Fiş Kartı").transform;
            cardPivot.SetParent(transform, false);
            cardRestPosition = new Vector3(0f, 0.58f, 0f);
            cardPivot.localPosition = cardRestPosition;
            cardPivot.localEulerAngles = new Vector3(55f, 0f, 0f);

            PrototypeVisuals.CreatePrimitive(
                "Kart Gölgesi", PrimitiveType.Cube, cardPivot,
                new Vector3(0.035f, -0.035f, 0.045f), new Vector3(0.80f, 0.55f, 0.055f), Ink);
            PrototypeVisuals.CreatePrimitive(
                "Kart", PrimitiveType.Cube, cardPivot,
                Vector3.zero, new Vector3(0.76f, 0.51f, 0.060f), Card);

            // Banknote pictogram: dark rim, green note and a warm coin seal.
            PrototypeVisuals.CreatePrimitive(
                "Banknot Kenarı", PrimitiveType.Cube, cardPivot,
                new Vector3(0f, 0.115f, -0.040f), new Vector3(0.36f, 0.18f, 0.028f), LeafDark,
                new Vector3(0f, 0f, -7f));
            PrototypeVisuals.CreatePrimitive(
                "Banknot", PrimitiveType.Cube, cardPivot,
                new Vector3(0f, 0.115f, -0.058f), new Vector3(0.31f, 0.135f, 0.018f), Leaf,
                new Vector3(0f, 0f, -7f));
            PrototypeVisuals.CreatePrimitive(
                "Banknot Mührü", PrimitiveType.Cylinder, cardPivot,
                new Vector3(0f, 0.115f, -0.075f), new Vector3(0.075f, 0.012f, 0.075f), Gold,
                new Vector3(90f, 0f, 0f));

            GameObject textObject = new("Tutar");
            textObject.transform.SetParent(cardPivot, false);
            textObject.transform.localPosition = new Vector3(0f, -0.125f, -0.045f);
            amountLabel = textObject.AddComponent<TextMesh>();
            amountLabel.anchor = TextAnchor.MiddleCenter;
            amountLabel.alignment = TextAlignment.Center;
            amountLabel.font = UI.UITheme.DisplayFont;
            amountLabel.fontSize = 64;
            amountLabel.characterSize = 0.050f;
            amountLabel.fontStyle = FontStyle.Bold;
            amountLabel.color = Ink;
            Renderer textRenderer = amountLabel.GetComponent<Renderer>();
            if (textRenderer != null && amountLabel.font != null)
                textRenderer.sharedMaterial = amountLabel.font.material;

            // One deliberate accent from the reference: a compact down chevron,
            // not a giant tutorial arrow repeated across the dining room.
            PrototypeVisuals.CreatePrimitive(
                "Ok Gövdesi", PrimitiveType.Cube, cardPivot,
                new Vector3(0f, 0.38f, 0f), new Vector3(0.055f, 0.13f, 0.035f), Gold);
            PrototypeVisuals.CreatePrimitive(
                "Ok Sol", PrimitiveType.Cube, cardPivot,
                new Vector3(-0.055f, 0.315f, 0f), new Vector3(0.13f, 0.045f, 0.035f), Gold,
                new Vector3(0f, 0f, 42f));
            PrototypeVisuals.CreatePrimitive(
                "Ok Sağ", PrimitiveType.Cube, cardPivot,
                new Vector3(0.055f, 0.315f, 0f), new Vector3(0.13f, 0.045f, 0.035f), Gold,
                new Vector3(0f, 0f, -42f));
        }

        private void BuildPurchaseVisuals(string displayName)
        {
            Transform brackets = new GameObject("Satın Alma Köşeleri").transform;
            brackets.SetParent(transform, false);
            BuildPurchaseBracket(brackets, -0.78f, -0.56f, 1f, 1f);
            BuildPurchaseBracket(brackets, 0.78f, -0.56f, -1f, 1f);
            BuildPurchaseBracket(brackets, -0.78f, 0.56f, 1f, -1f);
            BuildPurchaseBracket(brackets, 0.78f, 0.56f, -1f, -1f);

            cardPivot = new GameObject("Zemin Bilgisi").transform;
            cardPivot.SetParent(transform, false);
            cardRestPosition = new Vector3(0f, 0.12f, -0.04f);
            cardPivot.localPosition = cardRestPosition;
            cardPivot.localEulerAngles = new Vector3(78f, 0f, 0f);

            // A tiny crossed-tool mark carries the same "build" cue as the
            // reference without adding another word or a solid button behind it.
            PrototypeVisuals.CreatePrimitive(
                "İnşa İkonu Sol", PrimitiveType.Cube, cardPivot,
                new Vector3(-0.49f, 0.14f, -0.050f), new Vector3(0.18f, 0.045f, 0.020f),
                UI.UITheme.CounterPaper, new Vector3(0f, 0f, 45f));
            PrototypeVisuals.CreatePrimitive(
                "İnşa İkonu Sağ", PrimitiveType.Cube, cardPivot,
                new Vector3(-0.49f, 0.14f, -0.052f), new Vector3(0.18f, 0.045f, 0.020f),
                UI.UITheme.CounterPaper, new Vector3(0f, 0f, -45f));

            float titleSize = displayName.Length <= 10 ? 0.046f
                : displayName.Length <= 16 ? 0.039f
                : 0.033f;
            CreateText("Nesne Adı Gölgesi", cardPivot, displayName,
                new Vector3(0.075f, 0.124f, -0.038f), TextAnchor.MiddleCenter,
                titleSize, Ink);
            titleLabel = CreateText("Nesne Adı", cardPivot, displayName,
                new Vector3(0.060f, 0.140f, -0.058f), TextAnchor.MiddleCenter,
                titleSize, UI.UITheme.CounterPaper);

            // Small green banknote, then the number. The icon already says
            // currency, so purchase prices stay as clean digits like the reference.
            PrototypeVisuals.CreatePrimitive(
                "Banknot Kenarı", PrimitiveType.Cube, cardPivot,
                new Vector3(-0.20f, -0.12f, -0.040f), new Vector3(0.24f, 0.12f, 0.024f),
                LeafDark, new Vector3(0f, 0f, -7f));
            PrototypeVisuals.CreatePrimitive(
                "Banknot", PrimitiveType.Cube, cardPivot,
                new Vector3(-0.20f, -0.12f, -0.058f), new Vector3(0.20f, 0.085f, 0.016f),
                Leaf, new Vector3(0f, 0f, -7f));
            PrototypeVisuals.CreatePrimitive(
                "Banknot Mührü", PrimitiveType.Cylinder, cardPivot,
                new Vector3(-0.20f, -0.12f, -0.074f), new Vector3(0.046f, 0.010f, 0.046f),
                Gold, new Vector3(90f, 0f, 0f));

            amountShadowLabel = CreateText("Fiyat Gölgesi", cardPivot, string.Empty,
                new Vector3(-0.010f, -0.136f, -0.038f), TextAnchor.MiddleLeft,
                0.058f, Ink);
            amountLabel = CreateText("Fiyat", cardPivot, string.Empty,
                new Vector3(-0.025f, -0.120f, -0.058f), TextAnchor.MiddleLeft,
                0.058f, UI.UITheme.CounterPaper);
        }

        private static TextMesh CreateText(
            string objectName, Transform parent, string value, Vector3 localPosition,
            TextAnchor anchor, float characterSize, Color color)
        {
            GameObject textObject = new(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            TextMesh label = textObject.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = anchor;
            label.alignment = anchor == TextAnchor.MiddleLeft
                ? TextAlignment.Left
                : TextAlignment.Center;
            label.font = UI.UITheme.DisplayFont;
            label.fontSize = 64;
            label.characterSize = characterSize;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            Renderer renderer = label.GetComponent<Renderer>();
            if (renderer != null && label.font != null)
                renderer.sharedMaterial = label.font.material;
            return label;
        }

        private static void BuildPurchaseBracket(
            Transform parent, float x, float z, float inwardX, float inwardZ)
        {
            PrototypeVisuals.CreatePrimitive(
                "Köşe Yatay", PrimitiveType.Cube, parent,
                new Vector3(x + inwardX * 0.115f, 0.055f, z),
                new Vector3(0.28f, 0.024f, 0.055f), UI.UITheme.CounterPaper);
            PrototypeVisuals.CreatePrimitive(
                "Köşe Dikey", PrimitiveType.Cube, parent,
                new Vector3(x, 0.055f, z + inwardZ * 0.115f),
                new Vector3(0.055f, 0.024f, 0.28f), UI.UITheme.CounterPaper);
        }

        private static void BuildBracket(
            Transform parent, float x, float z, float inwardX, float inwardZ)
        {
            PrototypeVisuals.CreatePrimitive(
                "Köşe Yatay", PrimitiveType.Cube, parent,
                new Vector3(x + inwardX * 0.065f, 0.08f, z),
                new Vector3(0.16f, 0.025f, 0.045f), Card);
            PrototypeVisuals.CreatePrimitive(
                "Köşe Dikey", PrimitiveType.Cube, parent,
                new Vector3(x, 0.08f, z + inwardZ * 0.065f),
                new Vector3(0.045f, 0.025f, 0.16f), Card);
        }

        private void LateUpdate()
        {
            if (cardPivot == null) return;
            if (groundedPurchase)
            {
                cardPivot.localPosition = cardRestPosition;
                cardPivot.rotation = Quaternion.Euler(78f, 0f, 0f);
                cardPivot.localScale = Vector3.one;
                return;
            }

            punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * 0.9f);
            float bob = Mathf.Sin(Time.unscaledTime * 2.6f + phase) * 0.025f;
            cardPivot.localPosition = cardRestPosition + Vector3.up * bob;
            // Tables may face either aisle; the receipt must never inherit a
            // half-turn and present its amount upside down.
            cardPivot.rotation = Quaternion.Euler(55f, 0f, 0f);
            cardPivot.localScale = Vector3.one * (1f + punch);
        }
    }
}
