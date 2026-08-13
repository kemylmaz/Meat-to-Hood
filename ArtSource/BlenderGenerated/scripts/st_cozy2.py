"""Shawarma Tycoon - Phase 3 "soft cozy" set.

Matches the assetlist reference renders: everything is heavily filleted, the
palette is muted, and the characters are chibi at roughly 3.3 heads tall with
no visible neck.

Same export conventions as before: Z up, meters, front faces local -Y, origin
bottom-centre, ground contact at Z = 0.

The single most important difference from the earlier packs is the fillet
radius. These references read soft because their corner radii are a large
fraction of the part size, so `soft_box` takes a radius rather than the small
cosmetic bevel `finalize` applies.
"""

import math
from st_lib import (box, sphere, capsule, lathe, torus, tube_along, bevel,
                    shade, ellipsoid_front, finalize, Asset, make_lods, D2R)


# --------------------------------------------------------------------------
# soft primitives
# --------------------------------------------------------------------------

def soft_box(name, size, loc=(0, 0, 0), radius=0.06, segments=4, mat=None,
             rot=None, bone=None, taper_top=1.0, taper_bot=1.0, face_detail=False):
    """Box with a large fillet. Radius is clamped so it can never collapse."""
    limit = min(size) * 0.49
    r = max(0.0, min(radius, limit))
    ob = box(name, size, loc, mat, taper_top, taper_bot, rot, bone, face_detail)
    if r > 0.0005:
        bevel(ob, r, max(2, segments))
    return ob


def soft_plate(name, size, loc, radius=0.05, mat=None, rot=None, bone=None):
    """Thin slab (table tops, aprons, screens) that stays soft on its long edges."""
    return soft_box(name, size, loc, radius, 3, mat, rot, bone)


# --------------------------------------------------------------------------
# character kit
# --------------------------------------------------------------------------

# Reference proportions, total height 1.70 m:
#   shoes 0.00-0.11 | legs 0.11-0.52 | torso 0.52-1.03 | head 1.00-1.53
#   hair/cap up to 1.70. Head is ~0.52 m across, so the figure reads as 3.3
#   heads tall, which is what makes the references feel chibi.
HEAD_CENTER = (0.0, 0.0, 1.29)
HEAD_R = (0.255, 0.245, 0.265)
SHOULDER_Z = 0.96
HIP_Z = 0.55


def _face(prefix, mats, center=HEAD_CENTER, radii=HEAD_R, scale=1.0):
    """Dot eyes, thin brows, button nose and a small smile, all projected onto
    the head surface so nothing can float."""
    cx, cy, cz = center
    out = []
    eye_dx = 0.088 * scale
    # Sits low enough that the brow line clears a cap visor.
    eye_z = cz - 0.012

    for sgn, side in ((1, "L"), (-1, "R")):
        # A flattened dot has only ~22 mm of depth, so it has to sit almost on
        # the surface or the head swallows it entirely.
        p = ellipsoid_front(center, radii, sgn * eye_dx, eye_z, inward=0.006)
        out.append(sphere("%s_eye_%s" % (prefix, side), 0.047 * scale, p, 18, 9,
                          scale=(0.82, 0.48, 1.30), mat=mats["ink"],
                          bone="Head", face_detail=True))

        pts = []
        for k in range(7):
            t = -1.0 + 2.0 * k / 6.0
            bx = sgn * eye_dx + sgn * t * 0.042 * scale
            bz = eye_z + 0.074 * scale + 0.011 * (t * sgn) - 0.007 * t * t
            pts.append(ellipsoid_front(center, radii, bx, bz, inward=0.004))
        out.append(tube_along("%s_brow_%s" % (prefix, side), pts, 0.0105 * scale,
                              8, mat=mats["ink"], bone="Head", face_detail=True))

    pn = ellipsoid_front(center, radii, 0.0, cz - 0.026, inward=0.028)
    out.append(sphere("%s_nose" % prefix, 0.040 * scale, pn, 16, 8,
                      scale=(1.0, 0.95, 0.88), mat=mats["skin"],
                      bone="Head", face_detail=True))

    pts = []
    for k in range(9):
        t = -1.0 + 2.0 * k / 8.0
        pts.append(ellipsoid_front(center, radii, t * 0.050 * scale,
                                   cz - 0.108 + 0.020 * t * t, inward=0.004))
    taper = [0.5, 0.75, 0.95, 1.0, 1.0, 1.0, 0.95, 0.75, 0.5]
    out.append(tube_along("%s_mouth" % prefix, pts, 0.0125 * scale, 10,
                          mat=mats["ink"], bone="Head", face_detail=True,
                          taper=taper))
    return out


