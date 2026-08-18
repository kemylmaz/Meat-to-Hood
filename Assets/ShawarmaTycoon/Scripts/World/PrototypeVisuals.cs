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
        public static readonly Color Drink = new(0.26f, 0.56f, 0.86f);
        public static readonly Color Dessert = new(0.86f, 0.52f, 0.66f);
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
                ItemType.Drink => Drink,
                ItemType.Dessert => Dessert,
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

        /// <summary>
        /// The Food Kit model each carried item is drawn as, and how tall it is
        /// drawn. Heights are kept low enough that a stack of them still reads as
        /// a stack rather than a tower.
        /// </summary>
        /// <summary>
        /// Quarter turn for the long items. The food kit lays a sandwich, a steak
        /// and a sausage along their own Z, which points them straight into an
        /// order bubble and leaves the customer asking for an end-on sliver.
        /// Turned, they run across the bubble and across the tray.
        /// </summary>
        private const float SideOn = 90f;

        private static readonly Dictionary<ItemType, (string Id, float Height, float Yaw)> ItemModels = new()
        {
            // The line's three states, in the order the shop makes them: a raw
            // steak off the rack, cut into cubes at the spit, wrapped at the board.
            { ItemType.RawMeat, ("232_food_meat_raw", 0.045f, SideOn) },
            { ItemType.CookedMeat, ("187_shop_steak_cubes", 0.115f, 0f) },
            { ItemType.SlicedMeat, ("234_food_meat_sliced", 0.045f, SideOn) },
            { ItemType.Wrap, ("230_food_wrap", 0.094f, SideOn) },
            { ItemType.Drink, ("235_food_soda", 0.17f, 0f) },
            { ItemType.Dessert, ("238_food_cake", 0.13f, 0f) },
            // The shop's rubbish is dirty crockery, so the model is literally
            // what the player is carrying to the sink.
            { ItemType.Trash, ("171_shop_plate_dirty", 0.028f, 0f) }
        };

        /// <summary>
        /// Per item, resolved once and kept: the uniform scale that draws the
        /// prefab at its declared height, and the box it then occupies once the
        /// quarter turn is applied.
        /// </summary>
        private static readonly Dictionary<ItemType, (float Uniform, Vector3 Display)> ModelSizes = new();

        /// <summary>
        /// How big one of each thing is drawn. Public so effects that animate a
        /// stack know what size to settle it back to, and so stacking code can
        /// space items by their own height.
        ///
        /// With a model behind the item this is the model's own box at its drawn
        /// height, which is what keeps it from being squashed into a primitive's
        /// proportions.
        /// </summary>
        public static Vector3 ItemSize(ItemType type)
        {
            return TryMeasureModel(type, out float _, out Vector3 display)
                ? display
                : PrimitiveItemSize(type);
        }

        private static Vector3 PrimitiveItemSize(ItemType type) => type switch
        {
            ItemType.RawMeat => new Vector3(0.52f, 0.13f, 0.34f),
            ItemType.CookedMeat => new Vector3(0.48f, 0.12f, 0.31f),
            ItemType.SlicedMeat => new Vector3(0.40f, 0.09f, 0.28f),
            ItemType.Wrap => new Vector3(0.18f, 0.38f, 0.18f),
            ItemType.Trash => new Vector3(0.30f, 0.30f, 0.30f),
            ItemType.Drink => new Vector3(0.20f, 0.17f, 0.20f),
            ItemType.Dessert => new Vector3(0.32f, 0.14f, 0.32f),
            _ => Vector3.one * 0.2f
        };

        /// <summary>
        /// Vertical pitch for a column of these. Read off the item rather than
        /// fixed, because the pack draws a dirty plate three centimetres thick and
        /// a drink six times that, and one spacing cannot suit both.
        /// </summary>
        public static float StackStep(ItemType type, float scale = 1f)
        {
            return Mathf.Max(0.05f, ItemSize(type).y * scale * 1.15f);
        }

        public static GameObject CreateItemVisual(ItemType type, Transform parent, Vector3 localPosition, float scale = 1f)
        {
            Vector3 size = ItemSize(type) * scale;
            GameObject modelled = TryCreateModelVisual(type, parent, localPosition, size);
            if (modelled != null) return modelled;

            PrimitiveType primitive = type switch
            {
                ItemType.Wrap => PrimitiveType.Capsule,
                ItemType.Trash => PrimitiveType.Sphere,
                // A cup and a slice: the two are told apart at a glance by shape as
                // well as colour, because they share a bubble over a customer's head.
                ItemType.Drink => PrimitiveType.Cylinder,
                _ => PrimitiveType.Cube
            };

            Vector3 rotation = type == ItemType.Wrap ? new Vector3(90f, 0f, 0f) : Vector3.zero;
            return CreatePrimitive(type.ToString(), primitive, parent, localPosition,
                size, ItemColor(type), rotation);
        }

        /// <summary>
        /// Builds the item as its Food Kit model under a holder that still scales
        /// exactly like the primitive did.
        ///
        /// Everything that animates one of these - the pop, the belt parcels -
        /// drives the holder's localScale, and <see cref="ItemSize"/> is
        /// non-uniform, so a middle node divides that back out. The two multiply
        /// to a uniform scale, which is what stops a sandwich being stretched into
        /// the shape of the box it replaced, and it is also why the quarter turn
        /// goes on a third node below: a rotation between a non-uniform scale and
        /// the mesh would shear it.
        /// </summary>
        private static GameObject TryCreateModelVisual(
            ItemType type, Transform parent, Vector3 localPosition, Vector3 size)
        {
            if (!ItemModels.TryGetValue(type, out (string Id, float Height, float Yaw) model)) return null;
            if (!TryMeasureModel(type, out float uniform, out Vector3 display)) return null;
            if (size.x <= 0.0001f || size.y <= 0.0001f || size.z <= 0.0001f) return null;

            GameObject prefab = Resources.Load<GameObject>("PolyPrefabs/" + model.Id);
            if (prefab == null) return null;

            GameObject root = new(type.ToString());
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localScale = size;

            // Divides the holder's box back out and multiplies the model's own
            // scale in, leaving whatever hangs below it at a uniform scale.
            GameObject fit = new("Fit");
            fit.transform.SetParent(root.transform, false);
            fit.transform.localScale = new Vector3(
                uniform / display.x, uniform / display.y, uniform / display.z);
            // The prefabs stand on a bottom-centre pivot; a carried item is placed
            // by its middle, so it is dropped half its own height.
            fit.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            GameObject visual = Object.Instantiate(prefab, fit.transform, false);
            visual.name = model.Id;
            visual.transform.localRotation = Quaternion.Euler(0f, model.Yaw, 0f);

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            return root;
        }

        private static bool TryMeasureModel(ItemType type, out float uniform, out Vector3 display)
        {
            if (ModelSizes.TryGetValue(type, out (float Uniform, Vector3 Display) cached))
            {
                uniform = cached.Uniform;
                display = cached.Display;
                return display.sqrMagnitude > 0f;
            }

            uniform = 0f;
            display = Vector3.zero;
            if (ItemModels.TryGetValue(type, out (string Id, float Height, float Yaw) model))
            {
                GameObject prefab = Resources.Load<GameObject>("PolyPrefabs/" + model.Id);
                if (prefab != null)
                {
                    Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    bool found = false;
                    Bounds bounds = default;
                    foreach (Renderer renderer in renderers)
                    {
                        if (!found)
                        {
                            bounds = renderer.bounds;
                            found = true;
                        }
                        else bounds.Encapsulate(renderer.bounds);
                    }
                    if (found && bounds.size.y > 0.0001f)
                    {
                        uniform = model.Height / bounds.size.y;
                        Vector3 drawn = bounds.size * uniform;
                        display = Mathf.Abs(Mathf.DeltaAngle(model.Yaw, 90f)) < 45f
                            ? new Vector3(drawn.z, drawn.y, drawn.x)
                            : drawn;
                    }
                }
            }

            ModelSizes[type] = (uniform, display);
            return display.sqrMagnitude > 0f;
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
