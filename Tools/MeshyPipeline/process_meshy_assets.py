"""Convert the downloaded Meshy GLBs into mobile-ready semantic FBX LODs.

Run with Blender in background mode:
  blender --background --factory-startup --python process_meshy_assets.py -- <source> <output>

The source downloads were saved with eight incorrect semantic names.  The table
below is deliberately the single source of truth for both remapping and triangle
budgets so the conversion is reproducible.
"""

from __future__ import annotations

import json
import math
import sys
import time
from pathlib import Path

import bpy
from mathutils import Vector


ASSETS = (
    ("02_customer_character.glb", "01_player_character", (18_000, 10_000, 4_000)),
    ("21_entrance_door.glb", "02_customer_character", (16_000, 8_000, 3_000)),
    ("24_modular_wall_corner.glb", "03_cashier_worker", (16_000, 8_000, 3_000)),
    ("04_meat_storage_rack.glb", "04_meat_storage_rack", (24_000, 12_000, 4_000)),
    ("06_shawarma_rotisserie.glb", "06_shawarma_rotisserie", (20_000, 10_000, 3_500)),
    ("08_cutting_station.glb", "08_cutting_station", (16_000, 8_000, 3_000)),
    ("10_wrap_preparation_station.glb", "10_wrap_preparation_station", (18_000, 9_000, 3_000)),
    ("12_service_cashier_counter.glb", "12_service_cashier_counter", (14_000, 7_000, 2_500)),
    ("13_conveyor_straight.glb", "13_conveyor_straight", (3_000, 1_200, 400)),
    ("14_conveyor_corner.glb", "14_conveyor_corner", (4_000, 1_500, 500)),
    ("15_dining_table_clean.glb", "15_dining_table_clean", (8_000, 4_000, 1_500)),
    ("17_trash_bin.glb", "17_trash_bin", (5_000, 2_500, 800)),
    ("18_money_collection_pad.glb", "18_money_collection_pad", (2_500, 1_000, 300)),
    ("19_upgrade_pad.glb", "19_upgrade_pad", (1_500, 600, 200)),
    ("23_modular_wall_straight.glb", "21_entrance_door", (4_000, 1_500, 500)),
    ("03_cashier_worker.glb", "22_modular_floor_tile", (800, 300, 100)),
    ("34_floating_diorama_island.glb", "23_modular_wall_straight", (800, 300, 100)),
    ("22_modular_floor_tile.glb", "24_modular_wall_corner", (1_200, 500, 150)),
    ("01_player_character.glb", "34_floating_diorama_island", (12_000, 5_000, 1_500)),
)


PALETTE_PROFILES = {
    "01_player_character": "character",
    "02_customer_character": "character",
    "03_cashier_worker": "character",
    "04_meat_storage_rack": "rack",
    "06_shawarma_rotisserie": "rotisserie",
    "08_cutting_station": "station",
    "10_wrap_preparation_station": "station",
    "12_service_cashier_counter": "station",
    "13_conveyor_straight": "conveyor",
    "14_conveyor_corner": "conveyor",
    "15_dining_table_clean": "table",
    "17_trash_bin": "prop",
    "18_money_collection_pad": "prop",
    "19_upgrade_pad": "prop",
    "21_entrance_door": "architecture",
    "22_modular_floor_tile": "architecture",
    "23_modular_wall_straight": "architecture",
    "24_modular_wall_corner": "architecture",
    "34_floating_diorama_island": "island",
}


def script_arguments() -> tuple[Path, Path]:
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    if len(args) != 2:
        raise SystemExit("Expected: <source_glb_directory> <output_directory>")
    return Path(args[0]).resolve(), Path(args[1]).resolve()


def clear_scene() -> None:
    bpy.ops.object.mode_set(mode="OBJECT") if bpy.context.object and bpy.context.object.mode != "OBJECT" else None
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)


def triangle_count(mesh: bpy.types.Mesh) -> int:
    return sum(max(0, len(polygon.vertices) - 2) for polygon in mesh.polygons)


def mesh_bounds(obj: bpy.types.Object) -> tuple[Vector, Vector]:
    minimum = Vector((math.inf, math.inf, math.inf))
    maximum = Vector((-math.inf, -math.inf, -math.inf))
    for vertex in obj.data.vertices:
        minimum.x = min(minimum.x, vertex.co.x)
        minimum.y = min(minimum.y, vertex.co.y)
        minimum.z = min(minimum.z, vertex.co.z)
        maximum.x = max(maximum.x, vertex.co.x)
        maximum.y = max(maximum.y, vertex.co.y)
        maximum.z = max(maximum.z, vertex.co.z)
    return minimum, maximum


