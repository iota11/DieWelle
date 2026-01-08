bl_info = {
    "name": "Opacity Cutout Mesh Tool (BW Mask)",
    "author": "ChatGPT",
    "version": (1, 0, 0),
    "blender": (4, 0, 0),
    "location": "View3D > Sidebar (N) > Opacity Cutout",
    "description": "Convert black/white mask texture into real cutout mesh by subdivide + edge crossing + face ratio delete.",
    "category": "Mesh",
}

import bpy
import bmesh
from mathutils import Vector
from collections import deque
import random

# ----------------------------
# Core helpers (mask sampling)
# ----------------------------

def rgb_luma(r, g, b):
    return 0.2126*r + 0.7152*g + 0.0722*b

def clamp01(x):
    return 0.0 if x < 0.0 else (1.0 if x > 1.0 else x)

def wrap01(x):
    x = x % 1.0
    if x < 0.0:
        x += 1.0
    return x

def load_image_pixels(path: str):
    img = bpy.data.images.load(path, check_existing=True)
    w, h = img.size
    if w <= 0 or h <= 0:
        raise RuntimeError("Invalid image size.")
    px = img.pixels[:]  # RGBA float list
    return img, w, h, px

def sample_mask_bw(px, w, h, u, v, *, bilinear=True, invert=False, wrap_u=True, wrap_v=False, flip_v=True):
    # Read BLACK/WHITE from RGB only (ignore alpha)
    if flip_v:
        v = 1.0 - v

    u = wrap01(u) if wrap_u else clamp01(u)
    v = wrap01(v) if wrap_v else clamp01(v)

    def fetch(ix, iy):
        idx = (iy*w + ix)*4
        r, g, b = px[idx], px[idx+1], px[idx+2]
        val = rgb_luma(r, g, b)
        if invert:
            val = 1.0 - val
        return val

    if not bilinear:
        x = int(round(u*(w-1)))
        y = int(round(v*(h-1)))
        return fetch(x, y)

    fx = u*(w-1)
    fy = v*(h-1)
    x0 = int(fx); y0 = int(fy)
    x1 = min(x0+1, w-1)
    y1 = min(y0+1, h-1)
    tx = fx - x0
    ty = fy - y0

    v00 = fetch(x0, y0)
    v10 = fetch(x1, y0)
    v01 = fetch(x0, y1)
    v11 = fetch(x1, y1)

    v0 = v00*(1-tx) + v10*tx
    v1 = v01*(1-tx) + v11*tx
    return v0*(1-ty) + v1*ty


# ----------------------------
# BMesh helpers
# ----------------------------

def ensure_active_mesh_and_uv(context):
    obj = context.view_layer.objects.active
    if not obj or obj.type != "MESH":
        raise RuntimeError("Please select a Mesh object as Active.")
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    uv_layer = bm.loops.layers.uv.active
    if not uv_layer:
        bm.free()
        raise RuntimeError("Active mesh has no UV map (no active UV layer).")
    return obj, bm, uv_layer

def bm_commit(obj, bm):
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()

def subdivide_mesh(bm, cuts: int):
    if cuts <= 0:
        return
    bmesh.ops.subdivide_edges(
        bm,
        edges=bm.edges[:],
        cuts=cuts,
        use_grid_fill=True
    )

def vertex_uv_any(v, uv_layer):
    # seam vertices can have multiple uv values; we use the first loop as representative
    if not v.link_loops:
        return None
    return v.link_loops[0][uv_layer].uv.copy()

def edge_end_uvs(e, uv_layer):
    v0, v1 = e.verts
    return vertex_uv_any(v0, uv_layer), vertex_uv_any(v1, uv_layer)

def cut_edges_on_threshold(bm, uv_layer, px, w, h, threshold, *,
                           bilinear=True, invert=False, wrap_u=True, wrap_v=False, flip_v=True):
    """Insert verts on edges where mask crosses threshold. Returns number inserted verts."""
    edges_snapshot = list(bm.edges)
    edge_percents = {}

    for e in edges_snapshot:
        if not e.is_valid:
            continue
        uv0, uv1 = edge_end_uvs(e, uv_layer)
        if uv0 is None or uv1 is None:
            continue

        a0 = sample_mask_bw(px, w, h, uv0.x, uv0.y, bilinear=bilinear, invert=invert, wrap_u=wrap_u, wrap_v=wrap_v, flip_v=flip_v)
        a1 = sample_mask_bw(px, w, h, uv1.x, uv1.y, bilinear=bilinear, invert=invert, wrap_u=wrap_u, wrap_v=wrap_v, flip_v=flip_v)

        crosses = (a0 < threshold <= a1) or (a1 < threshold <= a0)
        if not crosses:
            continue

        denom = (a1 - a0)
        if abs(denom) < 1e-12:
            continue

        t = (threshold - a0) / denom
        t = max(0.0, min(1.0, t))
        if t < 1e-6 or t > 1.0 - 1e-6:
            continue

        edge_percents[e] = t

    if not edge_percents:
        return 0

    res = bmesh.ops.bisect_edges(
        bm,
        edges=list(edge_percents.keys()),
        cuts=1,
        edge_percents=edge_percents
    )

    inserted = 0
    for g in res.get("geom_split", []):
        if isinstance(g, bmesh.types.BMVert):
            inserted += 1
    return inserted

