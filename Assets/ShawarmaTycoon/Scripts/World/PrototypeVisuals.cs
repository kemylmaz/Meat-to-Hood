using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public static class PrototypeVisuals
    {
        private static readonly Dictionary<Color, Material> Materials = new();

        public static readonly Color Cream = new(0.96f, 0.82f, 0.64f);
        public static readonly Color IslandTop = new(0.91f, 0.68f, 0.48f);
        public static readonly Color IslandSide = new(0.43f, 0.25f, 0.20f);
        public static readonly Color RawMeat = new(0.72f, 0.23f, 0.18f);
        public static readonly Color CookedMeat = new(0.43f, 0.16f, 0.08f);
        public static readonly Color SlicedMeat = new(0.58f, 0.24f, 0.10f);
        public static readonly Color Wrap = new(0.94f, 0.72f, 0.30f);
        public static readonly Color Teal = new(0.16f, 0.58f, 0.52f);
        public static readonly Color Green = new(0.31f, 0.75f, 0.38f);
        public static readonly Color Red = new(0.85f, 0.28f, 0.24f);
        public static readonly Color Trash = new(0.32f, 0.38f, 0.34f);

        public static Material Material(Color color)
        {
            if (Materials.TryGetValue(color, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new(shader)
            {
                color = color
            };
            material.enableInstancing = true;
            Materials[color] = material;
            return material;
        }

        public static Color ItemColor(ItemType type)
        {
            return type switch
            {
                ItemType.RawMeat => RawMeat,
                ItemType.CookedMeat => CookedMeat,
                ItemType.SlicedMeat => SlicedMeat,
                ItemType.Wrap => Wrap,
                ItemType.Trash => Trash,
                _ => Color.white
            };
        }

        public static GameObject CreatePrimitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Vector3? localEuler = null,
            bool colliderEnabled = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localEulerAngles = localEuler ?? Vector3.zero;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = Material(color);

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = colliderEnabled;
            return go;
        }

        public static GameObject CreateItemVisual(ItemType type, Transform parent, Vector3 localPosition, float scale = 1f)
        {
            PrimitiveType primitive = type == ItemType.Wrap ? PrimitiveType.Capsule : type == ItemType.Trash ? PrimitiveType.Sphere : PrimitiveType.Cube;
            Vector3 size = type switch
            {
                ItemType.RawMeat => new Vector3(0.52f, 0.13f, 0.34f),
                ItemType.CookedMeat => new Vector3(0.48f, 0.12f, 0.31f),
                ItemType.SlicedMeat => new Vector3(0.40f, 0.09f, 0.28f),
                ItemType.Wrap => new Vector3(0.18f, 0.38f, 0.18f),
                ItemType.Trash => new Vector3(0.30f, 0.30f, 0.30f),
                _ => Vector3.one * 0.2f
            };

            Vector3 rotation = type == ItemType.Wrap ? new Vector3(90f, 0f, 0f) : Vector3.zero;
            return CreatePrimitive(type.ToString(), primitive, parent, localPosition, size * scale, ItemColor(type), rotation);
        }

        public static TextMesh CreateLabel(string text, Transform parent, Vector3 localPosition, float size = 0.16f)
        {
            GameObject labelObject = new("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localEulerAngles = new Vector3(55f, 0f, 0f);

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = size;
            label.fontSize = 48;
            label.color = new Color(0.25f, 0.13f, 0.10f);
            return label;
        }
    }
}
