"""Shawarma Tycoon - Phase 2A city kit.

Grounds the restaurant in a street block: modular road and sidewalk, two
background building facades, a street lamp and a drive-by car.

Same conventions as Phase 1: Z up, meters, front faces local -Y, origin is
bottom-centre and ground contact sits at Z = 0.

Module tiling
-------------
Road and sidewalk tiles are 4 m long along X and are meant to be repeated
along X. The road tile spans both lanes in Y, so one row of tiles is a
complete street. The car also faces -Y, so traffic driving along +X is a
plain -90 degree yaw.
"""

import math
from st_lib import (box, sphere, capsule, lathe, torus, pie_prism, tube_along,
                    dome_shell, finalize, shade, Asset, make_lods, D2R)

TILE = 4.0          # module length along X
LANE = 3.6          # single lane width
ROAD_HALF = LANE    # road spans -3.6 .. 3.6 in Y


# --------------------------------------------------------------------------
# 40_road_straight
# --------------------------------------------------------------------------

def build_road_straight():
    name = "40_road_straight"
    A = Asset(name)
    p = []

    # asphalt slab, slightly crowned so it does not read as a flat decal
    p.append(box("road_surface", (TILE, ROAD_HALF * 2.0, 0.12), (0, 0, 0.06),
                 mat="MAT_Asphalt"))
    # centre dashes: two per tile keeps the rhythm readable while tiling
    for x in (-TILE * 0.25, TILE * 0.25):
        p.append(box("road_dash_%d" % (x > 0), (1.35, 0.16, 0.03),
                     (x, 0.0, 0.125), mat="MAT_RoadLine"))
    # lane edge lines
    for sy in (1, -1):
        p.append(box("road_edge_%d" % sy, (TILE, 0.12, 0.03),
                     (0, sy * (ROAD_HALF - 0.30), 0.125), mat="MAT_RoadLine"))
    # gutter strip against the curb
    for sy in (1, -1):
        p.append(box("road_gutter_%d" % sy, (TILE, 0.34, 0.035),
                     (0, sy * (ROAD_HALF - 0.17), 0.118), mat="MAT_CurbStone"))

    for o in p:
        finalize(o, bevel_w=0.012, bevel_seg=2)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)

    A.anchor("TILE_NEXT", (TILE, 0.0, 0.0), display='ARROWS', size=0.4)
    A.anchor("LANE_NEAR", (0.0, -LANE * 0.5, 0.13))
    A.anchor("LANE_FAR", (0.0, LANE * 0.5, 0.13))
    return A


# --------------------------------------------------------------------------
# 41_sidewalk_straight
# --------------------------------------------------------------------------

def build_sidewalk_straight():
    name = "41_sidewalk_straight"
    A = Asset(name)
    p = []
    depth = 2.6
    top = 0.16

    p.append(box("walk_base", (TILE, depth, top), (0, 0, top * 0.5),
                 mat="MAT_Sidewalk"))
    # curb lip on the -Y (road facing) edge
    p.append(box("walk_curb", (TILE, 0.26, top + 0.06),
                 (0, -depth * 0.5 + 0.13, (top + 0.06) * 0.5),
                 mat="MAT_CurbStone"))
    # paving joints - kept thick enough that the bevel never collapses them
    for i in range(3):
        x = -TILE * 0.5 + TILE * (i + 1) / 4.0
        p.append(box("walk_joint_%d" % i, (0.06, depth - 0.30, 0.03),
                     (x, 0.06, top - 0.005), mat="MAT_CurbStone"))
    p.append(box("walk_joint_long", (TILE - 0.10, 0.06, 0.03),
                 (0, 0.42, top - 0.005), mat="MAT_CurbStone"))
    # storm drain against the kerb: third material and a readable detail
    p.append(box("walk_drain", (0.52, 0.30, 0.05),
                 (TILE * 0.25, -depth * 0.5 + 0.34, top - 0.012),
                 mat="MAT_LampPost"))
    for i in range(3):
        p.append(box("walk_drain_bar_%d" % i, (0.44, 0.035, 0.03),
                     (TILE * 0.25, -depth * 0.5 + 0.26 + i * 0.08, top + 0.004),
                     mat="MAT_CurbStone"))

    for o in p:
        finalize(o, bevel_w=0.005 if ("joint" in o.name or "bar" in o.name) else 0.014,
                 bevel_seg=2)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)

    A.anchor("TILE_NEXT", (TILE, 0.0, 0.0), display='ARROWS', size=0.4)
    A.anchor("WALK_SURFACE", (0.0, 0.35, top))
    return A


