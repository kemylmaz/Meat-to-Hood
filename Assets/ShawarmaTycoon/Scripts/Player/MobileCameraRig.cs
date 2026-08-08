using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Portrait isometric camera: follows the player softly while retaining two-finger pan and zoom.</summary>
    public sealed class MobileCameraRig : MonoBehaviour
    {
        [SerializeField] private float dragSpeed = 0.012f;
        [SerializeField] private float zoomSpeed = 0.02f;
        [SerializeField] private float followSpeed = 4.8f;
        [SerializeField] private float minZoom = 6.4f;
        [SerializeField] private float maxZoom = 11.5f;
        [SerializeField] private Vector2 xBounds = new(-9f, 19f);
        [SerializeField] private Vector2 zBounds = new(-9f, 10f);

        private Camera targetCamera;
        private Transform followTarget;
        private Vector2 lastCenter;
        private float lastDistance;
        private Vector3 lookOffset;

        public void Configure(Camera camera)
        {
            targetCamera = camera;
            if (targetCamera == null) return;
            targetCamera.orthographic = true;
            lookOffset = targetCamera.transform.position - new Vector3(0.8f, 0f, -0.6f);
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void SetFollowBounds(Vector2 xRange, Vector2 zRange)
        {
            xBounds = xRange;
            zBounds = zRange;
        }

        private void Update()
        {
            if (targetCamera == null) return;
            if (Input.touchCount >= 2)
            {
                HandleTwoFingerInput();
                return;
            }

            if (followTarget == null) return;
            Vector3 focus = followTarget.position;
            focus.x = Mathf.Clamp(focus.x, xBounds.x, xBounds.y);
            focus.z = Mathf.Clamp(focus.z, zBounds.x, zBounds.y);
            focus.y = 0f;
            Vector3 desiredPosition = focus + lookOffset;
            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, desiredPosition, followSpeed * Time.deltaTime);
            Vector3 lookPoint = new(targetCamera.transform.position.x - lookOffset.x, 0f, targetCamera.transform.position.z - lookOffset.z);
            targetCamera.transform.LookAt(lookPoint);
        }

        private void HandleTwoFingerInput()
        {
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
            Vector3 movement = new(-delta.x * dragSpeed, 0f, -delta.y * dragSpeed);
            Vector3 position = targetCamera.transform.position + movement;
            position.x = Mathf.Clamp(position.x, xBounds.x + lookOffset.x, xBounds.y + lookOffset.x);
            position.z = Mathf.Clamp(position.z, zBounds.x + lookOffset.z, zBounds.y + lookOffset.z);
            targetCamera.transform.position = position;

            if (lastDistance > 0.01f)
                targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize - (distance - lastDistance) * zoomSpeed, minZoom, maxZoom);

            lastCenter = center;
            lastDistance = distance;
        }
    }
}
