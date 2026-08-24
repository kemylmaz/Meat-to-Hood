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
    ///   two belts and four tables         ~158 coins/min
    ///   everything bought                 ~310 coins/min
    ///
    /// The probe is a floor, not a target: it walks the whole cook cycle before it
    /// collects any money, where a player picks the cash up on the way past. Run
    /// to run variance is wide because it depends on how early the tables jam.
    ///
    /// The old ladder let one ordinary sale almost buy a belt or a table. Belts
    /// still establish the production line early, but seating is now the major
    /// long-term investment: the first paid table takes sustained saving and each
    /// expansion remains meaningful deep into a developed restaurant.
    /// </summary>
    public static class ShopPrices
    {
        // --- things that change what the shop is -----------------------------

        // The two visible belts automate storage -> spit and spit -> cutting.
        // Finished wraps are carried to the detached checkout by hand, keeping
        // the cashier aisle visually and physically clear.
        public static readonly int[] Belt = { 180, 520 };

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
            // Four individual dining-room tables. Even the first is a real
            // capacity investment now, not the change from one ordinary sale.
            600, 1000, 1600, 2400,
            // Six expansion plots, each opening with two tables. The curve keeps
            // seating as the long-term money sink once the kitchen is automated.
            3500, 4800, 6500, 8500, 11000, 14000
        };

        /// <summary>A decoration lifts the shop's standing, and standing is the tip.</summary>
        public static readonly int[] Decoration = { 250, 450, 750, 1100 };

        /// <summary>
        /// Standing each decoration adds, and never lets the shop fall back below.
        /// Four of them carry a shop that would otherwise sit near the floor of
        /// the tip range through most of a bad rush.
        /// </summary>
        public const float DecorationStanding = 6f;

        public const int HumanResourcesOffice = 400;
        public const int GeneralManagerOffice = 700;
        public const int DriveThru = 1200;
        public const int Fridge = 700;
        public const int DessertOven = 1100;
        public const int Courier = 1800;

        // --- things that make what is already there work harder ---------------

        /// <summary>
        /// A visible, predictable curve: every level costs half again as much.
        /// The last level is a late-game purchase, but no one line dwarfs the shop.
        /// </summary>
        public const float BoardCostGrowth = 1.5f;

        public const int BoardLevels = 5;

        public const int StaffSpeed = 150;
        public const int StaffCapacity = 130;
        public const int StaffAutomation = 180;

        public const int PlayerSpeed = 150;
        public const int PlayerCapacity = 200;
        public const int PlayerIncome = 260;

        // Hires. Each removes a whole repeated chore, so they sit above a belt
        // level and below a content unlock.
        public const int HireCashier = 700;
        public const int HireDriveThruCashier = 900;
        public const int HireRunner = 850;
        public const int HireBusser = 650;
        public const int HireSecondBusser = 550;

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
            Sum(Belt) + Sum(Table) + Sum(Decoration) +
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
