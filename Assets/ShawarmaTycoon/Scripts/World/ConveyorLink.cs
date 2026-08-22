using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class ConveyorLink : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float transferInterval = 0.75f;
        private ItemStation source;
        private ItemStation destination;
        private Transform beltVisual;
        private bool unlocked;
        private float timer;
        private int level;

        public bool IsUnlocked => unlocked;
        public int Level => level;

        /// <summary>
        /// A belt you have not bought is not there at all. It used to stand in the
        /// kitchen from the first second, greyed out with "BANT KİLİTLİ" floating
        /// over it - three machines in the way of the walk between counters,
        /// advertising themselves, before the shop had sold a single wrap.
        /// </summary>
        public void Configure(ItemStation from, ItemStation to, Transform visual)
        {
            source = from;
            destination = to;
            beltVisual = visual;
            UpdateVisual();
        }

        /// <summary>Where a parcel enters and leaves this belt, in world space.</summary>
        private Vector3 RideStart => transform.position + Vector3.up * 0.78f +
            (source != null ? (source.transform.position - transform.position).normalized * 0.9f : Vector3.zero);

        private Vector3 RideEnd => transform.position + Vector3.up * 0.78f +
            (destination != null ? (destination.transform.position - transform.position).normalized * 0.9f : Vector3.zero);

        public void Unlock()
        {
            SetLevel(Mathf.Max(1, level));
        }

        public void SetLevel(int newLevel)
        {
            level = Mathf.Clamp(newLevel, 0, 1);
            unlocked = level > 0;
            ApplyInterval();
            UpdateVisual();
        }

        /// <summary>
        /// Recomputes the transfer rate from the two things that own it: the belt's
        /// purchase decides whether it exists, and the HR automation upgrade scales
        /// every owned belt. There are no invisible per-belt speed purchases.
        /// </summary>
        public void ApplyInterval()
        {
            transferInterval = Mathf.Max(
                0.1f, 0.75f * HumanResourcesSystem.AssistIntervalMultiplier);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (!unlocked || source == null || destination == null) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = transferInterval;

            if (!source.TryTakeOutputForConveyor(out ItemType item)) return;
            if (!destination.TryReceiveFromConveyor(item))
            {
                source.ReturnOutputFromConveyor(item);
                return;
            }

            // The move already happened; this is it made visible. The counts
            // change instantly either way, so a parcel that is still in flight
            // cannot be lost or double-counted.
            BeltParcel.Send(beltVisual, RideStart, RideEnd, item,
                Mathf.Min(0.55f, transferInterval * 0.8f));
        }

        /// <summary>
        /// A belt is either built or it is not; there is nothing else to show.
        /// It used to be tinted to mark its state, and since the authored model
        /// carries no submesh by that name the tint landed on the first renderer
        /// it found - which painted the whole machine flat green.
        /// </summary>
        private void UpdateVisual()
        {
            if (beltVisual != null) beltVisual.gameObject.SetActive(unlocked);
        }
    }
}
