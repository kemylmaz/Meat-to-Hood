using UnityEngine;

namespace ShawarmaTycoon
{
    public enum CustomerState
    {
        Queueing,
        WalkingToTable,
        Eating,
        Leaving
    }

    public sealed class CustomerAgent : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.4f;
        [SerializeField, Min(0.1f)] private float eatingDuration = 4.5f;
        [SerializeField, Min(1)] private int mealPayout = 15;
        [SerializeField, Min(1f)] private float angryAfterSeconds = 8f;
        [SerializeField, Min(2f)] private float patienceSeconds = 20f;

        private CustomerManager manager;
        private CustomerTable table;
        private Transform exitPoint;
        private Vector3 queueTarget;
        private float eatingTimer;
        private float queueTimer;
        private GameObject angryFace;
        private int finalPayout;
        private bool isVip;
        private bool isAngry;
        private bool reachedQueue;

        public CustomerState State { get; private set; } = CustomerState.Queueing;
        public bool IsVip => isVip;
        public bool IsAngry => isAngry;
        public bool LeftUnserved { get; private set; }

        public void Configure(
            CustomerManager owner, Transform exitTransform, float speed, float eatingSeconds,
            int payout, float patience, bool vip = false)
        {
            manager = owner;
            exitPoint = exitTransform;
            moveSpeed = Mathf.Max(0.1f, speed);
            eatingDuration = Mathf.Max(0.1f, eatingSeconds);
            mealPayout = Mathf.Max(1, payout);
            finalPayout = mealPayout;
            patienceSeconds = Mathf.Max(angryAfterSeconds + 1f, patience);
            isVip = vip;
            isAngry = false;
            reachedQueue = false;
            LeftUnserved = false;
            State = CustomerState.Queueing;
            if (isVip) CreateVipVisual();
        }

        public void SetQueueTarget(Vector3 target)
        {
            queueTarget = target;
        }

        public void Serve(CustomerTable assignedTable)
        {
            table = assignedTable;
            bool fast = !isAngry && queueTimer < angryAfterSeconds;
            ComboSystem.Instance?.RegisterDineIn(fast, isVip);
            float serviceMultiplier = isVip
                ? (fast ? 3f : 1f)
                : (fast ? 1f : 0.6f);
            finalPayout = RewardCalculator.Calculate(mealPayout, serviceMultiplier);
            State = CustomerState.WalkingToTable;
        }

        private void Update()
        {
            switch (State)
            {
                case CustomerState.Queueing:
                    // Patience measures queueing, not the walk in from the street.
                    // Counting from the spawn point burned a quarter of it on the
                    // 13 m approach, before the customer had waited for anything.
                    if (MoveTowards(queueTarget)) reachedQueue = true;
                    if (reachedQueue) queueTimer += Time.deltaTime;
                    if (!isAngry && queueTimer >= angryAfterSeconds)
                    {
                        isAngry = true;
                        SetAngryFace(true);
                    }
                    if (queueTimer >= patienceSeconds) GiveUp();
                    break;

                case CustomerState.WalkingToTable:
                    if (table == null || table.SeatPoint == null)
                    {
                        State = CustomerState.Leaving;
                        break;
                    }

                    if (MoveTowards(table.SeatPoint.position))
                    {
                        SetAngryFace(false);
                        transform.position = table.SeatPoint.position;
                        transform.rotation = table.SeatPoint.rotation;
                        eatingTimer = eatingDuration;
                        State = CustomerState.Eating;
                    }
                    break;

                case CustomerState.Eating:
                    eatingTimer -= Time.deltaTime;
                    if (eatingTimer <= 0f)
                    {
                        table?.FinishMeal(this, finalPayout);
                        State = CustomerState.Leaving;
                    }
                    break;

                case CustomerState.Leaving:
                    if (exitPoint == null || MoveTowards(exitPoint.position))
                        manager?.Despawn(this);
                    break;
            }
        }

