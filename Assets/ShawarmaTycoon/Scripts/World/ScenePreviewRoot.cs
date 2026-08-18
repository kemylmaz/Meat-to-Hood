using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Marks a world built into the Scene view for hand-placing props against,
    /// rather than one built for a live session.
    ///
    /// The editor tears a preview down before play starts, so this is the backstop
    /// for the case where one reaches a running game anyway - a build made from a
    /// scene somebody saved by hand at the wrong moment, say. A preview is a static
    /// copy of the world with none of the wiring, so left standing it would draw a
    /// second dead shop through the real one.
    ///
    /// The execution order is what makes the backstop useful: the bootstrap runs at
    /// -1000, this runs before it and is gone by the time the bootstrap looks.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    [DisallowMultipleComponent]
    public sealed class ScenePreviewRoot : MonoBehaviour
    {
        private void Awake()
        {
            DestroyImmediate(gameObject);
        }
    }
}
