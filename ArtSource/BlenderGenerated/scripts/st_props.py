"""Shawarma Tycoon - cozy stations, furniture and props.

Everything is authored so the customer facing side is local -Y,
origin bottom-center, ground contact at Z = 0.
"""

import bpy
import math
from st_lib import (box, sphere, capsule, lathe, torus, pie_prism, tube_along,
                    finalize, shade, Asset, make_lods, D2R)


# --------------------------------------------------------------------------
# 06_rotisserie_station
# --------------------------------------------------------------------------

def build_rotisserie():
    name = "06_rotisserie_station"
    A = Asset(name)
    p = []
    BW, BS = 0.020, 4

    # base ------------------------------------------------------------------
    p.append(box("rot_plinth", (0.96, 0.66, 0.075), (0, 0.03, 0.0375),
                 mat="MAT_DarkBlueGray"))
    p.append(box("rot_cabinet", (1.02, 0.72, 0.86), (0, 0.03, 0.505),
                 mat="MAT_DarkBlueGray", taper_bot=0.98))
    p.append(box("rot_door", (0.84, 0.06, 0.54), (0, -0.335, 0.52),
                 mat="MAT_Cream"))
    p.append(box("rot_door_line", (0.86, 0.04, 0.035), (0, -0.345, 0.80),
                 mat="MAT_Terracotta"))
    # cozy corner posts
    for sx in (1, -1):
        for sy, yy in ((-1, -0.292), (1, 0.352)):
            p.append(box("rot_post_%d%d" % (sx, sy), (0.078, 0.078, 0.865),
                         (sx * 0.482, yy, 0.505), mat="MAT_Cream"))
    # side vents
    for sx in (1, -1):
        for i, z in enumerate((0.30, 0.44, 0.58)):
            p.append(box("rot_vent_%d_%d" % (sx, i), (0.038, 0.38, 0.048),
                         (sx * 0.518, 0.03, z), mat="MAT_Cream"))
    # control knobs
    for i, z in enumerate((0.18, 0.32, 0.46)):
        p.append(lathe("rot_ctrl_%d" % i, [(0.0, 0.0), (0.050, 0.0), (0.053, 0.028),
                                           (0.038, 0.040), (0.0, 0.042)], 22,
                       loc=(0.335, -0.372, z), rot=(D2R(-90), 0, 0),
                       mat="MAT_Mustard"))
    # yellow handle
    p.append(capsule("rot_handle", (-0.24, -0.40, 0.62), (0.24, -0.40, 0.62),
                     0.024, 0.024, 16, 4, mat="MAT_Mustard"))
    for sgn in (1, -1):
        p.append(capsule("rot_handle_post_%d" % sgn, (sgn * 0.22, -0.352, 0.62),
                         (sgn * 0.22, -0.395, 0.62), 0.018, 0.018, 12, 3,
                         mat="MAT_Mustard"))
    # counter top -----------------------------------------------------------
    p.append(box("rot_counter", (1.10, 0.78, 0.09), (0, 0.03, 0.98),
                 mat="MAT_Cream"))
    p.append(box("rot_counter_lip", (1.12, 0.10, 0.045), (0, -0.335, 1.045),
                 mat="MAT_Terracotta"))

    # back column + heat panels --------------------------------------------
    p.append(box("rot_column", (1.00, 0.22, 0.775), (0, 0.30, 1.4125),
                 mat="MAT_DarkBlueGray"))
    for i, z in enumerate((1.19, 1.44, 1.69)):
        p.append(box("rot_heatpanel_%d" % i, (0.66, 0.075, 0.185), (0, 0.168, z),
                     mat="MAT_HeatOrange"))
    for sgn in (1, -1):
        p.append(box("rot_rail_%d" % sgn, (0.075, 0.26, 0.80),
                     (sgn * 0.478, 0.29, 1.40), mat="MAT_DarkBlueGray"))
    p.append(box("rot_hood", (1.14, 0.42, 0.115), (0, 0.20, 1.8575),
                 mat="MAT_Cream"))
    p.append(box("rot_hood_band", (1.16, 0.44, 0.035), (0, 0.20, 1.795),
                 mat="MAT_Mustard"))
    for i, x in enumerate((-0.34, -0.115, 0.115, 0.34)):
        p.append(box("rot_hoodvent_%d" % i, (0.135, 0.05, 0.055),
                     (x, -0.008, 1.858), mat="MAT_Terracotta"))
    p.append(capsule("rot_warmbar", (-0.40, 0.12, 1.782), (0.40, 0.12, 1.782),
                     0.030, 0.030, 16, 4, mat="MAT_HeatOrange"))

    # spit + doner cone -----------------------------------------------------
    p.append(lathe("rot_spit", [(0.0, 1.00), (0.024, 1.00), (0.024, 1.84),
                                (0.0, 1.845)], 20, loc=(0, -0.055, 0),
                   mat="MAT_Steel"))
    p.append(lathe("rot_cone_charred",
                   [(0.0, 1.055), (0.075, 1.055), (0.090, 1.115),
                    (0.122, 1.20), (0.148, 1.29), (0.155, 1.325), (0.0, 1.325)],
                   40, loc=(0, -0.055, 0), mat="MAT_DarkCookedMeat"))
    p.append(lathe("rot_cone",
                   [(0.0, 1.295), (0.150, 1.295), (0.172, 1.38), (0.192, 1.47),
                    (0.207, 1.57), (0.214, 1.655), (0.203, 1.715),
                    (0.155, 1.762), (0.080, 1.785), (0.0, 1.792)],
                   40, loc=(0, -0.055, 0), mat="MAT_MeatBrown"))
    p.append(torus("rot_cone_ring", 0.196, 0.014, (0, -0.055, 1.52), 40, 10,
                   mat="MAT_DarkCookedMeat"))
    p.append(torus("rot_cone_ring2", 0.178, 0.012, (0, -0.055, 1.39), 40, 10,
                   mat="MAT_DarkCookedMeat"))
    p.append(torus("rot_cone_ring3", 0.209, 0.012, (0, -0.055, 1.63), 40, 10,
                   mat="MAT_DarkCookedMeat"))
    p.append(lathe("rot_cone_cap", [(0.0, 1.775), (0.052, 1.782), (0.058, 1.812),
                                    (0.0, 1.822)], 24, loc=(0, -0.055, 0),
                   mat="MAT_Steel"))
    # drip tray
    p.append(box("rot_tray", (0.46, 0.36, 0.05), (0, -0.055, 1.05),
                 mat="MAT_Steel"))

    for o in p:
        rounded = o.name.split("rot_")[1].startswith(
            ("cone", "spit", "handle", "ctrl", "warmbar"))
        finalize(o, bevel_w=0.0 if rounded else BW, bevel_seg=BS)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)

    A.anchor("INPUT_ANCHOR", (0.0, -0.055, 1.90))
    A.anchor("OUTPUT_ANCHOR", (0.0, -0.30, 1.06))
    A.anchor("WORKER_ANCHOR", (0.0, -0.82, 0.0), display='ARROWS', size=0.22)
    return A