# --------------------------------------------------------------------------
# 42_sidewalk_corner
# --------------------------------------------------------------------------

def build_sidewalk_corner():
    name = "42_sidewalk_corner"
    A = Asset(name)
    p = []
    size = 2.6
    top = 0.16

    p.append(box("corner_base", (size, size, top), (0, 0, top * 0.5),
                 mat="MAT_Sidewalk"))
    p.append(box("corner_curb_y", (size, 0.26, top + 0.06),
                 (0, -size * 0.5 + 0.13, (top + 0.06) * 0.5), mat="MAT_CurbStone"))
    p.append(box("corner_curb_x", (0.26, size, top + 0.06),
                 (-size * 0.5 + 0.13, 0, (top + 0.06) * 0.5), mat="MAT_CurbStone"))
    # rounded outside corner so the kerb reads as a real street corner
    p.append(lathe("corner_round",
                   [(0.0, 0.0), (0.62, 0.0), (0.62, top + 0.06), (0.0, top + 0.06)],
                   18, loc=(-size * 0.5 + 0.62, -size * 0.5 + 0.62, 0.0),
                   mat="MAT_CurbStone"))
    p.append(box("corner_joint", (size - 0.4, 0.06, 0.03),
                 (0.1, 0.35, top - 0.005), mat="MAT_CurbStone"))
    # matching drain so the corner shares the straight tile's palette
    p.append(box("corner_drain", (0.46, 0.28, 0.05),
                 (0.55, -size * 0.5 + 0.34, top - 0.012), mat="MAT_LampPost"))
    for i in range(3):
        p.append(box("corner_drain_bar_%d" % i, (0.38, 0.035, 0.03),
                     (0.55, -size * 0.5 + 0.27 + i * 0.075, top + 0.004),
                     mat="MAT_CurbStone"))

    for o in p:
        if "round" in o.name:
            width = 0.0
        elif "joint" in o.name or "bar" in o.name:
            width = 0.005
        else:
            width = 0.014
        finalize(o, bevel_w=width, bevel_seg=2)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)
    return A


# --------------------------------------------------------------------------
# 43 / 44 city building facades
# --------------------------------------------------------------------------

