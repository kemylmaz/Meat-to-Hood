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

        private CustomerManager manager;
        private CustomerTable table;
        private Transform exitPoint;
        private Vector3 queueTarget;
        private float eatingTimer;

        public CustomerState State { get; private set; } = CustomerState.Queueing;

        public void Configure(CustomerManager owner, Transform exitTransform, float speed, float eatingSeconds, int payout)
        {
            manager = owner;
            exitPoint = exitTransform;
            moveSpeed = Mathf.Max(0.1f, speed);
            eatingDuration = Mathf.Max(0.1f, eatingSeconds);
            mealPayout = Mathf.Max(1, payout);
            State = CustomerState.Queueing;
        }

        public void SetQueueTarget(Vector3 target)
        {
            queueTarget = target;
        }

        public void Serve(CustomerTable assignedTable)
        {
            table = assignedTable;
            State = CustomerState.WalkingToTable;
        }

        private void Update()
        {
            switch (State)
            {
                case CustomerState.Queueing:
                    MoveTowards(queueTarget);
                    break;

                case CustomerState.WalkingToTable:
                    if (table == null || table.SeatPoint == null)
                    {
                        State = CustomerState.Leaving;
                        break;
                    }

                    if (MoveTowards(table.SeatPoint.position))
                    {
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
                        table?.FinishMeal(this, mealPayout);
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
    }
}
