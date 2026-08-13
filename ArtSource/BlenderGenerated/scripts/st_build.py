"""Shawarma Tycoon - Phase 1 orchestrator: build, QC, render, export."""

import bpy
import bmesh
import math
import os
import re
import json
from mathutils import Vector, Matrix, Euler

import st_lib
from st_lib import (get_mat, new_collection, move_to_collection, bbox_of,
                    tri_count, build_all_materials, box, lathe, D2R)
import st_chars
import st_props
import st_cozy2
import st_city

ROOT_DIR = os.path.normpath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
).replace("\\", "/")
BLEND_PATH = ROOT_DIR + "/ShawarmaTycoon_CozyPack.blend"
PREV_DIR = ROOT_DIR + "/Previews"
FBX_DIR = ROOT_DIR + "/ReviewFBX"
DATA_JSON = ROOT_DIR + "/phase1_data.json"

PHASE1_ORDER = ["01_player_character", "02_customer_character",
                "06_rotisserie_station", "15_dining_table", "17_trash_bin"]
CITY_ORDER = [name for name, _ in st_city.CITY_BUILDERS]
COZY2_ORDER = [name for name, _ in st_cozy2.COZY2_CHARACTERS]
ASSET_ORDER = PHASE1_ORDER + CITY_ORDER + COZY2_ORDER
CHARACTERS = {"01_player_character", "02_customer_character"} | set(COZY2_ORDER)

VIEWS = {
    "front": (0.0, -1.0, 0.0),
    "rear":  (0.0, 1.0, 0.0),
    "side":  (1.0, 0.0, 0.0),
    "iso":   (0.92, -1.0, 0.80),
}


# --------------------------------------------------------------------------
# scene / preview rig
# --------------------------------------------------------------------------

def wipe_scene():
    bpy.ops.wm.read_homefile(use_empty=True)
    sc = bpy.context.scene
    sc.unit_settings.system = 'METRIC'
    sc.unit_settings.scale_length = 1.0
    sc.render.fps = 30
    build_all_materials()


def setup_preview_rig():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    try:
        sc.eevee.taa_render_samples = 64
        sc.eevee.use_raytracing = True
        sc.eevee.use_shadows = True
    except Exception:
        pass
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = 'PNG'

    # warm neutral world
    w = bpy.data.worlds.get("PRV_World") or bpy.data.worlds.new("PRV_World")
    w.use_nodes = True
    bg = w.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = st_lib.hex_rgb("EFE2CE")
    bg.inputs[1].default_value = 1.15
    sc.world = w

    coll = new_collection("PREVIEW_RIG")

    me_ground = box("PRV_ground", (60, 60, 0.02), (0, 0, -0.012), mat="PRV_Ground")
    move_to_collection(me_ground, coll)

    def light(name, kind, energy, direction, size=6.0, soft=0.25):
        ld = bpy.data.lights.new(name, kind)
        ld.energy = energy
        ld.color = (1.0, 0.97, 0.92)
        if kind == 'SUN':
            ld.angle = soft
        else:
            ld.size = size
            ld.shape = 'SQUARE'
        ob = bpy.data.objects.new(name, ld)
        bpy.context.scene.collection.objects.link(ob)
        move_to_collection(ob, coll)
        d = Vector(direction).normalized()
        ob.location = -d * 12.0 + Vector((0, 0, 3.0))
        ob.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
        return ob

    light("PRV_key", 'SUN', 3.2, (0.45, 0.75, -1.0), soft=0.16)
    light("PRV_fill", 'AREA', 220.0, (-0.8, 0.5, -0.35), size=10.0)
    light("PRV_rim", 'AREA', 160.0, (0.1, -0.9, -0.45), size=9.0)

    cam_d = bpy.data.cameras.new("PRV_CAM")
    cam_d.type = 'ORTHO'
    cam = bpy.data.objects.new("PRV_CAM", cam_d)
    bpy.context.scene.collection.objects.link(cam)
    move_to_collection(cam, coll)
    sc.camera = cam
    return coll


