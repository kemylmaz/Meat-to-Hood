using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShawarmaTycoon.EditorTools
{
    /// <summary>
    /// Imports the downloaded Poly Pizza bundles - the city kit, the restaurant
    /// and kitchen sets, the food kit and the animated crowd.
    ///
    /// It cannot reuse <see cref="CozyPackBuilder"/>, which is built around the
    /// project's own Blender exports: those carry three named LOD meshes, a
    /// FRONT_DIRECTION anchor and material names that map onto the hand-picked
    /// palette. None of that is true here. These models arrive as one mesh with
    /// either an embedded atlas (city, restaurant, kitchen), plainly named
    /// material slots (the crowd, the interior props) or an OBJ whose MTL carries
    /// flat diffuse colours (the food kit), so the front direction and the metre
    /// scale are declared per asset in <see cref="Specs"/> instead of read off the
    /// mesh.
    /// </summary>
    public static class PolyPackBuilder
    {
        public const string SourceRoot = "Assets/ShawarmaTycoon/Art/PolyPack";
        public const string PrefabFolder = "Assets/ShawarmaTycoon/Resources/PolyPrefabs";
        public const string AnimationFolder = "Assets/ShawarmaTycoon/Art/PolyPack/Animation";

        /// <summary>
        /// Metre pitch of the shell kit's wall panels, taken from the code that
        /// tiles them so the models and the runs cannot drift apart, and the span
        /// of the corner piece at that same scale.
        /// </summary>
        private const float WallModule = ShopWorldBuilder.ShellModule;

        private const float WallCorner = WallModule * 2.25f / 2f;

        private enum Profile
        {
            /// <summary>Rigged crowd: needs an avatar and a locomotion controller.</summary>
            Character,

            /// <summary>Anything the player walks up to inside the shop.</summary>
            Prop,

            /// <summary>Street dressing, read at distance and never approached.</summary>
            Environment,

            /// <summary>Food and small items, drawn a few centimetres across.</summary>
            Handheld
        }

        /// <summary>
        /// Which measurement of a model the declared metre size refers to. The four
        /// bundles were authored at four unrelated scales - the city kit lands in
        /// single metres, the kitchen set a hundred times smaller, the food kit
        /// already life-sized - so every asset states one real dimension and the
        /// builder solves for the uniform scale from what the mesh measures.
        /// </summary>
        private enum Fit
        {
            /// <summary>Take the bundle's own scale.</summary>
            None,
            Height,
            Width,
            Depth,

            /// <summary>The model's largest dimension, whichever axis it lands on.</summary>
            Longest
        }

        private readonly struct Spec
        {
            public readonly string Category;
            public readonly string File;
            public readonly string Id;
            public readonly Profile Profile;
            public readonly Fit Fit;

            /// <summary>The real-world metres that <see cref="Fit"/> refers to.</summary>
            public readonly float Size;

            /// <summary>
            /// Turn that puts the model's front on +Z, which is what the rest of
            /// the project treats as forward. Stored in the prefab's
            /// <see cref="CozyVisualMetadata"/> so callers place these the same way
            /// they place the authored pack, rather than baked in.
            /// </summary>
            public readonly float Yaw;

            /// <summary>
            /// Standing correction baked into the model, on top of whatever
            /// orientation the importer worked out. Only needed where a bundle
            /// exports an up-axis Unity cannot read off the file.
            /// </summary>
            public readonly Vector3 ModelEuler;

            public Spec(
                string category, string file, string id, Profile profile,
                Fit fit = Fit.None, float size = 0f, float yaw = 0f, Vector3 modelEuler = default)
            {
                Category = category;
                File = file;
                Id = id;
                Profile = profile;
                Fit = fit;
                Size = size;
                Yaw = yaw;
                ModelEuler = modelEuler;
            }

            public string FolderPath => $"{SourceRoot}/{Category}";
            public string PrefabPath => $"{PrefabFolder}/{Id}.prefab";
        }

        private static readonly Spec[] Specs =
        {
            // --- city: background facades -------------------------------------
            // Heights are picked to sit in the same band as the authored facades
            // they stand beside, which run 11 m and 17.5 m tall on an 8.4 m front.
            new("City", "building_A", "100_city_building_a", Profile.Environment, Fit.Height, 9.5f),
            new("City", "building_B", "101_city_building_b", Profile.Environment, Fit.Height, 10.5f),
            new("City", "building_C", "102_city_building_c", Profile.Environment, Fit.Height, 14f),
            new("City", "building_D", "103_city_building_d", Profile.Environment, Fit.Height, 15f),
            new("City", "building_E", "104_city_building_e", Profile.Environment, Fit.Height, 12f),
            new("City", "building_F", "105_city_building_f", Profile.Environment, Fit.Height, 12.5f),
            new("City", "building_G", "106_city_building_g", Profile.Environment, Fit.Height, 15.5f),
            new("City", "building_H", "107_city_building_h", Profile.Environment, Fit.Height, 16.5f),

            // --- city: traffic ------------------------------------------------
            // Matched to the authored 46_city_car, which is 4.3 m long.
            new("City", "Car_Hatchback", "110_car_hatchback", Profile.Prop, Fit.Longest, 4.2f),
            new("City", "Stationwagon", "111_car_stationwagon", Profile.Prop, Fit.Longest, 4.7f),
            new("City", "Taxi", "112_car_taxi", Profile.Prop, Fit.Longest, 4.4f),
            new("City", "Police_car", "113_car_police", Profile.Prop, Fit.Longest, 4.5f),

            // --- city: street dressing ----------------------------------------
            new("City", "bench", "120_street_bench", Profile.Environment, Fit.Longest, 1.8f),
            new("City", "box_A", "121_street_box_a", Profile.Environment, Fit.Longest, 0.65f),
            new("City", "box_B", "122_street_box_b", Profile.Environment, Fit.Longest, 0.5f),
            new("City", "bush", "123_street_bush", Profile.Environment, Fit.Height, 1.3f),
            new("City", "dumpster", "124_street_dumpster", Profile.Environment, Fit.Longest, 2f),
            new("City", "firehydrant", "125_street_hydrant", Profile.Environment, Fit.Height, 0.9f),
            new("City", "streetlight", "126_street_lamp", Profile.Environment, Fit.Height, 4.8f),
            new("City", "trafficlight_A", "127_traffic_light_a", Profile.Environment, Fit.Height, 3.4f),
            new("City", "trafficlight_B", "128_traffic_light_b", Profile.Environment, Fit.Height, 4.2f),
            new("City", "trafficlight_C", "129_traffic_light_c", Profile.Environment, Fit.Height, 4.4f),
            new("City", "watertower", "130_water_tower", Profile.Environment, Fit.Height, 7.5f),
            new("City", "trash_B", "131_street_litter", Profile.Environment, Fit.Longest, 0.7f),

            // --- the crowd ----------------------------------------------------
            // 1.70 m, so the new bodies stand alongside the authored staff without
            // a head-height step between the two sets.
            new("People", "Male_Casual", "140_customer_male_casual", Profile.Character, Fit.Height, 1.74f),
            new("People", "Male_Shirt", "141_customer_male_shirt", Profile.Character, Fit.Height, 1.78f),
            new("People", "Male_LongSleeve", "142_customer_male_longsleeve", Profile.Character, Fit.Height, 1.72f),
            new("People", "Male_Suit", "143_customer_male_suit", Profile.Character, Fit.Height, 1.8f),
            new("People", "Female_Alternative", "144_customer_female_alt", Profile.Character, Fit.Height, 1.66f),
            new("People", "Female_Casual", "145_customer_female_casual", Profile.Character, Fit.Height, 1.68f),
            new("People", "Female_Dress", "146_customer_female_dress", Profile.Character, Fit.Height, 1.7f),
            new("People", "Female_TankTop", "147_customer_female_tanktop", Profile.Character, Fit.Height, 1.64f),

            // --- restaurant fittings ------------------------------------------
            new("Restaurant", "Fridge", "150_shop_fridge", Profile.Prop, Fit.Height, 1.95f),
            new("Restaurant", "oven", "151_shop_oven", Profile.Prop, Fit.Height, 1.5f),
            new("Restaurant", "stove_single_countertop", "152_shop_stove", Profile.Prop, Fit.Width, 0.8f),
            new("Restaurant", "extractorhood", "153_shop_extractor_hood", Profile.Prop, Fit.Width, 1.6f),
            new("Restaurant", "menu", "154_shop_menu", Profile.Prop, Fit.Height, 1.1f),
            new("Restaurant", "chair_A", "155_shop_chair_a", Profile.Prop, Fit.Height, 0.95f),
            new("Restaurant", "chair_B", "156_shop_chair_b", Profile.Prop, Fit.Height, 0.95f),
            new("Restaurant", "chair_stool", "157_shop_stool", Profile.Prop, Fit.Height, 0.6f),
            new("Restaurant", "table_round_A", "158_shop_table_round", Profile.Prop, Fit.Height, 0.78f),
            new("Restaurant", "table_round_A_small", "159_shop_table_round_small", Profile.Prop, Fit.Height, 0.76f),
            new("Restaurant", "table_round_A_decorated", "160_shop_table_served", Profile.Prop, Fit.Width, 1.15f),
            new("Restaurant", "kitchentable_A", "161_shop_kitchen_table", Profile.Prop, Fit.Height, 0.92f),
            new("Restaurant", "kitchentable_A_large", "162_shop_kitchen_table_large", Profile.Prop, Fit.Height, 0.92f),
            new("Restaurant", "kitchentable_sink", "163_shop_kitchen_sink", Profile.Prop, Fit.Width, 1.1f),
            new("Restaurant", "kitchencabinet", "164_shop_cabinet", Profile.Prop, Fit.Width, 1.1f),
            new("Restaurant", "kitchencabinet_half", "165_shop_cabinet_half", Profile.Prop, Fit.Width, 1.1f),
            new("Restaurant", "crate", "166_shop_crate", Profile.Prop, Fit.Width, 0.75f),
            new("Restaurant", "crate_buns", "167_shop_crate_buns", Profile.Prop, Fit.Width, 0.75f),
            new("Restaurant", "crate_ham", "168_shop_crate_ham", Profile.Prop, Fit.Width, 0.75f),
            new("Restaurant", "crate_lid", "169_shop_crate_lid", Profile.Prop, Fit.Width, 0.75f),
            new("Restaurant", "plate", "170_shop_plate", Profile.Handheld, Fit.Width, 0.26f),
            new("Restaurant", "plate_dirty", "171_shop_plate_dirty", Profile.Handheld, Fit.Width, 0.26f),
            new("Restaurant", "dishrack", "172_shop_dishrack", Profile.Prop, Fit.Width, 0.45f),
            new("Restaurant", "dishrack_plates", "173_shop_dishrack_plates", Profile.Prop, Fit.Width, 0.45f),
            new("Restaurant", "ketchup", "175_shop_ketchup", Profile.Handheld, Fit.Height, 0.24f),
            new("Restaurant", "mustard", "176_shop_mustard", Profile.Handheld, Fit.Height, 0.24f),
            new("Restaurant", "papertowel", "177_shop_papertowel", Profile.Handheld, Fit.Height, 0.28f),
            new("Restaurant", "shelf_papertowel", "178_shop_shelf_papertowel", Profile.Prop, Fit.Width, 0.6f),
            new("Restaurant", "towelrail", "179_shop_towel_rail", Profile.Prop, Fit.Width, 0.5f),
            new("Restaurant", "pot_large", "180_shop_pot_large", Profile.Handheld, Fit.Width, 0.42f),
            new("Restaurant", "pot_B", "181_shop_pot", Profile.Handheld, Fit.Width, 0.3f),
            new("Restaurant", "pan_B", "182_shop_pan", Profile.Handheld, Fit.Longest, 0.42f),
            new("Restaurant", "bowl", "183_shop_bowl", Profile.Handheld, Fit.Width, 0.2f),
            new("Restaurant", "pillar_A", "184_shop_pillar", Profile.Environment, Fit.Height, 3.2f),
            new("Restaurant", "door_A", "185_shop_door", Profile.Prop, Fit.Height, 2.1f),
            new("Restaurant", "door_B", "186_shop_door_glazed", Profile.Prop, Fit.Height, 2.1f),
            new("Restaurant", "food_ingredient_steak_pieces", "187_shop_steak_cubes", Profile.Handheld, Fit.Longest, 0.26f),

            // --- kitchen set --------------------------------------------------
            new("Kitchen", "fridge", "190_kitchen_fridge", Profile.Prop, Fit.Height, 1.85f),
            new("Kitchen", "extractor_hood", "191_kitchen_hood", Profile.Prop, Fit.Width, 1.2f),
            new("Kitchen", "countertop_straight_A", "192_kitchen_counter_a", Profile.Prop, Fit.Width, 1.2f),
            new("Kitchen", "countertop_straight_B", "193_kitchen_counter_b", Profile.Prop, Fit.Width, 1.2f),
            new("Kitchen", "countertop_corner_inner", "194_kitchen_counter_corner", Profile.Prop, Fit.Width, 1.2f),
            new("Kitchen", "countertop_sink", "195_kitchen_counter_sink", Profile.Prop, Fit.Width, 1.2f),
            new("Kitchen", "wall_cabinet_straight", "196_kitchen_wall_cabinet", Profile.Prop, Fit.Width, 1.2f),
            new("Kitchen", "wall_shelf_kitchen", "197_kitchen_wall_shelf", Profile.Prop, Fit.Width, 1.2f),
            new("Kitchen", "wall_shelf_kitchen_hooks_decorated", "198_kitchen_wall_shelf_hooks", Profile.Prop, Fit.Width, 1.2f),
            new("Kitchen", "wall_knife_rack", "199_kitchen_knife_rack", Profile.Prop, Fit.Width, 0.5f),
            new("Kitchen", "papertowel_holder", "200_kitchen_papertowel", Profile.Handheld, Fit.Height, 0.3f),
            new("Kitchen", "utensils_cup", "201_kitchen_utensils", Profile.Handheld, Fit.Height, 0.3f),
            new("Kitchen", "container_kitchen_A_red", "202_kitchen_container_red", Profile.Handheld, Fit.Height, 0.22f),
            new("Kitchen", "container_kitchen_A_white", "203_kitchen_container_white", Profile.Handheld, Fit.Height, 0.22f),
            new("Kitchen", "container_kitchen_B_blue", "204_kitchen_container_blue", Profile.Handheld, Fit.Height, 0.2f),
            new("Kitchen", "blinds_kitchen", "205_kitchen_blinds", Profile.Prop, Fit.Width, 1.2f),

            // --- the shop shell -----------------------------------------------
            // A modular kit: 2x4x0.5 unit panels on a 2 unit grid, with 2x2 floor
            // tiles. Everything structural is scaled by the same 0.705, which puts
            // the module on 1.41 m and the wall at 2.82 m. That pitch is not
            // arbitrary - the lot is 22.56 x 16.92, so the runs come out at exactly
            // 16 and 12 panels with nothing left over to fudge at the corner.
            new("Kitchen", "wall_tiles_kitchen_straight", "260_wall_straight", Profile.Environment, Fit.Width, WallModule),
            new("Kitchen", "wall_tiles_kitchen_window", "261_wall_window", Profile.Environment, Fit.Width, WallModule),
            new("Kitchen", "wall_tiles_kitchen_doorway", "262_wall_doorway", Profile.Environment, Fit.Width, WallModule),
            new("Kitchen", "wall_tiles_kitchen_corner_inner", "263_wall_corner_inner", Profile.Environment, Fit.Width, WallCorner),
            new("Kitchen", "wall_tiles_kitchen_corner_outer", "264_wall_corner_outer", Profile.Environment, Fit.Width, WallCorner),
            new("Kitchen", "wall_plain_kitchen_straight", "265_wall_plain_straight", Profile.Environment, Fit.Width, WallModule),
            new("Kitchen", "wall_plain_kitchen_window", "266_wall_plain_window", Profile.Environment, Fit.Width, WallModule),
            new("Kitchen", "wall_plain_kitchen_corner_inner", "267_wall_plain_corner_inner", Profile.Environment, Fit.Width, WallCorner),
            new("Kitchen", "wall_plain_kitchen_corner_outer", "268_wall_plain_corner_outer", Profile.Environment, Fit.Width, WallCorner),
            // Laid at twice the wall pitch. At the wall's own 1.41 m the floor
            // needed 192 tiles and the grout drew a grid over every metre of the
            // shop, which is what got the last tiled floor taken out again.
            new("Kitchen", "floor_tiles_kitchen", "269_floor_tile", Profile.Environment, Fit.Width, WallModule * 2f),
            new("Restaurant", "floor_kitchen", "270_floor_tile_warm", Profile.Environment, Fit.Width, WallModule * 2f),
            new("Kitchen", "wall_cabinet_corner", "206_kitchen_wall_cabinet_corner", Profile.Prop, Fit.Width, 1.2f),

            // --- dressing and office furniture --------------------------------
            new("Interior", "WoolCarpet1", "210_decor_carpet", Profile.Prop, Fit.Longest, 2.4f),
            new("Interior", "CeilingLamp4", "212_decor_ceiling_lamp", Profile.Prop, Fit.Width, 0.55f),
            new("Interior", "FloorLamp1", "213_decor_floor_lamp", Profile.Prop, Fit.Height, 1.7f),
            new("Interior", "ThreeSeaterCouch1", "214_decor_couch", Profile.Prop, Fit.Longest, 2.1f),
            new("Interior", "ModernKitchenStool1", "215_decor_stool", Profile.Prop, Fit.Height, 0.75f),
            new("Interior", "CoffeeMachine1", "216_decor_coffee_machine", Profile.Prop, Fit.Height, 0.45f),
            new("Interior", "Monitor1", "217_office_monitor", Profile.Prop, Fit.Longest, 0.55f),
            new("Interior", "ExecutiveDesk1", "218_office_desk", Profile.Prop, Fit.Longest, 1.6f),
            new("Interior", "ExecutiveChair1", "219_office_chair", Profile.Prop, Fit.Height, 1.15f),

            // --- food kit -----------------------------------------------------
            // Already close to life size, so these carry the bundle's own scale
            // except where the game draws them deliberately chunky.
            new("Food", "sub", "230_food_wrap", Profile.Handheld, Fit.Longest, 0.3f),
            new("Food", "taco", "231_food_taco", Profile.Handheld, Fit.Longest, 0.22f),
            new("Food", "meatRaw", "232_food_meat_raw", Profile.Handheld, Fit.Longest, 0.34f),
            new("Food", "wholeHam", "233_food_meat_cooked", Profile.Handheld, Fit.Longest, 0.34f),
            new("Food", "meatSausage", "234_food_meat_sliced", Profile.Handheld, Fit.Longest, 0.24f),
            new("Food", "soda", "235_food_soda", Profile.Handheld, Fit.Height, 0.22f),
            new("Food", "sodaCan", "236_food_soda_can", Profile.Handheld, Fit.Height, 0.16f),
            new("Food", "cup", "237_food_cup", Profile.Handheld, Fit.Height, 0.12f),
            new("Food", "cake", "238_food_cake", Profile.Handheld, Fit.Width, 0.3f),
            new("Food", "cupcake", "239_food_cupcake", Profile.Handheld, Fit.Height, 0.14f),
            new("Food", "donutChocolate", "240_food_donut", Profile.Handheld, Fit.Width, 0.15f),
            new("Food", "muffin", "241_food_muffin", Profile.Handheld, Fit.Height, 0.12f),
            new("Food", "croissant", "242_food_croissant", Profile.Handheld, Fit.Longest, 0.2f),
            new("Food", "pie", "243_food_pie", Profile.Handheld, Fit.Width, 0.3f),
            new("Food", "bag", "244_food_bag", Profile.Handheld, Fit.Height, 0.34f),
            new("Food", "bagFlat", "245_food_bag_flat", Profile.Handheld, Fit.Longest, 0.34f),
            new("Food", "carton", "246_food_carton", Profile.Handheld, Fit.Height, 0.24f),
            new("Food", "styrofoam", "247_food_styrofoam", Profile.Handheld, Fit.Longest, 0.3f),
            new("Food", "styrofoamDinner", "248_food_styrofoam_dinner", Profile.Handheld, Fit.Longest, 0.3f),
            new("Food", "fries", "249_food_fries", Profile.Handheld, Fit.Height, 0.2f),
            new("Food", "tomato", "250_food_tomato", Profile.Handheld, Fit.Height, 0.09f),
            new("Food", "lemon", "251_food_lemon", Profile.Handheld, Fit.Height, 0.09f),
            new("Food", "salad", "252_food_salad", Profile.Handheld, Fit.Width, 0.26f),
            new("Food", "bread", "253_food_bread", Profile.Handheld, Fit.Longest, 0.22f),
            new("Food", "loafBaguette", "254_food_baguette", Profile.Handheld, Fit.Longest, 0.5f),
            new("Food", "barrel", "255_food_barrel", Profile.Prop, Fit.Height, 0.85f),
            new("Food", "can", "256_food_can", Profile.Handheld, Fit.Height, 0.12f),
            new("Food", "frappe", "257_food_frappe", Profile.Handheld, Fit.Height, 0.24f),
            new("Food", "iceCream", "258_food_ice_cream", Profile.Handheld, Fit.Height, 0.22f),
            new("Food", "sundae", "259_food_sundae", Profile.Handheld, Fit.Height, 0.24f)
        };

        /// <summary>
        /// The locomotion states <see cref="CozyAnimationDriver"/> crossfades to,
        /// paired with the clip names these bundles ship. The crowd walks and
        /// stands; nothing in the pack carries a tray, so CarryWalk reuses the walk
        /// and a customer holding a bag simply walks with it.
        /// </summary>
        private static readonly (string State, string[] ClipCandidates)[] LocomotionStates =
        {
            ("Idle", new[] { "Idle", "Idle_A", "Breathing Idle", "Stand" }),
            ("Walk", new[] { "Walk", "Walking", "Walk_A", "Run" }),
            ("CarryWalk", new[] { "CarryWalk", "Walk", "Walking", "Walk_A", "Run" })
        };

        [MenuItem("Shawarma Tycoon/PolyPack/Rebuild Poly Prefabs", priority = 120)]
        public static void RebuildMenu() => BuildAll();

        [MenuItem("Shawarma Tycoon/PolyPack/Rebuild Poly Prefabs", true)]
        private static bool ValidateRebuildMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling;
        }

        /// <summary>
        /// Prints what every source model actually measures, in metres, with its
        /// pivot offset. The bundles come from four authors at four scales, so this
        /// is how the <see cref="Spec.Scale"/> column gets filled in.
        /// </summary>
        [MenuItem("Shawarma Tycoon/PolyPack/Report Poly Bounds", priority = 121)]
        public static void ReportBounds()
        {
            List<string> lines = new();
            foreach (Spec spec in Specs)
            {
                string path = FindSourcePath(spec);
                if (path == null)
                {
                    lines.Add($"{spec.Id}\tMISSING");
                    continue;
                }

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                {
                    lines.Add($"{spec.Id}\tNOT IMPORTED");
                    continue;
                }

                Bounds bounds = MeasureBounds(asset);
                string clips = spec.Profile == Profile.Character
                    ? "\tclips=" + string.Join(",", ClipNames(path))
                    : string.Empty;
                lines.Add(
                    $"{spec.Id}\tsize={bounds.size.x:0.###}x{bounds.size.y:0.###}x{bounds.size.z:0.###}" +
                    $"\tminY={bounds.min.y:0.###}\tcentre={bounds.center.x:0.##},{bounds.center.z:0.##}{clips}");
            }

            Debug.Log("[PolyPack] bounds report\n" + string.Join("\n", lines));
        }

        public static int BuildAll()
        {
            EnsureAssetFolder(PrefabFolder);
            EnsureAssetFolder(AnimationFolder);

            List<string> missing = Specs
                .Where(spec => FindSourcePath(spec) == null)
                .Select(spec => $"{spec.FolderPath}/{spec.File}")
                .ToList();
            if (missing.Count > 0)
            {
                Debug.LogError("[PolyPack] Missing source models:\n" + string.Join("\n", missing));
                return 0;
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (Spec spec in Specs)
                    ConfigureImporter(spec);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            ExtractEmbeddedTextures();
            SharpenAtlasTextures();

            int built = 0;
            foreach (Spec spec in Specs)
                if (BuildPrefab(spec))
                    built++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (built == Specs.Length)
                Debug.Log($"[PolyPack] Built {built}/{Specs.Length} prefabs into {PrefabFolder}.");
            else
                Debug.LogError($"[PolyPack] Built only {built}/{Specs.Length} prefabs.");
            return built;
        }

        private static void ConfigureImporter(Spec spec)
        {
            string path = FindSourcePath(spec);
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError($"[PolyPack] No ModelImporter for {path}.");
                return;
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = false;
            importer.preserveHierarchy = false;
            importer.optimizeGameObjects = false;
            importer.addCollider = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importConstraints = false;
            importer.importAnimatedCustomProperties = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.generateSecondaryUV = false;
            importer.weldVertices = true;
            importer.optimizeMeshVertices = true;
            importer.optimizeMeshPolygons = true;
            importer.indexFormat = ModelImporterIndexFormat.UInt16;

            if (spec.Profile == Profile.Character)
            {
                // Generic rather than Humanoid: retargeting buys nothing when every
                // body shares one rig and one clip set, and it would cost an avatar
                // mapping pass per body on load.
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.resampleCurves = true;
                importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                // The bundles keep the skinned mesh under an armature root; flattening
                // it would drop the bones the clips address.
                importer.preserveHierarchy = true;

                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                    clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    foreach (ModelImporterClipAnimation clip in clips)
                    {
                        clip.loop = true;
                        clip.loopTime = true;
                        // The crowd is moved by code, so any translation baked into
                        // a clip would fight the mover.
                        clip.lockRootRotation = true;
                        clip.lockRootHeightY = true;
                        clip.lockRootPositionXZ = true;
                        clip.keepOriginalOrientation = true;
                        clip.keepOriginalPositionY = true;
                        clip.keepOriginalPositionXZ = true;
                    }
                    importer.clipAnimations = clips;
                }
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }

            importer.SaveAndReimport();
        }

        /// <summary>
        /// Unpacks the palette atlas each textured bundle carries inside its FBX
        /// files and relinks the materials onto it. Without this the models import
        /// with a material that names the atlas but binds no texture, and the whole
        /// city, the restaurant set and the kitchen set come through plain white.
        ///
        /// A bundle shares one atlas across all its models, so the first extraction
        /// writes the PNG and the rest bind to the file already sitting there.
        /// </summary>
        private static void ExtractEmbeddedTextures()
        {
            foreach (string folder in Specs.Select(spec => spec.FolderPath).Distinct())
                EnsureAssetFolder(folder + "/Textures");

            foreach (Spec spec in Specs)
            {
                string path = FindSourcePath(spec);
                if (AssetImporter.GetAtPath(path) is ModelImporter importer)
                    importer.ExtractTextures($"{spec.FolderPath}/Textures");
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Point-filters the unpacked atlases. They are grids of flat colour
        /// patches a few texels across, and bilinear sampling reads over the patch
        /// borders, which puts a wrong-coloured fringe along every UV seam.
        /// </summary>
        private static void SharpenAtlasTextures()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SourceRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                if (importer.filterMode == FilterMode.Point && importer.wrapMode == TextureWrapMode.Clamp)
                    continue;

                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
            }
        }

        private static bool BuildPrefab(Spec spec)
        {
            string path = FindSourcePath(spec);
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (modelAsset == null)
            {
                Debug.LogError($"[PolyPack] {spec.Id} failed to import from {path}.");
                return false;
            }

            GameObject prefabRoot = new(spec.Id);
            try
            {
                GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject
                                   ?? UnityEngine.Object.Instantiate(modelAsset);
                model.name = "Model";
                model.transform.SetParent(prefabRoot.transform, false);
                // The importer's own rotation is what stands a Z-up export upright,
                // so it is kept and any declared correction turns it further.
                // Clearing it here is what used to lay the interior props on their
                // backs and stand the carpet up like a wall.
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation =
                    Quaternion.Euler(spec.ModelEuler) * model.transform.localRotation;

                foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogError($"[PolyPack] {spec.Id} imported without renderers.");
                    return false;
                }

                if (!TryPlaceModel(spec, model.transform, renderers))
                    return false;

                foreach (Renderer renderer in renderers)
                {
                    // Food and cutlery are drawn a few centimetres across and often
                    // sit inside a counter or a bag; their shadows cost more than
                    // they read.
                    renderer.shadowCastingMode = spec.Profile == Profile.Handheld
                        ? ShadowCastingMode.Off
                        : ShadowCastingMode.On;
                    renderer.receiveShadows = spec.Profile != Profile.Handheld;
                    renderer.allowOcclusionWhenDynamic = true;
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        skinned.updateWhenOffscreen = false;
                        skinned.quality = SkinQuality.Bone2;
                    }
                }

                prefabRoot.AddComponent<CozyVisualMetadata>()
                    .Configure(spec.File, new Vector3(0f, spec.Yaw, 0f), spec.Profile == Profile.Character);

                if (spec.Profile == Profile.Character && !SetUpCharacter(spec, model, path))
                    return false;

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, spec.PrefabPath, out bool saved);
                if (!saved)
                {
                    Debug.LogError($"[PolyPack] Failed to save {spec.PrefabPath}.");
                    return false;
                }
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabRoot);
            }
        }

        /// <summary>
        /// Solves the uniform scale from what the mesh measures against the metre
        /// size the spec declares, then lands the model on a bottom-centre pivot.
        /// That pivot is the contract the rest of the project places against: a
        /// caller gives a floor position and expects the piece to stand on it.
        /// </summary>
        private static bool TryPlaceModel(Spec spec, Transform model, Renderer[] renderers)
        {
            model.localScale = Vector3.one;
            Bounds bounds = WorldBounds(renderers);
            if (bounds.size.sqrMagnitude <= 0f)
            {
                Debug.LogError($"[PolyPack] {spec.Id} measures nothing; cannot scale it.");
                return false;
            }

            float measured = spec.Fit switch
            {
                Fit.Height => bounds.size.y,
                Fit.Width => bounds.size.x,
                Fit.Depth => bounds.size.z,
                Fit.Longest => Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)),
                _ => 0f
            };

            float scale = 1f;
            if (spec.Fit != Fit.None)
            {
                if (measured <= 0.00001f)
                {
                    Debug.LogError($"[PolyPack] {spec.Id} is flat on the {spec.Fit} axis; pick another fit.");
                    return false;
                }
                scale = spec.Size / measured;
            }

            model.localScale = Vector3.one * scale;
            bounds = WorldBounds(renderers);
            model.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return true;
        }

        private static Bounds WorldBounds(Renderer[] renderers)
        {
            bool found = false;
            Bounds bounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found ? bounds : new Bounds(Vector3.zero, Vector3.zero);
        }

        private static bool SetUpCharacter(Spec spec, GameObject model, string modelPath)
        {
            AnimatorController controller = CreateOrUpdateController(spec, modelPath);
            if (controller == null) return false;

            Animator animator = model.GetComponentInChildren<Animator>(true)
                                ?? model.AddComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<Avatar>()
                .FirstOrDefault();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            model.transform.parent.gameObject.AddComponent<CozyAnimationDriver>();
            return true;
        }

        private static AnimatorController CreateOrUpdateController(Spec spec, string modelPath)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length == 0)
            {
                Debug.LogError($"[PolyPack] {spec.Id} carries no animation clips.");
                return null;
            }

            string path = $"{AnimationFolder}/{spec.Id}.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path)
                                            ?? AnimatorController.CreateAnimatorControllerAtPath(path);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
                stateMachine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
                stateMachine.RemoveAnyStateTransition(transition);

            foreach ((string stateName, string[] candidates) in LocomotionStates)
            {
                AnimationClip motion = FindClip(clips, candidates) ?? clips[0];
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = motion;
                if (stateName == "Idle") stateMachine.defaultState = state;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip FindClip(IReadOnlyList<AnimationClip> clips, string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                foreach (AnimationClip clip in clips)
                    if (string.Equals(ShortName(clip.name), candidate, StringComparison.OrdinalIgnoreCase))
                        return clip;
            }

            // Fall back to a loose match so a pack that ships "CharacterArmature|Walk"
            // or "Walk_Loop" still finds its clip instead of freezing on the idle.
            foreach (string candidate in candidates)
            {
                foreach (AnimationClip clip in clips)
                    if (clip.name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                        return clip;
            }
            return null;
        }

        private static string[] ClipNames(string modelPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .Select(clip => clip.name)
                .ToArray();
        }

        private static string ShortName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            string[] parts = name.Split('|');
            return parts[parts.Length - 1].Trim();
        }

        private static string FindSourcePath(Spec spec)
        {
            foreach (string extension in new[] { ".fbx", ".obj" })
            {
                string path = $"{spec.FolderPath}/{spec.File}{extension}";
                if (File.Exists(path)) return path;
            }
            return null;
        }

        private static Bounds MeasureBounds(GameObject asset)
        {
            Renderer[] renderers = asset.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = default;
            foreach (Renderer renderer in renderers)
            {
                Bounds source = renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null
                    ? skinned.sharedMesh.bounds
                    : renderer.bounds;
                if (!found)
                {
                    bounds = source;
                    found = true;
                }
                else bounds.Encapsulate(source);
            }
            return found ? bounds : new Bounds(Vector3.zero, Vector3.zero);
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
