"""Shawarma Tycoon - cozy characters + shared humanoid rig + loop animations.

Characters face local -Y. Height ~1.70 m. Rigid (chunky cartoon) skinning:
every body part is bound 1.0 to a single bone, capsule ends keep joints
looking connected when they bend.
"""

import bpy
import math
from mathutils import Vector, Matrix
from st_lib import (box, sphere, capsule, lathe, torus, tube_along, pie_prism,
                    dome_shell, ellipsoid_front, finalize, shade, Asset,
                    make_lods, get_mat, move_to_collection, parent_keep, D2R)

RIG_NAME = "ST_HumanoidRig"

# shared skeleton (world space, meters) -------------------------------------
BONES = [
    # name,        head,               tail,               parent
    ("Hips",       (0.00, 0, 0.86),    (0.00, 0, 0.98),    None),
    ("Spine",      (0.00, 0, 0.98),    (0.00, 0, 1.10),    "Hips"),
    ("Chest",      (0.00, 0, 1.10),    (0.00, 0, 1.24),    "Spine"),
    ("Neck",       (0.00, 0, 1.24),    (0.00, 0, 1.32),    "Chest"),
    ("Head",       (0.00, 0, 1.32),    (0.00, 0, 1.66),    "Neck"),
    ("Shoulder.L", (0.055, 0, 1.225),  (0.155, 0, 1.235),  "Chest"),
    ("UpperArm.L", (0.155, 0, 1.235),  (0.228, 0, 1.02),   "Shoulder.L"),
    ("LowerArm.L", (0.228, 0, 1.02),   (0.248, 0, 0.80),   "UpperArm.L"),
    ("Hand.L",     (0.248, 0, 0.80),   (0.252, 0, 0.70),   "LowerArm.L"),
    ("Shoulder.R", (-0.055, 0, 1.225), (-0.155, 0, 1.235), "Chest"),
    ("UpperArm.R", (-0.155, 0, 1.235), (-0.228, 0, 1.02),  "Shoulder.R"),
    ("LowerArm.R", (-0.228, 0, 1.02),  (-0.248, 0, 0.80),  "UpperArm.R"),
    ("Hand.R",     (-0.248, 0, 0.80),  (-0.252, 0, 0.70),  "LowerArm.R"),
    ("UpperLeg.L", (0.105, 0, 0.84),   (0.110, 0, 0.46),   "Hips"),
    ("LowerLeg.L", (0.110, 0, 0.46),   (0.112, 0, 0.10),   "UpperLeg.L"),
    ("Foot.L",     (0.112, 0, 0.10),   (0.112, -0.16, 0.04), "LowerLeg.L"),
    ("UpperLeg.R", (-0.105, 0, 0.84),  (-0.110, 0, 0.46),  "Hips"),
    ("LowerLeg.R", (-0.110, 0, 0.46),  (-0.112, 0, 0.10),  "UpperLeg.R"),
    ("Foot.R",     (-0.112, 0, 0.10),  (-0.112, -0.16, 0.04), "LowerLeg.R"),
]


def _override():
    wm = bpy.context.window_manager
    win = wm.windows[0]
    scr = win.screen
    area = next((a for a in scr.areas if a.type == 'VIEW_3D'), scr.areas[0])
    region = next((r for r in area.regions if r.type == 'WINDOW'), area.regions[0])
    return dict(window=win, screen=scr, area=area, region=region,
                scene=bpy.context.scene, view_layer=bpy.context.view_layer)


def build_rig(obj_name):
    """Create (once) the shared humanoid armature datablock and an object for it."""
    arm = bpy.data.armatures.get(RIG_NAME)
    fresh = arm is None
    if fresh:
        arm = bpy.data.armatures.new(RIG_NAME)
    ob = bpy.data.objects.new(obj_name, arm)
    bpy.context.scene.collection.objects.link(ob)
    if fresh:
        ov = _override()
        with bpy.context.temp_override(**ov):
            bpy.context.view_layer.objects.active = ob
            ob.select_set(True)
            bpy.ops.object.mode_set(mode='EDIT')
            eb = arm.edit_bones
            for name, head, tail, parent in BONES:
                b = eb.new(name)
                b.head = head
                b.tail = tail
                b.use_connect = False
                if parent:
                    b.parent = eb[parent]
            bpy.ops.object.mode_set(mode='OBJECT')
            ob.select_set(False)
    return ob


