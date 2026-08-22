using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// A heap of takings sitting somewhere in the shop, collected by walking onto
    /// it. The tables have their own; this is the one at the till, where the queue
    /// pays before it sits down.
    ///
    /// The whole bill used to land on the table at the end of the meal, so a busy
    /// counter earned nothing until the first diner had finished eating.
    /// </summary>
    public sealed class CashPile : MonoBehaviour
    {
        [SerializeField, Min(0.3f)] private float pickupRadius = 0.95f;

        private Transform player;
        private GameObject pad;
        private GameObject bills;
        private TextMesh amountLabel;
        private int pending;

        public int Pending => pending;
        public bool HasCash => pending > 0;
        public Vector3 CollectPoint => pad != null ? pad.transform.position : transform.position;

        public void Configure(Transform playerTransform, float radius = 0.95f)
        {
            player = playerTransform;
            pickupRadius = Mathf.Max(0.3f, radius);

            pad = new GameObject("Cash Pad");
            pad.transform.SetParent(transform, false);
            PrototypeVisuals.CreatePrimitive(
                "Cash Pad Surface", PrimitiveType.Cube, pad.transform,
                new Vector3(0f, 0.055f, 0f), new Vector3(0.62f, 0.04f, 0.40f),
                new Color(0.12f, 0.66f, 0.27f));
            amountLabel = PrototypeVisuals.CreateLabel("", pad.transform, Vector3.up * 0.36f, 0.13f);

            bills = new GameObject("Cash Stack");
            bills.transform.SetParent(transform, false);
            bills.transform.localPosition = Vector3.up * 0.11f;
            for (int i = 0; i < 4; i++)
            {
                PrototypeVisuals.CreatePrimitive("Cash Bill", PrimitiveType.Cube, bills.transform,
                    new Vector3(i % 2 == 0 ? -0.03f : 0.03f, i * 0.025f, 0f),
                    new Vector3(0.56f, 0.025f, 0.32f),
                    i % 2 == 0 ? new Color(0.25f, 0.88f, 0.33f) : new Color(0.14f, 0.68f, 0.24f));
            }

            // The authored pad already includes its banded cash, so it replaces
            // both the pad surface and the loose bills.
            MeshyVisuals.TryReplaceDirect(pad.transform, "18_money_collection_pad",
                new Vector3(0.86f, 0.42f, 0.86f), Vector3.zero, Vector3.zero, false,
                "Cash Pad Surface");
            foreach (Transform bill in bills.transform)
                bill.gameObject.SetActive(!MeshyVisuals.IsAvailable("18_money_collection_pad"));

            Refresh();
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            pending += amount;
            Refresh();
        }

        /// <summary>Takes the heap. False when there was nothing to take.</summary>
        public bool TryCollect(bool byWorker)
        {
            if (pending <= 0 || GameEconomy.Instance == null) return false;

            int collected = pending;
            pending = 0;
            GameEconomy.Instance.AddCoins(collected);
            GameProgress.RecordRevenue(collected);
            if (byWorker) ComboSystem.Instance?.RegisterWorkerAction();
            else ComboSystem.Instance?.RegisterManualAction();
            CoinBurst.SpawnGain(CollectPoint + Vector3.up * 0.35f, collected);
            AudioDirector.Play(GameSfx.CashRegister);
            Refresh();
            return true;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (pending <= 0 || player == null) return;
            if (Vector3.SqrMagnitude(player.position - CollectPoint) >
                pickupRadius * pickupRadius) return;
            TryCollect(false);
        }

        private void Refresh()
        {
            bool has = pending > 0;
            if (pad != null) pad.SetActive(has);
            if (bills != null) bills.SetActive(has);
            if (amountLabel != null) amountLabel.text = has ? "+$" + pending : string.Empty;
        }
    }
}
