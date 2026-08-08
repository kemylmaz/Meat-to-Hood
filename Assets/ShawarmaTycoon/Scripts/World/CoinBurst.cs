using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Large, readable cash feedback with a small burst of visible banknotes.</summary>
    public sealed class CoinBurst : MonoBehaviour
    {
        private float life = 1.8f;
        private Vector3 velocity;
        private Transform visualsRoot;

        public static void Spawn(Vector3 position, int value) => Create(position, value, true);
        public static void SpawnGain(Vector3 position, int value) => Create(position, value, false);

        private static void Create(Vector3 position, int value, bool expense)
        {
            GameObject root = new(expense ? "Spent Cash Feedback" : "Cash Collected Feedback");
            root.transform.position = position;
            CoinBurst burst = root.AddComponent<CoinBurst>();
            burst.velocity = new Vector3(Random.Range(-0.35f, 0.35f), 2.25f, Random.Range(-0.18f, 0.18f));

            burst.visualsRoot = new GameObject("Bills").transform;
            burst.visualsRoot.SetParent(root.transform, false);
            Color billColor = expense ? new Color(0.92f, 0.35f, 0.27f) : new Color(0.22f, 0.88f, 0.30f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 offset = new(Mathf.Cos(angle) * 0.26f, i * 0.025f, Mathf.Sin(angle) * 0.17f);
                PrototypeVisuals.CreatePrimitive("Banknote", PrimitiveType.Cube, burst.visualsRoot, offset,
                    new Vector3(0.32f, 0.03f, 0.19f), billColor, new Vector3(0f, i * 23f, 0f));
            }

            TextMesh label = PrototypeVisuals.CreateLabel((expense ? "-" : "+") + "$" + value, root.transform, Vector3.up * 0.60f, 0.28f);
            label.color = expense ? new Color(0.90f, 0.17f, 0.14f) : new Color(0.08f, 0.70f, 0.20f);
            label.fontStyle = FontStyle.Bold;
        }

        private void Update()
        {
            life -= Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            velocity += Vector3.up * -1.7f * Time.deltaTime;
            if (visualsRoot != null) visualsRoot.Rotate(0f, 260f * Time.deltaTime, 0f, Space.Self);
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