def swing_axis(pbone):
    """Local euler axis index + sign that swings the bone tip toward -Y."""
    rest = pbone.bone.matrix_local.to_3x3()
    d = (Vector(pbone.bone.tail_local) - Vector(pbone.bone.head_local)).normalized()
    best = None
    for i in range(3):
        u = Vector((1 if i == 0 else 0, 1 if i == 1 else 0, 1 if i == 2 else 0))
        ax = (rest @ u).normalized()
        score = abs(ax.dot(Vector((1, 0, 0))))
        if best is None or score > best[1]:
            best = (i, score, ax)
    i, _, ax = best
    disp = ax.cross(d)
    sign = 1.0 if disp.dot(Vector((0, -1, 0))) > 0 else -1.0
    return i, sign


# --------------------------------------------------------------------------
# animation
# --------------------------------------------------------------------------

def _new_action(rig, name):
    if rig.animation_data is None:
        rig.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data.action = act
    if hasattr(rig.animation_data, "action_slot"):
        try:
            if rig.animation_data.action_slot is None:
                slot = act.slots.new(id_type='OBJECT', name=rig.name)
                rig.animation_data.action_slot = slot
        except Exception:
            pass
    return act


def _key(rig, axes, frame, rot=None, loc=None):
    for bname, deg in (rot or {}).items():
        pb = rig.pose.bones[bname]
        pb.rotation_mode = 'XYZ'
        i, s = axes[bname]
        e = [0.0, 0.0, 0.0]
        e[i] = s * D2R(deg)
        pb.rotation_euler = e
        pb.keyframe_insert("rotation_euler", frame=frame)
    for bname, v in (loc or {}).items():
        pb = rig.pose.bones[bname]
        pb.location = v
        pb.keyframe_insert("location", frame=frame)


def _reset_pose(rig):
    for pb in rig.pose.bones:
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)


ALL_ROT = ["Hips", "Spine", "Chest", "Neck", "Head",
           "UpperArm.L", "LowerArm.L", "UpperArm.R", "LowerArm.R",
           "UpperLeg.L", "LowerLeg.L", "Foot.L",
           "UpperLeg.R", "LowerLeg.R", "Foot.R"]