def _hair(prefix, mat, center=HEAD_CENTER, radii=HEAD_R):
    """Soft brown cap of hair: low at the back and sides, swept clear of the brows."""
    cx, cy, cz = center
    parts = []

    def rim(a):
        # 1 at the face, 0 at the back of the head
        t = (1.0 + math.cos(a - math.pi * 1.5)) * 0.5
        return D2R(52.0 + (1.0 - t) * 42.0)

    from st_lib import dome_shell
    parts.append(dome_shell("%s_hair" % prefix, center,
                            radii[0] + 0.016, radii[0] + 0.003, rim, 32, 7,
                            scale=(1.0, radii[1] / radii[0], radii[2] / radii[0]),
                            mat=mat, bone="Head"))
    # thicker mass at the back so the silhouette is not a thin shell
    parts.append(sphere("%s_hair_back" % prefix, 0.155, (cx, cy + 0.150, cz + 0.055),
                        20, 10, scale=(1.15, 0.80, 0.95), mat=mat, bone="Head"))
    # sideburn wedges that read under a cap
    for sgn, side in ((1, "L"), (-1, "R")):
        parts.append(sphere("%s_hair_side_%s" % (prefix, side), 0.085,
                            (sgn * 0.215, 0.055, cz - 0.020), 14, 7,
                            scale=(0.55, 1.10, 1.05), mat=mat, bone="Head"))
    return parts


def _cap(prefix, mat, backwards=False, center=HEAD_CENTER, radii=HEAD_R):
    """Baseball cap: dome, button and a curved brim on the -Y face."""
    from st_lib import dome_shell, pie_prism
    cx, cy, cz = center
    parts = []
    # The crown has to come down close to the brows, otherwise it reads as a
    # beanie perched on top instead of a fitted cap.
    rim_polar = 70.0
    z_scale = (radii[2] + 0.014) / radii[0]
    r_out = radii[0] + 0.028
    parts.append(dome_shell("%s_cap" % prefix, center, r_out, radii[0] + 0.012,
                            lambda a: D2R(rim_polar), 34, 7,
                            scale=(1.0, radii[1] / radii[0], z_scale),
                            mat=mat, bone="Head"))
    parts.append(sphere("%s_cap_button" % prefix, 0.028,
                        (cx, cy, cz + (radii[2] + 0.014) + 0.026), 12, 6,
                        scale=(1.0, 1.0, 0.80), mat=mat, bone="Head"))

    # Visor sits at the crown rim so the two read as one piece, and projects
    # further forward than sideways. Flips to +Y when the cap is worn backwards.
    brim_z = cz + r_out * math.cos(D2R(rim_polar)) * z_scale
    a0, a1 = (194.0, 346.0) if not backwards else (14.0, 166.0)
    parts.append(pie_prism("%s_cap_brim" % prefix, radii[0] + 0.055, 0.075,
                           brim_z - 0.020, brim_z + 0.030, a0, a1, 24,
                           loc=(0, 0, 0), mat=mat, scale=(1.0, 1.24, 1.0),
                           bone="Head"))
    return parts


