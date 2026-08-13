# Shawarma Tycoon

Shawarma Tycoon is a mobile-first restaurant-management prototype built in Unity. Players run a stylized shawarma shop, move ingredients through a production chain, serve dine-in and takeaway customers, hire staff, unlock automation, and expand the business.

> Development status: private Unity prototype. The core loop is implemented; runtime stabilization, balancing, and build validation are still in progress.

![The shop mid-service: production line, dining room, management office and the street outside](Docs/shop-overview.png)

## Core gameplay

- Process `Raw Meat -> Cooked Meat -> Sliced Meat -> Wrap`
- Carry ingredients, meals, and dirty plates with capacity-based inventory rules
- Manage customer queues, patience, VIP guests, tables, and cleaning
- Fulfill takeaway orders before they expire
- Build combos and capitalize on rush-hour multipliers
- Upgrade the player, stations, staff, conveyors, and the management office
- Recruit cashiers, cleaners, runners, and production workers
- Expand the restaurant diorama and surrounding city block
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

`Shawarma Tycoon → Reset Save Progress` in the menu bar clears coins, records, unlocks, and hired staff. Progress lives in `PlayerPrefs` and persists between sessions.

## Project architecture

The main scene stays intentionally lightweight. `ShawarmaPrototypeBootstrap.cs` assembles the prototype at runtime, while feature code is grouped by responsibility:

- `Assets/ShawarmaTycoon/Scripts/Core` - economy, progression, combos, rewards, and persistence
- `Assets/ShawarmaTycoon/Scripts/Stations` - production stations and worker operation
- `Assets/ShawarmaTycoon/Scripts/Customers` - queues, tables, takeaway, and cleaning
- `Assets/ShawarmaTycoon/Scripts/Player` - movement, camera, and carry inventory
- `Assets/ShawarmaTycoon/Scripts/World` - upgrades, staff, expansion, visuals, and traffic
- `Assets/ShawarmaTycoon/Scripts/UI` - HUD, safe area, tasks, and touch controls

Two conventions are worth knowing before editing:

- **Models are resolved by name, not by reference.** `MeshyVisuals` fits a model into a target box for gameplay objects; `CityKit` places modular street pieces at 1:1 so tile pitch survives. Both fall back to primitives when a model is missing, so the project still runs with an empty `Resources` folder.
- **The UI is generated at runtime too.** `UIFactory` builds its own nine-sliced sprites and uses the built-in legacy font, so there is no TextMeshPro import step and no HUD prefab to keep in sync.

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

The figures below are measured rather than estimated: a scripted near-optimal player was run against the build from a clean save, and the pacing constants were set from what it could actually achieve.

| Time | Served/min | Customers kept | Income |
| ---: | ---: | ---: | ---: |
| 1.4 min | 4.4 | 86% | 50 /min |
| 4.4 min | 5.5 | 80% | 90 /min |
| 6.8 min | 7.5 | 89% | 258 /min |
| 9.4 min | 10.0 | 94% | 374 /min |

By nine minutes that run had bought all seven station upgrades, the dining expansion, and both the cleaner and the cashier.

Three constants have to agree with the achievable service rate or the game breaks in ways that are hard to see: queue length against patience, the full-price window against how long the back of the queue waits, and the combo timeout against the gap between customers. They are commented in place — changing one means checking the other two.

## Current development priorities

- Stabilize the management-menu refresh path and remove repeated runtime exceptions
- Validate Android and WebGL builds on target devices and browsers
- Balance offline income and the later upgrade tiers
- Replace remaining fallback visuals and complete animation/audio polish
- Add automated tests for progression, persistence, and reward calculations
- Audit third-party asset licenses and add root `LICENSE` / `CREDITS` files
- Expand the screenshot set once the layout pass is finished

## Ownership and licensing

Created by **Kemal Yılmaz / Poppanda Interactive**.

No open-source license has been granted yet. Until a root `LICENSE` file is added, the project source and original assets are all rights reserved. Third-party assets remain subject to their respective licenses.