def build_actions(rig, fps=30):
    """Idle 2s, Walk 1s, CarryWalk 1s - all loop (last key == first key)."""
    axes = {b: swing_axis(rig.pose.bones[b]) for b in ALL_ROT}
    made = []

    def full(d):
        out = {b: 0.0 for b in ALL_ROT}
        out.update(d)
        return out

    # ---------------- Idle : 2 s ----------------
    _reset_pose(rig)
    act = _new_action(rig, "Idle")
    n = fps * 2
    for f in range(0, n + 1, 3):
        t = f / n
        s = math.sin(math.tau * t)
        _key(rig, axes, f + 1,
             rot=full({"Spine": 1.6 * s, "Chest": 1.0 * s, "Head": -1.8 * s,
                       "UpperArm.L": 2.5 * s, "UpperArm.R": 2.5 * s,
                       "LowerArm.L": 1.5 * s, "LowerArm.R": 1.5 * s}),
             loc={"Hips": (0.0, 0.012 * s, 0.0)})
    made.append(("Idle", 1, n + 1))

    # ---------------- Walk : 1 s ----------------
    _reset_pose(rig)
    act = _new_action(rig, "Walk")
    n = fps
    for f in range(0, n + 1, 2):
        t = f / n
        a = math.tau * t
        s = math.sin(a)
        _key(rig, axes, f + 1,
             rot=full({
                 "UpperLeg.L": 26.0 * s,
                 "UpperLeg.R": -26.0 * s,
                 "LowerLeg.L": -30.0 * max(0.0, math.sin(a - 1.0)),
                 "LowerLeg.R": -30.0 * max(0.0, math.sin(a - 1.0 + math.pi)),
                 "Foot.L": 8.0 * math.sin(a + 1.2),
                 "Foot.R": 8.0 * math.sin(a + 1.2 + math.pi),
                 "UpperArm.L": -20.0 * s,
                 "UpperArm.R": 20.0 * s,
                 "LowerArm.L": -8.0 - 6.0 * s,
                 "LowerArm.R": -8.0 + 6.0 * s,
                 "Spine": 1.5 * math.sin(2 * a),
                 "Head": -1.5 * math.sin(2 * a),
             }),
             loc={"Hips": (0.0, 0.022 * abs(math.sin(a)) - 0.011, 0.0)})
    made.append(("Walk", 1, n + 1))

    # ---------------- CarryWalk : 1 s ----------------
    _reset_pose(rig)
    act = _new_action(rig, "CarryWalk")
    n = fps
    for f in range(0, n + 1, 2):
        t = f / n
        a = math.tau * t
        s = math.sin(a)
        _key(rig, axes, f + 1,
             rot=full({
                 "UpperLeg.L": 20.0 * s,
                 "UpperLeg.R": -20.0 * s,
                 "LowerLeg.L": -26.0 * max(0.0, math.sin(a - 1.0)),
                 "LowerLeg.R": -26.0 * max(0.0, math.sin(a - 1.0 + math.pi)),
                 "Foot.L": 6.0 * math.sin(a + 1.2),
                 "Foot.R": 6.0 * math.sin(a + 1.2 + math.pi),
                 "UpperArm.L": 68.0 + 3.0 * s,
                 "UpperArm.R": 68.0 - 3.0 * s,
                 "LowerArm.L": 30.0,
                 "LowerArm.R": 30.0,
                 "Spine": 2.0 + 1.0 * math.sin(2 * a),
                 "Head": -2.0,
             }),
             loc={"Hips": (0.0, 0.018 * abs(math.sin(a)) - 0.009, 0.0)})
    made.append(("CarryWalk", 1, n + 1))

    _reset_pose(rig)
    # stash every action into its own NLA track so the FBX exporter sees them
    for name, _s, _e in made:
        act = bpy.data.actions[name]
        rig.animation_data.action = None
        tr = rig.animation_data.nla_tracks.new()
        tr.name = name
        st = tr.strips.new(name, 1, act)
        st.name = name
        tr.mute = True
    rig.animation_data.action = bpy.data.actions["Idle"]
    return made


def bind(rig, mesh):
    parent_keep(mesh, rig)
    m = mesh.modifiers.new("Armature", 'ARMATURE')
    m.object = rig
    m.use_vertex_groups = True
    return mesh


# --------------------------------------------------------------------------
# face kit (shared by both characters)
# --------------------------------------------------------------------------

def build_face(prefix, center, radii, mats, eye_z=None, brow_z=None,
               nose_z=None, mouth_z=None, eye_dx=0.072, scale=1.0):
    """Returns list of facial-detail objects, all laid ON the head surface."""
    cx, cy, cz = center
    eye_z = eye_z if eye_z is not None else cz + 0.008
    brow_z = brow_z if brow_z is not None else cz + 0.078
    nose_z = nose_z if nose_z is not None else cz - 0.025
    mouth_z = mouth_z if mouth_z is not None else cz - 0.098
    out = []

    for sgn, side in ((1, "L"), (-1, "R")):
        x = sgn * eye_dx
        p = ellipsoid_front(center, radii, x, eye_z, inward=0.021)
        e = sphere("%s_eye_%s" % (prefix, side), 0.046 * scale, p, 20, 10,
                   scale=(1.0, 0.5, 1.2), mat=mats["eye"], bone="Head",
                   face_detail=True)
        out.append(e)
        p2 = ellipsoid_front(center, radii, x, eye_z - 0.004, inward=0.010)
        pu = sphere("%s_pupil_%s" % (prefix, side), 0.030 * scale, p2, 16, 8,
                    scale=(1.0, 0.5, 1.15), mat=mats["dark"], bone="Head",
                    face_detail=True)
        out.append(pu)
        # eyebrow: short arc laid on the surface
        pts = []
        for k in range(7):
            t = -1.0 + 2.0 * k / 6.0
            bx = x + sgn * t * 0.036
            bz = brow_z + 0.007 * (t * sgn) + 0.005 * (1.0 - t * t)
            pts.append(ellipsoid_front(center, radii, bx, bz, inward=0.004))
        out.append(tube_along("%s_brow_%s" % (prefix, side), pts, 0.0095 * scale,
                              8, mat=mats["hair"], bone="Head", face_detail=True))

    # nose
    pn = ellipsoid_front(center, radii, 0.0, nose_z, inward=0.020)
    out.append(sphere("%s_nose" % prefix, 0.034 * scale, pn, 18, 9,
                      scale=(1.0, 0.95, 0.78), mat=mats["skin"], bone="Head",
                      face_detail=True))

    # smile: tube whose spine follows the head surface
    pts = []
    for k in range(11):
        t = -1.0 + 2.0 * k / 10.0
        mx = t * 0.055
        mz = mouth_z + 0.030 * t * t
        pts.append(ellipsoid_front(center, radii, mx, mz, inward=0.002))
    taper = [0.55, 0.75, 0.9, 1.0, 1.0, 1.0, 1.0, 1.0, 0.9, 0.75, 0.55]
    out.append(tube_along("%s_mouth" % prefix, pts, 0.014 * scale, 10,
                          mat=mats["hair"], bone="Head", face_detail=True,
                          taper=taper))
    return out


