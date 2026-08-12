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
            root.AddComponent<RushHourSystem>();
            root.AddComponent<ComboSystem>();

            BuildFloatingWorld();
            CreatePlayer();

            ItemStation meatSource = CreateStation(
                "ET DEPOSU", new Vector3(-8f, 0.25f, 8.15f), new Vector3(2.5f, 0.9f, 2.0f),
                new Color(0.74f, 0.39f, 0.26f), StationMode.Source,
                ItemType.None, ItemType.RawMeat, 0.5f, 1, 16, 0.65f);
            DecorateMeatSource(meatSource.transform);
            MeshyVisuals.TryReplaceDirect(
                meatSource.transform, "04_meat_storage_rack", new Vector3(2.4f, 2.25f, 1.75f),
                Vector3.zero, new Vector3(0f, 180f, 0f), false,
                "Counter", "Work Top", "Rack Back", "RawMeat");
            meatSource.SetVisualLayout(
                new Vector3(-0.48f, 0.25f, -1.05f), new Vector3(0.48f, 0.25f, -1.05f), 2.9f);

            ItemStation oven = CreateStation(
                "OCAK", new Vector3(-4f, 0.25f, 8.15f), new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.88f, 0.45f, 0.20f), StationMode.Processor,
                ItemType.RawMeat, ItemType.CookedMeat, 2.2f, 10, 10, 1f);
            DecorateOven(oven.transform);
            MeshyVisuals.TryReplaceDirect(
                oven.transform, "06_shawarma_rotisserie", new Vector3(1.37f, 2.25f, 1.0f),
                Vector3.zero, new Vector3(0f, 180f, 0f), false,
                "Counter", "Work Top", "Heater Left", "Heater Right", "Doner Spit");
            oven.SetVisualLayout(
                new Vector3(-0.34f, 1.27f, -0.42f), new Vector3(0.34f, 1.27f, -0.42f), 2.55f);

            ItemStation cutting = CreateStation(
                "KESİM", new Vector3(0f, 0.25f, 8.15f), new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.65f, 0.70f, 0.67f), StationMode.Processor,
                ItemType.CookedMeat, ItemType.SlicedMeat, 1.15f, 10, 10, 1f);
            DecorateCuttingCounter(cutting.transform);
            MeshyVisuals.TryReplaceDirect(
                cutting.transform, "08_cutting_station", new Vector3(2.05f, 1.2f, 1.75f),
                Vector3.zero, new Vector3(0f, 180f, 0f), false,
                "Counter", "Work Top", "Cutting Board", "Knife");
            cutting.SetVisualLayout(
                new Vector3(-0.50f, 1.42f, -0.38f), new Vector3(0.50f, 1.42f, -0.38f), 1.9f);

            ItemStation wrap = CreateStation(
                "DÜRÜM", new Vector3(4f, 0.25f, 8.15f), new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.91f, 0.70f, 0.30f), StationMode.Processor,
                ItemType.SlicedMeat, ItemType.Wrap, 0.9f, 10, 10, 1f);
            DecorateWrapCounter(wrap.transform);
            MeshyVisuals.TryReplaceDirect(
                wrap.transform, "10_wrap_preparation_station", new Vector3(2.05f, 1.2f, 1.75f),
                Vector3.zero, new Vector3(0f, 180f, 0f), false,
                "Counter", "Work Top", "Lavash", "Greens");
            wrap.SetVisualLayout(
                new Vector3(-0.50f, 1.42f, -0.38f), new Vector3(0.50f, 1.42f, -0.38f), 1.9f);

            ItemStation service = CreateStation(
                "SERVİS", new Vector3(8f, 0.25f, 8.15f), new Vector3(2.2f, 0.9f, 1.9f),
                PrototypeVisuals.Teal, StationMode.Service,
                ItemType.Wrap, ItemType.None, 0.1f, 1, 14, 1f);
            MeshyVisuals.TryReplaceDirect(
                service.transform, "12_service_cashier_counter", new Vector3(2.05f, 1.2f, 1.75f),
                Vector3.zero, new Vector3(0f, 180f, 0f), false,
                "Counter", "Work Top");
            service.SetVisualLayout(
                new Vector3(-0.50f, 1.42f, -0.38f), new Vector3(0.50f, 1.42f, -0.38f), 1.9f);

            GameObject trashBinObject = new("Çöp Kutusu");
            trashBinObject.transform.SetParent(runtimeRoot, false);
            trashBinObject.transform.localPosition = new Vector3(-10.2f, 0.25f, -1.1f);
            PrototypeVisuals.CreatePrimitive("Çöp Gövdesi", PrimitiveType.Cube, trashBinObject.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.82f, 0.96f, 0.70f), new Color(0.34f, 0.52f, 0.43f),
                colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Çöp Kapak", PrimitiveType.Cube, trashBinObject.transform,
                new Vector3(0f, 1.04f, 0f), new Vector3(0.91f, 0.12f, 0.79f), new Color(0.20f, 0.34f, 0.28f));
            PrototypeVisuals.CreatePrimitive("Çöp Açıklığı", PrimitiveType.Cube, trashBinObject.transform,
                new Vector3(0f, 0.83f, -0.36f), new Vector3(0.50f, 0.22f, 0.035f), new Color(0.10f, 0.16f, 0.14f));
            PrototypeVisuals.CreatePrimitive("Ayak Pedalı", PrimitiveType.Cube, trashBinObject.transform,
                new Vector3(0f, 0.08f, -0.42f), new Vector3(0.32f, 0.08f, 0.22f), new Color(0.88f, 0.68f, 0.26f));
            TrashBin trashBin = trashBinObject.AddComponent<TrashBin>();
            trashBin.Configure(playerTransform, inventory);
            MeshyVisuals.TryReplaceDirect(
                trashBinObject.transform, "17_trash_bin", new Vector3(0.70f, 1.05f, 0.70f),
                Vector3.zero, Vector3.zero, false,
                "Çöp Gövdesi", "Çöp Kapak", "Çöp Açıklığı", "Ayak Pedalı");

            GameObject takeawayRoot = new("Takeaway Counter");
            takeawayRoot.transform.SetParent(runtimeRoot, false);
            takeawayRoot.transform.localPosition = new Vector3(-9.45f, 0.25f, -7.05f);
            TakeawaySystem takeaway = takeawayRoot.AddComponent<TakeawaySystem>();
            takeaway.Configure(playerTransform, inventory);

            GameObject takeawayUnlockObject = new("Takeaway Unlock Pad");
            takeawayUnlockObject.transform.SetParent(runtimeRoot, false);
            takeawayUnlockObject.transform.localPosition = new Vector3(-9.45f, 0.28f, -5.60f);
            PrototypeVisuals.CreatePrimitive(
                "Takeaway Upgrade Pad", PrimitiveType.Cylinder, takeawayUnlockObject.transform,
                Vector3.zero, new Vector3(0.82f, 0.06f, 0.82f), new Color(0.95f, 0.58f, 0.20f));
            ManagementRoomUnlockPad takeawayUnlock = takeawayUnlockObject.AddComponent<ManagementRoomUnlockPad>();
            takeawayUnlock.Configure(
                playerTransform, takeawayRoot, 180, "takeaway.unlocked", "TAKEAWAY");

            meatSource.SetWorldLabelVisible(false);
            oven.SetWorldLabelVisible(false);
            cutting.SetWorldLabelVisible(false);
            wrap.SetWorldLabelVisible(false);
            service.SetWorldLabelVisible(false);

            ConveyorLink rawBelt = CreateConveyor("Et Bandı", meatSource, oven);
            ConveyorLink ovenBelt = CreateConveyor("Ocak Bandı", oven, cutting);
            ConveyorLink cutBelt = CreateConveyor("Kesim Bandı", cutting, wrap);
            ConveyorLink wrapBelt = CreateConveyor("Dürüm Bandı", wrap, service);

            GameObject managementRoot = new("Yönetim Ofisleri");
            managementRoot.transform.SetParent(runtimeRoot, false);
            CreateManagementArea(managementRoot.transform, "HR", new Vector3(-6.4f, 0.25f, -3.3f), new Color(0.35f, 0.55f, 0.88f));
            CreateManagementArea(managementRoot.transform, "GM", new Vector3(-6.4f, 0.25f, -5.1f), new Color(0.75f, 0.43f, 0.70f));
            CreateManagementPad(managementRoot.transform, "Ocak İşçisi", new Vector3(-5.1f, 0.28f, -3.3f), StationUpgradeType.Worker, 35, oven, null);
            CreateManagementPad(managementRoot.transform, "Kesim İşçisi", new Vector3(-3.7f, 0.28f, -3.3f), StationUpgradeType.Worker, 55, cutting, null);
            CreateManagementPad(managementRoot.transform, "Dürüm İşçisi", new Vector3(-2.3f, 0.28f, -3.3f), StationUpgradeType.Worker, 75, wrap, null);
            CreateManagementPad(managementRoot.transform, "Et Bandı", new Vector3(-5.1f, 0.28f, -5.1f), StationUpgradeType.Conveyor, 45, null, rawBelt);
            CreateManagementPad(managementRoot.transform, "Ocak Bandı", new Vector3(-3.7f, 0.28f, -5.1f), StationUpgradeType.Conveyor, 65, null, ovenBelt);
            CreateManagementPad(managementRoot.transform, "Kesim Bandı", new Vector3(-2.3f, 0.28f, -5.1f), StationUpgradeType.Conveyor, 85, null, cutBelt);
            CreateManagementPad(managementRoot.transform, "Dürüm Bandı", new Vector3(-0.9f, 0.28f, -5.1f), StationUpgradeType.Conveyor, 105, null, wrapBelt);
            CreatePlayerUpgradePad(managementRoot.transform, "Hız Geliştirmesi", new Vector3(0.1f, 0.28f, -3.3f), PlayerUpgradeType.MoveSpeed, 55);
            CreatePlayerUpgradePad(managementRoot.transform, "Kapasite Geliştirmesi", new Vector3(1.5f, 0.28f, -3.3f), PlayerUpgradeType.CarryCapacity, 65);
            managementRoot.SetActive(false);

            GameObject officePad = new("Ofis Açma Alanı");
            officePad.transform.SetParent(runtimeRoot, false);
            officePad.transform.localPosition = new Vector3(-5.2f, 0.28f, -4.2f);
            PrototypeVisuals.CreatePrimitive("Ofis Upgrade Pad", PrimitiveType.Cylinder, officePad.transform, Vector3.zero,
                new Vector3(0.75f, 0.06f, 0.75f), new Color(0.55f, 0.38f, 0.88f));
            ManagementRoomUnlockPad officeUnlock = officePad.AddComponent<ManagementRoomUnlockPad>();
            officeUnlock.Configure(playerTransform, managementRoot, 120);
            officePad.SetActive(false);
            managementRoot.SetActive(false);

            GameObject hrWing = new("HR Wing");
            hrWing.transform.SetParent(runtimeRoot, false);
            ManagementOfficeTerminal hrTerminal = CreateOfficeRoom(hrWing.transform, "HR Manager Office", new Vector3(-6.15f, 0.25f, -4.4f),
                new Color(0.28f, 0.56f, 0.91f));
            ManagementOfficeTerminal recruitTerminal = CreateOfficeRoom(hrWing.transform, "Recruit Office", new Vector3(-2.75f, 0.25f, -4.4f),
                new Color(0.32f, 0.70f, 0.64f));

            GameObject gmWing = new("GM Office");
            gmWing.transform.SetParent(runtimeRoot, false);
            ManagementOfficeTerminal gmTerminal = CreateOfficeRoom(gmWing.transform, "General Manager Office", new Vector3(0.65f, 0.25f, -4.4f),
                new Color(0.76f, 0.45f, 0.72f));
            hrWing.SetActive(false);
            gmWing.SetActive(false);

            CreateOfficeUnlockPad("Unlock HR and Recruit", new Vector3(-6.15f, 0.28f, -6.10f), new Color(0.28f, 0.56f, 0.91f),
                playerTransform, hrWing, 120, "office.hr", "HR + RECRUIT\n$120");
            CreateOfficeUnlockPad("Unlock GM", new Vector3(0.65f, 0.28f, -6.10f), new Color(0.76f, 0.45f, 0.72f),
                playerTransform, gmWing, 200, "office.gm", "GM OFFICE\n$200");


            List<CustomerTable> tables = new()
            {
                CreateTable(runtimeRoot, "Masa 1", new Vector3(0.8f, 0.25f, -0.55f)),
                CreateTable(runtimeRoot, "Masa 2", new Vector3(4.5f, 0.25f, -0.55f))
            };

            List<GameObject> expansionModules = new();
            GameObject moduleOne = CreateExpansionModule(
                "Genişleme 1", new Vector3(16f, 0f, -3f), new Vector3(8f, 0.5f, 14f));
            CustomerTable tableThree = CreateTable(moduleOne.transform, "Masa 3", new Vector3(-1.9f, 0.25f, -2.8f));
            CustomerTable tableFour = CreateTable(moduleOne.transform, "Masa 4", new Vector3(1.9f, 0.25f, -2.8f));
            expansionModules.Add(moduleOne);
            tables.Add(tableThree);
            tables.Add(tableFour);

            GameObject moduleTwo = CreateExpansionModule(
                "Genişleme 2", new Vector3(16f, 0f, 6.5f), new Vector3(8f, 0.5f, 5f));
            CustomerTable tableFive = CreateTable(moduleTwo.transform, "Masa 5", new Vector3(-1.9f, 0.25f, 0f));
            CustomerTable tableSix = CreateTable(moduleTwo.transform, "Masa 6", new Vector3(1.9f, 0.25f, 0f));
            expansionModules.Add(moduleTwo);
            tables.Add(tableFive);
            tables.Add(tableSix);

            GameObject previewOne = CreateLockedExpansionPlot("Locked Dining Wing", new Vector3(16f, 0.03f, -3f), new Vector3(8f, 0.5f, 14f));
            GameObject previewTwo = CreateLockedExpansionPlot("Locked Office Wing", new Vector3(16f, 0.03f, 6.5f), new Vector3(8f, 0.5f, 5f));

            moduleOne.SetActive(false);
            moduleTwo.SetActive(false);

            DioramaExpansion expansion = root.AddComponent<DioramaExpansion>();
            expansion.Configure(playerMotor, expansionModules, new[] { previewOne, previewTwo }, new[] { 19f, 19f });

            GameObject upgradeRoot = new("Masa Genişletme Alanı");
            upgradeRoot.transform.SetParent(runtimeRoot, false);
            upgradeRoot.transform.localPosition = new Vector3(10.25f, 0.27f, 1.0f);
            PrototypeVisuals.CreatePrimitive(
                "Upgrade Pad", PrimitiveType.Cylinder, upgradeRoot.transform,
                Vector3.zero, new Vector3(1.05f, 0.05f, 1.05f), PrototypeVisuals.Green);
            UpgradePad upgradePad = upgradeRoot.AddComponent<UpgradePad>();
            upgradePad.Configure(playerTransform, expansion, 60);

            Transform entry = CreateMarker("Müşteri Girişi", new Vector3(10.0f, 0.25f, -8.35f));
            Transform exit = CreateMarker("Müşteri Çıkışı", new Vector3(11.15f, 0.25f, -8.15f));
            Transform queueFront = CreateMarker("Kuyruk Başı", new Vector3(8f, 0.25f, 6.35f));

            GameObject customerRoot = new("Müşteriler");
            customerRoot.transform.SetParent(runtimeRoot, false);
            CreateCustomerEntrance(entry.position);
            CustomerManager customerManager = customerRoot.AddComponent<CustomerManager>();
            customerManager.Configure(service, entry, exit, queueFront, Vector3.back, tables);

            FloorSpillSystem floorSpills = root.AddComponent<FloorSpillSystem>();
            floorSpills.Configure(playerTransform, tables);

            HumanResourcesSystem humanResources = root.AddComponent<HumanResourcesSystem>();
            humanResources.Configure(playerTransform, new[] { rawBelt, ovenBelt, cutBelt, wrapBelt });
            PlayerUpgradeSystem playerUpgrades = root.AddComponent<PlayerUpgradeSystem>();
            playerUpgrades.Configure(playerTransform, playerMotor, inventory);
            RecruitmentSystem recruitment = root.AddComponent<RecruitmentSystem>();
            recruitment.Configure(customerManager, wrap, service, takeaway, floorSpills, runtimeRoot);
            ManagementMenuHUD managementHud = root.AddComponent<ManagementMenuHUD>();
            managementHud.Configure(humanResources, playerUpgrades, recruitment);
            hrTerminal.Configure(playerTransform, managementHud, ManagementMenu.HumanResources, "HR UPGRADE");
            gmTerminal.Configure(playerTransform, managementHud, ManagementMenu.GeneralManager, "GM UPGRADE");
            recruitTerminal.Configure(playerTransform, managementHud, ManagementMenu.Recruiting, "RECRUIT");

            PrototypeHUD hud = root.AddComponent<PrototypeHUD>();
            hud.Configure(inventory);
            root.AddComponent<DailyTasksHUD>();
            root.AddComponent<TycoonStatusHUD>();
            root.AddComponent<GameSessionPersistence>();

            GameObject tutorial = new("Öğretici Ok");
            tutorial.transform.SetParent(runtimeRoot, false);
            PrototypeVisuals.CreatePrimitive("Ok Gövdesi", PrimitiveType.Cylinder, tutorial.transform, Vector3.zero,
                new Vector3(0.20f, 0.52f, 0.20f), new Color(1f, 0.82f, 0.16f));
            PrototypeVisuals.CreatePrimitive("Ok Ucu", PrimitiveType.Sphere, tutorial.transform, Vector3.down * 0.38f,
                new Vector3(0.46f, 0.24f, 0.46f), new Color(1f, 0.82f, 0.16f));
            TutorialArrow tutorialArrow = tutorial.AddComponent<TutorialArrow>();
            tutorialArrow.Configure(inventory, meatSource.transform, oven.transform, cutting.transform, wrap.transform, service.transform);

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
            camera.orthographicSize = 5.4f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.90f, 0.95f);
            camera.transform.position = new Vector3(9.8f, 16.5f, -11.6f);
            camera.transform.LookAt(new Vector3(0.8f, 0f, -0.6f));

            MobileCameraRig cameraRig = camera.GetComponent<MobileCameraRig>();
            if (cameraRig == null) cameraRig = camera.gameObject.AddComponent<MobileCameraRig>();
            cameraRig.Configure(camera);

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
            CreateIslandGeometry(island.transform, new Vector3(24f, 0.5f, 20f));

            Vector3[] cloudPositions =
            {
                new(-13f, -2.4f, -9f),
                new(-11f, -2.8f, 10f),
                new(12f, -2.6f, 10f),
                new(13f, -2.3f, -8f)
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

            CreateBoundaryRail(new Vector3(0f, 0.75f, 9.75f), new Vector3(24f, 1.0f, 0.22f));
            CreateBoundaryRail(new Vector3(-11.75f, 0.75f, 0f), new Vector3(0.22f, 1.0f, 20f));
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

            CreateFloorGrid(parent, topScale);
        }

        private static void CreateFloorGrid(Transform parent, Vector3 floorSize)
        {
            const float spacing = 1.5f;
            Color grout = new(0.73f, 0.55f, 0.41f);
            float halfWidth = floorSize.x * 0.5f;
            float halfDepth = floorSize.z * 0.5f;

            for (float x = -halfWidth + spacing; x < halfWidth; x += spacing)
            {
                PrototypeVisuals.CreatePrimitive(
                    "Floor Grout X", PrimitiveType.Cube, parent,
                    new Vector3(x, floorSize.y * 0.5f + 0.006f, 0f),
                    new Vector3(0.025f, 0.012f, floorSize.z - 0.08f), grout);
            }

            for (float z = -halfDepth + spacing; z < halfDepth; z += spacing)
            {
                PrototypeVisuals.CreatePrimitive(
                    "Floor Grout Z", PrimitiveType.Cube, parent,
                    new Vector3(0f, floorSize.y * 0.5f + 0.006f, z),
                    new Vector3(floorSize.x - 0.08f, 0.012f, 0.025f), grout);
            }
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
            player.transform.localPosition = new Vector3(-6.0f, 0.26f, 2.2f);
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
            playerMotor.Configure(4.6f, new Vector2(-11.4f, -9.4f), new Vector2(11.4f, 9.4f));
            MobileCameraRig cameraRig = Camera.main != null ? Camera.main.GetComponent<MobileCameraRig>() : null;
            if (cameraRig != null)
            {
                cameraRig.SetFollowTarget(player.transform);
                cameraRig.SetFollowBounds(new Vector2(-10.5f, 19f), new Vector2(-9f, 9f));
            }

            inventory = player.AddComponent<CarryInventory>();
            inventory.Configure(12);
            if (MeshyVisuals.TryReplaceDirect(
                    player.transform, "01_player_character", new Vector3(0.75f, 1.70f, 0.85f),
                    Vector3.zero, Vector3.zero, false, "Body", "Apron") &&
                MeshyVisuals.TryFindAnchor(player.transform, "CARRY_ANCHOR", out Transform carryAnchor))
            {
                inventory.SetStackAnchor(carryAnchor);
            }
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

        private ConveyorLink CreateConveyor(string beltName, ItemStation from, ItemStation to)
        {
            Vector3 start = from.transform.position;
            Vector3 end = to.transform.position;
            Vector3 midpoint = (start + end) * 0.5f + Vector3.back * 0.68f + Vector3.up * 0.78f;
            GameObject belt = new(beltName);
            belt.transform.SetParent(runtimeRoot, false);
            belt.transform.position = midpoint;
            float length = Vector3.Distance(start, end) - 1.5f;
            PrototypeVisuals.CreatePrimitive("Bant", PrimitiveType.Cube, belt.transform, Vector3.zero,
                new Vector3(Mathf.Max(0.7f, length), 0.14f, 0.56f), new Color(0.35f, 0.32f, 0.30f));
            ConveyorLink link = belt.AddComponent<ConveyorLink>();
            link.Configure(from, to);
            return link;
        }

        private void CreateManagementArea(Transform parent, string title, Vector3 position, Color color)
        {
            GameObject desk = new(title);
            desk.transform.SetParent(parent, false);
            desk.transform.localPosition = position;
            PrototypeVisuals.CreatePrimitive("Manager Desk", PrimitiveType.Cube, desk.transform, new Vector3(0f, 0.48f, 0f),
                new Vector3(1.5f, 0.9f, 0.8f), color);
            PrototypeVisuals.CreatePrimitive("Manager", PrimitiveType.Capsule, desk.transform, new Vector3(0f, 1.15f, 0.25f),
                new Vector3(0.42f, 0.56f, 0.42f), PrototypeVisuals.Cream);
            PrototypeVisuals.CreateLabel(title, desk.transform, new Vector3(0f, 1.95f, 0f), 0.14f);
        }

        private void CreateManagementPad(Transform parent, string name, Vector3 position, StationUpgradeType type, int cost, ItemStation station, ConveyorLink conveyor)
        {
            GameObject pad = new(name);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = position;
            PrototypeVisuals.CreatePrimitive("Satın Alma Alanı", PrimitiveType.Cylinder, pad.transform, Vector3.zero,
                new Vector3(0.55f, 0.05f, 0.55f), type == StationUpgradeType.Worker ? new Color(0.38f, 0.72f, 0.95f) : new Color(0.75f, 0.48f, 0.85f));
            StationUpgradePad upgrade = pad.AddComponent<StationUpgradePad>();
            upgrade.Configure(playerTransform, type, cost, station, conveyor);
        }

        private void CreatePlayerUpgradePad(Transform parent, string name, Vector3 position, PlayerUpgradeType type, int cost)
        {
            GameObject pad = new(name);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = position;
            PrototypeVisuals.CreatePrimitive("Oyuncu Upgrade", PrimitiveType.Cylinder, pad.transform, Vector3.zero,
                new Vector3(0.55f, 0.05f, 0.55f), new Color(0.28f, 0.68f, 0.92f));
            PlayerUpgradePad upgrade = pad.AddComponent<PlayerUpgradePad>();
            upgrade.Configure(playerTransform, playerMotor, inventory, type, cost);
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

        private ManagementOfficeTerminal CreateOfficeRoom(Transform parent, string roomName, Vector3 position, Color accent)
        {
            GameObject room = new(roomName);
            room.transform.SetParent(parent, false);
            room.transform.localPosition = position;

            Color floor = new Color(0.94f, 0.80f, 0.61f);
            Color wall = new Color(0.98f, 0.91f, 0.76f);
            PrototypeVisuals.CreatePrimitive("Office Floor", PrimitiveType.Cube, room.transform, Vector3.zero,
                new Vector3(2.95f, 0.08f, 2.70f), floor);
            PrototypeVisuals.CreatePrimitive("Back Wall", PrimitiveType.Cube, room.transform, new Vector3(0f, 0.92f, 1.28f),
                new Vector3(2.95f, 1.82f, 0.12f), wall);
            PrototypeVisuals.CreatePrimitive("Left Wall", PrimitiveType.Cube, room.transform, new Vector3(-1.42f, 0.92f, 0f),
                new Vector3(0.12f, 1.82f, 2.70f), wall);
            PrototypeVisuals.CreatePrimitive("Right Wall", PrimitiveType.Cube, room.transform, new Vector3(1.42f, 0.92f, 0f),
                new Vector3(0.12f, 1.82f, 2.70f), wall);
            PrototypeVisuals.CreatePrimitive("Door Beam", PrimitiveType.Cube, room.transform, new Vector3(0f, 1.55f, -1.22f),
                new Vector3(1.35f, 0.16f, 0.16f), accent);
            PrototypeVisuals.CreatePrimitive("Door Left", PrimitiveType.Cube, room.transform, new Vector3(-0.67f, 0.76f, -1.22f),
                new Vector3(0.14f, 1.55f, 0.16f), accent);
            PrototypeVisuals.CreatePrimitive("Door Right", PrimitiveType.Cube, room.transform, new Vector3(0.67f, 0.76f, -1.22f),
                new Vector3(0.14f, 1.55f, 0.16f), accent);

            GameObject desk = new("Manager Desk");
            desk.transform.SetParent(room.transform, false);
            desk.transform.localPosition = new Vector3(0f, 0f, 0.46f);
            PrototypeVisuals.CreatePrimitive("Desk", PrimitiveType.Cube, desk.transform, new Vector3(0f, 0.46f, 0f),
                new Vector3(1.55f, 0.82f, 0.62f), accent);
            PrototypeVisuals.CreatePrimitive("Manager", PrimitiveType.Capsule, desk.transform, new Vector3(0f, 1.10f, 0.32f),
                new Vector3(0.42f, 0.58f, 0.42f), PrototypeVisuals.Cream);
            PrototypeVisuals.CreatePrimitive("Manager Hat", PrimitiveType.Sphere, desk.transform, new Vector3(0f, 1.78f, 0.32f),
                new Vector3(0.48f, 0.14f, 0.48f), accent);

            return desk.AddComponent<ManagementOfficeTerminal>();
        }

        private void CreateOfficeUnlockPad(string padName, Vector3 position, Color color, Transform player, GameObject roomRoot, int cost, string saveKey, string title)
        {
            GameObject pad = new(padName);
            pad.transform.SetParent(runtimeRoot, false);
            pad.transform.localPosition = position;
            PrototypeVisuals.CreatePrimitive("Office Unlock Pad", PrimitiveType.Cylinder, pad.transform, Vector3.zero,
                new Vector3(0.80f, 0.06f, 0.80f), color);
            ManagementRoomUnlockPad unlock = pad.AddComponent<ManagementRoomUnlockPad>();
            unlock.Configure(player, roomRoot, cost, saveKey, title);
        }

        private void CreateCustomerEntrance(Vector3 position)
        {
            GameObject gate = new("Customer Entrance Gate");
            gate.transform.SetParent(runtimeRoot, false);
            gate.transform.position = position;
            Color frame = new Color(0.92f, 0.34f, 0.20f);
            PrototypeVisuals.CreatePrimitive("Entrance Top", PrimitiveType.Cube, gate.transform, new Vector3(0f, 1.72f, 0f),
                new Vector3(2.0f, 0.24f, 0.25f), frame);
            PrototypeVisuals.CreatePrimitive("Entrance Left", PrimitiveType.Cube, gate.transform, new Vector3(-0.90f, 0.82f, 0f),
                new Vector3(0.22f, 1.65f, 0.25f), frame);
            PrototypeVisuals.CreatePrimitive("Entrance Right", PrimitiveType.Cube, gate.transform, new Vector3(0.90f, 0.82f, 0f),
                new Vector3(0.22f, 1.65f, 0.25f), frame);
            TextMesh sign = PrototypeVisuals.CreateLabel("ENTRANCE", gate.transform, new Vector3(0f, 2.55f, 0f), 0.11f);
            sign.color = new Color(0.95f, 0.22f, 0.16f);
        }

        private CustomerTable CreateTable(Transform parent, string tableName, Vector3 localPosition)
        {
            GameObject tableObject = new(tableName);
            tableObject.transform.SetParent(parent, false);
            tableObject.transform.localPosition = localPosition;

            PrototypeVisuals.CreatePrimitive("Table Top", PrimitiveType.Cube, tableObject.transform,
                new Vector3(0f, 0.72f, 0f), new Vector3(1.20f, 0.16f, 0.80f),
                new Color(0.56f, 0.32f, 0.20f), colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Table Leg", PrimitiveType.Cube, tableObject.transform,
                new Vector3(0f, 0.35f, 0f), new Vector3(0.28f, 0.70f, 0.28f),
                new Color(0.35f, 0.22f, 0.18f), colliderEnabled: true);
            CreateDiningChair(tableObject.transform, "Customer Chair", -0.66f, 0f);
            CreateDiningChair(tableObject.transform, "Guest Chair", 0.66f, 180f);

            GameObject seat = new("Customer Seat");
            seat.transform.SetParent(tableObject.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0f, -1.05f);
            seat.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            CustomerTable table = tableObject.AddComponent<CustomerTable>();
            table.Configure(playerTransform, seat.transform);
            MeshyVisuals.TryReplaceDirect(
                tableObject.transform, "15_dining_table_clean", new Vector3(1.44f, 1.08f, 2.12f),
                Vector3.zero, Vector3.zero, false,
                "Table Top", "Table Leg", "Customer Chair", "Guest Chair");
            return table;
        }

        private static void CreateDiningChair(Transform parent, string name, float z, float yaw)
        {
            GameObject chair = new(name);
            chair.transform.SetParent(parent, false);
            chair.transform.localPosition = new Vector3(0f, 0f, z);
            chair.transform.localEulerAngles = new Vector3(0f, yaw, 0f);

            Color seatColor = new(0.28f, 0.64f, 0.61f);
            Color frameColor = new(0.25f, 0.35f, 0.34f);
            PrototypeVisuals.CreatePrimitive("Seat", PrimitiveType.Cube, chair.transform,
                new Vector3(0f, 0.34f, 0f), new Vector3(0.46f, 0.13f, 0.42f), seatColor, colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Back", PrimitiveType.Cube, chair.transform,
                new Vector3(0f, 0.72f, -0.23f), new Vector3(0.46f, 0.72f, 0.10f), seatColor);
            PrototypeVisuals.CreatePrimitive("Leg Left", PrimitiveType.Cube, chair.transform,
                new Vector3(-0.17f, 0.16f, 0f), new Vector3(0.08f, 0.32f, 0.08f), frameColor);
            PrototypeVisuals.CreatePrimitive("Leg Right", PrimitiveType.Cube, chair.transform,
                new Vector3(0.17f, 0.16f, 0f), new Vector3(0.08f, 0.32f, 0.08f), frameColor);
        }

        private GameObject CreateLockedExpansionPlot(string plotName, Vector3 position, Vector3 scale)
        {
            GameObject plot = new(plotName);
            plot.transform.SetParent(runtimeRoot, false);
            plot.transform.localPosition = position;

            Color lockedGround = new(0.38f, 0.34f, 0.31f);
            Color lockGold = new(0.93f, 0.68f, 0.18f);
            PrototypeVisuals.CreatePrimitive("Locked Ground", PrimitiveType.Cube, plot.transform, Vector3.zero,
                new Vector3(scale.x, 0.10f, scale.z), lockedGround, colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Unlock Pad", PrimitiveType.Cylinder, plot.transform, new Vector3(0f, 0.10f, 0f),
                new Vector3(1.20f, 0.06f, 1.20f), lockGold);
            PrototypeVisuals.CreatePrimitive("Lock Body", PrimitiveType.Cube, plot.transform, new Vector3(0f, 0.42f, 0f),
                new Vector3(0.62f, 0.48f, 0.20f), new Color(0.98f, 0.84f, 0.32f));
            PrototypeVisuals.CreatePrimitive("Lock Loop", PrimitiveType.Cylinder, plot.transform, new Vector3(0f, 0.75f, 0f),
                new Vector3(0.34f, 0.24f, 0.16f), new Color(0.98f, 0.84f, 0.32f));
            return plot;
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