def aim_camera(cam, objs, direction, margin=1.16, distance=40.0):
    lo, hi = bbox_of(objs)
    ctr = (lo + hi) * 0.5
    d = Vector(direction).normalized()
    cam.location = ctr + d * distance
    if abs(d.z) > 0.999:                     # straight top/bottom view:
        cam.rotation_euler = (0.0, 0.0, 0.0) # keep world -Y pointing screen-down
    else:
        cam.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    xs, ys = [], []
    for i in range(8):
        c = Vector((lo.x if i & 1 else hi.x, lo.y if i & 2 else hi.y,
                    lo.z if i & 4 else hi.z))
        v = inv @ c
        xs.append(v.x)
        ys.append(v.y)
    cx = (min(xs) + max(xs)) * 0.5
    cy = (min(ys) + max(ys)) * 0.5
    cam.location = cam.matrix_world @ Vector((cx, cy, 0.0))
    bpy.context.view_layer.update()
    sc = bpy.context.scene
    aspect = sc.render.resolution_x / sc.render.resolution_y
    need_w = (max(xs) - min(xs)) * margin
    need_h = (max(ys) - min(ys)) * margin
    cam.data.ortho_scale = max(need_w, need_h * aspect) if aspect >= 1.0 \
        else max(need_w / aspect, need_h)
    return cam


def render_to(path):
    sc = bpy.context.scene
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


# --------------------------------------------------------------------------
# scene queries
# --------------------------------------------------------------------------

def asset_coll(name):
    return bpy.data.collections[name]


def asset_root(name):
    return bpy.data.objects[name + "_ROOT"]


def lod_objs(name):
    out = []
    for lv in (0, 1, 2):
        ob = bpy.data.objects.get("%s_LOD%d" % (name, lv))
        if ob:
            out.append(ob)
    return out


def all_asset_objs(name):
    seen = []

    def rec(c):
        for o in c.objects:
            if o not in seen:
                seen.append(o)
        for ch in c.children:
            rec(ch)
    rec(asset_coll(name))
    return seen


# --------------------------------------------------------------------------
# QC
# --------------------------------------------------------------------------

def qc_mesh(ob):
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    nonman = sum(1 for e in bm.edges if len(e.link_faces) != 2)
    seen = {}
    dupfaces = 0
    for f in bm.faces:
        k = tuple(sorted(v.index for v in f.verts))
        if k in seen:
            dupfaces += 1
        seen[k] = 1
    zero = sum(1 for f in bm.faces if f.calc_area() < 1e-9)
    bm.free()
    return {"non_manifold_edges": nonman, "duplicate_faces": dupfaces,
            "zero_area_faces": zero}


def qc_asset(name):
    objs = all_asset_objs(name)
    lods = lod_objs(name)
    lo, hi = bbox_of(lods[:1])
    root = asset_root(name)
    front = next(o for o in objs if o.name.startswith("FRONT_DIRECTION"))
    res = {
        "name": name,
        "dims_XYZ": [round(hi[i] - lo[i], 4) for i in range(3)],
        "bbox_min": [round(v, 4) for v in lo],
        "bbox_max": [round(v, 4) for v in hi],
        "ground_contact_z": round(lo.z, 5),
        "origin_bottom_center": [round((lo.x + hi.x) / 2, 4),
                                 round((lo.y + hi.y) / 2, 4)],
        "root_location": [round(v, 5) for v in root.location],
        "root_rotation_deg": [round(math.degrees(v), 4) for v in root.rotation_euler],
        "root_scale": [round(v, 5) for v in root.scale],
        "front_direction_local": [round(v, 4) for v in front.location],
        "tris": {},
        "materials": [m.name for m in lods[0].data.materials],
        "mesh_checks": {},
        "unapplied_modifiers": {},
        "anchors": {},
        "has_rig": False,
        "actions": [],
    }
    for ob in lods:
        res["tris"][ob.name[-4:]] = tri_count(ob)
        res["mesh_checks"][ob.name[-4:]] = qc_mesh(ob)
        res["unapplied_modifiers"][ob.name[-4:]] = [m.type for m in ob.modifiers]
    for o in objs:
        if o.type == 'EMPTY':
            res["anchors"][o.name] = [round(v, 4) for v in o.location]
        if o.type == 'ARMATURE':
            res["has_rig"] = True
            if o.animation_data:
                res["actions"] = sorted(t.name for t in o.animation_data.nla_tracks)
    res["cameras_or_lights_inside"] = [o.name for o in objs
                                       if o.type in ('CAMERA', 'LIGHT')]
    return res


