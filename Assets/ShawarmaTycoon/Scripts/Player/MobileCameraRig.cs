using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Portrait isometric camera: keeps the player centered and supports pinch zoom.</summary>
    public sealed class MobileCameraRig : MonoBehaviour
    {
        [SerializeField] private float zoomSpeed = 0.02f;
        [SerializeField] private float followSpeed = 5.8f;
        [SerializeField] private float minZoom = 4.4f;
        [SerializeField] private float maxZoom = 8.0f;
        [SerializeField] private Vector3 cameraOffset = new(9f, 16.5f, -11f);
        [SerializeField, Min(0f)] private float lookAtHeight = 0.85f;
        [SerializeField] private Vector2 xBounds = new(-9f, 19f);
        [SerializeField] private Vector2 zBounds = new(-9f, 10f);

        private Camera targetCamera;
        private Transform followTarget;
        private float lastDistance;

        public void Configure(Camera camera)
        {
            targetCamera = camera;
            if (targetCamera == null) return;
            targetCamera.orthographic = true;
            Vector3 initialPosition = new(0.8f, 0f, -0.6f);
            ApplyPose(initialPosition, true);
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            if (followTarget != null) ApplyPose(followTarget.position, true);
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
            ApplyPose(followTarget.position, false);
        }

        private void ApplyPose(Vector3 targetPosition, bool immediate)
        {
            Vector3 groundFocus = targetPosition;
            groundFocus.x = Mathf.Clamp(groundFocus.x, xBounds.x, xBounds.y);
            groundFocus.z = Mathf.Clamp(groundFocus.z, zBounds.x, zBounds.y);
            groundFocus.y = 0f;

            Vector3 desiredPosition = groundFocus + cameraOffset;
            targetCamera.transform.position = immediate
                ? desiredPosition
                : Vector3.Lerp(targetCamera.transform.position, desiredPosition, followSpeed * Time.deltaTime);
            targetCamera.transform.LookAt(groundFocus + Vector3.up * lookAtHeight);
        }

        private void HandleTwoFingerInput()
        {
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);
            float distance = Vector2.Distance(a.position, b.position);

            if (a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
            {
                lastDistance = distance;
                return;
            }

            if (lastDistance > 0.01f)
                targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize - (distance - lastDistance) * zoomSpeed, minZoom, maxZoom);

            lastDistance = distance;

            if (followTarget != null) ApplyPose(followTarget.position, true);
        }
    }
}