# --------------------------------------------------------------------------
# 01_player_character
# --------------------------------------------------------------------------

def build_player():
    name = "01_player_character"
    A = Asset(name)
    SKIN = "MAT_SkinWarm"
    body = []

    # legs / shoes
    for sgn, side in ((1, "L"), (-1, "R")):
        body.append(box("plr_shoe_%s" % side, (0.145, 0.285, 0.10),
                        (sgn * 0.115, -0.035, 0.05), mat="MAT_DarkNavy",
                        bone="Foot.%s" % side))
        body.append(capsule("plr_shin_%s" % side, (sgn * 0.112, 0, 0.10),
                            (sgn * 0.111, 0, 0.50), 0.076, 0.084, 16, 4,
                            mat="MAT_DarkBlueGray", bone="LowerLeg.%s" % side))
        body.append(capsule("plr_thigh_%s" % side, (sgn * 0.108, 0, 0.42),
                            (sgn * 0.105, 0, 0.86), 0.094, 0.104, 16, 4,
                            mat="MAT_DarkBlueGray", bone="UpperLeg.%s" % side))
    body.append(box("plr_hips", (0.375, 0.265, 0.22), (0, 0, 0.87),
                    mat="MAT_DarkBlueGray", bone="Hips", taper_bot=0.94))

    # torso + apron
    body.append(box("plr_torso", (0.40, 0.275, 0.33), (0, 0, 0.135 + 0.995),
                    mat="MAT_Cream", bone="Chest", taper_top=0.94, taper_bot=0.97))
    body.append(box("plr_apron", (0.305, 0.055, 0.40), (0, -0.128, 1.06),
                    mat="MAT_Terracotta", bone="Chest", taper_top=0.72))
    for sgn, side in ((1, "L"), (-1, "R")):
        body.append(box("plr_strap_%s" % side, (0.050, 0.036, 0.135),
                        (sgn * 0.093, -0.117, 1.283), mat="MAT_Terracotta",
                        bone="Chest", rot=(-0.30, 0, 0)))

    # arms
    for sgn, side in ((1, "L"), (-1, "R")):
        body.append(capsule("plr_sleeve_%s" % side, (sgn * 0.155, 0, 1.235),
                            (sgn * 0.222, 0, 1.055), 0.075, 0.070, 16, 4,
                            mat="MAT_Cream", bone="UpperArm.%s" % side))
        # forearm cap tucked fully inside the sleeve cuff (no skin poke-through)
        body.append(capsule("plr_forearm_%s" % side, (sgn * 0.2245, 0, 1.062),
                            (sgn * 0.247, 0, 0.805), 0.055, 0.051, 16, 4,
                            mat=SKIN, bone="LowerArm.%s" % side))
        body.append(sphere("plr_hand_%s" % side, 0.067, (sgn * 0.250, -0.008, 0.762),
                           20, 10, scale=(0.85, 1.0, 0.95), mat=SKIN,
                           bone="Hand.%s" % side))

    # neck + head
    body.append(capsule("plr_neck", (0, 0, 1.24), (0, 0, 1.335), 0.063, 0.063,
                        16, 4, mat=SKIN, bone="Neck"))
    HC = (0.0, 0.0, 1.492)
    HR = (0.18, 0.1764, 0.1872)
    body.append(sphere("plr_head", 0.18, HC, 36, 18, scale=(1.0, 0.98, 1.04),
                       mat=SKIN, bone="Head"))
    for sgn, side in ((1, "L"), (-1, "R")):
        body.append(sphere("plr_ear_%s" % side, 0.046, (sgn * 0.173, 0.006, 1.484),
                           18, 9, scale=(0.5, 1.0, 1.15), mat=SKIN, bone="Head"))

    # cap
    body.append(dome_shell("plr_cap_crown", HC, 0.197, 0.184,
                           lambda a: D2R(60.0), 32, 6,
                           scale=(1.0, 0.99, 1.045), mat="MAT_WarmRed",
                           bone="Head"))
    body.append(pie_prism("plr_cap_brim", 0.206, 0.052, 1.5775, 1.6035,
                          196.0, 344.0, 22, loc=(0, 0, 0), mat="MAT_WarmRed",
                          scale=(1.0, 1.34, 1.0), bone="Head"))

    face = build_face("plr", HC, HR,
                      {"eye": "MAT_Cream", "dark": "MAT_DarkNavy",
                       "hair": "MAT_HairDarkBrown", "skin": SKIN},
                      eye_z=1.500, brow_z=1.568, nose_z=1.467, mouth_z=1.384)

    BEVELS = (("torso", 0.038, 3), ("hips", 0.036, 3), ("apron", 0.022, 3),
              ("shoe", 0.020, 3), ("strap", 0.012, 2), ("brim", 0.008, 2))
    for o in body:
        w, s = 0.0, 2
        for k, bw, bs in BEVELS:
            if k in o.name:
                w, s = bw, bs
                break
        finalize(o, bevel_w=w, bevel_seg=s)
    for o in face:
        shade(o, 40.0, weighted=False)

    A.rig = build_rig(name + "_RIG")
    move_to_collection(A.rig, A.lod_colls[0])
    parent_keep(A.rig, A.root)

    l0, l1, l2 = make_lods(A, body, face, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)
        bind(A.rig, ob)

    A.anchor("CARRY_ANCHOR", (0.0, -0.40, 1.00), display='SPHERE', size=0.07)
    A.anchor("HEAD_UI_ANCHOR", (0.0, 0.0, 1.95), display='SPHERE', size=0.07)
    return A


