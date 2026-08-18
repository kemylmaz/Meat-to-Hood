#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShawarmaTycoon.Editor
{
    public static class Phase2ConfigurationBuilder
    {
        private const string ConfigFolder = "Assets/ShawarmaTycoon/Resources/Config";

        private static readonly Dictionary<string, string> Aliases = new()
        {
            { "01_player_character", "54_worker_red" },
            { "02_customer_character", "52_customer_sweater" },
            { "03_cashier_worker", "53_worker_teal" },
            { "06_shawarma_rotisserie", "06_rotisserie_station" },
            { "08_cutting_station", "60_cutting_station" },
            { "10_wrap_preparation_station", "61_wrap_station" },
            { "12_service_cashier_counter", "62_cashier_counter" },
            { "13_conveyor_straight", "63_conveyor_straight" },
            { "14_conveyor_corner", "64_conveyor_corner" },
            { "15_dining_table_clean", "70_dining_table" },
            { "16_dirty_table_props", "71_dining_table_dirty" },
            { "17_trash_bin", "72_trash_bin" },
            { "18_money_collection_pad", "82_money_pad" },
            { "19_upgrade_pad", "83_upgrade_pad" },
            { "20_locked_expansion_pad", "81_lock_pad" },
            { "21_entrance_door", "80_entrance" },
            { "22_modular_floor_tile", "78_floor_tiled" },
            { "23_modular_wall_straight", "77_wall_straight" },
            { "24_modular_wall_corner", "76_wall_corner" },
            { "25_hr_manager_desk", "65_manager_desk_stamp" },
            { "26_recruitment_desk", "66_manager_desk_pencils" },
            { "27_general_manager_desk", "67_manager_desk_plant" },
            { "33_decorative_plant", "73_planter" },
            { "34_floating_diorama_island", "79_floor_plot" }
        };

        [MenuItem("Shawarma Tycoon/Phase 2/Rebuild Configuration Assets")]
        public static void BuildDefaults()
        {
            EnsureFolder(ConfigFolder);
            CreateIfMissing<GameConfig>($"{ConfigFolder}/GameConfig.asset");
            CreateIfMissing<EconomyConfig>($"{ConfigFolder}/EconomyConfig.asset");
            CreateIfMissing<RestaurantLayoutConfig>($"{ConfigFolder}/RestaurantLayoutConfig.asset");
            DioramaWorldConfig world = CreateIfMissing<DioramaWorldConfig>($"{ConfigFolder}/DioramaWorldConfig.asset");
            world.RestorePhase3Defaults();
            EditorUtility.SetDirty(world);
            ArtCatalog catalog = CreateIfMissing<ArtCatalog>($"{ConfigFolder}/ArtCatalog.asset");
            RebuildArtCatalog(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GameCatalogs.ResetForEditor();
            Debug.Log($"[ShawarmaTycoon] Phase 3 configuration ready: {catalog.Entries.Count} art ids.");
        }

        private static void RebuildArtCatalog(ArtCatalog catalog)
        {
            Dictionary<string, GameObject> prefabs = new();
            AddPrefabs(prefabs, "Assets/ShawarmaTycoon/Resources/Phase1Prefabs", overwrite: true);
            AddPrefabs(prefabs, "Assets/ShawarmaTycoon/Resources/MeshyPrefabs", overwrite: false);

            List<ArtCatalog.Entry> entries = new();
            foreach (KeyValuePair<string, GameObject> pair in prefabs)
                entries.Add(new ArtCatalog.Entry { id = pair.Key, prefab = pair.Value });

            foreach (KeyValuePair<string, string> alias in Aliases)
            {
                if (!prefabs.TryGetValue(alias.Value, out GameObject prefab)) continue;
                entries.RemoveAll(entry => entry.id == alias.Key);
                entries.Add(new ArtCatalog.Entry { id = alias.Key, prefab = prefab });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            catalog.ReplaceEntries(entries);
            EditorUtility.SetDirty(catalog);
        }

        private static void AddPrefabs(
            IDictionary<string, GameObject> prefabs,
            string root,
            bool overwrite)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { root });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                if (overwrite || !prefabs.ContainsKey(prefab.name)) prefabs[prefab.name] = prefab;
            }
        }

        private static T CreateIfMissing<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
