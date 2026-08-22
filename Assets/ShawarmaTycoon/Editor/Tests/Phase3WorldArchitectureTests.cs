#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ShawarmaTycoon.Tests
{
    public sealed class Phase3WorldArchitectureTests
    {
        private static readonly string[] RequiredAuthoredArtIds =
        {
            "06_rotisserie_station",
            "50_customer_vest_green",
            "51_customer_vest_navy",
            "52_customer_sweater",
            "53_worker_teal",
            "54_worker_red",
            "55_worker_red_backcap",
            "62_cashier_counter",
            "63_conveyor_straight",
            "65_manager_desk_stamp",
            "66_manager_desk_pencils",
            "67_manager_desk_plant",
            "70_dining_table",
            "71_dining_table_dirty",
            "72_trash_bin",
            "73_planter",
            "76_wall_corner",
            "77_wall_straight",
            "78_floor_tiled",
            "79_floor_plot",
            "80_entrance",
            "81_lock_pad",
            "82_money_pad",
            "83_upgrade_pad"
        };

        /// <summary>
        /// The shop stands on a street, so the block and its traffic are part of
        /// the world rather than optional decor. They were both off while the shop
        /// was a floating island, which is what left it with no outside at all.
        /// </summary>
        [Test]
        public void ShippingConfig_EnablesCityAndTraffic()
        {
            GameConfig shipping = Resources.Load<GameConfig>("Config/GameConfig");
            Assert.That(shipping, Is.Not.Null, "The shipping GameConfig resource is missing.");
            Assert.That(shipping.Features.CityDecor, Is.True);
            Assert.That(shipping.Features.Traffic, Is.True);

            GameConfig defaults = GameConfig.CreateRuntimeDefaults();
            try
            {
                Assert.That(defaults.Features.CityDecor, Is.True);
                Assert.That(defaults.Features.Traffic, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(defaults);
            }
        }

        /// <summary>
        /// The kitchen line has to fit the lot with the belts between the counters
        /// and the counters clear of the back wall. Laid out by hand in the config
        /// asset, this is the check that stops a nudge putting a station through a
        /// wall or two counters on top of each other.
        /// </summary>
        [Test]
        public void KitchenLine_FitsInsideTheLotWithRoomForTheBelts()
        {
            RestaurantLayoutConfig layout =
                Resources.Load<RestaurantLayoutConfig>("Config/RestaurantLayoutConfig");
            DioramaWorldConfig world = Resources.Load<DioramaWorldConfig>("Config/DioramaWorldConfig");
            Assert.That(layout, Is.Not.Null);
            Assert.That(world, Is.Not.Null);

            Vector3[] line = { layout.MeatSource, layout.Oven, layout.Cutting, layout.Service };
            float halfX = world.CoreSize.x * 0.5f;
            float halfZ = world.CoreSize.y * 0.5f;

            for (int i = 0; i < line.Length; i++)
            {
                Assert.That(Mathf.Abs(line[i].x), Is.LessThan(halfX - 1.4f),
                    "A kitchen station overhangs the lot's east or west edge.");
                Assert.That(line[i].z, Is.LessThan(halfZ - 1.2f),
                    "A kitchen station is inside the back wall.");
                Assert.That(line[i].z, Is.EqualTo(line[0].z).Within(0.001f),
                    "The kitchen stations are not on one line.");

                // 2.05 m of authored belt, plus half a counter at each end.
                if (i > 0)
                    Assert.That(line[i].x - line[i - 1].x, Is.GreaterThan(3.4f),
                        $"No room for the belt into station {i}.");
            }
        }

        /// <summary>
        /// Income measured with the editor's economy probe, driving the carry loop
        /// on a fresh save. Coins per minute.
        /// </summary>
        private const float HandCarriedIncome = 50f;
        private const float BeltedIncome = 158f;

        /// <summary>
        /// Re-measured on the widened lot: 999 coins over 188 s with all six plots
        /// bought. Doubling the floor moved this by eight coins a minute, because
        /// what caps the shop is how fast the kitchen line turns meat into wraps,
        /// not how many people can sit down. Worth knowing before pricing seating
        /// as though it were the constraint.
        /// </summary>
        private const float FullyBuiltIncome = 318f;

        /// <summary>
        /// The price ladder has to stay in step with what the shop earns. These
        /// bounds are loose - balance is a judgement, not an equation - but they
        /// catch the two ways it went wrong: a first purchase the player cannot
        /// reach, and a total that turns the game into a four hour grind.
        ///
        /// The first pass cost 49,000 against these same income figures, which is
        /// over four hours, and four fifths of it was the two upgrade boards.
        /// </summary>
        [Test]
        public void Prices_StayInStepWithMeasuredIncome()
        {
            float secondsToFirstBuy = ShopPrices.Belt[0] / HandCarriedIncome * 60f;
            Assert.That(secondsToFirstBuy, Is.LessThanOrEqualTo(120f),
                "The first thing to buy is out of reach for the opening two minutes.");
            Assert.That(secondsToFirstBuy, Is.GreaterThan(60f),
                "The first purchase is free money; there is no opening to play.");

            float minutesToFirstTable = ShopPrices.Table[0] / HandCarriedIncome;
            Assert.That(minutesToFirstTable, Is.InRange(3f, 6f),
                "The first extra table should be a deliberate early-game purchase.");

            float firstPlotPayback = ShopPrices.Table[4] / BeltedIncome;
            float lastPlotPayback = ShopPrices.Table[^1] / FullyBuiltIncome;
            Assert.That(firstPlotPayback, Is.InRange(5f, 12f),
                "The first two-table expansion is not priced like an expansion.");
            Assert.That(lastPlotPayback, Is.InRange(8f, 15f),
                "The final two-table expansion is too cheap or becomes a grind.");

            // Roughly: the content half is bought while hand-carrying and belted,
            // the boards once the shop is running properly.
            float minutes = ShopPrices.ContentTotal / BeltedIncome +
                            ShopPrices.BoardTotal / FullyBuiltIncome;
            Assert.That(minutes, Is.InRange(150f, 270f),
                $"Buying everything takes {minutes:0} minutes of measured income.");

            // Content is what the player sees appear. The boards sell multipliers
            // on a shop they have already built, so they must not dwarf it.
            Assert.That(ShopPrices.BoardTotal, Is.LessThan(ShopPrices.ContentTotal),
                "Invisible multipliers cost more than the visible restaurant.");
        }

        /// <summary>
        /// Every ladder has to climb. A flat or falling step means a later level
        /// is cheaper than the one before it, which reads as a bug to the player.
        /// </summary>
        [Test]
        public void Prices_ClimbWithEveryLevel()
        {
            AssertClimbs(ShopPrices.Belt, nameof(ShopPrices.Belt));
            AssertClimbs(ShopPrices.Table, nameof(ShopPrices.Table));
            AssertClimbs(ShopPrices.Decoration, nameof(ShopPrices.Decoration));

            foreach (int baseCost in new[]
                     {
                         ShopPrices.StaffSpeed, ShopPrices.StaffCapacity, ShopPrices.StaffAutomation,
                         ShopPrices.PlayerSpeed, ShopPrices.PlayerCapacity, ShopPrices.PlayerIncome
                     })
            {
                for (int level = 1; level < ShopPrices.BoardLevels; level++)
                    Assert.That(ShopPrices.BoardCost(baseCost, level),
                        Is.GreaterThan(ShopPrices.BoardCost(baseCost, level - 1)),
                        $"Board line from {baseCost} does not climb at level {level}.");
            }
        }

        private static void AssertClimbs(int[] ladder, string name)
        {
            Assert.That(ladder, Is.Not.Empty, $"'{name}' has no levels.");
            for (int i = 1; i < ladder.Length; i++)
                Assert.That(ladder[i], Is.GreaterThan(ladder[i - 1]),
                    $"'{name}' step {i} is not dearer than the one before it.");
        }

        /// <summary>
        /// An order is only worth more than a wrap if the extras are priced above
        /// zero, and trimming has to leave something to serve - an order stripped
        /// to nothing would take a customer off the queue for free.
        /// </summary>
        [Test]
        public void CustomerOrder_KeepsItsWrapWhenTheExtrasAreOutOfStock()
        {
            CustomerOrder order = new();
            order.Add(ItemType.Wrap, 1);
            order.Add(ItemType.Drink, 1);
            order.Add(ItemType.Dessert, 1);
            Assert.That(order.LineCount, Is.EqualTo(3));
            Assert.That(order.TotalItems, Is.EqualTo(3));

            float full = order.ValueMultiplier;
            Assert.That(full, Is.GreaterThan(1f), "A bag has to be worth more than a wrap.");

            // Nothing but wraps in stock: the extras go, the wrap stays.
            Assert.That(order.TrimUnavailableExtras(type => type == ItemType.Wrap), Is.True);
            Assert.That(order.CountOf(ItemType.Wrap), Is.EqualTo(1));
            Assert.That(order.CountOf(ItemType.Drink), Is.Zero);
            Assert.That(order.CountOf(ItemType.Dessert), Is.Zero);
            Assert.That(order.ValueMultiplier, Is.LessThan(full),
                "Serving less than was ordered has to be worth less.");

            // Nothing left to give up, so a second pass reports no change.
            Assert.That(order.TrimUnavailableExtras(type => type == ItemType.Wrap), Is.False);
        }

        /// <summary>
        /// The shop progress bar is only honest if the roster it adds up behaves:
        /// a track counted twice or a level read above its own maximum would let
        /// the bar sit past 100%, and one that never reaches its maximum would
        /// strand it below.
        /// </summary>
        [Test]
        public void UpgradeProgress_CountsEachTrackOnceAndClampsToItsOwnMaximum()
        {
            UpgradeProgress.Reset();
            try
            {
                int beltLevel = 0;
                UpgradeProgress.Register("belt", 3, () => beltLevel);
                UpgradeProgress.Register("hire", 1, () => 1);
                Assert.That(UpgradeProgress.TotalSteps, Is.EqualTo(4));
                Assert.That(UpgradeProgress.OwnedSteps, Is.EqualTo(1));

                beltLevel = 3;
                Assert.That(UpgradeProgress.Ratio, Is.EqualTo(1f).Within(0.0001f),
                    "Everything bought has to read as finished.");

                // Rebuilding the shop re-registers the same ids; they must replace
                // rather than pile up, or the denominator grows every rebuild.
                UpgradeProgress.Register("belt", 3, () => 3);
                Assert.That(UpgradeProgress.TotalSteps, Is.EqualTo(4));

                UpgradeProgress.Register("overreported", 2, () => 99);
                Assert.That(UpgradeProgress.OwnedSteps, Is.EqualTo(6));
                Assert.That(UpgradeProgress.Ratio, Is.EqualTo(1f).Within(0.0001f));
            }
            finally
            {
                UpgradeProgress.Reset();
            }

            Assert.That(UpgradeProgress.TotalSteps, Is.Zero);
            Assert.That(UpgradeProgress.Ratio, Is.Zero);
        }

        [TestCase(0f, 1)]
        [TestCase(0.149f, 1)]
        [TestCase(0.15f, 2)]
        [TestCase(0.379f, 2)]
        [TestCase(0.38f, 3)]
        [TestCase(0.649f, 3)]
        [TestCase(0.65f, 4)]
        [TestCase(0.899f, 4)]
        [TestCase(0.90f, 5)]
        [TestCase(1f, 5)]
        public void RestaurantMakeover_UsesFiveStableProgressTiers(float ratio, int expectedTier)
        {
            Assert.That(RestaurantMakeoverSystem.GetTierForRatio(ratio), Is.EqualTo(expectedTier));
            Assert.That(RestaurantMakeoverSystem.TierName(expectedTier), Is.Not.Empty);
            Assert.That(RestaurantMakeoverSystem.TierShortName(expectedTier), Is.Not.Empty);
        }

        [Test]
        public void WorldLabels_UseTheCozyDisplayFontAndCompactBadgeGeometry()
        {
            GameObject parent = new("World Label Test");
            try
            {
                TextMesh label = PrototypeVisuals.CreateLabel("MAX", parent.transform, Vector3.zero, 0.14f);
                Assert.That(label.font, Is.EqualTo(ShawarmaTycoon.UI.UITheme.DisplayFont));
                Assert.That(label.characterSize, Is.LessThan(0.06f),
                    "Legacy world-label size would spill across the floor.");

                TextMesh badge = PrototypeVisuals.CreateCozyBadge(
                    "DOLU", parent.transform, Vector3.up, 1.02f);
                Assert.That(badge.font, Is.EqualTo(ShawarmaTycoon.UI.UITheme.DisplayFont));
                Assert.That(badge.transform.childCount, Is.EqualTo(6),
                    "The cozy chip needs a paper silhouette and its offset shadow.");
                foreach (Collider collider in badge.GetComponentsInChildren<Collider>(true))
                    Assert.That(collider.enabled, Is.False,
                        "A status badge must never affect navigation or interaction.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        /// <summary>
        /// Pads drain coins while the player stands on one, so two within reach of
        /// a single spot take payment for two different things at once.
        /// </summary>
        [Test]
        public void PurchasePads_StandFarEnoughApartToBePaidOneAtATime()
        {
            RestaurantLayoutConfig layout =
                Resources.Load<RestaurantLayoutConfig>("Config/RestaurantLayoutConfig");
            Assert.That(layout, Is.Not.Null);

            Vector3[] pads =
            {
                layout.MeatBeltPad, layout.OvenBeltPad, layout.CuttingBeltPad,
                layout.TablePad, layout.DecorationPad, layout.DriveThruUnlockPad,
                layout.FridgePad, layout.DessertPad, layout.CourierPad
            };

            for (int i = 0; i < pads.Length; i++)
            for (int j = i + 1; j < pads.Length; j++)
            {
                float gap = Vector2.Distance(
                    new Vector2(pads[i].x, pads[i].z), new Vector2(pads[j].x, pads[j].z));
                Assert.That(gap, Is.GreaterThan(2.1f),
                    $"Pads {i} and {j} overlap: standing between them pays into both.");
            }

            // The player must not open the game already standing on one. Spawned
            // inside the table pad's reach, the shop bought itself tables from the
            // first second without anyone touching the controls.
            DioramaWorldConfig world = Resources.Load<DioramaWorldConfig>("Config/DioramaWorldConfig");
            Assert.That(world, Is.Not.Null);
            Vector2 spawn = new(world.PlayerSpawn.x, world.PlayerSpawn.z);
            for (int i = 0; i < pads.Length; i++)
            {
                float gap = Vector2.Distance(spawn, new Vector2(pads[i].x, pads[i].z));
                Assert.That(gap, Is.GreaterThan(1.2f),
                    $"The player spawns on pad {i} and starts buying it immediately.");
            }
        }

        [Test]
        public void DioramaConfig_HasUniqueIdsAndContiguousEastExpansions()
        {
            DioramaWorldConfig config = Resources.Load<DioramaWorldConfig>("Config/DioramaWorldConfig");
            Assert.That(config, Is.Not.Null, "The Phase 3 world config resource is missing.");
            Assert.That(config.CoreId, Is.Not.Empty);
            Assert.That(config.Expansions.Count, Is.GreaterThan(0));

            HashSet<string> ids = new(StringComparer.Ordinal) { config.CoreId };
            float coreEastEdge = config.CorePosition.x + config.CoreSize.x * 0.5f;
            float coreSouthEdge = config.CorePosition.z - config.CoreSize.y * 0.5f;
            float coreNorthEdge = config.CorePosition.z + config.CoreSize.y * 0.5f;

            foreach (DioramaWorldConfig.ExpansionDefinition expansion in config.Expansions)
            {
                Assert.That(expansion.Id, Is.Not.Empty);
                Assert.That(ids.Add(expansion.Id), Is.True, $"Duplicate module id '{expansion.Id}'.");
                Assert.That(expansion.Size.x, Is.GreaterThan(0f));
                Assert.That(expansion.Size.y, Is.GreaterThan(0f));

                float southEdge = expansion.Position.z - expansion.Size.y * 0.5f;
                float northEdge = expansion.Position.z + expansion.Size.y * 0.5f;
                Assert.That(southEdge, Is.GreaterThanOrEqualTo(coreSouthEdge - 0.001f),
                    $"Module '{expansion.Id}' hangs off the south end of the lot.");
                Assert.That(northEdge, Is.LessThanOrEqualTo(coreNorthEdge + 0.001f),
                    $"Module '{expansion.Id}' hangs off the north end of the lot.");
            }

            // The plots are a grid bolted onto the core's east face, so every
            // column and row has to line up on one pitch and every cell has to be
            // filled exactly once. Anything else leaves a gap in the floor or two
            // plots the player can buy into the same patch of ground.
            List<float> columns = config.Expansions
                .Select(definition => definition.Position.x).Distinct().OrderBy(x => x).ToList();
            List<float> rows = config.Expansions
                .Select(definition => definition.Position.z).Distinct().OrderBy(z => z).ToList();
            Assert.That(config.Expansions.Count, Is.EqualTo(columns.Count * rows.Count),
                "The east plots do not fill a rectangle: a cell is missing or doubled.");

            Vector2 plot = config.Expansions[0].Size;
            Assert.That(columns[0] - plot.x * 0.5f, Is.EqualTo(coreEastEdge).Within(0.001f),
                "The first column of plots does not meet the core island.");
            for (int i = 1; i < columns.Count; i++)
                Assert.That(columns[i] - columns[i - 1], Is.EqualTo(plot.x).Within(0.001f),
                    "Gap or overlap between plot columns.");
            for (int i = 1; i < rows.Count; i++)
                Assert.That(rows[i] - rows[i - 1], Is.EqualTo(plot.y).Within(0.001f),
                    "Gap or overlap between plot rows.");

            foreach (DioramaWorldConfig.ExpansionDefinition expansion in config.Expansions)
                Assert.That(expansion.Size, Is.EqualTo(plot),
                    $"Module '{expansion.Id}' is not on the same grid pitch as the rest.");
        }

        [TestCaseSource(nameof(RequiredAuthoredArtIds))]
        public void RequiredArt_ResolvesToAuthoredForwardFacingLodPrefab(string artId)
        {
            ArtCatalog catalog = Resources.Load<ArtCatalog>("Config/ArtCatalog");
            Assert.That(catalog, Is.Not.Null, "The ArtCatalog resource is missing.");
            Assert.That(catalog.TryGetPrefab(artId, out GameObject prefab), Is.True,
                $"ArtCatalog cannot resolve '{artId}'.");
            Assert.That(prefab, Is.Not.Null);

            string path = AssetDatabase.GetAssetPath(prefab).Replace('\\', '/');
            Assert.That(path, Does.Contain("/Resources/Phase1Prefabs/"),
                $"'{artId}' resolved outside the approved authored prefab pack.");
            Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(prefab.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(Vector3.Angle(prefab.transform.forward, Vector3.forward), Is.LessThan(0.01f),
                $"'{artId}' does not use Unity +Z as its prefab forward direction.");

            CozyVisualMetadata metadata = prefab.GetComponent<CozyVisualMetadata>();
            Assert.That(metadata, Is.Not.Null, $"'{artId}' has no CozyVisualMetadata.");
            Assert.That(metadata.SourceAssetId, Is.EqualTo(artId));
            Assert.That(metadata.RuntimeEulerOffset, Is.EqualTo(Vector3.zero),
                $"'{artId}' still relies on a runtime rotation correction.");

            LODGroup lod = prefab.GetComponent<LODGroup>();
            Assert.That(lod, Is.Not.Null, $"'{artId}' has no root LODGroup.");
            Assert.That(lod.GetLODs(), Has.Length.EqualTo(3), $"'{artId}' must ship with three LOD levels.");
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty,
                $"'{artId}' is visual-only and must not introduce gameplay colliders.");
        }
    }
}
#endif
