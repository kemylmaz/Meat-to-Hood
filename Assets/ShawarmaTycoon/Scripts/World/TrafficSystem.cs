using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Ambient drive-by traffic on the block's road. Cars are pooled, driven
    /// along X in two lanes and recycled once they leave the ground slab.
    ///
    /// The near lane is the service lane, the one that runs past the drive-through
    /// window. Nothing drives in it until the window has been bought: a car going
    /// by a wall where the window will be reads as a drive-through the shop is
    /// failing to serve. Once it opens, a car in that lane pulls up at the window
    /// and waits for its order.
    /// </summary>
    public sealed class TrafficSystem : MonoBehaviour
    {
        [SerializeField, Min(0.4f)] private float minSpawnInterval = 2.4f;
        [SerializeField, Min(0.5f)] private float maxSpawnInterval = 6.5f;
        [SerializeField, Min(1)] private int maxActiveCars = 6;

        private static readonly Color[] BodyColors =
        {
            new(0.85f, 0.34f, 0.29f),
            new(0.33f, 0.72f, 0.68f),
            new(0.95f, 0.78f, 0.34f),
            new(0.38f, 0.45f, 0.62f),
            new(0.92f, 0.92f, 0.90f),
            new(0.45f, 0.58f, 0.40f)
        };

        private readonly List<CityCar> active = new();
        private readonly Stack<CityCar> pool = new();

        private CityLayout layout;
        private Transform carRoot;
        private TakeawaySystem window;
        private float spawnTimer;
        private int colorIndex;

        /// <summary>World X where a drive-through car would stop.</summary>
        public float ServiceStopX { get; set; }
        public bool StopRequested { get; private set; }

        /// <summary>+1 drives toward +X on the lane nearest the shop.</summary>
        public const int ServiceLaneDirection = 1;

        /// <summary>Whether anything is allowed into the lane past the window.</summary>
        public bool ServiceLaneOpen { get; private set; }

        public void Configure(CityLayout cityLayout)
        {
            layout = cityLayout;
            carRoot = new GameObject("Traffic").transform;
            carRoot.SetParent(transform, false);
            spawnTimer = 1.5f;

            // Seed the road so it is never empty on the first frame. Every seeded
            // car takes the far lane; the service lane stays empty until it opens.
            for (int i = 0; i < 3; i++)
                Spawn(-ServiceLaneDirection, Random.Range(0.15f, 0.85f));
        }

        /// <summary>
        /// Opens the service lane and points it at the window that is served from
        /// it. Called by the drive-through purchase, so the lane and the counter
        /// can never exist without each other.
        /// </summary>
        public void OpenServiceLane(TakeawaySystem driveThruWindow, float stopX)
        {
            window = driveThruWindow;
            ServiceStopX = stopX;
            ServiceLaneOpen = true;
        }

        public void RequestStop() => StopRequested = true;

        public void ReleaseStop()
        {
            StopRequested = false;
            for (int i = 0; i < active.Count; i++)
                active[i].Release();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (layout == null) return;

            // A car waits at the window exactly as long as the order does, rather
            // than for a fixed few seconds: the queue is the shop's problem to
            // clear, which is the whole point of putting one there.
            StopRequested = ServiceLaneOpen && window != null && window.PendingOrder;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
                if (active.Count < maxActiveCars)
                    Spawn(Random.value > 0.5f ? 1 : -1, 0f);
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                CityCar car = active[i];
                if (car == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (!car.Tick(Time.deltaTime)) continue;
                active.RemoveAt(i);
                car.gameObject.SetActive(false);
                pool.Push(car);
            }
        }

        private void Spawn(int direction, float startProgress)
        {
            float halfSpan = layout.GroundWidth * 0.5f;
            float startX = layout.CenterX - direction * halfSpan;
            float endX = layout.CenterX + direction * halfSpan;
            if (startProgress > 0f)
                startX = Mathf.Lerp(startX, endX, startProgress);

            // Half the +X traffic pulls into the driveway once it is open. The
            // rest, and everything before it opens, stays out on the road: a car
            // in the driveway is a customer, and there are none until the window
            // is there to serve them.
            bool serviceLane = ServiceLaneOpen &&
                direction == ServiceLaneDirection && Random.value > 0.45f;
            float laneZ = serviceLane ? layout.ServiceLaneZ : layout.LaneZ(direction);

            CityCar car = pool.Count > 0 ? pool.Pop() : CityCar.Create(carRoot, NextColor());
            car.gameObject.SetActive(true);
            car.Launch(this,
                new Vector3(startX, layout.RoadY, laneZ),
                direction,
                endX,
                Random.Range(4.4f, 7.6f),
                serviceLane);
            active.Add(car);
        }

        private Color NextColor()
        {
            colorIndex = (colorIndex + 1) % BodyColors.Length;
            return BodyColors[colorIndex];
        }
    }

    /// <summary>A single pooled car. Visuals fall back to primitives.</summary>
    public sealed class CityCar : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private TrafficSystem owner;
        private int direction = 1;
        private float endX;
        private float speed;
        private float currentSpeed;
        private float stopHold;
        private bool holding;
        private bool serviceLane;

        public static CityCar Create(Transform parent, Color bodyColor)
        {
            GameObject go = new("City Car");
            go.transform.SetParent(parent, false);
            CityCar car = go.AddComponent<CityCar>();

            // One of the four City Builder vehicles, each already painted on the
            // shared atlas. The authored car is the fallback below it, and it
            // ships a single red body material, so that one still gets tinted.
            string model = CityBlock.TrafficCars[Random.Range(0, CityBlock.TrafficCars.Length)];
            if (CityKit.Spawn(model, go.transform, Vector3.zero) != null)
                return car;

            if (MeshyVisuals.TryAttach(go.transform, "46_city_car",
                    new Vector3(1.9f, 1.5f, 4.2f), Vector3.zero, Vector3.zero) == null)
                BuildPrimitiveCar(go.transform, bodyColor);
            else
                TintBody(go.transform, bodyColor);
            return car;
        }

        /// <summary>
        /// The authored car ships one shared body material, so every instance
        /// would be the same red. Recolour just the body submesh through a
        /// property block, which keeps batching and touches no shared asset.
        /// </summary>
        public static void TintBody(Transform root, Color bodyColor)
        {
            MaterialPropertyBlock block = new();
            block.SetColor(BaseColorId, bodyColor);
            block.SetColor(ColorId, bodyColor);

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null || !slots[i].name.Contains("WarmRed")) continue;
                    renderer.SetPropertyBlock(block, i);
                }
            }
        }

        private static void BuildPrimitiveCar(Transform parent, Color bodyColor)
        {
            Color glass = new(0.55f, 0.75f, 0.82f);
            Color tire = new(0.13f, 0.13f, 0.15f);

            PrototypeVisuals.CreatePrimitive("Body", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.62f, 0f), new Vector3(1.86f, 0.68f, 4.05f), bodyColor);
            PrototypeVisuals.CreatePrimitive("Cabin", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.16f, 0.12f), new Vector3(1.62f, 0.60f, 1.95f), bodyColor);
            PrototypeVisuals.CreatePrimitive("Windshield", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.18f, -0.86f), new Vector3(1.42f, 0.44f, 0.10f), glass);
            PrototypeVisuals.CreatePrimitive("Rear Glass", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.18f, 1.08f), new Vector3(1.38f, 0.40f, 0.10f), glass);
            foreach (int sx in new[] { 1, -1 })
            {
                PrototypeVisuals.CreatePrimitive("Side Glass", PrimitiveType.Cube, parent,
                    new Vector3(sx * 0.80f, 1.20f, 0.12f), new Vector3(0.09f, 0.36f, 1.55f), glass);
                PrototypeVisuals.CreatePrimitive("Headlight", PrimitiveType.Cube, parent,
                    new Vector3(sx * 0.58f, 0.68f, -2.02f), new Vector3(0.34f, 0.18f, 0.10f),
                    new Color(1f, 0.95f, 0.78f));
                PrototypeVisuals.CreatePrimitive("Taillight", PrimitiveType.Cube, parent,
                    new Vector3(sx * 0.58f, 0.72f, 2.02f), new Vector3(0.30f, 0.16f, 0.10f),
                    new Color(0.86f, 0.22f, 0.18f));
                foreach (int sz in new[] { 1, -1 })
                    PrototypeVisuals.CreatePrimitive("Wheel", PrimitiveType.Cylinder, parent,
                        new Vector3(sx * 0.88f, 0.34f, sz * 1.30f),
                        new Vector3(0.34f, 0.10f, 0.34f), tire,
                        new Vector3(0f, 0f, 90f));
            }
        }

        public void Launch(TrafficSystem system, Vector3 position, int travelDirection,
                           float finishX, float travelSpeed, bool inServiceLane)
        {
            owner = system;
            direction = travelDirection;
            endX = finishX;
            speed = travelSpeed;
            currentSpeed = travelSpeed;
            serviceLane = inServiceLane;
            holding = false;
            stopHold = 0f;
            transform.position = position;
            // Approved CozyPack convention: authored models face local +Z.
            transform.rotation = Quaternion.LookRotation(
                new Vector3(direction, 0f, 0f), Vector3.up);
        }

        public void Release()
        {
            holding = false;
            stopHold = 0f;
        }

        /// <summary>Grace after an order is handed over, so the car does not lurch off mid-transaction.</summary>
        private const float PullAwayDelay = 0.5f;

        /// <summary>Returns true once the car has left the block and can be pooled.</summary>
        public bool Tick(float deltaTime)
        {
            if (owner == null) return true;

            float target = speed;
            // Only a car that pulled into the driveway stops. Braking out on the
            // road looked like a jam and served nobody.
            if (serviceLane && owner.StopRequested && !holding)
            {
                float distance = (owner.ServiceStopX - transform.position.x) * direction;
                if (distance > 0f && distance < 6f)
                {
                    target = Mathf.Lerp(0f, speed, Mathf.Clamp01(distance / 6f));
                    if (distance < 0.4f)
                    {
                        holding = true;
                        stopHold = PullAwayDelay;
                    }
                }
            }

            if (holding)
            {
                target = 0f;
                // The wait lasts as long as the order does. On a fixed timer the
                // car drove off mid-order and the window paid out to nobody.
                if (owner.StopRequested) stopHold = PullAwayDelay;
                else
                {
                    stopHold -= deltaTime;
                    if (stopHold <= 0f) holding = false;
                }
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, target, deltaTime * 9f);
            transform.position += new Vector3(direction * currentSpeed * deltaTime, 0f, 0f);

            return direction > 0
                ? transform.position.x > endX
                : transform.position.x < endX;
        }
    }
}
