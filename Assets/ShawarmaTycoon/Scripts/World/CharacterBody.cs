using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Gives a scripted walker - a customer, a hired worker - a body the rest of
    /// the world is felt through. Without one they were pure transforms: the
    /// player walked through the queue, and the queue walked through the counters.
    ///
    /// Walkers are put on their own layer with self-collision switched off. That
    /// is deliberate rather than a shortcut: they are driven to fixed slots by
    /// script, not by steering, so a customer blocked by the person already
    /// standing in their queue slot would stop short, never register as arrived,
    /// and hold up everyone behind them. Ignoring each other costs nothing to look
    /// at - the queue stands a metre apart - and cannot deadlock. What matters is
    /// that they collide with the shop and with the player, and they still do.
    /// </summary>
    public static class CharacterBody
    {
        public const string LayerName = "Character";

        private static int cachedLayer = -1;
        private static bool matrixApplied;

        /// <summary>
        /// Fits a capsule to a walker and returns it. The models stand on a
        /// bottom-centre pivot, so the capsule is centred at half its own height.
        /// </summary>
        public static CharacterController Attach(
            GameObject walker, float height = 1.68f, float radius = 0.25f)
        {
            if (walker == null) return null;

            CharacterController controller = walker.GetComponent<CharacterController>();
            if (controller == null) controller = walker.AddComponent<CharacterController>();
            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            controller.slopeLimit = 60f;
            controller.stepOffset = 0.3f;
            // Tight: at Unity's default the capsule stands a visible step away from
            // whatever it is pressed against.
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            ApplyLayer(walker);
            return controller;
        }

        /// <summary>
        /// Walks a body toward a point on the level, sliding along whatever it
        /// meets. Height is held rather than driven: the pavement outside the shop
        /// is laid level with the floor but its collider sits a hand lower, and
        /// under gravity everyone who stepped outside would sink into it.
        /// </summary>
        public static void StepTowards(
            CharacterController controller, Vector3 target, float speed, float deltaTime)
        {
            if (controller == null) return;
            Vector3 here = controller.transform.position;
            target.y = here.y;
            Vector3 step = Vector3.MoveTowards(here, target, speed * deltaTime) - here;
            if (step.sqrMagnitude > 0f) controller.Move(step);
        }

        private static void ApplyLayer(GameObject walker)
        {
            if (cachedLayer < 0) cachedLayer = LayerMask.NameToLayer(LayerName);
            if (cachedLayer < 0) return;

            walker.layer = cachedLayer;
            if (matrixApplied) return;
            matrixApplied = true;
            Physics.IgnoreLayerCollision(cachedLayer, cachedLayer, true);
        }

        /// <summary>
        /// Play mode starts on a fresh physics matrix, so the pairing has to be put
        /// back each session rather than saved into the project's settings.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            cachedLayer = -1;
            matrixApplied = false;
        }
    }
}