def _building(name, width, depth, floors, wall_mat, accent_mat, awning=False):
    A = Asset(name)
    p = []
    floor_h = 3.2
    ground_h = 3.8
    height = ground_h + floors * floor_h

    # ground floor shopfront
    p.append(box("bld_ground", (width, depth, ground_h), (0, 0, ground_h * 0.5),
                 mat=accent_mat))
    p.append(box("bld_ground_glass", (width * 0.74, 0.10, ground_h * 0.55),
                 (0, -depth * 0.5 + 0.02, ground_h * 0.5),
                 mat="MAT_WindowGlass"))
    p.append(box("bld_door", (0.95, 0.14, 2.15),
                 (width * 0.30, -depth * 0.5 - 0.01, 1.075), mat="MAT_WarmWood"))
    if awning:
        p.append(box("bld_awning", (width * 0.80, 0.90, 0.20),
                     (0, -depth * 0.5 - 0.42, ground_h - 0.35),
                     mat="MAT_Awning", taper_bot=1.0))
        for sx in (1, -1):
            # capsule ends are hemispheres: start at the radius so the foot
            # lands exactly on Z = 0 instead of sinking below it
            p.append(capsule("bld_awning_post_%d" % sx,
                             (sx * width * 0.36, -depth * 0.5 - 0.80, 0.055),
                             (sx * width * 0.36, -depth * 0.5 - 0.80, ground_h - 0.42),
                             0.055, 0.055, 10, 3, mat="MAT_LampPost"))

    # upper floors
    p.append(box("bld_body", (width, depth, floors * floor_h),
                 (0, 0, ground_h + floors * floor_h * 0.5), mat=wall_mat))
    p.append(box("bld_band", (width + 0.16, depth + 0.16, 0.30),
                 (0, 0, ground_h + 0.05), mat=accent_mat))

    columns = max(2, int(width // 1.9))
    for f in range(floors):
        z = ground_h + floor_h * (f + 0.55)
        for c in range(columns):
            x = -width * 0.5 + width * (c + 0.5) / columns
            p.append(box("bld_win_%d_%d" % (f, c), (0.92, 0.12, 1.30),
                         (x, -depth * 0.5 + 0.02, z), mat="MAT_WindowGlass"))
            p.append(box("bld_sill_%d_%d" % (f, c), (1.06, 0.20, 0.12),
                         (x, -depth * 0.5 - 0.03, z - 0.78), mat=accent_mat))

    # cornice + roof edge
    p.append(box("bld_cornice", (width + 0.30, depth + 0.30, 0.34),
                 (0, 0, height + 0.17), mat=accent_mat))
    p.append(box("bld_parapet", (width, depth, 0.55),
                 (0, 0, height + 0.34 + 0.275), mat=wall_mat, taper_top=0.97))

    for o in p:
        if "post" in o.name:
            width = 0.0                      # already round
        elif any(k in o.name for k in ("glass", "win", "sill", "door")):
            width = 0.008                    # thin inset panels
        else:
            width = 0.022
        finalize(o, bevel_w=width, bevel_seg=2)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)

    A.anchor("FACADE_CENTER", (0.0, -depth * 0.5, ground_h * 0.5))
    return A


def build_building_a():
    # Chunky rather than tower-like: the cozy style wants a wide silhouette.
    return _building("43_city_building_a", 8.0, 5.4, 2,
                     "MAT_BrickWarm", "MAT_Cream", awning=True)


def build_building_b():
    return _building("44_city_building_b", 8.2, 5.6, 4,
                     "MAT_BrickCool", "MAT_Sidewalk", awning=False)


# --------------------------------------------------------------------------
# 45_street_lamp
# --------------------------------------------------------------------------

def build_street_lamp():
    name = "45_street_lamp"
    A = Asset(name)
    p = []
    height = 4.4

    p.append(lathe("lamp_base", [(0.0, 0.0), (0.26, 0.0), (0.26, 0.10),
                                 (0.19, 0.16), (0.17, 0.30), (0.0, 0.32)],
                   16, mat="MAT_LampPost"))
    p.append(capsule("lamp_post", (0, 0, 0.26), (0, 0, height), 0.075, 0.058,
                     14, 3, mat="MAT_LampPost"))
    # arm reaching over the road (-Y)
    p.append(capsule("lamp_arm", (0, 0, height - 0.10), (0, -0.85, height + 0.12),
                     0.055, 0.048, 12, 3, mat="MAT_LampPost"))
    p.append(lathe("lamp_head",
                   [(0.0, 0.0), (0.30, 0.06), (0.32, 0.16), (0.26, 0.30),
                    (0.0, 0.34)],
                   18, loc=(0, -0.85, height + 0.02), mat="MAT_LampPost"))
    p.append(lathe("lamp_glow", [(0.0, 0.0), (0.26, 0.015), (0.24, 0.07), (0.0, 0.08)],
                   18, loc=(0, -0.85, height - 0.02), mat="MAT_LampGlow"))
    p.append(torus("lamp_collar", 0.085, 0.026, (0, 0, 0.62), 14, 8,
                   mat="MAT_Mustard"))

    for o in p:
        finalize(o, bevel_w=0.0, bevel_seg=2)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)

    A.anchor("LIGHT_ANCHOR", (0.0, -0.85, height - 0.05), display='SPHERE', size=0.2)
    return A


# --------------------------------------------------------------------------
# 46_city_car
# --------------------------------------------------------------------------

