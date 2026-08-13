# Shawarma Tycoon

A mobile restaurant-management prototype built in Unity 6. You run a shawarma
shop on a city corner: carry raw meat down the line, wrap it, get it to the
counter before the queue gives up, clear the tables, and spend the takings on
staff and conveyors until the shop runs itself.

Everything you see is generated. The world is built from code at runtime, and
all 40 models in it were authored procedurally in Blender — 28 of them drawn
to match the reference sheets in `ArtSource/References/assetlist/`, the rest
a street kit and the first benchmark props. There are no hand-placed scene
objects beyond a camera, a light and a bootstrap component.

---

## Running it

- **Unity 6000.3.11f1** (URP 17.3, new Input System)
- **Git LFS is required.** The models and reference art are stored in LFS; a
  clone without it gives you pointer files and a scene full of grey boxes.

```bash
git lfs install
git clone https://github.com/kemylmaz/shawarmatycoon.git
```

Open the project, then open `Assets/ShawarmaTycoon/Scenes/ShawarmaTycoonPrototype.unity`
and press Play. It is the only scene in the build settings.

`Shawarma Tycoon → Reset Save Progress` in the menu bar wipes coins, records,
unlocks and hired staff. Progress lives in `PlayerPrefs`, so it survives
between sessions.

---

## How it is put together

The scene is close to empty on purpose. `ShawarmaPrototypeBootstrap.Awake()`
builds the city block, the kitchen line, the dining room, the management
office, the HUD and every system, in code. Nothing is wired through the
inspector, so a change to the layout is a change to one file rather than a
merge conflict in a scene.

```
Assets/ShawarmaTycoon/
  Scripts/
    Core/       economy, progress, combo, rush hour, reward maths
    Player/     movement, camera rig, carry inventory
    Stations/   the production line
    Customers/  queue, tables, takeaway window, floor spills
    World/      city, traffic, conveyors, upgrade pads, recruitment, HR
    UI/         runtime uGUI: HUD, joystick, panels, objective banner
    Audio/      synthesised SFX - no audio files ship with the project
  Editor/       cozy pack builder, save reset menu
  Resources/    model prefabs, loaded by name at runtime
  Scenes/       the one scene
```

Two details worth knowing before editing:

**Models are looked up by name, never by reference.** `MeshyVisuals` fits a
model into a target box for gameplay objects; `CityKit` places modular pieces
at 1:1 so tile pitch survives. Both fall back to primitives when a model is
missing, so the game runs with an empty `Resources` folder.

**The UI is built at runtime too.** `UIFactory` generates its own nine-sliced
sprites and uses the built-in legacy font, so there is no TMP import step and
no prefab to keep in sync.

---

## The art pipeline

`ArtSource/BlenderGenerated/scripts/` is a small procedural modelling library.
Running `st_build.py` inside Blender rebuilds all 40 assets from scratch:
meshes, materials, LOD0/1/2, anchor empties, character rigs with idle, walk
and carry-walk clips, preview renders, and FBX exports that are re-imported
into an empty file to prove they round-trip.

The house rules the library enforces — Z-up, characters facing local −Y,
origin at bottom-centre, ground contact at Z=0, triangle budgets per asset
class, no baked text or logos — are documented with the measured results in
[`PHASE1_REPORT.md`](ArtSource/BlenderGenerated/PHASE1_REPORT.md) and
[`PHASE2A_CITYKIT.md`](ArtSource/BlenderGenerated/PHASE2A_CITYKIT.md).

`ArtSource/References/assetlist/` holds the reference sheets every model was
matched against.

---

## The game loop

Raw meat → oven → cutting board → wrap counter → service till. A processing
station only runs while you stand at it, until you hire a worker for it;
conveyors move goods between stations once you buy them.

Customers queue, take a table, eat, and leave the table dirty and the cash on
its pad. Both need collecting. Dirty tables block seating, which is the main
thing that stops a busy shop dead.

Money goes on station workers, conveyor belts, dining-room expansion, three
hired assistants (cashier, cleaner, runner) and the shared HR upgrades. Rush
hour doubles income and makes everyone less patient.

### Balance

The numbers below are measured, not estimated: a scripted near-optimal player
was run against the build from a clean save, and the pacing thresholds were
set from what it could actually achieve.

| Time | Served/min | Customers kept | Income |
|-----:|-----------:|---------------:|-------:|
| 1.4 min | 4.4 | 86% | 50 /min |
| 4.4 min | 5.5 | 80% | 90 /min |
| 6.8 min | 7.5 | 89% | 258 /min |
| 9.4 min | 10.0 | 94% | 374 /min |

By nine minutes that run had bought all seven station upgrades, the dining
expansion, and the cleaner and cashier.

Three constants have to agree with the service rate or the game breaks in ways
that are hard to see: queue length against patience, the full-price window
against how long the back of the queue waits, and the combo timeout against
the gap between customers. They are commented in place — if you change one,
check the other two.

---

## Status

A working prototype: the full loop, progression and economy run end to end.
Not shipped, not content-complete, and the layout is still being tuned.