# --------------------------------------------------------------------------
# build
# --------------------------------------------------------------------------

def save_blend():
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)


def attach_shared_actions(rig):
    """Stash Idle/Walk/CarryWalk onto another rig.

    The clips are stored as bone-local rotations, so they retarget to any rig
    that uses the same bone names - including the shorter chibi skeleton. Slot
    identifiers are keyed to the rig that first authored them, so a second rig
    never auto-resolves one; bind it explicitly or the FBX exporter silently
    drops the clip.
    """
    if rig.animation_data is None:
        rig.animation_data_create()
    ad = rig.animation_data
    for act_name in ("Idle", "Walk", "CarryWalk"):
        act = bpy.data.actions.get(act_name)
        if act is None:
            continue
        tr = ad.nla_tracks.new()
        tr.name = act_name
        strip = tr.strips.new(act_name, 1, act)
        if hasattr(strip, "action_slot") and act.slots:
            strip.action_slot = act.slots[0]
        tr.mute = True

    idle = bpy.data.actions.get("Idle")
    if idle is None:
        return
    ad.action = idle
    if hasattr(ad, "action_slot") and idle.slots:
        ad.action_slot = idle.slots[0]


def build_all():
    wipe_scene()
    setup_preview_rig()
    log = []
    builders = [
        ("01_player_character", st_chars.build_player),
        ("02_customer_character", st_chars.build_customer),
        ("06_rotisserie_station", st_props.build_rotisserie),
        ("15_dining_table", st_props.build_dining_table),
        ("17_trash_bin", st_props.build_trash_bin),
    ] + list(st_city.CITY_BUILDERS) + list(st_cozy2.COZY2_CHARACTERS)
    first_rig = None
    for name, fn in builders:
        a = fn()
        if name in CHARACTERS and first_rig is None:
            first_rig = a.rig
            st_chars.build_actions(a.rig, fps=bpy.context.scene.render.fps)
        elif name in CHARACTERS:
            attach_shared_actions(a.rig)
        log.append(qc_asset(name))
        save_blend()
    with open(DATA_JSON, "w", encoding="utf-8") as f:
        json.dump(log, f, indent=2)
    return log


# --------------------------------------------------------------------------
# previews
# --------------------------------------------------------------------------

def render_asset_views(name, size=1100):
    sc = bpy.context.scene
    sc.render.resolution_x = size
    sc.render.resolution_y = size
    cam = bpy.data.objects["PRV_CAM"]
    hide_all_but(name)
    out = {}
    for view, d in VIEWS.items():
        aim_camera(cam, lod_objs(name)[:1], d)
        out[view] = render_to("%s/%s_%s.png" % (PREV_DIR, name, view))
    show_all()
    return out


def hide_all_but(name):
    for n in ASSET_ORDER:
        vis = (n == name)
        for o in all_asset_objs(n):
            o.hide_render = not vis
            o.hide_viewport = not vis
    for n in ASSET_ORDER:
        for lv in (1, 2):
            ob = bpy.data.objects.get("%s_LOD%d" % (n, lv))
            if ob:
                ob.hide_render = True
                ob.hide_viewport = True


def show_all():
    for n in ASSET_ORDER:
        for o in all_asset_objs(n):
            o.hide_render = False
            o.hide_viewport = False
    for n in ASSET_ORDER:
        for lv in (1, 2):
            ob = bpy.data.objects.get("%s_LOD%d" % (n, lv))
            if ob:
                ob.hide_render = True
                ob.hide_viewport = True


def render_lod_strip(name, size=760):
    """front render of LOD0/1/2 side by side for the report."""
    sc = bpy.context.scene
    sc.render.resolution_x = size
    sc.render.resolution_y = size
    cam = bpy.data.objects["PRV_CAM"]
    paths = []
    for lv in (0, 1, 2):
        hide_all_but(name)
        for l in (0, 1, 2):
            ob = bpy.data.objects.get("%s_LOD%d" % (name, l))
            if ob:
                ob.hide_render = (l != lv)
                ob.hide_viewport = (l != lv)
        aim_camera(cam, [bpy.data.objects["%s_LOD0" % name]], VIEWS["iso"])
        paths.append(render_to("%s/tmp_%s_lod%d.png" % (PREV_DIR, name, lv)))
    show_all()
    return paths


