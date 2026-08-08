using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ShawarmaPrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField, Min(0)] private int startingCoins;

        private Transform runtimeRoot;
        private Transform playerTransform;
        private MobilePlayerController playerMotor;
        private CarryInventory inventory;

        private void Awake()
        {
            if (buildOnAwake) BuildPrototype();
        }

        [ContextMenu("Build Prototype")]
        public void BuildPrototype()
        {
            if (GameObject.Find("Shawarma Prototype Runtime") != null)
                return;

            ConfigureMobileRuntime();
            ConfigureCameraAndLighting();

            GameObject root = new("Shawarma Prototype Runtime");
            runtimeRoot = root.transform;

            GameEconomy economy = root.AddComponent<GameEconomy>();
            economy.Configure(startingCoins);

            BuildFloatingWorld();
            CreatePlayer();

            ItemStation meatSource = CreateStation(
                "ET DEPOSU", new Vector3(-7f, 0.25f, 2.7f), new Vector3(2.5f, 0.9f, 2.0f),
                new Color(0.74f, 0.39f, 0.26f), StationMode.Source,
                ItemType.None, ItemType.RawMeat, 0.5f, 1, 16, 0.65f);
            DecorateMeatSource(meatSource.transform);

            ItemStation oven = CreateStation(
                "OCAK", new Vector3(-3.5f, 0.25f, 2.7f), new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.88f, 0.45f, 0.20f), StationMode.Processor,
                ItemType.RawMeat, ItemType.CookedMeat, 2.2f, 10, 10, 1f);
            DecorateOven(oven.transform);

            ItemStation cutting = CreateStation(
                "KESİM", new Vector3(0f, 0.25f, 2.7f), new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.65f, 0.70f, 0.67f), StationMode.Processor,
                ItemType.CookedMeat, ItemType.SlicedMeat, 1.15f, 10, 10, 1f);
            DecorateCuttingCounter(cutting.transform);

            ItemStation wrap = CreateStation(
                "DÜRÜM", new Vector3(3.5f, 0.25f, 2.7f), new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.91f, 0.70f, 0.30f), StationMode.Processor,
                ItemType.SlicedMeat, ItemType.Wrap, 0.9f, 10, 10, 1f);
            DecorateWrapCounter(wrap.transform);

            ItemStation service = CreateStation(
                "SERVİS", new Vector3(7f, 0.25f, 2.7f), new Vector3(2.2f, 0.9f, 1.9f),
                PrototypeVisuals.Teal, StationMode.Service,
                ItemType.Wrap, ItemType.None, 0.1f, 1, 14, 1f);

            List<CustomerTable> tables = new()
            {
                CreateTable(runtimeRoot, "Masa 1", new Vector3(3.6f, 0.25f, -3.2f)),
                CreateTable(runtimeRoot, "Masa 2", new Vector3(6.5f, 0.25f, -3.2f))
            };

            List<GameObject> expansionModules = new();
            GameObject moduleOne = CreateExpansionModule(
                "Genişleme 1", new Vector3(11f, 0f, -3.5f), new Vector3(4f, 0.5f, 7f));
            CustomerTable tableThree = CreateTable(moduleOne.transform, "Masa 3", new Vector3(0f, 0.25f, 0f));
            expansionModules.Add(moduleOne);
            tables.Add(tableThree);

            GameObject moduleTwo = CreateExpansionModule(
                "Genişleme 2", new Vector3(15f, 0f, -3.5f), new Vector3(4f, 0.5f, 7f));
            CustomerTable tableFour = CreateTable(moduleTwo.transform, "Masa 4", new Vector3(0f, 0.25f, 0f));
            expansionModules.Add(moduleTwo);
            tables.Add(tableFour);

            moduleOne.SetActive(false);
            moduleTwo.SetActive(false);

            DioramaExpansion expansion = root.AddComponent<DioramaExpansion>();
            expansion.Configure(playerMotor, expansionModules, new[] { 12.8f, 16.8f });

            GameObject upgradeRoot = new("Masa Genişletme Alanı");
            upgradeRoot.transform.SetParent(runtimeRoot, false);
            upgradeRoot.transform.localPosition = new Vector3(7f, 0.27f, 5.3f);
            PrototypeVisuals.CreatePrimitive(
                "Upgrade Pad", PrimitiveType.Cylinder, upgradeRoot.transform,
                Vector3.zero, new Vector3(1.05f, 0.05f, 1.05f), PrototypeVisuals.Green);
            UpgradePad upgradePad = upgradeRoot.AddComponent<UpgradePad>();
            upgradePad.Configure(playerTransform, expansion, 60);

            Transform entry = CreateMarker("Müşteri Girişi", new Vector3(8f, 0.88f, -6.1f));
            Transform exit = CreateMarker("Müşteri Çıkışı", new Vector3(8.5f, 0.88f, -6.4f));
            Transform queueFront = CreateMarker("Kuyruk Başı", new Vector3(7f, 0.88f, 0.45f));

            GameObject customerRoot = new("Müşteriler");
            customerRoot.transform.SetParent(runtimeRoot, false);
            CustomerManager customerManager = customerRoot.AddComponent<CustomerManager>();
            customerManager.Configure(service, entry, exit, queueFront, Vector3.back, tables);

            PrototypeHUD hud = root.AddComponent<PrototypeHUD>();
            hud.Configure(inventory);

            Debug.Log("[ShawarmaTycoon] Prototype ready: source → oven → cutting → wrap → service → tables → cleaning.");
        }

        private static void ConfigureMobileRuntime()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