def _body(prefix, mats, torso_mat, sleeve_mat, trouser_mat, shoe_mat,
          apron_mat=None, apron_bib=True, vest_mat=None, tie=False, collar=False):
    """Shared chibi body: chunky legs, barrel torso, mitten hands, no neck."""
    parts = []
    skin = mats["skin"]

    for sgn, side in ((1, "L"), (-1, "R")):
        parts.append(soft_box("%s_shoe_%s" % (prefix, side), (0.180, 0.290, 0.125),
                              (sgn * 0.106, -0.034, 0.0625), 0.058, 4,
                              mat=shoe_mat, bone="Foot.%s" % side))
        parts.append(capsule("%s_shin_%s" % (prefix, side),
                             (sgn * 0.104, 0, 0.105), (sgn * 0.104, 0, 0.36),
                             0.093, 0.099, 16, 4, mat=trouser_mat,
                             bone="LowerLeg.%s" % side))
        parts.append(capsule("%s_thigh_%s" % (prefix, side),
                             (sgn * 0.102, 0, 0.30), (sgn * 0.100, 0, 0.60),
                             0.104, 0.112, 16, 4, mat=trouser_mat,
                             bone="UpperLeg.%s" % side))

    parts.append(soft_box("%s_hips" % prefix, (0.320, 0.250, 0.190),
                          (0, 0, 0.615), 0.078, 4, mat=trouser_mat, bone="Hips"))
    parts.append(soft_box("%s_torso" % prefix, (0.360, 0.278, 0.430),
                          (0, 0, 0.820), 0.105, 5, mat=torso_mat, bone="Chest",
                          taper_bot=0.94))

    if collar:
        parts.append(soft_box("%s_collar" % prefix, (0.190, 0.200, 0.060),
                              (0, -0.010, 1.005), 0.026, 3, mat=torso_mat,
                              bone="Chest"))

    if vest_mat is not None:
        # Must be slightly LARGER than the torso in both X and Y: sized under it
        # the big fillets swallow the vest and only a patch shows at the front.
        parts.append(soft_box("%s_vest" % prefix, (0.374, 0.298, 0.372),
                              (0, 0, 0.800), 0.095, 5, mat=vest_mat,
                              bone="Chest", taper_top=0.88))
        if tie:
            parts.append(soft_box("%s_tie" % prefix, (0.045, 0.040, 0.145),
                                  (0, -0.152, 0.930), 0.016, 3, mat=mats["tie"],
                                  bone="Chest"))

    if apron_mat is not None:
        # One tapered panel from bib to hem: a separate bib box left a visible
        # seam floating over the chest.
        parts.append(soft_box("%s_apron" % prefix, (0.298, 0.064, 0.500),
                              (0, -0.130, 0.775), 0.030, 3, mat=apron_mat,
                              bone="Chest", taper_top=0.78))
        if apron_bib:
            for sgn, side in ((1, "L"), (-1, "R")):
                parts.append(soft_box("%s_strap_%s" % (prefix, side),
                                      (0.046, 0.042, 0.150),
                                      (sgn * 0.079, -0.108, 1.015), 0.017, 3,
                                      mat=apron_mat, bone="Chest",
                                      rot=(-0.26, 0, 0)))
        parts.append(soft_plate("%s_apron_pocket" % prefix, (0.132, 0.028, 0.096),
                                (0, -0.168, 0.700), 0.022, mat=apron_mat,
                                bone="Chest"))

    # arms: sleeve down to the wrist, mitten hand, no elbow break
    for sgn, side in ((1, "L"), (-1, "R")):
        parts.append(capsule("%s_sleeve_%s" % (prefix, side),
                             (sgn * 0.150, 0, SHOULDER_Z),
                             (sgn * 0.208, 0, 0.660), 0.088, 0.070, 16, 4,
                             mat=sleeve_mat, bone="UpperArm.%s" % side))
        parts.append(sphere("%s_hand_%s" % (prefix, side), 0.072,
                            (sgn * 0.212, -0.010, 0.612), 18, 9,
                            scale=(0.88, 1.05, 1.0), mat=skin,
                            bone="LowerArm.%s" % side))

    parts.append(sphere("%s_head" % prefix, HEAD_R[0], HEAD_CENTER, 36, 18,
                        scale=(1.0, HEAD_R[1] / HEAD_R[0], HEAD_R[2] / HEAD_R[0]),
                        mat=skin, bone="Head"))
    for sgn, side in ((1, "L"), (-1, "R")):
        parts.append(sphere("%s_ear_%s" % (prefix, side), 0.055,
                            (sgn * 0.243, 0.010, HEAD_CENTER[2] - 0.010), 14, 7,
                            scale=(0.45, 1.0, 1.15), mat=skin, bone="Head"))
    return parts