def sample_face_uv_points(face, uv_layer, samples_n, rng: random.Random):
    loops = face.loops
    if len(loops) < 3:
        return []

    uvs = [loops[i][uv_layer].uv.copy() for i in range(len(loops))]
    uv0 = uvs[0]

    pts = []
    # centroid
    cx = sum(u.x for u in uvs) / len(uvs)
    cy = sum(u.y for u in uvs) / len(uvs)
    pts.append(Vector((cx, cy)))

    # fan tri centroids
    for i in range(1, len(uvs)-1):
        a, b, c = uv0, uvs[i], uvs[i+1]
        pts.append((a + b + c) / 3.0)

    # random barycentric points
    need = max(0, samples_n - len(pts))
    for _ in range(need):
        i = rng.randint(1, len(uvs)-2)
        a, b, c = uv0, uvs[i], uvs[i+1]
        r1 = rng.random()
        r2 = rng.random()
        if r1 + r2 > 1.0:
            r1 = 1.0 - r1
            r2 = 1.0 - r2
        p = a + (b - a)*r1 + (c - a)*r2
        pts.append(p)

    return pts[:samples_n]

def delete_faces_by_ratio(bm, uv_layer, px, w, h, threshold, *,
                          keep_white=True, keep_ratio=0.8, samples_n=7,
                          bilinear=True, invert=False, wrap_u=True, wrap_v=False, flip_v=True,
                          random_seed=12345, debug=False):
    bm.faces.ensure_lookup_table()
    before = len(bm.faces)

    rng = random.Random(random_seed)
    to_delete = []

    for f in bm.faces:
        pts = sample_face_uv_points(f, uv_layer, samples_n, rng)
        if not pts:
            continue

        white = 0
        for uv in pts:
            val = sample_mask_bw(px, w, h, uv.x, uv.y, bilinear=bilinear, invert=invert, wrap_u=wrap_u, wrap_v=wrap_v, flip_v=flip_v)
            if val >= threshold:
                white += 1

        ratio_white = white / float(len(pts))
        keep = (ratio_white >= keep_ratio) if keep_white else (ratio_white <= (1.0 - keep_ratio))
        if not keep:
            to_delete.append(f)

    if to_delete:
        bmesh.ops.delete(bm, geom=to_delete, context="FACES")

    bm.faces.ensure_lookup_table()
    after = len(bm.faces)
    if debug:
        print(f"[OpacityCutout] faces before={before}, delete={len(to_delete)}, after={after}")

    return len(to_delete)

def cleanup_small_islands(bm, min_faces=20):
    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        f.tag = False

    for f in list(bm.faces):
        if not f.is_valid or f.tag:
            continue

        q = deque([f])
        comp = []
        f.tag = True

        while q:
            cur = q.popleft()
            if not cur.is_valid:
                continue
            comp.append(cur)
            for e in cur.edges:
                for nf in e.link_faces:
                    if nf.is_valid and not nf.tag:
                        nf.tag = True
                        q.append(nf)

        if len(comp) < min_faces:
            bmesh.ops.delete(bm, geom=comp, context="FACES")


# ----------------------------
# Properties (UI exposed)
# ----------------------------

class OC_Props(bpy.types.PropertyGroup):
    image_path: bpy.props.StringProperty(
        name="Mask PNG Path",
        description="Path to black/white PNG mask (RGB). Alpha is ignored.",
        subtype='FILE_PATH',
        default="",
    )

    threshold: bpy.props.FloatProperty(
        name="Threshold",
        default=0.5,
        min=0.0, max=1.0,
    )

    invert: bpy.props.BoolProperty(
        name="Invert (Black/White)",
        default=False,
    )

    flip_v: bpy.props.BoolProperty(
        name="Flip V",
        description="Flip V when sampling (common when image origin differs from UV)",
        default=True,
    )

    wrap_u: bpy.props.BoolProperty(
        name="Wrap U",
        default=True,
    )

    wrap_v: bpy.props.BoolProperty(
        name="Wrap V",
        default=False,
    )

    bilinear: bpy.props.BoolProperty(
        name="Bilinear",
        default=True,
    )

    subdiv_cuts: bpy.props.IntProperty(
        name="Subdivide Cuts",
        default=2,
        min=0, max=6,
    )

    cut_rounds: bpy.props.IntProperty(
        name="Edge Cut Rounds",
        description="Run edge-threshold cutting multiple times to catch new crossings after splits",
        default=2,
        min=1, max=6,
    )

    keep_white: bpy.props.BoolProperty(
        name="Keep White Side",
        description="Keep faces that are mostly white (>= threshold). If off, keep mostly black side.",
        default=True,
    )

    keep_ratio: bpy.props.FloatProperty(
        name="Keep Ratio",
        description="Keep a face if white ratio >= this (or black ratio if Keep White is off)",
        default=0.80,
        min=0.5, max=1.0,
    )

    samples_per_face: bpy.props.IntProperty(
        name="Samples/Face",
        default=9,
        min=3, max=25,
    )

    remove_tiny_islands: bpy.props.BoolProperty(
        name="Cleanup Tiny Islands",
        default=True,
    )

    min_island_faces: bpy.props.IntProperty(
        name="Min Island Faces",
        default=20,
        min=1, max=99999,
    )

    random_seed: bpy.props.IntProperty(
        name="Random Seed",
        default=12345,
        min=0, max=2**31-1,
    )

    debug_print: bpy.props.BoolProperty(
        name="Debug Print",
        default=False,
    )

    # cache (internal)
    _img_name: bpy.props.StringProperty
