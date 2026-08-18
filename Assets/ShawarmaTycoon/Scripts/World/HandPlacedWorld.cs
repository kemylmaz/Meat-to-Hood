using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Says which generated parts of the world somebody has taken over by hand.
    ///
    /// Moving a generated group out of a preview and into the saved scene is only
    /// half the job: the builder would still lay its own copy down on top at run
    /// time, and the shop would have two skylines a metre apart. Ticking the part
    /// here is what stops that, so the hand-placed one is the only one.
    ///
    /// It lives on the scene's hand-placed root and is read once per build.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HandPlacedWorld : MonoBehaviour
    {
        [Flags]
        public enum Parts
        {
            None = 0,

            /// <summary>Facades, water towers, flanking blocks, street lamps and parked cars.</summary>
            Skyline = 1 << 0,

            /// <summary>Benches, bushes, hydrants, bins, crates and traffic lights.</summary>
            StreetProps = 1 << 1
        }

        [SerializeField, Tooltip(
            "Parts of the generated world this scene provides by hand, on top of " +
            "whatever the kept groups already declare for themselves. Only needed " +
            "for a part built from scratch rather than kept out of a preview.")]
        private Parts replaces = Parts.None;

        public Parts Replaces => replaces;

        /// <summary>
        /// What the open scene has taken over: the groups kept out of a preview,
        /// which say so themselves, plus anything declared by hand. Nothing
        /// hand-placed is the normal case, so a scene without any of this behaves
        /// exactly as it always did.
        /// </summary>
        public static Parts Replaced
        {
            get
            {
                Parts replaced = Parts.None;
                foreach (HandPlacedPart part in FindObjectsByType<HandPlacedPart>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    replaced |= part.Part;
                foreach (HandPlacedWorld marker in FindObjectsByType<HandPlacedWorld>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    replaced |= marker.replaces;
                return replaced;
            }
        }
    }
}
