#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ShawarmaTycoon.Editor
{
    /// <summary>
    /// Measures what the shop actually earns, by driving the player around the
    /// carry loop and sampling revenue.
    ///
    /// Prices were guesses stacked on guesses, and a guess cannot be checked by
    /// reading it. The bot is deliberately plain - it fills up, walks the line one
    /// station at a time and waits where it has to - so what it reports is close
    /// to a floor rather than a best case. A player who interleaves trips will
    /// beat it; nobody should do worse.
    /// </summary>
    public static class EconomyProbe
    {
        /// <summary>
        /// Clearing tables is in the loop because a shop with four tables and
        /// nobody bussing them jams inside a minute: the counter fills with wraps
        /// and the queue stands there because there is nowhere to seat anyone. A
        /// bot that only cooks measures a deadlock, not an economy.
        /// </summary>
        private enum Step
        {
            Load, ToOven, TakeCooked, ToCutting, TakeWraps, ToService, Collect, ClearTable, ToBin
        }

        private const float BotSpeed = 4.6f;
        private const float Reach = 0.55f;

        /// <summary>
        /// Where the bot stands to work a counter. The counters have solid
        /// colliders, so aiming at the middle of one just walks into it: the bot
        /// wedged against the rack and never got close enough to count as arrived,
        /// while the station happily kept serving it.
        /// </summary>
        private static readonly Vector3 CounterApproach = new(0f, 0f, -1.15f);

        /// <summary>Give up on a station that will not produce and move on.</summary>
        private const float WaitLimit = 6f;

        private static bool running;
        private static Step step;
        private static float stepAge;
        private static float elapsed;
        private static int revenueAtStart;
        private static string stageName;
        private static readonly List<string> samples = new();

        private static Transform player;
        private static CharacterController controller;
        private static CarryInventory hands;
        private static ItemStation rack, oven, cutting, service, crate, fridge;
        private static CashPile till;
        private static Transform bin;

        public static bool IsRunning => running;

        /// <summary>Starts the bot and the clock. Call once play mode is up.</summary>
        public static string Begin(string stage)
        {
            if (!Application.isPlaying) return "not playing";

            player = GameObject.Find("Player")?.transform;
            if (player == null) return "no player";
            controller = player.GetComponent<CharacterController>();
            hands = player.GetComponent<CarryInventory>();

            rack = oven = cutting = service = crate = fridge = null;
            foreach (ItemStation station in Object.FindObjectsByType<ItemStation>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                switch (station.name)
                {
                    case "ET DEPOSU": rack = station; break;
                    case "OCAK": oven = station; break;
                    case "KESİM": cutting = station; break;
                    case "SERVİS": service = station; break;
                    case "İÇECEK DEPOSU": crate = station; break;
                    case "BUZDOLABI": fridge = station; break;
                }
            }
            till = Object.FindFirstObjectByType<CashPile>();
            bin = Object.FindFirstObjectByType<TrashBin>()?.transform;
            if (rack == null || oven == null || cutting == null || service == null)
                return "kitchen line not found";

            stageName = stage;
            revenueAtStart = GameProgress.RevenueToday;
            elapsed = 0f;
            stepAge = 0f;
            step = Step.Load;
            samples.Clear();

            if (!running)
            {
                EditorApplication.update += Tick;
                running = true;
            }
            return "measuring '" + stage + "'";
        }

        public static void Stop()
        {
            if (!running) return;
            EditorApplication.update -= Tick;
            running = false;
        }

        /// <summary>Revenue earned per minute since <see cref="Begin"/>.</summary>
        public static string Report()
        {
            int earned = GameProgress.RevenueToday - revenueAtStart;
            float perMinute = elapsed <= 0.01f ? 0f : earned * 60f / elapsed;
            StringBuilder sb = new();
            sb.AppendLine($"[{stageName}] {elapsed:0} s, kazanç {earned}, dakikada {perMinute:0}");
            sb.AppendLine($"  (ciro {revenueAtStart} -> {GameProgress.RevenueToday})");
            sb.AppendLine($"  served={GameProgress.ServedToday} lost={GameProgress.LostToday}");
            for (int i = 0; i < samples.Count; i++) sb.AppendLine("  " + samples[i]);
            return sb.ToString();
        }

        private static void Tick()
        {
            if (!Application.isPlaying || player == null)
            {
                Stop();
                return;
            }

            float dt = Time.deltaTime;
            elapsed += dt;
            stepAge += dt;

            // A snapshot every 30 s, so a rate that decays as tables fill up is
            // visible rather than averaged away.
            if (samples.Count < Mathf.FloorToInt(elapsed / 30f))
            {
                int earned = GameProgress.RevenueToday - revenueAtStart;
                samples.Add($"{elapsed:0} s: toplam {earned}, anlık ~{earned * 60f / elapsed:0}/dk");
            }

            KeepFridgeStocked();
            Advance();
        }

        /// <summary>
        /// The drinks run, folded in so a measured stage with a fridge is not
        /// measuring a permanently empty one.
        /// </summary>
        private static void KeepFridgeStocked()
        {
            if (crate == null || fridge == null) return;
            if (!crate.isActiveAndEnabled || !fridge.isActiveAndEnabled) return;
            if (fridge.OutputCount > 3) return;
            if (crate.TryTakeOutputForConveyor(out ItemType drink))
                fridge.TryReceiveFromConveyor(drink);
        }

        private static void Advance()
        {
            bool full = hands.Count >= hands.Capacity;
            bool empty = hands.Count == 0;

            switch (step)
            {
                case Step.Load:
                    if (Worked(rack, full)) Next(Step.ToOven);
                    return;
                case Step.ToOven:
                    if (Worked(oven, empty)) Next(Step.TakeCooked);
                    return;
                case Step.TakeCooked:
                    if (Worked(oven, full)) Next(Step.ToCutting);
                    return;
                case Step.ToCutting:
                    if (Worked(cutting, empty)) Next(Step.TakeWraps);
                    return;
                case Step.TakeWraps:
                    if (Worked(cutting, full)) Next(Step.ToService);
                    return;
                case Step.ToService:
                    if (Worked(service, empty)) Next(Step.Collect);
                    return;
                case Step.Collect:
                    if (till != null && till.HasCash && !Stalled())
                    {
                        Walk(till.transform, Vector3.zero);
                        return;
                    }
                    Next(Step.ClearTable);
                    return;
                case Step.ClearTable:
                    CustomerTable dirty = FindDirtyTable();
                    if (dirty == null) { Next(Step.Load); return; }
                    if ((Walk(dirty.transform, Vector3.zero) && hands.TrashCount > 0) || Stalled())
                        Next(Step.ToBin);
                    return;
                default:
                    if (hands.TrashCount == 0) { Next(Step.Load); return; }
                    if (Walk(bin, Vector3.zero) || Stalled()) Next(Step.Load);
                    return;
            }
        }

        private static CustomerTable FindDirtyTable()
        {
            CustomerTable best = null;
            float nearest = float.MaxValue;
            foreach (CustomerTable table in Object.FindObjectsByType<CustomerTable>(
                         FindObjectsSortMode.None))
            {
                if (!table.IsDirty) continue;
                float distance = (table.transform.position - player.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                best = table;
            }
            return best;
        }

        /// <summary>
        /// Walks to a counter and reports whether this step is finished - either
        /// the job is done, or the bot has waited long enough that a real player
        /// would have given up and moved on. The timeout is checked outside the
        /// arrival test on purpose: a bot that cannot reach its target at all must
        /// still move on rather than lean on it forever.
        /// </summary>
        private static bool Worked(ItemStation station, bool done)
        {
            bool arrived = Walk(station != null ? station.transform : null, CounterApproach);
            return (arrived && done) || Stalled();
        }

        private static bool Stalled() => stepAge > WaitLimit;

        private static void Next(Step next)
        {
            step = next;
            stepAge = 0f;
        }

        private static bool Walk(Transform target, Vector3 localOffset)
        {
            if (target == null) return true;
            Vector3 destination = target.position + target.TransformVector(localOffset);
            Vector3 to = destination - player.position;
            to.y = 0f;
            if (to.sqrMagnitude <= Reach * Reach) return true;

            Vector3 stepMove = to.normalized * (BotSpeed * Time.deltaTime);
            if (controller != null && controller.enabled)
                controller.Move(stepMove + Vector3.down * 2f * Time.deltaTime);
            else player.position += stepMove;
            return false;
        }
    }
}
#endif
