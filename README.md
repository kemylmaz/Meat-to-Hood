# Shawarma Tycoon

Shawarma Tycoon is a mobile-first restaurant-management prototype built in Unity. Players run a stylized shawarma shop, move ingredients through a production chain, serve dine-in and takeaway customers, hire staff, unlock automation, and expand the business.

> Development status: private Unity prototype. The core loop is implemented; runtime stabilization, balancing, and build validation are still in progress.

![The shop mid-service: production line, dining room, management office and the street outside](Docs/shop-overview.png)

## Core gameplay

- Process `Raw Meat -> Cooked Meat -> Wrap` across a four-station line: rack, spit, carving board, till — a steak off the rack, cut into cubes at the spit, wrapped at the board
- Stations work unattended once fed, so the job is carrying batches between them
- Read each customer's order from the bubble over their head and fill it from the counters
- Sell drinks from a fridge you restock by hand, and desserts from a bakery oven
- Carry ingredients, meals, and dirty plates with capacity-based inventory rules
- Manage customer queues, patience, VIP guests, tables, and cleaning
- Open a drive-through window and serve the cars that queue in the lane outside
- Pack whole bags — wrap, dessert and drink in one order — at the courier bay
- Build combos and capitalize on rush-hour multipliers
- Upgrade the player, stations, staff, conveyors, and the two management offices
- Recruit a cashier, a drive-through cashier, a runner, and two bussers
- Expand the restaurant across six unlockable plots along the lot's east side, two tables to a plot
- Watch one bar at the top of the screen creep toward 100% as the shop gets built out
- Earn offline income through local session persistence

## Mobile-first UX

- Floating touch joystick
- Orthographic follow camera with pinch zoom
- Safe-area-aware UI
- Touch-friendly HUD, objectives, tasks, and upgrade panels
- Local save/autosave flow designed for mobile and WebGL-compatible persistence

## Technology

- Unity `6000.3.11f1`
- C#
- Universal Render Pipeline `17.3.0`
- Unity Input System `1.19.0`
- Unity AI Navigation
- Git LFS for large art assets

## Getting started

**Git LFS is required.** Models and reference art are stored in LFS; a clone without it produces pointer files and a scene full of grey placeholder boxes.

```bash
git lfs install
git clone https://github.com/kemylmaz/shawarmatycoon.git
```

Open the project and load `Assets/ShawarmaTycoon/Scenes/ShawarmaTycoonPrototype.unity` — the only scene in the build settings — then press Play.

`Shawarma Tycoon → Reset Save Progress` in the menu bar clears coins, records, unlocks, and hired staff. Progress is stored in a versioned local save document and persists between sessions.

## Placing props by hand

The saved scene holds a camera, a light and the bootstrap and nothing else — the shop is assembled at runtime — so opening it gives you an empty Scene view with no floor to drop a chair onto. `Shawarma Tycoon → Scene → Build World Preview` builds the same world the game builds, as ordinary editable objects, to place against. It ships without a key binding — Unity already owns the combinations worth having — but it appears in `Edit → Shortcuts` under `Main Menu/` if you want one. Prefabs to drag in live in `Assets/ShawarmaTycoon/Resources/PolyPrefabs`.

The preview is throwaway: it is torn down before the scene is saved and before play starts, so it can neither be committed by accident nor stand in the live world. What survives is whatever you put under the **`El Yerleşimi`** root, which is a normal saved object the runtime build leaves alone — anything parented into the preview instead goes when the preview goes. Outside play mode the build stops at the world: the HUD, the camera rig and the input are skipped rather than half-built, because they need MonoBehaviour lifecycle callbacks that only run in play mode.

**Keeping an edit you made to the preview.** Turning a generated building to face a better way does nothing on its own — the preview is rebuilt from code and discarded at the next save. Select what you changed and run `Shawarma Tycoon → Scene → Keep Selection`: it lifts it out of the preview and into `El Yerleşimi`, keeping its world position, and stamps it with a `HandPlacedPart` saying which part of the world it is. That stamp is what stops `CityBlock` producing its own copy, so there is no checkbox to forget and no way to end up with two skylines standing in each other.

Selecting inside a generated group keeps the **whole group**, not the one object picked. Half a group cannot be made to work: the builder makes its groups whole or not at all, so keeping three buildings out of twelve would leave you choosing between three duplicates and nine missing ones. `HandPlacedWorld` on the root carries the same flags by hand, for a part built from scratch rather than kept out of a preview.

## Project architecture

The main scene stays intentionally lightweight. `ShawarmaPrototypeBootstrap.cs` assembles the prototype at runtime, while feature code is grouped by responsibility:

