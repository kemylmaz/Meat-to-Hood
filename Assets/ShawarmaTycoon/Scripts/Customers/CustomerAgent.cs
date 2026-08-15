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
        /// <summary>
        /// How long a customer stays happy. Under this they pay full and step the
        /// combo; past it their mood drops a notch every AngerStepSeconds. It has
        /// to be reachable from the back of a full queue - at 8 s it was not, so
        /// every customer past the first paid a penalty and the combo could never
        /// get off the ground in a busy shop.
        /// </summary>
        [SerializeField, Min(1f)] private float angryAfterSeconds = 15f;
        [SerializeField, Min(2f)] private float patienceSeconds = 20f;

        /// <summary>Seconds between one drop in mood and the next.</summary>
        [SerializeField, Min(1f)] private float angerStepSeconds = 7f;

        /// <summary>
        /// What each mood pays, from content down to furious. An ordinary
        /// customer never leaves - they just get less worth serving, which is
        /// what the queue costs you now.
        /// </summary>
        private static readonly float[] MoodPayout = { 1f, 0.8f, 0.55f, 0.3f };

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
        private int moodStep;
        private float arrivalMultiplier = 1f;

        public CustomerState State { get; private set; } = CustomerState.Queueing;
        public bool IsVip => isVip;
        public bool IsAngry => isAngry;
        public bool LeftUnserved { get; private set; }
        /// <summary>0 content, 3 furious. Drives payout and the face.</summary>
        public int MoodStep => moodStep;

        public void Configure(
            CustomerManager owner, Transform exitTransform, float speed, float eatingSeconds,
            int payout, float patience, bool vip = false, float arrivalMood = 1f)
        {
            manager = owner;
            exitPoint = exitTransform;
            moveSpeed = Mathf.Max(0.1f, speed);
            eatingDuration = Mathf.Max(0.1f, eatingSeconds);
            mealPayout = Mathf.Max(1, payout);
            finalPayout = mealPayout;
            patienceSeconds = Mathf.Max(angryAfterSeconds + 1f, patience);
            arrivalMultiplier = Mathf.Clamp(arrivalMood, 0.1f, 1f);
            isVip = vip;
            isAngry = false;
            reachedQueue = false;
            LeftUnserved = false;
            moodStep = 0;
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
            bool fast = moodStep == 0;
            ComboSystem.Instance?.RegisterDineIn(fast, isVip);

            // Mood, what the queue looked like when they arrived, and the shop's
            // standing all bear on the bill. A VIP served promptly is still the
            // prize; a VIP kept waiting is worth no more than anyone else.
            float serviceMultiplier = MoodPayout[moodStep] * arrivalMultiplier
                * ReputationSystem.PayoutMultiplier;
            if (isVip && fast) serviceMultiplier *= 3f;

            if (fast) ReputationSystem.Instance?.RegisterHappyCustomer();
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
                    UpdateMood();
                    // Only a VIP walks. An ordinary customer waits, and what it
                    // costs you is what they are worth by the time you get there.
                    if (isVip && queueTimer >= patienceSeconds) GiveUp();
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
        /// Steps the mood down while they wait. Each step costs the shop's
        /// standing, so an ignored queue bleeds reputation for as long as it is
        /// ignored rather than settling at one flat penalty.
        /// </summary>
        private void UpdateMood()
        {
            if (queueTimer < angryAfterSeconds) return;

            int step = 1 + Mathf.FloorToInt((queueTimer - angryAfterSeconds) / angerStepSeconds);
            step = Mathf.Min(step, MoodPayout.Length - 1);
            if (step <= moodStep) return;

            // The first notch is the ordinary cost of a queue and only shows in
            // the bill. Reputation answers for real neglect, from the second
            // notch on - otherwise a well run shop still bleeds it, because the
            // back of a three deep queue passes the happy mark by design.
            for (int i = Mathf.Max(moodStep, 1); i < step; i++)
                ReputationSystem.Instance?.RegisterAngerStep();

            moodStep = step;
            if (!isAngry)
            {
                isAngry = true;
                SetAngryFace(true);
                ComboSystem.Instance?.BreakCombo();
            }
            ShowMood();
        }

        /// <summary>Deepens the angry face as the mood drops.</summary>
        private void ShowMood()
        {
            if (angryFace == null) return;
            Transform face = angryFace.transform.Find("Yüz");
            if (face == null) return;
            Renderer renderer = face.GetComponent<Renderer>();
            if (renderer == null) return;

            float t = moodStep / (float)(MoodPayout.Length - 1);
            renderer.sharedMaterial = PrototypeVisuals.Material(
                Color.Lerp(new Color(0.98f, 0.76f, 0.26f), PrototypeVisuals.Red, t));
            float scale = 1f + t * 0.35f;
            angryFace.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// A VIP gives up and walks out. Ordinary customers stay - what an
        /// ignored queue costs is their mood, the combo and the shop's name.
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
                Quaternion.LookRotation(direction, Vector3.up),
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
