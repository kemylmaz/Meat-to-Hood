using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ShawarmaPrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField, Min(0)] private int startingCoins;

        /// <summary>
        /// Half turn for anything that should look at the customer. Models are
        /// authored looking along +Z, which is Unity's forward, and this shop is
        /// laid out with the customer side at -Z. The turn used to be buried in
        /// every prefab's metadata, which meant nothing could be placed without
        /// silently inheriting it.
        /// </summary>
        private static readonly Vector3 FacingCustomer = new(0f, 180f, 0f);

        /// <summary>Tables the shop opens with, before anything is bought.</summary>
        private const int FreeTables = 2;

        /// <summary>How many stand on the shop's own floor.</summary>
        private const int MainFloorTables = 6;

        /// <summary>Tables per purchasable plot; one purchase brings the pair.</summary>
        private const int TablesPerPlot = 2;

        /// <summary>
        /// How much bigger the kitchen line is drawn than its authored metre. The
        /// counters were sized against a knee-high perimeter; the tiled shell put a
        /// 2.82 m wall behind them and left them looking like doll's furniture.
        /// Visual only - the collider and the interaction radius are gameplay.
        /// </summary>
        private const float KitchenVisualScale = 1.3f;

        /// <summary>
        /// Where the tables go, in the order they are bought: two rows of three
        /// across the dining room, then two on each purchasable plot. The plot
        /// entries carry no position of their own - they are placed on the plot
        /// they belong to, so the list only has to say how many there are.
        /// </summary>
        private static readonly Vector3[] MainFloorSlots =
        {
            new(-9f, 0.25f, 1.4f), new(-6.2f, 0.25f, 1.4f), new(-3.4f, 0.25f, 1.4f),
            new(-9f, 0.25f, -2f), new(-6.2f, 0.25f, -2f), new(-3.4f, 0.25f, -2f)
        };

        private Transform runtimeRoot;
        private Transform playerTransform;
        private MobilePlayerController playerMotor;
        private CarryInventory inventory;
        private CityLayout cityLayout;
        private TrafficSystem traffic;
        private DioramaWorld shopWorld;

        private void Awake()
        {
            if (buildOnAwake) BuildPrototype();
        }

        /// <summary>
        /// Assembles the whole shop. Runs on Awake in a live session, and can be
        /// run in the editor to put the same world in the Scene view - see
        /// <see cref="ShawarmaTycoon.EditorTools.ScenePreview"/>. Outside play mode
        /// it stops at the world: the parts that need MonoBehaviour lifecycle
        /// callbacks to exist are skipped rather than half-built.
        /// </summary>
        [ContextMenu("Build Prototype")]
        public void BuildPrototype()
        {
            if (GameObject.Find("Shawarma Prototype Runtime") != null)
                return;

            GameCatalogs.Initialize();
            GameConfig gameConfig = GameCatalogs.Game;
            EconomyConfig economyConfig = GameCatalogs.Economy;
            RestaurantLayoutConfig layout = GameCatalogs.Layout;
            DioramaWorldConfig worldConfig = GameCatalogs.World;

            PrototypeRuntimeInstaller.ConfigureApplication(gameConfig);
            // Skipped in the editor: it moves the scene's own camera and light and
            // writes the render and quality settings, which would dirty the scene
            // every time somebody opened a preview to place a prop against.
            if (Application.isPlaying) ConfigureCameraAndLighting();

            GameObject root = new("Shawarma Prototype Runtime");
            runtimeRoot = root.transform;

            // Cleared before anything registers. The tracks hold closures over the
            // objects that own them, so a rebuild into a live session would
            // otherwise keep counting the shop it just destroyed.
            UpgradeProgress.Reset();

            PrototypeRuntimeInstaller.Install(root, gameConfig, economyConfig, startingCoins);

            shopWorld = ShopWorldBuilder.Build(runtimeRoot, worldConfig);
            BuildCity(gameConfig.Features, worldConfig);
            CreatePlayer(worldConfig);

            // --- kitchen line -------------------------------------------------
            // Rack, spit, carving board, till. The spit and the board work on
            // their own once they are fed, so the line is about carrying batches
            // between four points rather than standing at each one in turn.
            ItemStation meatSource = CreateStation(
                shopWorld.KitchenRoot, "ET DEPOSU", layout.MeatSource, new Vector3(2.5f, 0.9f, 2.0f),
                new Color(0.74f, 0.39f, 0.26f), StationMode.Source,
                ItemType.None, ItemType.RawMeat, 0.5f, 1, 16, 0.65f);
            MarkPlaceable(meatSource.gameObject, "station.meat_source", "Et Deposu");
            DecorateMeatSource(meatSource.transform);
            MeshyVisuals.TryReplaceDirect(
                meatSource.transform, "04_meat_storage_rack", new Vector3(3.0f, 2.8f, 2.1f),
                Vector3.zero, new Vector3(0f, 180f, 0f), false,
                "Counter", "Work Top", "Rack Back", "RawMeat");
            ApplyAuthoredStationLayout(meatSource, 2.9f, -1.05f);
            meatSource.SetVisualItemScale(1.18f);

            ItemStation oven = CreateStation(
                shopWorld.KitchenRoot, "OCAK", layout.Oven, new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.88f, 0.45f, 0.20f), StationMode.Processor,
                ItemType.RawMeat, ItemType.CookedMeat, 1.4f, 12, 12, 1f);
            MarkPlaceable(oven.gameObject, "station.oven", "Ocak");
            DecorateOven(oven.transform);
            // Fitted rather than placed at its authored 1.16 m. The spit is the
            // middle of the shop, and next to a 2.4 m rack and a 2.2 m counter the
            // model as authored read as a microwave.
            MeshyVisuals.TryReplaceDirect(
                oven.transform, "06_shawarma_rotisserie", new Vector3(2.4f, 3.9f, 1.75f),
                Vector3.zero, FacingCustomer, false,
                "Counter", "Work Top", "Heater Left", "Heater Right", "Doner Spit");
            ApplyAuthoredStationLayout(oven, 2.55f, -0.82f);
            oven.SetVisualItemScale(1.24f);
            oven.SetOutputBatchVisual("75_meat_tray_stack", 4,
                new Vector3(0.72f, 0.50f, 0.56f), 0.52f);

            ItemStation cutting = CreateStation(
                shopWorld.KitchenRoot, "KESİM", layout.Cutting, new Vector3(2.2f, 0.9f, 1.9f),
                new Color(0.65f, 0.70f, 0.67f), StationMode.Processor,
                ItemType.CookedMeat, ItemType.Wrap, 1.6f, 12, 12, 1f);
            MarkPlaceable(cutting.gameObject, "station.cutting", "Kesim Tezgâhı");
            DecorateCuttingCounter(cutting.transform);
            MeshyVisuals.TryReplaceDirectAuthored(
                cutting.transform, "08_cutting_station",
                Vector3.zero, FacingCustomer, KitchenVisualScale,
                "Counter", "Work Top", "Cutting Board", "Knife");
            ApplyAuthoredStationLayout(cutting, 1.9f, -0.75f);
            cutting.SetVisualItemScale(1.18f);
            cutting.SetOutputBatchVisual("74_wrap_tray_stack", 4,
                new Vector3(0.68f, 0.55f, 0.52f), 0.55f);

            ItemStation service = CreateStation(
                shopWorld.KitchenRoot, "SERVİS", layout.Service, new Vector3(2.2f, 0.9f, 1.9f),
                PrototypeVisuals.Teal, StationMode.Service,
                ItemType.Wrap, ItemType.None, 0.1f, 1, 14, 1f);
            MarkPlaceable(service.gameObject, "station.service", "Servis Tezgâhı");
            MeshyVisuals.TryReplaceDirectAuthored(
                service.transform, "12_service_cashier_counter",
                Vector3.zero, FacingCustomer, KitchenVisualScale,
                "Counter", "Work Top");
            Vector3 serviceTray = ApplyAuthoredStationLayout(service, 1.9f, -0.66f);
            service.SetVisualItemScale(1.05f);
            service.SetOutputBatchVisual("74_wrap_tray_stack", 3,
                new Vector3(0.68f, 0.55f, 0.52f), 0.55f);
            BuildServingDisplay(service, serviceTray);
            // The shop opens prepped. The queue arrives within seconds of the
            // first frame and the line takes a minute and a half to produce
            // anything, so without this the opening is spent watching.
            service.Prime(4);
            cutting.Prime(3);
            oven.Prime(3);

            // Where the queue pays. Set beside the till on the shop-floor side, so
            // it is collected on the way past rather than reached over the counter.
            GameObject tillObject = new("Kasa Parası");
            tillObject.transform.SetParent(service.transform, false);
            tillObject.transform.localPosition = new Vector3(-1.35f, 0f, -0.9f);
            CashPile till = tillObject.AddComponent<CashPile>();
            till.Configure(playerTransform);

            meatSource.SetWorldLabelVisible(false);
            oven.SetWorldLabelVisible(false);
            cutting.SetWorldLabelVisible(false);
            service.SetWorldLabelVisible(false);

            // --- belts --------------------------------------------------------
            ConveyorLink rawBelt = CreateConveyor(
                shopWorld.KitchenRoot, "Et Bandı", meatSource, oven, layout.MeatSource, layout.Oven);
            MarkPlaceable(rawBelt.gameObject, "belt.raw", "Et Bandı");
            ConveyorLink ovenBelt = CreateConveyor(
                shopWorld.KitchenRoot, "Ocak Bandı", oven, cutting, layout.Oven, layout.Cutting);
            MarkPlaceable(ovenBelt.gameObject, "belt.oven", "Ocak Bandı");
            ConveyorLink cuttingBelt = CreateConveyor(
                shopWorld.KitchenRoot, "Kesim Bandı", cutting, service, layout.Cutting, layout.Service);
            MarkPlaceable(cuttingBelt.gameObject, "belt.cutting", "Kesim Bandı");
            CreateConveyorPad(shopWorld.KitchenRoot, "Et Bandı Pedi",
                layout.MeatBeltPad, "belt.raw", rawBelt, 0);
            CreateConveyorPad(shopWorld.KitchenRoot, "Ocak Bandı Pedi",
                layout.OvenBeltPad, "belt.oven", ovenBelt, 1);
            CreateConveyorPad(shopWorld.KitchenRoot, "Kesim Bandı Pedi",
                layout.CuttingBeltPad, "belt.cutting", cuttingBelt, 2);

            // --- utilities ----------------------------------------------------
            GameObject trashBinObject = CreateTrashBin(shopWorld.UtilityRoot, layout.TrashBin);
            TakeawaySystem driveThru = CreateDriveThruWindow(shopWorld.UtilityRoot, gameConfig, layout);

            // --- drinks, desserts and couriers --------------------------------
            // All three stand built but switched off; their pads bring them in.
            // Placeholder shapes for now - the fridge and the courier's scooter
            // are waiting on authored models.
            ItemStation drinkCrate = CreateStation(
                shopWorld.UtilityRoot, "İÇECEK DEPOSU", layout.DrinkCrate,
                new Vector3(1.9f, 1.0f, 1.2f), new Color(0.30f, 0.44f, 0.62f), StationMode.Source,
                ItemType.None, ItemType.Drink, 0.5f, 1, 18, 1.1f);
            drinkCrate.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            MarkPlaceable(drinkCrate.gameObject, "station.drink_crate", "İçecek Deposu");
            DecorateDrinkCrate(drinkCrate.transform);
            ApplyAuthoredStationLayout(drinkCrate, 1.7f, -0.72f);
            drinkCrate.SetVisualItemScale(1.08f);
            drinkCrate.SetOutputGrid(4, 0.25f, 0.18f);

            ItemStation fridge = CreateStation(
                shopWorld.UtilityRoot, "BUZDOLABI", layout.Fridge,
                new Vector3(1.5f, 1.0f, 1.0f), new Color(0.86f, 0.90f, 0.93f), StationMode.Service,
                ItemType.Drink, ItemType.None, 0.1f, 1, 12, 1f);
            fridge.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            MarkPlaceable(fridge.gameObject, "station.fridge", "Buzdolabı");
            DecorateFridge(fridge.transform);
            // A fridge has shelves, not a processor's pair of metal trays. Stock
            // is spread across its lower display shelf instead of piled vertically.
            fridge.SetVisualLayout(Vector3.zero, new Vector3(0f, 0.72f, -0.48f), 2.15f);
            fridge.SetVisualItemScale(1.12f);
            fridge.SetOutputGrid(4, 0.25f, 0.18f);
            fridge.SetEmptyWarning("İÇECEK BİTTİ");

            ItemStation dessertOven = CreateStation(
                shopWorld.UtilityRoot, "TATLI FIRINI", layout.DessertOven,
                new Vector3(1.7f, 1.0f, 1.1f), new Color(0.80f, 0.55f, 0.38f), StationMode.Source,
                ItemType.None, ItemType.Dessert, 0.5f, 1, 8, 4.2f);
            MarkPlaceable(dessertOven.gameObject, "station.dessert_oven", "Tatlı Fırını");
            DecorateDessertOven(dessertOven.transform);
            ApplyAuthoredStationLayout(dessertOven, 2.2f, -0.7f);

            drinkCrate.SetWorldLabelVisible(false);
            fridge.SetWorldLabelVisible(false);
            dessertOven.SetWorldLabelVisible(false);

            GameObject drinksRoot = new("İçecek Hattı");
            drinksRoot.transform.SetParent(shopWorld.UtilityRoot, false);
            drinkCrate.transform.SetParent(drinksRoot.transform, true);
            fridge.transform.SetParent(drinksRoot.transform, true);
            RepairLegacyDrinkLineLayout(layout, drinkCrate, fridge);
            drinksRoot.SetActive(false);
            dessertOven.gameObject.SetActive(false);

            CourierStation courier = CreateCourierBay(shopWorld.UtilityRoot, layout);

            // --- offices ------------------------------------------------------
            ManagementMenuHUD managementHud = root.AddComponent<ManagementMenuHUD>();
            GameObject managementRoot = new("Yönetim Odaları");
            managementRoot.transform.SetParent(shopWorld.ManagementRoot, false);

            // Two rooms, built into the south-west corner against the perimeter
            // wall, each properly closed with a doorway onto the dining room. They
            // stand from the first second but empty; the money pad inside puts the
            // desk and the clerk in. Recruiting moved onto the HR desk, which is
            // where hiring belongs anyway - three rooms for two jobs was one door
            // too many to walk through.
            CreateOffice(managementRoot.transform, "İK Odası", "25_hr_manager_desk",
                OfficeWestX, ManagementMenu.HumanResources, "PERSONEL", "office.hr",
                ShopPrices.HumanResourcesOffice, managementHud);
            CreateOffice(managementRoot.transform, "GM Odası", "27_general_manager_desk",
                OfficeWestX + OfficeWidth, ManagementMenu.GeneralManager, "GM",
                "office.gm", ShopPrices.GeneralManagerOffice, managementHud);

            CreatePlanter(managementRoot.transform, new Vector3(2.6f, 0.25f, -7.4f), "office.planter.1");
            CreatePlanter(managementRoot.transform, new Vector3(6.2f, 0.25f, -7.4f), "office.planter.2");

            // --- dining -------------------------------------------------------
            // Two tables to open with, then one at a time up to ten. Seating is
            // the measured bottleneck on the whole shop, so it is the thing the
            // player spends on all the way through rather than a pair of wings
            // bought once and forgotten.
            IReadOnlyList<DioramaModule> expansionModules = shopWorld.ExpansionModules;
            DioramaExpansion expansion = root.AddComponent<DioramaExpansion>();
            expansion.Configure(expansionModules);

            // Six on the floor, then two on every plot the shop can reach. The
            // count follows the plots rather than a written-down list, so widening
            // the expansion grid adds tables to buy without touching this.
            List<CustomerTable> tables = new();
            List<GameObject> boughtFloorTables = new();
            for (int i = 0; i < MainFloorTables; i++)
            {
                CustomerTable table = CreateTable(
                    shopWorld.DiningRoot, $"Masa {i + 1}", MainFloorSlots[i], $"table.{i + 1}");
                // The south row faces the dining aisle. Facing its chair into the
                // partition left no standing room between chair and wall.
                if (i >= 3) table.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                tables.Add(table);
                if (i >= FreeTables) boughtFloorTables.Add(table.gameObject);
            }

            // A plot is one purchase that brings two tables with it, rather than
            // two purchases that happen to share a plot. That is what lets the shop
            // reach eighteen covers on a ten step ladder: seating is the biggest
            // sink in the game, and the measured income says a wider floor earns
            // no more than a narrow one, so eighteen separately priced tables put
            // buying the shop out at twice the intended pace.
            List<GameObject> plotTables = new();
            for (int module = 0; module < expansionModules.Count; module++)
            for (int seat = 0; seat < TablesPerPlot; seat++)
            {
                int number = MainFloorTables + module * TablesPerPlot + seat + 1;
                CustomerTable table = CreateTable(
                    expansionModules[module].ContentRoot, $"Masa {number}",
                    new Vector3(seat == 0 ? -1.4f : 1.4f, 0.25f, 0f), $"table.{number}");
                tables.Add(table);
                plotTables.Add(table.gameObject);
            }

            foreach (GameObject bought in boughtFloorTables) bought.SetActive(false);
            foreach (GameObject bought in plotTables) bought.SetActive(false);

            CreatePurchasePad(
                shopWorld.UtilityRoot, "Masa Ekle", layout.TablePad, "tables",
                ShopPrices.Table,
                (level, _) =>
                {
                    // The floor fills first, one table a level; after that each
                    // level opens the next plot and stands both its tables on it.
                    int index = level - 1;
                    if (index < 0) return;
                    if (index < boughtFloorTables.Count)
                    {
                        boughtFloorTables[index].SetActive(true);
                        return;
                    }

                    int plot = index - boughtFloorTables.Count;
                    if (plot >= expansionModules.Count) return;
                    expansion.UnlockNext();
                    for (int seat = 0; seat < TablesPerPlot; seat++)
                    {
                        int table = plot * TablesPerPlot + seat;
                        if (table < plotTables.Count) plotTables[table].SetActive(true);
                    }
                },
                previewAsset: "15_dining_table_clean",
                previewSize: new Vector3(0.82f, 0.72f, 0.82f));

            // --- customers ----------------------------------------------------
            Transform entry = CreateMarker(shopWorld.CustomerFlowRoot, "Müşteri Girişi", layout.CustomerEntry);
            Transform exit = CreateMarker(shopWorld.CustomerFlowRoot, "Müşteri Çıkışı", layout.CustomerExit);
            Transform queueFront = CreateMarker(shopWorld.CustomerFlowRoot, "Kuyruk Başı", layout.QueueFront);

            GameObject customerRoot = new("Müşteriler");
            customerRoot.transform.SetParent(shopWorld.CustomerFlowRoot, false);
            CustomerManager customerManager = customerRoot.AddComponent<CustomerManager>();
            customerManager.Configure(playerTransform, service, till, entry, exit,
                shopWorld.EntranceAnchor, queueFront, Vector3.back, tables);
            customerManager.SetApproachRoute(
                CreateMarker(shopWorld.CustomerFlowRoot, "Yaklaşma Başı", ApproachStart(layout)),
                CreateMarker(shopWorld.CustomerFlowRoot, "Yaklaşma Dönüşü", ApproachCorner(layout)));
            customerManager.RegisterCounter(ItemType.Drink, fridge);
            customerManager.RegisterCounter(ItemType.Dessert, dessertOven);
            courier.Configure(playerTransform, inventory, customerManager, courierCash);

            CreatePurchasePad(shopWorld.UtilityRoot, "Buzdolabı Pedi",
                layout.FridgePad, "shop.fridge", new[] { ShopPrices.Fridge },
                (_, __) => drinksRoot.SetActive(true),
                previewAsset: "190_kitchen_fridge",
                previewSize: new Vector3(0.72f, 0.94f, 0.66f), previewYaw: 90f);
            CreatePurchasePad(shopWorld.UtilityRoot, "Tatlı Fırını Pedi",
                layout.DessertPad, "shop.dessert", new[] { ShopPrices.DessertOven },
                (_, __) => dessertOven.gameObject.SetActive(true),
                previewAsset: "151_shop_oven",
                previewSize: new Vector3(0.76f, 0.78f, 0.68f));
            CreatePurchasePad(shopWorld.UtilityRoot, "Kurye Pedi",
                layout.CourierPad, "shop.courier", new[] { ShopPrices.Courier },
                (_, __) => courier.transform.parent.gameObject.SetActive(true),
                previewAsset: "244_food_bag",
                previewSize: new Vector3(0.62f, 0.72f, 0.48f));

            FloorSpillSystem floorSpills = null;
            if (gameConfig.Features.FloorSpills)
            {
                floorSpills = root.AddComponent<FloorSpillSystem>();
                floorSpills.Configure(playerTransform, tables);
            }

            // --- staff and management ----------------------------------------
            HumanResourcesSystem humanResources = root.AddComponent<HumanResourcesSystem>();
            humanResources.Configure(playerTransform, new[] { rawBelt, ovenBelt, cuttingBelt });
            PlayerUpgradeSystem playerUpgrades = root.AddComponent<PlayerUpgradeSystem>();
            playerUpgrades.Configure(playerTransform, playerMotor, inventory);
            RecruitmentSystem recruitment = root.AddComponent<RecruitmentSystem>();
            recruitment.Configure(
                customerManager, cutting, service, driveThru, till, floorSpills,
                shopWorld.BaseModule.ContentRoot, trashBinObject.transform,
                StaffPosts(layout, driveThru));
            managementHud.Configure(humanResources, playerUpgrades, recruitment);

            // The HUD builds its own panels from Awake, so outside play mode there
            // is nothing to bind to. An editor preview gets the world and no
            // interface, which is what you want to place props against anyway.
            if (Application.isPlaying)
            {
                UI.GameHUD hud = UI.GameHUD.Ensure(runtimeRoot);
                hud.Objective.Bind(inventory);
                hud.Objective.BindTables(tables);
                hud.Objective.BindStations(new[] { oven, cutting, service });
                playerMotor.SetJoystick(hud.Joystick);
                BuildModeController buildMode = root.AddComponent<BuildModeController>();
                buildMode.Configure(Camera.main, playerMotor, hud.Joystick,
                    shopWorld.WalkableRegistry, hud.BuildMode);
            }

            CreateTutorialArrow(meatSource, oven, cutting, service);

            // Things that go wrong, so an automated shop still needs somebody in
            // it. Held back until at least one belt is running - see the system.
            ShopEventSystem events = root.AddComponent<ShopEventSystem>();
            events.Configure(playerTransform, new[] { oven, cutting });

            CreateDecorations(shopWorld.DiningRoot, layout);
            DressKitchenWall(shopWorld.KitchenRoot, layout);
            AnimateStations(oven, cutting, dessertOven);

            // Every meaningful slice of shop completion now has a physical
            // counterpart: facade, floor, walls, light, decor and furniture all
            // advance together while gameplay positions remain untouched.
            RestaurantMakeoverSystem makeover = root.AddComponent<RestaurantMakeoverSystem>();
            makeover.Configure(shopWorld, tables, playerTransform);

            // The whole restaurant is runtime-built, so navigation can only be
            // baked after every floor, counter, table and decoration exists.
            if (Application.isPlaying)
            {
                RestaurantNavigation navigation = root.AddComponent<RestaurantNavigation>();
                navigation.Rebuild();
            }

            Debug.Log("[ShawarmaTycoon] Prototype ready: rack → spit → carving board → till, with a drive-through on the street side.");
        }

        /// <summary>
        /// Where each hire idles between jobs, in the order of
        /// <see cref="RecruitmentSystem.AllRoles"/>. Behind the counter they work
        /// at, rather than the three fixed spots in the middle of the floor the
        /// old roster used.
        /// </summary>
        private static Vector3[] StaffPosts(RestaurantLayoutConfig layout, TakeawaySystem driveThru)
        {
            Vector3 window = driveThru != null ? driveThru.transform.localPosition : layout.DriveThruCounter;
            return new[]
            {
                layout.Service + new Vector3(-1.3f, 0f, 0.95f),
                window + new Vector3(0.9f, 0f, -1.1f),
                new Vector3((layout.Cutting.x + window.x) * 0.5f, layout.Cutting.y, layout.Cutting.z - 2.2f),
                // The bussers wait at either end of the dining room's first row,
                // read off the table slots rather than a table that may not have
                // been bought yet.
                MainFloorSlots[0] + new Vector3(-2.1f, 0f, 0f),
                MainFloorSlots[2] + new Vector3(2.1f, 0f, 0f)
            };
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
        /// The street the shop stands on. Its measurements come from the lot, so
        /// the pavement meets the kerb and the driveway runs along the back wall
        /// wherever the lot is sized to.
        /// </summary>
        private void BuildCity(GameFeatureFlags features, DioramaWorldConfig worldConfig)
        {
            cityLayout = new CityLayout
            {
                LotWidth = worldConfig.CoreSize.x,
                LotDepth = worldConfig.CoreSize.y,
                CenterX = worldConfig.CorePosition.x,
                SurfaceY = worldConfig.DeckTopY
            };

            if (!features.CityDecor) return;
            CityBlock.Build(runtimeRoot, cityLayout);

            if (!features.Traffic) return;
            traffic = runtimeRoot.gameObject.AddComponent<TrafficSystem>();
            traffic.Configure(cityLayout);
        }

        private void CreatePlayer(DioramaWorldConfig worldConfig)
        {
            GameObject player = new("Player");
            player.transform.SetParent(runtimeRoot, false);
            player.transform.localPosition = worldConfig.PlayerSpawn;
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
            playerMotor.Configure(4.6f, shopWorld.WalkableRegistry, worldConfig.EdgeSafetyMargin);
            // Only in a live session: the rig is put on the camera and given its
            // camera by ConfigureCameraAndLighting, which an editor preview skips
            // so it does not move the scene's own camera about.
            MobileCameraRig cameraRig = Application.isPlaying && Camera.main != null
                ? Camera.main.GetComponent<MobileCameraRig>()
                : null;
            if (cameraRig != null)
                cameraRig.SetFollowTarget(player.transform);

            inventory = player.AddComponent<CarryInventory>();
            // Small enough that the natural batch flows through the line quickly.
            // At 12 the source filled the player up in a second and a half, and
            // since a processor only runs while you stand at it, that meant 26 s
            // parked at the oven and the best part of a minute before the first
            // wrap reached the counter. The capacity upgrade grows this.
            inventory.Configure(6);
            if (MeshyVisuals.TryReplaceDirectAuthored(
                    player.transform, "01_player_character",
                    Vector3.zero, Vector3.zero, "Body", "Apron") &&
                MeshyVisuals.TryFindAnchor(player.transform, "CARRY_ANCHOR", out Transform carryAnchor))
            {
                inventory.SetStackAnchor(carryAnchor);
            }
        }

        private ItemStation CreateStation(
            Transform parent,
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
            stationObject.transform.SetParent(parent, false);
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

        /// <summary>
        /// Glass case over the finished wraps on the serving counter. The counter
        /// held a bare pile of food where a shop front should be; the frame is
        /// open on purpose so the stack inside stays the thing you read.
        /// </summary>
        private static void BuildServingDisplay(ItemStation service, Vector3 trayLocalPosition)
        {
            GameObject display = new("Vitrin");
            display.transform.SetParent(service.transform, false);
            display.transform.localPosition = trayLocalPosition;

            Color frame = new(0.86f, 0.90f, 0.93f);
            Color rail = new(0.55f, 0.62f, 0.66f);
            const float halfX = 0.44f;
            const float halfZ = 0.34f;
            const float height = 0.72f;

            foreach (float x in new[] { -halfX, halfX })
            foreach (float z in new[] { -halfZ, halfZ })
                PrototypeVisuals.CreatePrimitive("Vitrin Direği", PrimitiveType.Cube,
                    display.transform, new Vector3(x, height * 0.5f, z),
                    new Vector3(0.035f, height, 0.035f), rail);

            PrototypeVisuals.CreatePrimitive("Vitrin Tablası", PrimitiveType.Cube,
                display.transform, new Vector3(0f, -0.02f, 0f),
                new Vector3(halfX * 2f + 0.08f, 0.04f, halfZ * 2f + 0.08f), frame);
            PrototypeVisuals.CreatePrimitive("Vitrin Tepesi", PrimitiveType.Cube,
                display.transform, new Vector3(0f, height, 0f),
                new Vector3(halfX * 2f + 0.08f, 0.04f, halfZ * 2f + 0.08f), frame);
            PrototypeVisuals.CreatePrimitive("Vitrin Kaşı", PrimitiveType.Cube,
                display.transform, new Vector3(0f, height + 0.10f, -halfZ),
                new Vector3(halfX * 2f + 0.12f, 0.16f, 0.05f), new Color(0.92f, 0.44f, 0.26f));
        }

        /// <summary>
        /// A belt seated in the gap between the two counters it links, at the
        /// kitchen line rather than a metre in front of it. The old placement put
        /// every belt on the customer side of the counters, in the walk the player
        /// uses to get between them.
        ///
        /// The belt starts switched off and invisible; its pad builds it.
        /// </summary>
        private ConveyorLink CreateConveyor(
            Transform parent,
            string beltName,
            ItemStation from,
            ItemStation to,
            Vector3 fromPosition,
            Vector3 toPosition)
        {
            GameObject belt = new(beltName);
            belt.transform.SetParent(parent, false);
            belt.transform.localPosition = new Vector3(
                (fromPosition.x + toPosition.x) * 0.5f, fromPosition.y, fromPosition.z);

            GameObject visual = new("Bant Görseli");
            visual.transform.SetParent(belt.transform, false);
            float gap = Mathf.Abs(toPosition.x - fromPosition.x);
            PrototypeVisuals.CreatePrimitive("Bant", PrimitiveType.Cube, visual.transform,
                Vector3.up * 0.38f, new Vector3(Mathf.Max(0.7f, gap - 1.9f), 0.14f, 0.56f),
                new Color(0.35f, 0.32f, 0.30f));
            MeshyVisuals.TryReplaceDirectAuthored(
                visual.transform, "13_conveyor_straight", Vector3.zero, Vector3.zero, "Bant");

            ConveyorLink link = belt.AddComponent<ConveyorLink>();
            link.Configure(from, to, visual.transform);
            return link;
        }

        private void CreateConveyorPad(
            Transform parent, string padName, Vector3 position, string saveKey, ConveyorLink belt,
            int priceIndex)
        {
            CreatePurchasePad(parent, padName, position, saveKey,
                new[] { ShopPrices.Belt[Mathf.Clamp(priceIndex, 0, ShopPrices.Belt.Length - 1)] },
                (level, _) => belt.SetLevel(level),
                previewAsset: "13_conveyor_straight",
                previewSize: new Vector3(0.90f, 0.30f, 0.52f));
        }

        private PurchasePad CreatePurchasePad(
            Transform parent,
            string padName,
            Vector3 position,
            string saveKey,
            int[] costs,
            System.Action<int, bool> onLevel,
            string padAsset = "19_upgrade_pad",
            string previewAsset = null,
            Vector3? previewSize = null,
            float previewYaw = 0f)
        {
            GameObject pad = new(padName);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = position;
            PurchasePad purchase = pad.AddComponent<PurchasePad>();
            purchase.Configure(playerTransform, saveKey, costs, onLevel, padAsset);
            if (!string.IsNullOrEmpty(previewAsset) && previewSize.HasValue)
                purchase.SetPreview(previewAsset, previewSize.Value, previewYaw);
            return purchase;
        }

        /// <summary>Cash the courier bay pays into. Assigned when the bay is built.</summary>
        private CashPile courierCash;

        /// <summary>
        /// The courier bay: a counter with an order board and a scooter parked
        /// outside the fence waiting for the bag. Both shapes are placeholders -
        /// the scooter is being modelled - so everything here is primitives that
        /// can be swapped for one authored prefab later.
        /// </summary>
        private CourierStation CreateCourierBay(Transform parent, RestaurantLayoutConfig layout)
        {
            GameObject bay = new("Kurye Noktası");
            bay.transform.SetParent(parent, false);

            GameObject counter = new("Kurye Tezgahı");
            counter.transform.SetParent(bay.transform, false);
            counter.transform.localPosition = layout.CourierCounter;

            PrototypeVisuals.CreatePrimitive("Gövde", PrimitiveType.Cube, counter.transform,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.7f, 0.90f, 1.0f),
                new Color(0.90f, 0.52f, 0.22f), colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Tezgah Üstü", PrimitiveType.Cube, counter.transform,
                new Vector3(0f, 0.94f, 0f), new Vector3(1.8f, 0.12f, 1.1f), PrototypeVisuals.Cream);
            PrototypeVisuals.CreatePrimitive("Paket Kutusu", PrimitiveType.Cube, counter.transform,
                new Vector3(-0.45f, 1.15f, 0f), new Vector3(0.5f, 0.30f, 0.4f),
                new Color(0.78f, 0.62f, 0.40f));

            // A packing bench with bags waiting on it. The bags are what the bay
            // is for, so they stand in for the box the primitive used.
            if (MeshyVisuals.TryReplaceDirectAuthored(
                    counter.transform, "162_shop_kitchen_table_large",
                    Vector3.zero, FacingCustomer, "Gövde", "Tezgah Üstü", "Paket Kutusu"))
            {
                MeshyVisuals.TryAttachAuthored(counter.transform, "244_food_bag",
                    new Vector3(-0.5f, 0.92f, 0.05f), new Vector3(0f, 25f, 0f));
                MeshyVisuals.TryAttachAuthored(counter.transform, "245_food_bag_flat",
                    new Vector3(0.42f, 0.92f, 0.12f), new Vector3(0f, -15f, 0f));
                MeshyVisuals.TryAttachAuthored(counter.transform, "247_food_styrofoam",
                    new Vector3(0.1f, 0.92f, -0.28f), new Vector3(0f, 8f, 0f));
            }

            courierCash = counter.AddComponent<CashPile>();
            courierCash.Configure(playerTransform);
            // The pile is drawn beside the counter rather than under it, so the
            // player can see the takings without walking behind the bay.
            foreach (Transform child in counter.transform)
                if (child.name == "Cash Pad" || child.name == "Cash Stack")
                    child.localPosition = new Vector3(1.15f, child.localPosition.y, -0.35f);

            CourierStation station = counter.AddComponent<CourierStation>();
            MarkPlaceable(counter, "station.courier", "Kurye Tezgâhı");
            station.SetScooter(BuildScooter(bay.transform,
                layout.CourierCounter + new Vector3(0.2f, 0f, -2.9f)));

            bay.SetActive(false);
            return station;
        }

        /// <summary>Placeholder scooter, standing in until the model arrives.</summary>
        private static GameObject BuildScooter(Transform parent, Vector3 position)
        {
            GameObject scooter = new("Kurye Motoru");
            scooter.transform.SetParent(parent, false);
            scooter.transform.localPosition = position;
            scooter.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

            Color body = new(0.86f, 0.26f, 0.22f);
            Color trim = new(0.24f, 0.25f, 0.28f);
            PrototypeVisuals.CreatePrimitive("Gövde", PrimitiveType.Cube, scooter.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.38f, 0.30f, 1.15f), body);
            PrototypeVisuals.CreatePrimitive("Sele", PrimitiveType.Cube, scooter.transform,
                new Vector3(0f, 0.72f, -0.15f), new Vector3(0.32f, 0.14f, 0.50f), trim);
            PrototypeVisuals.CreatePrimitive("Kasa", PrimitiveType.Cube, scooter.transform,
                new Vector3(0f, 0.86f, -0.52f), new Vector3(0.44f, 0.42f, 0.44f),
                new Color(0.94f, 0.74f, 0.28f));
            PrototypeVisuals.CreatePrimitive("Gidon", PrimitiveType.Cube, scooter.transform,
                new Vector3(0f, 0.92f, 0.48f), new Vector3(0.56f, 0.07f, 0.07f), trim);
            PrototypeVisuals.CreatePrimitive("Ön Direk", PrimitiveType.Cube, scooter.transform,
                new Vector3(0f, 0.70f, 0.48f), new Vector3(0.10f, 0.46f, 0.10f), trim);
            foreach (float z in new[] { 0.52f, -0.50f })
                PrototypeVisuals.CreatePrimitive("Tekerlek", PrimitiveType.Cylinder, scooter.transform,
                    new Vector3(0f, 0.26f, z), new Vector3(0.26f, 0.06f, 0.26f), trim,
                    new Vector3(0f, 0f, 90f));
            return scooter;
        }

        /// <summary>
        /// Gives the kitchen a pulse. Everything here is presentation only - the
        /// spit turning does not cook anything - but a shop where nothing moves
        /// reads as a screenshot however busy the numbers are.
        /// </summary>
        private static void AnimateStations(
            ItemStation oven, ItemStation cutting, ItemStation dessertOven)
        {
            // The spit turns if the model has one that can turn on its own. The
            // authored rotisserie exports as a single merged mesh, so until it is
            // re-exported with a named SPIT part the whole cabinet rocks instead.
            System.Func<bool> ovenRunning = () => oven.InputCount > 0 && !oven.IsBroken;
            Transform spit = FindPart(oven.transform, "SPIT");
            if (spit != null) spit.gameObject.AddComponent<SpinningPart>().Configure(48f, Vector3.up);
            else AddWorkingShake(oven.transform, ovenRunning, 1.4f);

            SmokeStack ovenSmoke = SmokeStack.Attach(
                oven.transform, new Vector3(0f, 2.35f, 0.15f), new Color(0.84f, 0.82f, 0.80f), 1.1f);
            ovenSmoke.IsActive = () => oven.InputCount > 0 && !oven.IsBroken;

            HeatGlow ovenGlow = HeatGlow.Attach(
                oven.transform, new Vector3(0f, 0.62f, -0.18f), new Vector3(0.9f, 0.06f, 0.5f));
            ovenGlow.IsActive = () => oven.InputCount > 0 && !oven.IsBroken;

            if (dessertOven != null)
            {
                SmokeStack bakerySmoke = SmokeStack.Attach(
                    dessertOven.transform, new Vector3(0.62f, 2.1f, 0.15f),
                    new Color(0.88f, 0.86f, 0.83f), 1.6f);
                bakerySmoke.IsActive = () => dessertOven.OutputCount < 8;
            }

            // Same story on the carving board: a named BLADE would rock on its
            // own, otherwise the counter does.
            System.Func<bool> cuttingRunning = () => cutting.InputCount > 0 && !cutting.IsBroken;
            Transform blade = FindPart(cutting.transform, "BLADE");
            if (blade != null)
                blade.gameObject.AddComponent<ChoppingKnife>().IsActive = cuttingRunning;
            else AddWorkingShake(cutting.transform, cuttingRunning, 1.9f);
        }

        /// <summary>
        /// Rocks a station's visuals, leaving its gameplay transform alone. The
        /// stacks, trays and interaction radius all hang off the station itself,
        /// so shaking that would shake the food off the counter with it.
        /// </summary>
        private static void AddWorkingShake(
            Transform station, System.Func<bool> isRunning, float degrees)
        {
            for (int i = 0; i < station.childCount; i++)
            {
                Transform child = station.GetChild(i);
                if (!child.name.EndsWith(" Visual")) continue;
                WorkingShake shake = child.gameObject.AddComponent<WorkingShake>();
                shake.IsActive = isRunning;
                return;
            }
        }

        /// <summary>Depth-first search for a named part inside a station's visuals.</summary>
        private static Transform FindPart(Transform root, string name)
        {
            return MeshyVisuals.TryFindAnchor(root, name, out Transform found) ? found : null;
        }

        /// <summary>
        /// Four spots around the dining room that can be dressed. Each one lifts
        /// the shop's standing for good, and standing is what the tip is.
        /// </summary>
        private void CreateDecorations(Transform parent, RestaurantLayoutConfig layout)
        {
            GameObject root = new("Dekorasyon");
            root.transform.SetParent(parent, false);

            Vector3[] spots =
            {
                new(-10.4f, 0.25f, 3.4f), new(-10.4f, 0.25f, -3.6f),
                new(1.4f, 0.25f, 3.2f), new(1.4f, 0.25f, -3.4f)
            };

            List<GameObject> props = new();
            for (int i = 0; i < spots.Length; i++)
                props.Add(BuildDecoration(root.transform, spots[i], i));
            for (int i = 0; i < props.Count; i++) props[i].SetActive(false);

            CreatePurchasePad(parent, "Dekorasyon Pedi", layout.DecorationPad, "decor",
                ShopPrices.Decoration,
                (level, _) =>
                {
                    int index = level - 1;
                    if (index < 0 || index >= props.Count) return;
                    props[index].SetActive(true);
                    ReputationSystem.Instance?.AddStandingFloor(ShopPrices.DecorationStanding);
                },
                previewAsset: "73_planter",
                previewSize: new Vector3(0.66f, 0.82f, 0.66f));
        }

        /// <summary>
        /// One purchasable piece of dressing. Each is a single model where the
        /// pack has one, with the original primitive group behind it, so the pad
        /// and the standing it buys never depend on the art being there.
        /// </summary>
        private static GameObject BuildDecoration(Transform parent, Vector3 position, int variant)
        {
            GameObject prop = new("Dekor " + (variant + 1));
            prop.transform.SetParent(parent, false);
            prop.transform.localPosition = position;
            MarkPlaceable(prop, $"decoration.{variant + 1}", $"Dekorasyon {variant + 1}");

            // A lamp, the menu board, a bench seat for people waiting, and a rug.
            string[] models = { "213_decor_floor_lamp", "154_shop_menu", "214_decor_couch", "210_decor_carpet" };
            string model = models[Mathf.Clamp(variant, 0, models.Length - 1)];
            if (MeshyVisuals.TryAttachAuthored(prop.transform, model, Vector3.zero, FacingCustomer) != null)
            {
                // The menu board keeps the lit panel the primitive sign had.
                if (variant == 1)
                    HeatGlow.Attach(prop.transform, new Vector3(0f, 0.78f, -0.23f),
                        new Vector3(0.5f, 0.62f, 0.04f));
                return prop;
            }

            switch (variant)
            {
                case 0: // a tall planter
                    PrototypeVisuals.CreatePrimitive("Saksı", PrimitiveType.Cylinder, prop.transform,
                        new Vector3(0f, 0.28f, 0f), new Vector3(0.5f, 0.28f, 0.5f),
                        new Color(0.80f, 0.44f, 0.30f));
                    PrototypeVisuals.CreatePrimitive("Gövde", PrimitiveType.Cylinder, prop.transform,
                        new Vector3(0f, 0.85f, 0f), new Vector3(0.12f, 0.36f, 0.12f),
                        new Color(0.42f, 0.34f, 0.24f));
                    PrototypeVisuals.CreatePrimitive("Yaprak", PrimitiveType.Sphere, prop.transform,
                        new Vector3(0f, 1.35f, 0f), new Vector3(0.90f, 0.70f, 0.90f),
                        new Color(0.30f, 0.60f, 0.34f));
                    break;
                case 1: // a framed picture on a stand
                    PrototypeVisuals.CreatePrimitive("Ayak", PrimitiveType.Cube, prop.transform,
                        new Vector3(0f, 0.45f, 0f), new Vector3(0.10f, 0.90f, 0.10f),
                        new Color(0.36f, 0.28f, 0.22f));
                    PrototypeVisuals.CreatePrimitive("Çerçeve", PrimitiveType.Cube, prop.transform,
                        new Vector3(0f, 1.25f, 0f), new Vector3(0.92f, 0.68f, 0.07f),
                        new Color(0.36f, 0.28f, 0.22f));
                    PrototypeVisuals.CreatePrimitive("Resim", PrimitiveType.Cube, prop.transform,
                        new Vector3(0f, 1.25f, -0.05f), new Vector3(0.78f, 0.54f, 0.04f),
                        new Color(0.94f, 0.76f, 0.42f));
                    break;
                case 2: // a neon sign
                    PrototypeVisuals.CreatePrimitive("Direk", PrimitiveType.Cylinder, prop.transform,
                        new Vector3(0f, 0.70f, 0f), new Vector3(0.10f, 0.70f, 0.10f),
                        new Color(0.30f, 0.32f, 0.34f));
                    PrototypeVisuals.CreatePrimitive("Tabela", PrimitiveType.Cube, prop.transform,
                        new Vector3(0f, 1.58f, 0f), new Vector3(1.10f, 0.52f, 0.09f),
                        new Color(0.20f, 0.22f, 0.26f));
                    HeatGlow.Attach(prop.transform, new Vector3(0f, 1.58f, -0.07f),
                        new Vector3(0.86f, 0.30f, 0.05f));
                    break;
                default: // a rug
                    PrototypeVisuals.CreatePrimitive("Halı", PrimitiveType.Cube, prop.transform,
                        new Vector3(0f, 0.02f, 0f), new Vector3(2.4f, 0.03f, 1.7f),
                        new Color(0.72f, 0.32f, 0.28f));
                    PrototypeVisuals.CreatePrimitive("Halı Deseni", PrimitiveType.Cube, prop.transform,
                        new Vector3(0f, 0.035f, 0f), new Vector3(1.9f, 0.03f, 1.25f),
                        new Color(0.90f, 0.72f, 0.44f));
                    break;
            }
            return prop;
        }

        /// <summary>
        /// The stockroom pallet the fridge is refilled from: crates on the floor
        /// with loose cups on top, so it reads as stock rather than as a counter.
        /// </summary>
        private static void DecorateDrinkCrate(Transform station)
        {
            if (MeshyVisuals.IsAvailable(ShopCrate))
            {
                for (int i = 0; i < 3; i++)
                    MeshyVisuals.TryAttach(station, ShopCrate,
                        new Vector3(0.82f, 0.62f, 0.68f),
                        new Vector3(-0.5f + i % 2 * 0.78f, i > 1 ? 0.3f : 0f, i > 1 ? 0.1f : 0f),
                        new Vector3(0f, i * 12f, 0f), false);
                for (int i = 0; i < 4; i++)
                    PrototypeVisuals.CreateItemVisual(ItemType.Drink, station,
                        new Vector3(-0.45f + i * 0.3f, 0.68f, -0.28f), 1.1f);
                MeshyVisuals.HideDirectRenderers(station, "Counter", "Work Top");
                return;
            }

            PrototypeVisuals.CreatePrimitive("Palet", PrimitiveType.Cube, station,
                new Vector3(0f, 0.08f, 0f), new Vector3(1.9f, 0.16f, 1.2f),
                new Color(0.62f, 0.45f, 0.30f));
            for (int row = 0; row < 2; row++)
            for (int column = 0; column < 4; column++)
                PrototypeVisuals.CreateItemVisual(ItemType.Drink, station,
                    new Vector3(-0.6f + column * 0.4f, 0.95f + row * 0.30f, 0.1f), 1.1f);
        }

        private static void DecorateFridge(Transform station)
        {
            // The source model is a wide commercial display case. At raw authored
            // scale it spans almost five metres and dominates the dining room;
            // fit it to a compact two-door footprint while preserving its aspect.
            if (MeshyVisuals.TryReplaceDirect(
                    station, "190_kitchen_fridge", new Vector3(2.55f, 1.65f, 0.95f),
                    Vector3.zero, FacingCustomer, false, "Counter", "Work Top"))
                return;

            PrototypeVisuals.CreatePrimitive("Dolap Gövdesi", PrimitiveType.Cube, station,
                new Vector3(0f, 1.05f, 0.12f), new Vector3(1.5f, 2.10f, 0.85f),
                new Color(0.88f, 0.91f, 0.94f), colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Cam", PrimitiveType.Cube, station,
                new Vector3(0f, 1.20f, -0.32f), new Vector3(1.24f, 1.55f, 0.06f),
                new Color(0.62f, 0.82f, 0.90f));
            PrototypeVisuals.CreatePrimitive("Kasa Üstü", PrimitiveType.Cube, station,
                new Vector3(0f, 2.16f, 0.12f), new Vector3(1.6f, 0.14f, 0.95f),
                new Color(0.36f, 0.55f, 0.70f));
            for (int shelf = 0; shelf < 3; shelf++)
                PrototypeVisuals.CreatePrimitive("Raf", PrimitiveType.Cube, station,
                    new Vector3(0f, 0.66f + shelf * 0.46f, -0.1f),
                    new Vector3(1.20f, 0.05f, 0.45f), new Color(0.74f, 0.78f, 0.81f));
        }

        /// <summary>
        /// The bakery: an oven under an extractor hood, with a tray of what it
        /// sells left out on the counter beside it.
        /// </summary>
        private static void DecorateDessertOven(Transform station)
        {
            if (MeshyVisuals.TryReplaceDirectAuthored(
                    station, "151_shop_oven", new Vector3(0f, 0f, 0.2f), FacingCustomer,
                    "Counter", "Work Top"))
            {
                MeshyVisuals.TryAttachAuthored(station, "191_kitchen_hood",
                    new Vector3(0f, 2.05f, 0.35f), FacingCustomer);
                for (int i = 0; i < 3; i++)
                    PrototypeVisuals.CreateItemVisual(ItemType.Dessert, station,
                        new Vector3(-0.32f + i * 0.32f, 1.62f, 0.2f), 0.7f);
                return;
            }

            PrototypeVisuals.CreatePrimitive("Fırın Gövdesi", PrimitiveType.Cube, station,
                new Vector3(0f, 0.80f, 0.15f), new Vector3(1.7f, 1.60f, 0.90f),
                new Color(0.78f, 0.52f, 0.36f), colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Fırın Kapağı", PrimitiveType.Cube, station,
                new Vector3(0f, 0.95f, -0.32f), new Vector3(1.20f, 0.70f, 0.08f),
                new Color(0.30f, 0.24f, 0.22f));
            PrototypeVisuals.CreatePrimitive("Baca", PrimitiveType.Cylinder, station,
                new Vector3(0.62f, 1.85f, 0.15f), new Vector3(0.22f, 0.35f, 0.22f),
                new Color(0.42f, 0.34f, 0.30f));
            PrototypeVisuals.CreatePrimitive("Üst Tabla", PrimitiveType.Cube, station,
                new Vector3(0f, 1.64f, 0.15f), new Vector3(1.8f, 0.12f, 1.0f),
                PrototypeVisuals.Cream);
        }

        private const string ShopCrate = "166_shop_crate";

        /// <summary>
        /// Dresses the wall the kitchen line backs onto: shelves and a knife rack
        /// over the counters, and the stock the line runs on stacked in the corner
        /// past the rack.
        ///
        /// It all hangs off the kitchen root rather than off the stations. A
        /// station's own children are load-bearing - the trays, the label height
        /// and the visual the working shake picks up all read from them - and a
        /// jar of pickles is not worth putting inside that.
        /// </summary>
        private void DressKitchenWall(Transform kitchenRoot, RestaurantLayoutConfig layout)
        {
            if (!MeshyVisuals.IsAvailable("198_kitchen_wall_shelf_hooks")) return;

            GameObject dressing = new("Mutfak Duvarı");
            dressing.transform.SetParent(kitchenRoot, false);
            Transform parent = dressing.transform;

            float wallZ = shopWorld.BackWallZ - 0.14f;
            float floorY = layout.Oven.y;

            // Hung in the top half of the wall. The perimeter runs 1.47 m, so
            // anything mounted at head height ends up over the top of it, hanging
            // in the air above the street.
            MeshyVisuals.TryAttachAuthored(parent, "198_kitchen_wall_shelf_hooks",
                new Vector3(layout.MeatSource.x, floorY + 0.9f, wallZ), FacingCustomer);
            MeshyVisuals.TryAttachAuthored(parent, "197_kitchen_wall_shelf",
                new Vector3(layout.Cutting.x, floorY + 1.05f, wallZ), FacingCustomer);
            MeshyVisuals.TryAttachAuthored(parent, "199_kitchen_knife_rack",
                new Vector3(layout.Cutting.x + 1.5f, floorY + 1.0f, wallZ), FacingCustomer);

            // The rack's own stock, in the north-west corner. West of the
            // drive-through window rather than beside the rack: the window is set
            // in this same wall and the delivery would be stacked in it.
            float storeX = layout.TrashBin.x;
            MeshyVisuals.TryAttachAuthored(parent, "168_shop_crate_ham",
                new Vector3(storeX, floorY, wallZ - 0.5f), new Vector3(0f, 8f, 0f));
            MeshyVisuals.TryAttachAuthored(parent, "167_shop_crate_buns",
                new Vector3(storeX + 0.82f, floorY, wallZ - 0.45f), new Vector3(0f, -6f, 0f));
            MeshyVisuals.TryAttachAuthored(parent, ShopCrate,
                new Vector3(storeX + 0.1f, floorY + 0.3f, wallZ - 0.52f), new Vector3(0f, 15f, 0f));
            // At the far end of the line rather than beside the crates: the corner
            // between the west wall and the drive-through bay is only two metres
            // wide, and the barrel was standing in the bay itself.
            MeshyVisuals.TryAttachAuthored(parent, "255_food_barrel",
                new Vector3(layout.Service.x + 1.4f, floorY, wallZ - 0.55f), Vector3.zero);
        }

        private GameObject CreateTrashBin(Transform parent, Vector3 position)
        {
            GameObject trashBinObject = new("Çöp Kutusu");
            trashBinObject.transform.SetParent(parent, false);
            trashBinObject.transform.localPosition = position;
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
            MarkPlaceable(trashBinObject, "utility.trash_bin", "Çöp Kutusu");
            MeshyVisuals.TryReplaceDirectAuthored(
                trashBinObject.transform, "17_trash_bin",
                Vector3.zero, FacingCustomer,
                "Çöp Gövdesi", "Çöp Kapak", "Çöp Açıklığı", "Ayak Pedalı");
            return trashBinObject;
        }

        /// <summary>
        /// The drive-through window, set in the opening left in the back wall and
        /// facing the driveway. It stays shut until its pad is paid; opening it is
        /// also what lets a car into the lane outside, so the shop never has cars
        /// driving past a window that cannot serve them.
        /// </summary>
        private TakeawaySystem CreateDriveThruWindow(
            Transform parent, GameConfig gameConfig, RestaurantLayoutConfig layout)
        {
            GameObject windowRoot = new("Drive-Thru Penceresi");
            windowRoot.transform.SetParent(parent, false);
            windowRoot.transform.localPosition = new Vector3(
                shopWorld.DriveThruWindowX, layout.DriveThruCounter.y,
                shopWorld.BackWallZ - 0.78f);

            TakeawaySystem window = windowRoot.AddComponent<TakeawaySystem>();
            window.Configure(playerTransform, inventory);
            MarkPlaceable(windowRoot, "station.drive_thru", "Drive-Thru Tezgâhı");
            // Turned to face the shop, like every other counter. The camera only
            // ever sees the -Z faces, so a counter facing the driveway showed the
            // player its blank back; what marks this one as a drive-through is the
            // opening in the wall behind it and the lane on the other side.
            if (MeshyVisuals.TryReplaceDirectAuthored(
                    windowRoot.transform, "12_service_cashier_counter",
                    Vector3.zero, FacingCustomer,
                    "Takeaway Counter Body", "Takeaway Counter Top"))
                window.SetCounterTopHeight(1.51f);
            window.SetStaffSide(-1f);
            // Shut until bought. This has to happen before the pad is wired: a
            // saved purchase reopens the window from inside Configure, and hiding
            // it afterwards would close a drive-through the player already owns.
            windowRoot.SetActive(false);

            GameObject unlockPad = new("Drive-Thru Pedi");
            unlockPad.transform.SetParent(parent, false);
            unlockPad.transform.localPosition = new Vector3(
                shopWorld.DriveThruWindowX, layout.DriveThruUnlockPad.y,
                shopWorld.BackWallZ - 2.9f);

            if (!gameConfig.Features.Takeaway)
            {
                unlockPad.SetActive(false);
                return window;
            }

            PurchasePad driveThruPad = unlockPad.AddComponent<PurchasePad>();
            driveThruPad.Configure(
                playerTransform, "drivethru.unlocked", new[] { ShopPrices.DriveThru },
                (_, __) => OpenDriveThru(window), "18_money_collection_pad");
            driveThruPad.SetPreview(
                "244_food_bag", new Vector3(0.62f, 0.72f, 0.48f));
            return window;
        }

        private void OpenDriveThru(TakeawaySystem window)
        {
            // The wall comes down and the window goes in. Until now the back of
            // the shop was solid, which is what a shop without a drive-through
            // looks like.
            shopWorld.OpenDriveThruBay();
            window.gameObject.SetActive(true);
            if (traffic == null || cityLayout == null) return;
            traffic.OpenServiceLane(window, shopWorld.DriveThruWindowX);
            CityBlock.BuildDriveway(runtimeRoot, cityLayout, shopWorld.DriveThruWindowX);
        }

        /// <summary>
        /// The office block, hard into the lot's south-west corner. Two rooms
        /// sharing the wall between them, using the lot's own west wall as the far
        /// side of the first.
        /// </summary>
        private const float OfficeWestX = -10.9f;
        private const float OfficeWidth = 6f;
        private const float OfficeSouthZ = -8.1f;
        private const float OfficeNorthZ = -4.1f;

        /// <summary>Wall base, level with the lot surface.</summary>
        private const float WallBaseY = 0.20f;

        /// <summary>
        /// One closed office: south wall, east wall, and a north wall with a 3 m
        /// doorway onto the dining room. The desk stands against the west side
        /// facing east, which is both the way the camera looks and the side the
        /// player walks up to it from.
        ///
        /// The rooms used to be three loose wall runs sitting mid-floor on a
        /// staircase of offsets, which read as a pile of walls rather than rooms.
        /// </summary>
        private void CreateOffice(
            Transform parent, string roomName, string deskAsset, float westX,
            ManagementMenu menu, string title, string saveKey, int cost, ManagementMenuHUD hud)
        {
            GameObject room = new(roomName);
            room.transform.SetParent(parent, false);

            float eastX = westX + OfficeWidth;
            float centreZ = (OfficeSouthZ + OfficeNorthZ) * 0.5f;
            float doorWestX = eastX - DoorWidth;

            // Runs are laid corner to corner and panelled to fit, so a side never
            // ends in a stub of wall standing beside the room. Each yaw turns the
            // panel's finished +Z face into the room.
            AddRoomWall(room.transform,
                new Vector3(westX, WallBaseY, OfficeSouthZ),
                new Vector3(eastX, WallBaseY, OfficeSouthZ), false, 0f);
            AddRoomWall(room.transform,
                new Vector3(eastX, WallBaseY, OfficeSouthZ),
                new Vector3(eastX, WallBaseY, OfficeNorthZ), true, -90f);

            // North wall: panelled from the west corner up to the door, then the
            // door in the last stretch.
            AddRoomWall(room.transform,
                new Vector3(westX, WallBaseY, OfficeNorthZ),
                new Vector3(doorWestX, WallBaseY, OfficeNorthZ), false, 180f);
            AddDoorway(room.transform,
                new Vector3((doorWestX + eastX) * 0.5f, WallBaseY, OfficeNorthZ), DoorWidth);

            ManagementOffice office = room.AddComponent<ManagementOffice>();
            office.Configure(playerTransform, hud, menu, deskAsset, title,
                new Vector3(westX + 1.3f, 0.25f, centreZ), 90f);

            CreatePurchasePad(parent, roomName + " Pedi",
                new Vector3(eastX - 1.6f, 0.28f, centreZ), saveKey,
                new[] { cost }, (_, __) => office.Furnish(), "18_money_collection_pad",
                previewAsset: "218_office_desk",
                previewSize: new Vector3(0.82f, 0.68f, 0.68f), previewYaw: 90f);
        }

        /// <summary>Clear width of an office door.</summary>
        private const float DoorWidth = 1.7f;

        // The kit's own wall palette, so the door frame built from primitives sits
        // against the panels beside it without reading as a patch.
        private static readonly Color WallPanel = new(0.941f, 0.902f, 0.824f);
        private static readonly Color WallCap = new(0.353f, 0.251f, 0.196f);

        /// <summary>
        /// One side of an office, panelled with the same tiled kit as the shop's
        /// own walls. Panels are spaced to divide the side exactly rather than at
        /// their nominal 1.41 m, so a 6 m run of 1.41 m panels overlaps slightly
        /// instead of leaving a stub of bare floor at one end.
        /// </summary>
        private static void AddRoomWall(
            Transform room, Vector3 from, Vector3 to, bool alongZ, float yaw)
        {
            float span = Vector3.Distance(from, to);
            int count = Mathf.Max(1, Mathf.RoundToInt(span / RoomWallModule));
            for (int i = 0; i < count; i++)
            {
                Vector3 position = Vector3.Lerp(from, to, (i + 0.5f) / count);
                CityKit.Spawn(RoomWall, room, position, yaw);
                AddWallBlocker(room, position,
                    alongZ ? span / count : RoomWallThickness,
                    alongZ ? RoomWallThickness : span / count);
            }
        }

        private const string RoomWall = "265_wall_plain_straight";
        private const float RoomWallModule = 1.41f;
        private const float RoomWallThickness = 0.36f;
        private const float RoomWallHeight = 2.82f;

        /// <summary>
        /// A framed door standing open. The opening stays walkable - the leaf is
        /// swung back against the wall and nothing here blocks the way through.
        /// </summary>
        private static void AddDoorway(Transform room, Vector3 centre, float width)
        {
            GameObject door = new("Kapı");
            door.transform.SetParent(room, false);
            door.transform.localPosition = centre;

            float half = width * 0.5f;
            foreach (float x in new[] { -half, half })
                PrototypeVisuals.CreatePrimitive("Söve", PrimitiveType.Cube, door.transform,
                    new Vector3(x, RoomWallHeight * 0.5f, 0f),
                    new Vector3(0.16f, RoomWallHeight, 0.34f), WallCap);
            PrototypeVisuals.CreatePrimitive("Lento", PrimitiveType.Cube, door.transform,
                new Vector3(0f, RoomWallHeight - 0.32f, 0f),
                new Vector3(width + 0.16f, 0.64f, 0.37f), WallPanel);

            // Hinged on the west jamb and swung into the room, so the doorway it
            // frames stays clear.
            GameObject leaf = new("Kanat");
            leaf.transform.SetParent(door.transform, false);
            leaf.transform.localPosition = new Vector3(-half + 0.08f, 0f, 0f);
            leaf.transform.localEulerAngles = new Vector3(0f, -108f, 0f);
            if (MeshyVisuals.TryAttachAuthored(leaf.transform, "186_shop_door_glazed",
                    new Vector3(0f, 0f, width * 0.46f), new Vector3(0f, 90f, 0f)) != null)
                return;

            PrototypeVisuals.CreatePrimitive("Kanat Gövdesi", PrimitiveType.Cube, leaf.transform,
                new Vector3(0f, 0.70f, width * 0.46f), new Vector3(0.09f, 1.34f, width * 0.9f),
                new Color(0.78f, 0.36f, 0.24f));
            PrototypeVisuals.CreatePrimitive("Kol", PrimitiveType.Cube, leaf.transform,
                new Vector3(-0.07f, 0.78f, width * 0.82f), new Vector3(0.07f, 0.07f, 0.20f),
                new Color(0.93f, 0.82f, 0.42f));
        }

        private static void AddWallBlocker(Transform room, Vector3 position, float depthZ, float widthX)
        {
            GameObject blocker = new("Wall Blocker");
            blocker.transform.SetParent(room, false);
            blocker.transform.localPosition = position + Vector3.up * (RoomWallHeight * 0.5f);
            blocker.AddComponent<BoxCollider>().size = new Vector3(
                Mathf.Max(RoomWallThickness, widthX), RoomWallHeight,
                Mathf.Max(RoomWallThickness, depthZ));
        }

        private void CreatePlanter(Transform parent, Vector3 position, string stableId)
        {
            GameObject planter = new("Saksı");
            planter.transform.SetParent(parent, false);
            planter.transform.localPosition = position;
            MarkPlaceable(planter, stableId, "Saksı");
            if (MeshyVisuals.TryAttachAuthored(
                    planter.transform, "33_decorative_plant", Vector3.zero, Vector3.zero) == null)
                PrototypeVisuals.CreatePrimitive("Planter Fallback", PrimitiveType.Cylinder,
                    planter.transform, new Vector3(0f, 0.3f, 0f),
                    new Vector3(0.5f, 0.3f, 0.5f), new Color(0.78f, 0.45f, 0.32f));
        }

        /// <summary>
        /// Moves a station's two stacks onto trays at the front of its counter and
        /// returns where the finished goods now sit.
        ///
        /// The authored anchors put both piles on the machine's centre line, so
        /// meat grew out of the middle of the rack and wraps out of the top of the
        /// spit. Food belongs on a tray on the counter, facing the person who is
        /// going to pick it up.
        /// </summary>
        private static Vector3 ApplyAuthoredStationLayout(
            ItemStation station, float maxLabelHeight, float trayZ)
        {
            Vector3 output = new(0.52f, 0.95f, trayZ);
            if (station == null) return output;

            bool hasInput = MeshyVisuals.TryFindAnchor(
                station.transform, "INPUT_ANCHOR", out Transform inputAnchor);
            bool hasOutput = MeshyVisuals.TryFindAnchor(
                station.transform, "OUTPUT_ANCHOR", out Transform outputAnchor);

            float top = 0.95f;
            if (hasInput && hasOutput)
            {
                top = Mathf.Max(
                    station.transform.InverseTransformPoint(inputAnchor.position).y,
                    station.transform.InverseTransformPoint(outputAnchor.position).y);
            }

            // Clamped to counter height. The spit's anchors sit at the top of a
            // three metre machine, which put its trays up in the air above the
            // player's head.
            top = Mathf.Clamp(top, 0.85f, 1.15f);

            Vector3 input = new(-0.52f, top, trayZ);
            output = new Vector3(0.52f, top, trayZ);
            // A source has no input and a service counter stores only finished
            // goods. Building both trays for every station left one obviously
            // empty tray in front of racks, fridges and tills.
            if (station.InputType != ItemType.None && station.Mode != StationMode.Service)
                BuildTray(station.transform, input);
            ItemType visibleOutput = station.Mode == StationMode.Service
                ? station.InputType
                : station.OutputType;
            if (visibleOutput != ItemType.None)
                BuildTray(station.transform, output);
            station.SetVisualLayout(input, output, maxLabelHeight);

            return output;
        }

        /// <summary>
        /// Earlier builds could save the hidden drink line after it inherited a
        /// transient editor position. Repair only that clearly displaced legacy
        /// pair once; ordinary player-authored layouts remain untouched.
        /// </summary>
        private static void RepairLegacyDrinkLineLayout(
            RestaurantLayoutConfig layout, ItemStation drinkCrate, ItemStation fridge)
        {
            const string migrationKey = "visual_fix.drink_line_layout.v2";
            if (GameProgress.GetInt(migrationKey, 0) > 0) return;

            PlaceableObject cratePlaceable = drinkCrate.GetComponent<PlaceableObject>();
            PlaceableObject fridgePlaceable = fridge.GetComponent<PlaceableObject>();
            cratePlaceable?.EnsureInitialized();
            fridgePlaceable?.EnsureInitialized();

            bool crateDisplaced = Vector3.SqrMagnitude(
                drinkCrate.transform.position - layout.DrinkCrate) > 16f;
            bool fridgeDisplaced = Vector3.SqrMagnitude(
                fridge.transform.position - layout.Fridge) > 16f;
            if (crateDisplaced || fridgeDisplaced)
            {
                cratePlaceable?.ResetToDefault();
                fridgePlaceable?.ResetToDefault();
                cratePlaceable?.Commit();
                fridgePlaceable?.Commit();
            }

            GameProgress.SetInt(migrationKey, 1);
            GameProgress.FlushNow();
        }

        /// <summary>A shallow open tray for a stack to grow out of.</summary>
        private static void BuildTray(Transform station, Vector3 at)
        {
            GameObject tray = new("Tepsi");
            tray.transform.SetParent(station, false);
            tray.transform.localPosition = at - Vector3.up * 0.03f;

            Color body = new(0.80f, 0.83f, 0.85f);
            Color rim = new(0.55f, 0.60f, 0.63f);
            const float halfX = 0.42f;
            const float halfZ = 0.30f;

            PrototypeVisuals.CreatePrimitive("Taban", PrimitiveType.Cube, tray.transform,
                Vector3.zero, new Vector3(halfX * 2f, 0.04f, halfZ * 2f), body);
            PrototypeVisuals.CreatePrimitive("Kenar Ön", PrimitiveType.Cube, tray.transform,
                new Vector3(0f, 0.03f, -halfZ), new Vector3(halfX * 2f, 0.08f, 0.04f), rim);
            PrototypeVisuals.CreatePrimitive("Kenar Arka", PrimitiveType.Cube, tray.transform,
                new Vector3(0f, 0.03f, halfZ), new Vector3(halfX * 2f, 0.08f, 0.04f), rim);
            PrototypeVisuals.CreatePrimitive("Kenar Sol", PrimitiveType.Cube, tray.transform,
                new Vector3(-halfX, 0.03f, 0f), new Vector3(0.04f, 0.08f, halfZ * 2f), rim);
            PrototypeVisuals.CreatePrimitive("Kenar Sağ", PrimitiveType.Cube, tray.transform,
                new Vector3(halfX, 0.03f, 0f), new Vector3(0.04f, 0.08f, halfZ * 2f), rim);
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

        private static void DecorateCuttingCounter(Transform station)
        {
            PrototypeVisuals.CreatePrimitive("Cutting Board", PrimitiveType.Cube, station,
                new Vector3(0f, 1.03f, 0f), new Vector3(0.95f, 0.06f, 0.72f),
                new Color(0.68f, 0.42f, 0.22f));
            PrototypeVisuals.CreatePrimitive("Knife", PrimitiveType.Cube, station,
                new Vector3(0.35f, 1.13f, 0f), new Vector3(0.65f, 0.05f, 0.12f),
                new Color(0.72f, 0.76f, 0.78f), new Vector3(0f, 25f, 0f));
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

        private CustomerTable CreateTable(
            Transform parent, string tableName, Vector3 localPosition, string stableId)
        {
            GameObject tableObject = new(tableName);
            tableObject.transform.SetParent(parent, false);
            tableObject.transform.localPosition = localPosition;
            MarkPlaceable(tableObject, stableId, tableName);

            PrototypeVisuals.CreatePrimitive("Table Top", PrimitiveType.Cube, tableObject.transform,
                new Vector3(0f, 0.72f, 0f), new Vector3(1.20f, 0.16f, 0.80f),
                new Color(0.56f, 0.32f, 0.20f), colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Table Leg", PrimitiveType.Cube, tableObject.transform,
                new Vector3(0f, 0.35f, 0f), new Vector3(0.28f, 0.70f, 0.28f),
                new Color(0.35f, 0.22f, 0.18f), colliderEnabled: true);
            CreateDiningChair(tableObject.transform, "Customer Chair", -1.02f, 0f);
            CreateDiningChair(tableObject.transform, "Guest Chair", 1.02f, 180f);

            GameObject seat = new("Customer Seat A");
            seat.transform.SetParent(tableObject.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0f, -1.05f);
            // The seat is south of the table, so looking north is looking at it.
            seat.transform.localRotation = Quaternion.identity;

            GameObject secondSeat = new("Customer Seat B");
            secondSeat.transform.SetParent(tableObject.transform, false);
            secondSeat.transform.localPosition = new Vector3(0f, 0f, 1.05f);
            secondSeat.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            CustomerTable table = tableObject.AddComponent<CustomerTable>();
            table.Configure(playerTransform, seat.transform, secondSeat.transform);

            bool swapped = MeshyVisuals.TryReplaceDirectAuthored(
                tableObject.transform, "15_dining_table_clean",
                Vector3.zero, FacingCustomer,
                "Table Top", "Table Leg", "Customer Chair", "Guest Chair");
            if (swapped)
            {
                if (MeshyVisuals.TryFindAnchor(
                        tableObject.transform, "SEAT_A", out Transform authoredSeat))
                {
                    Vector3 localSeat = tableObject.transform.InverseTransformPoint(authoredSeat.position);
                    seat.transform.localPosition = new Vector3(localSeat.x, 0f, localSeat.z);
                    Vector3 towardTable = -seat.transform.localPosition;
                    towardTable.y = 0f;
                    if (towardTable.sqrMagnitude > 0.001f)
                        seat.transform.localRotation = Quaternion.LookRotation(towardTable, Vector3.up);

                    if (MeshyVisuals.TryFindAnchor(
                            tableObject.transform, "SEAT_B", out Transform authoredSecondSeat))
                    {
                        Vector3 localSecond = tableObject.transform.InverseTransformPoint(
                            authoredSecondSeat.position);
                        secondSeat.transform.localPosition = new Vector3(localSecond.x, 0f, localSecond.z);
                    }
                    else secondSeat.transform.localPosition = -seat.transform.localPosition;

                    Vector3 secondTowardTable = -secondSeat.transform.localPosition;
                    secondTowardTable.y = 0f;
                    if (secondTowardTable.sqrMagnitude > 0.001f)
                        secondSeat.transform.localRotation = Quaternion.LookRotation(
                            secondTowardTable, Vector3.up);
                }

                // Both states are authored, so the table swaps whole models
                // rather than dressing the clean one with loose props.
                Transform clean = tableObject.transform.Find("15_dining_table_clean Visual");
                GameObject dirty = MeshyVisuals.TryAttachAuthored(
                    tableObject.transform, "16_dirty_table_props",
                    Vector3.zero, FacingCustomer);
                if (clean != null && dirty != null)
                    table.SetTableVariants(clean.gameObject, dirty);
            }
            return table;
        }

        private static PlaceableObject MarkPlaceable(GameObject target, string stableId, string label)
        {
            PlaceableObject placeable = target.GetComponent<PlaceableObject>();
            if (placeable == null) placeable = target.AddComponent<PlaceableObject>();
            placeable.Configure(stableId, label);
            return placeable;
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

        private void CreateTutorialArrow(
            ItemStation source, ItemStation oven, ItemStation cutting, ItemStation service)
        {
            GameObject tutorial = new("Öğretici Ok");
            tutorial.transform.SetParent(runtimeRoot, false);
            PrototypeVisuals.CreatePrimitive("Ok Gövdesi", PrimitiveType.Cylinder, tutorial.transform, Vector3.zero,
                new Vector3(0.20f, 0.52f, 0.20f), new Color(1f, 0.82f, 0.16f));
            PrototypeVisuals.CreatePrimitive("Ok Ucu", PrimitiveType.Sphere, tutorial.transform, Vector3.down * 0.38f,
                new Vector3(0.46f, 0.24f, 0.46f), new Color(1f, 0.82f, 0.16f));
            tutorial.AddComponent<TutorialArrow>()
                .Configure(inventory, source.transform, oven.transform,
                    cutting.transform, service.transform);
        }

        /// <summary>
        /// Top of the pavement running down the shop's west flank, where the queue
        /// comes from. Read off the lot rather than written down, so the walk stays
        /// on the paving if the lot is ever resized.
        /// </summary>
        private Vector3 ApproachStart(RestaurantLayoutConfig layout) =>
            new(FlankPavementX, layout.CustomerEntry.y, cityLayout.LotDepth * 0.35f);

        /// <summary>
        /// The turn at the bottom of that pavement, out on the forecourt clear of
        /// the fence, from which the walk turns east for the gate.
        /// </summary>
        private Vector3 ApproachCorner(RestaurantLayoutConfig layout) =>
            new(FlankPavementX, layout.CustomerEntry.y, -(cityLayout.LotDepth * 0.5f + 2.8f));

        /// <summary>Middle of the paved strip between the lot and the west facades.</summary>
        private float FlankPavementX =>
            -(cityLayout.LotWidth * 0.5f + cityLayout.SideWalkGap * 0.5f);

        private Transform CreateMarker(Transform parent, string markerName, Vector3 position)
        {
            GameObject marker = new(markerName);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = position;
            return marker.transform;
        }
    }
}