#if !UNITY_EDITOR
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
        }

        private static void ConfigureCameraAndLighting()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 16.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.90f, 0.95f);
            camera.transform.position = new Vector3(0f, 20f, -22f);
            camera.transform.LookAt(Vector3.zero);

            Light light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
            }

            RenderSettings.ambientLight = new Color(0.72f, 0.67f, 0.63f);
            QualitySettings.shadowDistance = 40f;
        }

        private void BuildFloatingWorld()
        {
            GameObject island = new("Başlangıç Adası");
            island.transform.SetParent(runtimeRoot, false);
            CreateIslandGeometry(island.transform, new Vector3(18f, 0.5f, 14f));

            Vector3[] cloudPositions =
            {
                new(-8.5f, -2.4f, -6f),
                new(-6f, -2.8f, 7f),
                new(7f, -2.6f, 7f),
                new(9f, -2.3f, -5f)
            };

            foreach (Vector3 position in cloudPositions)
            {
                GameObject cloud = new("Bulut");
                cloud.transform.SetParent(runtimeRoot, false);
                cloud.transform.localPosition = position;
                PrototypeVisuals.CreatePrimitive("Cloud A", PrimitiveType.Sphere, cloud.transform, Vector3.zero,
                    new Vector3(2.3f, 0.65f, 1.2f), new Color(0.95f, 0.98f, 1f));
                PrototypeVisuals.CreatePrimitive("Cloud B", PrimitiveType.Sphere, cloud.transform, new Vector3(1.2f, 0.1f, 0f),
                    new Vector3(1.5f, 0.55f, 1.0f), new Color(0.95f, 0.98f, 1f));
            }

            CreateBoundaryRail(new Vector3(0f, 0.75f, 6.75f), new Vector3(18f, 1.0f, 0.22f));
            CreateBoundaryRail(new Vector3(-8.75f, 0.75f, 0f), new Vector3(0.22f, 1.0f, 14f));
        }

        private void CreateIslandGeometry(Transform parent, Vector3 topScale)
        {
            PrototypeVisuals.CreatePrimitive(
                "Island Top", PrimitiveType.Cube, parent,
                Vector3.zero, topScale, PrototypeVisuals.IslandTop, colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive(
                "Island Underside", PrimitiveType.Cube, parent,
                new Vector3(0f, -1.05f, 0f),
                new Vector3(topScale.x * 0.90f, 1.65f, topScale.z * 0.90f),
                PrototypeVisuals.IslandSide);
        }

        private void CreateBoundaryRail(Vector3 position, Vector3 scale)
        {
            GameObject rail = PrototypeVisuals.CreatePrimitive(
                "Island Rail", PrimitiveType.Cube, runtimeRoot,
                position, scale, new Color(0.62f, 0.34f, 0.24f), colliderEnabled: true);
            rail.isStatic = true;
        }

        private void CreatePlayer()
        {
            GameObject player = new("Player");
            player.transform.SetParent(runtimeRoot, false);
            player.transform.localPosition = new Vector3(-6.8f, 0.26f, -2.1f);
            playerTransform = player.transform;

            PrototypeVisuals.CreatePrimitive(
                "Body", PrimitiveType.Capsule, player.transform,
                new Vector3(0f, 0.82f, 0f), new Vector3(0.62f, 0.80f, 0.62f),
                new Color(0.20f, 0.48f, 0.68f));
            PrototypeVisuals.CreatePrimitive(
                "Apron", PrimitiveType.Cube, player.transform,
                new Vector3(0f, 0.82f, 0.31f), new Vector3(0.48f, 0.72f, 0.08f),
                new Color(0.88f, 0.30f, 0.22f));

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.82f, 0f);
            controller.height = 1.64f;
            controller.radius = 0.34f;
            controller.stepOffset = 0.25f;

            playerMotor = player.AddComponent<MobilePlayerController>();
            playerMotor.Configure(4.6f, new Vector2(-8.35f, -6.35f), new Vector2(8.35f, 6.35f));

            inventory = player.AddComponent<CarryInventory>();
            inventory.Configure(12);
        }

        private ItemStation CreateStation(
            string stationName,
            Vector3 position,
            Vector3 bodyScale,
            Color bodyColor,
            StationMode mode,
            ItemType input,
            ItemType output,
            float duration,
            int inputCapacity,
            int outputCapacity,
            float refillInterval)
        {
            GameObject stationObject = new(stationName);
            stationObject.transform.SetParent(runtimeRoot, false);
            stationObject.transform.localPosition = position;

            PrototypeVisuals.CreatePrimitive(
                "Counter", PrimitiveType.Cube, stationObject.transform,
                new Vector3(0f, 0.45f, 0f), bodyScale, bodyColor, colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive(
                "Work Top", PrimitiveType.Cube, stationObject.transform,
                new Vector3(0f, 0.93f, 0f),
                new Vector3(bodyScale.x * 1.04f, 0.12f, bodyScale.z * 1.04f),
                PrototypeVisuals.Cream);

            ItemStation station = stationObject.AddComponent<ItemStation>();
            station.Configure(
                stationName, mode, input, output, playerTransform, inventory,
                duration, inputCapacity, outputCapacity, refillInterval);
            return station;
        }

        private static void DecorateMeatSource(Transform station)
        {
            PrototypeVisuals.CreatePrimitive("Rack Back", PrimitiveType.Cube, station,
                new Vector3(0f, 1.55f, 0.72f), new Vector3(2.2f, 1.8f, 0.14f),
                new Color(0.38f, 0.22f, 0.17f));
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    PrototypeVisuals.CreateItemVisual(
                        ItemType.RawMeat,
                        station,
                        new Vector3(-0.72f + column * 0.48f, 1.12f + row * 0.17f, 0.48f),
                        0.9f);
                }
            }
        }

        private static void DecorateOven(Transform station)
        {
            PrototypeVisuals.CreatePrimitive("Heater Left", PrimitiveType.Cube, station,
                new Vector3(-0.75f, 1.55f, 0.25f), new Vector3(0.22f, 1.9f, 0.65f),
                new Color(0.28f, 0.23f, 0.22f));
            PrototypeVisuals.CreatePrimitive("Heater Right", PrimitiveType.Cube, station,
                new Vector3(0.75f, 1.55f, 0.25f), new Vector3(0.22f, 1.9f, 0.65f),
                new Color(0.28f, 0.23f, 0.22f));
            PrototypeVisuals.CreatePrimitive("Doner Spit", PrimitiveType.Cylinder, station,
                new Vector3(0f, 1.55f, 0.25f), new Vector3(0.42f, 0.86f, 0.42f),
                PrototypeVisuals.CookedMeat);
        }

        private static void DecorateCuttingCounter(Transform station)
        {
            PrototypeVisuals.CreatePrimitive("Cutting Board", PrimitiveType.Cube, station,
                new Vector3(0f, 1.03f, 0f), new Vector3(0.95f, 0.06f, 0.72f),
                new Color(0.68f, 0.42f, 0.22f));
            PrototypeVisuals.CreatePrimitive("Knife", PrimitiveType.Cube, station,
                new Vector3(0.35f, 1.13f, 0f), new Vector3(0.65f, 0.05f, 0.12f),
                new Color(0.72f, 0.76f, 0.78f), new Vector3(0f, 25f, 0f));
        }

        private static void DecorateWrapCounter(Transform station)
        {
            PrototypeVisuals.CreatePrimitive("Lavash", PrimitiveType.Cylinder, station,
                new Vector3(0f, 1.05f, 0f), new Vector3(0.48f, 0.025f, 0.48f),
                PrototypeVisuals.Wrap);
            PrototypeVisuals.CreatePrimitive("Greens", PrimitiveType.Sphere, station,
                new Vector3(-0.35f, 1.16f, 0f), new Vector3(0.20f, 0.12f, 0.20f),
                new Color(0.30f, 0.70f, 0.30f));
        }

        private CustomerTable CreateTable(Transform parent, string tableName, Vector3 localPosition)
        {
            GameObject tableObject = new(tableName);
            tableObject.transform.SetParent(parent, false);
            tableObject.transform.localPosition = localPosition;

            PrototypeVisuals.CreatePrimitive("Table Top", PrimitiveType.Cube, tableObject.transform,
                new Vector3(0f, 0.72f, 0f), new Vector3(1.75f, 0.16f, 1.15f),
                new Color(0.56f, 0.32f, 0.20f), colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Table Leg", PrimitiveType.Cube, tableObject.transform,
                new Vector3(0f, 0.35f, 0f), new Vector3(0.28f, 0.70f, 0.28f),
                new Color(0.35f, 0.22f, 0.18f), colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Chair", PrimitiveType.Cube, tableObject.transform,
                new Vector3(0f, 0.30f, -1.05f), new Vector3(0.75f, 0.55f, 0.70f),
                new Color(0.28f, 0.58f, 0.56f), colliderEnabled: true);

            GameObject seat = new("Customer Seat");
            seat.transform.SetParent(tableObject.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.63f, -1.05f);
            seat.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            CustomerTable table = tableObject.AddComponent<CustomerTable>();
            table.Configure(playerTransform, seat.transform);
            return table;
        }

        private GameObject CreateExpansionModule(string moduleName, Vector3 position, Vector3 scale)
        {
            GameObject module = new(moduleName);
            module.transform.SetParent(runtimeRoot, false);
            module.transform.localPosition = position;
            CreateIslandGeometry(module.transform, scale);
            return module;
        }

        private Transform CreateMarker(string markerName, Vector3 position)
        {
            GameObject marker = new(markerName);
            marker.transform.SetParent(runtimeRoot, false);
            marker.transform.localPosition = position;
            return marker.transform;
        }
    }
}
