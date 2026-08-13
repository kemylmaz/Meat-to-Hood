using UnityEditor;
using UnityEngine;

namespace ShawarmaTycoon.EditorTools
{
    /// <summary>Editor entry point for wiping the local save during testing.</summary>
    public static class SaveResetMenu
    {
        [MenuItem("Shawarma Tycoon/Reset Save Progress", priority = 10)]
        public static void ResetProgress()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset save progress",
                    "This clears coins, records, daily tasks, unlocks and hired staff " +
                    "for this machine. It cannot be undone.",
                    "Reset", "Cancel"))
                return;

            GameProgress.ResetAll();
            Debug.Log("[ShawarmaTycoon] Save progress reset.");
        }

        [MenuItem("Shawarma Tycoon/Reset Save Progress", true)]
        private static bool ValidateResetProgress() => !EditorApplication.isPlaying;
    }
}
