using UnityEngine;

namespace ShawarmaTycoon
{
    [CreateAssetMenu(menuName = "Shawarma Tycoon/Configuration/Restaurant Layout", fileName = "RestaurantLayoutConfig")]
    public sealed class RestaurantLayoutConfig : ScriptableObject
    {
        /// <summary>
        /// The prep line sits along the back wall, shifted west to leave the east
        /// end for the fridge. Checkout steps forward from that line: customers
        /// wait on its south side while the cashier has a real working aisle on
        /// the north side.
        /// </summary>
        [Header("Kitchen")]
        [SerializeField] private Vector3 meatSource = new(-5.9f, 0.25f, 7f);
        [SerializeField] private Vector3 oven = new(-2.5f, 0.25f, 7f);
        [SerializeField] private Vector3 cutting = new(1.1f, 0.25f, 7f);
        [SerializeField] private Vector3 service = new(4.9f, 0.25f, 5.35f);

        /// <summary>
        /// Pads stand beside the thing they build, not on a board somewhere else.
        /// Belt pads sit in front of the gap each belt fills; no two are within
        /// reach of one spot.
        /// </summary>
        [Header("Purchase Pads")]
        [SerializeField] private Vector3 meatBeltPad = new(-4.2f, 0.28f, 4.3f);
        [SerializeField] private Vector3 ovenBeltPad = new(-0.7f, 0.28f, 4.3f);
        [SerializeField] private Vector3 tablePad = new(5.2f, 0.28f, 0f);
        [SerializeField] private Vector3 decorationPad = new(5.2f, 0.28f, -3.2f);

        [Header("Dining and Flow")]
        [SerializeField] private Vector3 customerEntry = new(0f, 0.25f, -11.6f);
        [SerializeField] private Vector3 customerExit = new(9.9f, 0.25f, -11.9f);
        [SerializeField] private Vector3 queueFront = new(4.9f, 0.25f, 2.35f);
        // Centred between the two east-facing office doors, with enough room on
        // both sides for their approaches and a clear aisle to the nearest table.
        [SerializeField] private Vector3 trashBin = new(-5.9f, 0.25f, -2.1f);

        /// <summary>
        /// Only the heights are read from these: the window and its pad are pinned
        /// to the opening the shell leaves in the back wall, so the two can never
        /// drift apart.
        /// </summary>
        [Header("Drive-Through")]
        [SerializeField] private Vector3 driveThruCounter = new(-6.82f, 0.25f, 7.5f);
        [SerializeField] private Vector3 driveThruUnlockPad = new(-6.82f, 0.28f, 5.4f);

        /// <summary>
        /// Drinks use two storage points: the crate remains in the south-east,
        /// while the fridge finishes the back-wall counter run in the checkout's
        /// former slot. Dessert and courier utilities remain in the east wing.
        /// </summary>
        [Header("Drinks, Desserts and Couriers")]
        [SerializeField] private Vector3 drinkCrate = new(10.6f, 0.25f, -2.6f);
        [SerializeField] private Vector3 fridge = new(7.3f, 0.25f, 7f);
        [SerializeField] private Vector3 fridgePad = new(7.8f, 0.28f, 1.1f);
        [SerializeField] private Vector3 dessertOven = new(8.8f, 0.25f, -5.4f);
        [SerializeField] private Vector3 dessertPad = new(7.4f, 0.28f, -7.2f);
        [SerializeField] private Vector3 courierCounter = new(10.4f, 0.25f, -7.5f);
        [SerializeField] private Vector3 courierPad = new(10.6f, 0.28f, -5.4f);

        public Vector3 MeatSource => meatSource;
        public Vector3 Oven => oven;
        public Vector3 Cutting => cutting;
        public Vector3 Service => service;
        public Vector3 MeatBeltPad => meatBeltPad;
        public Vector3 OvenBeltPad => ovenBeltPad;
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
