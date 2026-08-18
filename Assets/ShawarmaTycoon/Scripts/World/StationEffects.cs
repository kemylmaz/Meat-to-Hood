using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Turns something at a steady rate. The spit is the obvious one: a doner
    /// kebab that does not turn reads as a plastic prop, and the whole kitchen
    /// was static geometry.
    /// </summary>
    public sealed class SpinningPart : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 55f;
        [SerializeField] private Vector3 axis = Vector3.up;

        public void Configure(float speed, Vector3 spinAxis)
        {
            degreesPerSecond = speed;
            axis = spinAxis.sqrMagnitude < 0.001f ? Vector3.up : spinAxis.normalized;
        }

        private void Update() => transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }

    /// <summary>
    /// Puffs of smoke that rise, spread and fade out. Built from primitives and
    /// pooled by hand rather than through a particle system, so it costs nothing
    /// to ship and needs no material setup.
    /// </summary>
    public sealed class SmokeStack : MonoBehaviour
    {
        private sealed class Puff
        {
            public Transform Transform;
            public Renderer Renderer;
            public float Age;
        }

        [SerializeField, Min(0.1f)] private float interval = 0.85f;
        [SerializeField, Min(0.2f)] private float lifetime = 2.4f;
        [SerializeField, Min(0.05f)] private float riseSpeed = 0.55f;
        [SerializeField] private float startScale = 0.16f;
        [SerializeField] private float endScale = 0.52f;

        private readonly List<Puff> puffs = new();
        private Color tint = new(0.86f, 0.86f, 0.88f);
        private float timer;

        /// <summary>Whether the thing this sits on is actually running.</summary>
        public System.Func<bool> IsActive { get; set; }

        public static SmokeStack Attach(
            Transform parent, Vector3 localPosition, Color smokeTint, float puffInterval = 0.85f)
        {
            GameObject root = new("Duman");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            SmokeStack stack = root.AddComponent<SmokeStack>();
            stack.tint = smokeTint;
            stack.interval = puffInterval;
            return stack;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                Puff puff = puffs[i];
                puff.Age += dt;
                float t = puff.Age / lifetime;
                if (t >= 1f || puff.Transform == null)
                {
                    if (puff.Transform != null) puff.Transform.gameObject.SetActive(false);
                    puffs.RemoveAt(i);
                    continue;
                }

                puff.Transform.localPosition += Vector3.up * (riseSpeed * dt);
                float scale = Mathf.Lerp(startScale, endScale, t);
                puff.Transform.localScale = new Vector3(scale, scale * 0.8f, scale);
                // Fading by colour rather than alpha: the palette material is
                // opaque, so a puff thins out by washing toward the sky instead.
                if (puff.Renderer != null)
                    puff.Renderer.sharedMaterial = PrototypeVisuals.Material(
                        Color.Lerp(tint, new Color(0.80f, 0.89f, 0.94f), t));
            }

            if (IsActive != null && !IsActive()) return;
            timer -= dt;
            if (timer > 0f) return;
            timer = interval;
            Emit();
        }

        private void Emit()
        {
            GameObject puffObject = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.gameObject.activeSelf) continue;
                puffObject = child.gameObject;
                break;
            }

            if (puffObject == null)
                puffObject = PrototypeVisuals.CreatePrimitive(
                    "Puf", PrimitiveType.Sphere, transform, Vector3.zero,
                    Vector3.one * startScale, tint);

            puffObject.SetActive(true);
            puffObject.transform.localPosition = new Vector3(
                Random.Range(-0.07f, 0.07f), 0f, Random.Range(-0.07f, 0.07f));
            Renderer renderer = puffObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            Collider collider = puffObject.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            puffs.Add(new Puff { Transform = puffObject.transform, Renderer = renderer, Age = 0f });
        }
    }

    /// <summary>
    /// The hot glow under a grill, breathing between two colours. Cheap, and it
    /// gives the oven a pulse without any lighting work.
    /// </summary>
    public sealed class HeatGlow : MonoBehaviour
    {
        private Renderer target;
        private Color cool = new(0.72f, 0.24f, 0.12f);
        private Color hot = new(1f, 0.62f, 0.18f);
        private float phase;

        public System.Func<bool> IsActive { get; set; }

        public static HeatGlow Attach(Transform parent, Vector3 localPosition, Vector3 size)
        {
            GameObject glow = PrototypeVisuals.CreatePrimitive(
                "Kor", PrimitiveType.Cube, parent, localPosition, size,
                new Color(0.86f, 0.38f, 0.14f));
            HeatGlow heat = glow.AddComponent<HeatGlow>();
            heat.target = glow.GetComponent<Renderer>();
            if (heat.target != null)
            {
                heat.target.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                heat.target.receiveShadows = false;
            }
            return heat;
        }

        private void Update()
        {
            if (target == null) return;
            bool running = IsActive == null || IsActive();
            phase += Time.deltaTime * (running ? 2.6f : 0.5f);
            float t = running ? (Mathf.Sin(phase) + 1f) * 0.5f : 0f;
            target.sharedMaterial = PrototypeVisuals.Material(Color.Lerp(cool, hot, t));
        }
    }

    /// <summary>
    /// A machine leaning into its work: a small, quick rock while it is running,
    /// still when it is not.
    ///
    /// This is the fallback for the authored kitchen models, which each export as
    /// one merged mesh per LOD. Nothing inside them can be moved on its own - no
    /// spit, no blade, no door - so until they ship with those parts split out and
    /// named, the whole cabinet does the moving.
    /// </summary>
    public sealed class WorkingShake : MonoBehaviour
    {
        [SerializeField] private float degrees = 1.6f;
        [SerializeField] private float speed = 9f;

        private Vector3 restEuler;
        private float phase;

        public System.Func<bool> IsActive { get; set; }

        private void Awake() => restEuler = transform.localEulerAngles;

        private void Update()
        {
            bool working = IsActive == null || IsActive();
            if (!working)
            {
                phase = Mathf.MoveTowards(phase, 0f, Time.deltaTime * 6f);
                if (phase <= 0f) { transform.localEulerAngles = restEuler; return; }
            }
            else phase += Time.deltaTime * speed;

            transform.localEulerAngles = restEuler +
                new Vector3(0f, 0f, Mathf.Sin(phase) * degrees);
        }
    }

    /// <summary>
    /// A knife rocking on its board while there is something to carve. Small, but
    /// it is the difference between a carving station and a picture of one.
    /// </summary>
    public sealed class ChoppingKnife : MonoBehaviour
    {
        private Vector3 restEuler;
        private float phase;

        public System.Func<bool> IsActive { get; set; }

        private void Awake() => restEuler = transform.localEulerAngles;

        private void Update()
        {
            bool working = IsActive == null || IsActive();
            if (!working)
            {
                transform.localEulerAngles = restEuler;
                phase = 0f;
                return;
            }

            phase += Time.deltaTime * 7f;
            float swing = Mathf.Abs(Mathf.Sin(phase)) * 26f;
            transform.localEulerAngles = restEuler + new Vector3(0f, 0f, -swing);
        }
    }

    /// <summary>
    /// Snaps a freshly placed item up to size instead of having it blink into
    /// existence. Stacks change constantly, and without this the trays flicker
    /// rather than fill.
    /// </summary>
    public sealed class PopIn : MonoBehaviour
    {
        private const float Duration = 0.16f;

        private Vector3 target;
        private float age;

        public static void Play(GameObject visual, Vector3 finalScale)
        {
            if (visual == null) return;
            PopIn pop = visual.GetComponent<PopIn>() ?? visual.AddComponent<PopIn>();
            pop.target = finalScale;
            pop.age = 0f;
            pop.enabled = true;
            visual.transform.localScale = finalScale * 0.55f;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Duration);
            // A touch past full size and back, so it lands rather than grows.
            float scale = t < 1f ? Mathf.LerpUnclamped(0.55f, 1.06f, t) : 1f;
            transform.localScale = target * scale;
            if (t < 1f) return;
            transform.localScale = target;
            enabled = false;
        }
    }

    /// <summary>
    /// A tray riding a belt from one counter to the next.
    ///
    /// The belts moved their contents instantly and invisibly - a number left one
    /// station and appeared at another - so a running kitchen looked exactly like
    /// a stopped one. This is the same transfer, made visible.
    /// </summary>
    public sealed class BeltParcel : MonoBehaviour
    {
        private Vector3 from;
        private Vector3 to;
        private float duration;
        private float age;

        public static void Send(Transform belt, Vector3 start, Vector3 end, ItemType item, float seconds)
        {
            if (belt == null) return;
            GameObject parcel = GameplayObjectPool.Rent(
                $"belt.parcel.{item}", belt,
                () => PrototypeVisuals.CreateItemVisual(item, belt, Vector3.zero, 0.7f));
            if (parcel == null) return;

            foreach (Collider collider in parcel.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            BeltParcel rider = parcel.GetComponent<BeltParcel>() ?? parcel.AddComponent<BeltParcel>();
            rider.from = start;
            rider.to = end;
            rider.duration = Mathf.Max(0.12f, seconds);
            rider.age = 0f;
            parcel.transform.position = start;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            // A shallow arc, so it reads as riding over the belt rather than
            // sliding through it.
            Vector3 position = Vector3.Lerp(from, to, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.12f;
            transform.position = position;

            if (t < 1f) return;
            GameplayObjectPool.Release(gameObject);
        }
    }
}