def build_city_car():
    name = "46_city_car"
    A = Asset(name)
    p = []
    length = 4.10          # along Y, because the car faces -Y
    width = 1.86
    wheel_r = 0.34

    # lower body
    p.append(box("car_body", (width, length, 0.72), (0, 0, wheel_r + 0.30),
                 mat="MAT_WarmRed", taper_bot=0.92))
    # nose and tail tapers keep the silhouette readable from above
    p.append(box("car_nose", (width * 0.92, 0.70, 0.52),
                 (0, -length * 0.5 + 0.22, wheel_r + 0.24),
                 mat="MAT_WarmRed", taper_top=0.86))
    p.append(box("car_tail", (width * 0.94, 0.60, 0.56),
                 (0, length * 0.5 - 0.22, wheel_r + 0.26),
                 mat="MAT_WarmRed", taper_top=0.90))
    # cabin
    p.append(box("car_cabin", (width * 0.86, length * 0.46, 0.62),
                 (0, 0.08, wheel_r + 0.94),
                 mat="MAT_WarmRed", taper_top=0.84, taper_top_y=0.80))
    p.append(box("car_windshield", (width * 0.74, 0.10, 0.44),
                 (0, 0.08 - length * 0.23, wheel_r + 0.96), mat="MAT_CarGlass"))
    p.append(box("car_rearglass", (width * 0.72, 0.10, 0.40),
                 (0, 0.08 + length * 0.23, wheel_r + 0.96), mat="MAT_CarGlass"))
    for sx in (1, -1):
        p.append(box("car_sideglass_%d" % sx, (0.09, length * 0.34, 0.36),
                     (sx * width * 0.40, 0.08, wheel_r + 0.98), mat="MAT_CarGlass"))
    # bumpers and lights
    for sy, tag in ((-1, "front"), (1, "rear")):
        p.append(box("car_bumper_%s" % tag, (width * 0.96, 0.22, 0.26),
                     (0, sy * (length * 0.5 - 0.05), wheel_r + 0.12),
                     mat="MAT_DarkBlueGray"))
    for sx in (1, -1):
        p.append(box("car_headlight_%d" % sx, (0.34, 0.10, 0.18),
                     (sx * width * 0.30, -length * 0.5 + 0.02, wheel_r + 0.40),
                     mat="MAT_LampGlow"))
        p.append(box("car_taillight_%d" % sx, (0.30, 0.10, 0.16),
                     (sx * width * 0.30, length * 0.5 - 0.02, wheel_r + 0.42),
                     mat="MAT_WarmRed"))
    # wheels
    for sx in (1, -1):
        for sy in (1, -1):
            centre = (sx * (width * 0.5 - 0.06), sy * (length * 0.5 - 0.85), wheel_r)
            p.append(lathe("car_tire_%d%d" % (sx, sy),
                           [(0.0, -0.13), (wheel_r * 0.55, -0.14),
                            (wheel_r, -0.10), (wheel_r, 0.10),
                            (wheel_r * 0.55, 0.14), (0.0, 0.13)],
                           16, loc=centre, rot=(0, D2R(90), 0), mat="MAT_Tire"))
            p.append(lathe("car_hub_%d%d" % (sx, sy),
                           [(0.0, 0.0), (wheel_r * 0.46, 0.0),
                            (wheel_r * 0.44, 0.05), (0.0, 0.06)],
                           14, loc=(centre[0] + sx * 0.05, centre[1], centre[2]),
                           rot=(0, D2R(90 * sx), 0), mat="MAT_Steel"))

    for o in p:
        rounded = any(k in o.name for k in ("tire", "hub"))
        finalize(o, bevel_w=0.0 if rounded else 0.035, bevel_seg=3)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)

    A.anchor("DRIVER_WINDOW", (width * 0.5, -0.25, wheel_r + 0.95),
             display='SPHERE', size=0.14)
    A.anchor("EXHAUST", (-width * 0.28, length * 0.5, 0.24))
    return A


CITY_BUILDERS = [
    ("40_road_straight", build_road_straight),
    ("41_sidewalk_straight", build_sidewalk_straight),
    ("42_sidewalk_corner", build_sidewalk_corner),
    ("43_city_building_a", build_building_a),
    ("44_city_building_b", build_building_b),
    ("45_street_lamp", build_street_lamp),
    ("46_city_car", build_city_car),
]
