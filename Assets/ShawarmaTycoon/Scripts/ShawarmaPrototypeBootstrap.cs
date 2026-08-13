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
        private CityLayout cityLayout;

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
            AudioDirector.Ensure(runtimeRoot);

            BuildCity();
            CreatePlayer();

            ItemStation meatSource = CreateStation(
                "ET DEPOSU", new Vector3(-6f, 0.25f, 6.3f), new Vector3(2.5f, 0.9f, 2.0f),
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
                "OCAK", new Vector3(-3f, 0.25f, 6.3f), new Vector3(2.2f, 0.9f, 1.9f),
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
                "KESİM", new Vector3(0f, 0.25f, 6.3f), new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.65f, 0.70f, 0.67f), StationMode.Processor,
                ItemType.CookedMeat, ItemType.SlicedMeat, 1.15f, 10, 10, 1f);
            DecorateCuttingCounter(cutting.transform);
            MeshyVisuals.TryReplaceDirect(
                cutting.transform, "08_cutting_station", new Vector3(2.05f, 1.2f, 1.75f),
                Vector3.zero, new Vector3(0f, 180f, 0f), false,
                "Counter", "Work Top", "Cutting Board", "Knife");
            cutting.SetVisualLayout(
                new Vector3(-0.50f, 1.42f, -0.38f), new Vector3(0.50f, 1.42f, -0.38f), 1.9f);
            cutting.SetOutputBatchVisual("75_meat_tray_stack", 4,
                new Vector3(0.46f, 0.34f, 0.36f), 0.35f);

            ItemStation wrap = CreateStation(
                "DÜRÜM", new Vector3(3f, 0.25f, 6.3f), new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.91f, 0.70f, 0.30f), StationMode.Processor,
                ItemType.SlicedMeat, ItemType.Wrap, 0.9f, 10, 10, 1f);
            DecorateWrapCounter(wrap.transform);
            MeshyVisuals.TryReplaceDirect(
                wrap.transform, "10_wrap_preparation_station", new Vector3(2.05f, 1.2f, 1.75f),
                Vector3.zero, new Vector3(0f, 180f, 0f), false,
                "Counter", "Work Top", "Lavash", "Greens");
            wrap.SetVisualLayout(
                new Vector3(-0.50f, 1.42f, -0.38f), new Vector3(0.50f, 1.42f, -0.38f), 1.9f);
            wrap.SetOutputBatchVisual("74_wrap_tray_stack", 4,
                new Vector3(0.46f, 0.44f, 0.36f), 0.45f);

            ItemStation service = CreateStation(
                "SERVİS", new Vector3(6f, 0.25f, 6.3f), new Vector3(2.2f, 0.9f, 1.9f),
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
            trashBinObject.transform.localPosition = new Vector3(-9.4f, 0.25f, 3.2f);
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
            // Same authored counter as the service station: both are a till the
            // customer walks up to, and the window was still a bare grey box.
            // Only the body is replaced - the bag, order light and cash pad on top
            // of it are gameplay state and stay.
            if (MeshyVisuals.TryReplaceDirect(
                    takeawayRoot.transform, "12_service_cashier_counter",
                    new Vector3(2.05f, 1.20f, 1.60f), Vector3.zero, new Vector3(0f, 180f, 0f),
                    false, "Takeaway Counter Body", "Takeaway Counter Top"))
                takeaway.SetCounterTopHeight(1.20f);

            GameObject takeawayUnlockObject = new("Takeaway Unlock Pad");
            takeawayUnlockObject.transform.SetParent(runtimeRoot, false);
            takeawayUnlockObject.transform.localPosition = new Vector3(-9.45f, 0.28f, -5.60f);
            PrototypeVisuals.CreatePrimitive(
                "Takeaway Upgrade Pad", PrimitiveType.Cylinder, takeawayUnlockObject.transform,
                Vector3.zero, new Vector3(0.82f, 0.06f, 0.82f), new Color(0.95f, 0.58f, 0.20f));
            MeshyVisuals.TryReplaceDirect(takeawayUnlockObject.transform, "19_upgrade_pad",
                new Vector3(1.10f, 0.34f, 1.10f), Vector3.down * 0.03f, Vector3.zero,
                false, "Takeaway Upgrade Pad");
            ManagementRoomUnlockPad takeawayUnlock = takeawayUnlockObject.AddComponent<ManagementRoomUnlockPad>();
            takeawayUnlock.Configure(
                playerTransform, takeawayRoot, 280, "takeaway.unlocked", "TAKEAWAY");

            meatSource.SetWorldLabelVisible(false);
            oven.SetWorldLabelVisible(false);
            cutting.SetWorldLabelVisible(false);
            wrap.SetWorldLabelVisible(false);
            service.SetWorldLabelVisible(false);

            ConveyorLink rawBelt = CreateConveyor("Et Bandı", meatSource, oven);
            ConveyorLink ovenBelt = CreateConveyor("Ocak Bandı", oven, cutting);
            ConveyorLink cutBelt = CreateConveyor("Kesim Bandı", cutting, wrap);
            ConveyorLink wrapBelt = CreateConveyor("Dürüm Bandı", wrap, service);

            // One management room replaces the three unlockable office wings.
            // Those wings, and the upgrade pads that used to sit behind them,
            // were both created inactive and never switched on, so every pad in
            // here was unreachable in the shipped build.
            GameObject managementRoot = new("Yönetim Odası");
            managementRoot.transform.SetParent(runtimeRoot, false);
            BuildOfficeFloor(managementRoot.transform);

            ManagementOfficeTerminal hrTerminal = CreateManagerDesk(
                managementRoot.transform, "HR Masası", "25_hr_manager_desk",
                new Vector3(-6.6f, 0.25f, -4.2f));
            ManagementOfficeTerminal recruitTerminal = CreateManagerDesk(
                managementRoot.transform, "İşe Alım Masası", "26_recruitment_desk",
                new Vector3(-3.4f, 0.25f, -4.2f));
            ManagementOfficeTerminal gmTerminal = CreateManagerDesk(
                managementRoot.transform, "GM Masası", "27_general_manager_desk",
                new Vector3(-0.2f, 0.25f, -4.2f));

            // Upgrade pads in front of the desks. Each pad floats a world-space
            // label, so the two rows are staggered and spaced well apart -
            // packed tighter the captions overlap into an unreadable pile.
            // Measured: a well run starting shop takes about 70 coins a minute,
            // rising to roughly 120 once the line is staffed. This board used to
            // total 525 - the whole early game was bought out in three minutes.
            // At the figures below it runs about a quarter of an hour.
            CreateManagementPad(managementRoot.transform, "Ocak İşçisi", new Vector3(-6.8f, 0.28f, -6.0f), StationUpgradeType.Worker, 90, oven, null);
            CreateManagementPad(managementRoot.transform, "Kesim İşçisi", new Vector3(-5.1f, 0.28f, -6.0f), StationUpgradeType.Worker, 150, cutting, null);
            CreateManagementPad(managementRoot.transform, "Dürüm İşçisi", new Vector3(-3.4f, 0.28f, -6.0f), StationUpgradeType.Worker, 210, wrap, null);
            CreatePlayerUpgradePad(managementRoot.transform, "Hız Geliştirmesi", new Vector3(-1.7f, 0.28f, -6.0f), PlayerUpgradeType.MoveSpeed, 55);
            CreatePlayerUpgradePad(managementRoot.transform, "Kapasite Geliştirmesi", new Vector3(0.0f, 0.28f, -6.0f), PlayerUpgradeType.CarryCapacity, 65);
            CreateManagementPad(managementRoot.transform, "Et Bandı", new Vector3(-5.95f, 0.28f, -7.7f), StationUpgradeType.Conveyor, 120, null, rawBelt);
            CreateManagementPad(managementRoot.transform, "Ocak Bandı", new Vector3(-4.25f, 0.28f, -7.7f), StationUpgradeType.Conveyor, 180, null, ovenBelt);
            CreateManagementPad(managementRoot.transform, "Kesim Bandı", new Vector3(-2.55f, 0.28f, -7.7f), StationUpgradeType.Conveyor, 240, null, cutBelt);
            CreateManagementPad(managementRoot.transform, "Dürüm Bandı", new Vector3(-0.85f, 0.28f, -7.7f), StationUpgradeType.Conveyor, 320, null, wrapBelt);

            CreatePlanter(managementRoot.transform, new Vector3(1.5f, 0.25f, -4.4f));
            CreatePlanter(managementRoot.transform, new Vector3(-8.4f, 0.25f, -4.4f));


            // Dining room sits symmetrically in the open middle of the lot, clear
            // of the kitchen line at z = 6.3, the office strip around z = -4 and
            // the walk-in path from the entrance to the queue.
            List<CustomerTable> tables = new()
            {
                CreateTable(runtimeRoot, "Masa 1", new Vector3(-3.4f, 0.25f, 1.3f)),
                CreateTable(runtimeRoot, "Masa 2", new Vector3(3.4f, 0.25f, 1.3f))
            };

            // Both wings are one authored plot each, so their footprints match the
            // model exactly and the two squares butt up against the lot and each
            // other with no seam.
            Vector3 plotScale = new(PlotSpan, 0.5f, PlotSpan);
            Vector3 plotOne = new(12f + PlotSpan * 0.5f, 0f, -3f);
            Vector3 plotTwo = new(plotOne.x, 0f, plotOne.z + PlotSpan);

            List<GameObject> expansionModules = new();
            GameObject moduleOne = CreateExpansionModule("Genişleme 1", plotOne, plotScale);
            CustomerTable tableThree = CreateTable(moduleOne.transform, "Masa 3", new Vector3(-1.4f, 0.25f, 0f));
            CustomerTable tableFour = CreateTable(moduleOne.transform, "Masa 4", new Vector3(1.4f, 0.25f, 0f));
            expansionModules.Add(moduleOne);
            tables.Add(tableThree);
            tables.Add(tableFour);

            GameObject moduleTwo = CreateExpansionModule("Genişleme 2", plotTwo, plotScale);
            CustomerTable tableFive = CreateTable(moduleTwo.transform, "Masa 5", new Vector3(-1.4f, 0.25f, 0f));
            CustomerTable tableSix = CreateTable(moduleTwo.transform, "Masa 6", new Vector3(1.4f, 0.25f, 0f));
            expansionModules.Add(moduleTwo);
            tables.Add(tableFive);
            tables.Add(tableSix);

            GameObject previewOne = CreateLockedExpansionPlot(
                "Locked Dining Wing", plotOne + Vector3.up * 0.03f, plotScale);
            GameObject previewTwo = CreateLockedExpansionPlot(
                "Locked Office Wing", plotTwo + Vector3.up * 0.03f, plotScale);

            moduleOne.SetActive(false);
            moduleTwo.SetActive(false);

            DioramaExpansion expansion = root.AddComponent<DioramaExpansion>();
            float plotEdge = plotOne.x + PlotSpan * 0.5f - 0.6f;
            expansion.Configure(playerMotor, expansionModules, new[] { previewOne, previewTwo },
                new[] { plotEdge, plotEdge });

            GameObject upgradeRoot = new("Masa Genişletme Alanı");
            upgradeRoot.transform.SetParent(runtimeRoot, false);
            upgradeRoot.transform.localPosition = new Vector3(10.25f, 0.27f, 1.0f);
            PrototypeVisuals.CreatePrimitive(
                "Upgrade Pad", PrimitiveType.Cylinder, upgradeRoot.transform,
                Vector3.zero, new Vector3(1.05f, 0.05f, 1.05f), PrototypeVisuals.Green);
            MeshyVisuals.TryReplaceDirect(upgradeRoot.transform, "19_upgrade_pad",
                new Vector3(1.30f, 0.40f, 1.30f), Vector3.down * 0.03f, Vector3.zero,
                false, "Upgrade Pad");
            UpgradePad upgradePad = upgradeRoot.AddComponent<UpgradePad>();
            upgradePad.Configure(playerTransform, expansion, 140);

            // Pedestrians come in from the side street at the front corner; the
            // main road behind the kitchen is for traffic and the future
            // drive-through window.
            Transform entry = CreateMarker("Müşteri Girişi", new Vector3(9.6f, 0.25f, -8.0f));
            Transform exit = CreateMarker("Müşteri Çıkışı", new Vector3(10.8f, 0.25f, -7.8f));
            Transform queueFront = CreateMarker("Kuyruk Başı", new Vector3(6f, 0.25f, 4.5f));

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

            UI.GameHUD hud = UI.GameHUD.Ensure(runtimeRoot);
            hud.Objective.Bind(inventory);
            hud.Objective.BindTables(tables);
            playerMotor.SetJoystick(hud.Joystick);
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
            camera.orthographicSize = 6.8f;
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

        /// <summary>
        /// The restaurant now sits on a real street corner instead of a floating
        /// diorama, which is what makes drive-by traffic possible at all.
        /// </summary>
        private void BuildCity()
        {
            cityLayout = new CityLayout();
            CityBlock.Build(runtimeRoot, cityLayout);

            // Low railings mark the lot edge without hiding the street behind it.
            GameObject backRail = CreateBoundaryRail(
                new Vector3(0f, 0.52f, WallBackZ), new Vector3(24f, 0.55f, 0.20f));
            GameObject sideRail = CreateBoundaryRail(
                new Vector3(WallSideX, 0.52f, 0f), new Vector3(0.20f, 0.55f, 17f));

            if (BuildLotWalls())
            {
                // The authored wall stands in for the railing. The primitive stays
                // alive as an invisible blocker so the player still cannot walk
                // off the lot - the model's own colliders are switched off.
                backRail.GetComponent<Renderer>().enabled = false;
                sideRail.GetComponent<Renderer>().enabled = false;
            }

            TrafficSystem traffic = runtimeRoot.gameObject.AddComponent<TrafficSystem>();
            traffic.Configure(cityLayout);
        }

        /// <summary>Lot edges the perimeter wall runs along.</summary>
        private const float WallBackZ = 8.35f;
        private const float WallSideX = -11.85f;

        /// <summary>
        /// Wall module pitch. The model measures 3.09 across because its skirting
        /// overhangs the body on both sides; the bodies themselves are 3.00, so
        /// tiling on bounds width would open a 9 cm crack between every pair.
        /// </summary>
        private const float WallPitch = 3.00f;

        /// <summary>How far each corner run sits in from the corner piece origin.</summary>
        private const float WallArm = 1.33f;

        /// <summary>Wall base, level with the lot surface.</summary>
        private const float WallBaseY = 0.24f;

        /// <summary>
        /// Cream perimeter wall along the two lot edges that face away from the
        /// camera. The near edges are deliberately left open: the rig sits at
        /// +X / -Z, so a wall there would stand between the camera and the
        /// management pads instead of behind them.
        /// </summary>
        private bool BuildLotWalls()
        {
            if (!CityKit.Has("76_wall_corner") || !CityKit.Has("77_wall_straight"))
                return false;

            GameObject walls = new("Arsa Duvarı");
            walls.transform.SetParent(runtimeRoot, false);

            // The corner model carries its two runs on +X and +Z, so a quarter
            // turn anti-clockwise lands them on the lot's -X and +Z edges.
            CityKit.Spawn("76_wall_corner", walls.transform,
                new Vector3(WallSideX + WallArm, WallBaseY, WallBackZ - WallArm), -90f);

            // Both runs start one module in, where the corner's own arms end.
            TileWall(walls.transform, WallSideX + WallPitch, 12f, true, WallBackZ);
            TileWall(walls.transform, WallBackZ - WallPitch, -8.5f, false, WallSideX);
            return true;
        }

        /// <summary>
        /// Repeats the straight wall between two points on one axis.
        /// <see cref="CityKit.Tile"/> spaces pieces by their bounds width, which
        /// for this model is the skirting, not the body - hence the explicit pitch.
        /// </summary>
        private static void TileWall(
            Transform parent, float from, float to, bool alongX, float fixedCoordinate)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(to - from) / WallPitch));
            float step = (to > from ? WallPitch : -WallPitch);
            float first = (from + to) * 0.5f - step * (count - 1) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float p = first + step * i;
                Vector3 position = alongX
                    ? new Vector3(p, WallBaseY, fixedCoordinate)
                    : new Vector3(fixedCoordinate, WallBaseY, p);
                CityKit.Spawn("77_wall_straight", parent, position, alongX ? 0f : 90f);
            }
        }

        private void CreateIslandGeometry(Transform parent, Vector3 topScale)
        {
            PrototypeVisuals.CreatePrimitive(
                "Lot Floor", PrimitiveType.Cube, parent,
                Vector3.zero, topScale, PrototypeVisuals.IslandTop, colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive(
                "Lot Kerb", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.14f, 0f),
                new Vector3(topScale.x + 0.36f, 0.20f, topScale.z + 0.36f),
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

        private GameObject CreateBoundaryRail(Vector3 position, Vector3 scale)
        {
            GameObject rail = PrototypeVisuals.CreatePrimitive(
                "Island Rail", PrimitiveType.Cube, runtimeRoot,
                position, scale, new Color(0.62f, 0.34f, 0.24f), colliderEnabled: true);
            rail.isStatic = true;
            return rail;
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
            playerMotor.Configure(4.6f, new Vector2(-11.4f, -7.7f), new Vector2(11.4f, 7.7f));
            MobileCameraRig cameraRig = Camera.main != null ? Camera.main.GetComponent<MobileCameraRig>() : null;
            if (cameraRig != null)
            {
                cameraRig.SetFollowTarget(player.transform);
                cameraRig.SetFollowBounds(new Vector2(-10.5f, 19f), new Vector2(-7.5f, 7.5f));
            }

            inventory = player.AddComponent<CarryInventory>();
            // Small enough that the natural batch flows through the line quickly.
            // At 12 the source filled the player up in a second and a half, and
            // since a processor only runs while you stand at it, that meant 26 s
            // parked at the oven and the best part of a minute before the first
            // wrap reached the counter. The capacity upgrade grows this.
            inventory.Configure(6);
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
            float length = Mathf.Max(0.7f, Vector3.Distance(start, end) - 1.5f);

            // The authored belt is a floor standing unit with its own legs, so
            // it sits on the lot rather than floating at counter height.
            GameObject belt = new(beltName);
            belt.transform.SetParent(runtimeRoot, false);
            // Station roots already sit on the lot surface, so the belt shares
            // their height and the model stands on the floor rather than
            // hovering at counter level.
            belt.transform.position = (start + end) * 0.5f + Vector3.back * 0.78f;

            PrototypeVisuals.CreatePrimitive("Bant", PrimitiveType.Cube, belt.transform,
                Vector3.up * 0.78f, new Vector3(length, 0.14f, 0.56f),
                new Color(0.35f, 0.32f, 0.30f));
            MeshyVisuals.TryReplaceDirect(
                belt.transform, "13_conveyor_straight",
                new Vector3(length + 0.55f, 0.86f, 0.95f), Vector3.zero, Vector3.zero,
                false, "Bant");

            ConveyorLink link = belt.AddComponent<ConveyorLink>();
            link.Configure(from, to);
            return link;
        }

        /// <summary>
        /// One authored manager desk. The model already carries its own corner
        /// walls, so the room is just three of these in a row - no separate
        /// wall shell needed.
        /// </summary>
        private ManagementOfficeTerminal CreateManagerDesk(
            Transform parent, string title, string assetId, Vector3 position)
        {
            GameObject desk = new(title);
            desk.transform.SetParent(parent, false);
            desk.transform.localPosition = position;

            if (MeshyVisuals.TryAttach(desk.transform, assetId,
                    new Vector3(2.75f, 1.92f, 2.07f), Vector3.zero, Vector3.zero) == null)
            {
                PrototypeVisuals.CreatePrimitive("Desk Fallback", PrimitiveType.Cube,
                    desk.transform, new Vector3(0f, 0.45f, 0.2f),
                    new Vector3(1.7f, 0.85f, 0.7f), PrototypeVisuals.Cream);
            }

            // Trigger volume sits where the player walks up to the desk.
            GameObject terminal = new("Terminal");
            terminal.transform.SetParent(desk.transform, false);
            terminal.transform.localPosition = new Vector3(0f, 0f, -1.15f);
            return terminal.AddComponent<ManagementOfficeTerminal>();
        }

        /// <summary>
        /// Tiled floor marking out the management room. The desks already carry
        /// their own back walls, so the room only needed a surface to stand on to
        /// read as a room rather than a strip of open lot.
        ///
        /// The model is laid so its terracotta tiles clear the lot by a bare
        /// centimetre - enough to avoid z-fighting, little enough that the desks
        /// and pads standing at the lot surface do not look sunk - with the cream
        /// base band buried inside the lot slab.
        /// </summary>
        private void BuildOfficeFloor(Transform parent)
        {
            if (!CityKit.Has("78_floor_tiled")) return;

            // Tiles are laid on the 3.00 tile field, not the 3.28 base slab.
            // Spacing on the slab would leave a 28 cm tan channel along every
            // module join; on the field the grout stays even right across it,
            // and the overlapping slabs are buried out of sight anyway.
            const float pitch = 3.00f;
            Vector3 center = new(-3.45f, 0.012f, -5.2f);

            // Four by two covers the desk row, both pad rows and the planters
            // without spilling over the lot's south edge at z = -8.5.
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 2; j++)
                    CityKit.Spawn("78_floor_tiled", parent, new Vector3(
                        center.x + pitch * (i - 1.5f), center.y,
                        center.z + pitch * (j - 0.5f)));
        }

        private void CreatePlanter(Transform parent, Vector3 position)
        {
            GameObject planter = new("Saksı");
            planter.transform.SetParent(parent, false);
            planter.transform.localPosition = position;
            if (MeshyVisuals.TryAttach(planter.transform, "33_decorative_plant",
                    new Vector3(1.0f, 1.2f, 1.0f), Vector3.zero, Vector3.zero) == null)
                PrototypeVisuals.CreatePrimitive("Planter Fallback", PrimitiveType.Cylinder,
                    planter.transform, new Vector3(0f, 0.3f, 0f),
                    new Vector3(0.5f, 0.3f, 0.5f), new Color(0.78f, 0.45f, 0.32f));
        }

        private void CreateManagementPad(Transform parent, string name, Vector3 position, StationUpgradeType type, int cost, ItemStation station, ConveyorLink conveyor)
        {
            GameObject pad = new(name);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = position;
            PrototypeVisuals.CreatePrimitive("Satın Alma Alanı", PrimitiveType.Cylinder, pad.transform, Vector3.zero,
                new Vector3(0.55f, 0.05f, 0.55f), type == StationUpgradeType.Worker ? new Color(0.38f, 0.72f, 0.95f) : new Color(0.75f, 0.48f, 0.85f));
            MeshyVisuals.TryReplaceDirect(pad.transform, "19_upgrade_pad",
                new Vector3(0.95f, 0.30f, 0.95f), Vector3.down * 0.03f, Vector3.zero,
                false, "Satın Alma Alanı");
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
            MeshyVisuals.TryReplaceDirect(pad.transform, "19_upgrade_pad",
                new Vector3(0.95f, 0.30f, 0.95f), Vector3.down * 0.03f, Vector3.zero,
                false, "Oyuncu Upgrade");
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
            TextMesh sign = PrototypeVisuals.CreateLabel("ENTRANCE", gate.transform, new Vector3(0f, 3.15f, 0f), 0.11f);
            sign.color = new Color(0.95f, 0.22f, 0.16f);
            MeshyVisuals.TryReplaceDirect(gate.transform, "21_entrance_door",
                new Vector3(3.05f, 2.90f, 0.95f), Vector3.zero, Vector3.zero, false,
                "Entrance Top", "Entrance Left", "Entrance Right");
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

            Vector3 tableSize = new(1.44f, 1.08f, 2.12f);
            bool swapped = MeshyVisuals.TryReplaceDirect(
                tableObject.transform, "15_dining_table_clean", tableSize,
                Vector3.zero, Vector3.zero, false,
                "Table Top", "Table Leg", "Customer Chair", "Guest Chair");
            if (swapped)
            {
                // Both states are authored, so the table swaps whole models
                // rather than dressing the clean one with loose props.
                Transform clean = tableObject.transform.Find("15_dining_table_clean Visual");
                GameObject dirty = MeshyVisuals.TryAttach(
                    tableObject.transform, "16_dirty_table_props", tableSize,
                    Vector3.zero, Vector3.zero);
                if (clean != null && dirty != null)
                    table.SetTableVariants(clean.gameObject, dirty);
            }
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
            MeshyVisuals.TryReplaceDirect(plot.transform, "20_locked_expansion_pad",
                new Vector3(1.75f, 0.98f, 1.75f), Vector3.up * 0.05f, Vector3.zero,
                false, "Unlock Pad", "Lock Body", "Lock Loop");
            return plot;
        }

        /// <summary>Authored plot footprint; modules are sized to it so it never scales.</summary>
        private const float PlotSpan = 5.64f;

        /// <summary>
        /// Drop needed to land the model's deck on the lot surface. The plot is
        /// authored as a floating island, so its earth tapers 1.3 m below the
        /// deck; sunk this far the city ground hides the earth and only the kerb
        /// and tiling read above the lot.
        /// </summary>
        private const float PlotDeckOffset = -1.23f;

        private GameObject CreateExpansionModule(string moduleName, Vector3 position, Vector3 scale)
        {
            GameObject module = new(moduleName);
            module.transform.SetParent(runtimeRoot, false);
            module.transform.localPosition = position;

            if (MeshyVisuals.IsAvailable("34_floating_diorama_island"))
            {
                // Invisible slab keeps the walkable surface and its collider;
                // the model supplies everything you actually see.
                GameObject floor = PrototypeVisuals.CreatePrimitive(
                    "Plot Surface", PrimitiveType.Cube, module.transform,
                    Vector3.zero, scale, PrototypeVisuals.IslandTop, colliderEnabled: true);
                Renderer renderer = floor.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;

                MeshyVisuals.TryAttach(module.transform, "34_floating_diorama_island",
                    new Vector3(scale.x, 20f, scale.z),
                    Vector3.up * PlotDeckOffset, Vector3.zero);
            }
            else
            {
                CreateIslandGeometry(module.transform, scale);
            }
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