def _finish_character(asset, body, face, rig_name):
    import st_chars
    for o in body:
        shade(o, 34.0, weighted=True)
    for o in face:
        shade(o, 40.0, weighted=False)

    from st_lib import move_to_collection, parent_keep
    asset.rig = st_chars.build_rig(rig_name, st_chars.CHIBI_RIG_NAME,
                                   st_chars.CHIBI_BONES)
    move_to_collection(asset.rig, asset.lod_colls[0])
    parent_keep(asset.rig, asset.root)

    l0, l1, l2 = make_lods(asset, body, face, r1=0.5, r2=0.2)
    for lv, ob in ((0, l0), (1, l1), (2, l2)):
        asset.set_lod(lv, ob)
        st_chars.bind(asset.rig, ob)

    asset.anchor("CARRY_ANCHOR", (0.0, -0.38, 0.86), display='SPHERE', size=0.07)
    asset.anchor("HEAD_UI_ANCHOR", (0.0, 0.0, 1.95), display='SPHERE', size=0.07)
    return asset


def _character(name, rig_name, build_extras, **body_kwargs):
    A = Asset(name)
    mats = {"skin": "MAT_SkinSoft", "ink": "MAT_FaceInk", "tie": "MAT_TieGold"}
    body = _body(name[:6], mats, **body_kwargs)
    body += build_extras(name[:6], mats)
    face = _face(name[:6], mats)
    return _finish_character(A, body, face, rig_name)


# --------------------------------------------------------------------------
# 50..52 customers
# --------------------------------------------------------------------------

def build_customer_vest_green():
    return _character(
        "50_customer_vest_green", "50_customer_vest_green_RIG",
        lambda p, m: _hair(p, "MAT_HairSoft"),
        torso_mat="MAT_ShirtIvory", sleeve_mat="MAT_ShirtIvory",
        trouser_mat="MAT_TrouserCharcoal", shoe_mat="MAT_ShoeBrown",
        vest_mat="MAT_VestOlive", tie=True, collar=True)


def build_customer_vest_navy():
    return _character(
        "51_customer_vest_navy", "51_customer_vest_navy_RIG",
        lambda p, m: _hair(p, "MAT_HairSoft"),
        torso_mat="MAT_ShirtSky", sleeve_mat="MAT_ShirtSky",
        trouser_mat="MAT_TrouserKhaki", shoe_mat="MAT_ShoeBrown",
        vest_mat="MAT_VestNavy", collar=True)


def build_customer_sweater():
    return _character(
        "52_customer_sweater", "52_customer_sweater_RIG",
        lambda p, m: _hair(p, "MAT_HairSoft"),
        torso_mat="MAT_SweaterTeal", sleeve_mat="MAT_SweaterTeal",
        trouser_mat="MAT_TrouserKhaki", shoe_mat="MAT_ShoeBrown")


# --------------------------------------------------------------------------
# 53..55 staff
# --------------------------------------------------------------------------

def _staff_head(cap_mat, backwards):
    def extras(prefix, mats):
        return _hair(prefix, "MAT_HairSoft") + _cap(prefix, cap_mat, backwards)
    return extras


def build_worker_teal():
    return _character(
        "53_worker_teal", "53_worker_teal_RIG",
        _staff_head("MAT_UniformTeal", False),
        torso_mat="MAT_ShirtIvory", sleeve_mat="MAT_ShirtIvory",
        trouser_mat="MAT_TrouserCharcoal", shoe_mat="MAT_ShoeCharcoal",
        apron_mat="MAT_UniformTeal")


def build_worker_red():
    return _character(
        "54_worker_red", "54_worker_red_RIG",
        _staff_head("MAT_UniformRed", False),
        torso_mat="MAT_ShirtIvory", sleeve_mat="MAT_ShirtIvory",
        trouser_mat="MAT_TrouserCharcoal", shoe_mat="MAT_ShoeCharcoal",
        apron_mat="MAT_UniformRed", collar=True)


def build_worker_red_backcap():
    return _character(
        "55_worker_red_backcap", "55_worker_red_backcap_RIG",
        _staff_head("MAT_UniformRed", True),
        torso_mat="MAT_ShirtIvory", sleeve_mat="MAT_ShirtIvory",
        trouser_mat="MAT_TrouserCharcoal", shoe_mat="MAT_ShoeCharcoal",
        apron_mat="MAT_UniformRed", collar=True)


COZY2_CHARACTERS = [
    ("50_customer_vest_green", build_customer_vest_green),
    ("51_customer_vest_navy", build_customer_vest_navy),
    ("52_customer_sweater", build_customer_sweater),
    ("53_worker_teal", build_worker_teal),
    ("54_worker_red", build_worker_red),
    ("55_worker_red_backcap", build_worker_red_backcap),
]
