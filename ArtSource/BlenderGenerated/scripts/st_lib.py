"""Shawarma Tycoon - cozy mobile tycoon asset generation library.

Blender 5.1 / Python 3.13.
All geometry is generated in WORLD space with identity object transforms,
so "apply transforms" is satisfied by construction.

Conventions
-----------
* Z up, meters.
* Every asset FRONT faces local -Y.
* Origin at bottom-center, ground contact at Z = 0.
"""

import bpy
import bmesh
import math
import os
import json
from mathutils import Vector, Matrix, Euler

TAU = math.pi * 2.0
D2R = math.radians

# --------------------------------------------------------------------------
# Palette
# --------------------------------------------------------------------------
# name: (hex, roughness, metallic)
PALETTE = {
    "MAT_Cream":           ("F4DFC0", 0.75, 0.0),
    "MAT_Terracotta":      ("D97A55", 0.72, 0.0),
    "MAT_MeatBrown":       ("A9583B", 0.70, 0.0),
    "MAT_DarkCookedMeat":  ("743D2B", 0.72, 0.0),
    "MAT_Teal":            ("55B7AD", 0.70, 0.0),
    "MAT_Mustard":         ("F1C557", 0.70, 0.0),
    "MAT_WarmRed":         ("D9564A", 0.70, 0.0),
    "MAT_DarkBlueGray":    ("44515B", 0.75, 0.0),
    "MAT_WarmWood":        ("9A5A3A", 0.78, 0.0),
    "MAT_WoodLight":       ("BE8354", 0.78, 0.0),
    "MAT_SkinWarm":        ("E8B98C", 0.72, 0.0),
    "MAT_SkinWarmLight":   ("F2D2B0", 0.72, 0.0),
    "MAT_SkinWarmDeep":    ("C98C63", 0.72, 0.0),
    "MAT_HairDarkBrown":   ("3A2A22", 0.80, 0.0),
    "MAT_HairBrown":       ("6B4230", 0.80, 0.0),
    "MAT_HairAuburn":      ("8C4A2F", 0.80, 0.0),
    "MAT_HairBlack":       ("241E1E", 0.80, 0.0),
    "MAT_DarkNavy":        ("2E3944", 0.72, 0.0),
    "MAT_HeatOrange":      ("F08A3C", 0.68, 0.0),
    "MAT_BinGreen":        ("4C6B58", 0.75, 0.0),
    "MAT_Steel":           ("C9CDD2", 0.35, 1.0),
    # --- city kit (phase 2A) ---
    "MAT_Asphalt":         ("4E4E56", 0.85, 0.0),
    "MAT_RoadLine":        ("EFE3C8", 0.70, 0.0),
    "MAT_Sidewalk":        ("CBBDA8", 0.80, 0.0),
    "MAT_CurbStone":       ("A2937F", 0.80, 0.0),
    "MAT_BrickWarm":       ("C4785B", 0.78, 0.0),
    "MAT_BrickCool":       ("8E9AA6", 0.78, 0.0),
    "MAT_WindowGlass":     ("9FD3E0", 0.28, 0.0),
    "MAT_LampPost":        ("3A4148", 0.60, 0.0),
    "MAT_LampGlow":        ("FFE9A8", 0.40, 0.0),
    "MAT_CarGlass":        ("7FB6C9", 0.25, 0.0),
    "MAT_Tire":            ("26262A", 0.85, 0.0),
    "MAT_Awning":          ("D9564A", 0.72, 0.0),
    # --- phase 3 "soft cozy" set, matched to the assetlist references ---
    "MAT_SkinSoft":        ("F2CDA9", 0.72, 0.0),
    "MAT_HairSoft":        ("5B4232", 0.80, 0.0),
    "MAT_FaceInk":         ("3B2A21", 0.78, 0.0),
    "MAT_ShirtIvory":      ("F2EDDF", 0.76, 0.0),
    "MAT_ShirtSky":        ("AECDE2", 0.76, 0.0),
    "MAT_VestOlive":       ("4C6B3E", 0.80, 0.0),
    "MAT_VestNavy":        ("3F4B63", 0.80, 0.0),
    "MAT_TieGold":         ("E0A93B", 0.70, 0.0),
    "MAT_SweaterTeal":     ("5CAFA6", 0.82, 0.0),
    "MAT_TrouserCharcoal": ("5A554E", 0.80, 0.0),
    "MAT_TrouserKhaki":    ("C6B08D", 0.80, 0.0),
    "MAT_ShoeBrown":       ("6B4530", 0.72, 0.0),
    "MAT_ShoeCharcoal":    ("2E2B28", 0.72, 0.0),
    "MAT_UniformTeal":     ("4FA9A2", 0.78, 0.0),
    "MAT_UniformRed":      ("C4453E", 0.78, 0.0),
    "MAT_WoodPale":        ("E0B87E", 0.78, 0.0),
    "MAT_WoodPaleDark":    ("C79A63", 0.78, 0.0),
    "MAT_PanelCream":      ("F0E6D2", 0.78, 0.0),
    "MAT_ScreenGrey":      ("6E7686", 0.60, 0.0),
    "MAT_ScreenGlass":     ("D8DAE0", 0.35, 0.0),
    "MAT_ChairRed":        ("C9584A", 0.78, 0.0),
    "MAT_ChairBlue":       ("5B87C4", 0.78, 0.0),
    "MAT_BookMint":        ("9CCFC0", 0.78, 0.0),
    "MAT_Taupe":           ("A79C90", 0.78, 0.0),
    "MAT_TaupeDark":       ("8B8175", 0.78, 0.0),
    "MAT_BoardAmber":      ("D89A46", 0.74, 0.0),
    "MAT_BeltDark":        ("5C5A57", 0.82, 0.0),
    "MAT_CounterDark":     ("4E4A45", 0.80, 0.0),
    "MAT_LettuceGreen":    ("6FB04A", 0.78, 0.0),
    "MAT_TomatoRed":       ("D9453C", 0.74, 0.0),
    "MAT_SauceCream":      ("F2EDE0", 0.76, 0.0),
    "MAT_LavashPale":      ("E8D7A8", 0.80, 0.0),
    "MAT_KnifeSteel":      ("C8CCD2", 0.35, 1.0),
    # preview-only helpers (never inside an asset collection)
    "PRV_Ground":          ("E9DCC8", 0.90, 0.0),
    "PRV_Arrow":           ("E0217A", 0.60, 0.0),
    "PRV_Label":           ("3A3330", 0.80, 0.0),
}