# --------------------------------------------------------------------------
# 15_dining_table
# --------------------------------------------------------------------------

def build_dining_table():
    name = "15_dining_table"
    A = Asset(name)
    p = []
    BW, BS = 0.018, 3

    p.append(box("tbl_top", (1.20, 0.80, 0.075), (0, 0, 0.7125),
                 mat="MAT_WarmWood"))
    p.append(box("tbl_apron", (1.08, 0.68, 0.065), (0, 0, 0.6425),
                 mat="MAT_WoodLight"))
    for sx in (1, -1):
        for sy in (1, -1):
            p.append(box("tbl_leg_%d%d" % (sx, sy), (0.085, 0.085, 0.61),
                         (sx * 0.50, sy * 0.305, 0.305), mat="MAT_WarmWood",
                         taper_bot=0.85))
    p.append(box("tbl_stretcher", (1.00, 0.07, 0.055), (0, 0, 0.20),
                 mat="MAT_WoodLight"))

    # two chairs, backs pointing AWAY from the table
    for sy, tag in ((-1, "A"), (1, "B")):
        cy = sy * 0.66
        p.append(box("chair%s_seat" % tag, (0.42, 0.42, 0.07), (0, cy, 0.435),
                     mat="MAT_Teal"))
        p.append(box("chair%s_cushion" % tag, (0.35, 0.35, 0.032),
                     (0, cy, 0.482), mat="MAT_Cream"))
        p.append(box("chair%s_back" % tag, (0.42, 0.075, 0.47),
                     (0, cy + sy * 0.1725, 0.665), mat="MAT_Teal",
                     taper_top=0.94))
        p.append(box("chair%s_backrail" % tag, (0.36, 0.10, 0.075),
                     (0, cy + sy * 0.1725, 0.855), mat="MAT_Cream"))
        for lx in (1, -1):
            for ly in (1, -1):
                p.append(box("chair%s_leg_%d%d" % (tag, lx, ly),
                             (0.055, 0.055, 0.42),
                             (lx * 0.165, cy + ly * 0.165, 0.21),
                             mat="MAT_DarkNavy", taper_bot=0.82))

    for o in p:
        finalize(o, bevel_w=BW, bevel_seg=BS)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)

    A.anchor("SEAT_A", (0.0, -0.66, 0.50), display='ARROWS', size=0.18)
    A.anchor("SEAT_B", (0.0, 0.66, 0.50), display='ARROWS', size=0.18)
    A.anchor("CASH_PAD_ANCHOR", (0.40, 0.0, 0.755))
    A.anchor("DIRTY_DISH_ANCHOR", (-0.32, 0.0, 0.755))
    return A


