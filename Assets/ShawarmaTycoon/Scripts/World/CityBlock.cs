using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Grounds the restaurant in a street block instead of a floating diorama:
    /// paved lot, kerb, a two lane road along the customer side, background
    /// facades that close the horizon and the lamps that light them.
    ///
    /// Layout is driven from <see cref="CityLayout"/> so the traffic system and
    /// the bootstrap agree on where the road actually is.
    /// </summary>
    public static class CityBlock
    {
        // --- palette ---------------------------------------------------------
        private static readonly Color GroundBase = new(0.62f, 0.60f, 0.57f);
        private static readonly Color Asphalt = new(0.31f, 0.31f, 0.34f);
        private static readonly Color RoadLine = new(0.94f, 0.89f, 0.78f);
        private static readonly Color Sidewalk = new(0.80f, 0.74f, 0.66f);
        private static readonly Color Curb = new(0.64f, 0.58f, 0.50f);
        private static readonly Color LotFloor = new(0.91f, 0.68f, 0.48f);
        private static readonly Color LotEdge = new(0.55f, 0.36f, 0.27f);
        private static readonly Color LampPost = new(0.23f, 0.25f, 0.28f);
        private static readonly Color LampGlow = new(1f, 0.91f, 0.66f);

        private static readonly Color[] BuildingWalls =
        {
            new(0.77f, 0.47f, 0.36f),
            new(0.56f, 0.60f, 0.65f),
            new(0.84f, 0.66f, 0.46f),
            new(0.49f, 0.55f, 0.52f),
            new(0.72f, 0.55f, 0.62f)
        };
        private static readonly Color BuildingTrim = new(0.95f, 0.91f, 0.82f);
        private static readonly Color WindowLit = new(0.98f, 0.86f, 0.55f);
        private static readonly Color WindowDark = new(0.42f, 0.52f, 0.58f);

        public static Transform Build(Transform parent, CityLayout layout)
        {
            GameObject root = new("City Block");
            root.transform.SetParent(parent, false);
            Transform city = root.transform;

            BuildGround(city, layout);
            BuildLot(city, layout);
            BuildSidewalks(city, layout);
            BuildRoad(city, layout);
            BuildSurroundings(city, layout);
            return city;
        }

        // --- ground ----------------------------------------------------------

        private static void BuildGround(Transform city, CityLayout layout)
        {
            // One big slab under everything so nothing reads as floating.
            PrototypeVisuals.CreatePrimitive(
                "City Ground", PrimitiveType.Cube, city,
                new Vector3(layout.CenterX, -0.30f, layout.CenterZ),
                new Vector3(layout.GroundWidth, 0.60f, layout.GroundDepth),
                GroundBase, colliderEnabled: true);
        }

        private static void BuildLot(Transform city, CityLayout layout)
        {
            GameObject lot = new("Restaurant Lot");
            lot.transform.SetParent(city, false);

            PrototypeVisuals.CreatePrimitive(
                "Lot Floor", PrimitiveType.Cube, lot.transform,
                new Vector3(0f, 0f, 0f),
                new Vector3(layout.LotWidth, 0.5f, layout.LotDepth),
                LotFloor, colliderEnabled: true);

            // kerb ring so the lot sits proud of the pavement
            float halfW = layout.LotWidth * 0.5f;
            float halfD = layout.LotDepth * 0.5f;
            AddEdge(lot.transform, new Vector3(0f, 0.14f, halfD + 0.11f),
                new Vector3(layout.LotWidth + 0.44f, 0.20f, 0.22f));
            AddEdge(lot.transform, new Vector3(0f, 0.14f, -halfD - 0.11f),
                new Vector3(layout.LotWidth + 0.44f, 0.20f, 0.22f));
            AddEdge(lot.transform, new Vector3(halfW + 0.11f, 0.14f, 0f),
                new Vector3(0.22f, 0.20f, layout.LotDepth + 0.44f));
            AddEdge(lot.transform, new Vector3(-halfW - 0.11f, 0.14f, 0f),
                new Vector3(0.22f, 0.20f, layout.LotDepth + 0.44f));

            CreateFloorGrid(lot.transform, layout.LotWidth, layout.LotDepth);
        }

        private static void AddEdge(Transform parent, Vector3 position, Vector3 scale)
        {
            PrototypeVisuals.CreatePrimitive("Lot Kerb", PrimitiveType.Cube, parent,
                position, scale, LotEdge);
        }

        private static void CreateFloorGrid(Transform parent, float width, float depth)
        {
            const float spacing = 1.5f;
            Color grout = new(0.73f, 0.55f, 0.41f);
            for (float x = -width * 0.5f + spacing; x < width * 0.5f; x += spacing)
                PrototypeVisuals.CreatePrimitive("Floor Grout X", PrimitiveType.Cube, parent,
                    new Vector3(x, 0.256f, 0f), new Vector3(0.025f, 0.012f, depth - 0.08f), grout);
            for (float z = -depth * 0.5f + spacing; z < depth * 0.5f; z += spacing)
                PrototypeVisuals.CreatePrimitive("Floor Grout Z", PrimitiveType.Cube, parent,
                    new Vector3(0f, 0.256f, z), new Vector3(width - 0.08f, 0.012f, 0.025f), grout);
        }

        // --- pavement + road --------------------------------------------------

        private const string RoadTile = "40_road_straight";
        private const string WalkTile = "41_sidewalk_straight";
        private const string LampModel = "45_street_lamp";
        private const string BuildingA = "43_city_building_a";
        private const string BuildingB = "44_city_building_b";

        private static void BuildSidewalks(Transform city, CityLayout layout)
        {
            GameObject walks = new("Sidewalks");
            walks.transform.SetParent(city, false);

            if (CityKit.Has(WalkTile))
            {
                float span = layout.GroundWidth * 0.86f;
                // Authored kerb sits on the tile's -Z edge: the near pavement has
                // to face the other way, so it is turned around.
                CityKit.Tile(WalkTile, walks.transform, layout.CenterX,
                    layout.NearWalkCenterZ, span, 180f);
                CityKit.Tile(WalkTile, walks.transform, layout.CenterX,
                    layout.FarWalkCenterZ, span, 0f);
                return;
            }

            // near pavement between the lot and the road; its kerb faces the road
            Strip(walks.transform, "Near Walk",
                new Vector3(layout.CenterX, 0.09f, layout.NearWalkCenterZ),
                new Vector3(layout.GroundWidth * 0.86f, 0.18f, layout.WalkDepth));
            Kerb(walks.transform, layout.CenterX,
                layout.NearWalkCenterZ + layout.WalkDepth * 0.5f - 0.13f,
                layout.GroundWidth * 0.86f);

            // far pavement on the other side of the road
            Strip(walks.transform, "Far Walk",
                new Vector3(layout.CenterX, 0.09f, layout.FarWalkCenterZ),
                new Vector3(layout.GroundWidth * 0.86f, 0.18f, layout.WalkDepth));
            Kerb(walks.transform, layout.CenterX,
                layout.FarWalkCenterZ - layout.WalkDepth * 0.5f + 0.13f,
                layout.GroundWidth * 0.86f);

            // paving joints, cheap but they stop the pavement reading as a plane
            for (float x = -layout.GroundWidth * 0.42f; x < layout.GroundWidth * 0.42f; x += 2.4f)
            {
                PrototypeVisuals.CreatePrimitive("Walk Joint", PrimitiveType.Cube, walks.transform,
                    new Vector3(layout.CenterX + x, 0.185f, layout.NearWalkCenterZ),
                    new Vector3(0.06f, 0.012f, layout.WalkDepth - 0.5f), Curb);
                PrototypeVisuals.CreatePrimitive("Walk Joint", PrimitiveType.Cube, walks.transform,
                    new Vector3(layout.CenterX + x, 0.185f, layout.FarWalkCenterZ),
                    new Vector3(0.06f, 0.012f, layout.WalkDepth - 0.5f), Curb);
            }
        }

        private static void Strip(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            PrototypeVisuals.CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale,
                Sidewalk, colliderEnabled: true);
        }

        private static void Kerb(Transform parent, float centerX, float z, float width)
        {
            PrototypeVisuals.CreatePrimitive("Kerb", PrimitiveType.Cube, parent,
                new Vector3(centerX, 0.11f, z), new Vector3(width, 0.24f, 0.26f), Curb);
        }

        private static void BuildRoad(Transform city, CityLayout layout)
        {
            GameObject road = new("Road");
            road.transform.SetParent(city, false);

            if (CityKit.Has(RoadTile))
            {
                CityKit.Tile(RoadTile, road.transform, layout.CenterX,
                    layout.RoadCenterZ, layout.GroundWidth * 0.92f);
                AddCrossing(road.transform, layout);
                return;
            }

            PrototypeVisuals.CreatePrimitive("Asphalt", PrimitiveType.Cube, road.transform,
                new Vector3(layout.CenterX, 0.02f, layout.RoadCenterZ),
                new Vector3(layout.GroundWidth * 0.92f, 0.12f, layout.RoadWidth),
                Asphalt, colliderEnabled: true);

            // centre dashes
            float half = layout.GroundWidth * 0.44f;
            for (float x = -half; x < half; x += 3.2f)
                PrototypeVisuals.CreatePrimitive("Road Dash", PrimitiveType.Cube, road.transform,
                    new Vector3(layout.CenterX + x, 0.085f, layout.RoadCenterZ),
                    new Vector3(1.5f, 0.02f, 0.16f), RoadLine);

            // lane edges
            foreach (int side in new[] { 1, -1 })
                PrototypeVisuals.CreatePrimitive("Road Edge", PrimitiveType.Cube, road.transform,
                    new Vector3(layout.CenterX, 0.085f,
                        layout.RoadCenterZ + side * (layout.RoadWidth * 0.5f - 0.35f)),
                    new Vector3(layout.GroundWidth * 0.92f, 0.02f, 0.12f), RoadLine);

            AddCrossing(road.transform, layout);
        }

        /// <summary>Zebra stripes marking the drive-through / service stop.</summary>
        private static void AddCrossing(Transform road, CityLayout layout)
        {
            for (int i = 0; i < 6; i++)
                PrototypeVisuals.CreatePrimitive("Crossing", PrimitiveType.Cube, road,
                    new Vector3(layout.CrossingX - 1.5f + i * 0.6f, 0.155f, layout.RoadCenterZ),
                    new Vector3(0.34f, 0.02f, layout.RoadWidth - 1.6f), RoadLine);
        }

        // --- skyline ----------------------------------------------------------

        private static void BuildSurroundings(Transform city, CityLayout layout)
        {
            GameObject skyline = new("Skyline");
            skyline.transform.SetParent(city, false);
            int seed = 0;

            // The main row sits across the road and faces the camera, so it is
            // the wall the player actually reads behind the traffic.
            float frontLine = layout.FarWalkCenterZ + layout.WalkDepth * 0.5f;
            float fromX = layout.CenterX - layout.GroundWidth * 0.46f;
            float toX = layout.CenterX + layout.GroundWidth * 0.46f;

            if (!AuthoredRow(skyline.transform, frontLine, fromX, toX, ref seed))
                RowOfBuildings(skyline.transform, frontLine, 1f, fromX, toX, 9f, 18f, ref seed);

            // a taller blocked-in second row staggers the skyline behind the first
            RowOfBuildings(skyline.transform, frontLine + 10.5f, 1f,
                layout.CenterX - layout.GroundWidth * 0.40f + 3.5f,
                layout.CenterX + layout.GroundWidth * 0.40f,
                15f, 25f, ref seed);

            // side walls of the block
            SideBuildings(skyline.transform, layout, -1f, ref seed);
            SideBuildings(skyline.transform, layout, 1f, ref seed);

            BuildLamps(skyline.transform, layout);
        }

        /// <summary>
        /// Row of authored facades along a street line. The models already face
        /// -Z, so the front wall lands exactly on <paramref name="frontZ"/>.
        /// </summary>
        private static bool AuthoredRow(
            Transform parent, float frontZ, float fromX, float toX, ref int seed)
        {
            if (!CityKit.Has(BuildingA) || !CityKit.Has(BuildingB))
                return false;

            float x = fromX;
            bool alternate = false;
            while (x < toX)
            {
                string model = alternate ? BuildingB : BuildingA;
                alternate = !alternate;
                float width = CityKit.TileWidth(model, 8f);
                float depth = CityKit.TileDepth(model, 6f);
                if (x + width > toX) break;
                CityKit.Spawn(model, parent,
                    new Vector3(x + width * 0.5f, 0f, frontZ + depth * 0.5f));
                x += width + Random(ref seed, 0.25f, 1.0f);
            }
            return true;
        }

        private static void RowOfBuildings(
            Transform parent, float zCenter, float facing,
            float fromX, float toX, float minHeight, float maxHeight, ref int seed)
        {
            float x = fromX;
            while (x < toX)
            {
                float width = Random(ref seed, 5.5f, 9f);
                if (x + width > toX) width = toX - x;
                if (width < 3f) break;
                float height = Random(ref seed, minHeight, maxHeight);
                float depth = Random(ref seed, 6f, 9f);
                Building(parent, new Vector3(x + width * 0.5f, 0f, zCenter + facing * depth * 0.5f),
                    width, depth, height, facing, ref seed);
                x += width + Random(ref seed, 0.3f, 1.1f);
            }
        }

        private static void SideBuildings(Transform parent, CityLayout layout, float side, ref int seed)
        {
            // Asymmetric on purpose: the -X side can hug the lot and fill the
            // empty left of frame, while +X has to clear the expansion plots.
            float offset = side < 0f ? layout.LotWidth * 0.5f + 7f : layout.LotWidth * 0.5f + 14f;
            float frontX = layout.CenterX + side * offset;
            float z = layout.FrontEdgeZ + 2f;
            float end = layout.RoadCenterZ - layout.RoadWidth * 0.5f - 1f;

            // Authored facades face -Z, so turn them to look across the lot.
            bool authored = CityKit.Has(BuildingA) && CityKit.Has(BuildingB);
            float yaw = side < 0f ? -90f : 90f;
            bool alternate = false;

            while (z < end)
            {
                if (authored)
                {
                    string model = alternate ? BuildingB : BuildingA;
                    alternate = !alternate;
                    // Rotated a quarter turn, the model's width runs along Z and
                    // its depth along X.
                    float along = CityKit.TileWidth(model, 8f);
                    float deep = CityKit.TileDepth(model, 6f);
                    if (z + along > end) break;
                    CityKit.Spawn(model, parent,
                        new Vector3(frontX - side * deep * 0.5f, 0f, z + along * 0.5f), yaw);
                    z += along + Random(ref seed, 0.25f, 0.9f);
                }
                else
                {
                    float depth = Random(ref seed, 5.5f, 8.5f);
                    float height = Random(ref seed, 8f, 15f);
                    Building(parent, new Vector3(frontX, 0f, z + depth * 0.5f), 9f, depth, height,
                        -side, ref seed, sideFacing: true);
                    z += depth + Random(ref seed, 0.4f, 1.2f);
                }
            }
        }

        private static void Building(
            Transform parent, Vector3 position, float width, float depth, float height,
            float facing, ref int seed, bool sideFacing = false)
        {
            GameObject building = new("Building");
            building.transform.SetParent(parent, false);
            building.transform.localPosition = position;

            Color wall = BuildingWalls[Mathf.Abs(NextInt(ref seed)) % BuildingWalls.Length];
            PrototypeVisuals.CreatePrimitive("Body", PrimitiveType.Cube, building.transform,
                new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth),
                wall, colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive("Cornice", PrimitiveType.Cube, building.transform,
                new Vector3(0f, height + 0.22f, 0f),
                new Vector3(width + 0.35f, 0.44f, depth + 0.35f), BuildingTrim);
            PrototypeVisuals.CreatePrimitive("Ground Band", PrimitiveType.Cube, building.transform,
                new Vector3(0f, 1.9f, 0f), new Vector3(width + 0.12f, 3.8f, depth + 0.12f),
                BuildingTrim);

            // windows only on the face the camera can see
            int floors = Mathf.Max(1, Mathf.FloorToInt((height - 4.2f) / 2.9f));
            int columns = Mathf.Max(2, Mathf.FloorToInt(width / 2.1f));
            float faceOffset = (sideFacing ? width : depth) * 0.5f + 0.08f;
            for (int f = 0; f < floors; f++)
            {
                float y = 4.6f + f * 2.9f;
                if (y > height - 1.2f) break;
                for (int c = 0; c < columns; c++)
                {
                    float t = (c + 0.5f) / columns - 0.5f;
                    float along = t * (sideFacing ? depth : width) * 0.86f;
                    Vector3 local = sideFacing
                        ? new Vector3(facing * faceOffset, y, along)
                        : new Vector3(along, y, facing * faceOffset);
                    Vector3 size = sideFacing
                        ? new Vector3(0.12f, 1.35f, 1.0f)
                        : new Vector3(1.0f, 1.35f, 0.12f);
                    Color glass = NextInt(ref seed) % 3 == 0 ? WindowLit : WindowDark;
                    PrototypeVisuals.CreatePrimitive("Window", PrimitiveType.Cube,
                        building.transform, local, size, glass);
                }
            }
        }

        private static void BuildLamps(Transform parent, CityLayout layout)
        {
            GameObject lamps = new("Street Lamps");
            lamps.transform.SetParent(parent, false);
            float half = layout.GroundWidth * 0.40f;
            bool authored = CityKit.Has(LampModel);
            for (float x = -half; x <= half; x += 9f)
            {
                Vector3 near = new(layout.CenterX + x, 0.16f,
                    layout.NearWalkCenterZ - layout.WalkDepth * 0.26f);
                Vector3 far = new(layout.CenterX + x + 4.5f, 0.16f,
                    layout.FarWalkCenterZ + layout.WalkDepth * 0.26f);

                if (authored)
                {
                    // Authored arm hangs toward +Z once the import offset is
                    // applied, so the far pavement lamp is the one spun round.
                    CityKit.Spawn(LampModel, lamps.transform, near, 0f);
                    CityKit.Spawn(LampModel, lamps.transform, far, 180f);
                }
                else
                {
                    Lamp(lamps.transform, near, -1f);
                    Lamp(lamps.transform, far, 1f);
                }
            }
        }

        private static void Lamp(Transform parent, Vector3 position, float armSign)
        {
            GameObject lamp = new("Street Lamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = position;

            PrototypeVisuals.CreatePrimitive("Base", PrimitiveType.Cylinder, lamp.transform,
                new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.12f, 0.42f), LampPost);
            PrototypeVisuals.CreatePrimitive("Post", PrimitiveType.Cylinder, lamp.transform,
                new Vector3(0f, 2.3f, 0f), new Vector3(0.14f, 2.2f, 0.14f), LampPost);
            PrototypeVisuals.CreatePrimitive("Arm", PrimitiveType.Cube, lamp.transform,
                new Vector3(0f, 4.45f, armSign * -0.45f), new Vector3(0.11f, 0.11f, 1.0f), LampPost);
            PrototypeVisuals.CreatePrimitive("Head", PrimitiveType.Cube, lamp.transform,
                new Vector3(0f, 4.32f, armSign * -0.92f), new Vector3(0.52f, 0.22f, 0.44f), LampPost);
            PrototypeVisuals.CreatePrimitive("Glow", PrimitiveType.Cube, lamp.transform,
                new Vector3(0f, 4.18f, armSign * -0.92f), new Vector3(0.42f, 0.06f, 0.34f), LampGlow);
        }

        // --- tiny deterministic RNG so the skyline is stable between runs -----

        private static int NextInt(ref int seed)
        {
            seed = seed * 1103515245 + 12345;
            return (seed >> 16) & 0x7FFF;
        }

        private static float Random(ref int seed, float min, float max)
        {
            return min + (max - min) * (NextInt(ref seed) / 32767f);
        }
    }

    /// <summary>
    /// Single source of truth for where the block's pieces live.
    ///
    /// The street runs along +Z, behind the kitchen line. That is the direction
    /// the isometric camera looks toward, so passing traffic actually reads on
    /// screen; a road on the -Z side would sit behind the camera.
    /// </summary>
    public sealed class CityLayout
    {
        public float LotWidth = 24f;
        // Kept shallow on purpose: the isometric camera has to fit the lot AND
        // the street behind it in one portrait frame.
        public float LotDepth = 17f;
        // Match the authored tile depths so modular pieces line up exactly.
        public float WalkDepth = 2.6f;
        public float RoadWidth = 7.2f;
        public float CenterX = 0f;
        public float CrossingX = 6f;

        /// <summary>Gap between the lot kerb and the near pavement.</summary>
        public float LotToWalk = 0.6f;

        public float NearWalkCenterZ => LotDepth * 0.5f + LotToWalk + WalkDepth * 0.5f;
        public float RoadCenterZ => NearWalkCenterZ + WalkDepth * 0.5f + RoadWidth * 0.5f;
        public float FarWalkCenterZ => RoadCenterZ + RoadWidth * 0.5f + WalkDepth * 0.5f;
        public float FarEdgeZ => FarWalkCenterZ + WalkDepth * 0.5f + 16f;
        public float FrontEdgeZ => -LotDepth * 0.5f - 10f;
        public float CenterZ => (FrontEdgeZ + FarEdgeZ) * 0.5f;

        public float GroundWidth = 78f;
        public float GroundDepth => FarEdgeZ - FrontEdgeZ;

        /// <summary>
        /// Z of a traffic lane. +1 drives toward +X on the near lane, -1 drives
        /// toward -X on the far lane, so traffic keeps right.
        /// </summary>
        public float LaneZ(int direction) =>
            RoadCenterZ - direction * RoadWidth * 0.24f;
    }
}
