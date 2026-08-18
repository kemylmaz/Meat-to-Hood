#if UNITY_EDITOR
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
            Assert.That(oven.WorkerAssigned, Is.False, "No worker is hired at the start.");

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
