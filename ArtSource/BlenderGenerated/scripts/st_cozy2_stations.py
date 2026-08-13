"""Shawarma Tycoon - Phase 3 stations, conveyors and manager desks.

Modelled from ArtSource/References/assetlist. Same soft vocabulary as
st_cozy2: large fillets, muted palette, front on local -Y, origin
bottom-centre, ground contact at Z = 0.
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
# 60_cutting_station  (kesmetezgahı)
# --------------------------------------------------------------------------

def build_cutting_station():
    A = Asset("60_cutting_station")
    p = []
    W, D = 1.86, 1.28

    # stacked slabs: taupe plinth, cream body, taupe worktop
    p.append(soft_box("cut_plinth", (W, D, 0.26), (0, 0, 0.13), 0.070, 4,
                      mat="MAT_Taupe"))
    p.append(soft_box("cut_drawer", (W * 0.74, 0.05, 0.16), (0, -D * 0.5 - 0.005, 0.135),
                      0.045, 3, mat="MAT_Taupe"))
    p.append(capsule("cut_drawer_pull", (0.30, -D * 0.5 - 0.035, 0.135),
                     (0.42, -D * 0.5 - 0.035, 0.135), 0.022, 0.022, 10, 3,
                     mat="MAT_TaupeDark"))
    p.append(soft_box("cut_body", (W * 0.97, D * 0.97, 0.60), (0, 0, 0.56), 0.105, 5,
                      mat="MAT_PanelCream"))
    p.append(soft_box("cut_top", (W, D, 0.16), (0, 0, 0.925), 0.062, 4,
                      mat="MAT_Taupe"))

    # raised backsplash bar
    p.append(soft_box("cut_splash", (W * 0.86, 0.20, 0.30), (0, D * 0.5 - 0.16, 1.14),
                      0.085, 4, mat="MAT_TaupeDark"))

    # amber board with a meat slab
    p.append(soft_plate("cut_board", (0.78, 0.62, 0.09), (-0.05, -0.08, 1.045),
                        0.038, mat="MAT_BoardAmber"))
    p.append(soft_box("cut_meat", (0.52, 0.42, 0.14), (-0.05, -0.08, 1.145), 0.055, 4,
                      mat="MAT_MeatBrown"))

    # small tray, and a knife resting on the right
    p.append(soft_box("cut_tray", (0.30, 0.26, 0.09), (-0.68, 0.20, 1.045), 0.036, 3,
                      mat="MAT_SauceCream"))
    p.append(soft_plate("cut_knife_blade", (0.44, 0.10, 0.035), (0.60, -0.12, 1.025),
                        0.016, mat="MAT_KnifeSteel"))
    p.append(soft_box("cut_knife_grip", (0.17, 0.075, 0.055), (0.34, -0.12, 1.030),
                      0.024, 3, mat="MAT_CounterDark"))

    _finish(A, p)
    A.anchor("INPUT_ANCHOR", (-0.55, -0.30, 1.02))
    A.anchor("OUTPUT_ANCHOR", (0.55, -0.30, 1.02))
    A.anchor("WORKER_ANCHOR", (0.0, -1.05, 0.0), display='ARROWS', size=0.22)
    return A


# --------------------------------------------------------------------------
# 61_wrap_station  (dürümhazırlamamasası)
# --------------------------------------------------------------------------

def build_wrap_station():
    A = Asset("61_wrap_station")
    p = []
    W, D = 1.78, 1.30

    p.append(soft_box("wrap_body", (W, D, 0.86), (0, 0, 0.43), 0.130, 5,
                      mat="MAT_PanelCream"))
    p.append(soft_box("wrap_top", (W * 1.02, D * 1.02, 0.18), (0, 0, 0.94), 0.070, 4,
                      mat="MAT_Taupe"))

    # three recessed wells along the back edge
    wells = (("lettuce", -0.46, "MAT_LettuceGreen"),
             ("tomato", 0.06, "MAT_TomatoRed"),
             ("sauce", 0.58, "MAT_SauceCream"))
    for name, x, mat in wells:
        p.append(soft_box("wrap_well_%s" % name, (0.44, 0.38, 0.10),
                          (x, D * 0.5 - 0.30, 1.005), 0.036, 3, mat="MAT_TaupeDark"))
        p.append(soft_box("wrap_fill_%s" % name, (0.34, 0.28, 0.11),
                          (x, D * 0.5 - 0.30, 1.045), 0.042, 4, mat=mat))

    # board with a flatbread
    p.append(soft_plate("wrap_board", (0.80, 0.60, 0.09), (-0.14, -0.22, 1.075),
                        0.038, mat="MAT_BoardAmber"))
    p.append(lathe("wrap_lavash",
                   [(0.0, 0.0), (0.28, 0.005), (0.30, 0.035), (0.27, 0.058),
                    (0.0, 0.064)], 26, loc=(-0.14, -0.22, 1.118),
                   mat="MAT_LavashPale"))

    _finish(A, p)
    A.anchor("INPUT_ANCHOR", (-0.52, -0.34, 1.06))
    A.anchor("OUTPUT_ANCHOR", (0.52, -0.34, 1.06))
    A.anchor("WORKER_ANCHOR", (0.0, -1.05, 0.0), display='ARROWS', size=0.22)
    return A


# --------------------------------------------------------------------------
# 62_cashier_counter  (kasa)
# --------------------------------------------------------------------------

def build_cashier_counter():
    A = Asset("62_cashier_counter")
    p = []
    W, D = 2.16, 1.04

    p.append(soft_box("cash_body", (W, D, 0.92), (0, 0, 0.46), 0.135, 5,
                      mat="MAT_PanelCream"))
    p.append(soft_box("cash_top", (W * 1.02, D * 1.04, 0.15), (0, 0, 0.975), 0.062, 4,
                      mat="MAT_CounterDark"))
    # recessed serving tray in the top
    p.append(soft_box("cash_tray", (W * 0.56, D * 0.52, 0.06), (-0.24, 0, 1.030),
                      0.035, 3, mat="MAT_BeltDark"))
    # terracotta kick plate on the customer side
    p.append(soft_plate("cash_kick", (W * 0.62, 0.06, 0.34), (-0.22, -D * 0.5 - 0.01, 0.20),
                        0.055, mat="MAT_Terracotta"))

    # register: body, keys, screen
    rx = W * 0.5 - 0.34
    p.append(soft_box("cash_reg_base", (0.50, 0.44, 0.10), (rx, 0.02, 1.100), 0.038, 3,
                      mat="MAT_Taupe"))
    p.append(soft_box("cash_reg_body", (0.44, 0.38, 0.20), (rx, 0.02, 1.240), 0.062, 4,
                      mat="MAT_PanelCream"))
    p.append(soft_box("cash_reg_screen", (0.20, 0.10, 0.17), (rx, 0.10, 1.420), 0.045, 4,
                      mat="MAT_Taupe"))
    p.append(soft_box("cash_reg_keys", (0.34, 0.12, 0.05), (rx, -0.14, 1.335), 0.020, 3,
                      mat="MAT_TaupeDark"))
    p.append(sphere("cash_reg_key_red", 0.036, (rx + 0.04, -0.16, 1.360), 12, 6,
                    scale=(1.0, 1.0, 0.55), mat="MAT_TomatoRed"))

    _finish(A, p)
    A.anchor("INPUT_ANCHOR", (-0.24, 0.0, 1.06))
    A.anchor("OUTPUT_ANCHOR", (-0.24, -0.34, 1.06))
    A.anchor("WORKER_ANCHOR", (0.0, 0.95, 0.0), display='ARROWS', size=0.22)
    return A


# --------------------------------------------------------------------------
# 63_conveyor_straight  (düzbant)
# --------------------------------------------------------------------------

def _conveyor_leg(parts, name, x, y, top):
    # Capsule ends are hemispheres, so the foot starts at its own radius to
    # land exactly on Z = 0 instead of sinking below it.
    r = 0.095
    parts.append(capsule(name, (x, y, r), (x, y, top), r, r, 12, 4,
                         mat="MAT_Taupe"))


def build_conveyor_straight():
    A = Asset("63_conveyor_straight")
    p = []
    L, W = 2.00, 0.84
    deck = 0.62

    for sx in (1, -1):
        for sy in (1, -1):
            _conveyor_leg(p, "belt_leg_%d%d" % (sx, sy),
                          sx * (L * 0.5 - 0.28), sy * (W * 0.5 - 0.16), deck - 0.10)

    # side plates with bolt caps
    for sy in (1, -1):
        p.append(soft_box("belt_side_%d" % sy, (L, 0.12, 0.22),
                          (0, sy * (W * 0.5 - 0.06), deck - 0.02), 0.055, 4,
                          mat="MAT_Taupe"))
        for sx in (1, -1):
            p.append(sphere("belt_bolt_%d%d" % (sx, sy), 0.045,
                            (sx * (L * 0.5 - 0.16), sy * (W * 0.5 + 0.005), deck - 0.02),
                            12, 6, scale=(1.0, 0.5, 1.0), mat="MAT_TaupeDark"))

    # orange stripe on the customer facing side
    p.append(soft_box("belt_stripe", (L * 0.74, 0.06, 0.075),
                      (0, -(W * 0.5 + 0.035), deck - 0.06), 0.030, 3,
                      mat="MAT_Terracotta"))

    # dark belt deck plus a roller at each end
    p.append(soft_box("belt_deck", (L * 0.92, W * 0.80, 0.11), (0, 0, deck + 0.055),
                      0.045, 4, mat="MAT_BeltDark"))
    for sx in (1, -1):
        p.append(capsule("belt_roller_%d" % sx,
                         (sx * L * 0.47, -W * 0.40, deck + 0.055),
                         (sx * L * 0.47, W * 0.40, deck + 0.055),
                         0.085, 0.085, 14, 4, mat="MAT_Taupe"))

    _finish(A, p)
    A.anchor("INPUT_ANCHOR", (-L * 0.5, 0.0, deck + 0.12))
    A.anchor("OUTPUT_ANCHOR", (L * 0.5, 0.0, deck + 0.12))
    return A


# --------------------------------------------------------------------------
# 64_conveyor_corner  (köşebant)
# --------------------------------------------------------------------------

def build_conveyor_corner():
    """Quarter turn: enters on -X, leaves on +Y. Inner radius faces -Y/-X."""
    A = Asset("64_conveyor_corner")
    p = []
    deck = 0.62
    r_in, r_out = 0.52, 1.28
    # arc centre sits at the inner corner so the belt sweeps the -X -> +Y quadrant
    cx, cy = -0.42, 0.42

    # Belt sits proud of the rails; sized level with them it disappeared behind
    # the frame from every angle except dead-on top.
    p.append(pie_prism("corner_deck", r_out - 0.08, r_in + 0.08,
                       deck + 0.02, deck + 0.14, 180.0, 270.0, 24,
                       loc=(cx, cy, 0.0), mat="MAT_BeltDark"))
    p.append(pie_prism("corner_outer", r_out, r_out - 0.14,
                       deck - 0.16, deck + 0.05, 178.0, 272.0, 24,
                       loc=(cx, cy, 0.0), mat="MAT_Taupe"))
    p.append(pie_prism("corner_inner", r_in + 0.14, r_in,
                       deck - 0.16, deck + 0.05, 178.0, 272.0, 22,
                       loc=(cx, cy, 0.0), mat="MAT_Taupe"))
    p.append(pie_prism("corner_stripe", r_in + 0.03, r_in - 0.03,
                       deck - 0.13, deck - 0.055, 182.0, 268.0, 22,
                       loc=(cx, cy, 0.0), mat="MAT_Terracotta"))

    for angle, tag in ((180.0, "a"), (270.0, "b")):
        a = D2R(angle)
        for radius, side in ((r_out - 0.07, "o"), (r_in + 0.07, "i")):
            _conveyor_leg(p, "corner_leg_%s%s" % (tag, side),
                          cx + radius * math.cos(a), cy + radius * math.sin(a),
                          deck - 0.10)
        p.append(sphere("corner_bolt_%s" % tag, 0.050,
                        (cx + (r_out - 0.07) * math.cos(a),
                         cy + (r_out - 0.07) * math.sin(a), deck - 0.02),
                        12, 6, mat="MAT_TaupeDark"))

    _finish(A, p)
    A.anchor("INPUT_ANCHOR", (cx - r_out + 0.34, cy, deck + 0.12))
    A.anchor("OUTPUT_ANCHOR", (cx, cy + r_out - 0.34, deck + 0.12))
    return A


# --------------------------------------------------------------------------
# 65..67 manager desks  (mudur1/2/3)
# --------------------------------------------------------------------------

def _manager_desk(name, chair_mat, extra):
    A = Asset(name)
    p = []
    W, D = 2.60, 2.20

    # two wall panels forming a corner, pale wood frame around a cream inset
    p.append(soft_box("desk_wall_back", (W, 0.16, 1.92), (0, D * 0.5 - 0.08, 0.96),
                      0.115, 5, mat="MAT_WoodPale"))
    p.append(soft_box("desk_wall_back_in", (W * 0.86, 0.06, 1.56),
                      (0, D * 0.5 - 0.17, 0.94), 0.075, 4, mat="MAT_PanelCream"))
    p.append(soft_box("desk_wall_side", (0.16, D * 0.86, 1.92),
                      (-W * 0.5 + 0.08, 0.14, 0.96), 0.115, 5, mat="MAT_WoodPale"))
    p.append(soft_box("desk_wall_side_in", (0.06, D * 0.72, 1.56),
                      (-W * 0.5 + 0.17, 0.14, 0.94), 0.075, 4, mat="MAT_PanelCream"))

    # waterfall desk: top plus two side panels down to the floor
    top_z = 0.78
    p.append(soft_box("desk_top", (1.72, 0.86, 0.12), (0.34, 0.10, top_z), 0.055, 4,
                      mat="MAT_WoodPale"))
    for sx, x in ((1, 1.14), (-1, -0.46)):
        p.append(soft_box("desk_leg_%d" % sx, (0.14, 0.86, top_z),
                          (x, 0.10, top_z * 0.5), 0.058, 4, mat="MAT_WoodPaleDark"))

    # monitor
    p.append(soft_box("desk_monitor", (0.74, 0.10, 0.46), (0.30, 0.44, 1.24), 0.075, 4,
                      mat="MAT_ScreenGrey"))
    p.append(soft_plate("desk_screen", (0.60, 0.045, 0.34), (0.30, 0.38, 1.24), 0.035,
                        mat="MAT_ScreenGlass"))
    p.append(soft_box("desk_monitor_neck", (0.10, 0.09, 0.16), (0.30, 0.44, 0.95),
                      0.035, 3, mat="MAT_ScreenGrey"))
    p.append(soft_box("desk_monitor_foot", (0.34, 0.22, 0.06), (0.30, 0.44, 0.875),
                      0.028, 3, mat="MAT_ScreenGrey"))

    # book stack
    p.append(soft_plate("desk_book_a", (0.44, 0.34, 0.055), (0.86, 0.02, 0.868), 0.024,
                        mat="MAT_PanelCream"))
    p.append(soft_plate("desk_book_b", (0.46, 0.36, 0.070), (0.86, 0.02, 0.925), 0.028,
                        mat="MAT_BookMint"))

    p += extra()

    # chair: seat, back, column and a four star base
    p.append(soft_box("chair_seat", (0.62, 0.60, 0.14), (-0.62, -0.62, 0.50), 0.062, 4,
                      mat=chair_mat))
    p.append(soft_box("chair_back", (0.58, 0.14, 0.70), (-0.62, -0.90, 0.90), 0.070, 4,
                      mat=chair_mat))
    for sx in (1, -1):
        p.append(soft_box("chair_arm_%d" % sx, (0.10, 0.42, 0.10),
                          (-0.62 + sx * 0.34, -0.66, 0.66), 0.042, 3, mat=chair_mat))
    p.append(capsule("chair_column", (-0.62, -0.62, 0.16), (-0.62, -0.62, 0.46),
                     0.062, 0.062, 12, 4, mat=chair_mat))
    for i in range(4):
        a = D2R(45.0 + i * 90.0)
        p.append(capsule("chair_star_%d" % i, (-0.62, -0.62, 0.12),
                         (-0.62 + 0.30 * math.cos(a), -0.62 + 0.30 * math.sin(a), 0.09),
                         0.055, 0.048, 10, 3, mat=chair_mat))

    _finish(A, p)
    A.anchor("WORKER_ANCHOR", (-0.62, -0.62, 0.0), display='ARROWS', size=0.22)
    A.anchor("TERMINAL_ANCHOR", (0.30, -0.40, 0.90))
    return A


def build_manager_desk_stamp():
    def extra():
        parts = []
        parts.append(lathe("desk_stamp_base",
                           [(0.0, 0.0), (0.13, 0.0), (0.13, 0.055), (0.10, 0.075),
                            (0.05, 0.085), (0.045, 0.16), (0.075, 0.20),
                            (0.055, 0.235), (0.0, 0.245)], 18,
                           loc=(1.32, 0.10, 0.845), mat="MAT_TieGold"))
        return parts
    return _manager_desk("65_manager_desk_stamp", "MAT_ChairRed", extra)


def build_manager_desk_pencils():
    def extra():
        parts = []
        parts.append(lathe("desk_cup", [(0.0, 0.0), (0.115, 0.0), (0.125, 0.20),
                                        (0.105, 0.20), (0.098, 0.02), (0.0, 0.018)], 18,
                           loc=(1.32, 0.10, 0.845), mat="MAT_Terracotta"))
        for i, (dx, dy, mat) in enumerate((
                (-0.03, 0.02, "MAT_LettuceGreen"),
                (0.03, -0.02, "MAT_TieGold"),
                (0.0, 0.04, "MAT_TomatoRed"))):
            parts.append(capsule("desk_pencil_%d" % i,
                                 (1.32 + dx, 0.10 + dy, 0.90),
                                 (1.32 + dx * 1.6, 0.10 + dy * 1.6, 1.16),
                                 0.020, 0.018, 8, 2, mat=mat))
        return parts
    return _manager_desk("66_manager_desk_pencils", "MAT_ChairBlue", extra)


def build_manager_desk_plant():
    def extra():
        parts = []
        parts.append(lathe("desk_pot", [(0.0, 0.0), (0.13, 0.0), (0.145, 0.16),
                                        (0.125, 0.17), (0.115, 0.02), (0.0, 0.016)], 18,
                           loc=(1.32, 0.10, 0.845), mat="MAT_Terracotta"))
        for i in range(4):
            a = D2R(i * 90.0 + 20.0)
            parts.append(sphere("desk_leaf_%d" % i, 0.10,
                                (1.32 + 0.08 * math.cos(a), 0.10 + 0.08 * math.sin(a),
                                 1.075 + 0.02 * i),
                                12, 6, scale=(1.0, 1.0, 0.55),
                                mat="MAT_LettuceGreen"))
        return parts
    return _manager_desk("67_manager_desk_plant", "MAT_ChairRed", extra)


COZY2_STATIONS = [
    ("60_cutting_station", build_cutting_station),
    ("61_wrap_station", build_wrap_station),
    ("62_cashier_counter", build_cashier_counter),
    ("63_conveyor_straight", build_conveyor_straight),
    ("64_conveyor_corner", build_conveyor_corner),
    ("65_manager_desk_stamp", build_manager_desk_stamp),
    ("66_manager_desk_pencils", build_manager_desk_pencils),
    ("67_manager_desk_plant", build_manager_desk_plant),
]