# ---- sheets ---------------------------------------------------------------

def layout_row(names, gap=1.0):
    """Spread assets along X with a gap, sized from their own footprints."""
    widths = []
    for name in names:
        lo, hi = bbox_of(lod_objs(name)[:1])
        widths.append(max(0.4, hi.x - lo.x))
    total = sum(widths) + gap * (len(widths) - 1)
    cursor = -total * 0.5
    layout = {}
    for name, width in zip(names, widths):
        layout[name] = cursor + width * 0.5
        cursor += width + gap
    return layout


def make_label(text, loc, coll, size=0.24, rot_z=0.0):
    cu = bpy.data.curves.new("LBL_" + text, type='FONT')
    cu.body = text
    cu.align_x = 'CENTER'
    cu.align_y = 'CENTER'
    cu.size = size
    cu.extrude = 0.003
    ob = bpy.data.objects.new("LBL_" + text, cu)
    ob.location = loc
    ob.rotation_euler = (0, 0, rot_z)
    ob.data.materials.append(get_mat("PRV_Label"))
    bpy.context.scene.collection.objects.link(ob)
    move_to_collection(ob, coll)
    return ob


def make_arrow(name, x, y_start, coll, length=0.80):
    """Flat floor arrow pointing along -Y (the asset FRONT)."""
    parts = []
    parts.append(box(name + "_shaft", (0.085, length * 0.60, 0.028),
                     (x, y_start - length * 0.30, 0.022), mat="PRV_Arrow"))
    parts.append(lathe(name + "_head", [(0.0, 0.0), (0.145, 0.0), (0.0, 0.32)],
                       20, loc=(x, y_start - length * 0.60, 0.022),
                       rot=(D2R(-90), 0, 0), mat="PRV_Arrow"))
    for p in parts:
        move_to_collection(p, coll)
    return parts


def render_labeled_iso(name, size=920):
    """Single asset, isometric, with a floor label that reads square to camera."""
    sc = bpy.context.scene
    coll = bpy.data.collections["PREVIEW_RIG"]
    cam = bpy.data.objects["PRV_CAM"]
    sc.render.resolution_x = size
    sc.render.resolution_y = size
    hide_all_but(name)
    d = Vector(VIEWS["iso"])
    flat = Vector((d.x, d.y, 0.0)).normalized()
    lo, hi = bbox_of(lod_objs(name)[:1])
    reach = max(hi.x - lo.x, hi.y - lo.y) * 0.5 + 0.42
    lbl = make_label(name, tuple(flat * reach + Vector((0, 0, 0.004))), coll,
                     0.165, rot_z=math.atan2(flat.y, flat.x) + math.pi / 2)
    aim_camera(cam, lod_objs(name)[:1] + [lbl], VIEWS["iso"], margin=1.12)
    path = render_to("%s/tmp_contact_%s.png" % (PREV_DIR, name))
    bpy.data.objects.remove(lbl, do_unlink=True)
    show_all()
    return path


