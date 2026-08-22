#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ShawarmaTycoon.Tests
{
    public sealed class Phase3DioramaRuntimeTests
    {
        private GameObject bootstrapHost;
        private DioramaWorld world;
        private DioramaExpansion expansion;

        [OneTimeSetUp]
        public void BuildCleanPrototypeForFixture()
        {
            DestroyRuntimeIfPresent();
            SaveRepository.ResetStateForTests();
            SaveRepository.InitializeForTests(new MemorySaveProvider());
            GameProgress.SetInt("expansion", 0);

            bootstrapHost = new GameObject("Phase 3 Test Bootstrap");
            bootstrapHost.AddComponent<ShawarmaPrototypeBootstrap>();
            world = Object.FindFirstObjectByType<DioramaWorld>();
            expansion = Object.FindFirstObjectByType<DioramaExpansion>();
        }

        [SetUp]
        public void RestoreLockedExpansionState()
        {
            Assert.That(world, Is.Not.Null, "The Phase 3 bootstrap did not create a DioramaWorld.");
            Assert.That(expansion, Is.Not.Null, "The Phase 3 bootstrap did not create DioramaExpansion.");
            GameProgress.SetInt("expansion", 0);
            expansion.Configure(world.ExpansionModules);
        }

        [OneTimeTearDown]
        public void TearDownFixture()
        {
            DestroyRuntimeIfPresent();
            if (bootstrapHost != null) Object.DestroyImmediate(bootstrapHost);
            SaveRepository.ResetStateForTests();
        }

        [Test]
        public void Prototype_StandsOnTheStreetAndStartsWithLockedWings()
        {
            Assert.That(Object.FindObjectsByType<DioramaWorld>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            // Core plus every purchasable plot; the plot count is a design choice
            // the config owns, so this follows it rather than repeating it.
            int expectedModules = world.ExpansionModules.Count + 1;
            Assert.That(world.GetComponentsInChildren<DioramaModule>(true),
                Has.Length.EqualTo(expectedModules));
            Assert.That(world.BaseModule, Is.Not.Null);
            Assert.That(world.BaseModule.IsBaseModule, Is.True);
            Assert.That(world.BaseModule.IsUnlocked, Is.True);
            Assert.That(world.ExpansionModules.Count, Is.GreaterThan(0));
            Assert.That(world.WalkableRegistry.ActiveSurfaceCount, Is.EqualTo(1));
            // The lot has an outside now: a block around it, traffic on the road
            // and a gate in the fence between the two.
            Assert.That(Object.FindFirstObjectByType<TrafficSystem>(), Is.Not.Null);
            Assert.That(GameObject.Find("City Ground"), Is.Not.Null);
            Assert.That(world.ShellRoot, Is.Not.Null);
            Assert.That(world.EntranceAnchor, Is.Not.Null);

            foreach (DioramaModule module in world.ExpansionModules)
            {
                Assert.That(module.IsUnlocked, Is.False);
                Assert.That(module.SurfaceRoot.gameObject.activeSelf, Is.False);
                Assert.That(module.VisualRoot.gameObject.activeSelf, Is.False);
                Assert.That(module.ContentRoot.gameObject.activeSelf, Is.False);
                Assert.That(module.LockedPreview, Is.Not.Null);
                Assert.That(module.LockedPreview.activeSelf, Is.True);
                Assert.That(module.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(module.SurfaceRoot.localScale, Is.EqualTo(Vector3.one));
            }
        }

        [Test]
        public void UnlockingWing_ActivatesSafeSurfaceWithoutScalingGameplayRoots()
        {
            DioramaModule first = world.ExpansionModules[0];
            Assert.That(expansion.UnlockNext(), Is.True);

            Assert.That(first.IsUnlocked, Is.True);
            Assert.That(first.SurfaceRoot.gameObject.activeSelf, Is.True);
            Assert.That(first.ContentRoot.gameObject.activeSelf, Is.True);
            Assert.That(first.LockedPreview.activeSelf, Is.False);
            Assert.That(first.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(first.SurfaceRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(first.ContentRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(world.WalkableRegistry.ActiveSurfaceCount, Is.EqualTo(2));

            // Cancel the presentation-only scale animation. This must never alter
            // the module, surface or content transforms used by gameplay.
            first.SetUnlocked(true, false);
            Assert.That(first.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(first.SurfaceRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(first.ContentRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(first.VisualRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(world.WalkableRegistry.ContainsFootprint(first.WalkableBounds.center, 0.25f), Is.True);
        }

        [Test]
        public void UnlockedSurfaces_MeetAtCoreAndWingSeamsWithoutWalkableGaps()
        {
            foreach (DioramaModule module in world.ExpansionModules)
                module.SetUnlocked(true, false);

            Assert.That(world.WalkableRegistry.ActiveSurfaceCount,
                Is.EqualTo(world.ExpansionModules.Count + 1));
            Bounds core = world.BaseModule.WalkableBounds;

            // The plots are a grid rather than a single row now, so rather than
            // naming which module meets what, every seam is found by looking for
            // surfaces that touch and then asked whether a player can stand on it.
            int seamsChecked = 0;
            void AssertSeamWalkable(Vector3 point, string what)
            {
                seamsChecked++;
                Assert.That(world.WalkableRegistry.ContainsFootprint(point, 0.20f), Is.True,
                    $"The player footprint falls through the seam {what}.");
            }

            int againstCore = 0;
            foreach (DioramaModule module in world.ExpansionModules)
            {
                Bounds wing = module.WalkableBounds;
                if (Mathf.Abs(wing.min.x - core.max.x) >= 0.002f) continue;
                againstCore++;
                AssertSeamWalkable(
                    new Vector3(core.max.x, core.max.y, wing.center.z), $"beside '{module.Id}'");
            }
            Assert.That(againstCore, Is.GreaterThan(0),
                "No expansion plot meets the core surface; the grid is detached.");

            foreach (DioramaModule a in world.ExpansionModules)
            foreach (DioramaModule b in world.ExpansionModules)
            {
                if (a == b) continue;
                Bounds west = a.WalkableBounds, east = b.WalkableBounds;
                if (Mathf.Abs(east.min.x - west.max.x) < 0.002f &&
                    Mathf.Abs(east.center.z - west.center.z) < 0.002f)
                    AssertSeamWalkable(
                        new Vector3(west.max.x, west.max.y, west.center.z),
                        $"between '{a.Id}' and '{b.Id}'");
                if (Mathf.Abs(east.min.z - west.max.z) < 0.002f &&
                    Mathf.Abs(east.center.x - west.center.x) < 0.002f)
                    AssertSeamWalkable(
                        new Vector3(west.center.x, west.max.y, west.max.z),
                        $"between '{a.Id}' and '{b.Id}'");
            }

            Assert.That(seamsChecked, Is.GreaterThan(world.ExpansionModules.Count),
                "Found fewer seams than there are plots; the grid is not joined up.");
        }

        [Test]
        public void Camera_KeepsPlayerFocusCenteredAtTheIslandEdge()
        {
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            MobileCameraRig rig = camera.GetComponent<MobileCameraRig>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.FollowTarget, Is.Not.Null);
            Assert.That(rig.FollowTarget.name, Is.EqualTo("Player"));

            Bounds core = world.BaseModule.WalkableBounds;
            Transform player = rig.FollowTarget;
            player.position = new Vector3(core.max.x - 0.8f, player.position.y, core.center.z);
            rig.SendMessage("LateUpdate", SendMessageOptions.RequireReceiver);

            Vector3 focus = player.position + Vector3.up * rig.LookAtHeight;
            Vector3 viewport = camera.WorldToViewportPoint(focus);
            Assert.That(viewport.z, Is.GreaterThan(0f));
            Assert.That(viewport.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(viewport.y, Is.EqualTo(0.5f).Within(0.01f));
        }

        /// <summary>
        /// Everything the player has not paid for yet has to be absent, not
        /// greyed out. A locked belt used to stand in the kitchen from the first
        /// frame with a caption over it, and the drive-through lane had cars
        /// driving past a wall.
        /// </summary>
        [Test]
        public void UnboughtContent_IsAbsentRatherThanShownLocked()
        {
            ConveyorLink[] belts = Object.FindObjectsByType<ConveyorLink>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(belts, Has.Length.EqualTo(3),
                "The line is rack, spit, carving board and till: three belts.");
            foreach (ConveyorLink belt in belts)
            {
                Assert.That(belt.IsUnlocked, Is.False);
                Transform visual = belt.transform.Find("Bant Görseli");
                Assert.That(visual, Is.Not.Null);
                Assert.That(visual.gameObject.activeSelf, Is.False, "An unbought belt is still on show.");
            }

            ManagementOffice[] offices = Object.FindObjectsByType<ManagementOffice>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(offices, Has.Length.EqualTo(2), "Two offices: personnel and the GM.");
            foreach (ManagementOffice office in offices)
            {
                Assert.That(office.IsFurnished, Is.False);
                Assert.That(office.gameObject.activeInHierarchy, Is.True,
                    "The room shell should be standing even before it is furnished.");
                Assert.That(office.GetComponentInChildren<ManagementOfficeTerminal>(true), Is.Null,
                    "An empty office has no desk to open a menu from.");
            }

            TakeawaySystem window = Object.FindFirstObjectByType<TakeawaySystem>(
                FindObjectsInactive.Include);
            Assert.That(window, Is.Not.Null);
            Assert.That(window.gameObject.activeInHierarchy, Is.False);

            TrafficSystem traffic = Object.FindFirstObjectByType<TrafficSystem>();
            Assert.That(traffic.ServiceLaneOpen, Is.False,
                "Cars must not use the lane past the window before it is bought.");
        }

        /// <summary>
        /// The gate is only a gate if the two sides of it are different. The lot
        /// is walkable, the street is not, and both are at one height so nobody
        /// walking in has to climb a step that the customer agents cannot climb.
        /// </summary>
        [Test]
        public void Entrance_SeparatesAWalkableLotFromTheStreetAtOneHeight()
        {
            // The world is built in setup and read in the same frame, so no
            // physics step has run: without this the colliders still report their
            // untransformed unit boxes.
            Physics.SyncTransforms();

            Bounds lot = world.BaseModule.WalkableBounds;
            Vector3 gate = world.EntranceAnchor.position;

            Assert.That(gate.z, Is.GreaterThan(lot.min.z),
                "The gate anchor should stand just inside the lot's front edge.");
            Assert.That(gate.z, Is.LessThan(lot.min.z + 2f));
            Assert.That(world.WalkableRegistry.ContainsFootprint(gate, 0.25f), Is.True,
                "A customer through the gate is not standing on anything walkable.");

            Vector3 street = new(gate.x, gate.y, lot.min.z - 3f);
            Assert.That(world.WalkableRegistry.ContainsFootprint(street, 0.25f), Is.False,
                "The pavement outside the gate must not be player-walkable.");

            // Customers keep the height they spawn at, so the paving they arrive
            // on has to be level with the floor they walk onto. A step either way
            // leaves half the queue sunk into the tiles or hovering over them.
            GameObject forecourt = GameObject.Find("Front Forecourt");
            Assert.That(forecourt, Is.Not.Null, "There is no paving outside the gate.");
            Bounds paving = MeasureRenderers(forecourt);
            Assert.That(paving.max.y, Is.EqualTo(lot.max.y).Within(0.02f),
                "The paving outside the gate is not level with the shop floor.");
            Assert.That(paving.min.z, Is.LessThan(street.z),
                "The paving does not reach the point customers spawn at.");
        }

        private static Bounds MeasureRenderers(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, $"'{root.name}' draws nothing.");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>
        /// Everything the shop sells has to be on the progress bar's roster, and a
        /// new save has to read as nothing built. A pad added without registering
        /// itself would quietly cap the bar below 100% forever.
        /// </summary>
        [Test]
        public void ShopProgress_StartsEmptyAndCountsTheWholeShop()
        {
            int pads = Object.FindObjectsByType<PurchasePad>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Assert.That(pads, Is.GreaterThan(0));

            // Pads, the five hires and the two upgrade boards between them are far
            // more steps than there are pads; this only has to catch a roster that
            // has stopped being wired up at all.
            Assert.That(UpgradeProgress.TotalSteps, Is.GreaterThan(pads + 5),
                "The hires and the office upgrade boards are missing from the bar.");
            Assert.That(UpgradeProgress.OwnedSteps, Is.Zero,
                "A fresh save should read as nothing built yet.");
            Assert.That(UpgradeProgress.Ratio, Is.Zero);
        }

        [Test]
        public void ConveyorPads_BuildOnce_AndStationWorkerPadsAreAbsent()
        {
            Assert.That(ShopPrices.Belt, Has.Length.EqualTo(1),
                "A belt pad still sells invisible levels after building the belt.");

            string[] beltPads = { "Et Bandı Pedi", "Ocak Bandı Pedi", "Kesim Bandı Pedi" };
            foreach (string padName in beltPads)
            {
                GameObject padObject = GameObject.Find(padName);
                Assert.That(padObject, Is.Not.Null, $"'{padName}' was removed with the upgrades.");
                PurchasePad pad = padObject.GetComponent<PurchasePad>();
                Assert.That(pad, Is.Not.Null);
                Assert.That(pad.Level, Is.Zero);
                Assert.That(pad.CurrentCost, Is.EqualTo(ShopPrices.Belt[0]));
            }

            PurchasePad[] pads = Object.FindObjectsByType<PurchasePad>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(pads.Any(p => p.name == "Ocak İşçisi" || p.name == "Kesim İşçisi"),
                Is.False, "A decorative station-worker purchase pad is still in the shop.");
            Assert.That(Object.FindObjectsByType<ItemStation>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .SelectMany(s => s.GetComponentsInChildren<Transform>(true))
                .Any(t => t.name == "İşçi"), Is.False,
                "A removed station worker is still standing beside a machine.");
        }

        [Test]
        public void BuildMode_WiresTheHudAndEveryPlaceableHasAUniquePersistentId()
        {
            BuildModeController controller = Object.FindFirstObjectByType<BuildModeController>();
            Assert.That(controller, Is.Not.Null, "The runtime has no build-mode controller.");
            Assert.That(Object.FindFirstObjectByType<UI.BuildModeHUD>(), Is.Not.Null,
                "The HUD has no build-mode button or toolbar.");

            PlaceableObject[] placeables = Object.FindObjectsByType<PlaceableObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(placeables.Length, Is.GreaterThanOrEqualTo(30),
                "Tables, equipment and decorations were not all registered for build mode.");
            Assert.That(placeables.Select(item => item.StableId).Distinct().Count(),
                Is.EqualTo(placeables.Length), "Two movable objects share one save id.");
            Assert.That(placeables.All(item => !string.IsNullOrWhiteSpace(item.StableId)), Is.True,
                "A movable object cannot persist because its save id is empty.");

            ConveyorLink lockedBelt = Object.FindObjectsByType<ConveyorLink>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).First(link => !link.IsUnlocked);
            Assert.That(lockedBelt.GetComponent<PlaceableObject>().IsSelectable, Is.False,
                "An invisible, unbought belt can still be selected in build mode.");

            RestaurantNavigation navigation = Object.FindFirstObjectByType<RestaurantNavigation>();
            Assert.That(navigation, Is.Not.Null);
            int navigationVersion = navigation.Version;
            float timeScale = Time.timeScale;
            try
            {
                controller.SetBuildMode(true);
                Assert.That(controller.IsActive, Is.True);
                Assert.That(Time.timeScale, Is.Zero, "Restaurant simulation keeps running during layout edits.");
                MobilePlayerController player = Object.FindFirstObjectByType<MobilePlayerController>();
                Assert.That(player.enabled, Is.True,
                    "The player should remain controllable while the restaurant is paused.");
                Assert.That(player.IsBuildModeMovement, Is.True,
                    "The player is not using unscaled movement during the paused build mode.");
                Assert.That(UI.GameHUD.Instance.Joystick.enabled, Is.True,
                    "Mobile movement input was disabled during build mode.");
            }
            finally
            {
                controller.SetBuildMode(false);
                Time.timeScale = timeScale;
            }
            Assert.That(controller.IsActive, Is.False);
            Assert.That(Object.FindFirstObjectByType<MobilePlayerController>().IsBuildModeMovement, Is.False);
            Assert.That(navigation.Version, Is.GreaterThan(navigationVersion),
                "Leaving build mode did not refresh customer routes for the new layout.");
        }

        [Test]
        public void CustomerNavigation_CarvesFurnitureAndReachesOpeningTables()
        {
            RestaurantNavigation navigation = Object.FindFirstObjectByType<RestaurantNavigation>();
            Assert.That(navigation, Is.Not.Null);
            Assert.That(navigation.Version, Is.GreaterThan(0));

            CustomerTable[] tables = Object.FindObjectsByType<CustomerTable>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.That(tables.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(tables.All(table => table.IsSeatApproachClear()), Is.True,
                "An opening table has its chair approach blocked by the authored layout.");

            GameObject queueFront = GameObject.Find("Kuyruk Başı");
            Assert.That(queueFront, Is.Not.Null);
            List<Vector3> corners = new();
            Assert.That(navigation.TryCalculatePath(
                    queueFront.transform.position, tables[0].SeatApproachPoint, corners),
                Is.True, "No complete customer route exists from the till to an opening table.");
            Assert.That(corners.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void BuildMode_RejectsLockedFloorAndFurnitureOverlap()
        {
            BuildModeController controller = Object.FindFirstObjectByType<BuildModeController>();
            CustomerTable[] tables = Object.FindObjectsByType<CustomerTable>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.That(tables.Length, Is.GreaterThanOrEqualTo(2));

            PlaceableObject moving = tables[0].GetComponent<PlaceableObject>();
            Vector3 original = moving.transform.position;
            Quaternion originalRotation = moving.transform.rotation;
            moving.EnsureInitialized();

            try
            {
                Assert.That(controller.CanPlace(moving), Is.True,
                    "An authored opening table starts in an invalid build position.");

                moving.MoveWorld(tables[1].transform.position);
                Assert.That(controller.CanPlace(moving), Is.False,
                    "Two dining tables can be placed through one another.");

                Bounds lot = world.BaseModule.WalkableBounds;
                moving.MoveWorld(new Vector3(lot.max.x + 3f, original.y, lot.center.z));
                Assert.That(controller.CanPlace(moving), Is.False,
                    "Furniture can be dropped outside the unlocked restaurant floor.");
            }
            finally
            {
                moving.transform.SetPositionAndRotation(original, originalRotation);
                Physics.SyncTransforms();
            }
        }

        [Test]
        public void TableTakings_UseAReadableLiraReceiptCard()
        {
            CustomerTable[] tables = Object.FindObjectsByType<CustomerTable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(tables, Is.Not.Empty);

            foreach (CustomerTable table in tables)
            {
                WorldCashMarker marker = table.GetComponentInChildren<WorldCashMarker>(true);
                Assert.That(marker, Is.Not.Null, $"'{table.name}' still has no visual money card.");
                Assert.That(marker.transform.Find("Fiş Kartı/Banknot"), Is.Not.Null,
                    $"'{table.name}' money card has no banknote pictogram.");

                marker.SetAmount(125);
                Assert.That(marker.AmountText, Is.EqualTo("₺125"),
                    "World takings should use the same lira currency as the HUD.");
            }
        }

        [Test]
        public void Stations_DrawOnlyUsefulTrays_AndDrinkLineFitsTheFloor()
        {
            ItemStation meat = FindStation("ET DEPOSU");
            ItemStation oven = FindStation("OCAK");
            ItemStation service = FindStation("SERVİS");
            ItemStation crate = FindStation("İÇECEK DEPOSU");
            ItemStation fridge = FindStation("BUZDOLABI");
            Assert.That(new[] { meat, oven, service, crate, fridge }, Has.None.Null);

            Assert.That(meat.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Tepsi"),
                Is.EqualTo(1), "A source station should not draw an empty input tray.");
            Assert.That(oven.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Tepsi"),
                Is.EqualTo(2), "A processor needs one input and one output tray.");
            Assert.That(service.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Tepsi"),
                Is.EqualTo(1), "A service station should not draw a meaningless second tray.");
            Assert.That(crate.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Tepsi"),
                Is.EqualTo(1));
            Assert.That(fridge.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Tepsi"),
                Is.Zero, "The fridge should use its shelves instead of loose processor trays.");

            BuildModeController controller = Object.FindFirstObjectByType<BuildModeController>();
            Assert.That(controller.CanPlace(crate.GetComponent<PlaceableObject>()), Is.True,
                "The repaired drink rack starts in an invalid build-mode position.");
            Assert.That(controller.CanPlace(fridge.GetComponent<PlaceableObject>()), Is.True,
                "The repaired fridge starts in an invalid build-mode position.");
            Assert.That(fridge.transform.position.x, Is.GreaterThan(10f),
                "The fridge has drifted back into the centre of the dining floor.");
        }

        /// <summary>
        /// A fed station has to keep working with nobody at it. While that needed
        /// the player in range the whole game was standing next to a machine, and
        /// the queue could only ever be as long as one pair of hands could serve.
        /// </summary>
        [Test]
        public void Stations_KeepWorkingWithNobodyStandingAtThem()
        {
            ItemStation oven = FindStation("OCAK");
            Assert.That(oven, Is.Not.Null);
            Assert.That(oven.Mode, Is.EqualTo(StationMode.Processor));

            Transform player = Object.FindFirstObjectByType<MobilePlayerController>().transform;
            player.position = new Vector3(oven.transform.position.x, player.position.y, -6f);

            // Measured against where it starts, not against zero: the shop opens
            // with prepped stock on the line so the first minute is not spent
            // watching a cold kitchen.
            int before = oven.OutputCount;
            Assert.That(oven.TryReceiveFromConveyor(oven.InputType), Is.True);

            // Drive the station directly rather than waiting on wall-clock frames.
            for (int i = 0; i < 400 && oven.OutputCount == before; i++)
                oven.SendMessage("Update", SendMessageOptions.RequireReceiver);

            Assert.That(oven.OutputCount, Is.GreaterThan(before),
                "The spit never finished a batch with the player across the room.");
        }

        private static ItemStation FindStation(string displayName)
        {
            foreach (ItemStation station in Object.FindObjectsByType<ItemStation>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (station.name == displayName) return station;
            return null;
        }

        private static void DestroyRuntimeIfPresent()
        {
            GameObject runtime = GameObject.Find("Shawarma Prototype Runtime");
            if (runtime != null) Object.DestroyImmediate(runtime);
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private SaveData data;

            public bool TryLoad(out SaveData loaded)
            {
                loaded = data;
                return loaded != null;
            }

            public void Save(SaveData value) => data = value;
            public void Delete() => data = null;
        }
    }
}
#endif
