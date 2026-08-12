using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Ambient drive-by traffic on the block's road. Cars are pooled, driven
    /// along X in two lanes and recycled once they leave the ground slab.
    ///
    /// A drive-through service lane can be layered on later without touching the
    /// spawner: call <see cref="RequestStop"/> to make the next car pull up at
    /// <see cref="ServiceStopX"/> and wait, then <see cref="ReleaseStop"/>.
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
        private float spawnTimer;
        private int colorIndex;

        /// <summary>World X where a drive-through car would stop.</summary>
        public float ServiceStopX { get; set; }
        public bool StopRequested { get; private set; }

        public void Configure(CityLayout cityLayout)
        {
            layout = cityLayout;
            ServiceStopX = cityLayout.CrossingX;
            carRoot = new GameObject("Traffic").transform;
            carRoot.SetParent(transform, false);
            spawnTimer = 1.5f;

            // Seed the road so it is never empty on the first frame.
            for (int i = 0; i < 3; i++)
                Spawn(Random.Range(0f, 1f) > 0.5f ? 1 : -1, Random.Range(0.15f, 0.85f));
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
            if (layout == null) return;

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

            CityCar car = pool.Count > 0 ? pool.Pop() : CityCar.Create(carRoot, NextColor());
            car.gameObject.SetActive(true);
            car.Launch(this,
                new Vector3(startX, 0.10f, layout.LaneZ(direction)),
                direction,
                endX,
                Random.Range(4.4f, 7.6f));
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

        public static CityCar Create(Transform parent, Color bodyColor)
        {
            GameObject go = new("City Car");
            go.transform.SetParent(parent, false);
            CityCar car = go.AddComponent<CityCar>();

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
        private static void TintBody(Transform root, Color bodyColor)
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
                           float finishX, float travelSpeed)
        {
            owner = system;
            direction = travelDirection;
            endX = finishX;
            speed = travelSpeed;
            currentSpeed = travelSpeed;
            holding = false;
            stopHold = 0f;
            transform.position = position;
            // Project convention: authored models face their local -Z.
            transform.rotation = Quaternion.LookRotation(
                new Vector3(-direction, 0f, 0f), Vector3.up);
        }

        public void Release()
        {
            holding = false;
            stopHold = 0f;
        }

        /// <summary>Returns true once the car has left the block and can be pooled.</summary>
        public bool Tick(float deltaTime)
        {
            if (owner == null) return true;

            float target = speed;
            if (owner.StopRequested && !holding)
            {
                float distance = (owner.ServiceStopX - transform.position.x) * direction;
                if (distance > 0f && distance < 6f)
                {
                    target = Mathf.Lerp(0f, speed, Mathf.Clamp01(distance / 6f));
                    if (distance < 0.4f)
                    {
                        holding = true;
                        stopHold = 2.5f;
                    }
                }
            }

            if (holding)
            {
                stopHold -= deltaTime;
                target = 0f;
                if (stopHold <= 0f) holding = false;
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, target, deltaTime * 9f);
            transform.position += new Vector3(direction * currentSpeed * deltaTime, 0f, 0f);

            return direction > 0
                ? transform.position.x > endX
                : transform.position.x < endX;
        }
    }
}
