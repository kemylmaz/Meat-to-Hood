using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Tracks short service streaks and exposes their current income multiplier.</summary>
    public sealed class ComboSystem : MonoBehaviour
    {
        public static ComboSystem Instance { get; private set; }

        [SerializeField, Min(0.1f)] private float timeoutSeconds = 8f;
        [SerializeField, Min(1)] private int mediumMultiplierThreshold = 3;
        [SerializeField, Min(2)] private int maximumMultiplierThreshold = 6;

        private int streak;
        private int maximumStreak;
        private float timeRemaining;

        public int Streak => streak;
        public int MaximumStreak => maximumStreak;
        public float TimeRemaining => streak > 0 ? Mathf.Max(0f, timeRemaining) : 0f;
        /// <summary>Full length of the streak window, so HUDs can draw a fill bar.</summary>
        public float TimeoutSeconds => timeoutSeconds;
        public float Multiplier => MultiplierFor(streak);
        public bool IsActive => streak > 0;

        public static float CurrentMultiplier => Instance != null ? Instance.Multiplier : 1f;

        public event Action<int, float> ComboChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (streak <= 0)
                return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
                BreakCombo();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RegisterDineIn(bool fast, bool isVip)
        {
            if (!fast)
            {
                BreakCombo();
                return;
            }

            Increment(isVip ? 2 : 1);
        }

        public void RegisterManualAction()
        {
            Increment(1);
        }

        public void RegisterWorkerAction()
        {
            RefreshTimeout();
        }

        public void RegisterTakeaway(bool byWorker)
        {
            if (byWorker)
                RefreshTimeout();
            else
                Increment(1);
        }

        public void BreakCombo()
        {
            if (streak <= 0)
            {
                timeRemaining = 0f;
                return;
            }

            streak = 0;
            timeRemaining = 0f;
            ComboChanged?.Invoke(streak, Multiplier);
        }

        private void Increment(int amount)
        {
            streak += Mathf.Max(1, amount);
            timeRemaining = timeoutSeconds;
            // Pitch climbs with the streak so a hot run is audible.
            AudioDirector.Play(GameSfx.ComboUp, 0.8f, 1f + Mathf.Min(streak, 8) * 0.07f);

            if (streak > maximumStreak)
            {
                maximumStreak = streak;
                GameProgress.RecordBestCombo(maximumStreak);
            }

            ComboChanged?.Invoke(streak, Multiplier);
        }

        private void RefreshTimeout()
        {
            if (streak > 0)
                timeRemaining = timeoutSeconds;
        }

        private float MultiplierFor(int value)
        {
            if (value >= maximumMultiplierThreshold)
                return 2f;
            if (value >= mediumMultiplierThreshold)
                return 1.5f;
            return 1f;
        }
    }
}
