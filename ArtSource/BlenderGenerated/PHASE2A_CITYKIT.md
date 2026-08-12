# Shawarma Tycoon — Phase 2A City Kit

Seven street pieces that ground the restaurant in a real block, authored with the
same conventions as Phase 1: Z up, meters, front faces local **−Y**, origin
bottom-centre, ground contact at **Z = 0**, three LODs, palette materials.

Source: `scripts/st_city.py` · master file: `ShawarmaTycoon_CozyPack.blend`
Exports: `ReviewFBX/4*.fbx` · Unity models: `Assets/ShawarmaTycoon/Art/BlenderPhase1/Models/`

---

## Assets

| Asset | Dimensions X×Y×Z (m) | LOD0 / LOD1 / LOD2 | Mats | Anchors |
|---|---|---|---|---|
| 40_road_straight | 4.00 × 7.20 × 0.14 | 756 / 378 / 150 | 3 | TILE_NEXT, LANE_NEAR, LANE_FAR |
| 41_sidewalk_straight | 4.00 × 2.60 × 0.22 | 1 080 / 540 / 216 | 3 | TILE_NEXT, WALK_SURFACE |
| 42_sidewalk_corner | 2.60 × 2.60 × 0.22 | 936 / 468 / 186 | 3 | — |
| 43_city_building_a | 8.30 × 6.42 × 11.09 | 2 792 / 1 396 / 558 | 6 | FACADE_CENTER |
| 44_city_building_b | 8.50 × 5.90 × 17.49 | 4 212 / 2 106 / 842 | 4 | FACADE_CENTER |
| 45_street_lamp | 0.64 × 1.43 × 4.76 | 792 / 396 / 158 | 3 | LIGHT_ANCHOR |
| 46_city_car | 2.02 × 4.30 × 1.59 | 3 368 / 1 684 / 672 | 6 | DRIVER_WINDOW, EXHAUST |

Every asset also carries `<name>_ROOT` and `FRONT_DIRECTION (0,-1,0)`.
LOD1 = 50 % of LOD0, LOD2 = 20 %, exactly.

## Tiling contract

* Road and pavement tiles repeat on a **4 m pitch along X**. One row of road
  tiles is a complete two-lane street: the tile spans both lanes in Y.
* `LANE_NEAR` / `LANE_FAR` mark the two driving lines. `CityLayout.LaneZ()`
  mirrors these so traffic keeps right.
* The pavement kerb sits on the tile's **−Y edge**. The pavement between the lot
  and the road is therefore placed with a 180° yaw; the far pavement at 0°.
* The street lamp arm reaches over the road once the import orientation offset
  is applied, so the *far* pavement lamp is the one that gets turned around.

## New palette entries

`MAT_Asphalt #4E4E56` · `MAT_RoadLine #EFE3C8` · `MAT_Sidewalk #CBBDA8` ·
`MAT_CurbStone #A2937F` · `MAT_BrickWarm #C4785B` · `MAT_BrickCool #8E9AA6` ·
`MAT_WindowGlass #9FD3E0` · `MAT_LampPost #3A4148` · `MAT_LampGlow #FFE9A8` ·
`MAT_CarGlass #7FB6C9` · `MAT_Tire #26262A` · `MAT_Awning #D9564A`

## Quality check

All twelve assets in the pack (Phase 1 + city) now report **0 non-manifold
edges, 0 duplicate faces and 0 zero-area faces on every LOD**, with LOD0 bottoms
at exactly Z = 0 and identity root transforms. Every FBX was re-imported into an
empty Blender file and measured: triangle counts, material slots, anchors and
world bounds all match the source.

**Fixed during this pass**

1. The joined-mesh cleanup welded coincident vertices, which turned the shared
   edges of merely-touching parts (a shopfront sitting on a plinth, a kerb flush
   with a pavement) into non-manifold ones. Welding is now per-part only —
   `clean_mesh(weld=False)` for joined and decimated meshes.
2. Building A's awning posts were capsules starting at Z = 0, so their lower
   hemispheres sank 55 mm below ground. They now start at their own radius.
3. The pavements shipped only two materials, under the 3–8 slot rule. A storm
   drain against the kerb adds the third and reads as real street detail.
4. Building A was tower-shaped at 6.4 m wide by 14.3 m tall. Now 8.0 m wide with
   two upper floors, which suits the chunky cozy silhouette.

## Unity integration

* `CozyPackBuilder` (renamed from `CozyPackPhase1Builder`) now covers all twelve
  assets. City pieces use an `Environment` profile whose LOD transitions drop
  early, since they are background dressing.
* `CityKit` instantiates street pieces at **1:1 scale**. `MeshyVisuals` fits a
  model into a target box, which is right for gameplay wrappers but would break
  a modular tile's 4 m pitch.
* `CityBlock` prefers authored models and falls back to primitives when a prefab
  is missing, so the scene never breaks if the pack is not imported.
* The authored car ships one shared body material. `CityCar` recolours only the
  body submesh through a `MaterialPropertyBlock`, so traffic varies without
  touching the shared asset.

## Open

* `42_sidewalk_corner` is authored and exported but not yet placed — the current
  block uses straight runs only. It is there for when the lot gets an L-shaped
  pavement or a second street.
* A drive-through lane would reuse `TrafficSystem.RequestStop()` and
  `ServiceStopX`, which already stops a car behind the service counter.