def polygon_center(mesh: bpy.types.Mesh, polygon: bpy.types.MeshPolygon) -> Vector:
    return sum((mesh.vertices[index].co for index in polygon.vertices), Vector()) / len(polygon.vertices)


def add_palette_slots(obj: bpy.types.Object) -> None:
    """Create four stable submesh slots; Unity replaces their materials by slot index."""
    obj.data.materials.clear()
    preview_colours = (
        (0.48, 0.62, 0.60, 1.0),
        (1.00, 0.92, 0.76, 1.0),
        (0.86, 0.42, 0.28, 1.0),
        (0.27, 0.31, 0.32, 1.0),
    )
    for index, colour in enumerate(preview_colours):
        material = bpy.data.materials.new(f"MeshyPaletteSlot{index}")
        material.diffuse_color = colour
        obj.data.materials.append(material)


def assign_palette_segments(obj: bpy.types.Object, semantic_name: str) -> None:
    """Split textureless Meshy geometry into four cozy, mobile-friendly colour regions."""
    add_palette_slots(obj)
    minimum, maximum = mesh_bounds(obj)
    center = (minimum + maximum) * 0.5
    half = (maximum - minimum) * 0.5
    height = max(maximum.z - minimum.z, 1e-5)
    profile = PALETTE_PROFILES[semantic_name]

    for polygon in obj.data.polygons:
        point = polygon_center(obj.data, polygon)
        xn = abs((point.x - center.x) / max(half.x, 1e-5))
        yn = abs((point.y - center.y) / max(half.y, 1e-5))
        zn = (point.z - minimum.z) / height
        nz = polygon.normal.z

        if profile == "character":
            if zn < 0.24:
                slot = 3
            elif zn > 0.84:
                slot = 2 if nz > 0.30 or xn > 0.62 else 1
            elif zn > 0.62 or (0.36 < zn < 0.63 and xn > 0.58):
                slot = 1
            else:
                slot = 0
        elif profile == "rack":
            if xn > 0.78 or (yn > 0.84 and xn > 0.55):
                slot = 0
            elif zn < 0.10:
                slot = 3
            elif nz > 0.62:
                slot = 1
            elif xn < 0.72 and 0.13 < zn < 0.90:
                slot = 2
            else:
                slot = 0
        elif profile == "rotisserie":
            if zn < 0.10 or xn > 0.82:
                slot = 3
            elif xn < 0.46 and yn < 0.62 and 0.14 < zn < 0.91:
                slot = 2
            elif nz > 0.60 and zn > 0.40:
                slot = 1
            else:
                slot = 0
        elif profile == "station":
            if zn < 0.12 or (xn > 0.84 and zn < 0.70):
                slot = 3
            elif nz > 0.58 and zn > 0.45:
                slot = 1
            elif yn > 0.70 and 0.25 < zn < 0.78:
                slot = 2
            else:
                slot = 0
        elif profile == "conveyor":
            if zn < 0.22:
                slot = 3
            elif nz > 0.58:
                slot = 1
            elif yn > 0.70:
                slot = 2
            else:
                slot = 0
        elif profile == "table":
            if zn < 0.18:
                slot = 3
            elif nz > 0.58 and zn > 0.38:
                slot = 1
            elif yn > 0.42 or xn > 0.66:
                slot = 2
            else:
                slot = 0
        elif profile == "island":
            if zn < 0.18:
                slot = 3
            elif nz > 0.58 and zn > 0.62:
                slot = 1
            elif xn > 0.72 or yn > 0.72:
                slot = 2
            else:
                slot = 0
        elif profile == "architecture":
            if zn < 0.10:
                slot = 3
            elif nz > 0.58:
                slot = 1
            elif xn > 0.82 or yn > 0.82 or zn > 0.88:
                slot = 2
            else:
                slot = 0
        else:
            if zn < 0.14:
                slot = 3
            elif nz > 0.58:
                slot = 1
            elif xn > 0.70 or yn > 0.70 or zn > 0.82:
                slot = 2
            else:
                slot = 0

        polygon.material_index = slot

    obj.data.update()


