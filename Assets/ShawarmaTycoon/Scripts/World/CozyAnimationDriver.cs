using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Drives the authored Idle, Walk and CarryWalk clips from actual wrapper
    /// movement. It works for both the player and directly moved customers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CozyAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField, Min(0.001f)] private float movementThreshold = 0.045f;
        [SerializeField, Min(0f)] private float transitionSeconds = 0.12f;

        private CarryInventory inventory;
        private Vector3 previousPosition;
        private string activeState;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            inventory = GetComponentInParent<CarryInventory>();
            previousPosition = transform.position;

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.updateMode = AnimatorUpdateMode.Normal;
            }
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
            activeState = null;
        }

        private void LateUpdate()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            Vector3 displacement = transform.position - previousPosition;
            displacement.y = 0f;
            previousPosition = transform.position;

            float speed = Time.deltaTime > 0.0001f
                ? displacement.magnitude / Time.deltaTime
                : 0f;
            bool moving = speed >= movementThreshold;
            bool carrying = inventory != null && inventory.Count > 0;
            string desiredState = moving
                ? carrying ? "CarryWalk" : "Walk"
                : "Idle";

            if (activeState == desiredState)
                return;

            activeState = desiredState;
            animator.CrossFadeInFixedTime(desiredState, transitionSeconds, 0);
        }
    }
}
