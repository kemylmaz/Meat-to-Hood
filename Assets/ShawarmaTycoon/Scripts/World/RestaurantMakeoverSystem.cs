using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Converts the abstract shop-completion percentage into a restaurant the
    /// player can see growing. Every tier is a complete visual direction: floor,
    /// wainscot, sign, lighting, dining rug and dressing change together.
    /// Gameplay roots and colliders never move, so the makeover cannot invalidate
    /// navigation or a player-authored build-mode layout.
    /// </summary>
    public sealed class RestaurantMakeoverSystem : MonoBehaviour
    {
        private static readonly float[] Thresholds = { 0f, 0.15f, 0.38f, 0.65f, 0.90f };
        private static readonly string[] Names =
        {
            string.Empty,
            "MAHALLE DÖNERCİSİ",
            "SICAK BİSTRO",
            "YEŞİL LOKANTA",
            "USTA RESTORANI",
            "MEAT & EAT İMZA"
        };

        private static readonly string[] ShortNames =
        {
            string.Empty, "MAHALLE", "BİSTRO", "YEŞİL", "USTA", "İMZA"
        };

        private readonly GameObject[] stages = new GameObject[6];
        private readonly List<CustomerTable> tables = new();
        private readonly List<GameObject[]> expansionFloorThemes = new();
        private DioramaWorld world;
        private Transform player;
        private int currentTier;
        private bool configured;

        public static RestaurantMakeoverSystem Instance { get; private set; }
        public static int CurrentTier => Instance != null
            ? Instance.currentTier
            : GetTierForRatio(UpgradeProgress.MakeoverRatio);
        public static event Action<int> TierChanged;

        public static int GetTierForRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            for (int tier = 5; tier >= 2; tier--)
                if (ratio >= Thresholds[tier - 1]) return tier;
            return 1;
        }

        public static string TierName(int tier) => Names[Mathf.Clamp(tier, 1, 5)];
        public static string TierShortName(int tier) => ShortNames[Mathf.Clamp(tier, 1, 5)];

        public void Configure(
            DioramaWorld restaurantWorld,
            IReadOnlyList<CustomerTable> restaurantTables,
            Transform playerTransform)
        {
            world = restaurantWorld;
            player = playerTransform;
            tables.Clear();
            if (restaurantTables != null)
                for (int i = 0; i < restaurantTables.Count; i++)
                    if (restaurantTables[i] != null) tables.Add(restaurantTables[i]);

            Instance = this;
            BuildStages();
            UpgradeProgress.Changed += OnProgressChanged;
            configured = true;
            ApplyTier(GetTierForRatio(UpgradeProgress.MakeoverRatio), false);
        }

        private void OnDestroy()
        {
            UpgradeProgress.Changed -= OnProgressChanged;
            if (Instance == this) Instance = null;
        }

        private void OnProgressChanged()
        {
            if (!configured) return;
            ApplyTier(GetTierForRatio(UpgradeProgress.MakeoverRatio), true);
        }

#if UNITY_EDITOR
        /// <summary>Editor-only visual review; does not touch progress or saves.</summary>
        public void PreviewTier(int tier) => ApplyTier(tier, false);
