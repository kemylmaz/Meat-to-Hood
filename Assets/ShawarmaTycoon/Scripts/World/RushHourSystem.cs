using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Runs recurring restaurant rush periods and exposes their global modifiers.</summary>
    public sealed class RushHourSystem : MonoBehaviour
    {
        public static RushHourSystem Instance { get; private set; }

        [SerializeField, Min(0f)] private float initialDelaySeconds = 60f;
        [SerializeField, Min(0.1f)] private float activeDurationSeconds = 30f;
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 75f;
        [SerializeField, Range(0.1f, 1f)] private float activeSpawnIntervalMultiplier = 0.47f;
        [SerializeField, Min(1f)] private float activeIncomeMultiplier = 2f;
        [SerializeField, Range(0.1f, 1f)] private float activePatienceMultiplier = 0.7f;

        private bool isActive;
        private float countdown;

        public bool IsActive => isActive;
        public float Countdown => Mathf.Max(0f, countdown);
        public float ActiveTimeRemaining => isActive ? Countdown : 0f;
        public float TimeUntilNextRush => isActive ? 0f : Countdown;

        public static float SpawnIntervalMultiplier =>
            Instance != null && Instance.isActive ? Instance.activeSpawnIntervalMultiplier : 1f;

        public static float IncomeMultiplier =>
            Instance != null && Instance.isActive ? Instance.activeIncomeMultiplier : 1f;

        /// <summary>
        /// Rush arrivals are in a hurry and give up sooner. This is read once,
        /// when a customer or a takeaway order appears, so the rush starting does
        /// not shorten the fuse on people who are already waiting - which would
        /// empty the queue in one go the moment the bell rang.
        /// </summary>
        public static float PatienceMultiplier =>
            Instance != null && Instance.isActive ? Instance.activePatienceMultiplier : 1f;

        public event Action<bool> RushStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            isActive = false;
            countdown = initialDelaySeconds;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            countdown -= Time.deltaTime;
            if (countdown > 0f)
                return;

            if (isActive)
                EndRush();
            else
                StartRush(activeDurationSeconds);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ForceRush(float seconds = 15f)
        {
            StartRush(Mathf.Max(0.1f, seconds));
        }

        private void StartRush(float duration)
        {
            bool stateChanged = !isActive;
            isActive = true;
            countdown = Mathf.Max(0.1f, duration);
            if (stateChanged)
                RushStateChanged?.Invoke(true);
        }

        private void EndRush()
        {
            isActive = false;
            countdown = cooldownSeconds;
            RushStateChanged?.Invoke(false);
        }
    }
}
