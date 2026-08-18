using UnityEngine;

namespace ShawarmaTycoon
{
    [CreateAssetMenu(menuName = "Shawarma Tycoon/Configuration/Restaurant Layout", fileName = "RestaurantLayoutConfig")]
    public sealed class RestaurantLayoutConfig : ScriptableObject
    {
        /// <summary>
        /// Kitchen line, west to east along the back of the lot: rack, spit,
        /// carving board, till. Spacing is 4.4 m, which is the narrowest gap that
        /// still takes a 2.05 m conveyor between two counters.
        /// </summary>
        [Header("Kitchen")]
        [SerializeField] private Vector3 meatSource = new(-3.8f, 0.25f, 6.6f);
        [SerializeField] private Vector3 oven = new(0.6f, 0.25f, 6.6f);
        [SerializeField] private Vector3 cutting = new(5f, 0.25f, 6.6f);
        [SerializeField] private Vector3 service = new(9.4f, 0.25f, 6.6f);

        /// <summary>
        /// Pads stand beside the thing they build, not on a board somewhere else.
        /// Belt pads sit in front of the gap each belt fills; worker pads a row
        /// further out, so no two are within reach of one spot.
        /// </summary>
        [Header("Purchase Pads")]
        [SerializeField] private Vector3 meatBeltPad = new(-1.6f, 0.28f, 4.3f);
        [SerializeField] private Vector3 ovenBeltPad = new(2.8f, 0.28f, 4.3f);
        [SerializeField] private Vector3 cuttingBeltPad = new(7.2f, 0.28f, 4.3f);
        [SerializeField] private Vector3 ovenWorkerPad = new(0.6f, 0.28f, 2.8f);
        [SerializeField] private Vector3 cuttingWorkerPad = new(5f, 0.28f, 2.8f);
        [SerializeField] private Vector3 tablePad = new(0.6f, 0.28f, -0.4f);
        [SerializeField] private Vector3 decorationPad = new(-10.4f, 0.28f, -1.4f);

        [Header("Dining and Flow")]
        [SerializeField] private Vector3 customerEntry = new(7.68f, 0.25f, -11.6f);
        [SerializeField] private Vector3 customerExit = new(9.9f, 0.25f, -11.9f);
        [SerializeField] private Vector3 queueFront = new(9.4f, 0.25f, 4.2f);
        [SerializeField] private Vector3 trashBin = new(-10.3f, 0.25f, 4.2f);

        /// <summary>
        /// Only the heights are read from these: the window and its pad are pinned
        /// to the opening the shell leaves in the back wall, so the two can never
        /// drift apart.
        /// </summary>
        [Header("Drive-Through")]
        [SerializeField] private Vector3 driveThruCounter = new(-6.82f, 0.25f, 7.5f);
        [SerializeField] private Vector3 driveThruUnlockPad = new(-6.82f, 0.28f, 5.4f);

        /// <summary>
        /// The second counter row, along the south-east of the floor: drink crate,
        /// fridge, dessert oven, and the courier bay in the corner. Well clear of
        /// the kitchen line and the queue, so restocking is its own errand rather
        /// than something done in passing.
        /// </summary>
        [Header("Drinks, Desserts and Couriers")]
        [SerializeField] private Vector3 drinkCrate = new(2.6f, 0.25f, -5.4f);
        [SerializeField] private Vector3 fridge = new(5.4f, 0.25f, -5.4f);
        [SerializeField] private Vector3 fridgePad = new(5.4f, 0.28f, -3.4f);
        [SerializeField] private Vector3 dessertOven = new(8.2f, 0.25f, -5.4f);
        [SerializeField] private Vector3 dessertPad = new(8.2f, 0.28f, -3.4f);
        [SerializeField] private Vector3 courierCounter = new(10.2f, 0.25f, -7.4f);
        [SerializeField] private Vector3 courierPad = new(10.6f, 0.28f, -5.4f);

        public Vector3 MeatSource => meatSource;
        public Vector3 Oven => oven;
        public Vector3 Cutting => cutting;
        public Vector3 Service => service;
        public Vector3 MeatBeltPad => meatBeltPad;
        public Vector3 OvenBeltPad => ovenBeltPad;
        public Vector3 CuttingBeltPad => cuttingBeltPad;
        public Vector3 OvenWorkerPad => ovenWorkerPad;
        public Vector3 CuttingWorkerPad => cuttingWorkerPad;
        public Vector3 TablePad => tablePad;
        public Vector3 DecorationPad => decorationPad;
        public Vector3 CustomerEntry => customerEntry;
        public Vector3 CustomerExit => customerExit;
        public Vector3 QueueFront => queueFront;
        public Vector3 TrashBin => trashBin;
        public Vector3 DriveThruCounter => driveThruCounter;
        public Vector3 DriveThruUnlockPad => driveThruUnlockPad;
        public Vector3 DrinkCrate => drinkCrate;
        public Vector3 Fridge => fridge;
        public Vector3 FridgePad => fridgePad;
        public Vector3 DessertOven => dessertOven;
        public Vector3 DessertPad => dessertPad;
        public Vector3 CourierCounter => courierCounter;
        public Vector3 CourierPad => courierPad;

        public static RestaurantLayoutConfig CreateRuntimeDefaults()
        {
            RestaurantLayoutConfig config = CreateInstance<RestaurantLayoutConfig>();
            config.name = "Runtime Restaurant Layout";
            return config;
        }
    }
}