#endif

        private void ApplyTier(int tier, bool celebrate)
        {
            tier = Mathf.Clamp(tier, 1, 5);
            if (tier == currentTier && currentTier != 0) return;

            int previous = currentTier;
            currentTier = tier;
            for (int i = 1; i < stages.Length; i++)
                if (stages[i] != null) stages[i].SetActive(i == tier);

            for (int module = 0; module < expansionFloorThemes.Count; module++)
            for (int i = 2; i <= 5; i++)
                if (expansionFloorThemes[module][i] != null)
                    expansionFloorThemes[module][i].SetActive(i == tier);

            for (int i = 0; i < tables.Count; i++)
                tables[i]?.ApplyMakeoverTier(tier);

            TierChanged?.Invoke(tier);
            if (!celebrate || previous <= 0 || tier <= previous) return;

            Vector3 celebration = player != null
                ? player.position + Vector3.up * 0.25f
                : Vector3.up * 0.25f;
            UnlockCelebration.Spawn(celebration);
            RestaurantLevelCard.Spawn(celebration + Vector3.up * 1.9f, tier);
            AudioDirector.Play(GameSfx.Reward, 0.9f, 1f + tier * 0.035f);
        }

        private void BuildStages()
        {
            if (world == null) return;

            GameObject root = new("Restoran Görsel Seviyeleri");
            root.transform.SetParent(transform, false);
            for (int tier = 1; tier <= 5; tier++)
            {
                GameObject stage = new($"Seviye {tier} - {TierName(tier)}");
                stage.transform.SetParent(root.transform, false);
                stages[tier] = stage;
                BuildStage(stage.transform, tier);
                stage.SetActive(false);
            }

            BuildExpansionFloorThemes();
        }

        private void BuildExpansionFloorThemes()
        {
            expansionFloorThemes.Clear();
            if (world.ExpansionModules == null) return;

            for (int moduleIndex = 0; moduleIndex < world.ExpansionModules.Count; moduleIndex++)
            {
                DioramaModule module = world.ExpansionModules[moduleIndex];
                GameObject[] themes = new GameObject[6];
                expansionFloorThemes.Add(themes);
                if (module == null || module.ContentRoot == null || module.SurfaceRoot == null) continue;

                BoxCollider surface = module.SurfaceRoot.GetComponent<BoxCollider>();
                if (surface == null) continue;
                Vector3 size = surface.size;

                for (int tier = 2; tier <= 5; tier++)
                {
                    GetExpansionPalette(tier, out Color floor, out Color accent);
                    GameObject root = new($"Kanat Zemin Teması SV.{tier}");
                    root.transform.SetParent(module.ContentRoot, false);
                    themes[tier] = root;

                    GameObject panel = PrototypeVisuals.CreatePrimitive("Kanat Zemini", PrimitiveType.Cube,
                        root.transform, new Vector3(0f, world.DeckTopY + 0.016f, 0f),
                        new Vector3(size.x - 0.20f, 0.026f, size.z - 0.20f), floor);
                    DisableCollider(panel);
                    CreateStrip(root.transform, "Kanat İç Çıtası",
                        new Vector3(0f, world.DeckTopY + 0.035f, 0f),
                        new Vector3(size.x - 0.62f, 0.012f, 0.075f), accent);
                    root.SetActive(false);
                }
            }
        }

        private static void GetExpansionPalette(int tier, out Color floor, out Color accent)
        {
            switch (tier)
            {
                case 2:
                    floor = new Color(0.83f, 0.66f, 0.48f);
                    accent = new Color(0.70f, 0.42f, 0.29f);
                    break;
                case 3:
                    floor = new Color(0.74f, 0.58f, 0.39f);
                    accent = new Color(0.55f, 0.38f, 0.24f);
                    break;
                case 4:
                    floor = new Color(0.56f, 0.37f, 0.24f);
                    accent = new Color(0.38f, 0.23f, 0.16f);
                    break;
                default:
                    floor = new Color(0.42f, 0.27f, 0.20f);
                    accent = new Color(0.24f, 0.15f, 0.12f);
                    break;
            }
        }

        private void BuildStage(Transform stage, int tier)
        {
            Color floor;
            Color floorAccent;
            Color wall;
            Color trim;
            Color textile;

            switch (tier)
            {
                case 2:
                    floor = new Color(0.83f, 0.66f, 0.48f);
                    floorAccent = new Color(0.70f, 0.42f, 0.29f);
                    wall = new Color(0.95f, 0.82f, 0.66f);
                    trim = new Color(0.82f, 0.37f, 0.25f);
                    textile = new Color(0.84f, 0.38f, 0.27f);
                    break;
                case 3:
                    floor = new Color(0.74f, 0.58f, 0.39f);
                    floorAccent = new Color(0.55f, 0.38f, 0.24f);
                    wall = new Color(0.72f, 0.82f, 0.62f);
                    trim = new Color(0.27f, 0.58f, 0.45f);
                    textile = new Color(0.28f, 0.64f, 0.52f);
                    break;
                case 4:
                    floor = new Color(0.56f, 0.37f, 0.24f);
                    floorAccent = new Color(0.38f, 0.23f, 0.16f);
                    wall = new Color(0.94f, 0.86f, 0.71f);
                    trim = new Color(0.85f, 0.54f, 0.20f);
                    textile = new Color(0.24f, 0.55f, 0.52f);
                    break;
                case 5:
                    floor = new Color(0.42f, 0.27f, 0.20f);
                    floorAccent = new Color(0.24f, 0.15f, 0.12f);
                    wall = new Color(0.96f, 0.90f, 0.79f);
                    trim = new Color(0.95f, 0.67f, 0.20f);
                    textile = new Color(0.70f, 0.20f, 0.18f);
                    break;
                default:
                    floor = new Color(0.74f, 0.68f, 0.59f);
                    floorAccent = new Color(0.55f, 0.38f, 0.28f);
                    wall = new Color(0.90f, 0.82f, 0.72f);
                    trim = new Color(0.80f, 0.33f, 0.24f);
                    textile = new Color(0.72f, 0.30f, 0.24f);
                    break;
            }

            BuildEntrance(stage, tier, trim, wall);
            if (tier == 1)
            {
                BuildWelcomeMat(stage, textile);
                return;
            }

            BuildFloor(stage, tier, floor, floorAccent);
            BuildWallTheme(stage, wall, trim);
            BuildDiningRug(stage, tier, textile);
            BuildLighting(stage, tier, trim);
            BuildDecor(stage, tier, trim);
        }

        private void BuildFloor(Transform parent, int tier, Color floor, Color accent)
        {
            Vector3 centre = world.BaseModule.transform.localPosition;
            float y = world.DeckTopY + 0.016f;
            GameObject surface = PrototypeVisuals.CreatePrimitive(
                "Tema Zemini", PrimitiveType.Cube, parent,
                new Vector3(centre.x, y, centre.z),
                new Vector3(world.DeckSize.x - 0.46f, 0.026f, world.DeckSize.y - 0.46f), floor);
            DisableCollider(surface);

            float halfX = (world.DeckSize.x - 0.58f) * 0.5f;
            float halfZ = (world.DeckSize.y - 0.58f) * 0.5f;
            CreateStrip(parent, "Kuzey Bordür", new Vector3(centre.x, y + 0.016f, centre.z + halfZ),
                new Vector3(halfX * 2f, 0.018f, 0.14f), accent);
            CreateStrip(parent, "Güney Bordür", new Vector3(centre.x, y + 0.016f, centre.z - halfZ),
                new Vector3(halfX * 2f, 0.018f, 0.14f), accent);
            CreateStrip(parent, "Batı Bordür", new Vector3(centre.x - halfX, y + 0.016f, centre.z),
                new Vector3(0.14f, 0.018f, halfZ * 2f), accent);
            CreateStrip(parent, "Doğu Bordür", new Vector3(centre.x + halfX, y + 0.016f, centre.z),
                new Vector3(0.14f, 0.018f, halfZ * 2f), accent);

            int lines = tier >= 4 ? 13 : 9;
            for (int i = 1; i < lines; i++)
            {
                float z = centre.z - halfZ + (halfZ * 2f) * i / lines;
                CreateStrip(parent, "Zemin Çizgisi", new Vector3(centre.x, y + 0.012f, z),
                    new Vector3(halfX * 2f, 0.010f, tier == 3 ? 0.028f : 0.045f),
                    Color.Lerp(floor, accent, tier >= 4 ? 0.52f : 0.34f));
            }
        }

        private void BuildWallTheme(Transform parent, Color wall, Color trim)
        {
            Vector3 centre = world.BaseModule.transform.localPosition;
            float panelY = world.DeckTopY + 0.67f;
            float back = world.BackWallZ - 0.205f;
            float west = centre.x - world.DeckSize.x * 0.5f + 0.205f;

            CreateStrip(parent, "Arka Duvar Lambri", new Vector3(centre.x, panelY, back),
                new Vector3(world.DeckSize.x - 0.55f, 1.30f, 0.055f), wall);
            CreateStrip(parent, "Batı Duvar Lambri", new Vector3(west, panelY, centre.z),
                new Vector3(0.055f, 1.30f, world.DeckSize.y - 0.55f), wall);
            CreateStrip(parent, "Arka Duvar Çıtası", new Vector3(centre.x, panelY + 0.69f, back - 0.025f),
                new Vector3(world.DeckSize.x - 0.48f, 0.11f, 0.095f), trim);
            CreateStrip(parent, "Batı Duvar Çıtası", new Vector3(west + 0.025f, panelY + 0.69f, centre.z),
                new Vector3(0.095f, 0.11f, world.DeckSize.y - 0.48f), trim);
        }

        private void BuildEntrance(Transform parent, int tier, Color accent, Color panel)
        {
            Vector3 centre = world.BaseModule.transform.localPosition;
            float entranceX = centre.x + world.DeckSize.x * 0.5f - 3.6f;
            float front = centre.z - world.DeckSize.y * 0.5f + 0.02f;
            float signY = world.DeckTopY + (tier >= 4 ? 2.52f : 2.34f);
            float width = tier == 1 ? 2.7f : tier >= 4 ? 4.1f : 3.45f;

            CreateStrip(parent, "Dış Tabela Gölgesi",
                new Vector3(entranceX + 0.05f, signY - 0.05f, front - 0.10f),
                new Vector3(width + 0.12f, 0.76f, 0.16f), new Color(0.28f, 0.17f, 0.13f));
            CreateStrip(parent, "Dış Tabela",
                new Vector3(entranceX, signY, front - 0.14f),
                new Vector3(width, 0.66f, 0.12f), accent);

            // The facade is branding, not another floating status label. The
            // progression comes from the richer frame/tent/bulbs around it.
            TextMesh title = CreateSignText("MEAT & EAT", parent,
                new Vector3(entranceX, signY + 0.01f, front - 0.215f),
                tier >= 4 ? 0.045f : 0.038f, Color.white);
            title.fontStyle = FontStyle.Bold;

            if (tier >= 2)
            {
                GameObject canopy = new("Çizgili Tente");
                canopy.transform.SetParent(parent, false);
                canopy.transform.localPosition = new Vector3(
                    entranceX, world.DeckTopY + 1.83f, front - 0.28f);
                for (int i = 0; i < 7; i++)
                {
                    Color stripe = i % 2 == 0 ? panel : accent;
                    CreateStrip(canopy.transform, "Tente Şeridi",
                        new Vector3((i - 3f) * 0.47f, 0f, 0f),
                        new Vector3(0.45f, 0.10f, 0.78f), stripe);
                }
            }

            if (tier >= 3)
            {
                CreateExteriorPlanter(parent, new Vector3(entranceX - 2.05f, world.DeckTopY, front - 0.34f), 0f);
                CreateExteriorPlanter(parent, new Vector3(entranceX + 2.05f, world.DeckTopY, front - 0.34f), 0f);
            }

            if (tier >= 4)
            {
                for (int i = 0; i < 7; i++)
                    PrototypeVisuals.CreatePrimitive("Tabela Ampulü", PrimitiveType.Sphere, parent,
                        new Vector3(entranceX - width * 0.42f + i * width * 0.14f,
                            signY + 0.39f, front - 0.23f), Vector3.one * 0.10f,
                        new Color(1f, 0.76f, 0.22f));
            }

            if (tier >= 5)
            {
                CreateStrip(parent, "İmza Sol", new Vector3(entranceX - width * 0.56f, signY, front - 0.17f),
                    new Vector3(0.13f, 0.98f, 0.13f), new Color(0.96f, 0.69f, 0.18f));
                CreateStrip(parent, "İmza Sağ", new Vector3(entranceX + width * 0.56f, signY, front - 0.17f),
                    new Vector3(0.13f, 0.98f, 0.13f), new Color(0.96f, 0.69f, 0.18f));
            }
        }

        private void BuildWelcomeMat(Transform parent, Color color)
        {
            Vector3 centre = world.BaseModule.transform.localPosition;
            float entranceX = centre.x + world.DeckSize.x * 0.5f - 3.6f;
            float front = centre.z - world.DeckSize.y * 0.5f + 1.12f;
            CreateStrip(parent, "Hoş Geldin Paspası",
                new Vector3(entranceX, world.DeckTopY + 0.025f, front),
                new Vector3(2.1f, 0.035f, 0.92f), color);
        }

        private void BuildDiningRug(Transform parent, int tier, Color textile)
        {
            Vector3 centre = world.BaseModule.transform.localPosition;
            GameObject rug = MeshyVisuals.TryAttach(parent, "210_decor_carpet",
                new Vector3(8.3f, 0.055f, 5.8f),
                centre + new Vector3(-6.2f, world.DeckTopY + 0.035f, -0.3f),
                Vector3.zero, true);
            if (rug == null)
                CreateStrip(parent, "Yemek Alanı Halısı",
                    centre + new Vector3(-6.2f, world.DeckTopY + 0.035f, -0.3f),
                    new Vector3(8.3f, 0.035f, 5.8f), textile);

            if (tier < 4) return;
            CreateStrip(parent, "Halı İç Çerçeve",
                centre + new Vector3(-6.2f, world.DeckTopY + 0.066f, -0.3f),
                new Vector3(7.7f, 0.015f, 0.10f), new Color(0.96f, 0.72f, 0.26f));
        }

        private void BuildLighting(Transform parent, int tier, Color accent)
        {
            Vector3 centre = world.BaseModule.transform.localPosition;
            Vector3[] positions =
            {
                centre + new Vector3(-8.0f, 2.62f, -0.3f),
                centre + new Vector3(-4.4f, 2.62f, -0.3f),
                centre + new Vector3(1.0f, 2.62f, -2.8f),
                centre + new Vector3(6.3f, 2.62f, -2.8f),
                centre + new Vector3(-8.0f, 2.62f, 3.1f),
                centre + new Vector3(6.3f, 2.62f, 3.1f)
            };

            int count = Mathf.Clamp(tier, 2, 5) + 1;
            for (int i = 0; i < count; i++)
            {
                Vector3 position = positions[i];
                GameObject fixture = new("Sıcak Sarkıt");
                fixture.transform.SetParent(parent, false);
                fixture.transform.localPosition = position;
                MeshyVisuals.TryAttach(fixture.transform, "212_decor_ceiling_lamp",
                    new Vector3(0.62f, 0.72f, 0.62f), Vector3.zero, Vector3.zero);
                PrototypeVisuals.CreatePrimitive("Sıcak Ampul", PrimitiveType.Sphere,
                    fixture.transform, Vector3.down * 0.20f, Vector3.one * 0.13f,
                    new Color(1f, 0.72f, 0.25f));

                Light light = fixture.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.68f, 0.40f);
                light.intensity = tier >= 4 ? 1.65f : 1.25f;
                light.range = tier >= 4 ? 5.8f : 4.7f;
                light.shadows = LightShadows.None;
            }
        }

        private void BuildDecor(Transform parent, int tier, Color accent)
        {
            Vector3 centre = world.BaseModule.transform.localPosition;
            Vector3[] plantPositions =
            {
                centre + new Vector3(-10.1f, world.DeckTopY, 3.8f),
                centre + new Vector3(-10.1f, world.DeckTopY, -4.6f),
                centre + new Vector3(1.7f, world.DeckTopY, 3.7f),
                centre + new Vector3(1.8f, world.DeckTopY, -5.4f)
            };
            int plantCount = Mathf.Clamp(tier - 1, 1, 4);
            for (int i = 0; i < plantCount; i++)
                MeshyVisuals.TryAttach(parent, "73_planter", new Vector3(0.72f, 1.05f, 0.72f),
                    plantPositions[i], Vector3.zero);

            float west = centre.x - world.DeckSize.x * 0.5f + 0.28f;
            MeshyVisuals.TryAttach(parent, "154_shop_menu", new Vector3(1.35f, 1.2f, 0.16f),
                new Vector3(west, world.DeckTopY + 1.42f, centre.z + 2.7f),
                new Vector3(0f, 90f, 0f), true);
            if (tier >= 3)
                MeshyVisuals.TryAttach(parent, "154_shop_menu", new Vector3(1.35f, 1.2f, 0.16f),
                    new Vector3(west, world.DeckTopY + 1.42f, centre.z - 2.8f),
                    new Vector3(0f, 90f, 0f), true);

            if (tier >= 4)
            {
                CreateStrip(parent, "Dekoratif Duvar Şeridi",
                    new Vector3(west + 0.04f, world.DeckTopY + 2.05f, centre.z),
                    new Vector3(0.08f, 0.09f, world.DeckSize.y - 1.4f), accent);
                MeshyVisuals.TryAttach(parent, "213_decor_floor_lamp",
                    new Vector3(0.72f, 1.65f, 0.72f),
                    centre + new Vector3(-1.0f, world.DeckTopY, -5.5f), Vector3.zero);
            }
        }

        private static void CreateExteriorPlanter(Transform parent, Vector3 position, float yaw)
        {
            MeshyVisuals.TryAttach(parent, "73_planter", new Vector3(0.74f, 1.08f, 0.74f),
                position, new Vector3(0f, yaw, 0f));
        }

        private static TextMesh CreateSignText(
            string text, Transform parent, Vector3 position, float size, Color color)
        {
            GameObject label = new("Tabela Yazısı");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.font = UI.UITheme.DisplayFont;
            mesh.fontSize = 72;
            mesh.characterSize = size;
            mesh.color = color;
            Renderer renderer = mesh.GetComponent<Renderer>();
            if (renderer != null && mesh.font != null)
            {
                renderer.sharedMaterial = mesh.font.material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return mesh;
        }

        private static void CreateStrip(
            Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject strip = PrototypeVisuals.CreatePrimitive(
                name, PrimitiveType.Cube, parent, position, scale, color);
            DisableCollider(strip);
        }

        private static void DisableCollider(GameObject visual)
        {
            if (visual == null) return;
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }
    }

    /// <summary>A compact world-space card for the rare restaurant-tier change.</summary>
    public sealed class RestaurantLevelCard : MonoBehaviour
    {
        private float life = 2.4f;
        private Vector3 restScale;

        public static void Spawn(Vector3 position, int tier)
        {
            GameObject root = new("Restoran Seviye Kartı");
            root.transform.position = position;
            root.transform.localEulerAngles = new Vector3(55f, 0f, 0f);
            RestaurantLevelCard card = root.AddComponent<RestaurantLevelCard>();

            PrototypeVisuals.CreatePrimitive("Gölge", PrimitiveType.Cube, root.transform,
                new Vector3(0.04f, -0.05f, 0.04f), new Vector3(2.65f, 0.82f, 0.055f),
                new Color(0.28f, 0.16f, 0.12f));
            PrototypeVisuals.CreatePrimitive("Kart", PrimitiveType.Cube, root.transform,
                Vector3.zero, new Vector3(2.55f, 0.75f, 0.06f),
                new Color(0.94f, 0.49f, 0.32f));

            GameObject label = new("Yazı");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = $"SV. {tier}  {RestaurantMakeoverSystem.TierShortName(tier)}";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.font = UI.UITheme.DisplayFont;
            text.fontSize = 64;
            text.characterSize = 0.030f;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            Renderer renderer = text.GetComponent<Renderer>();
            if (renderer != null && text.font != null)
                renderer.sharedMaterial = text.font.material;
            card.restScale = root.transform.localScale;
            root.transform.localScale = card.restScale * 0.2f;
        }

        private void Update()
        {
            life -= Time.deltaTime;
            float age = 2.4f - life;
            float inT = Mathf.Clamp01(age / 0.28f);
            float outT = Mathf.Clamp01(life / 0.36f);
            float bounce = 1f + Mathf.Sin(inT * Mathf.PI) * 0.08f;
            transform.localScale = restScale * Mathf.Min(inT * bounce, outT);
            transform.position += Vector3.up * (0.18f * Time.deltaTime);
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
