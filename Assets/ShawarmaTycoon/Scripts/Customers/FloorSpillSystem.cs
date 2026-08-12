using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Spawns an occasional, non-blocking floor spill beside a dining table.
    /// The player cleans it by standing nearby; the recruited cleaner can remove
    /// the oldest spill through TryCleanByWorker.
    /// </summary>
    public sealed class FloorSpillSystem : MonoBehaviour
    {
        private sealed class SpillRecord
        {
            public CustomerTable Table;
            public GameObject Root;
            public float Dwell;
            public int SpawnOrder;
        }

        public static FloorSpillSystem Instance { get; private set; }

        [SerializeField, Range(0f, 1f)] private float spawnChance = 0.30f;
        [SerializeField, Min(1)] private int maximumActiveSpills = 4;
        [SerializeField, Min(0.1f)] private float cleanupRadius = 0.90f;
        [SerializeField, Min(0.1f)] private float cleanupDwellSeconds = 0.80f;

        private readonly List<CustomerTable> tables = new();
        private readonly List<SpillRecord> spills = new();
        private Transform player;
        private int spawnSequence;

        public int ActiveCount
        {
            get
            {
                PruneMissing();
                return spills.Count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Configure(Transform playerTransform, IEnumerable<CustomerTable> customerTables)
        {
            player = playerTransform;
            tables.Clear();

            if (customerTables == null)
                return;

            foreach (CustomerTable table in customerTables)
            {
                if (table != null && !tables.Contains(table))
                    tables.Add(table);
            }
        }

        public bool TrySpawnForTable(CustomerTable table, bool force = false)
        {
            PruneMissing();
            if (table == null || !table.gameObject.activeInHierarchy)
                return false;
            if (!tables.Contains(table) || spills.Count >= maximumActiveSpills || HasSpill(table))
                return false;
            if (!force && Random.value > spawnChance)
                return false;

            int tableIndex = tables.IndexOf(table);
            float side = tableIndex % 2 == 0 ? -1.30f : 1.30f;

            GameObject spillRoot = new($"Floor Spill - {table.name}");
            spillRoot.transform.SetParent(table.transform, false);
            spillRoot.transform.localPosition = new Vector3(side, 0.04f, 0.45f);

            Color sauce = tableIndex % 2 == 0
                ? new Color(0.72f, 0.32f, 0.18f)
                : new Color(0.84f, 0.55f, 0.20f);
            Color crumb = new(0.95f, 0.75f, 0.33f);

            PrototypeVisuals.CreatePrimitive(
                "Sauce Mark", PrimitiveType.Cylinder, spillRoot.transform,
                Vector3.zero, new Vector3(0.52f, 0.018f, 0.38f), sauce);
            PrototypeVisuals.CreatePrimitive(
                "Small Mark", PrimitiveType.Cylinder, spillRoot.transform,
                new Vector3(0.31f, 0.008f, -0.11f), new Vector3(0.18f, 0.014f, 0.13f), sauce);
            PrototypeVisuals.CreatePrimitive(
                "Crumb A", PrimitiveType.Sphere, spillRoot.transform,
                new Vector3(-0.20f, 0.035f, 0.18f), new Vector3(0.10f, 0.035f, 0.08f), crumb);
            PrototypeVisuals.CreatePrimitive(
                "Crumb B", PrimitiveType.Sphere, spillRoot.transform,
                new Vector3(0.16f, 0.035f, 0.22f), new Vector3(0.075f, 0.03f, 0.065f), crumb);

            spills.Add(new SpillRecord
            {
                Table = table,
                Root = spillRoot,
                Dwell = 0f,
                SpawnOrder = ++spawnSequence
            });
            return true;
        }

        public bool SpawnNow()
        {
            PruneMissing();
            if (spills.Count >= maximumActiveSpills || tables.Count == 0)
                return false;

            int start = Random.Range(0, tables.Count);
            for (int i = 0; i < tables.Count; i++)
            {
                CustomerTable table = tables[(start + i) % tables.Count];
                if (TrySpawnForTable(table, true))
                    return true;
            }

            return false;
        }

        public bool TryCleanByWorker()
        {
            PruneMissing();
            int oldestIndex = -1;
            int oldestOrder = int.MaxValue;

            for (int i = 0; i < spills.Count; i++)
            {
                SpillRecord spill = spills[i];
                if (spill.Root == null || !spill.Root.activeInHierarchy)
                    continue;
                if (spill.SpawnOrder >= oldestOrder)
                    continue;

                oldestOrder = spill.SpawnOrder;
                oldestIndex = i;
            }

            if (oldestIndex < 0)
                return false;

            Clean(oldestIndex, false);
            return true;
        }

        private void Update()
        {
            if (player == null)
                return;

            float radiusSquared = cleanupRadius * cleanupRadius;
            for (int i = spills.Count - 1; i >= 0; i--)
            {
                SpillRecord spill = spills[i];
                if (spill.Root == null || spill.Table == null)
                {
                    spills.RemoveAt(i);
                    continue;
                }

                if (!spill.Root.activeInHierarchy ||
                    Vector3.SqrMagnitude(player.position - spill.Root.transform.position) > radiusSquared)
                {
                    spill.Dwell = Mathf.Max(0f, spill.Dwell - Time.deltaTime * 2f);
                    continue;
                }

                spill.Dwell += Time.deltaTime;
                float progress = Mathf.Clamp01(spill.Dwell / cleanupDwellSeconds);
                spill.Root.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.72f, progress);
                if (spill.Dwell >= cleanupDwellSeconds)
                    Clean(i, true);
            }
        }

        private bool HasSpill(CustomerTable table)
        {
            for (int i = 0; i < spills.Count; i++)
                if (spills[i].Table == table) return true;
            return false;
        }

        private void Clean(int index, bool manual)
        {
            if (index < 0 || index >= spills.Count)
                return;

            SpillRecord spill = spills[index];
            spills.RemoveAt(index);
            if (spill.Root != null)
                Destroy(spill.Root);

            if (manual) ComboSystem.Instance?.RegisterManualAction();
            else ComboSystem.Instance?.RegisterWorkerAction();
            GameProgress.RecordTrash(1);
        }

        private void PruneMissing()
        {
            for (int i = spills.Count - 1; i >= 0; i--)
            {
                if (spills[i].Table == null || spills[i].Root == null)
                    spills.RemoveAt(i);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.88f, 0.48f, 0.18f, 0.75f);
            for (int i = 0; i < spills.Count; i++)
            {
                if (spills[i].Root != null)
                    Gizmos.DrawWireSphere(spills[i].Root.transform.position, cleanupRadius);
            }
        }
    }
}
