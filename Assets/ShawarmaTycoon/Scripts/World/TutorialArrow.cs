using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class TutorialArrow : MonoBehaviour
    {
        private Transform target;

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
            gameObject.SetActive(target != null);
        }

        private void Update()
        {
            if (target == null) return;
            transform.position = target.position + Vector3.up *
                (2.65f + Mathf.Sin(Time.unscaledTime * 5f) * 0.18f);
            transform.Rotate(0f, 120f * Time.unscaledDeltaTime, 0f, Space.World);
        }
    }
}
