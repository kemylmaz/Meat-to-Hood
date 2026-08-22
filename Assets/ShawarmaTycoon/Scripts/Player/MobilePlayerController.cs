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
        private DioramaWalkableRegistry walkableRegistry;
        private float edgeSafetyRadius = 0.28f;
        private float baseMoveSpeed;
        private Vector3 safePosition;
        private bool buildModeMovement;
        /// <summary>
        /// Largest drop the player will step down; anything deeper is a ledge.
        /// The lot and the plots stand 25 cm over the pavement and everything the
        /// player is meant to walk on is level with them, so this sits under that
        /// step: the kerb becomes the edge of the premises.
        /// </summary>
        [SerializeField, Min(0.05f)] private float maxStepDown = 0.15f;

        public Vector2 JoystickValue => joystick != null ? joystick.Value : Vector2.zero;
        public bool IsBuildModeMovement => buildModeMovement;
        public DioramaWalkableRegistry WalkableRegistry => walkableRegistry;
        public Bounds MovementBounds => walkableRegistry != null
            ? walkableRegistry.ActiveBounds
            : new Bounds(
                new Vector3((minBounds.x + maxBounds.x) * 0.5f, 0f,
                    (minBounds.y + maxBounds.y) * 0.5f),
                new Vector3(maxBounds.x - minBounds.x, 0f, maxBounds.y - minBounds.y));

        /// <summary>The on-screen stick now lives in the uGUI HUD, not in OnGUI.</summary>
        public void SetJoystick(TouchJoystick stick) => joystick = stick;

        /// <summary>
        /// Build mode pauses the restaurant clock, so the player alone advances
        /// with unscaled time while the CharacterController keeps normal walls
        /// and walkable-surface constraints.
        /// </summary>
        public void SetBuildModeMovement(bool active)
        {
            buildModeMovement = active;
            if (active) verticalVelocity = -1f;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            safePosition = transform.position;
        }

        public void Configure(float speed, Vector2 minimumBounds, Vector2 maximumBounds)
        {
            walkableRegistry = null;
            moveSpeed = Mathf.Max(0.1f, speed);
            baseMoveSpeed = moveSpeed;
            minBounds = minimumBounds;
            maxBounds = maximumBounds;
        }

        public void Configure(
            float speed,
            DioramaWalkableRegistry registry,
            float safetyRadius = 0.28f)
        {
            walkableRegistry = registry;
            edgeSafetyRadius = Mathf.Max(0.05f, safetyRadius);
            moveSpeed = Mathf.Max(0.1f, speed);
            baseMoveSpeed = moveSpeed;
            safePosition = transform.position;
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
            float deltaTime = buildModeMovement ? Time.unscaledDeltaTime : Time.deltaTime;
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
            Vector3 horizontal = direction * (moveSpeed * deltaTime);

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -1f;
            else
                verticalVelocity += Physics.gravity.y * deltaTime;

            Vector3 desired = transform.position + horizontal;
            if (walkableRegistry == null)
            {
                desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
                desired.z = Mathf.Clamp(desired.z, minBounds.y, maxBounds.y);
            }
            Vector3 constrainedHorizontal = ResolveFooting(
                new Vector3(desired.x - transform.position.x, 0f, desired.z - transform.position.z));

            characterController.Move(constrainedHorizontal + Vector3.up * (verticalVelocity * deltaTime));

            if (characterController.isGrounded &&
                (walkableRegistry == null ||
                 walkableRegistry.ContainsFootprint(transform.position, FootprintRadius)))
                safePosition = transform.position;
            else if (transform.position.y < -2f)
                RecoverFromFall();

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
        }

        /// <summary>
        /// Refuses a step that would walk off a ledge, sliding along the edge
        /// rather than sticking to it. The lot and the expansion plots stand
        /// 25 cm above the city pavement while the walk bounds are one rectangle,
        /// so past the ends of the plots that rectangle ran out over open ground.
        /// Probing the floor keeps the player on it whatever the layout does next.
        /// </summary>
        private Vector3 ResolveFooting(Vector3 step)
        {
            if (walkableRegistry != null)
            {
                if (step.sqrMagnitude < 1e-6f ||
                    walkableRegistry.ContainsFootprint(transform.position + step, FootprintRadius))
                    return step;

                Vector3 registryX = new(step.x, 0f, 0f);
                if (registryX.sqrMagnitude > 1e-6f &&
                    walkableRegistry.ContainsFootprint(transform.position + registryX, FootprintRadius))
                    return registryX;

                Vector3 registryZ = new(0f, 0f, step.z);
                if (registryZ.sqrMagnitude > 1e-6f &&
                    walkableRegistry.ContainsFootprint(transform.position + registryZ, FootprintRadius))
                    return registryZ;

                return Vector3.zero;
            }

            if (step.sqrMagnitude < 1e-6f || HasFooting(transform.position + step))
                return step;

            Vector3 alongX = new(step.x, 0f, 0f);
            if (alongX.sqrMagnitude > 1e-6f && HasFooting(transform.position + alongX))
                return alongX;

            Vector3 alongZ = new(0f, 0f, step.z);
            if (alongZ.sqrMagnitude > 1e-6f && HasFooting(transform.position + alongZ))
                return alongZ;

            return Vector3.zero;
        }

        private float FootprintRadius => Mathf.Max(
            edgeSafetyRadius,
            characterController != null ? characterController.radius * 0.82f : 0.28f);

        private bool HasFooting(Vector3 position)
        {
            float there = GroundHeight(position);
            if (there <= NoGround) return false;
            float here = GroundHeight(transform.position);
            return here <= NoGround || there >= here - maxStepDown;
        }

        private const float NoGround = -900f;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        /// <summary>
        /// Height of the floor under <paramref name="position"/>. The probe starts
        /// above head height and so passes straight through the player's own
        /// capsule on the way down; that hit has to be discarded or every probe
        /// reports the player standing on themselves and nothing can move.
        /// </summary>
        private float GroundHeight(Vector3 position)
        {
            if (walkableRegistry != null)
                return walkableRegistry.TryGetGroundHeight(position, out float registeredHeight)
                    ? registeredHeight
                    : NoGround;

            int count = Physics.RaycastNonAlloc(position + Vector3.up * 2.5f, Vector3.down,
                groundHits, 8f, ~0, QueryTriggerInteraction.Ignore);

            float best = NoGround;
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i].collider.transform.IsChildOf(transform)) continue;
                if (groundHits[i].point.y > best) best = groundHits[i].point.y;
            }
            return best;
        }

        private void RecoverFromFall()
        {
            // Land on whatever the last safe spot actually stands on rather than a
            // fixed height, so a rescue onto the raised lot does not drop the
            // player through it.
            float ground = GroundHeight(safePosition);
            float y = ground <= NoGround ? safePosition.y : ground + 0.05f;
            characterController.enabled = false;
            transform.position = new Vector3(safePosition.x, y, safePosition.z);
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
