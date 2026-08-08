using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class CustomerTable : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float cleaningDuration = 1.4f;
        [SerializeField, Min(0.5f)] private float cleaningRadius = 1.65f;

        private Transform player;
        private Transform seatPoint;
        private CustomerAgent occupant;
        private GameObject dirtyIndicator;
        private TextMesh statusLabel;
        private bool reserved;
        private bool dirty;
        private float cleaningProgress;
        private int pendingCash;
        private int lastDisplayedState = -1;
        private int lastDisplayedPercent = -1;

        public bool IsAvailable => gameObject.activeInHierarchy && !reserved && !dirty;
        public bool IsDirty => dirty;
        public bool IsReserved => reserved;
        public Transform SeatPoint => seatPoint;
        public float CleaningProgress => cleaningDuration <= 0f ? 0f : cleaningProgress / cleaningDuration;

        public void Configure(Transform playerTransform, Transform customerSeat)
        {
            player = playerTransform;
            seatPoint = customerSeat;

            dirtyIndicator = PrototypeVisuals.CreatePrimitive(
                "Dirty Plate",
                PrimitiveType.Cylinder,
                transform,
                new Vector3(0f, 0.86f, 0f),
                new Vector3(0.36f, 0.035f, 0.36f),
                PrototypeVisuals.Red);
            dirtyIndicator.SetActive(false);

            statusLabel = PrototypeVisuals.CreateLabel("Masa", transform, new Vector3(0f, 1.25f, 0f), 0.12f);
        }

        public bool TryReserve(CustomerAgent customer)
        {
            if (!IsAvailable || customer == null) return false;
            occupant = customer;
            reserved = true;
            UpdateLabel();
            return true;
        }

        public void CancelReservation(CustomerAgent customer)
        {
            if (occupant != customer) return;
            occupant = null;
            reserved = false;
            UpdateLabel();
        }

        public void FinishMeal(CustomerAgent customer, int payout)
        {
            if (occupant != customer) return;
            occupant = null;
            reserved = false;
            dirty = true;
            cleaningProgress = 0f;
            pendingCash += Mathf.Max(0, payout);
            if (dirtyIndicator != null) dirtyIndicator.SetActive(true);
            UpdateLabel();
        }

        private void Update()
        {
            if (!dirty || player == null) return;

            float sqrDistance = Vector3.SqrMagnitude(player.position - transform.position);
            if (sqrDistance > cleaningRadius * cleaningRadius)
            {
                cleaningProgress = Mathf.Max(0f, cleaningProgress - Time.deltaTime * 0.5f);
                return;
            }

            if (pendingCash > 0 && GameEconomy.Instance != null)
            {
                GameEconomy.Instance.AddCoins(pendingCash);
                pendingCash = 0;
            }

            cleaningProgress += Time.deltaTime;
            if (cleaningProgress < cleaningDuration)
            {
                UpdateLabel();
                return;
            }

            dirty = false;
            cleaningProgress = 0f;
            if (dirtyIndicator != null) dirtyIndicator.SetActive(false);
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (statusLabel == null) return;

            int state = dirty ? 2 : reserved ? 1 : 0;
            int percent = dirty
                ? Mathf.FloorToInt(Mathf.Clamp01(CleaningProgress) * 10f) * 10
                : -1;

            if (state == lastDisplayedState && percent == lastDisplayedPercent)
                return;

            lastDisplayedState = state;
            lastDisplayedPercent = percent;

            if (dirty)
                statusLabel.text = $"Temizle %{percent}";
            else if (reserved)
                statusLabel.text = "Dolu";
            else
                statusLabel.text = "Masa";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, cleaningRadius);
        }
    }
}