# --------------------------------------------------------------------------
# 17_trash_bin
# --------------------------------------------------------------------------

def build_trash_bin():
    name = "17_trash_bin"
    A = Asset(name)
    p = []
    SEG = 24

    # single closed shell: outside wall -> over the rim -> back down inside
    p.append(lathe("bin_body",
                   [(0.000, 0.000), (0.212, 0.000), (0.230, 0.028),
                    (0.243, 0.100), (0.256, 0.400), (0.266, 0.660),
                    (0.269, 0.700), (0.251, 0.706),
                    (0.247, 0.660), (0.237, 0.400), (0.224, 0.100),
                    (0.212, 0.038), (0.000, 0.058)],
                   SEG, mat="MAT_BinGreen"))
    # dark liner makes the opening read as a real hole from the -Y front
    p.append(lathe("bin_liner",
                   [(0.196, 0.060), (0.2305, 0.060), (0.2345, 0.699),
                    (0.196, 0.699)],
                   SEG, mat="MAT_DarkNavy"))
    p.append(lathe("bin_inner_floor",
                   [(0.0, 0.058), (0.205, 0.058), (0.207, 0.080), (0.0, 0.082)],
                   SEG, mat="MAT_DarkNavy"))
    p.append(torus("bin_rim", 0.2695, 0.022, (0, 0, 0.700), SEG, 8,
                   mat="MAT_DarkBlueGray"))
    p.append(torus("bin_belt", 0.2545, 0.021, (0, 0, 0.400), SEG, 6,
                   mat="MAT_DarkBlueGray"))
    # lid held up by two symmetric rear posts -> front opening stays clear
    for sx in (1, -1):
        p.append(box("bin_post_%d" % sx, (0.080, 0.090, 0.145),
                     (sx * 0.140, 0.212, 0.757), mat="MAT_DarkBlueGray"))
    p.append(lathe("bin_lid",
                   [(0.000, 0.792), (0.150, 0.795), (0.240, 0.802),
                    (0.281, 0.812), (0.286, 0.823), (0.282, 0.837),
                    (0.250, 0.862), (0.170, 0.886), (0.000, 0.898)],
                   SEG, mat="MAT_Cream"))
    p.append(sphere("bin_knob", 0.048, (0, 0, 0.900), 16, 8,
                    scale=(1.0, 1.0, 0.70), mat="MAT_DarkBlueGray"))

    for o in p:
        finalize(o, bevel_w=0.014 if "post" in o.name else 0.0, bevel_seg=3)

    l0, l1, l2 = make_lods(A, p, None, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        A.set_lod(lv, ob)

    A.anchor("INPUT_ANCHOR", (0.0, -0.12, 0.75))
    return A
