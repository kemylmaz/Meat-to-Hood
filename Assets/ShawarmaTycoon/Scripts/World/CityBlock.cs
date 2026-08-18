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

        /// <summary>
        /// Darker than the fallback asphalt so the driveway still reads as tarmac
        /// beside the authored road tiles, which are darker than the primitive
        /// colour the road falls back to.
        /// </summary>
        private static readonly Color Driveway = new(0.23f, 0.23f, 0.26f);
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
            BuildSidewalks(city, layout);
            BuildRoad(city, layout);
            BuildSurroundings(city, layout, HandPlacedWorld.Replaced);
            return city;
        }

        // --- ground ----------------------------------------------------------

        private static void BuildGround(Transform city, CityLayout layout)
        {
            // One big slab under everything so nothing reads as floating. Its top
            // is level with the shop floor: the two used to sit 25 cm apart, and a
            // customer walking in from the street kept the height they spawned at,
            // so half the queue stood sunk into the tiles.
            //
            // It sits below the tarmac, not level with the pavement. The slab runs
            // under the lot as well, so laid at the walking height it both z-fought
            // with the shop floor and buried the road and the driveway that are
            // meant to lie a kerb's depth under it.
            float top = layout.RoadY - 0.03f;
            PrototypeVisuals.CreatePrimitive(
                "City Ground", PrimitiveType.Cube, city,
                new Vector3(layout.CenterX, top - 0.30f, layout.CenterZ),
                new Vector3(layout.GroundWidth, 0.60f, layout.GroundDepth),
                GroundBase, colliderEnabled: true);
        }

        // --- pavement + road --------------------------------------------------

        private const string RoadTile = "40_road_straight";
        private const string WalkTile = "41_sidewalk_straight";
        private const string LampModel = "126_street_lamp";

        /// <summary>
        /// The eight City Builder facades, tallest last. The row alternates
        /// through them rather than through the two authored blocks it used to,
        /// which repeated every second building along a 70 m street.
        /// </summary>
        private static readonly string[] Facades =
        {
            "100_city_building_a", "104_city_building_e", "102_city_building_c",
            "101_city_building_b", "105_city_building_f", "103_city_building_d",
            "106_city_building_g", "107_city_building_h"
        };

        private static void BuildSidewalks(Transform city, CityLayout layout)
        {
            GameObject walks = new("Sidewalks");
            walks.transform.SetParent(city, false);

            BuildServiceLaneSurface(walks.transform, layout);
            BuildForecourt(walks.transform, layout);

            if (CityKit.Has(WalkTile))
            {
                float span = layout.GroundWidth * 0.86f;
                // The model's pivot is at its base, so it is dropped by its own
                // height to land level with the shop floor.
                float walkY = layout.SurfaceY - CityKit.TileHeight(WalkTile, 0.22f);
                CityKit.Tile(WalkTile, walks.transform, layout.CenterX,
                    layout.FarWalkCenterZ, span, 0f, walkY);

                // Pavement down the -X flank, filling the gap the side facades
                // were pushed back to leave. +X is left clear for expansions.
                float flankX = layout.CenterX -
                    (layout.LotWidth * 0.5f + layout.SideWalkGap * 0.5f);
                float flankFrom = layout.FrontEdgeZ;
                float flankTo = layout.LotDepth * 0.5f;
                CityKit.TileAlongZ(WalkTile, walks.transform, flankX,
                    (flankFrom + flankTo) * 0.5f, flankTo - flankFrom, -90f, walkY);
                return;
            }

            Strip(walks.transform, "Far Walk",
                new Vector3(layout.CenterX, layout.SurfaceY - 0.09f, layout.FarWalkCenterZ),
                new Vector3(layout.GroundWidth * 0.86f, 0.18f, layout.WalkDepth));

            // paving joints, cheap but they stop the pavement reading as a plane
            for (float x = -layout.GroundWidth * 0.42f; x < layout.GroundWidth * 0.42f; x += 2.4f)
                PrototypeVisuals.CreatePrimitive("Walk Joint", PrimitiveType.Cube, walks.transform,
                    new Vector3(layout.CenterX + x, layout.SurfaceY + 0.005f, layout.FarWalkCenterZ),
                    new Vector3(0.06f, 0.012f, layout.WalkDepth - 0.5f), Curb);
        }

        /// <summary>
        /// Paving outside the gate, level with the shop floor. This is the ground
        /// the queue spawns on and walks in over, and without it customers arrived
        /// out of bare earth and the gate divided nothing from nothing.
        /// </summary>
        private static void BuildForecourt(Transform parent, CityLayout layout)
        {
            GameObject court = new("Front Forecourt");
            court.transform.SetParent(parent, false);

            float kerb = -layout.LotDepth * 0.5f - 0.2f;
            float span = layout.LotWidth + layout.SideWalkGap * 2f;

            if (CityKit.Has(WalkTile))
            {
                float y = layout.SurfaceY - CityKit.TileHeight(WalkTile, 0.22f);
                // Two rows deep, so the spawn point outside the gate is on paving
                // rather than a metre past the end of it.
                for (int row = 0; row < 2; row++)
                    CityKit.Tile(WalkTile, court.transform, layout.CenterX,
                        kerb - layout.WalkDepth * (row + 0.5f), span, 180f, y);
                return;
            }

            PrototypeVisuals.CreatePrimitive("Forecourt", PrimitiveType.Cube, court.transform,
                new Vector3(layout.CenterX, layout.SurfaceY - 0.09f, kerb - layout.WalkDepth),
                new Vector3(span, 0.18f, layout.WalkDepth * 2f), Sidewalk, colliderEnabled: true);
        }

        /// <summary>
        /// Tarmac for the driveway that runs the length of the back of the lot.
        /// The surface is here from the start - it is the forecourt whether or not
        /// anything is served off it. What the drive-through purchase adds is the
        /// lane markings and the cars, in <see cref="BuildDriveway"/>.
        /// </summary>
        private static void BuildServiceLaneSurface(Transform parent, CityLayout layout)
        {
            PrototypeVisuals.CreatePrimitive("Service Lane", PrimitiveType.Cube, parent,
                new Vector3(layout.CenterX, layout.RoadY - 0.06f, layout.ServiceLaneZ),
                new Vector3(layout.GroundWidth * 0.86f, 0.12f, layout.ServiceLaneWidth),
                Driveway, colliderEnabled: true);

            foreach (int side in new[] { 1, -1 })
                PrototypeVisuals.CreatePrimitive("Lane Kerb", PrimitiveType.Cube, parent,
                    new Vector3(layout.CenterX, layout.SurfaceY - 0.10f,
                        layout.ServiceLaneZ + side * (layout.ServiceLaneWidth * 0.5f + 0.11f)),
                    new Vector3(layout.GroundWidth * 0.86f, 0.20f, 0.22f), Curb);
        }

        private static void Strip(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            PrototypeVisuals.CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale,
                Sidewalk, colliderEnabled: true);
        }

        private static void BuildRoad(Transform city, CityLayout layout)
        {
            GameObject road = new("Road");
            road.transform.SetParent(city, false);

            if (CityKit.Has(RoadTile))
            {
                float roadY = layout.RoadY - CityKit.TileHeight(RoadTile, 0.14f);
                CityKit.Tile(RoadTile, road.transform, layout.CenterX,
                    layout.RoadCenterZ, layout.GroundWidth * 0.92f, 0f, roadY);
                return;
            }

            PrototypeVisuals.CreatePrimitive("Asphalt", PrimitiveType.Cube, road.transform,
                new Vector3(layout.CenterX, layout.RoadY - 0.06f, layout.RoadCenterZ),
                new Vector3(layout.GroundWidth * 0.92f, 0.12f, layout.RoadWidth),
                Asphalt, colliderEnabled: true);

            // centre dashes
            float half = layout.GroundWidth * 0.44f;
            for (float x = -half; x < half; x += 3.2f)
                PrototypeVisuals.CreatePrimitive("Road Dash", PrimitiveType.Cube, road.transform,
                    new Vector3(layout.CenterX + x, layout.RoadY + 0.005f, layout.RoadCenterZ),
                    new Vector3(1.5f, 0.02f, 0.16f), RoadLine);

            // lane edges
            foreach (int side in new[] { 1, -1 })
                PrototypeVisuals.CreatePrimitive("Road Edge", PrimitiveType.Cube, road.transform,
                    new Vector3(layout.CenterX, layout.RoadY + 0.005f,
                        layout.RoadCenterZ + side * (layout.RoadWidth * 0.5f - 0.35f)),
                    new Vector3(layout.GroundWidth * 0.92f, 0.02f, 0.12f), RoadLine);
        }

        /// <summary>
        /// The driveway: a marked apron between the service lane and the pavement
        /// outside the drive-through window, with a stop line under the window and
        /// arrows showing which way the queue runs.
        ///
        /// Built on demand rather than with the block, because until the window is
        /// bought there is no drive-through and painting a lane for one would
        /// promise a service the shop cannot give.
        /// </summary>
        public static Transform BuildDriveway(Transform parent, CityLayout layout, float windowX)
        {
            GameObject driveway = new("Driveway");
            driveway.transform.SetParent(parent, false);

            float laneZ = layout.ServiceLaneZ;
            float y = layout.RoadY + 0.008f;
            float from = windowX - 13f;
            float to = windowX + 5f;

            PrototypeVisuals.CreatePrimitive("Lane Surface", PrimitiveType.Cube, driveway.transform,
                new Vector3((from + to) * 0.5f, y, laneZ),
                new Vector3(to - from, 0.02f, 3.1f), new Color(0.27f, 0.27f, 0.30f));

            // Dashed guide line on the pavement side of the lane.
            for (float x = from + 1f; x < to - 1f; x += 2.4f)
                PrototypeVisuals.CreatePrimitive("Lane Dash", PrimitiveType.Cube, driveway.transform,
                    new Vector3(x, y + 0.01f, laneZ - 1.45f),
                    new Vector3(1.3f, 0.02f, 0.14f), RoadLine);

            // Arrows point the way the near lane drives, toward +X.
            for (float x = from + 2.5f; x < windowX - 1f; x += 4.5f)
            {
                PrototypeVisuals.CreatePrimitive("Arrow Shaft", PrimitiveType.Cube, driveway.transform,
                    new Vector3(x, y + 0.01f, laneZ), new Vector3(1.5f, 0.02f, 0.20f), RoadLine);
                PrototypeVisuals.CreatePrimitive("Arrow Head", PrimitiveType.Cube, driveway.transform,
                    new Vector3(x + 0.85f, y + 0.01f, laneZ),
                    new Vector3(0.42f, 0.02f, 0.42f), RoadLine, new Vector3(0f, 45f, 0f));
            }

            PrototypeVisuals.CreatePrimitive("Stop Line", PrimitiveType.Cube, driveway.transform,
                new Vector3(windowX + 1.5f, y + 0.01f, laneZ),
                new Vector3(0.22f, 0.02f, 2.9f), RoadLine);
            return driveway.transform;
        }

        // --- skyline ----------------------------------------------------------

        /// <summary>
        /// The street's dressing. <paramref name="handPlaced"/> names the parts the
        /// open scene already provides by hand; those are left out rather than laid
        /// down a second time on top of somebody's own arrangement.
        ///
        /// The two groups draw from one seed in this order, so a scene that has
        /// taken over neither gets exactly the block it always got.
        /// </summary>
        private static void BuildSurroundings(
            Transform city, CityLayout layout, HandPlacedWorld.Parts handPlaced)
        {
            int seed = 0;
            if (!handPlaced.HasFlag(HandPlacedWorld.Parts.Skyline))
            {
                GameObject skyline = new("Skyline");
                skyline.transform.SetParent(city, false);

                // The main row sits across the road and faces the camera, so it is
                // the wall the player actually reads behind the traffic.
                float frontLine = layout.FarWalkCenterZ + layout.WalkDepth * 0.5f;
                float fromX = layout.CenterX - layout.GroundWidth * 0.46f;
                float toX = layout.CenterX + layout.GroundWidth * 0.46f;

                if (AuthoredRow(skyline.transform, frontLine, fromX, toX, layout.SurfaceY, ref seed))
                {
                    // A second row set back behind the first, running the tall half
                    // of the set so the roofline breaks rather than reading as one
                    // wall.
                    AuthoredRow(skyline.transform, frontLine + 13f,
                        fromX + 5.5f, toX - 2f, layout.SurfaceY, ref seed, tallOnly: true);
                    BuildWaterTowers(skyline.transform, layout, frontLine, ref seed);
                }
                else
                {
                    RowOfBuildings(skyline.transform, frontLine, 1f, fromX, toX, 9f, 18f,
                        layout.SurfaceY, ref seed);
                    RowOfBuildings(skyline.transform, frontLine + 10.5f, 1f,
                        layout.CenterX - layout.GroundWidth * 0.40f + 3.5f,
                        layout.CenterX + layout.GroundWidth * 0.40f,
                        15f, 25f, layout.SurfaceY, ref seed);
                }

                // Only the -X flank gets facades. The camera always sits at +X/-Z
                // of the player, so anything tall on the +X side ends up between
                // the lens and the restaurant and swallows the play area.
                SideBuildings(skyline.transform, layout, -1f, ref seed);
                BuildEastEdge(skyline.transform, layout);

                BuildLamps(skyline.transform, layout);
            }

            if (!handPlaced.HasFlag(HandPlacedWorld.Parts.StreetProps))
                BuildStreetProps(city, layout, ref seed);
        }

        /// <summary>
        /// Row of authored facades along a street line. Each model's pivot is at
        /// its own centre, so the piece is pushed back by half its depth to land
        /// its front wall on <paramref name="frontZ"/>.
        /// </summary>
        private static bool AuthoredRow(
            Transform parent, float frontZ, float fromX, float toX, float baseY, ref int seed,
            bool tallOnly = false)
        {
            if (!CityKit.Has(Facades[0])) return false;

            // The set is ordered by height, so the back row draws from the tall
            // half and stands over the front row rather than hiding behind it.
            int first = tallOnly ? Facades.Length / 2 : 0;
            float x = fromX;
            while (x < toX)
            {
                string model = Facades[first + Mathf.Abs(NextInt(ref seed)) % (Facades.Length - first)];
                float width = CityKit.TileWidth(model, 8f);
                float depth = CityKit.TileDepth(model, 6f);
                if (x + width > toX) break;
                // Turned to look back across the road at the shop. Only the +Z
                // face of these models is a shopfront - awning, door, glazing -
                // and the other three are blank wall, so left unturned the whole
                // street showed the player its back.
                CityKit.Spawn(model, parent,
                    new Vector3(x + width * 0.5f, baseY, frontZ + depth * 0.5f), 180f);
                x += width + Random(ref seed, 0.25f, 1.0f);
            }
            return true;
        }

        /// <summary>
        /// Two tanks breaking the roofline. They stand behind the back row rather
        /// than on a roof: the facades are single closed meshes with no flat top
        /// to put anything on, and set back they read as the next street over.
        /// </summary>
        private static void BuildWaterTowers(
            Transform parent, CityLayout layout, float frontLine, ref int seed)
        {
            if (!CityKit.Has(WaterTower)) return;
            float behind = frontLine + 13f + CityKit.TileDepth(Facades[0], 11f) + 2f;
            foreach (float x in new[] { -0.16f, 0.1f })
                CityKit.Spawn(WaterTower, parent,
                    new Vector3(layout.CenterX + layout.GroundWidth * x, layout.SurfaceY,
                        behind + Random(ref seed, 0f, 2.5f)),
                    Random(ref seed, 0f, 90f));
        }

        private static void RowOfBuildings(
            Transform parent, float zCenter, float facing,
            float fromX, float toX, float minHeight, float maxHeight, float baseY, ref int seed)
        {
            float x = fromX;
            while (x < toX)
            {
                float width = Random(ref seed, 5.5f, 9f);
                if (x + width > toX) width = toX - x;
                if (width < 3f) break;
                float height = Random(ref seed, minHeight, maxHeight);
                float depth = Random(ref seed, 6f, 9f);
                Building(parent, new Vector3(x + width * 0.5f, baseY, zCenter + facing * depth * 0.5f),
                    width, depth, height, facing, ref seed);
                x += width + Random(ref seed, 0.3f, 1.1f);
            }
        }

        private static void SideBuildings(Transform parent, CityLayout layout, float side, ref int seed)
        {
            // frontX is where the facade's FRONT WALL lands, leaving a pavement
            // strip between it and the lot. Asymmetric on purpose: +X has to
            // clear the purchasable expansion plots, which reach x = 20.
            float offset = side < 0f
                ? layout.LotWidth * 0.5f + layout.SideWalkGap
                : layout.ExpansionReach + layout.SideWalkGap;
            float frontX = layout.CenterX + side * offset;
            float z = layout.FrontEdgeZ + 2f;
            float end = layout.RoadCenterZ - layout.RoadWidth * 0.5f - 1f;

            // Turned so the shopfront on the model's +Z face looks across the lot,
            // which on the -X flank means east, into the camera. This row lines the
            // pavement the queue walks down, so it is the one row the player reads
            // close up and the one that most wants its front toward them.
            bool authored = CityKit.Has(Facades[0]);
            float yaw = side < 0f ? 90f : -90f;

            while (z < end)
            {
                if (authored)
                {
                    string model = Facades[Mathf.Abs(NextInt(ref seed)) % Facades.Length];
                    // Rotated a quarter turn, the model's width runs along Z and
                    // its depth along X.
                    float along = CityKit.TileWidth(model, 8f);
                    float deep = CityKit.TileDepth(model, 6f);
                    if (z + along > end) break;
                    // The facade faces the lot, so the body sits BEHIND frontX.
                    CityKit.Spawn(model, parent,
                        new Vector3(frontX + side * deep * 0.5f, layout.SurfaceY, z + along * 0.5f), yaw);
                    z += along + Random(ref seed, 0.25f, 0.9f);
                }
                else
                {
                    float depth = Random(ref seed, 5.5f, 8.5f);
                    float height = Random(ref seed, 8f, 15f);
                    Building(parent, new Vector3(frontX + side * 4.5f, layout.SurfaceY, z + depth * 0.5f),
                        9f, depth, height, -side, ref seed, sideFacing: true);
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

        /// <summary>
        /// Low dressing for the +X flank: a pavement run past the expansion
        /// plots with a few parked cars. Nothing here is tall enough to occlude,
        /// which is the whole point of not putting facades on this side.
        /// </summary>
        private static void BuildEastEdge(Transform parent, CityLayout layout)
        {
            GameObject edge = new("East Edge");
            edge.transform.SetParent(parent, false);

            float walkX = layout.CenterX + layout.ExpansionReach + layout.WalkDepth * 0.5f + 0.4f;
            float from = layout.FrontEdgeZ;
            float to = layout.LotDepth * 0.5f + 2f;

            if (CityKit.Has(WalkTile))
            {
                CityKit.TileAlongZ(WalkTile, edge.transform, walkX,
                    (from + to) * 0.5f, to - from, 90f,
                    layout.SurfaceY - CityKit.TileHeight(WalkTile, 0.22f));
            }
            else
            {
                PrototypeVisuals.CreatePrimitive("East Walk", PrimitiveType.Cube, edge.transform,
                    new Vector3(walkX, layout.SurfaceY - 0.09f, (from + to) * 0.5f),
                    new Vector3(layout.WalkDepth, 0.18f, to - from), Sidewalk);
            }

            float parkX = walkX + layout.WalkDepth * 0.5f + 1.2f;
            for (int i = 0; i < 3; i++)
            {
                GameObject car = CityKit.Spawn(TrafficCars[i % TrafficCars.Length], edge.transform,
                    new Vector3(parkX, layout.SurfaceY, from + 4f + i * 6.5f), 0f);
                if (car == null) continue;
                car.name = "Parked Car";
            }
        }

        /// <summary>
        /// The four City Builder vehicles. They carry their own paintwork on the
        /// shared atlas, so unlike the authored car they are not tinted per
        /// instance - variety comes from which one is spawned.
        /// </summary>
        public static readonly string[] TrafficCars =
        {
            "110_car_hatchback", "111_car_stationwagon", "112_car_taxi", "113_car_police"
        };

        private const string WaterTower = "130_water_tower";

        /// <summary>
        /// Street furniture along the pavements. It is scattered from the same
        /// deterministic seed as the skyline, so the block looks hand-placed but
        /// comes back identical on every run.
        ///
        /// Everything here goes on the far pavement, the -X flank or the east
        /// edge. The near side of the street is the drive-through lane, and a
        /// bench in it is something the queue has to drive over.
        /// </summary>
        private static void BuildStreetProps(Transform city, CityLayout layout, ref int seed)
        {
            if (!CityKit.Has("120_street_bench")) return;

            GameObject props = new("Street Props");
            props.transform.SetParent(city, false);
            Transform parent = props.transform;
            float y = layout.SurfaceY;

            // --- far pavement, between the lamps ---
            float walkZ = layout.FarWalkCenterZ;
            float half = layout.GroundWidth * 0.40f;
            string[] pavement =
            {
                "120_street_bench", "123_street_bush", "125_street_hydrant",
                "121_street_box_a", "123_street_bush", "131_street_litter",
                "120_street_bench", "122_street_box_b"
            };
            for (int i = 0; i < pavement.Length; i++)
            {
                float x = layout.CenterX - half + (i + 0.5f) * (half * 2f / pavement.Length);
                CityKit.Spawn(pavement[i], parent,
                    new Vector3(x + Random(ref seed, -1.2f, 1.2f), y,
                        walkZ + Random(ref seed, -0.5f, 0.5f)),
                    Random(ref seed, 0f, 360f));
            }

            // --- the alley down the -X flank ---
            float flankX = layout.CenterX - (layout.LotWidth * 0.5f + layout.SideWalkGap * 0.5f);
            CityKit.Spawn("124_street_dumpster", parent,
                new Vector3(flankX, y, layout.FrontEdgeZ + 6.5f), 90f);
            CityKit.Spawn("121_street_box_a", parent,
                new Vector3(flankX - 0.7f, y, layout.FrontEdgeZ + 8.6f), Random(ref seed, 0f, 90f));
            CityKit.Spawn("122_street_box_b", parent,
                new Vector3(flankX + 0.6f, y, layout.FrontEdgeZ + 9.2f), Random(ref seed, 0f, 90f));
            CityKit.Spawn("123_street_bush", parent,
                new Vector3(flankX, y, layout.FrontEdgeZ + 12.5f), 0f);

            // --- the strip past the expansion plots ---
            float eastX = layout.CenterX + layout.ExpansionReach + layout.WalkDepth * 0.5f + 0.4f;
            CityKit.Spawn("123_street_bush", parent,
                new Vector3(eastX, y, layout.FrontEdgeZ + 3f), 0f);
            CityKit.Spawn("120_street_bench", parent,
                new Vector3(eastX, y, layout.FrontEdgeZ + 7.5f), 90f);
            CityKit.Spawn("125_street_hydrant", parent,
                new Vector3(eastX, y, layout.LotDepth * 0.5f), 0f);

            // --- the junction at either end of the road ---
            CityKit.Spawn("129_traffic_light_c", parent,
                new Vector3(layout.CenterX - half - 3f, y, walkZ - layout.WalkDepth * 0.3f), -90f);
            CityKit.Spawn("127_traffic_light_a", parent,
                new Vector3(layout.CenterX + half + 3f, y, walkZ - layout.WalkDepth * 0.3f), 90f);
        }

        private static void BuildLamps(Transform parent, CityLayout layout)
        {
            GameObject lamps = new("Street Lamps");
            lamps.transform.SetParent(parent, false);
            float half = layout.GroundWidth * 0.40f;
            bool authored = CityKit.Has(LampModel);
            for (float x = -half; x <= half; x += 9f)
            {
                Vector3 far = new(layout.CenterX + x + 4.5f, layout.SurfaceY,
                    layout.FarWalkCenterZ + layout.WalkDepth * 0.26f);

                // Only the far pavement gets lamps. The near side of the street is
                // the driveway now, and a lamp post standing in it would be
                // something the drive-through queue has to drive through.
                if (authored) CityKit.Spawn(LampModel, lamps.transform, far, 180f);
                else Lamp(lamps.transform, far, 1f);
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
        public float LotWidth = 22.56f;
        // Kept shallow on purpose: the isometric camera has to fit the lot AND
        // the street behind it in one portrait frame.
        public float LotDepth = 16.92f;
        // Match the authored tile depths so modular pieces line up exactly.
        public float WalkDepth = 2.6f;
        public float RoadWidth = 7.2f;
        public float CenterX = 0f;

        /// <summary>
        /// Height of everything the player and the customers walk on. The lot and
        /// the pavement share it, so walking through the gate is not a step.
        /// </summary>
        public float SurfaceY = 0.20f;

        /// <summary>Kerb drop from the pavement down to the tarmac.</summary>
        public float KerbHeight = 0.06f;
        public float RoadY => SurfaceY - KerbHeight;

        /// <summary>Clear space between the lot edge and the flanking facades.</summary>
        public float SideWalkGap = 3.4f;

        /// <summary>How far the purchasable expansion plots reach along +X.</summary>
        /// <summary>
        /// How far the purchasable plots reach along +X, and so where the pavement
        /// and the parked cars on that flank have to start. Two columns of 5.64 m
        /// plots off a lot half-width of 11.28 land their outer edge here; at the
        /// old 20 m the east pavement was laid across the outer column and the
        /// parked cars stood inside the last plot the player could buy.
        /// </summary>
        public float ExpansionReach = 22.56f;

        /// <summary>The drive-through lane, and the gap between it and the lot kerb.</summary>
        public float ServiceLaneWidth = 3.4f;
        public float LotToLane = 0.30f;

        /// <summary>
        /// The driveway, hard against the back of the lot. It takes the place of
        /// the near pavement: a car has to end up within reading distance of the
        /// window it is being served from, and across a pavement and half a road
        /// it was six metres away and usually outside the portrait frame.
        /// </summary>
        public float ServiceLaneZ => LotDepth * 0.5f + LotToLane + ServiceLaneWidth * 0.5f;

        public float RoadCenterZ => ServiceLaneZ + ServiceLaneWidth * 0.5f + RoadWidth * 0.5f;
        public float FarWalkCenterZ => RoadCenterZ + RoadWidth * 0.5f + WalkDepth * 0.5f;
        /// <summary>
        /// How far the ground reaches behind the far pavement. Deep enough for
        /// both rows of facades and the water towers behind them - the back row
        /// alone is 13 m of setback plus its own footprint.
        /// </summary>
        public float FarEdgeZ => FarWalkCenterZ + WalkDepth * 0.5f + 34f;
        public float FrontEdgeZ => -LotDepth * 0.5f - 10f;
        public float CenterZ => (FrontEdgeZ + FarEdgeZ) * 0.5f;

        public float GroundWidth = 78f;
        public float GroundDepth => FarEdgeZ - FrontEdgeZ;

        public float RoadNearLaneZ => RoadCenterZ - RoadWidth * 0.22f;
        public float RoadFarLaneZ => RoadCenterZ + RoadWidth * 0.22f;

        /// <summary>
        /// Z of a through-traffic lane. +1 drives toward +X on the near lane, -1
        /// drives toward -X on the far lane, so traffic keeps right. The driveway
        /// is not one of these: <see cref="ServiceLaneZ"/> is reached only by a car
        /// that has pulled out of the near lane to be served.
        /// </summary>
        public float LaneZ(int direction) => direction > 0 ? RoadNearLaneZ : RoadFarLaneZ;
    }
}
