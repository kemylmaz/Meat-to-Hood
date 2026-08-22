using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// A hired assistant that goes and does the job rather than causing it from
    /// where it stands. The three recruits used to be statues on a fixed spot
    /// while their effect fired on a timer somewhere else in the shop, so a paid
    /// shift showed up as a number changing on its own.
    ///
    /// A job is up to two stops: collect something, then deliver it. The cashier
    /// only has the first, the runner and the cleaner have both.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorkerAgent : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float moveSpeed = 3.1f;
        [SerializeField, Min(0.2f)] private float arriveRadius = 0.9f;
        [SerializeField, Min(0f)] private float workSeconds = 0.3f;

        private enum Phase { Idle, ToPickup, Collecting, ToDelivery, Delivering, GoingHome }

        private Vector3 home;

        /// <summary>The capsule other things bump into. Null if none was fitted.</summary>
        private CharacterController body;
        private CarryInventory hands;
        private Phase phase = Phase.Idle;

        private Vector3 pickupPoint;
        private Vector3 deliveryPoint;
        private Func<bool> onPickup;
        private Func<bool> onDeliver;
        private ItemType carriedType;
        private bool hasDeliveryLeg;
        private float timer;

        /// <summary>True while a job is in hand, so nothing new is thrown at it.</summary>
        public bool IsBusy => phase != Phase.Idle;

        public void Configure(Vector3 homePosition, CarryInventory carryHands)
        {
            home = homePosition;
            hands = carryHands;
            transform.position = homePosition;
            body = GetComponent<CharacterController>();
        }

        /// <summary>
        /// Sends the worker out. <paramref name="collect"/> runs on arrival at the
        /// first stop and decides whether the trip achieved anything; a false
        /// answer sends the worker straight home rather than miming a delivery.
        /// </summary>
        public bool Dispatch(
            Vector3 collectAt, Func<bool> collect, ItemType carrying,
            Vector3 deliverAt, Func<bool> deliver)
        {
            if (IsBusy) return false;

            pickupPoint = collectAt;
            deliveryPoint = deliverAt;
            onPickup = collect;
            onDeliver = deliver;
            carriedType = carrying;
            hasDeliveryLeg = deliver != null;
            phase = Phase.ToPickup;
            return true;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            switch (phase)
            {
                case Phase.Idle:
                    return;

                case Phase.ToPickup:
                    if (!Advance(pickupPoint)) return;
                    timer = workSeconds;
                    phase = Phase.Collecting;
                    return;

                case Phase.Collecting:
                    timer -= Time.deltaTime;
                    if (timer > 0f) return;
                    if (onPickup == null || !onPickup())
                    {
                        phase = Phase.GoingHome;
                        return;
                    }
                    if (carriedType != ItemType.None && hands != null)
                        hands.TryAdd(carriedType);
                    phase = hasDeliveryLeg ? Phase.ToDelivery : Phase.GoingHome;
                    return;

                case Phase.ToDelivery:
                    if (!Advance(deliveryPoint)) return;
                    timer = workSeconds;
                    phase = Phase.Delivering;
                    return;

                case Phase.Delivering:
                    timer -= Time.deltaTime;
                    if (timer > 0f) return;
                    onDeliver?.Invoke();
                    DropCarried();
                    phase = Phase.GoingHome;
                    return;

                case Phase.GoingHome:
                    if (!Advance(home)) return;
                    DropCarried();
                    phase = Phase.Idle;
                    return;
            }
        }

        /// <summary>Walks toward a point; true once it is close enough to work.</summary>
        private bool Advance(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 delta = target - transform.position;
            if (delta.sqrMagnitude <= arriveRadius * arriveRadius) return true;

            float effectiveSpeed = moveSpeed * HumanResourcesSystem.WorkerSpeedMultiplier;
            if (body != null)
                CharacterBody.StepTowards(body, target, effectiveSpeed, Time.deltaTime);
            else
                transform.position = Vector3.MoveTowards(
                    transform.position, target, effectiveSpeed * Time.deltaTime);
            // The models look along +Z once their authored offset is applied, and
            // the animation driver reads the movement itself for walk vs carry.
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up),
                9f * Time.deltaTime);
            return false;
        }

        /// <summary>
        /// Empties both hands. Plates ride in their own slot and Clear only drops
        /// the stack, so a cleaner that only cleared the stack would come back
        /// from the bin still holding the plates and accumulate them forever.
        /// </summary>
        private void DropCarried()
        {
            if (hands == null) return;
            if (hands.Count > 0) hands.Clear();
            if (hands.TrashCount > 0) hands.ClearTrash();
        }
    }
}
