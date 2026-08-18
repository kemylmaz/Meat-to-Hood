using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// How finished the shop is, as one number: every purchasable step in the
    /// game counted once, whether it is a belt level, a hire, an office or a plot.
    ///
    /// Each owner registers what it sells and how to read its current level, so
    /// adding a pad or a staff role moves the total on its own. Nothing here
    /// stores state - the levels live in the save, and this only adds them up.
    /// </summary>
    public static class UpgradeProgress
    {
        private readonly struct Track
        {
            public Track(string id, int steps, Func<int> owned)
            {
                Id = id;
                Steps = Mathf.Max(1, steps);
                Owned = owned;
            }

            public string Id { get; }
            public int Steps { get; }
            public Func<int> Owned { get; }
        }

        private static readonly List<Track> Tracks = new();

        /// <summary>Raised when a step is bought, so the HUD does not have to poll.</summary>
        public static event Action Changed;

        public static int TotalSteps
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Tracks.Count; i++) total += Tracks[i].Steps;
                return total;
            }
        }

        public static int OwnedSteps
        {
            get
            {
                int owned = 0;
                for (int i = 0; i < Tracks.Count; i++)
                    owned += Mathf.Clamp(Tracks[i].Owned(), 0, Tracks[i].Steps);
                return owned;
            }
        }

        public static float Ratio
        {
            get
            {
                int total = TotalSteps;
                return total <= 0 ? 0f : OwnedSteps / (float)total;
            }
        }

        /// <summary>
        /// Registers one thing that can be bought, in <paramref name="steps"/>
        /// stages. Re-registering the same id replaces it rather than counting it
        /// twice, which matters because the bootstrap can rebuild the whole shop
        /// into a live session.
        /// </summary>
        public static void Register(string id, int steps, Func<int> ownedLevels)
        {
            if (string.IsNullOrWhiteSpace(id) || ownedLevels == null) return;

            Track track = new(id, steps, ownedLevels);
            for (int i = 0; i < Tracks.Count; i++)
            {
                if (!string.Equals(Tracks[i].Id, id, StringComparison.Ordinal)) continue;
                Tracks[i] = track;
                Changed?.Invoke();
                return;
            }

            Tracks.Add(track);
            Changed?.Invoke();
        }

        /// <summary>Called by whatever just sold a step.</summary>
        public static void NotifyChanged() => Changed?.Invoke();

        /// <summary>
        /// Drops every track. The bootstrap calls this before it builds, so a
        /// rebuilt shop does not inherit tracks whose owners have been destroyed
        /// and whose level readers point at dead objects.
        /// </summary>
        public static void Reset()
        {
            Tracks.Clear();
            Changed?.Invoke();
        }
    }
}
