using System.Collections.Generic;
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
        [SerializeField, Min(0.1f)] private float eatingDuration = 8f;
        [SerializeField, Min(1)] private int mealPayout = 15;
        /// <summary>
        /// How long a customer stays happy. Under this they pay full and step the
        /// combo; past it their mood drops a notch every AngerStepSeconds. It has
        /// to be reachable from the back of a full queue - at 8 s it was not, so
        /// every customer past the first paid a penalty and the combo could never
        /// get off the ground in a busy shop. The queue is seven deep now, so this
        /// moved out with it.
        /// </summary>
        [SerializeField, Min(1f)] private float angryAfterSeconds = 26f;
        [SerializeField, Min(2f)] private float patienceSeconds = 20f;

        /// <summary>Seconds between one drop in mood and the next.</summary>
        [SerializeField, Min(1f)] private float angerStepSeconds = 9f;

        /// <summary>
        /// What each mood pays, from content down to furious. An ordinary
        /// customer never leaves - they just get less worth serving, which is
        /// what the queue costs you now.
        /// </summary>
        private static readonly float[] MoodPayout = { 1f, 0.8f, 0.55f, 0.3f };

        private CustomerManager manager;
        private OrderBubble bubble;
        private float bubbleLift;

        /// <summary>The capsule other things bump into. Null if none was fitted.</summary>
        private CharacterController body;
        private CustomerTable table;
        private Transform exitPoint;
        private Vector3 queueTarget;
        private Vector3 gatePoint;
        private bool hasGate;
        private bool throughGate;
        private Vector3 cornerPoint;
        private bool hasCorner;
        private bool roundedCorner;
        private float eatingTimer;
        private float queueTimer;
        private GameObject angryFace;
        private int finalPayout;
        private bool isVip;
        private bool isAngry;
        private bool reachedQueue;
        private int moodStep;
        private float arrivalMultiplier = 1f;
        private List<Vector3> navigationCorners = new();
        private int navigationCorner;
        private int navigationVersion = -1;
        private Vector3 navigationGoal;
        private float navigationStuckTimer;

        public CustomerState State { get; private set; } = CustomerState.Queueing;

        /// <summary>What they are queueing for. Never null once configured.</summary>
        public CustomerOrder Order { get; private set; } = new();

        public bool IsVip => isVip;

        /// <summary>They are in the line proper, not still walking up the street.</summary>
        public bool HasReachedQueue => reachedQueue;

        /// <summary>
        /// Waited long enough to settle for whatever is in stock. Until then they
        /// hold out for the full order, which is what makes an empty fridge cost
        /// the shop something.
        /// </summary>
        public bool HasGivenUpOnExtras => moodStep >= 1;

        public bool IsAngry => isAngry;
        public bool LeftUnserved { get; private set; }
        /// <summary>0 content, 3 furious. Drives payout and the face.</summary>
        public int MoodStep => moodStep;

        public void Configure(
            CustomerManager owner, Transform exitTransform, float speed, float eatingSeconds,
            int payout, float patience, bool vip = false, float arrivalMood = 1f,
            CustomerOrder order = null)
        {
            manager = owner;
            Order = order ?? new CustomerOrder();
            if (Order.LineCount == 0) Order.Add(ItemType.Wrap, 1);
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

            bubble = OrderBubble.Create(transform, isVip ? 2.55f : 2.2f, bubbleLift);
            bubble.Show(Order);
        }

        /// <summary>
        /// Raises this customer's bubble clear of their neighbours'. The manager
        /// steps it along the queue, so a line of people reads as a staircase of
        /// cards rather than a row of overlapping ones.
        /// </summary>
        public void SetBubbleLift(float lift) => bubbleLift = lift;

        /// <summary>Redraws the bubble after the order has been trimmed.</summary>
        public void RefreshOrderBubble() => bubble?.Show(Order);

        public void SetQueueTarget(Vector3 target)
        {
            queueTarget = target;
        }

        /// <summary>
        /// The spot just inside the gate. Customers walk up the pavement and in
        /// through the door rather than appearing at it, and leave the same way,
        /// so the boundary the gate draws is one they are actually seen crossing.
        /// </summary>
        public void SetGatePoint(Vector3 point)
        {
            gatePoint = point;
            hasGate = true;
        }

        /// <summary>
        /// The turn at the bottom of the pavement, walked to before the gate.
        /// </summary>
        public void SetApproachCorner(Vector3 point)
        {
            cornerPoint = point;
            hasCorner = true;
        }

        /// <summary>The bill, paid at the till the moment they are served.</summary>
        public int CounterPayment { get; private set; }

        public void Serve(CustomerTable assignedTable)
        {
            table = assignedTable;
            bool fast = moodStep == 0;
            ComboSystem.Instance?.RegisterDineIn(fast, isVip);

            // Mood and what the queue looked like when they arrived bear on the
            // bill. A VIP served promptly is still the prize; a VIP kept waiting
            // is worth no more than anyone else. The shop's standing is not in
            // here - it moves the tip, below.
            float serviceMultiplier = MoodPayout[moodStep] * arrivalMultiplier;
            if (isVip && fast) serviceMultiplier *= 3f;

            if (fast) ReputationSystem.Instance?.RegisterHappyCustomer();
            // A bag with a drink and a dessert in it is worth more than a wrap on
            // its own, so what they walked out with sets the size of the bill.
            int bill = RewardCalculator.Calculate(
                mealPayout, serviceMultiplier * Order.ValueMultiplier);

            // The bill is paid at the till, and paid now, so a moving queue earns
            // while it moves. What is left on the table afterwards is the tip: how
            // much depends on what the shop's name is worth and on whether this
            // particular customer was kept waiting.
            CounterPayment = Mathf.Max(1, bill);
            finalPayout = Mathf.Max(1, Mathf.RoundToInt(
                bill * ReputationSystem.TipRate * MoodPayout[moodStep]));
            // Served: they have what they came for, so the bubble comes down.
            bubble?.Hide();
            ClearNavigation();
            State = CustomerState.WalkingToTable;
        }

        private void Awake() => body = GetComponent<CharacterController>();

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            switch (State)
            {
                case CustomerState.Queueing:
                    // Patience measures queueing, not the walk in from the street.
                    // Counting from the spawn point burned a quarter of it on the
                    // approach, before the customer had waited for anything - and
                    // that approach is now the length of the pavement.
                    // Down the pavement to the corner first, then in through the
                    // gate. Without the corner the walk cut diagonally across the
                    // forecourt from wherever they spawned, which read as people
                    // drifting at the shop rather than walking down a street to it.
                    if (hasCorner && !roundedCorner)
                    {
                        if (MoveTowards(cornerPoint)) roundedCorner = true;
                        UpdateMood();
                        break;
                    }

                    if (hasGate && !throughGate)
                    {
                        if (MoveTowards(gatePoint)) throughGate = true;
                        UpdateMood();
                        break;
                    }

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

                    if (MoveNavigatedTowards(table.SeatApproachPoint))
                    {
                        SetAngryFace(false);
                        WarpTo(table.SeatPoint.position, table.SeatPoint.rotation);
                        eatingTimer = eatingDuration;
                        ClearNavigation();
                        State = CustomerState.Eating;
                    }
                    break;

                case CustomerState.Eating:
                    eatingTimer -= Time.deltaTime;
                    if (eatingTimer <= 0f)
                    {
                        if (table != null)
                            WarpTo(table.SeatApproachPoint, table.SeatPoint.rotation);
                        table?.FinishMeal(this, finalPayout);
                        ClearNavigation();
                        State = CustomerState.Leaving;
                    }
                    break;

                case CustomerState.Leaving:
                    // Out through the same door they came in by, then off up the
                    // pavement. Walking diagonally through the fence to the exit
                    // marker made the gate decoration.
                    if (hasGate && throughGate)
                    {
                        if (MoveNavigatedTowards(gatePoint))
                        {
                            throughGate = false;
                            ClearNavigation();
                        }
                        break;
                    }

                    if (exitPoint == null || MoveNavigatedTowards(exitPoint.position))
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
            if (body != null)
                CharacterBody.StepTowards(body, target, moveSpeed, Time.deltaTime);
            else
                transform.position = Vector3.MoveTowards(
                    transform.position, target, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                12f * Time.deltaTime);
            return false;
        }

        /// <summary>
        /// Follows corners around counters and moved furniture. It deliberately
        /// keeps the existing CharacterController instead of handing motion to a
        /// NavMeshAgent, so collision and the game's current visual movement stay
        /// unchanged.
        /// </summary>
        private bool MoveNavigatedTowards(Vector3 target)
        {
            if (navigationCorners == null) navigationCorners = new List<Vector3>();
            target.y = transform.position.y;
            RestaurantNavigation navigation = RestaurantNavigation.Instance;
            int version = navigation != null ? navigation.Version : -1;
            bool needsPath = navigationCorners.Count == 0 ||
                             navigationVersion != version ||
                             Vector3.SqrMagnitude(navigationGoal - target) > 0.09f;
            if (needsPath) BuildNavigationPath(navigation, target, version);

            Vector3 waypoint = target;
            while (navigationCorner < navigationCorners.Count)
            {
                waypoint = navigationCorners[navigationCorner];
                waypoint.y = transform.position.y;
                if (Vector3.SqrMagnitude(waypoint - transform.position) > 0.05f) break;
                navigationCorner++;
            }
            // The last corner is the nearest reachable point around the goal. A
            // seat is intentionally off the navmesh, inside its chair, so once
            // this corner is reached the seating warp completes the last step.
            if (navigationCorners.Count > 0 && navigationCorner >= navigationCorners.Count)
                return true;
            if (navigationCorner >= navigationCorners.Count) waypoint = target;

            Vector3 before = transform.position;
            bool reachedWaypoint = MoveTowards(waypoint);
            if (reachedWaypoint && navigationCorner < navigationCorners.Count)
            {
                navigationCorner++;
                return false;
            }

            float moved = Vector3.SqrMagnitude(transform.position - before);
            if (moved < 0.000025f && Vector3.SqrMagnitude(target - transform.position) > 0.05f)
            {
                navigationStuckTimer += Time.deltaTime;
                if (navigationStuckTimer >= 0.65f)
                {
                    BuildNavigationPath(navigation, target, version);
                    navigationStuckTimer = 0f;
                }
            }
            else navigationStuckTimer = 0f;
            return navigationCorner >= navigationCorners.Count &&
                   Vector3.SqrMagnitude(target - transform.position) <= 0.025f;
        }

        private void BuildNavigationPath(
            RestaurantNavigation navigation, Vector3 target, int version)
        {
            if (navigationCorners == null) navigationCorners = new List<Vector3>();
            navigationCorners.Clear();
            navigationCorner = 0;
            navigationGoal = target;
            navigationVersion = version;
            if (navigation != null)
                navigation.TryCalculatePath(transform.position, target, navigationCorners);
        }

        private void ClearNavigation()
        {
            if (navigationCorners == null) navigationCorners = new List<Vector3>();
            navigationCorners.Clear();
            navigationCorner = 0;
            navigationVersion = -1;
            navigationStuckTimer = 0f;
        }

        private void WarpTo(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = body != null && body.enabled;
            if (wasEnabled) body.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (wasEnabled) body.enabled = true;
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