EMISSIVE = {"MAT_HeatOrange": 0.55, "PRV_Arrow": 0.35, "MAT_LampGlow": 1.6}


def hex_rgb(h):
    h = h.lstrip("#")
    r, g, b = (int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    # sRGB -> linear
    def lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (lin(r), lin(g), lin(b), 1.0)


def get_mat(name):
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    hexcol, rough, metal = PALETTE[name]
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    if bsdf is None:
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
        nt.links.new(bsdf.outputs[0], nt.nodes["Material Output"].inputs[0])
    col = hex_rgb(hexcol)
    bsdf.inputs["Base Color"].default_value = col
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.35
    if name in EMISSIVE and "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = col
        bsdf.inputs["Emission Strength"].default_value = EMISSIVE[name]
    # viewport color so the Blender solid view already reads correctly
    mat.diffuse_color = col
    mat.roughness = rough
    mat.metallic = metal
    # keep the whole palette in the .blend even when a colour is unused yet
    mat.use_fake_user = name.startswith("MAT_")
    return mat


def build_all_materials():
    for n in PALETTE:
        get_mat(n)


# --------------------------------------------------------------------------
# low level mesh creation
# --------------------------------------------------------------------------

def _finish_mesh(name, verts, faces, mat_name, bone=None, is_face_detail=False):
    me = bpy.data.meshes.new(name + "_mesh")
    me.from_pydata(verts, [], faces)
    me.validate(verbose=False)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    # outward normals guaranteed
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    me.update()
    if mat_name:
        ob.data.materials.append(get_mat(mat_name))
        for p in ob.data.polygons:
            p.material_index = 0
    if bone:
        ob["ST_BONE"] = bone
    if is_face_detail:
        ob["ST_FACEDETAIL"] = 1
    return ob


def _xform(verts, mat):
    if mat is None:
        return verts
    return [tuple(mat @ Vector(v)) for v in verts]


def _basis_from_dir(direction):
    """Rotation matrix mapping local +Z onto `direction`."""
    d = Vector(direction).normalized()
    up = Vector((0.0, 0.0, 1.0))
    if abs(d.dot(up)) > 0.9999:
        return Matrix.Identity(3) if d.z > 0 else Matrix.Rotation(math.pi, 3, 'X')
    x = up.cross(d).normalized()
    y = d.cross(x).normalized()
    return Matrix((x, y, d)).transposed()


# ---- primitives -----------------------------------------------------------

def box(name, size, loc=(0, 0, 0), mat=None, taper_top=1.0, taper_bot=1.0,
        rot=None, bone=None, face_detail=False, taper_top_y=None, taper_bot_y=None):
    """Axis aligned box, optionally tapered along Z. `loc` is the CENTER."""
    sx, sy, sz = (s * 0.5 for s in size)
    tt = taper_top
    tb = taper_bot
    tty = taper_top_y if taper_top_y is not None else tt
    tby = taper_bot_y if taper_bot_y is not None else tb
    v = [
        (-sx * tb, -sy * tby, -sz), (sx * tb, -sy * tby, -sz),
        (sx * tb, sy * tby, -sz), (-sx * tb, sy * tby, -sz),
        (-sx * tt, -sy * tty, sz), (sx * tt, -sy * tty, sz),
        (sx * tt, sy * tty, sz), (-sx * tt, sy * tty, sz),
    ]
    f = [(0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4),
         (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    m = Matrix.Translation(Vector(loc))
    if rot:
        m = m @ Euler(rot, 'XYZ').to_matrix().to_4x4()
    return _finish_mesh(name, _xform(v, m), f, mat, bone, face_detail)


def lathe(name, profile, segments=24, loc=(0, 0, 0), mat=None, scale=(1, 1, 1),
          rot=None, bone=None, face_detail=False, closed_bottom=True, closed_top=True):
    """Revolve a (radius, z) profile around Z. profile ordered bottom -> top."""
    verts = []
    faces = []
    rings = []
    for (r, z) in profile:
        if abs(r) < 1e-6:
            rings.append(("pole", len(verts)))
            verts.append((0.0, 0.0, z))
        else:
            rings.append(("ring", len(verts)))
            for s in range(segments):
                a = TAU * s / segments
                verts.append((r * math.cos(a), r * math.sin(a), z))
    for i in range(len(profile) - 1):
        t0, i0 = rings[i]
        t1, i1 = rings[i + 1]
        if t0 == "pole" and t1 == "ring":
            for s in range(segments):
                faces.append((i0, i1 + s, i1 + (s + 1) % segments))
        elif t0 == "ring" and t1 == "pole":
            for s in range(segments):
                faces.append((i0 + s, i0 + (s + 1) % segments, i1))
        elif t0 == "ring" and t1 == "ring":
            for s in range(segments):
                s2 = (s + 1) % segments
                faces.append((i0 + s, i0 + s2, i1 + s2, i1 + s))
    # flat caps (fan around a center vertex)
    if closed_bottom and rings[0][0] == "ring":
        c = len(verts)
        verts.append((0.0, 0.0, profile[0][1]))
        b = rings[0][1]
        for s in range(segments):
            faces.append((c, b + (s + 1) % segments, b + s))
    if closed_top and rings[-1][0] == "ring":
        c = len(verts)
        verts.append((0.0, 0.0, profile[-1][1]))
        b = rings[-1][1]
        for s in range(segments):
            faces.append((c, b + s, b + (s + 1) % segments))
    m = Matrix.Translation(Vector(loc))
    if rot:
        m = m @ Euler(rot, 'XYZ').to_matrix().to_4x4()
    m = m @ Matrix.Diagonal(Vector(scale).to_4d())
    return _finish_mesh(name, _xform(verts, m), faces, mat, bone, face_detail)


def sphere(name, r, loc=(0, 0, 0), segments=24, rings=12, scale=(1, 1, 1),
           mat=None, rot=None, bone=None, face_detail=False):
    prof = []
    for j in range(rings + 1):
        a = -math.pi / 2 + math.pi * j / rings
        prof.append((r * math.cos(a), r * math.sin(a)))
    return lathe(name, prof, segments, loc, mat, scale, rot, bone, face_detail)


def capsule(name, p0, p1, r0, r1=None, segments=16, cap_rings=4, mat=None,
            bone=None, face_detail=False):
    """Capsule (round ended) between two world points."""
    r1 = r0 if r1 is None else r1
    p0 = Vector(p0)
    p1 = Vector(p1)
    d = p1 - p0
    L = d.length
    prof = []
    for j in range(cap_rings + 1):
        a = -math.pi / 2 + (math.pi / 2) * j / cap_rings
        prof.append((r0 * math.cos(a), r0 * math.sin(a)))
    for j in range(1, cap_rings + 1):
        a = (math.pi / 2) * j / cap_rings
        prof.append((r1 * math.cos(a), L + r1 * math.sin(a)))
    verts = []
    faces = []
    rings_idx = []
    for (r, z) in prof:
        if abs(r) < 1e-6:
            rings_idx.append(("pole", len(verts)))
            verts.append((0.0, 0.0, z))
        else:
            rings_idx.append(("ring", len(verts)))
            for s in range(segments):
                a = TAU * s / segments
                verts.append((r * math.cos(a), r * math.sin(a), z))
    for i in range(len(prof) - 1):
        t0, i0 = rings_idx[i]
        t1, i1 = rings_idx[i + 1]
        if t0 == "pole":
            for s in range(segments):
                faces.append((i0, i1 + s, i1 + (s + 1) % segments))
        elif t1 == "pole":
            for s in range(segments):
                faces.append((i0 + s, i0 + (s + 1) % segments, i1))
        else:
            for s in range(segments):
                s2 = (s + 1) % segments
                faces.append((i0 + s, i0 + s2, i1 + s2, i1 + s))
    m = Matrix.Translation(p0) @ _basis_from_dir(d).to_4x4()
    return _finish_mesh(name, _xform(verts, m), faces, mat, bone, face_detail)


def torus(name, major, minor, loc=(0, 0, 0), seg_major=24, seg_minor=10,
          mat=None, rot=None, scale=(1, 1, 1), bone=None, face_detail=False):
    verts = []
    faces = []
    for i in range(seg_major):
        a = TAU * i / seg_major
        ca, sa = math.cos(a), math.sin(a)
        for j in range(seg_minor):
            b = TAU * j / seg_minor
            rr = major + minor * math.cos(b)
            verts.append((rr * ca, rr * sa, minor * math.sin(b)))
    for i in range(seg_major):
        i2 = (i + 1) % seg_major
        for j in range(seg_minor):
            j2 = (j + 1) % seg_minor
            faces.append((i * seg_minor + j, i2 * seg_minor + j,
                          i2 * seg_minor + j2, i * seg_minor + j2))
    m = Matrix.Translation(Vector(loc))
    if rot:
        m = m @ Euler(rot, 'XYZ').to_matrix().to_4x4()
    m = m @ Matrix.Diagonal(Vector(scale).to_4d())
    return _finish_mesh(name, _xform(verts, m), faces, mat, bone, face_detail)


def tube_along(name, pts, radius, segments=10, mat=None, bone=None,
               face_detail=False, ref=(0, -1, 0), taper=None):
    """Round tube swept along a polyline. Used for mouths / eyebrows so the
    feature can be laid exactly onto a curved face surface."""
    pts = [Vector(p) for p in pts]
    n = len(pts)
    refv = Vector(ref).normalized()
    verts = []
    faces = []
    rings = []
    for i, p in enumerate(pts):
        if i == 0:
            t = (pts[1] - pts[0])
        elif i == n - 1:
            t = (pts[-1] - pts[-2])
        else:
            t = (pts[i + 1] - pts[i - 1])
        t.normalize()
        nrm = (refv - t * refv.dot(t))
        if nrm.length < 1e-5:
            nrm = Vector((0, 0, 1)) - t * t.z
        nrm.normalize()
        bnr = t.cross(nrm)
        rr = radius * (taper[i] if taper else 1.0)
        base = len(verts)
        rings.append(base)
        for s in range(segments):
            a = TAU * s / segments
            verts.append(tuple(p + nrm * (rr * math.cos(a)) + bnr * (rr * math.sin(a))))
    for i in range(n - 1):
        a0, b0 = rings[i], rings[i + 1]
        for s in range(segments):
            s2 = (s + 1) % segments
            faces.append((a0 + s, a0 + s2, b0 + s2, b0 + s))
    c0 = len(verts)
    verts.append(tuple(pts[0]))
    for s in range(segments):
        faces.append((c0, rings[0] + (s + 1) % segments, rings[0] + s))
    c1 = len(verts)
    verts.append(tuple(pts[-1]))
    for s in range(segments):
        faces.append((c1, rings[-1] + s, rings[-1] + (s + 1) % segments))
    return _finish_mesh(name, verts, faces, mat, bone, face_detail)


def ellipsoid_front(center, radii, x, z, inward=0.0):
    """Point on the -Y surface of an ellipsoid at (x, z), pushed `inward` (+Y)."""
    cx, cy, cz = center
    rx, ry, rz = radii
    dx = (x - cx) / rx
    dz = (z - cz) / rz
    s = max(0.0, 1.0 - dx * dx - dz * dz)
    return (x, cy - ry * math.sqrt(s) + inward, z)


def pie_prism(name, r_out, r_in, z0, z1, a0_deg, a1_deg, segments=16,
              loc=(0, 0, 0), mat=None, scale=(1, 1, 1), bone=None):
    """Solid annular sector (used for cap brims)."""
    a0, a1 = D2R(a0_deg), D2R(a1_deg)
    n = segments
    verts = []
    faces = []

    def ring(radius, z):
        base = len(verts)
        for i in range(n + 1):
            a = a0 + (a1 - a0) * i / n
            verts.append((radius * math.cos(a), radius * math.sin(a), z))
        return base
    bo = ring(r_out, z0)
    bi = ring(r_in, z0)
    to = ring(r_out, z1)
    ti = ring(r_in, z1)
    for i in range(n):
        faces.append((bo + i, bo + i + 1, bi + i + 1, bi + i))       # bottom
        faces.append((to + i, to + i + 1, ti + i + 1, ti + i))       # top
        faces.append((bo + i, bo + i + 1, to + i + 1, to + i))       # outer wall
        faces.append((bi + i, bi + i + 1, ti + i + 1, ti + i))       # inner wall
    faces.append((bo, bi, ti, to))                                    # start cap
    faces.append((bo + n, bi + n, ti + n, to + n))                    # end cap
    m = Matrix.Translation(Vector(loc)) @ Matrix.Diagonal(Vector(scale).to_4d())
    return _finish_mesh(name, _xform(verts, m), faces, mat, bone)


def dome_shell(name, center, r_out, r_in, rim_fn, segments=32, rings=8,
               scale=(1, 1, 1), mat=None, bone=None):
    """Hollow dome (cap crown / hair). rim_fn(azimuth_rad) -> polar angle rad."""
    verts = []
    faces = []

    def shell(radius):
        pole = len(verts)
        verts.append((0.0, 0.0, radius))
        idx = [[pole] * segments]
        for j in range(1, rings + 1):
            row = []
            for s in range(segments):
                a = TAU * s / segments
                pol = rim_fn(a) * j / rings
                row.append(len(verts))
                verts.append((radius * math.sin(pol) * math.cos(a),
                              radius * math.sin(pol) * math.sin(a),
                              radius * math.cos(pol)))
            idx.append(row)
        return idx

    out = shell(r_out)
    inn = shell(r_in)
    for j in range(rings):
        for s in range(segments):
            s2 = (s + 1) % segments
            if j == 0:
                faces.append((out[0][s], out[1][s], out[1][s2]))
                faces.append((inn[0][s], inn[1][s2], inn[1][s]))
            else:
                faces.append((out[j][s], out[j + 1][s], out[j + 1][s2], out[j][s2]))
                faces.append((inn[j][s], inn[j][s2], inn[j + 1][s2], inn[j + 1][s]))
    for s in range(segments):                       # rim band
        s2 = (s + 1) % segments
        faces.append((out[rings][s], out[rings][s2], inn[rings][s2], inn[rings][s]))
    m = Matrix.Translation(Vector(center)) @ Matrix.Diagonal(Vector(scale).to_4d())
    return _finish_mesh(name, _xform(verts, m), faces, mat, bone)


# --------------------------------------------------------------------------
# modifier / shading pipeline
# --------------------------------------------------------------------------

def apply_modifiers(ob):
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    new_me = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True,
                                             depsgraph=dg)
    old = ob.data
    ob.modifiers.clear()
    ob.data = new_me
    new_me.name = ob.name + "_mesh"
    if old.users == 0:
        bpy.data.meshes.remove(old)
    return ob


def bevel(ob, width=0.014, segments=2, angle=40.0, clamp=True):
    m = ob.modifiers.new("Bevel", 'BEVEL')
    m.width = width
    m.segments = segments
    m.limit_method = 'ANGLE'
    m.angle_limit = D2R(angle)
    m.use_clamp_overlap = clamp
    m.miter_outer = 'MITER_ARC'
    return apply_modifiers(ob)


def clean_mesh(ob, dist=2e-5, weld=True):
    """Dissolve degenerate (zero area) faces, optionally welding coincident
    vertices first.

    Welding must stay OFF for joined meshes: two parts that merely touch (a
    shopfront sitting exactly on a plinth, a kerb flush with a pavement) share
    corner positions, and welding those turns their shared edges into
    non-manifold ones.
    """
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    if weld:
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=dist)
    bmesh.ops.dissolve_degenerate(bm, dist=dist, edges=bm.edges)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    me.update()
    return ob


def shade(ob, sharp_angle=38.0, weighted=True, weld=True):
    clean_mesh(ob, weld=weld)
    me = ob.data
    for p in me.polygons:
        p.use_smooth = True
    # mark sharp edges by face angle (Blender 4.1+ replacement for auto smooth)
    bm = bmesh.new()
    bm.from_mesh(me)
    thr = D2R(sharp_angle)
    for e in bm.edges:
        if len(e.link_faces) == 2:
            e.smooth = e.calc_face_angle(0.0) < thr
        else:
            e.smooth = False
    bm.to_mesh(me)
    bm.free()
    me.update()
    if weighted:
        try:
            wn = ob.modifiers.new("WeightedNormal", 'WEIGHTED_NORMAL')
            wn.keep_sharp = True
            wn.mode = 'FACE_AREA_WITH_ANGLE'
            apply_modifiers(ob)
        except Exception:
            ob.modifiers.clear()
    return ob


def finalize(ob, bevel_w=0.014, bevel_seg=2, sharp=38.0, weighted=True):
    if bevel_w > 0:
        bevel(ob, bevel_w, bevel_seg)
    shade(ob, sharp, weighted)
    return ob


def decimate(ob, ratio):
    m = ob.modifiers.new("Decimate", 'DECIMATE')
    m.decimate_type = 'COLLAPSE'
    m.ratio = ratio
    m.use_collapse_triangulate = True
    apply_modifiers(ob)
    shade(ob, 38.0, weighted=False, weld=False)
    return ob


# --------------------------------------------------------------------------
# join / duplicate / counting
# --------------------------------------------------------------------------

def join_objects(objs, name):
    """Context-free join. Keeps material slots, rebuilds vertex groups from
    the ST_BONE custom property that each part carries."""
    verts, faces, midx = [], [], []
    mats = []
    groups = {}
    for o in objs:
        me = o.data
        base = len(verts)
        mw = o.matrix_world
        verts.extend(tuple(mw @ v.co) for v in me.vertices)
        remap = {}
        for i, m in enumerate(me.materials):
            if m is None:
                remap[i] = 0
                continue
            if m.name not in [x.name for x in mats]:
                mats.append(m)
            remap[i] = [x.name for x in mats].index(m.name)
        for p in me.polygons:
            faces.append(tuple(base + i for i in p.vertices))
            midx.append(remap.get(p.material_index, 0))
        bone = o.get("ST_BONE")
        if bone:
            for vi in range(len(me.vertices)):
                groups.setdefault(bone, {}).setdefault(1.0, []).append(base + vi)
        if o.vertex_groups:
            gnames = {vg.index: vg.name for vg in o.vertex_groups}
            for vi, v in enumerate(me.vertices):
                for g in v.groups:
                    gn = gnames.get(g.group)
                    if gn:
                        w = round(g.weight, 3)
                        groups.setdefault(gn, {}).setdefault(w, []).append(base + vi)
    me = bpy.data.meshes.new(name + "_mesh")
    me.from_pydata(verts, [], faces)
    me.validate(verbose=False)
    for i, m in enumerate(mats):
        me.materials.append(m)
    for i, p in enumerate(me.polygons):
        p.material_index = midx[i] if i < len(midx) else 0
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for gname, buckets in groups.items():
        vg = ob.vertex_groups.new(name=gname)
        for w, idxs in buckets.items():
            vg.add(idxs, w, 'REPLACE')
    for o in list(objs):
        d = o.data
        bpy.data.objects.remove(o, do_unlink=True)
        if d.users == 0:
            bpy.data.meshes.remove(d)
    return ob


def dup_object(ob, name):
    n = ob.copy()
    n.data = ob.data.copy()
    n.name = name
    n.data.name = name + "_mesh"
    n.modifiers.clear()
    bpy.context.scene.collection.objects.link(n)
    return n  # vertex groups travel with the object copy


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def bbox_of(objs):
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        if o.type not in ('MESH', 'CURVE', 'FONT', 'SURFACE'):
            continue
        # text/curve bounds are only meaningful on the evaluated object
        src = o.evaluated_get(dg) if o.type != 'MESH' else o
        for c in src.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector((min(lo[i], w[i]) for i in range(3)))
            hi = Vector((max(hi[i], w[i]) for i in range(3)))
    return lo, hi


# --------------------------------------------------------------------------
# collections / empties / asset scaffolding
# --------------------------------------------------------------------------

def new_collection(name, parent=None):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
    par = parent if parent else bpy.context.scene.collection
    if c.name not in [x.name for x in par.children]:
        par.children.link(c)
    return c


def move_to_collection(ob, coll):
    for c in list(ob.users_collection):
        c.objects.unlink(ob)
    coll.objects.link(ob)


def add_empty(name, loc, coll, parent=None, display='PLAIN_AXES', size=0.09):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = display
    e.empty_display_size = size
    e.location = loc
    e.show_in_front = True
    bpy.context.scene.collection.objects.link(e)
    move_to_collection(e, coll)
    if parent:
        e.parent = parent
        e.matrix_parent_inverse = Matrix.Identity(4)
        e.location = loc
    return e


def parent_keep(ob, parent):
    ob.parent = parent
    ob.matrix_parent_inverse = Matrix.Identity(4)


# --------------------------------------------------------------------------
# asset assembly
# --------------------------------------------------------------------------

class Asset:
    def __init__(self, name):
        self.name = name
        self.coll = new_collection(name)
        self.lod_colls = {i: new_collection("%s_LOD%d" % (name, i), self.coll)
                          for i in (0, 1, 2)}
        self.root = add_empty(name + "_ROOT", (0, 0, 0), self.coll,
                              display='ARROWS', size=0.35)
        self.front = add_empty("FRONT_DIRECTION", (0, -1, 0), self.coll,
                               parent=self.root, display='ARROWS', size=0.22)
        self.anchors = {"FRONT_DIRECTION": (0.0, -1.0, 0.0)}
        self.lods = {}
        self.rig = None

    def anchor(self, name, loc, display='PLAIN_AXES', size=0.10):
        add_empty(name, loc, self.coll, parent=self.root, display=display, size=size)
        self.anchors[name] = tuple(round(v, 4) for v in loc)

    def set_lod(self, level, ob):
        ob.name = "%s_LOD%d" % (self.name, level)
        ob.data.name = ob.name + "_mesh"
        move_to_collection(ob, self.lod_colls[level])
        parent_keep(ob, self.rig if self.rig else self.root)
        self.lods[level] = ob
        return ob

    def report(self):
        lo, hi = bbox_of([self.lods[0]])
        return {
            "name": self.name,
            "dims": [round(hi[i] - lo[i], 4) for i in range(3)],
            "bbox_min": [round(v, 4) for v in lo],
            "bbox_max": [round(v, 4) for v in hi],
            "tris": {("LOD%d" % k): tri_count(v) for k, v in sorted(self.lods.items())},
            "materials": [m.name for m in self.lods[0].data.materials],
            "anchors": self.anchors,
            "has_rig": self.rig is not None,
        }


def make_lods(asset, lod0_parts, face_parts=None, r1=0.5, r2=0.2):
    """lod0_parts / face_parts: lists of finalized part objects.
    Face detail geometry is kept intact in LOD1 so the face survives."""
    face_parts = face_parts or []
    body = join_objects(lod0_parts, asset.name + "_body")
    face = join_objects(face_parts, asset.name + "_face") if face_parts else None

    # ---- LOD0
    b0 = dup_object(body, "b0")
    parts0 = [b0] + ([dup_object(face, "f0")] if face else [])
    lod0 = join_objects(parts0, asset.name + "_LOD0")
    shade(lod0, 38.0, weighted=True, weld=False)

    # ---- LOD1 : decimate the body only, keep facial features
    b1 = dup_object(body, "b1")
    body_tris = tri_count(body)
    face_tris = tri_count(face) if face else 0
    total = body_tris + face_tris
    target = total * r1
    ratio = max(0.05, min(1.0, (target - face_tris) / max(1, body_tris)))
    decimate(b1, ratio)
    parts1 = [b1] + ([dup_object(face, "f1")] if face else [])
    lod1 = join_objects(parts1, asset.name + "_LOD1")
    shade(lod1, 38.0, weighted=True, weld=False)

    # ---- LOD2
    b2 = dup_object(lod0, "b2")
    decimate(b2, max(0.03, r2))
    lod2 = b2

    for o in ([body] + ([face] if face else [])):
        d = o.data
        bpy.data.objects.remove(o, do_unlink=True)
        if d.users == 0:
            bpy.data.meshes.remove(d)
    return lod0, lod1, lod2
