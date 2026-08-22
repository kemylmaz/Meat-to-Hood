using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Gives a locked purchase a small showroom turntable. The preview is built
    /// from the same authored model as the real unlock, so a pad explains itself
    /// without another label or a screen-covering tutorial card.
    /// </summary>
    public sealed class PurchasePreviewBob : MonoBehaviour
    {
        private Vector3 restPosition;
        private float phase;

        private void Awake()
        {
            restPosition = transform.localPosition;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            phase += Time.deltaTime * 1.8f;
            transform.localPosition = restPosition + Vector3.up * (Mathf.Sin(phase) * 0.055f);
            transform.Rotate(0f, 18f * Time.deltaTime, 0f, Space.Self);
        }
    }

    /// <summary>A visible banknote travelling from the player's wallet to a pad.</summary>
    public sealed class PaymentFlyer : MonoBehaviour
    {
        private Vector3 from;
        private Vector3 to;
        private float age;
        private float duration;
        private Transform bill;
        private Vector3 billScale;

        public static void Send(Vector3 start, Vector3 end)
        {
            if (!Application.isPlaying) return;

            GameObject root = new("Ödeme Banknotu");
            PaymentFlyer flyer = root.AddComponent<PaymentFlyer>();
            flyer.from = start + new Vector3(Random.Range(-0.12f, 0.12f), 0f, Random.Range(-0.12f, 0.12f));
            flyer.to = end;
            flyer.duration = Random.Range(0.28f, 0.38f);
            root.transform.position = flyer.from;

            GameObject note = PrototypeVisuals.CreatePrimitive(
                "Banknot", PrimitiveType.Cube, root.transform, Vector3.zero,
                new Vector3(0.28f, 0.025f, 0.17f), new Color(0.30f, 0.82f, 0.34f));
            flyer.bill = note.transform;
            flyer.billScale = note.transform.localScale;
            DisablePhysicsAndShadows(note);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            Vector3 position = Vector3.Lerp(from, to, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.72f;
            transform.position = position;
            if (bill != null)
            {
                bill.Rotate(180f * Time.deltaTime, 360f * Time.deltaTime, 70f * Time.deltaTime, Space.Self);
                float scale = Mathf.Sin(t * Mathf.PI);
                bill.localScale = billScale * Mathf.Lerp(0.72f, 1f, scale);
            }

            if (t >= 1f) Destroy(gameObject);
        }

        internal static void DisablePhysicsAndShadows(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }

    /// <summary>
    /// Shows the item that just moved instead of letting one stack decrement and
    /// another increment in the same frame. The inventory rules stay instant;
    /// this is only the short visual bridge between the two states.
    /// </summary>
    public sealed class ItemTransferArc : MonoBehaviour
    {
        private Vector3 from;
        private Vector3 to;
        private float age;
        private const float Duration = 0.24f;

        public static void Send(ItemType type, Vector3 start, Vector3 end)
        {
            if (!Application.isPlaying || type == ItemType.None) return;

            GameObject root = new(type + " Transfer");
            ItemTransferArc arc = root.AddComponent<ItemTransferArc>();
            arc.from = start;
            arc.to = end;
            root.transform.position = start;
            // The arc is the only frame where the player can see what actually
            // moved between stations, so it must not shrink an already small food
            // model. A slight presentation boost makes the hand-off legible.
            GameObject visual = PrototypeVisuals.CreateItemVisual(type, root.transform, Vector3.zero, 1.08f);
            PaymentFlyer.DisablePhysicsAndShadows(visual);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Duration);
            transform.position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.42f);
            transform.Rotate(0f, 420f * Time.deltaTime, 0f, Space.Self);
            float scale = 1f - Mathf.Clamp01((t - 0.76f) / 0.24f);
            transform.localScale = Vector3.one * Mathf.Max(0.08f, scale);
            if (t >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>A tiny squash-and-settle response shared by carried stacks.</summary>
    public sealed class FeedbackPulse : MonoBehaviour
    {
        private float age = 1f;
        private const float Duration = 0.22f;

        public void Kick() => age = 0f;

        private void Update()
        {
            if (age >= Duration) return;
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Duration);
            float wave = Mathf.Sin(t * Mathf.PI);
            transform.localScale = new Vector3(1f - wave * 0.06f, 1f + wave * 0.13f, 1f - wave * 0.06f);
            if (t >= 1f) transform.localScale = Vector3.one;
        }
    }

    /// <summary>A warm radial pop reserved for completed unlocks.</summary>
    public sealed class UnlockCelebration : MonoBehaviour
    {
        private sealed class Spark
        {
            public Transform Transform;
            public Vector3 Velocity;
        }

        private readonly List<Spark> sparks = new();
        private float life = 0.85f;

        public static void Spawn(Vector3 position)
        {
            if (!Application.isPlaying) return;

            GameObject root = new("Kilidi Açma Kutlaması");
            root.transform.position = position;
            UnlockCelebration burst = root.AddComponent<UnlockCelebration>();
            Color[] palette = { new(1f, 0.78f, 0.20f), new(0.30f, 0.83f, 0.50f), new(1f, 0.48f, 0.34f) };
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                GameObject spark = PrototypeVisuals.CreatePrimitive(
                    "Konfeti", i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    root.transform, Vector3.up * 0.15f, Vector3.one * (i % 3 == 0 ? 0.13f : 0.10f),
                    palette[i % palette.Length]);
                PaymentFlyer.DisablePhysicsAndShadows(spark);
                burst.sparks.Add(new Spark
                {
                    Transform = spark.transform,
                    Velocity = new Vector3(Mathf.Cos(angle) * 2.1f, 2.2f + (i % 3) * 0.25f, Mathf.Sin(angle) * 2.1f)
                });
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            for (int i = 0; i < sparks.Count; i++)
            {
                Spark spark = sparks[i];
                spark.Transform.localPosition += spark.Velocity * dt;
                spark.Velocity += Vector3.down * (4.6f * dt);
                spark.Transform.Rotate(320f * dt, 220f * dt, 150f * dt, Space.Self);
                spark.Transform.localScale = Vector3.one * Mathf.Clamp01(life * 2.2f) * 0.12f;
            }
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
