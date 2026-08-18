using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShawarmaTycoon.EditorTools
{
    /// <summary>
    /// Puts the generated world into the Scene view so props can be placed against
    /// it by hand.
    ///
    /// The shop is assembled at runtime, so the saved scene holds a camera, a light
    /// and the bootstrap and nothing else - there is no floor to drop a chair onto
    /// and no counter to line it up with. This builds the same world the game
    /// builds, as ordinary editable objects.
    ///
    /// The preview is deliberately throwaway: it is torn down before the scene is
    /// saved and before play starts, so it can neither be committed by accident
    /// nor end up standing in the live world. What survives is whatever is placed
    /// under <see cref="HandPlacedRoot"/>, which is a normal saved object the
    /// runtime build leaves alone.
    /// </summary>
    [InitializeOnLoad]
    public static class ScenePreview
    {
        static ScenePreview()
        {
            // Taking the preview down here, rather than letting it be carried into
            // play, is what makes it safe. Marking it DontSaveInEditor instead did
            // keep it out of the scene file, but on entering play Unity detached
            // the hierarchy from the scene rather than dropping it: it stayed
            // alive, belonged to no scene so nothing ever called Awake on it, and
            // drew seven hundred renderers straight over the real shop.
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) Clear();
            };
            EditorSceneManager.sceneSaving += (_, __) => Clear();
        }

        private const string RuntimeRootName = "Shawarma Prototype Runtime";

        /// <summary>Where hand-placed props go, so a cleared preview does not take them.</summary>
        public const string HandPlacedRoot = "El Yerleşimi";

        // No key binding on purpose. Every combination worth having is already
        // Unity's - Ctrl+Shift+B is Build Profiles - and a menu item that fights an
        // editor shortcut pops a conflict dialog instead of running. Unity lists
        // menu items in Edit > Shortcuts under "Main Menu/", so a binding is one
        // the person picks rather than one shipped over the top of theirs.
        [MenuItem("Shawarma Tycoon/Scene/Build World Preview", priority = 10)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            ShawarmaPrototypeBootstrap bootstrap =
                Object.FindFirstObjectByType<ShawarmaPrototypeBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError(
                    "[ScenePreview] No ShawarmaPrototypeBootstrap in the open scene. " +
                    "Open Assets/ShawarmaTycoon/Scenes/ShawarmaTycoonPrototype.unity.");
                return;
            }

            Clear();
            bootstrap.BuildPrototype();

            GameObject root = GameObject.Find(RuntimeRootName);
            if (root == null)
            {
                Debug.LogError("[ScenePreview] The bootstrap built nothing.");
                return;
            }

            root.name = RuntimeRootName + " (Preview)";
            root.AddComponent<ScenePreviewRoot>();
            EnsureHandPlacedRoot();

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log(
                $"[ScenePreview] World built for editing. Put your own props under " +
                $"\"{HandPlacedRoot}\" - the preview itself is cleared when you save " +
                "or press play, and anything parented into it goes with it.");
        }

        /// <summary>
        /// The groups the builder produces that a scene can take over wholesale,
        /// keyed by the name the builder gives them.
        /// </summary>
        private static readonly (string Name, HandPlacedWorld.Parts Part)[] GeneratedGroups =
        {
            ("Skyline", HandPlacedWorld.Parts.Skyline),
            ("Street Props", HandPlacedWorld.Parts.StreetProps)
        };

        /// <summary>
        /// Lifts the selection out of the preview and into the saved scene, keeping
        /// where it is in the world.
        ///
        /// This is the only way an edit made to a preview survives: the preview is
        /// rebuilt from code every time and thrown away on save, so a building
        /// turned to face a better way is gone at the next Ctrl+S unless it is
        /// moved out first.
        ///
        /// Selecting inside a generated group keeps the whole group, not the one
        /// object picked. Half a group cannot be made to work - the builder makes
        /// its groups whole or not at all, so keeping three buildings out of twelve
        /// leaves you choosing between three duplicates and nine missing ones. The
        /// group is stamped with what it is on the way out, which is what tells the
        /// builder to stop producing its own.
        /// </summary>
        [MenuItem("Shawarma Tycoon/Scene/Keep Selection", priority = 12)]
        public static void KeepSelection()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("[ScenePreview] Nothing selected to keep.");
                return;
            }

            EnsureHandPlacedRoot();
            Transform target = GameObject.Find(HandPlacedRoot).transform;

            // Resolved to whole groups first, so picking two buildings out of one
            // skyline keeps that skyline once rather than twice.
            System.Collections.Generic.Dictionary<Transform, HandPlacedWorld.Parts> keep = new();
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null || go.transform == target || go.transform.IsChildOf(target)) continue;
                keep[ResolveGroup(go.transform, out HandPlacedWorld.Parts part)] = part;
            }

            System.Text.StringBuilder report = new();
            foreach (System.Collections.Generic.KeyValuePair<Transform, HandPlacedWorld.Parts> entry in keep)
            {
                Transform group = entry.Key;
                HandPlacedWorld.Parts part = entry.Value;
                Undo.SetTransformParent(group, target, "Keep in scene");
                // Dropped so a kept group is never mistaken for a preview and swept
                // up with the next one.
                foreach (ScenePreviewRoot marker in group.GetComponentsInChildren<ScenePreviewRoot>(true))
                    Undo.DestroyObjectImmediate(marker);

                if (part == HandPlacedWorld.Parts.None) continue;
                HandPlacedPart stamp = group.GetComponent<HandPlacedPart>()
                                       ?? Undo.AddComponent<HandPlacedPart>(group.gameObject);
                stamp.Configure(part);
                EditorUtility.SetDirty(stamp);
                report.Append(' ').Append(part);
            }

            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            Debug.Log(
                $"[ScenePreview] Kept {keep.Count} object(s) under \"{HandPlacedRoot}\"." +
                (report.Length > 0
                    ? $" The builder will stop producing:{report}."
                    : " These are loose props; nothing generated is replaced.") +
                " Save the scene to keep them.");
        }

        /// <summary>
        /// The generated group an object belongs to, or the object itself when it
        /// is not part of one.
        /// </summary>
        private static Transform ResolveGroup(Transform picked, out HandPlacedWorld.Parts part)
        {
            for (Transform step = picked; step != null; step = step.parent)
            {
                foreach ((string name, HandPlacedWorld.Parts groupPart) in GeneratedGroups)
                {
                    if (step.name != name) continue;
                    part = groupPart;
                    return step;
                }
            }

            part = HandPlacedWorld.Parts.None;
            return picked;
        }

        [MenuItem("Shawarma Tycoon/Scene/Clear World Preview", priority = 11)]
        public static void Clear()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Found through the marker rather than by walking the scene's roots.
            // An object can end up belonging to no scene at all - hide flags will
            // do it - and one of those is invisible to both GetRootGameObjects and
            // FindObjectsByType while still drawing itself perfectly happily.
            foreach (ScenePreviewRoot marker in Resources.FindObjectsOfTypeAll<ScenePreviewRoot>())
            {
                if (marker == null || EditorUtility.IsPersistent(marker)) continue;
                Object.DestroyImmediate(marker.gameObject);
            }

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name.StartsWith(RuntimeRootName, System.StringComparison.Ordinal))
                    Object.DestroyImmediate(root);
            }

            // The HUD and the input event system are parented outside the runtime
            // root, so clearing only that root leaves them behind.
            foreach (string leftover in new[] { "Game HUD", "EventSystem" })
            {
                GameObject found = GameObject.Find(leftover);
                while (found != null)
                {
                    Object.DestroyImmediate(found);
                    found = GameObject.Find(leftover);
                }
            }
        }

        [MenuItem("Shawarma Tycoon/Scene/Build World Preview", true)]
        [MenuItem("Shawarma Tycoon/Scene/Clear World Preview", true)]
        [MenuItem("Shawarma Tycoon/Scene/Keep Selection", true)]
        private static bool Validate() =>
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

        /// <summary>
        /// The one object here that is meant to be saved. It sits at the scene root
        /// and the runtime build never touches it, so whatever is parented under it
        /// is in the game exactly where it was left.
        /// </summary>
        private static void EnsureHandPlacedRoot()
        {
            GameObject placed = GameObject.Find(HandPlacedRoot);
            if (placed == null)
            {
                placed = new GameObject(HandPlacedRoot);
                Undo.RegisterCreatedObjectUndo(placed, "Create hand-placed root");
                EditorSceneManager.MarkSceneDirty(placed.scene);
            }

            // Carried by the root itself so the switch is in the Inspector next to
            // the things it is switching off.
            if (placed.GetComponent<HandPlacedWorld>() == null)
                Undo.AddComponent<HandPlacedWorld>(placed);
        }
    }
}
