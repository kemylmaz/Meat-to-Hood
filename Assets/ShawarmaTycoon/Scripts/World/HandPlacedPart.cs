using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Stamped on a generated group that somebody has taken over by hand, saying
    /// which part of the world it is.
    ///
    /// This is what stops the builder laying its own copy over the top, and it is
    /// stamped automatically when a group is kept out of a preview. Leaving that to
    /// a checkbox somebody had to remember meant forgetting it produced two
    /// skylines standing in each other, which is a hard thing to read as a missing
    /// tick rather than a broken tool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HandPlacedPart : MonoBehaviour
    {
        [SerializeField] private HandPlacedWorld.Parts part;

        public HandPlacedWorld.Parts Part => part;

        public void Configure(HandPlacedWorld.Parts value) => part = value;
    }
}
