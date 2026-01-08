import bpy
import bmesh
from mathutils import Vector
import random
from collections import deque


# ============================================================
# USER SETTINGS
# ============================================================
PNG_PATH = r"D:\Projects\DieWelle\ArtRaw\architectures\2_1.png"  # <-- 改成你的PNG（带alpha或黑白图）
USE_ALPHA = False                        # True: 用alpha通道; False: 用RGB亮度
INVERT = False                          # mask反了就开
THRESHOLD = 0.5                         # clip阈值

SUBDIV_CUTS = 2                         # 细分次数（1~4常用，越大越平滑越慢）
BILINEAR = True                         # 贴图采样方式
WRAP_U = True                           # 球体常用：U方向wrap
WRAP_V = False                          # 通常V不wrap
FLIP_V = True                           # ✅ 重要：图片坐标和UV坐标V方向常常相反

# 面保留判定
KEEP_WHITE = True                       # True: 保留白(>=threshold); False: 保留黑(<threshold)
KEEP_RATIO = 0.50                       # 80% 判定阈值
SAMPLES_PER_FACE = 7                    # 每个面采样点数（>=5 推荐）

# 清理
REMOVE_TINY_ISLANDS = True
MIN_ISLAND_FACES = 20

# 随机采样稳定
RANDOM_SEED = 12345


# ============================================================
# Image sampling
# ============================================================
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
    px = img.pixels[:]  # RGBA floats
    return img, w, h, px

def sample_mask(px, w, h, u, v, bilinear=True):
    # v-flip to match typical image-top vs UV-top mismatch
    if FLIP_V:
        v = 1.0 - v

    # wrap/clamp
    u = wrap01(u) if WRAP_U else clamp01(u)
    v = wrap01(v) if WRAP_V else clamp01(v)

    def fetch(ix, iy):
        idx = (iy*w + ix)*4
        r, g, b, a = px[idx], px[idx+1], px[idx+2], px[idx+3]
        val = a if USE_ALPHA else rgb_luma(r, g, b)
        if INVERT:
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


# ============================================================
# UV sampling on vertices/points
# ============================================================
def get_uv_layer(bm):
    uv_layer = bm.loops.layers.uv.active
    if not uv_layer:
        raise RuntimeError("Mesh has no active UV map.")
    return uv_layer

def vertex_uv_any(v, uv_layer):
    # 注意：同一个顶点在UV seam处可能有多个UV（不同loop）
    # 这里我们取第一个loop的uv作为该顶点的“代表”，对于 seam 边界，后面靠细分会降低误差。
    if not v.link_loops:
        return None
    return v.link_loops[0][uv_layer].uv.copy()

def edge_end_uvs(e, uv_layer):
    # 返回两个端点对应的UV（按端点各取一个loop uv）
    v0, v1 = e.verts
    uv0 = vertex_uv_any(v0, uv_layer)
    uv1 = vertex_uv_any(v1, uv_layer)
    return uv0, uv1


# ============================================================
# Subdivide
# ============================================================
def subdivide_mesh(bm, cuts):
    if cuts <= 0:
        return
    bmesh.ops.subdivide_edges(
        bm,
        edges=bm.edges[:],
        cuts=cuts,
        use_grid_fill=True
    )


# ============================================================
# Edge crossing -> insert intersection vertices
# ============================================================
def cut_edges_on_threshold(bm, uv_layer, px, w, h, threshold):
    """
    For each edge, if alpha crosses threshold, bisect that edge at interpolated t.
    Returns number of inserted points.
    """
    inserted = 0

    # Important: edges list changes while cutting; iterate over a snapshot
    edges_snapshot = list(bm.edges)

    edge_percents = {}  # for bisect_edges

    # Precompute per-edge t
    for e in edges_snapshot:
        if not e.is_valid:
            continue
        uv0, uv1 = edge_end_uvs(e, uv_layer)
        if uv0 is None or uv1 is None:
            continue

        a0 = sample_mask(px, w, h, uv0.x, uv0.y, bilinear=BILINEAR)
        a1 = sample_mask(px, w, h, uv1.x, uv1.y, bilinear=BILINEAR)

        # crossing?
        if (a0 < threshold <= a1) or (a1 < threshold <= a0):
            denom = (a1 - a0)
            if abs(denom) < 1e-12:
                continue
            t = (threshold - a0) / denom
            # clamp
            t = max(0.0, min(1.0, t))
            # avoid super close to endpoints
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

    # Count inserted verts
    for g in res.get("geom_split", []):
        if isinstance(g, bmesh.types.BMVert):
            inserted += 1

    return inserted