- `Assets/ShawarmaTycoon/Scripts/Core` - economy, progression, combos, rewards, and persistence
- `Assets/ShawarmaTycoon/Scripts/Stations` - production stations and worker operation
- `Assets/ShawarmaTycoon/Scripts/Customers` - queues, tables, takeaway, and cleaning
- `Assets/ShawarmaTycoon/Scripts/Player` - movement, camera, and carry inventory
- `Assets/ShawarmaTycoon/Scripts/World` - upgrades, staff, expansion, visuals, and traffic
- `Assets/ShawarmaTycoon/Scripts/UI` - HUD, safe area, tasks, and touch controls

Two conventions are worth knowing before editing:

- **Models are resolved by catalog id.** Approved CozyPack models are placed at their authored 1:1 metre scale and use their exported interaction anchors; the one remaining legacy Meshy rack still uses target-box fitting. Missing art falls back to primitive collision-safe visuals.
- **There are two model packs, imported by two builders.** `CozyPackBuilder` handles the project's own Blender exports, which carry three named LOD meshes, a `FRONT_DIRECTION` anchor and material names that map onto a hand-picked palette. `PolyPackBuilder` handles the downloaded [Poly Pizza](https://poly.pizza) bundles under `Art/PolyPack` — the city kit, the restaurant and kitchen sets, the food kit and the animated crowd — which have none of that. Those bundles were authored at four unrelated scales, so each model states one real-world measurement and the builder solves for the uniform scale, lands the pivot at bottom-centre and unpacks the palette atlas embedded in the FBX. Both write id-addressed prefabs into `Resources`, and `MeshyVisuals`/`CityKit` search the authored pack first.
- **The UI is generated at runtime too.** `UIFactory` builds its own nine-sliced sprites and uses the built-in legacy font, so there is no TextMeshPro import step and no HUD prefab to keep in sync.

The world is assembled as `Restaurant World/Restaurant Lot` plus a grid of six independently unlockable `DioramaModule` plots along the east side, two columns of three covering the lot's full depth. Each module separates its always-full-size walkable surface, animated visual root, gameplay content root, and non-walkable locked preview. `DioramaWalkableRegistry` is the only ground authority used by the player, so tables, counters, previews and the street outside cannot be mistaken for walkable floor.

The lot stands on a street rather than floating: `ShopWorldBuilder` raises walls along the two edges the camera looks past and a knee-high fence with a shopfront gate along the edge it looks over, and `CityBlock` lays the pavement, the driveway, the road and the skyline around it. The pavement is level with the shop floor on purpose — customers keep the height they spawn at, so a step at the gate would leave half the queue sunk into the tiles. Everything the player has not bought yet is absent rather than shown greyed out: no belt stands in the kitchen, no desk stands in an office, and no car uses the lane past a drive-through window that is not there.

The shell is built from a modular tiled kit on a 1.41 m panel pitch — the lot is 22.56 × 16.92, so its two walled edges come out at exactly 16 and 12 panels with nothing to fudge at the corner. Only the panels' +Z face carries the tiling and the window, so each run is turned to put that face inside. Every third panel is glazed, plus both neighbours of the drive-through bay: a 2.82 m wall hides everything within about two and a half metres behind it and the service lane is only two metres out, so without glass either side of the bay the car being served would be standing behind a blank wall. Floor tiles are laid at twice the wall pitch; at the wall's own pitch the grout drew a hard grid over every metre of the shop, which is what got the previous tiled floor taken out again.

The two management offices are built into the south-west corner, sharing a wall and using the lot's own perimeter wall as the far side of the first. The shop floor is a flat placeholder colour rather than a tiled model: stretched to the deck's tile field, the authored floor drew a hard grid over every metre of the shop and nothing else could be read against it.

Customers arrive with a `CustomerOrder` rolled from what the shop actually sells, so nobody asks for a drink before the fridge is bought. An order that cannot be filled is held at the front of the queue until its owner's patience runs out, at which point they give up on the extras and take the wrap for a smaller bill — an empty fridge costs the shop money without deadlocking the line. The fridge, the dessert oven, the courier bay and the four bought decorations now use Poly Pizza models, and the crowd is drawn from eight animated bodies rather than three; the player and the hired staff deliberately stay on the authored pack, so the shop's own people read as a uniformed set against customers dressed at random. The courier's scooter is still placeholder geometry — no bundle has one — and every replaced piece keeps its primitive group behind a null check, so missing art degrades rather than breaks.

`UpgradeProgress` adds every purchasable step in the game into the single percentage on the HUD. Owners register what they sell and how to read their own level, so adding a pad moves the denominator on its own.

## Art pipeline

`ArtSource/BlenderGenerated/scripts/` is a procedural modelling library. Running `st_build.py` inside Blender rebuilds all 40 assets from scratch: meshes, materials, LOD0/1/2, anchor empties, character rigs with idle, walk, and carry-walk clips, preview renders, and FBX exports that are re-imported into an empty file to verify they round-trip.