def render_sheets(names=None, tag=""):
    names = list(names or PHASE1_ORDER)
    sc = bpy.context.scene
    coll = bpy.data.collections["PREVIEW_RIG"]
    cam = bpy.data.objects["PRV_CAM"]
    show_all()

    # ---------------- contact sheet: framed iso shots in a grid ------------
    tiles = [render_labeled_iso(n) for n in names]
    contact = compose(tiles, "%s/_contact_sheet%s.png" % (PREV_DIR, tag),
                      cols=3 if len(names) <= 6 else 4, pad=16)
    for t in tiles:
        try:
            os.remove(t)
        except OSError:
            pass

    # ---------------- orientation sheet (top down, FRONT markers) ----------
    layout = layout_row(names, gap=1.4)
    for n, x in layout.items():
        asset_root(n).location.x = x
    bpy.context.view_layer.update()
    front_y = min(bbox_of(lod_objs(n)[:1])[0].y for n in names) - 0.12
    label_size = 0.145 if len(names) <= 6 else 0.20
    temp = []
    for n, x in layout.items():
        temp += make_arrow("ARW_" + n, x, front_y, coll)
        temp.append(make_label(n, (x, 1.32, 0.004), coll, label_size))
        temp.append(make_label("FRONT  (0,-1,0)",
                               (x, front_y - 1.05, 0.004), coll, label_size * 0.8))
    temp.append(make_label("TOP VIEW   -   screen down = local -Y = FRONT",
                           (0.0, 2.35, 0.004), coll, label_size * 1.6))
    sc.render.resolution_x = 2800
    sc.render.resolution_y = 1250
    aim_camera(cam, [o for n in names for o in lod_objs(n)[:1]] + temp,
               (0.0, 0.0, 1.0), margin=1.05)
    orient = render_to("%s/_orientation_sheet%s.png" % (PREV_DIR, tag))
    for t in temp:
        bpy.data.objects.remove(t, do_unlink=True)

    for n in layout:
        asset_root(n).location.x = 0.0
    bpy.context.view_layer.update()
    return contact, orient


