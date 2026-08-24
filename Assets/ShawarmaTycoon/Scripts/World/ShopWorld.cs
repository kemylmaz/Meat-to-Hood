using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    [DisallowMultipleComponent]
    public sealed class DioramaWorld : MonoBehaviour
    {
        private readonly List<DioramaModule> expansionModules = new();

        public DioramaWalkableRegistry WalkableRegistry { get; private set; }
        public DioramaModule BaseModule { get; private set; }
        public IReadOnlyList<DioramaModule> ExpansionModules => expansionModules;
        public Transform KitchenRoot { get; private set; }
        public Transform DiningRoot { get; private set; }
        public Transform ManagementRoot { get; private set; }
        public Transform CustomerFlowRoot { get; private set; }
        public Transform UtilityRoot { get; private set; }

        /// <summary>Walls, fence and gate: everything that divides in from out.</summary>
        public Transform ShellRoot { get; private set; }

        /// <summary>Where a customer stands the moment they are through the gate.</summary>
        public Transform EntranceAnchor { get; private set; }

        /// <summary>Lot footprint and surface height, for anything laid around it.</summary>
        public Vector2 DeckSize { get; private set; }
        public float DeckTopY { get; private set; }

        /// <summary>Bay of the back wall the drive-through window is cut into.</summary>
        public float DriveThruWindowX { get; private set; }
        public float BackWallZ { get; private set; }

        /// <summary>
        /// The wall filling that bay. Solid until the drive-through is bought, at
        /// which point it comes down and the window takes its place. Leaving the
        /// bay open from the start put a hole in the back of the shop that led
        /// nowhere and could not be closed.
        /// </summary>
        public GameObject DriveThruWallBay { get; private set; }

        public void OpenDriveThruBay()
        {
            if (DriveThruWallBay != null) DriveThruWallBay.SetActive(false);
        }

        internal void Configure(
            DioramaWalkableRegistry registry,
            DioramaModule baseModule,
            IEnumerable<DioramaModule> expansions,
            Transform kitchen,
            Transform dining,
            Transform management,
            Transform customerFlow,
            Transform utility)
        {
            WalkableRegistry = registry;
            BaseModule = baseModule;
            expansionModules.Clear();
            expansionModules.AddRange(expansions);
            KitchenRoot = kitchen;
            DiningRoot = dining;
            ManagementRoot = management;
            CustomerFlowRoot = customerFlow;
            UtilityRoot = utility;
        }

        internal void ConfigureShell(
            Transform shell, Transform entranceAnchor,
            Vector2 deckSize, float deckTopY, float driveThruWindowX, float backWallZ,
            GameObject driveThruBay)
        {
            ShellRoot = shell;
            EntranceAnchor = entranceAnchor;
            DeckSize = deckSize;
            DeckTopY = deckTopY;
            DriveThruWindowX = driveThruWindowX;
            BackWallZ = backWallZ;
            DriveThruWallBay = driveThruBay;
        }
    }

    /// <summary>
    /// Builds the shop as a walled lot standing on the street, plus separately
    /// unlockable plots along its east side. Functional surfaces never scale;
    /// only presentation roots animate when a wing is purchased.
    ///
    /// This used to be a floating island: tapered earth underside, a ring of
    /// clouds and nothing around it. That left the shop with no outside at all,
    /// so an entrance was a doorway from thin air into thin air and passing
    /// traffic had nowhere to pass.
    /// </summary>
    public static class ShopWorldBuilder
    {
        /// <summary>Placeholder shop floor: warm and quiet, so the props read against it.</summary>
        private static readonly Color ShopFloor = new(0.87f, 0.77f, 0.65f);

        private static readonly Color RimColor = new(0.62f, 0.34f, 0.24f);
        private static readonly Color FenceColor = new(0.80f, 0.34f, 0.24f);
        private static readonly Color FencePost = new(0.55f, 0.24f, 0.18f);

        /// <summary>
        /// Panel pitch of the tiled wall kit the shop is built from, and the
        /// height those panels stand. The lot is 22.56 x 16.92, so its two walled
        /// edges come out at exactly 16 and 12 panels with nothing to fudge at the
        /// corner. <see cref="ShawarmaTycoon.EditorTools.PolyPackBuilder"/> scales
        /// the models to this, so the two cannot drift apart.
        /// </summary>
        public const float ShellModule = 1.41f;

        private const float ShellWallHeight = 2.82f;
        private const float ShellWallThickness = 0.353f;

        /// <summary>Floor tiles are laid at twice the wall pitch.</summary>
        private const float FloorTileSpan = ShellModule * 2f;

        /// <summary>How deep a floor tile is, so its top lands on the deck.</summary>
        private const float FloorTileThickness = ShellModule * 0.5f;

        private const string ShellWall = "260_wall_straight";
        private const string ShellWindow = "261_wall_window";

        /// <summary>
        /// The kitchen set's blue tile. "270_floor_tile_warm" is the other one
        /// imported - a black and white check - and it is not used because at this
        /// size it puts a chequerboard under every table and chair in the shop and
        /// nothing standing on it reads. This one is one value, so it does not.
        /// </summary>
        private const string ShellFloor = "269_floor_tile";

        public static DioramaWorld Build(Transform parent, DioramaWorldConfig config)
        {
            config ??= DioramaWorldConfig.CreateRuntimeDefaults();

            GameObject worldObject = new("Restaurant World");
            worldObject.transform.SetParent(parent, false);
            DioramaWorld world = worldObject.AddComponent<DioramaWorld>();
            DioramaWalkableRegistry registry = worldObject.AddComponent<DioramaWalkableRegistry>();

            DioramaModule core = BuildCoreModule(worldObject.transform, registry, config);
            Transform moduleContent = core.ContentRoot;
            Transform kitchen = CreateZone("Kitchen Module", moduleContent);
            Transform dining = CreateZone("Dining Module", moduleContent);
            Transform management = CreateZone("Management Module", moduleContent);
            Transform customerFlow = CreateZone("Customer Flow Module", moduleContent);
            Transform utility = CreateZone("Utility Module", moduleContent);

            List<DioramaModule> expansions = new();
            IReadOnlyList<DioramaWorldConfig.ExpansionDefinition> definitions = config.Expansions;
            for (int i = 0; i < definitions.Count; i++)
                expansions.Add(BuildExpansionModule(worldObject.transform, registry, config, definitions[i]));

            world.Configure(registry, core, expansions, kitchen, dining, management, customerFlow, utility);
            BuildShell(worldObject.transform, world, config);
            return world;
        }

        private static DioramaModule BuildCoreModule(
            Transform parent,
            DioramaWalkableRegistry registry,
            DioramaWorldConfig config)
        {
            GameObject root = new("Restaurant Lot");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = config.CorePosition;

            Vector3 size = new(config.CoreSize.x, config.DeckThickness, config.CoreSize.y);
            GameObject surface = CreateSurface("Lot Walkable Surface", root.transform, size, registry);

            GameObject visual = new("Visual Root");
            visual.transform.SetParent(root.transform, false);
            BuildKerb(visual.transform, config.CoreSize, config.DeckThickness);

            GameObject content = new("Content Root");
            content.transform.SetParent(root.transform, false);

            DioramaModule module = root.AddComponent<DioramaModule>();
            module.Configure(config.CoreId, true, surface.transform, visual.transform,
                content.transform, null, surface.GetComponent<DioramaWalkableSurface>());
            module.SetUnlocked(true, false);
            return module;
        }

        private static DioramaModule BuildExpansionModule(
            Transform parent,
            DioramaWalkableRegistry registry,
            DioramaWorldConfig config,
            DioramaWorldConfig.ExpansionDefinition definition)
        {
            GameObject root = new("Expansion Module " + definition.Id);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = definition.Position;

            Vector3 size = new(definition.Size.x, config.DeckThickness, definition.Size.y);
            GameObject surface = CreateSurface("Expansion Walkable Surface", root.transform, size, registry);

            GameObject visual = new("Visual Root");
            visual.transform.SetParent(root.transform, false);
            BuildKerb(visual.transform, definition.Size, config.DeckThickness);
            BuildPlotBoundary(visual.transform, config, definition);

            GameObject content = new("Content Root");
            content.transform.SetParent(root.transform, false);

            GameObject preview = BuildLockedPreview(root.transform, definition.Size, config.DeckThickness);
            DioramaModule module = root.AddComponent<DioramaModule>();
            module.Configure(definition.Id, false, surface.transform, visual.transform,
                content.transform, preview, surface.GetComponent<DioramaWalkableSurface>());
            module.SetUnlocked(false, false);
            return module;
        }

        /// <summary>
        /// Closes the outside edges of a plot. Only edges with no neighbouring plot
        /// get one, so the boundary follows the grid however it is shaped, and it
        /// hangs off the plot's own visual root so it arrives with the plot rather
        /// than standing over ground nobody has bought.
        ///
        /// Without this the floor simply stopped: the plots reach eight metres
        /// further east than the shell walls do, and everyone who walked out there
        /// walked off the deck into the road.
        ///
        /// The north edge gets a wall because it faces the street and nothing is
        /// behind it. East and south get the shopfront's knee-high fence instead -
        /// the camera sits off the shop's south-east corner, and a 2.82 m wall on
        /// either of those two edges stands between the lens and the floor it is
        /// meant to be showing.
        /// </summary>
        private static void BuildPlotBoundary(
            Transform parent, DioramaWorldConfig config, DioramaWorldConfig.ExpansionDefinition plot)
        {
            float halfX = plot.Size.x * 0.5f;
            float halfZ = plot.Size.y * 0.5f;

            if (!HasNeighbour(config, plot, new Vector3(0f, 0f, plot.Size.y)))
                TileWallRun(parent, config.DeckTopY, -halfX, halfX, halfZ, true, 180f, float.NaN, null);
            if (!HasNeighbour(config, plot, new Vector3(plot.Size.x, 0f, 0f)))
                BuildFenceRun(parent, halfX, -halfZ, halfZ, config.DeckTopY, true);
            if (!HasNeighbour(config, plot, new Vector3(0f, 0f, -plot.Size.y)))
                BuildFenceRun(parent, -halfZ, -halfX, halfX, config.DeckTopY, false);
        }

        private static bool HasNeighbour(
            DioramaWorldConfig config, DioramaWorldConfig.ExpansionDefinition plot, Vector3 offset)
        {
            Vector3 wanted = plot.Position + offset;
            foreach (DioramaWorldConfig.ExpansionDefinition other in config.Expansions)
                if ((other.Position - wanted).sqrMagnitude < 0.01f)
                    return true;
            return false;
        }

        private static GameObject CreateSurface(
            string name,
            Transform parent,
            Vector3 size,
            DioramaWalkableRegistry registry)
        {
            // Keep the functional root at identity scale. Expansion animation
            // and restore code may normalize roots, so storing floor dimensions
            // in transform.localScale can silently shrink a 22 m deck to 1 m.
            GameObject surface = new(name);
            surface.transform.SetParent(parent, false);
            surface.transform.localScale = Vector3.one;

            BoxCollider collider = surface.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = size;

            GameObject fallbackVisual = PrototypeVisuals.CreatePrimitive(
                "Floor", PrimitiveType.Cube, surface.transform,
                Vector3.zero, size, ShopFloor);
            Collider fallbackCollider = fallbackVisual.GetComponent<Collider>();
            if (fallbackCollider != null) fallbackCollider.enabled = false;
            if (TileFloor(surface.transform, size))
                fallbackVisual.GetComponent<Renderer>().enabled = false;

            DioramaWalkableSurface walkable = surface.AddComponent<DioramaWalkableSurface>();
            walkable.Configure(registry, collider);
            return surface;
        }

        /// <summary>
        /// Lays the kitchen floor tile over a deck. Returns false when the model
        /// is missing, which leaves the flat placeholder colour showing.
        ///
        /// The tile is laid at 2.82 m, twice the wall pitch. The last attempt at a
        /// tiled floor stretched one tile across the whole deck and its grout drew
        /// a hard grid over every metre of the shop; at the wall's own 1.41 m this
        /// one would do the same thing with 192 tiles instead of one.
        ///
        /// Tiles are sunk so their top face lands on the deck top. Everything under
        /// that is buried in the deck and the city ground below it.
        /// </summary>
        private static bool TileFloor(Transform surface, Vector3 deckSize)
        {
            if (!CityKit.Has(ShellFloor)) return false;

            int columns = Mathf.Max(1, Mathf.RoundToInt(deckSize.x / FloorTileSpan));
            int rows = Mathf.Max(1, Mathf.RoundToInt(deckSize.z / FloorTileSpan));
            float stepX = deckSize.x / columns;
            float stepZ = deckSize.z / rows;
            float y = deckSize.y * 0.5f - FloorTileThickness;

            GameObject tiles = new("Floor Tiles");
            tiles.transform.SetParent(surface, false);
            for (int column = 0; column < columns; column++)
            for (int row = 0; row < rows; row++)
            {
                CityKit.Spawn(ShellFloor, tiles.transform, new Vector3(
                    -deckSize.x * 0.5f + stepX * (column + 0.5f),
                    y,
                    -deckSize.z * 0.5f + stepZ * (row + 0.5f)));
            }
            return true;
        }

        /// <summary>
        /// Kerb around the deck edge. On the grounded lot this is what reads as
        /// the property line where there is no wall - the pavement outside is
        /// laid level with the floor so people walking in never step up.
        /// </summary>
        private static void BuildKerb(Transform parent, Vector2 size, float deckThickness)
        {
            float y = deckThickness * 0.20f;
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;
            CreateVisualBar("North Kerb", parent, new Vector3(0f, y, halfZ), new Vector3(size.x + 0.20f, 0.14f, 0.18f));
            CreateVisualBar("South Kerb", parent, new Vector3(0f, y, -halfZ), new Vector3(size.x + 0.20f, 0.14f, 0.18f));
            CreateVisualBar("West Kerb", parent, new Vector3(-halfX, y, 0f), new Vector3(0.18f, 0.14f, size.y));
            CreateVisualBar("East Kerb", parent, new Vector3(halfX, y, 0f), new Vector3(0.18f, 0.14f, size.y));
        }

        private static GameObject BuildLockedPreview(Transform parent, Vector2 size, float deckThickness)
        {
            GameObject preview = new("Locked Preview");
            preview.transform.SetParent(parent, false);

            GameObject ghost = PrototypeVisuals.CreatePrimitive(
                "Locked Ghost Deck", PrimitiveType.Cube, preview.transform,
                new Vector3(0f, deckThickness * 0.5f - 0.04f, 0f),
                new Vector3(size.x * 0.96f, 0.08f, size.y * 0.96f),
                new Color(0.42f, 0.37f, 0.34f));
            Collider collider = ghost.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            // Ghost of the tables the plot would add, so the pad beside it does
            // not have to say in words what the money buys.
            foreach (float x in new[] { -1.4f, 1.4f })
            {
                GameObject ghostTable = PrototypeVisuals.CreatePrimitive(
                    "Ghost Table", PrimitiveType.Cube, preview.transform,
                    new Vector3(x, deckThickness * 0.5f + 0.36f, 0f),
                    new Vector3(1.2f, 0.72f, 1.9f), new Color(0.56f, 0.51f, 0.47f));
                Collider ghostCollider = ghostTable.GetComponent<Collider>();
                if (ghostCollider != null) ghostCollider.enabled = false;
            }
            return preview;
        }

        private static void CreateVisualBar(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject bar = PrototypeVisuals.CreatePrimitive(
                name, PrimitiveType.Cube, parent, position, scale, RimColor);
            Collider collider = bar.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        // --- the shell: what makes an inside and an outside --------------------

        /// <summary>
        /// Walls along the two edges the camera looks past, a low fence and a gate
        /// along the edge it looks over.
        ///
        /// The rig sits at +X / -Z, so a full height wall on those two edges would
        /// stand between the lens and the shop floor. The front gets a knee high
        /// fence instead, which still draws the property line, and the gate is the
        /// one tall thing there because it is the thing the player has to read.
        /// </summary>
        private static void BuildShell(
            Transform parent, DioramaWorld world, DioramaWorldConfig config)
        {
            GameObject shell = new("Shop Shell");
            shell.transform.SetParent(parent, false);

            Vector2 size = config.CoreSize;
            float deckTop = config.DeckTopY;
            float back = size.y * 0.5f - 0.185f;
            float west = -size.x * 0.5f + 0.185f;
            float front = -size.y * 0.5f + 0.12f;

            // The window wants the westernmost full bay, clear of the kitchen line.
            // Where it actually lands is decided by the wall tiling, so the counter
            // and the opening cannot drift apart.
            float windowX = BuildWalls(
                shell.transform, size, deckTop, back, west,
                -size.x * 0.5f + ShellModule * 2.2f, out GameObject bay);
            Transform entrance = BuildFenceAndGate(shell.transform, size, deckTop, front);

            world.ConfigureShell(shell.transform, entrance, size, deckTop, windowX, back, bay);
        }

        /// <summary>
        /// Walls along the back and west edges, panelled end to end. Returns the
        /// centre of the bay left open for the drive-through window.
        ///
        /// The two runs simply cross at the corner. The kit does ship a corner
        /// piece, but a panel is only 35 cm thick and both runs span their whole
        /// edge, so they already overlap there and a third piece on top of them
        /// would only add a seam to look at.
        /// </summary>
        private static float BuildWalls(
            Transform shell, Vector2 size, float deckTop, float back, float west,
            float preferredWindowX, out GameObject bay)
        {
            bay = null;
            if (!CityKit.Has(ShellWall)) return preferredWindowX;

            GameObject walls = new("Perimeter Walls");
            walls.transform.SetParent(shell, false);

            // The bay is built like any other panel, into its own root, and taken
            // down when the drive-through is bought.
            GameObject bayRoot = new("Drive-Thru Wall Bay");
            bayRoot.transform.SetParent(shell, false);
            bay = bayRoot;

            // Only the panels' +Z face carries the tiling and the window; the other
            // side is flat colour. Both runs are turned so that face looks into the
            // shop - a half turn on the back run, a quarter on the west.
            float windowX = TileWallRun(
                walls.transform, deckTop, -size.x * 0.5f, size.x * 0.5f, back,
                true, 180f, preferredWindowX, bayRoot.transform);
            TileWallRun(
                walls.transform, deckTop, -size.y * 0.5f, size.y * 0.5f, west,
                false, 90f, float.NaN, null);
            return windowX;
        }

        /// <summary>
        /// Fills one edge between two coordinates. Pieces are spaced to divide the
        /// run exactly rather than at their nominal pitch, so the last one lands on
        /// the far edge; on the lot's own two runs that division is exact anyway.
        /// One bay can be parented off to <paramref name="bayParent"/> instead, so
        /// it can be taken down later; the returned value is where that bay's
        /// centre ended up.
        /// </summary>
        private static float TileWallRun(
            Transform parent, float deckTop, float from, float to, float fixedCoordinate,
            bool alongX, float yaw, float openingNear, Transform bayParent)
        {
            float span = to - from;
            int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(span) / ShellModule));
            float step = span / count;

            int bayIndex = -1;
            if (!float.IsNaN(openingNear))
                bayIndex = Mathf.Clamp(
                    Mathf.RoundToInt((openingNear - from) / step - 0.5f), 0, count - 1);

            float bayCentre = from + step * (bayIndex + 0.5f);
            for (int i = 0; i < count; i++)
            {
                float p = from + step * (i + 0.5f);
                Vector3 position = alongX
                    ? new Vector3(p, deckTop, fixedCoordinate)
                    : new Vector3(fixedCoordinate, deckTop, p);
                Transform target = i == bayIndex && bayParent != null ? bayParent : parent;
                SpawnWall(target, position, yaw, !alongX, IsGlazed(i, bayIndex));
            }
            return bayCentre;
        }

        /// <summary>
        /// Which panels of a run are glazed. Every third one, plus both neighbours
        /// of the drive-through bay: a 2.82 m wall hides everything within about
        /// two and a half metres behind it, and the service lane is only two
        /// metres out, so without glass either side of the bay the car being
        /// served would be behind a blank wall.
        /// </summary>
        private static bool IsGlazed(int index, int bayIndex)
        {
            if (index == bayIndex) return false;
            return index % 3 == 1 || (bayIndex >= 0 && Mathf.Abs(index - bayIndex) == 1);
        }

        /// <summary>
        /// One wall panel plus the blocker that makes it solid. The models arrive
        /// with their colliders switched off, so without one the wall is a picture
        /// and the player walks straight through it into the street.
        /// </summary>
        private static void SpawnWall(
            Transform parent, Vector3 position, float yaw, bool alongZ, bool glazed)
        {
            CityKit.Spawn(glazed ? ShellWindow : ShellWall, parent, position, yaw);

            GameObject blocker = new("Wall Blocker");
            blocker.transform.SetParent(parent, false);
            blocker.transform.localPosition = position + Vector3.up * (ShellWallHeight * 0.5f);
            blocker.AddComponent<BoxCollider>().size = alongZ
                ? new Vector3(ShellWallThickness, ShellWallHeight, ShellModule)
                : new Vector3(ShellModule, ShellWallHeight, ShellWallThickness);
        }

        /// <summary>
        /// Front fence with the gate in it, and the anchor a customer aims for
        /// once they are through. Returns that anchor.
        /// </summary>
        private static Transform BuildFenceAndGate(
            Transform shell, Vector2 size, float deckTop, float front)
        {
            GameObject boundary = new("Front Boundary");
            boundary.transform.SetParent(shell, false);

            float halfX = size.x * 0.5f;
            const float gateHalfWidth = 1.6f;
            // The storefront is the visual centre of the restaurant. Keeping its
            // gate on that same centre line makes the first approach legible and
            // leaves equal fence runs on either side.
            const float gateX = 0f;

            BuildFenceRun(boundary.transform, front, -halfX, gateX - gateHalfWidth, deckTop, false);
            BuildFenceRun(boundary.transform, front, gateX + gateHalfWidth, halfX, deckTop, false);

            GameObject gate = new("Entrance Gate");
            gate.transform.SetParent(boundary.transform, false);
            gate.transform.localPosition = new Vector3(gateX, deckTop, front);

            Color frame = new(0.92f, 0.34f, 0.20f);
            PrototypeVisuals.CreatePrimitive("Entrance Top", PrimitiveType.Cube, gate.transform,
                new Vector3(0f, 1.72f, 0f), new Vector3(2.6f, 0.24f, 0.25f), frame);
            PrototypeVisuals.CreatePrimitive("Entrance Left", PrimitiveType.Cube, gate.transform,
                new Vector3(-1.18f, 0.82f, 0f), new Vector3(0.22f, 1.65f, 0.25f), frame);
            PrototypeVisuals.CreatePrimitive("Entrance Right", PrimitiveType.Cube, gate.transform,
                new Vector3(1.18f, 0.82f, 0f), new Vector3(0.22f, 1.65f, 0.25f), frame);
            // The authored piece is a shopfront: taupe wall behind, glazed door and
            // terracotta trim on its +Z face. Turned to face the street, or the
            // customers walk in through the blank back of it.
            MeshyVisuals.TryReplaceDirectAuthored(gate.transform, "21_entrance_door",
                Vector3.zero, new Vector3(0f, 180f, 0f),
                "Entrance Top", "Entrance Left", "Entrance Right");

            // Hung off the unrotated boundary rather than the gate, so it stays on
            // the shop side of the door whichever way the door is turned.
            GameObject anchor = new("Entrance Anchor");
            anchor.transform.SetParent(boundary.transform, false);
            anchor.transform.localPosition = new Vector3(gateX, deckTop, front + 1.1f);
            return anchor.transform;
        }

        /// <summary>
        /// Knee high railing along one edge, and the blocker that makes it solid.
        /// <paramref name="alongZ"/> turns it a quarter turn for a side edge.
        ///
        /// It stops people as surely as a wall does - a railing you can see over is
        /// the point on the two edges the camera looks past - so it carries a
        /// collider even though it is only knee high.
        /// </summary>
        private static void BuildFenceRun(
            Transform parent, float fixedCoordinate, float from, float to, float deckTop, bool alongZ)
        {
            float span = to - from;
            if (span <= 0.2f) return;

            GameObject run = new("Fence Run");
            run.transform.SetParent(parent, false);
            float middle = (from + to) * 0.5f;
            run.transform.localPosition = alongZ
                ? new Vector3(fixedCoordinate, deckTop, middle)
                : new Vector3(middle, deckTop, fixedCoordinate);
            if (alongZ) run.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

            PrototypeVisuals.CreatePrimitive("Rail", PrimitiveType.Cube, run.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(span, 0.12f, 0.14f), FenceColor);
            PrototypeVisuals.CreatePrimitive("Skirt", PrimitiveType.Cube, run.transform,
                new Vector3(0f, 0.16f, 0f), new Vector3(span, 0.20f, 0.16f), FencePost);

            int posts = Mathf.Max(2, Mathf.RoundToInt(span / 2.4f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float x = -span * 0.5f + span * i / (posts - 1);
                PrototypeVisuals.CreatePrimitive("Post", PrimitiveType.Cube, run.transform,
                    new Vector3(x, 0.34f, 0f), new Vector3(0.16f, 0.68f, 0.20f), FencePost);
            }

            GameObject blocker = new("Fence Blocker");
            blocker.transform.SetParent(run.transform, false);
            blocker.transform.localPosition = Vector3.up * 0.6f;
            blocker.AddComponent<BoxCollider>().size = new Vector3(span, 1.2f, 0.3f);
        }

        private static Transform CreateZone(string name, Transform parent)
        {
            GameObject zone = new(name);
            zone.transform.SetParent(parent, false);
            return zone.transform;
        }
    }
}