Twenty-eight of the models were authored to match the reference sheets in `ArtSource/References/assetlist/`; the remainder are a modular street kit and the first benchmark props.

Rendered previews of the full library:

| Sheet | Contents |
| --- | --- |
| [Characters](ArtSource/BlenderGenerated/Previews/_contact_sheet_cozy2.png) | Player, workers, customer variants |
| [Stations](ArtSource/BlenderGenerated/Previews/_contact_sheet_stations.png) | Production line and manager desks |
| [Props](ArtSource/BlenderGenerated/Previews/_contact_sheet_props.png) | Tables, pads, trays, walls, floors |
| [City kit](ArtSource/BlenderGenerated/Previews/_contact_sheet_city.png) | Roads, pavements, buildings, vehicles |

The house rules the library enforces — Z-up, characters facing local −Y, origin at bottom-centre, ground contact at Z=0, per-class triangle budgets, no baked text or logos — are documented with measured results in [`PHASE1_REPORT.md`](ArtSource/BlenderGenerated/PHASE1_REPORT.md) and [`PHASE2A_CITYKIT.md`](ArtSource/BlenderGenerated/PHASE2A_CITYKIT.md).

## Balance

The figures below are measured rather than estimated. `Assets/ShawarmaTycoon/Editor/EconomyProbe.cs` drives the player around the carry loop — fill up at the rack, walk the line, collect the till, clear a table, repeat — and samples revenue every thirty seconds. Runs are five to eight minutes each, from a clean save.

| Shop | Income | Notes |
| --- | ---: | --- |
| Hand-carried, nothing bought | ~50 /min | two runs gave 42 and 65; variance is wide |
| Three belts and one table wing | ~120 /min | line runs itself, player collects and busses |
| Everything bought | ~318 /min | drive-through, courier and both extra counters live |

The probe is a **floor**, not a target: it walks a whole cook cycle before collecting any money, where a player picks the cash up on the way past. Every price in the game lives in `ShopPrices.cs` and is set against these three numbers.

| | Cost | Share |
| --- | ---: | ---: |
| Belts, workers, tables, offices, drive-through, fridge, dessert oven, courier, hires | 11,055 | 62% |
| The two upgrade boards, taken to level 5 | 6,852 | 38% |
| **Everything** | **17,907** | ~114 min |

The first pass cost 49,000 against the same income — over four hours — and four fifths of it was the two upgrade boards, which sell multipliers rather than anything the player can see appear. `Prices_StayInStepWithMeasuredIncome` holds the shape: a first purchase reachable in the opening two minutes, a completion between half an hour and two, and boards that cannot cost more than twice the shop they upgrade.

Widening the lot to six plots turned up the most useful thing the probe has said so far: **a bigger floor earns no more money**. Fully built, the shop measures 318 /min with eighteen covers and measured 310 /min with ten. What caps it is how fast the kitchen line turns meat into wraps, not how many people can sit down. So seating is priced as capacity the player wants rather than as the thing that pays for itself: a plot is sold whole, two tables at a time, which reaches eighteen covers on a ten step ladder. Pricing eighteen tables individually put buying the shop out at 242 minutes, twice the ceiling.

Two other things had to move with the prices. The shop now **opens prepped** — a few wraps on the counter and part-cooked stock behind it — because the queue arrives within seconds of the first frame while a cold line takes ninety seconds to produce anything, and that opening minute earned exactly nothing. And the binding constraint early on is **tables, not food**: with four tables and nobody bussing, the counter fills with wraps while the queue stands there because there is nowhere to seat anyone, so the table wing and the first busser are priced to be reachable within the first few minutes.

Three constants have to agree with the achievable service rate or the game breaks in ways that are hard to see: queue length against patience, the full-price window against how long the back of the queue waits, and the combo timeout against the gap between customers. They are commented in place — changing one means checking the other two.

## Current development priorities

- Play the rebalanced curve by hand — the probe is a floor, and a real player's pace is still unmeasured
- Validate the modular plot unlock/restore flow on physical devices
- Validate Android and WebGL builds on target devices and browsers
- Balance offline income and the later upgrade tiers
- Replace the remaining legacy meat-rack visual and complete animation/audio polish
- Add automated tests for progression, persistence, and reward calculations
- Audit third-party asset licenses and add root `LICENSE` / `CREDITS` files
- Expand the screenshot set once the layout pass is finished

## Ownership and licensing

Created by **Kemal Yılmaz / Poppanda Interactive**.

No open-source license has been granted yet. Until a root `LICENSE` file is added, the project source and original assets are all rights reserved. Third-party assets remain subject to their respective licenses.
