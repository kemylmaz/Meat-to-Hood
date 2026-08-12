using ShawarmaTycoon.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShawarmaTycoon
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MobilePlayerController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(1f)] private float rotationSpeed = 14f;

        private CharacterController characterController;
        private TouchJoystick joystick;
        private float verticalVelocity;
        private Vector2 minBounds = new(-8.4f, -6.4f);
        private Vector2 maxBounds = new(8.4f, 6.4f);
        private float baseMoveSpeed;
        private Vector3 safePosition;

        public Vector2 JoystickValue => joystick != null ? joystick.Value : Vector2.zero;

        /// <summary>The on-screen stick now lives in the uGUI HUD, not in OnGUI.</summary>
        public void SetJoystick(TouchJoystick stick) => joystick = stick;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            safePosition = transform.position;
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
            Vector2 input = ReadKeyboardInput();
            if (input.sqrMagnitude < 0.01f) input = JoystickValue;

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

            if (characterController.isGrounded)
                safePosition = transform.position;
            else if (transform.position.y < -2f)
                RecoverFromFall();

            if (direction.sqrMagnitude > 0.01f)
            {
                // Meshy character models face their local -Z axis.
                Quaternion targetRotation = Quaternion.LookRotation(-direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void RecoverFromFall()
        {
            characterController.enabled = false;
            transform.position = new Vector3(safePosition.x, 0.26f, safePosition.z);
            characterController.enabled = true;
            verticalVelocity = -1f;
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

    }
}
