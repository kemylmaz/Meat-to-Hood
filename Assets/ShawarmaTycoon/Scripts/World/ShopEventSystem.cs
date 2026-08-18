using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Things that go wrong while the shop runs itself.
    ///
    /// Once the belts and the staff are bought there is nothing left for the
    /// player to do but walk to the till - measured, that is most of the second
    /// half of the game. A station that breaks and has to be walked to and fixed
    /// puts them back on the floor without taking the automation away again.
    ///
    /// Breakdowns only start once the shop can actually cope without the player:
    /// breaking the spit on a hand-carried line would just be a tax on someone
    /// already doing all the work.
    /// </summary>
    public sealed class ShopEventSystem : MonoBehaviour
    {
        [SerializeField, Min(10f)] private float firstBreakdownDelay = 150f;
        [SerializeField, Min(10f)] private float minimumInterval = 70f;
        [SerializeField, Min(10f)] private float maximumInterval = 130f;

        /// <summary>How long the player has to stand at a broken station.</summary>
        [SerializeField, Min(0.5f)] private float repairSeconds = 2.4f;
        [SerializeField, Min(0.5f)] private float repairRadius = 1.7f;

        private readonly List<ItemStation> candidates = new();
        private Transform player;
        private float timer;

        public ItemStation Broken { get; private set; }

        public void Configure(Transform playerTransform, IEnumerable<ItemStation> processors)
        {
            player = playerTransform;
            candidates.Clear();
            foreach (ItemStation station in processors)
                if (station != null && station.Mode == StationMode.Processor)
                    candidates.Add(station);
            timer = firstBreakdownDelay;
        }

        /// <summary>
        /// Only worth breaking things once at least one belt runs the line. Before
        /// that the player is the conveyor, and a breakdown is just a delay.
        /// </summary>
        private static bool ShopRunsItself =>
            GameProgress.GetInt("belt.raw", 0) > 0 ||
            GameProgress.GetInt("belt.oven", 0) > 0 ||
            GameProgress.GetInt("belt.cutting", 0) > 0;

        private void Update()
        {
            if (Broken != null)
            {
                UpdateRepair();
                return;
            }

            if (!ShopRunsItself) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            TriggerBreakdown();
        }

        private void TriggerBreakdown()
        {
            timer = Random.Range(
                Mathf.Min(minimumInterval, maximumInterval),
                Mathf.Max(minimumInterval, maximumInterval));

            // Only something that is actually running: breaking an idle station
            // costs nothing and reads as noise.
            List<ItemStation> running = new();
            for (int i = 0; i < candidates.Count; i++)
            {
                ItemStation station = candidates[i];
                if (station == null || !station.isActiveAndEnabled || station.IsBroken) continue;
                if (station.InputCount <= 0) continue;
                running.Add(station);
            }
            if (running.Count == 0) return;

            Broken = running[Random.Range(0, running.Count)];
            Broken.Break(repairSeconds);
            AudioDirector.Play(GameSfx.Error, 0.7f);
        }

        private void UpdateRepair()
        {
            if (Broken == null || !Broken.IsBroken)
            {
                Broken = null;
                return;
            }

            if (player == null) return;
            if (Vector3.SqrMagnitude(player.position - Broken.transform.position) >
                repairRadius * repairRadius) return;

            if (!Broken.Repair(Time.deltaTime)) return;
            ComboSystem.Instance?.RegisterManualAction();
            Broken = null;
        }
    }
}
