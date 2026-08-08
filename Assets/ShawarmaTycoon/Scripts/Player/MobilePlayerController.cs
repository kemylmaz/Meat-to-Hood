using UnityEngine;
using UnityEngine.InputSystem;

namespace ShawarmaTycoon
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MobilePlayerController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(1f)] private float rotationSpeed = 14f;
        [SerializeField, Min(20f)] private float joystickRadius = 90f;
        [SerializeField, Min(0f)] private float inputDeadZone = 0.08f;

        private CharacterController characterController;
        private Vector2 pointerOrigin;
        private Vector2 joystickValue;
        private bool pointerActive;
        private float verticalVelocity;
        private Vector2 minBounds = new(-8.4f, -6.4f);
        private Vector2 maxBounds = new(8.4f, 6.4f);
        private float baseMoveSpeed;

        public Vector2 JoystickValue => joystickValue;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public void Configure(float speed, Vector2 minimumBounds, Vector2 maximumBounds)
        {
            moveSpeed = Mathf.Max(0.1f, speed);
            baseMoveSpeed = moveSpeed;
            minBounds = minimumBounds;
            maxBounds = maximumBounds;
        }

        public void ExpandMaximumX(float maximumX)
        {
            maxBounds.x = Mathf.Max(maxBounds.x, maximumX);
        }

        public void SetUpgradeLevel(int level)
        {
            if (baseMoveSpeed <= 0f) baseMoveSpeed = moveSpeed;
            moveSpeed = baseMoveSpeed + Mathf.Max(0, level) * 0.55f;
        }

        private void Update()
        {
            ReadPointerInput();
            Vector2 input = ReadKeyboardInput();
            if (input.sqrMagnitude < 0.01f) input = joystickValue;

            Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 direction = Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
            Vector3 horizontal = direction * (moveSpeed * Time.deltaTime);

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -1f;
            else
                verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 desired = transform.position + horizontal;
            desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
            desired.z = Mathf.Clamp(desired.z, minBounds.y, maxBounds.y);
            Vector3 constrainedHorizontal = new(desired.x - transform.position.x, 0f, desired.z - transform.position.z);

            characterController.Move(constrainedHorizontal + Vector3.up * (verticalVelocity * Time.deltaTime));

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void ReadPointerInput()
        {
            Pointer pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.isPressed;
            Vector2 position = pointer != null ? pointer.position.ReadValue() : Vector2.zero;

            if (pressed && !pointerActive)
            {
                pointerActive = true;
                pointerOrigin = position;
            }

            if (!pressed)
            {
                pointerActive = false;
                joystickValue = Vector2.zero;
                return;
            }

            Vector2 delta = Vector2.ClampMagnitude(position - pointerOrigin, joystickRadius);
            joystickValue = delta / joystickRadius;
            if (joystickValue.magnitude < inputDeadZone) joystickValue = Vector2.zero;
        }

        private static Vector2 ReadKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            float x = 0f;
            float y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private void OnGUI()
        {
            if (!pointerActive) return;

            float scale = Mathf.Max(0.75f, Screen.dpi > 0f ? Screen.dpi / 180f : 1f);
            float radius = joystickRadius * scale;
            Vector2 origin = new(pointerOrigin.x, Screen.height - pointerOrigin.y);
            Vector2 knob = origin + new Vector2(joystickValue.x, -joystickValue.y) * radius;

            Color oldColor = GUI.color;
            GUI.color = new Color(0.20f, 0.12f, 0.10f, 0.22f);
            GUI.DrawTexture(new Rect(origin.x - radius, origin.y - radius, radius * 2f, radius * 2f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.88f, 0.65f, 0.65f);
            GUI.DrawTexture(new Rect(knob.x - radius * 0.32f, knob.y - radius * 0.32f, radius * 0.64f, radius * 0.64f), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
