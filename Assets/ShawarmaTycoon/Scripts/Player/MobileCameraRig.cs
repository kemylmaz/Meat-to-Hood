using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class MobileCameraRig : MonoBehaviour
    {
        [SerializeField] private float dragSpeed = 0.012f;
        [SerializeField] private float zoomSpeed = 0.02f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 22f;
        [SerializeField] private Vector2 xBounds = new(-6f, 6f);
        [SerializeField] private Vector2 zBounds = new(-4f, 5f);

        private Camera targetCamera;
        private Vector2 lastCenter;
        private float lastDistance;

        public void Configure(Camera camera)
        {
            targetCamera = camera;
            if (targetCamera != null) targetCamera.orthographic = true;
        }

        private void Update()
        {
            if (targetCamera == null || Input.touchCount < 2) return;

            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);
            Vector2 center = (a.position + b.position) * 0.5f;
            float distance = Vector2.Distance(a.position, b.position);

            if (a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
            {
                lastCenter = center;
                lastDistance = distance;
                return;
            }

            Vector2 delta = center - lastCenter;
            Vector3 movement = new Vector3(-delta.x * dragSpeed, 0f, -delta.y * dragSpeed);
            Vector3 position = targetCamera.transform.position + movement;
            position.x = Mathf.Clamp(position.x, xBounds.x, xBounds.y);
            position.z = Mathf.Clamp(position.z, zBounds.x, zBounds.y);
            targetCamera.transform.position = position;

            if (lastDistance > 0.01f)
                targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize - (distance - lastDistance) * zoomSpeed, minZoom, maxZoom);

            lastCenter = center;
            lastDistance = distance;
        }
    }
}
