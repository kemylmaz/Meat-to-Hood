using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Every price in the game, in one place, with the income they were measured
    /// against written beside them.
    ///
    /// Measured with the editor's economy probe driving the carry loop, over runs
    /// of five to eight minutes each:
    ///
    ///   hand-carried, nothing bought      ~50 coins/min  (two runs: 42 and 65)
    ///   three belts and a table wing      ~120 coins/min
    ///   everything bought                 ~310 coins/min
    ///
    /// The probe is a floor, not a target: it walks the whole cook cycle before it
    /// collects any money, where a player picks the cash up on the way past. Run
    /// to run variance is wide because it depends on how early the tables jam.
    ///
    /// The ladder is priced so the first purchase lands about a minute in and the
    /// whole board takes a bit over an hour. The first pass cost 49,000 - roughly
    /// four hours - and four fifths of that was the two upgrade boards, which sell
    /// multipliers rather than anything the player can see.
    /// </summary>
    public static class ShopPrices
    {
        // --- things that change what the shop is -----------------------------

        public static readonly int[] Belt = { 45, 90, 170 };
        public static readonly int[] StationWorker = { 70, 130, 240 };

        /// <summary>
        /// The third table through to the tenth. Seating is what the whole shop
        /// throughput is measured against, so this is the ladder the player climbs
        /// for the entire game rather than a pair of wings bought once.
        /// </summary>
        /// <summary>
        /// One entry per purchase, not per table: four fill the shop's own floor,
        /// then six open a plot each and stand two tables on it. Eighteen covers
        /// off ten steps.
        ///
        /// How far this ladder can run is set by the income guardrail rather than
        /// by how much floor there is. Seating is the biggest single sink in the
        /// game, and the probe measures a fully built shop at 318 coins a minute
        /// whether it has six tables or eighteen - the kitchen line caps it, not
        /// the seats. Pricing eighteen tables individually put buying the shop out
        /// at 242 minutes against an intended ceiling of 120, which is why a plot
        /// is sold whole and the first eight steps are untouched.
        /// </summary>
        public static readonly int[] Table =
        {
            50, 90, 150, 230, 330, 450, 600, 780, 990, 1240
        };

        /// <summary>A decoration lifts the shop's standing, and standing is the tip.</summary>
        public static readonly int[] Decoration = { 120, 200, 320, 480 };

        /// <summary>
        /// Standing each decoration adds, and never lets the shop fall back below.
        /// Four of them carry a shop that would otherwise sit near the floor of
        /// the tip range through most of a bad rush.
        /// </summary>
        public const float DecorationStanding = 6f;

        public const int HumanResourcesOffice = 130;
        public const int GeneralManagerOffice = 240;
        public const int DriveThru = 420;
        public const int Fridge = 260;
        public const int DessertOven = 380;
        public const int Courier = 620;

        // --- things that make what is already there work harder ---------------

        /// <summary>
        /// Step-up per board level. At 1.55 the last level of one line cost more
        /// than every belt, worker and unlock in the game put together.
        /// </summary>
        public const float BoardCostGrowth = 1.45f;

        public const int BoardLevels = 5;

        public const int StaffSpeed = 90;
        public const int StaffCapacity = 75;
        public const int StaffAutomation = 85;

        public const int PlayerSpeed = 80;
        public const int PlayerCapacity = 110;
        public const int PlayerIncome = 130;

        // Hires. Each removes a whole repeated chore, so they sit above a belt
        // level and below a content unlock.
        public const int HireCashier = 240;
        public const int HireDriveThruCashier = 300;
        public const int HireRunner = 280;
        public const int HireBusser = 200;
        public const int HireSecondBusser = 160;

        /// <summary>Cost of one board line taken from nothing to its last level.</summary>
        public static int BoardLineTotal(int baseCost)
        {
            int total = 0;
            for (int level = 0; level < BoardLevels; level++)
                total += Mathf.RoundToInt(baseCost * Mathf.Pow(BoardCostGrowth, level));
            return total;
        }

        public static int BoardCost(int baseCost, int level) =>
            Mathf.RoundToInt(baseCost * Mathf.Pow(BoardCostGrowth, Mathf.Max(0, level)));

        /// <summary>Everything the player can buy that adds to the shop itself.</summary>
        public static int ContentTotal =>
            Sum(Belt) * 3 + Sum(StationWorker) * 2 + Sum(Table) + Sum(Decoration) +
            HumanResourcesOffice + GeneralManagerOffice + DriveThru +
            Fridge + DessertOven + Courier +
            HireCashier + HireDriveThruCashier + HireRunner + HireBusser + HireSecondBusser;

        /// <summary>The two upgrade boards, taken to the last level.</summary>
        public static int BoardTotal =>
            BoardLineTotal(StaffSpeed) + BoardLineTotal(StaffCapacity) + BoardLineTotal(StaffAutomation) +
            BoardLineTotal(PlayerSpeed) + BoardLineTotal(PlayerCapacity) + BoardLineTotal(PlayerIncome);

        public static int EverythingTotal => ContentTotal + BoardTotal;

        private static int Sum(int[] ladder)
        {
            int total = 0;
            for (int i = 0; i < ladder.Length; i++) total += ladder[i];
            return total;
        }
    }
}