        /// <summary>
        /// Patience runs out and the customer walks out without buying. The queue
        /// had no failure state before this: an ignored customer stood there going
        /// angry forever and still paid, at 0.6x, whenever you finally got to them,
        /// so letting the line back up cost nothing but time.
        /// </summary>
        private void GiveUp()
        {
            LeftUnserved = true;
            State = CustomerState.Leaving;
            SetAngryFace(true);
            ComboSystem.Instance?.BreakCombo();
            GameProgress.RecordLostCustomer();
            AudioDirector.Play(GameSfx.Error, 0.55f);
        }

        private bool MoveTowards(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 delta = target - transform.position;
            if (delta.sqrMagnitude <= 0.025f)
                return true;

            Vector3 direction = delta.normalized;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(-direction, Vector3.up),
                12f * Time.deltaTime);
            return false;
        }

        private void CreateVipVisual()
        {
            GameObject crown = new("VIP Crown");
            crown.transform.SetParent(transform, false);
            Color gold = new(1f, 0.72f, 0.12f);
            PrototypeVisuals.CreatePrimitive("Crown Base", PrimitiveType.Cube, crown.transform,
                new Vector3(0f, 1.82f, 0f), new Vector3(0.46f, 0.10f, 0.30f), gold);
            PrototypeVisuals.CreatePrimitive("Crown Left", PrimitiveType.Sphere, crown.transform,
                new Vector3(-0.16f, 1.96f, 0f), new Vector3(0.13f, 0.22f, 0.13f), gold);
            PrototypeVisuals.CreatePrimitive("Crown Center", PrimitiveType.Sphere, crown.transform,
                new Vector3(0f, 2.02f, 0f), new Vector3(0.13f, 0.28f, 0.13f), gold);
            PrototypeVisuals.CreatePrimitive("Crown Right", PrimitiveType.Sphere, crown.transform,
                new Vector3(0.16f, 1.96f, 0f), new Vector3(0.13f, 0.22f, 0.13f), gold);
            TextMesh vipLabel = PrototypeVisuals.CreateLabel("VIP", crown.transform, new Vector3(0f, 2.34f, 0f), 0.13f);
            vipLabel.color = new Color(0.82f, 0.42f, 0.04f);
            vipLabel.fontStyle = FontStyle.Bold;
        }

        private void SetAngryFace(bool visible)
        {
            if (angryFace == null && visible)
            {
                angryFace = new GameObject("Kızgın Emoji");
                angryFace.transform.SetParent(transform, false);
                angryFace.transform.localPosition = isVip
                    ? new Vector3(0.62f, 2.15f, 0f)
                    : new Vector3(0f, 2.0f, 0f);
                PrototypeVisuals.CreatePrimitive("Yüz", PrimitiveType.Sphere, angryFace.transform,
                    Vector3.zero, new Vector3(0.42f, 0.42f, 0.15f), PrototypeVisuals.Red);
                PrototypeVisuals.CreatePrimitive("Kaş Sol", PrimitiveType.Cube, angryFace.transform,
                    new Vector3(-0.11f, 0.08f, -0.13f), new Vector3(0.13f, 0.035f, 0.025f), Color.black, new Vector3(0f, 0f, -25f));
                PrototypeVisuals.CreatePrimitive("Kaş Sağ", PrimitiveType.Cube, angryFace.transform,
                    new Vector3(0.11f, 0.08f, -0.13f), new Vector3(0.13f, 0.035f, 0.025f), Color.black, new Vector3(0f, 0f, 25f));
                PrototypeVisuals.CreatePrimitive("Ağız", PrimitiveType.Cube, angryFace.transform,
                    new Vector3(0f, -0.12f, -0.13f), new Vector3(0.17f, 0.03f, 0.025f), Color.black);
            }
            if (angryFace != null) angryFace.SetActive(visible);
        }
    }
}