def import_and_clean(source_path: Path, semantic_name: str) -> bpy.types.Object:
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(source_path))

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"No mesh found in {source_path.name}")

    for obj in meshes:
        world_matrix = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = world_matrix
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        obj.select_set(False)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()

    obj = bpy.context.view_layer.objects.active
    obj.name = semantic_name
    obj.data.name = semantic_name + "_Mesh"
    obj.data.materials.clear()
    obj.data.validate(clean_customdata=False)

    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=1e-5, use_sharp_edge_from_normals=True)
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")

    minimum, maximum = mesh_bounds(obj)

    # Blender imports glTF into its native Z-up coordinate system.  Ground the
    # asset on Z before the FBX exporter converts it to Unity's Y-up system.
    # The old code grounded Y and centred Z, which left characters and props
    # half-buried or apparently lying on their side in Unity.
    offset = Vector((
        -(minimum.x + maximum.x) * 0.5,
        -(minimum.y + maximum.y) * 0.5,
        -minimum.z,
    ))
    for vertex in obj.data.vertices:
        vertex.co += offset
    obj.data.update()
    return obj


def export_lod(obj: bpy.types.Object, semantic_name: str, output_path: Path, target_triangles: int) -> int:
    original_triangles = triangle_count(obj.data)
    if original_triangles > target_triangles:
        modifier = obj.modifiers.new(name="MobileDecimate", type="DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = max(0.0001, min(1.0, target_triangles / original_triangles))
        modifier.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)

    # Decimation can move a few boundary vertices. Re-ground every LOD so all
    # variants share a stable bottom-centre pivot and do not pop through floors.
    minimum, maximum = mesh_bounds(obj)
    lod_offset = Vector((
        -(minimum.x + maximum.x) * 0.5,
        -(minimum.y + maximum.y) * 0.5,
        -minimum.z,
    ))
    for vertex in obj.data.vertices:
        vertex.co += lod_offset

    obj.data.validate(clean_customdata=False)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40.0), keep_sharp_edges=True)
    assign_palette_segments(obj, semantic_name)

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        check_existing=False,
        use_selection=True,
        object_types={"MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        use_space_transform=True,
        bake_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        use_mesh_modifiers_render=True,
        mesh_smooth_type="FACE",
        use_tspace=False,
        use_triangles=True,
        use_custom_props=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )
    return triangle_count(obj.data)


def process_asset(source_dir: Path, output_dir: Path, source_name: str, semantic_name: str, budgets: tuple[int, int, int]) -> dict:
    source_path = source_dir / source_name
    if not source_path.is_file():
        raise FileNotFoundError(source_path)

    started = time.perf_counter()
    obj = import_and_clean(source_path, semantic_name)
    original_mesh = obj.data.copy()
    original_triangles = triangle_count(original_mesh)
    lod_results = []

    for lod_index, budget in enumerate(budgets):
        working_mesh = original_mesh.copy()
        previous_mesh = obj.data
        obj.data = working_mesh
        if previous_mesh != original_mesh and previous_mesh.users == 0:
            bpy.data.meshes.remove(previous_mesh)
        obj.name = f"{semantic_name}_LOD{lod_index}"
        obj.data.name = f"{semantic_name}_LOD{lod_index}_Mesh"
        destination = output_dir / f"LOD{lod_index}" / f"{semantic_name}_LOD{lod_index}.fbx"
        actual = export_lod(obj, semantic_name, destination, budget)
        lod_results.append({"lod": lod_index, "budget": budget, "triangles": actual, "file": str(destination)})

    if original_mesh.users == 0:
        bpy.data.meshes.remove(original_mesh)

    return {
        "semantic_name": semantic_name,
        "source_file": source_name,
        "source_triangles": original_triangles,
        "lods": lod_results,
        "seconds": round(time.perf_counter() - started, 3),
    }


def main() -> None:
    source_dir, output_dir = script_arguments()
    output_dir.mkdir(parents=True, exist_ok=True)
    report = {
        "blender_version": bpy.app.version_string,
        "source_directory": str(source_dir),
        "output_directory": str(output_dir),
        "assets": [],
    }

    for source_name, semantic_name, budgets in ASSETS:
        print(f"[MeshyPipeline] {source_name} -> {semantic_name}", flush=True)
        result = process_asset(source_dir, output_dir, source_name, semantic_name, budgets)
        report["assets"].append(result)
        print(
            f"[MeshyPipeline] {semantic_name}: {result['source_triangles']} -> "
            + "/".join(str(item["triangles"]) for item in result["lods"])
            + f" tris ({result['seconds']}s)",
            flush=True,
        )

    report_path = output_dir / "meshy_optimization_report.json"
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"[MeshyPipeline] Complete: {report_path}", flush=True)


if __name__ == "__main__":
    main()
