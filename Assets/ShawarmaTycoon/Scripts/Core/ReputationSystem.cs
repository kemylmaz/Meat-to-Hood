using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// The shop's standing, on a 0 to 100 scale. Keeping people waiting costs
    /// it, serving them promptly earns it back, and it scales what everyone pays.
    ///
    /// This is what carries the pressure now that ordinary customers no longer
    /// walk out: a badly run shift still gets served, it just earns less and
    /// takes a while to recover.
    /// </summary>
    public sealed class ReputationSystem : MonoBehaviour
    {
        public static ReputationSystem Instance { get; private set; }

        /// <summary>
        /// Measured against a well run shift: about 2.7 customers a minute leave
        /// happy while roughly 4 anger steps land on the ones waiting. Gain has to
        /// beat that or the score sinks to zero however well the shop is run,
        /// which is what the first pair of numbers did.
        /// </summary>
        [SerializeField, Range(0f, 100f)] private float startingScore = 80f;
        [SerializeField, Min(0f)] private float gainPerHappyCustomer = 2.5f;
        [SerializeField, Min(0f)] private float lossPerAngerStep = 1.2f;
        [SerializeField, Range(0f, 1f)] private float worstMultiplier = 0.7f;

        private float score;

        public float Score => score;
        /// <summary>Reputation as a 0..1 fraction, for bars and labels.</summary>
        public float Normalised => Mathf.Clamp01(score / 100f);

        /// <summary>
        /// What the shop's name is worth on every sale. A spotless shop pays
        /// full; the worst one still trades, at worstMultiplier.
        /// </summary>
        public static float PayoutMultiplier => Instance == null
            ? 1f
            : Mathf.Lerp(Instance.worstMultiplier, 1f, Instance.Normalised);

        public event Action<float> ScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            score = GameProgress.GetInt("reputation", Mathf.RoundToInt(startingScore));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>A customer left the counter happy.</summary>
        public void RegisterHappyCustomer() => Adjust(gainPerHappyCustomer, true);

        /// <summary>
        /// A customer's patience slipped another notch. Called once per step, so
        /// the longer a queue is ignored the more it costs.
        /// </summary>
        public void RegisterAngerStep() => Adjust(-lossPerAngerStep, true);

        private void Adjust(float delta, bool persist)
        {
            float next = Mathf.Clamp(score + delta, 0f, 100f);
            if (Mathf.Approximately(next, score)) return;

            score = next;
            if (persist) GameProgress.SetInt("reputation", Mathf.RoundToInt(score));
            ScoreChanged?.Invoke(score);
        }
    }
}