# --------------------------------------------------------------------------
# 02_customer_character
# --------------------------------------------------------------------------

def build_customer():
    name = "02_customer_character"
    A = Asset(name)
    SKIN = "MAT_SkinWarmDeep"
    body = []

    for sgn, side in ((1, "L"), (-1, "R")):
        body.append(box("cst_shoe_%s" % side, (0.150, 0.275, 0.105),
                        (sgn * 0.115, -0.030, 0.0525), mat="MAT_DarkNavy",
                        bone="Foot.%s" % side))
        body.append(capsule("cst_shin_%s" % side, (sgn * 0.113, 0, 0.105),
                            (sgn * 0.112, 0, 0.50), 0.082, 0.092, 16, 4,
                            mat="MAT_DarkBlueGray", bone="LowerLeg.%s" % side))
        body.append(capsule("cst_thigh_%s" % side, (sgn * 0.110, 0, 0.42),
                            (sgn * 0.107, 0, 0.86), 0.104, 0.116, 16, 4,
                            mat="MAT_DarkBlueGray", bone="UpperLeg.%s" % side))
    body.append(box("cst_hips", (0.405, 0.295, 0.22), (0, 0, 0.87),
                    mat="MAT_DarkBlueGray", bone="Hips", taper_bot=0.92))

    # rounder, softer torso -> different silhouette from the player
    body.append(box("cst_torso", (0.445, 0.315, 0.33), (0, 0, 1.13),
                    mat="MAT_Teal", bone="Chest", taper_top=0.88, taper_bot=1.0))
    # stripe reads on the front/back only - stays inside the sleeves
    body.append(box("cst_sweater_band", (0.428, 0.336, 0.078), (0, 0, 1.115),
                    mat="MAT_Mustard", bone="Chest"))
    body.append(box("cst_collar", (0.20, 0.19, 0.055), (0, 0, 1.288),
                    mat="MAT_Teal", bone="Chest"))

    for sgn, side in ((1, "L"), (-1, "R")):
        body.append(capsule("cst_sleeve_%s" % side, (sgn * 0.170, 0, 1.235),
                            (sgn * 0.235, 0, 0.935), 0.085, 0.072, 16, 4,
                            mat="MAT_Teal", bone="UpperArm.%s" % side))
        body.append(capsule("cst_forearm_%s" % side, (sgn * 0.2365, 0, 0.942),
                            (sgn * 0.250, 0, 0.805), 0.056, 0.052, 16, 4,
                            mat=SKIN, bone="LowerArm.%s" % side))
        body.append(sphere("cst_hand_%s" % side, 0.069, (sgn * 0.252, -0.008, 0.762),
                           20, 10, scale=(0.85, 1.0, 0.95), mat=SKIN,
                           bone="Hand.%s" % side))

    body.append(capsule("cst_neck", (0, 0, 1.24), (0, 0, 1.33), 0.066, 0.066,
                        16, 4, mat=SKIN, bone="Neck"))
    HC = (0.0, 0.0, 1.495)
    HR = (0.1938, 0.19, 0.19)
    body.append(sphere("cst_head", 0.19, HC, 36, 18, scale=(1.02, 1.0, 1.0),
                       mat=SKIN, bone="Head"))
    for sgn, side in ((1, "L"), (-1, "R")):
        body.append(sphere("cst_ear_%s" % side, 0.047, (sgn * 0.186, 0.006, 1.487),
                           18, 9, scale=(0.5, 1.0, 1.15), mat=SKIN, bone="Head"))

    # hair: hairline high at the front (-Y), long at the back
    def rim(a):
        t = (1.0 + math.cos(a - math.pi * 1.5)) * 0.5   # 1 at -Y, 0 at +Y
        return D2R(56.0 + (1.0 - t) * 37.0)
    body.append(dome_shell("cst_hair", HC, 0.205, 0.1925, rim, 34, 7,
                           scale=(1.02, 1.0, 1.0), mat="MAT_HairBrown",
                           bone="Head"))
    body.append(sphere("cst_hair_back", 0.115, (0.0, 0.145, 1.438), 20, 10,
                       scale=(1.15, 0.85, 1.0), mat="MAT_HairBrown", bone="Head"))

    face = build_face("cst", HC, HR,
                      {"eye": "MAT_Cream", "dark": "MAT_DarkNavy",
                       "hair": "MAT_HairBrown", "skin": SKIN},
                      eye_z=1.505, brow_z=1.575, nose_z=1.471, mouth_z=1.387,
                      eye_dx=0.076)

    BEVELS = (("torso", 0.042, 3), ("hips", 0.038, 3), ("band", 0.020, 3),
              ("collar", 0.020, 3), ("shoe", 0.020, 3))
    for o in body:
        w, s = 0.0, 2
        for k, bw, bs in BEVELS:
            if k in o.name:
                w, s = bw, bs
                break
        finalize(o, bevel_w=w, bevel_seg=s)
    for o in face:
        shade(o, 40.0, weighted=False)

    A.rig = build_rig(name + "_RIG")
    move_to_collection(A.rig, A.lod_colls[0])
    parent_keep(A.rig, A.root)

    l0, l1, l2 = make_lods(A, body, face, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)
        bind(A.rig, ob)

    A.anchor("CARRY_ANCHOR", (0.0, -0.41, 1.00), display='SPHERE', size=0.07)
    A.anchor("HEAD_UI_ANCHOR", (0.0, 0.0, 1.95), display='SPHERE', size=0.07)
    return A
