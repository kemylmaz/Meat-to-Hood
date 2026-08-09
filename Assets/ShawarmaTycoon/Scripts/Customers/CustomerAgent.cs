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

        private CustomerManager manager;
        private CustomerTable table;
        private Transform exitPoint;
        private Vector3 queueTarget;
        private float eatingTimer;
        private float queueTimer;
        private GameObject angryFace;
        private int finalPayout;

        public CustomerState State { get; private set; } = CustomerState.Queueing;

        public void Configure(CustomerManager owner, Transform exitTransform, float speed, float eatingSeconds, int payout)
        {
            manager = owner;
            exitPoint = exitTransform;
            moveSpeed = Mathf.Max(0.1f, speed);
            eatingDuration = Mathf.Max(0.1f, eatingSeconds);
            mealPayout = Mathf.Max(1, payout);
            finalPayout = mealPayout;
            State = CustomerState.Queueing;
        }

        public void SetQueueTarget(Vector3 target)
        {
            queueTarget = target;
        }

        public void Serve(CustomerTable assignedTable)
        {
            table = assignedTable;
            finalPayout = queueTimer >= angryAfterSeconds ? Mathf.Max(1, Mathf.RoundToInt(mealPayout * 0.6f)) : mealPayout;
            State = CustomerState.WalkingToTable;
        }

        private void Update()
        {
            switch (State)
            {
                case CustomerState.Queueing:
                    MoveTowards(queueTarget);
                    queueTimer += Time.deltaTime;
                    if (queueTimer >= angryAfterSeconds) SetAngryFace(true);
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

        private void SetAngryFace(bool visible)
        {
            if (angryFace == null && visible)
            {
                angryFace = new GameObject("Kızgın Emoji");
                angryFace.transform.SetParent(transform, false);
                angryFace.transform.localPosition = new Vector3(0f, 2.0f, 0f);
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
