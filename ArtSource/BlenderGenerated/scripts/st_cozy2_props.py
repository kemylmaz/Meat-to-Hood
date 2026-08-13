"""Shawarma Tycoon - Phase 3 props, environment modules and interaction pads.

Modelled from ArtSource/References/assetlist. Soft vocabulary, muted palette,
front on local -Y, origin bottom-centre, ground contact at Z = 0.
"""

import math
from st_lib import (box, sphere, capsule, lathe, torus, pie_prism, tube_along,
                    shade, Asset, make_lods, D2R)
from st_cozy2 import soft_box, soft_plate


def _finish(asset, parts, r1=0.5, r2=0.2):
    for o in parts:
        shade(o, 34.0, weighted=True)
    l0, l1, l2 = make_lods(asset, parts, None, r1=r1, r2=r2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        asset.set_lod(lv, ob)
    return asset


# --------------------------------------------------------------------------
# dining table (normalmasa / bulaşıklı masa)
# --------------------------------------------------------------------------

TOP_Z = 0.74
TABLE = 1.32


def _chair(parts, tag, cy, facing):
    """Rounded chair; the back always points away from the table."""
    seat_z = 0.44
    parts.append(soft_box("chair%s_seat" % tag, (0.56, 0.54, 0.13),
                          (0, cy, seat_z), 0.055, 4, mat="MAT_ChairRed"))
    parts.append(soft_box("chair%s_back" % tag, (0.56, 0.14, 0.62),
                          (0, cy + facing * 0.24, seat_z + 0.34), 0.062, 4,
                          mat="MAT_ChairRed"))
    for sx in (1, -1):
        for sy in (1, -1):
            parts.append(capsule("chair%s_leg_%d%d" % (tag, sx, sy),
                                 (sx * 0.20, cy + sy * 0.19, 0.055),
                                 (sx * 0.20, cy + sy * 0.19, seat_z - 0.03),
                                 0.055, 0.055, 10, 3, mat="MAT_Taupe"))


def _table_base(parts):
    parts.append(soft_box("table_top", (TABLE, TABLE, 0.13), (0, 0, TOP_Z),
                          0.058, 4, mat="MAT_TableCream"))
    for sx in (1, -1):
        for sy in (1, -1):
            parts.append(capsule("table_leg_%d%d" % (sx, sy),
                                 (sx * 0.50, sy * 0.50, 0.075),
                                 (sx * 0.50, sy * 0.50, TOP_Z - 0.06),
                                 0.075, 0.075, 12, 3, mat="MAT_Taupe"))
    # napkin holder in the middle
    parts.append(soft_box("table_holder", (0.20, 0.16, 0.17), (0, 0, TOP_Z + 0.15),
                          0.045, 4, mat="MAT_Taupe"))
    parts.append(soft_plate("table_napkin", (0.15, 0.03, 0.15), (0, -0.02, TOP_Z + 0.19),
                            0.020, mat="MAT_SauceCream"))


def build_dining_table_clean():
    A = Asset("70_dining_table")
    p = []
    _table_base(p)
    _chair(p, "A", -1.02, -1.0)
    _chair(p, "B", 1.02, 1.0)
    _finish(A, p)
    A.anchor("SEAT_A", (0.0, -1.02, 0.50), display='ARROWS', size=0.18)
    A.anchor("SEAT_B", (0.0, 1.02, 0.50), display='ARROWS', size=0.18)
    A.anchor("CASH_PAD_ANCHOR", (0.44, 0.0, TOP_Z + 0.08))
    A.anchor("DIRTY_DISH_ANCHOR", (-0.40, 0.0, TOP_Z + 0.08))
    return A


def build_dining_table_dirty():
    A = Asset("71_dining_table_dirty")
    p = []
    _table_base(p)
    _chair(p, "A", -1.02, -1.0)
    _chair(p, "B", 1.02, 1.0)

    # two used plates, crumpled napkins and scattered crumbs
    for sx, sy in ((-1, 0.16), (1, -0.16)):
        p.append(lathe("table_plate_%d" % sx,
                       [(0.0, 0.0), (0.22, 0.008), (0.235, 0.035), (0.20, 0.042),
                        (0.0, 0.030)], 22,
                       loc=(sx * 0.34, sy, TOP_Z + 0.068), mat="MAT_SauceCream"))
    for i, (dx, dy) in enumerate(((-0.10, -0.36), (0.26, 0.30))):
        p.append(soft_plate("table_used_napkin_%d" % i, (0.24, 0.20, 0.028),
                            (dx, dy, TOP_Z + 0.075), 0.012, mat="MAT_SauceCream"))
    crumbs = ((-0.22, 0.30), (-0.06, 0.34), (0.10, 0.24), (0.30, -0.30),
              (0.16, -0.34), (-0.30, -0.26), (0.40, 0.12), (-0.42, -0.06))
    for i, (dx, dy) in enumerate(crumbs):
        p.append(soft_box("table_crumb_%d" % i, (0.055, 0.055, 0.045),
                          (dx, dy, TOP_Z + 0.085), 0.016, 2,
                          mat="MAT_BoardAmber" if i % 2 else "MAT_MeatBrown"))

    _finish(A, p)
    A.anchor("SEAT_A", (0.0, -1.02, 0.50), display='ARROWS', size=0.18)
    A.anchor("SEAT_B", (0.0, 1.02, 0.50), display='ARROWS', size=0.18)
    A.anchor("DIRTY_DISH_ANCHOR", (-0.40, 0.0, TOP_Z + 0.08))
    return A


# --------------------------------------------------------------------------
# 72_trash_bin (çöp)
# --------------------------------------------------------------------------

def build_trash_bin():
    A = Asset("72_trash_bin")
    p = []
    W = 0.66

    p.append(soft_box("bin_body", (W, W, 0.62), (0, 0, 0.31), 0.085, 5,
                      mat="MAT_BinTeal"))
    # collar sits proud of the body and holds the swing flap
    p.append(soft_box("bin_collar", (W * 1.05, W * 1.05, 0.30), (0, 0, 0.735),
                      0.085, 5, mat="MAT_BinTeal"))
    p.append(soft_box("bin_mouth", (W * 0.74, W * 0.80, 0.16), (0, 0.02, 0.760),
                      0.048, 4, mat="MAT_CounterDark"))
    # cream flap, tilted like the reference
    p.append(soft_plate("bin_flap", (W * 0.72, W * 0.74, 0.075), (0, -0.02, 0.845),
                        0.038, mat="MAT_PanelCream", rot=(D2R(-14), 0, 0)))
    p.append(soft_box("bin_flap_grip", (0.16, 0.04, 0.028), (0, -0.20, 0.828),
                      0.012, 2, mat="MAT_PanelCream"))
    # foot pedal on the -Y face
    p.append(soft_box("bin_pedal", (0.17, 0.12, 0.09), (-0.10, -W * 0.5 - 0.05, 0.075),
                      0.032, 3, mat="MAT_Taupe"))

    _finish(A, p)
    A.anchor("INPUT_ANCHOR", (0.0, -0.10, 0.92))
    return A


# --------------------------------------------------------------------------
# 73_planter (dekorasyonsaksı)
# --------------------------------------------------------------------------

def build_planter():
    A = Asset("73_planter")
    p = []

    p.append(lathe("pot_body",
                   [(0.0, 0.0), (0.155, 0.0), (0.175, 0.06), (0.215, 0.34),
                    (0.222, 0.40), (0.248, 0.415), (0.252, 0.475),
                    (0.228, 0.490), (0.196, 0.470), (0.0, 0.455)], 26,
                   mat="MAT_Terracotta"))
    p.append(lathe("pot_soil", [(0.0, 0.44), (0.205, 0.445), (0.198, 0.465),
                                (0.0, 0.470)], 24, mat="MAT_SoilDark"))

    # six leaves on curved stems
    for i in range(6):
        a = D2R(i * 60.0 + 18.0)
        reach = 0.30 + 0.05 * (i % 3)
        top = 0.92 + 0.10 * (i % 3)
        stem = []
        for k in range(6):
            t = k / 5.0
            stem.append((math.cos(a) * reach * t * t,
                         math.sin(a) * reach * t * t,
                         0.46 + (top - 0.46) * t))
        p.append(tube_along("plant_stem_%d" % i, stem, 0.022, 7,
                            mat="MAT_LeafGreen"))
        tip = stem[-1]
        p.append(sphere("plant_leaf_%d" % i, 0.145,
                        (tip[0] + math.cos(a) * 0.10,
                         tip[1] + math.sin(a) * 0.10, tip[2] + 0.02), 18, 9,
                        scale=(1.0, 0.62, 0.34),
                        rot=(0, 0, a), mat="MAT_LeafGreen"))

    _finish(A, p)
    return A


# --------------------------------------------------------------------------
# 74 / 75 tray stacks
# --------------------------------------------------------------------------

def _tray(parts, name, z, w=0.62, d=0.46):
    parts.append(soft_box(name, (w, d, 0.075), (0, 0, z), 0.034, 4,
                          mat="MAT_Taupe"))


def build_wrap_stack():
    A = Asset("74_wrap_tray_stack")
    p = []
    for i in range(4):
        z = 0.038 + i * 0.135
        _tray(p, "wrap_tray_%d" % i, z)
        for j, dy in enumerate((-0.11, 0.11)):
            cy = dy
            p.append(capsule("wrap_roll_%d_%d" % (i, j),
                             (-0.21, cy, z + 0.098), (0.21, cy, z + 0.098),
                             0.058, 0.058, 14, 4, mat="MAT_LavashPale"))
            p.append(soft_box("wrap_band_%d_%d" % (i, j), (0.11, 0.135, 0.128),
                              (0.0, cy, z + 0.098), 0.030, 3,
                              mat="MAT_UniformRed"))
    _finish(A, p)
    A.anchor("TOP_ANCHOR", (0.0, 0.0, 0.038 + 3 * 0.135 + 0.17))
    return A


def build_meat_stack():
    A = Asset("75_meat_tray_stack")
    p = []
    # Four trays of five slices keeps a small prop inside its triangle budget;
    # six of seven pushed it near 10k.
    trays = 4
    for i in range(trays):
        z = 0.038 + i * 0.112
        _tray(p, "meat_tray_%d" % i, z)
        for j in range(5):
            x = -0.19 + j * 0.095
            p.append(soft_box("meat_slice_%d_%d" % (i, j), (0.062, 0.30, 0.058),
                              (x, 0.0, z + 0.072), 0.022, 2,
                              mat="MAT_BoardAmber" if i == trays - 1 else "MAT_MeatBrown",
                              rot=(0, 0, D2R(8.0))))
    _finish(A, p)
    A.anchor("TOP_ANCHOR", (0.0, 0.0, 0.038 + (trays - 1) * 0.112 + 0.14))
    return A


# --------------------------------------------------------------------------
# 76 / 77 walls
# --------------------------------------------------------------------------

WALL_H = 1.46
WALL_T = 0.34
WALL_L = 3.00


def _wall_run(parts, tag, size, loc):
    """Cream body with a brown cap rail and a terracotta skirting."""
    w, d, h = size
    x, y, z = loc
    parts.append(soft_box("wall_%s_body" % tag, (w, d, h), (x, y, z + h * 0.5),
                          0.075, 4, mat="MAT_PanelCream"))
    parts.append(soft_box("wall_%s_skirt" % tag, (w * 1.03, d * 1.10, 0.20),
                          (x, y, z + 0.10), 0.065, 4, mat="MAT_Terracotta"))
    parts.append(soft_box("wall_%s_cap" % tag, (w * 1.02, d * 1.08, 0.17),
                          (x, y, z + h - 0.02), 0.062, 4, mat="MAT_WallCap"))


def build_wall_straight():
    A = Asset("77_wall_straight")
    p = []
    _wall_run(p, "main", (WALL_L, WALL_T, WALL_H), (0, 0, 0))
    _finish(A, p)
    A.anchor("TILE_NEXT", (WALL_L, 0.0, 0.0), display='ARROWS', size=0.4)
    return A


def build_wall_corner():
    A = Asset("76_wall_corner")
    p = []
    half = WALL_L * 0.5
    # two runs meeting at the -X/+Y corner, so the open side faces the camera
    _wall_run(p, "a", (WALL_L, WALL_T, WALL_H), (0, half - WALL_T * 0.5, 0))
    _wall_run(p, "b", (WALL_T, WALL_L - WALL_T, WALL_H),
              (-half + WALL_T * 0.5, -WALL_T * 0.5, 0))
    _finish(A, p)
    return A


# --------------------------------------------------------------------------
# 78 / 79 floors
# --------------------------------------------------------------------------

def _tile_grid(parts, name, span, z, count, mat, tile_h=0.055):
    pitch = span / count
    size = pitch * 0.90
    for i in range(count):
        for j in range(count):
            x = -span * 0.5 + pitch * (i + 0.5)
            y = -span * 0.5 + pitch * (j + 0.5)
            parts.append(soft_box("%s_%d_%d" % (name, i, j), (size, size, tile_h),
                                  (x, y, z), tile_h * 0.42, 2, mat=mat))


def build_floor_tiled():
    A = Asset("78_floor_tiled")
    p = []
    span = 3.00
    p.append(soft_box("floor_base", (span + 0.28, span + 0.28, 0.20),
                      (0, 0, 0.10), 0.075, 4, mat="MAT_PanelCream"))
    _tile_grid(p, "floor_tile", span, 0.225, 6, "MAT_TileTerracotta")
    _finish(A, p)
    A.anchor("TILE_NEXT", (span + 0.28, 0.0, 0.0), display='ARROWS', size=0.4)
    return A


def build_floor_plot():
    """Floating diorama plot: tiled deck, cream rim, tapered earth underside."""
    A = Asset("79_floor_plot")
    p = []
    span = 5.20

    # The earth tapers DOWN from the deck, so the whole block is lifted to keep
    # the lowest point on Z = 0 like every other asset.
    deck = 1.30
    p.append(lathe("plot_earth",
                   [(0.0, -1.28), (0.9, -1.05), (2.15, -0.55),
                    (2.72, -0.16), (2.80, 0.0), (0.0, 0.02)], 4,
                   loc=(0, 0, deck), rot=(0, 0, D2R(45.0)),
                   mat="MAT_PlotUnderside"))
    p.append(soft_box("plot_rim", (span + 0.44, span + 0.44, 0.34), (0, 0, deck),
                      0.115, 4, mat="MAT_PanelCream"))
    _tile_grid(p, "plot_tile", span, deck + 0.175, 10, "MAT_TileTerracotta",
               tile_h=0.05)

    # small bushes at the corners
    for sx in (1, -1):
        for sy in (1, -1):
            for k in range(3):
                p.append(sphere("plot_bush_%d%d_%d" % (sx, sy, k), 0.105,
                                (sx * (span * 0.5 + 0.06) - sx * k * 0.10,
                                 sy * (span * 0.5 + 0.06) - sy * (k % 2) * 0.08,
                                 deck + 0.20), 12, 6, scale=(1.0, 1.0, 0.72),
                                mat="MAT_LeafGreen"))

    _finish(A, p)
    A.anchor("DECK_ANCHOR", (0.0, 0.0, deck + 0.20))
    return A


# --------------------------------------------------------------------------
# 80_entrance
# --------------------------------------------------------------------------

def build_entrance():
    A = Asset("80_entrance")
    p = []
    W, H = 3.05, 2.90

    p.append(soft_box("ent_wall", (W, 0.30, H), (0.20, 0.16, H * 0.5), 0.105, 4,
                      mat="MAT_Taupe"))

    # Doorframe deliberately short of the wall top, so the awning has somewhere
    # to sit instead of crowding the opening.
    fw, fh, ft = 2.15, 2.10, 0.26
    p.append(soft_box("ent_frame_l", (0.30, ft, fh), (-0.72, -0.06, fh * 0.5),
                      0.085, 4, mat="MAT_PanelCream"))
    p.append(soft_box("ent_frame_r", (0.30, ft, fh), (1.12, -0.06, fh * 0.5),
                      0.085, 4, mat="MAT_PanelCream"))
    p.append(soft_box("ent_frame_top", (fw, ft, 0.30), (0.20, -0.06, fh - 0.15),
                      0.085, 4, mat="MAT_PanelCream"))
    p.append(soft_box("ent_sill", (fw + 0.26, ft + 0.16, 0.18), (0.20, -0.10, 0.09),
                      0.070, 4, mat="MAT_PanelCream"))

    # terracotta inner trim
    # Vertical trims run from the sill to the head trim; centred on the frame
    # they used to hang below ground.
    trim_top_z = fh - 0.28
    trim_h = trim_top_z - 0.20
    for tag, x in (("l", -0.55), ("r", 0.95)):
        p.append(soft_box("ent_trim_%s" % tag, (0.09, 0.10, trim_h),
                          (x, -0.19, 0.20 + trim_h * 0.5), 0.035, 3,
                          mat="MAT_Terracotta"))
    p.append(soft_box("ent_trim_top", (1.60, 0.10, 0.09), (0.20, -0.19, trim_top_z),
                      0.035, 3, mat="MAT_Terracotta"))
    p.append(soft_box("ent_trim_sill", (1.78, 0.12, 0.09), (0.20, -0.22, 0.20),
                      0.035, 3, mat="MAT_Terracotta"))

    # teal glass leaves, one swung open
    door_h = trim_top_z - 0.34
    p.append(soft_plate("ent_door_fixed", (0.62, 0.09, door_h),
                        (0.86, -0.14, 0.26 + door_h * 0.5), 0.045,
                        mat="MAT_DoorGlassTeal"))
    p.append(soft_plate("ent_door_open", (0.62, 0.09, door_h),
                        (-0.42, -0.34, 0.26 + door_h * 0.5), 0.045,
                        mat="MAT_DoorGlassTeal", rot=(0, 0, D2R(-22.0))))

    # scalloped awning, clear above the frame head
    awn_z = fh + 0.22
    for i in range(5):
        x = -0.52 + i * 0.36
        mat = "MAT_PanelCream" if i % 2 else "MAT_Terracotta"
        p.append(capsule("ent_awning_%d" % i, (x, -0.46, awn_z), (x, -0.10, awn_z),
                         0.185, 0.185, 14, 5, mat=mat))

    _finish(A, p)
    A.anchor("ENTRY_ANCHOR", (0.20, -0.90, 0.0), display='ARROWS', size=0.3)
    return A


# --------------------------------------------------------------------------
# 81..83 pads
# --------------------------------------------------------------------------

def build_lock_pad():
    A = Asset("81_lock_pad")
    p = []
    S = 1.36

    p.append(soft_box("lock_base", (S, S, 0.20), (0, 0, 0.10), 0.115, 5,
                      mat="MAT_PanelCream"))
    p.append(soft_box("lock_plate", (S * 0.74, S * 0.74, 0.09), (0, 0, 0.195),
                      0.045, 4, mat="MAT_LockPlate"))
    for sx in (1, -1):
        for sy in (1, -1):
            p.append(lathe("lock_bolt_%d%d" % (sx, sy),
                           [(0.0, 0.0), (0.062, 0.0), (0.062, 0.055), (0.0, 0.062)],
                           6, loc=(sx * S * 0.38, sy * S * 0.38, 0.195),
                           mat="MAT_KnifeSteel"))
    # padlock: body plus shackle
    p.append(soft_box("lock_body", (0.40, 0.26, 0.34), (0, 0, 0.40), 0.075, 4,
                      mat="MAT_TieGold"))
    # Full ring: its lower half hides inside the body, leaving a shackle.
    p.append(torus("lock_shackle", 0.135, 0.045, (0, 0, 0.575), 20, 10,
                   rot=(D2R(90), 0, 0), mat="MAT_TieGold"))
    p.append(lathe("lock_hole", [(0.0, 0.0), (0.045, 0.0), (0.045, 0.03), (0.0, 0.035)],
                   12, loc=(0, -0.132, 0.42), rot=(D2R(-90), 0, 0),
                   mat="MAT_WallCap"))

    _finish(A, p)
    A.anchor("PAD_ANCHOR", (0.0, 0.0, 0.22), display='SPHERE', size=0.12)
    return A


def build_money_pad():
    A = Asset("82_money_pad")
    p = []
    S = 1.28

    p.append(soft_box("money_base", (S, S, 0.20), (0, 0, 0.10), 0.115, 5,
                      mat="MAT_PanelCream"))
    p.append(soft_box("money_inner", (S * 0.72, S * 0.72, 0.075), (0, 0, 0.195),
                      0.055, 4, mat="MAT_Teal"))
    # three courses of banded notes
    for i in range(3):
        z = 0.245 + i * 0.085
        for j, dx in enumerate((-0.16, 0.16)):
            p.append(soft_box("money_note_%d_%d" % (i, j), (0.30, 0.44, 0.075),
                              (dx, 0.0, z), 0.028, 3, mat="MAT_MoneyGreen"))
        p.append(soft_box("money_band_%d" % i, (0.10, 0.46, 0.082), (0, 0, z),
                          0.024, 3, mat="MAT_LeafGreen"))
    for i, (dx, dy, dz) in enumerate(((0.42, -0.10, 0.58), (-0.40, 0.12, 0.50),
                                      (0.30, 0.26, 0.42))):
        p.append(sphere("money_spark_%d" % i, 0.070, (dx, dy, dz), 8, 4,
                        scale=(1.0, 1.0, 0.60), mat="MAT_TieGold"))

    _finish(A, p)
    A.anchor("PAD_ANCHOR", (0.0, 0.0, 0.22), display='SPHERE', size=0.12)
    return A


def build_upgrade_pad():
    A = Asset("83_upgrade_pad")
    p = []

    p.append(lathe("up_base", [(0.0, 0.0), (0.66, 0.0), (0.70, 0.06),
                               (0.70, 0.20), (0.64, 0.26), (0.0, 0.27)], 30,
                   mat="MAT_PadGold"))
    p.append(lathe("up_inner", [(0.0, 0.0), (0.54, 0.0), (0.545, 0.05),
                                (0.50, 0.075), (0.0, 0.080)], 28,
                   loc=(0, 0, 0.245), mat="MAT_PadMint"))

    # Arrow pointing up-and-right. Built as a shaft plus two chevron arms: a
    # revolved cone needed a compound rotation that came out facing sideways.
    z = 0.360
    p.append(soft_box("up_arrow_shaft", (0.17, 0.52, 0.10), (0.080, 0.095, z),
                      0.038, 3, mat="MAT_PanelCream", rot=(0, 0, D2R(-38.0))))
    for tag, cx, cy, deg in (("a", 0.071, 0.262, 96.95),
                             ("b", 0.238, 0.131, 186.95)):
        p.append(soft_box("up_arrow_head_%s" % tag, (0.17, 0.32, 0.10),
                          (cx, cy, z), 0.038, 3, mat="MAT_PanelCream",
                          rot=(0, 0, D2R(deg))))

    _finish(A, p)
    A.anchor("PAD_ANCHOR", (0.0, 0.0, 0.30), display='SPHERE', size=0.12)
    return A


COZY2_PROPS = [
    ("70_dining_table", build_dining_table_clean),
    ("71_dining_table_dirty", build_dining_table_dirty),
    ("72_trash_bin", build_trash_bin),
    ("73_planter", build_planter),
    ("74_wrap_tray_stack", build_wrap_stack),
    ("75_meat_tray_stack", build_meat_stack),
    ("76_wall_corner", build_wall_corner),
    ("77_wall_straight", build_wall_straight),
    ("78_floor_tiled", build_floor_tiled),
    ("79_floor_plot", build_floor_plot),
    ("80_entrance", build_entrance),
    ("81_lock_pad", build_lock_pad),
    ("82_money_pad", build_money_pad),
    ("83_upgrade_pad", build_upgrade_pad),
]
