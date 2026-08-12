using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ShawarmaTycoon
{
    /// <summary>Portrait isometric camera: keeps the player centered and supports pinch zoom.</summary>
    public sealed class MobileCameraRig : MonoBehaviour
    {
        [SerializeField] private float zoomSpeed = 0.02f;
        [SerializeField] private float scrollZoomSpeed = 0.006f;
        [SerializeField] private float followSpeed = 5.8f;
        [SerializeField] private float minZoom = 5.0f;
        [SerializeField] private float maxZoom = 10.0f;
        [SerializeField] private Vector3 cameraOffset = new(9f, 16.5f, -11f);
        [SerializeField, Min(0f)] private float lookAtHeight = 0.85f;
        // Bias the frame toward +Z so the street behind the kitchen stays in shot.
        [SerializeField] private float lookAheadZ = 2.6f;
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
            if (HandlePinchZoom())
                return;

            HandleScrollZoom();
            if (followTarget == null) return;
            ApplyPose(followTarget.position, false);
        }

        private void ApplyZoomDelta(float delta)
        {
            targetCamera.orthographicSize = Mathf.Clamp(
                targetCamera.orthographicSize - delta, minZoom, maxZoom);
        }

        private void ApplyPose(Vector3 targetPosition, bool immediate)
        {
            Vector3 groundFocus = targetPosition;
            groundFocus.x = Mathf.Clamp(groundFocus.x, xBounds.x, xBounds.y);
            groundFocus.z = Mathf.Clamp(groundFocus.z, zBounds.x, zBounds.y) + lookAheadZ;
            groundFocus.y = 0f;

            Vector3 desiredPosition = groundFocus + cameraOffset;
            targetCamera.transform.position = immediate
                ? desiredPosition
                : Vector3.Lerp(targetCamera.transform.position, desiredPosition, followSpeed * Time.deltaTime);
            targetCamera.transform.LookAt(groundFocus + Vector3.up * lookAtHeight);
        }

        /// <summary>
        /// Two finger pinch. The project runs on the Input System package only,
        /// so the legacy UnityEngine.Input touch API reports nothing here.
        /// </summary>
        private bool HandlePinchZoom()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null)
            {
                lastDistance = 0f;
                return false;
            }

            TouchControl first = null;
            TouchControl second = null;
            foreach (TouchControl touch in screen.touches)
            {
                if (!touch.press.isPressed) continue;
                if (first == null) first = touch;
                else if (second == null) { second = touch; break; }
            }

            if (first == null || second == null)
            {
                lastDistance = 0f;
                return false;
            }

            float distance = Vector2.Distance(
                first.position.ReadValue(), second.position.ReadValue());

            if (lastDistance > 0.01f)
                ApplyZoomDelta((distance - lastDistance) * zoomSpeed);
            lastDistance = distance;

            if (followTarget != null) ApplyPose(followTarget.position, true);
            return true;
        }

        /// <summary>Mouse wheel zoom so the same feel is reachable in the editor.</summary>
        private void HandleScrollZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            ApplyZoomDelta(scroll * scrollZoomSpeed);
        }
    }
}
