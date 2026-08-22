using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// A small physical receipt card for uncollected takings. The banknote icon
    /// carries the meaning first; the amount is supporting data rather than raw
    /// text floating over the floor.
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
        private Vector3 cardRestPosition;
        private float punch;
        private float phase;

        public string AmountText => amountLabel != null ? amountLabel.text : string.Empty;

        public static WorldCashMarker Create(Transform parent)
        {
            GameObject marker = new("Para Kartı");
            marker.transform.SetParent(parent, false);
            WorldCashMarker result = marker.AddComponent<WorldCashMarker>();
            result.BuildVisuals();
            return result;
        }

        public void SetAmount(int amount)
        {
            if (amountLabel == null) return;
            amountLabel.text = amount > 0 ? "₺" + amount : string.Empty;
            punch = amount > 0 ? 0.12f : 0f;
        }

        private void BuildVisuals()
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
