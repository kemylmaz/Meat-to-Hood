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
            UI.StartupPresentation.BypassGameplayPauseForTests = true;
            SaveRepository.ResetStateForTests();
            SaveRepository.InitializeForTests(new MemorySaveProvider());
            GameProgress.SetInt("expansion", 0);
            // This fixture exercises the unrestricted restaurant sandbox. The
            // first-shift flow has its own runtime QA and intentionally pauses a
            // fresh game until the opening button is pressed.
            GameProgress.SetInt(FirstShiftTutorial.CompletionKey, 1);

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
            UI.StartupPresentation.BypassGameplayPauseForTests = false;
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
        public void StartupPresentation_FramesTheShopBeforeGameplayBegins()
        {
            UI.StartupPresentation presentation =
                Object.FindFirstObjectByType<UI.StartupPresentation>(FindObjectsInactive.Include);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.BlocksGameplay, Is.True);
            Assert.That(
                presentation.Stage == UI.StartupPresentation.PresentationStage.InitialLoading ||
                presentation.Stage == UI.StartupPresentation.PresentationStage.MainMenu,
                Is.True);

            Canvas startupCanvas = presentation.GetComponentInChildren<Canvas>(true);
            Assert.That(startupCanvas, Is.Not.Null);
            Assert.That(startupCanvas.sortingOrder, Is.GreaterThan(UI.GameHUD.Instance.Canvas.sortingOrder));
            Assert.That(presentation.transform.Find("Startup Canvas/Safe Area/Loading Screen"), Is.Not.Null);
            Assert.That(presentation.transform.Find("Startup Canvas/Safe Area/Main Menu"), Is.Not.Null);
            Assert.That(presentation.GetComponentsInChildren<Transform>(true)
                    .Count(item => item.name == "Loading Screen" || item.name == "Main Menu"),
                Is.EqualTo(2));
            Assert.That(presentation.WorldReady, Is.True);
            Assert.That(presentation.transform.Find(
                "Startup Canvas/Safe Area/Main Menu/Neighbourhood Green/Menu Content/Open Shop"),
                Is.Not.Null, "The opening presentation has no primary action.");
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
            Assert.That(belts, Has.Length.EqualTo(2),
                "Only storage-to-spit and spit-to-cutting should have belts.");
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

                Transform tapedFootprint = office.transform.Find("Boş Oda İşareti");
                GameObject officePadObject = GameObject.Find(office.name + " Pedi");
                Assert.That(tapedFootprint, Is.Not.Null);
                Assert.That(officePadObject, Is.Not.Null);
                PurchasePad officePad = officePadObject.GetComponent<PurchasePad>();
                Assert.That(officePad, Is.Not.Null);
                Assert.That(Vector2.Distance(
                        new Vector2(officePad.transform.position.x, officePad.transform.position.z),
                        new Vector2(tapedFootprint.position.x, tapedFootprint.position.z)),
                    Is.LessThan(0.02f),
                    $"'{officePad.name}' is not centred inside its yellow desk outline.");

                Transform doorLeaf = office.GetComponentsInChildren<Transform>(true)
                    .First(transform => transform.name == "Kanat");
                Assert.That(doorLeaf.parent.forward.x, Is.GreaterThan(0.9f),
                    "The full office was not rotated: its doorway does not face east into dining.");
                Assert.That(doorLeaf.forward.x, Is.LessThan(-0.1f),
                    "The east-wall office door opens into dining instead of westward into its room.");
            }

            Vector3[] officeDoorPositions = offices
                .Select(office => office.GetComponentsInChildren<Transform>(true)
                    .First(transform => transform.name == "Kapı").position)
                .OrderBy(position => position.z)
                .ToArray();
            Assert.That(officeDoorPositions[1].x, Is.EqualTo(officeDoorPositions[0].x).Within(0.05f),
                "The two east-wall doors are not on the same north-south line.");
            Assert.That(officeDoorPositions[1].z - officeDoorPositions[0].z,
                Is.EqualTo(6f).Within(0.05f),
                "The full two-office block was not rotated into two adjacent six-metre rooms.");

            TakeawaySystem window = Object.FindFirstObjectByType<TakeawaySystem>(
                FindObjectsInactive.Include);
            Assert.That(window, Is.Not.Null);
            Assert.That(window.gameObject.activeInHierarchy, Is.False);

            TrafficSystem traffic = Object.FindFirstObjectByType<TrafficSystem>();
            Assert.That(traffic.ServiceLaneOpen, Is.False,
                "Cars must not use the lane past the window before it is bought.");
        }

        [Test]
        public void FurnishedOffice_UsesACompactDeskInsteadOfACubicleWall()
        {
            GameObject testRoom = new("Compact Office Furniture Test");
            testRoom.transform.position = new Vector3(100f, 0f, 100f);
            try
            {
                GameObject dummyPlayer = new("Dummy Office Player");
                dummyPlayer.transform.SetParent(testRoom.transform, false);
                dummyPlayer.transform.localPosition = new Vector3(20f, 0f, 0f);

                ManagementOffice office = testRoom.AddComponent<ManagementOffice>();
                office.Configure(dummyPlayer.transform, null, ManagementMenu.HumanResources,
                    "218_office_desk", "TEST", Vector3.zero, 90f);
                office.Furnish();

                Transform desk = testRoom.transform.Find("Mobilya/Masa");
                Assert.That(desk, Is.Not.Null);
                Transform deskVisual = desk.Find("218_office_desk Visual");
                Assert.That(deskVisual, Is.Not.Null,
                    "The oversized all-in-one manager cubicle is still furnishing the office.");
                Assert.That(desk.Find("217_office_monitor Visual"), Is.Not.Null,
                    "The compact modular desk has no readable management monitor.");

                Renderer[] deskRenderers = deskVisual.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                    .ToArray();
                Assert.That(deskRenderers, Is.Not.Empty);
                Bounds bounds = deskRenderers[0].bounds;
                for (int i = 1; i < deskRenderers.Length; i++)
                    bounds.Encapsulate(deskRenderers[i].bounds);

                Assert.That(bounds.size.x, Is.LessThan(1.0f));
                Assert.That(bounds.size.z, Is.LessThan(1.75f));
                Assert.That(bounds.size.y, Is.LessThan(0.8f),
                    "The office desk has grown back into a wall-height cubicle partition.");
            }
            finally
            {
                Object.DestroyImmediate(testRoom);
            }
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

            Assert.That(gate.x, Is.EqualTo(lot.center.x).Within(0.05f),
                "The storefront entrance is not centred on the restaurant frontage.");
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
            Assert.That(ShopPrices.Belt, Has.Length.EqualTo(2),
                "The two visible belts need their own progression prices.");

            string[] beltPads = { "Et Bandı Pedi", "Ocak Bandı Pedi" };
            for (int i = 0; i < beltPads.Length; i++)
            {
                string padName = beltPads[i];
                GameObject padObject = GameObject.Find(padName);
                Assert.That(padObject, Is.Not.Null, $"'{padName}' was removed with the upgrades.");
                PurchasePad pad = padObject.GetComponent<PurchasePad>();
                Assert.That(pad, Is.Not.Null);
                Assert.That(pad.Level, Is.Zero);
                Assert.That(pad.CurrentCost, Is.EqualTo(ShopPrices.Belt[i]));
                Assert.That(pad.IsAvailable, Is.EqualTo(i == 0),
                    "A fresh shop should reveal only the first belt stage.");
            }

            Assert.That(GameObject.Find("Kesim Bandı"), Is.Null,
                "The removed cutting-to-checkout belt is still in the scene.");
            Assert.That(GameObject.Find("Kesim Bandı Pedi"), Is.Null,
                "The removed belt still has a purchase pad.");

            PurchasePad[] pads = Object.FindObjectsByType<PurchasePad>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(pads.Count(p => p.IsAvailable), Is.EqualTo(4),
                "A fresh shop should show the first belt, table and both office upgrades.");
            Assert.That(pads.Single(p => p.name == "Masa Ekle").IsAvailable, Is.True,
                "The opening table upgrade should be visible beside the first belt pad.");
            Assert.That(pads.Single(p => p.name == "İK Odası Pedi").IsAvailable, Is.True,
                "The HR office upgrade should be visible from the beginning.");
            Assert.That(pads.Single(p => p.name == "GM Odası Pedi").IsAvailable, Is.True,
                "The GM office upgrade should be visible from the beginning.");
            Assert.That(pads.Any(p => p.name == "Ocak İşçisi" || p.name == "Kesim İşçisi"),
                Is.False, "A decorative station-worker purchase pad is still in the shop.");
            Assert.That(Object.FindObjectsByType<ItemStation>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .SelectMany(s => s.GetComponentsInChildren<Transform>(true))
                .Any(t => t.name == "İşçi"), Is.False,
                "A removed station worker is still standing beside a machine.");
        }

        [Test]
        public void DiningTable_ReservesTwoIndependentSeats()
        {
            CustomerTable table = Object.FindObjectsByType<CustomerTable>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .First(candidate => candidate.HasReservableSeat);
            GameObject firstObject = new("Seat test customer A");
            GameObject secondObject = new("Seat test customer B");
            GameObject thirdObject = new("Seat test customer C");
            try
            {
                CustomerAgent first = firstObject.AddComponent<CustomerAgent>();
                CustomerAgent second = secondObject.AddComponent<CustomerAgent>();
                CustomerAgent third = thirdObject.AddComponent<CustomerAgent>();

                Assert.That(table.SeatCapacity, Is.EqualTo(2));
                Assert.That(table.TryReserve(first), Is.True);
                Assert.That(table.TryReserve(second), Is.True);
                Assert.That(table.GetSeatPoint(first), Is.Not.Null);
                Assert.That(table.GetSeatPoint(second), Is.Not.Null);
                Assert.That(table.GetSeatPoint(first), Is.Not.SameAs(table.GetSeatPoint(second)));
                Assert.That(table.OccupiedSeatCount, Is.EqualTo(2));
                Assert.That(table.TryReserve(third), Is.False);

                table.CancelReservation(first);
                Assert.That(table.OccupiedSeatCount, Is.EqualTo(1));
                Assert.That(table.TryReserve(third), Is.True,
                    "Freeing one cover should not eject or block the other diner.");
                table.CancelReservation(second);
                table.CancelReservation(third);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(thirdObject);
            }
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
                    world.EntranceAnchor.position, queueFront.transform.position, corners),
                Is.True, "The centred entrance no longer reaches the service queue.");

            foreach (CustomerTable table in tables)
            {
                corners.Clear();
                Assert.That(navigation.TryCalculatePath(
                        queueFront.transform.position, table.SeatApproachPoint, corners),
                    Is.True, $"No complete route exists from the till to '{table.name}'.");
                Assert.That(corners.Count, Is.GreaterThanOrEqualTo(2));
            }
        }

        [Test]
        public void Checkout_HasSeparateCustomerAndCashierSides_WithFridgeAtTheWallEnd()
        {
            ItemStation service = FindStation("SERVİS");
            ItemStation cutting = FindStation("KESİM");
            ItemStation fridge = FindStation("BUZDOLABI");
            GameObject queueFront = GameObject.Find("Kuyruk Başı");
            CashPile till = Object.FindObjectsByType<CashPile>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(pile => pile.transform.IsChildOf(service.transform));

            Assert.That(new Object[] { service, cutting, fridge, queueFront, till }, Has.None.Null);
            Assert.That(queueFront.transform.position.z,
                Is.LessThan(service.transform.position.z - 2.8f),
                "The customer queue is not on the front side of checkout.");
            Assert.That(till.CollectPoint.z,
                Is.GreaterThan(service.transform.position.z + 1.2f),
                "Standing in front of the register can still complete a sale.");
            Assert.That(fridge.transform.position.z,
                Is.EqualTo(cutting.transform.position.z).Within(0.01f),
                "The fridge is not against the back-wall station run.");
            Assert.That(fridge.transform.position.x,
                Is.GreaterThan(service.transform.position.x + 1.8f),
                "The fridge is not occupying the former east-end till slot.");
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
        public void PurchasePads_UseBracketedMoneyMarkersAndKeepFloatingPreviews()
        {
            PurchasePad pad = Object.FindObjectsByType<PurchasePad>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(candidate => candidate.IsAvailable);
            WorldCashMarker marker = pad.GetComponentInChildren<WorldCashMarker>(true);

            Assert.That(marker, Is.Not.Null,
                "The purchase price is not using the bracketed money-marker language.");
            Assert.That(marker.IsGroundedPurchase, Is.True);
            Assert.That(marker.AmountText, Is.EqualTo(pad.Remaining.ToString()),
                "The banknote already identifies currency; the grounded price should be digits only.");
            string expectedTitle = pad.name.EndsWith(" Pedi")
                ? pad.name.Substring(0, pad.name.Length - 5)
                : pad.name;
            if (expectedTitle == "Masa Ekle") expectedTitle = "Masa";
            Assert.That(marker.TitleText, Is.EqualTo(expectedTitle));
            Assert.That(marker.GetComponentsInChildren<Transform>(true)
                    .Count(item => item.name == "Köşe Yatay" || item.name == "Köşe Dikey"),
                Is.EqualTo(8), "The pad is missing the four white reference corners.");
            Assert.That(marker.transform.Find("Zemin Bilgisi/Nesne Adı"), Is.Not.Null);
            Assert.That(marker.transform.Find("Zemin Bilgisi/Banknot"), Is.Not.Null);
            Assert.That(marker.transform.Find("Zemin Bilgisi/Fiyat"), Is.Not.Null);
            Assert.That(marker.transform.Find("Fiş Kartı"), Is.Null,
                "The table-takings receipt card is still floating over a purchase pad.");
            Assert.That(pad.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == "Gösterge" || item.name == "Pad Surface" ||
                                 item.name == "Kart" || item.name == "Kart Gölgesi"),
                Is.False, "An opaque plate is still covering the ground-price treatment.");
            float markerTop = marker.GetComponentsInChildren<Renderer>(true)
                .Max(renderer => renderer.bounds.max.y);
            Assert.That(markerTop, Is.LessThan(pad.transform.position.y + 0.40f),
                "The purchase price is still standing in the air instead of lying by the floor.");
            Assert.That(pad.GetComponentInChildren<PurchasePreviewBob>(true), Is.Not.Null,
                "The floating 3D unlock preview was removed with the old price plate.");
        }

        [Test]
        public void TrashBin_IsVisibleAccessibleAndNotHiddenBehindTheOffices()
        {
            TrashBin bin = Object.FindFirstObjectByType<TrashBin>();
            Assert.That(bin, Is.Not.Null);
            Assert.That(bin.gameObject.activeInHierarchy, Is.True);
            Assert.That(bin.transform.position.x, Is.EqualTo(-5.9f).Within(0.02f));
            Assert.That(bin.transform.position.z, Is.EqualTo(-2.1f).Within(0.02f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(bin.transform.eulerAngles.y, 90f)),
                Is.LessThan(0.5f), "The long side of the dumpster is not parallel to the office doors.");
            Assert.That(bin.transform.Find("124_street_dumpster Visual/124_street_dumpster"),
                Is.Not.Null, "The large industrial dumpster asset was not attached.");
            Assert.That(bin.transform.Find("İç Hazne"), Is.Not.Null,
                "The closed street dumpster was not converted into an open container.");

            Vector3[] officeDoors = Object.FindObjectsByType<ManagementOffice>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(office => office.GetComponentsInChildren<Transform>(true)
                    .First(item => item.name == "Kapı").position)
                .OrderBy(position => position.z)
                .ToArray();
            Assert.That(officeDoors, Has.Length.EqualTo(2));
            Vector3 doorMidpoint = (officeDoors[0] + officeDoors[1]) * 0.5f;
            Assert.That(bin.transform.position.z, Is.EqualTo(doorMidpoint.z).Within(0.02f),
                "The dumpster is not centred between the two office doors.");
            Assert.That(bin.transform.position.x - doorMidpoint.x, Is.EqualTo(1f).Within(0.02f),
                "The dumpster is not standing at the safe east-wall offset.");

            Renderer[] visible = bin.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(visible, Is.Not.Empty, "The trash bin has no visible authored renderer.");
            Bounds bounds = visible[0].bounds;
            for (int i = 1; i < visible.Length; i++) bounds.Encapsulate(visible[i].bounds);
            Assert.That(bounds.size.x, Is.InRange(1.15f, 1.35f));
            Assert.That(bounds.size.z, Is.InRange(1.90f, 2.10f));
            Assert.That(bounds.size.y, Is.GreaterThan(1.15f),
                "The replacement trash container is still the small pedal-bin scale.");

            const float DoorHalfWidth = 0.85f;
            foreach (Vector3 door in officeDoors)
            {
                float clearance = Mathf.Abs(door.z - bounds.center.z) -
                                  bounds.extents.z - DoorHalfWidth;
                Assert.That(clearance, Is.GreaterThanOrEqualTo(1.08f),
                    "The larger dumpster intrudes into an office doorway approach.");
            }

            Renderer cavity = bin.transform.Find("İç Hazne").GetComponent<Renderer>();
            float rimTop = bin.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.name.StartsWith("Üst Biyet"))
                .Max(renderer => renderer.bounds.max.y);
            Assert.That(rimTop - cavity.bounds.max.y, Is.GreaterThan(0.15f),
                "The dumpster mouth reads as a flat closed lid instead of an open cavity.");

            Transform collisionObject = bin.transform.Find("Konteyner Çarpışma");
            Assert.That(collisionObject, Is.Not.Null);
            BoxCollider solid = collisionObject.GetComponent<BoxCollider>();
            Assert.That(solid, Is.Not.Null);
            Assert.That(solid.enabled, Is.True);
            Assert.That(collisionObject.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(solid.size.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(collisionObject.localScale.x, Is.EqualTo(2.02f).Within(0.01f));
            Assert.That(collisionObject.localScale.z, Is.EqualTo(1.27f).Within(0.01f));

            Transform workerApproach = bin.transform.Find("Çalışan Yaklaşma Noktası");
            Assert.That(workerApproach, Is.Not.Null);
            Assert.That(workerApproach.position.x, Is.GreaterThan(bounds.max.x + 0.5f),
                "The busser still targets the solid middle of the large dumpster.");

            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            MobileCameraRig rig = camera.GetComponent<MobileCameraRig>();
            Assert.That(rig, Is.Not.Null);
            Vector3 originalPlayerPosition = rig.FollowTarget.position;
            try
            {
                Bounds floor = world.BaseModule.WalkableBounds;
                rig.FollowTarget.position = new Vector3(
                    floor.center.x, originalPlayerPosition.y, floor.center.z);
                rig.SendMessage("LateUpdate", SendMessageOptions.RequireReceiver);

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = new(
                        (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                    Vector3 viewport = camera.WorldToViewportPoint(point);
                    Assert.That(viewport.z, Is.GreaterThan(0f));
                    Assert.That(viewport.x, Is.InRange(0.05f, 0.95f));
                    Assert.That(viewport.y, Is.InRange(0.05f, 0.95f));
                }

                Physics.SyncTransforms();
                RaycastHit firstSolid = Physics.RaycastAll(
                        camera.transform.position,
                        bounds.center - camera.transform.position,
                        Vector3.Distance(camera.transform.position, bounds.center))
                    .Where(hit => hit.collider != null && hit.collider.enabled && !hit.collider.isTrigger)
                    .OrderBy(hit => hit.distance)
                    .FirstOrDefault();
                Assert.That(firstSolid.collider, Is.Not.Null);
                Assert.That(firstSolid.collider.transform.IsChildOf(bin.transform), Is.True,
                    $"'{firstSolid.collider.name}' still blocks the camera's view of the trash bin.");
            }
            finally
            {
                rig.FollowTarget.position = originalPlayerPosition;
                rig.SendMessage("LateUpdate", SendMessageOptions.RequireReceiver);
            }

            PlaceableObject placeable = bin.GetComponent<PlaceableObject>();
            BuildModeController buildMode = Object.FindFirstObjectByType<BuildModeController>();
            Assert.That(placeable, Is.Not.Null);
            Assert.That(placeable.StableId, Is.EqualTo("utility.trash_dumpster"),
                "The old saved pedal-bin placement can still override the new fixed default.");
            Assert.That(buildMode, Is.Not.Null);
            placeable.EnsureInitialized();
            Assert.That(placeable.FootprintBounds.size.x, Is.GreaterThan(1.15f));
            Assert.That(placeable.FootprintBounds.size.z, Is.GreaterThan(1.90f));
            Assert.That(buildMode.CanPlace(placeable), Is.True,
                "The restored trash-bin position overlaps another gameplay object.");
        }

        [Test]
        public void OrderBubbles_FaceCameraAndUseLargePlainCounts()
        {
            GameObject customer = new("Sipariş Rozeti Test Müşterisi");
            try
            {
                OrderBubble bubble = OrderBubble.Create(customer.transform, 2.2f);
                CustomerOrder order = new();
                order.Add(ItemType.Wrap, 2);
                order.Add(ItemType.Drink, 1);
                bubble.Show(order);

                TextMesh[] counts = bubble.GetComponentsInChildren<TextMesh>(true)
                    .Where(label => label.name == "Adet")
                    .ToArray();
                Assert.That(counts.Select(label => label.text),
                    Is.EquivalentTo(new[] { "2", "1" }),
                    "Order quantities should be plain numbers, not small x-count strings.");
                Assert.That(counts.All(label => label.font == UI.UITheme.DisplayFont), Is.True);
                Assert.That(counts.All(label => label.characterSize >= 0.07f), Is.True,
                    "The order count is still too small to read at gameplay distance.");
                Assert.That(bubble.GetComponentsInChildren<Transform>(true)
                    .Count(item => item.name == "İkon Halkası"), Is.EqualTo(2));
                Assert.That(bubble.GetComponentsInChildren<Collider>(true)
                    .All(collider => !collider.enabled), Is.True);

                customer.transform.rotation = Quaternion.Euler(0f, 137f, 0f);
                bubble.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
                Assert.That(Quaternion.Angle(
                        bubble.transform.rotation, Quaternion.Euler(55f, 0f, 0f)),
                    Is.LessThan(1f),
                    "A turning customer rotates the order badge away from the camera.");
            }
            finally
            {
                Object.DestroyImmediate(customer);
            }
        }

        [Test]
        public void Stations_DrawOnlyUsefulTrays_AndDrinkLineFitsTheFloor()
        {
            Assert.That(Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(item => item.name == "255_food_barrel Visual"), Is.False,
                "The decorative barrel behind the service counter has returned.");

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
            Assert.That(fridge.transform.position.x, Is.GreaterThan(service.transform.position.x + 1.8f),
                "The fridge is not finishing the east end of the counter run.");
            Assert.That(fridge.transform.position.z, Is.GreaterThan(service.transform.position.z + 1.2f),
                "The fridge has drifted from the back wall into the customer aisle.");
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