# ============================================================
# Face classification by sampling ratio
# ============================================================
def sample_face_uv_points(face, uv_layer, samples_n):
    """
    Return a list of UV positions inside the face.
    Works for tri/ngon by triangulating barycentric samples across a fan.
    """
    loops = face.loops
    if len(loops) < 3:
        return []

    # Collect loop UVs
    uvs = [loops[i][uv_layer].uv.copy() for i in range(len(loops))]

    # Use fan triangulation around loop 0
    uv0 = uvs[0]
    pts = []

    # Always include centroid of UV polygon (approx)
    cx = sum(u.x for u in uvs) / len(uvs)
    cy = sum(u.y for u in uvs) / len(uvs)
    pts.append(Vector((cx, cy)))

    # Add triangle centroids for each fan tri
    for i in range(1, len(uvs)-1):
        a, b, c = uv0, uvs[i], uvs[i+1]
        pts.append((a + b + c) / 3.0)

    # Add random barycentric points
    random_pts_needed = max(0, samples_n - len(pts))
    for _ in range(random_pts_needed):
        # pick a random fan triangle
        i = random.randint(1, len(uvs)-2)
        a, b, c = uv0, uvs[i], uvs[i+1]
        r1 = random.random()
        r2 = random.random()
        # uniform in triangle
        if r1 + r2 > 1.0:
            r1 = 1.0 - r1
            r2 = 1.0 - r2
        p = a + (b - a)*r1 + (c - a)*r2
        pts.append(p)

    return pts[:samples_n]

def delete_faces_by_ratio(bm, uv_layer, px, w, h, threshold, keep_white=True, keep_ratio=0.8, samples_n=7):
    bm.faces.ensure_lookup_table()
    to_delete = []

    for f in bm.faces:
        pts = sample_face_uv_points(f, uv_layer, samples_n)
        if not pts:
            continue

        white = 0
        for uv in pts:
            val = sample_mask(px, w, h, uv.x, uv.y, bilinear=BILINEAR)
            if val >= threshold:
                white += 1

        ratio_white = white / float(len(pts))
        keep = (ratio_white >= keep_ratio) if keep_white else (ratio_white <= (1.0 - keep_ratio))

        if not keep:
            to_delete.append(f)

    if to_delete:
        bmesh.ops.delete(bm, geom=to_delete, context="FACES")

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


# ============================================================
# MAIN
# ============================================================
def main():
    random.seed(RANDOM_SEED)

    obj = bpy.context.view_layer.objects.active
    if not obj or obj.type != "MESH":
        raise RuntimeError("请选中一个 Mesh 对象作为 Active。")

    img, w, h, px = load_image_pixels(PNG_PATH)

    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uv_layer = get_uv_layer(bm)

    # 1) Subdivide
    if SUBDIV_CUTS > 0:
        subdivide_mesh(bm, SUBDIV_CUTS)

    # 2) Cut edges at threshold crossing (一次可能不够：细分后会出现新边，也可能再次跨阈值)
    #    通常跑 1~2 轮就很够了
    total_inserted = 0
    for _ in range(2):
        ins = cut_edges_on_threshold(bm, uv_layer, px, w, h, THRESHOLD)
        total_inserted += ins
        if ins == 0:
            break

    print(f"[OK] Inserted threshold-crossing verts: {total_inserted}")

    # 3) Face classification by ratio and delete
    delete_faces_by_ratio(
        bm, uv_layer, px, w, h,
        threshold=THRESHOLD,
        keep_white=KEEP_WHITE,
        keep_ratio=KEEP_RATIO,
        samples_n=SAMPLES_PER_FACE
    )

    # 4) Cleanup tiny islands
    if REMOVE_TINY_ISLANDS:
        cleanup_small_islands(bm, MIN_ISLAND_FACES)

    bm.to_mesh(me)
    bm.free()
    me.update()

    print("[DONE] Cutout mesh generated.")

main()