def compose(paths, out_path, cols=None, pad=14, bg=None):
    import numpy as np
    imgs = []
    for p in paths:
        im = bpy.data.images.load(p)
        w, h = im.size
        a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
        imgs.append(a)
        bpy.data.images.remove(im)
    if bg is None:                       # match the render background exactly
        bg = tuple(float(v) for v in imgs[0][2, 2, :3])
    cols = cols or len(imgs)
    rows = (len(imgs) + cols - 1) // cols
    cw = max(i.shape[1] for i in imgs)
    ch = max(i.shape[0] for i in imgs)
    W = cols * cw + (cols + 1) * pad
    H = rows * ch + (rows + 1) * pad
    canvas = np.zeros((H, W, 4), dtype=np.float32)
    canvas[:, :, 0] = bg[0]
    canvas[:, :, 1] = bg[1]
    canvas[:, :, 2] = bg[2]
    canvas[:, :, 3] = 1.0
    for i, a in enumerate(imgs):
        r = rows - 1 - (i // cols)
        c = i % cols
        y = pad + r * (ch + pad)
        x = pad + c * (cw + pad)
        canvas[y:y + a.shape[0], x:x + a.shape[1], :] = a
    out = bpy.data.images.new("compose_tmp", W, H, alpha=True)
    out.pixels = canvas.reshape(-1).tolist()
    out.filepath_raw = out_path
    out.file_format = 'PNG'
    out.save()
    bpy.data.images.remove(out)
    return out_path


# --------------------------------------------------------------------------
# FBX export
# --------------------------------------------------------------------------

def select_only(objs):
    for o in bpy.context.view_layer.objects:
        try:
            o.select_set(False)
        except Exception:
            pass
    for o in objs:
        o.hide_viewport = False
        o.select_set(True)
    if objs:
        bpy.context.view_layer.objects.active = objs[0]


_SUFFIX = re.compile(r"\.\d{3}$")


def claim_anchor_names(objs):
    """Blender object names are unique per file, so the 2nd..5th asset get
    FRONT_DIRECTION.001 etc. Temporarily hand the canonical name back to the
    asset being exported so every FBX ships clean anchor names."""
    swaps = []
    for o in objs:
        if o.type != 'EMPTY':
            continue
        base = _SUFFIX.sub("", o.name)
        if base == o.name:
            continue
        old_o = o.name
        other = bpy.data.objects.get(base)
        if other is not None and other is not o:
            old_other = other.name
            other.name = "__park__" + base
            o.name = base
            swaps.append((o, old_o, other, old_other))
        else:
            o.name = base
            swaps.append((o, old_o, None, None))
    return swaps


def restore_anchor_names(swaps):
    for o, old_o, other, old_other in reversed(swaps):
        o.name = old_o
        if other is not None:
            other.name = old_other


def export_fbx(name, bake_transform=True, out_dir=None):
    out_dir = out_dir or FBX_DIR
    os.makedirs(out_dir, exist_ok=True)
    show_all()
    for lv in (1, 2):
        ob = bpy.data.objects.get("%s_LOD%d" % (name, lv))
        if ob:
            ob.hide_viewport = False
    objs = all_asset_objs(name)
    is_char = name in CHARACTERS
    select_only(objs)
    path = "%s/%s.fbx" % (out_dir, name)
    kw = dict(
        filepath=path, check_existing=False, use_selection=True,
        use_visible=False, use_active_collection=False,
        global_scale=1.0, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_NONE',
        use_space_transform=True, bake_space_transform=bake_transform,
        object_types={'EMPTY', 'ARMATURE', 'MESH'},
        use_mesh_modifiers=True, mesh_smooth_type='FACE',
        use_subsurf=False, use_mesh_edges=False, use_tspace=False,
        use_triangles=False, use_custom_props=False,
        add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X',
        armature_nodetype='NULL', use_armature_deform_only=False,
        bake_anim=is_char, bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False, bake_anim_use_all_actions=is_char,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode='COPY', embed_textures=False, batch_mode='OFF',
        axis_forward='-Z', axis_up='Y',
    )
    swaps = claim_anchor_names(objs)
    try:
        # full window context needed: the exporter reads context.selected_objects
        with bpy.context.temp_override(**st_chars._override()):
            select_only(objs)
            bpy.ops.export_scene.fbx(**kw)
    finally:
        restore_anchor_names(swaps)
    show_all()
    return path, os.path.getsize(path)


def export_all():
    out = {}
    for n in ASSET_ORDER:
        path, size = export_fbx(n, bake_transform=True)
        out[n] = {"fbx": path, "bytes": size, "bake_space_transform": True}
    # characters also get a non-baked variant (safer for armature + anim in Unity)
    alt = FBX_DIR + "/_alt_no_bake_transform"
    for n in sorted(CHARACTERS):
        path, size = export_fbx(n, bake_transform=False, out_dir=alt)
        out[n]["alt_fbx"] = path
        out[n]["alt_bytes"] = size
    return out


def verify_fbx():
    """Re-import every FBX into an empty file and report what actually landed."""
    res = {}
    ov = st_chars._override()
    for n in ASSET_ORDER:
        bpy.ops.wm.read_homefile(use_empty=True)
        path = "%s/%s.fbx" % (FBX_DIR, n)
        with bpy.context.temp_override(**st_chars._override()):
            bpy.ops.import_scene.fbx(filepath=path)
        meshes = [o for o in bpy.data.objects if o.type == 'MESH']
        empties = [o.name for o in bpy.data.objects if o.type == 'EMPTY']
        arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
        info = {
            "meshes": sorted(o.name for o in meshes),
            "tris": {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons)
                     for o in meshes},
            "materials": sorted({m.name for o in meshes for m in o.data.materials
                                 if m}),
            "empties": sorted(empties),
            "armatures": [o.name for o in arms],
            "bones": len(arms[0].data.bones) if arms else 0,
            "actions": sorted(a.name for a in bpy.data.actions),
            "root_xform": None,
        }
        root = next((o for o in bpy.data.objects if o.name.endswith("_ROOT")), None)
        if root:
            info["root_xform"] = {
                "loc": [round(v, 5) for v in root.location],
                "rot_deg": [round(math.degrees(v), 3) for v in root.rotation_euler],
                "scale": [round(v, 5) for v in root.scale]}
        for a in arms:                       # measure the un-posed silhouette
            a.data.pose_position = 'REST'
        bpy.context.view_layer.update()
        lod0 = next((o for o in meshes if o.name.startswith(n + "_LOD0")), None)
        if lod0:
            dg = bpy.context.evaluated_depsgraph_get()
            ev = lod0.evaluated_get(dg)
            me = ev.to_mesh()
            mw = lod0.matrix_world
            pts = [mw @ v.co for v in me.vertices]
            lo = Vector((min(p.x for p in pts), min(p.y for p in pts),
                         min(p.z for p in pts)))
            hi = Vector((max(p.x for p in pts), max(p.y for p in pts),
                         max(p.z for p in pts)))
            ev.to_mesh_clear()
            info["lod0_world_bbox"] = {"min": [round(v, 4) for v in lo],
                                       "max": [round(v, 4) for v in hi],
                                       "dims": [round(hi[i] - lo[i], 4)
                                                for i in range(3)]}
        res[n] = info
    bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)
    return res
