"""
Corta a folha de ambiente (ArtSource/ambiente_murim.png) em peças para o Unity.

A folha veio sem transparência: o "xadrez" cinzento e branco está desenhado na imagem.
O script apaga-o assim:
  1. píxeis claros e sem cor (cinzento/branco) ligados à borda da imagem = fundo;
  2. buracos claros fechados dentro das peças = fundo, se tiverem os dois tons do xadrez.
Peças com muito branco (a árvore de flores brancas e a cascata) ficam de fora,
porque o branco delas confunde-se com o xadrez.

Saída:
  Assets/_Project/Resources/Decor/<nome>.png        decoração (pivot em baixo, ao centro)
  Assets/_Project/Resources/Levels/Forest/platform.png     bloco de relva e pedra (plataformas e topo do chão),
                                                           com o meio refeito para se repetir sem emendas
  Assets/_Project/Resources/Levels/Forest/ground_body.png  pedras do corpo do chão, repetíveis nas duas direções

Uso:  python3 Tools/slice_decor.py
Precisa de: pip install pillow numpy scipy
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "ArtSource", "ambiente_murim.png")
OUT_DECOR = os.path.join(ROOT, "Assets", "_Project", "Resources", "Decor")
OUT_LEVEL = os.path.join(ROOT, "Assets", "_Project", "Resources", "Levels", "Forest")

# nome: (x0, y0, x1, y1) na folha original
PIECES = {
    # árvores (fundo)
    "tree_green": (0, 30, 304, 352),
    "tree_sakura": (299, 15, 616, 347),
    "tree_bamboo": (816, 19, 1033, 347),
    "tree_pine": (1029, 14, 1258, 359),
    "tree_pine_small": (1216, 81, 1436, 359),
    "tree_willow": (1432, 36, 1667, 359),
    # arbustos e ervas
    "bush_round": (10, 350, 145, 446),
    "bush_grass": (153, 366, 285, 446),
    "bush_flower": (294, 335, 446, 446),
    "bush_leafy": (455, 353, 580, 442),
    "fern_a": (727, 352, 858, 462),
    "fern_b": (866, 350, 1004, 453),
    # rochas
    "rock_big": (17, 458, 246, 604),
    "rock_flat": (230, 454, 363, 526),
    "rock_small": (276, 540, 374, 610),
    "rock_pebble": (378, 488, 464, 549),
    "rock_tall": (406, 450, 604, 624),
    "rock_round": (1213, 777, 1342, 872),
    "rock_mossy": (872, 833, 982, 913),
    # ruínas e objetos
    "ruin_arch": (619, 465, 882, 688),
    "ruin_pillar": (885, 509, 973, 692),
    "stone_lantern_tall": (976, 468, 1065, 696),
    "stone_lantern_short": (1069, 536, 1157, 708),
    "lamp_post": (1164, 476, 1317, 714),
    "fence": (1333, 576, 1543, 720),
    "banner": (1555, 458, 1669, 719),
}
PLATFORM = (1349, 734, 1672, 907)


def key_background(img):
    rgb = img.astype(int)
    mx, mn = rgb.max(2), rgb.min(2)
    light = (mn > 185) & ((mx - mn) < 28)
    lab, n = ndimage.label(light)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bright = rgb.mean(2)
    bg_ids = []
    for i, sl in enumerate(ndimage.find_objects(lab), start=1):
        if sl is None:
            continue
        if i in border:
            bg_ids.append(i)
            continue
        region = lab[sl] == i
        vals = bright[sl][region]
        if vals.size < 12:
            continue
        dark_band = ((vals > 205) & (vals < 235)).mean()
        light_band = (vals >= 243).mean()
        if dark_band > 0.15 and light_band > 0.15:  # tem os dois tons do xadrez
            bg_ids.append(i)
    bg = np.isin(lab, bg_ids)
    fg = ndimage.binary_opening(~bg, iterations=1)
    return np.dstack([img, (fg * 255).astype(np.uint8)])


def cut(rgba, box, keep_ratio=0.12):
    x0, y0, x1, y1 = box
    cell = rgba[y0:y1, x0:x1].copy()
    mask = cell[:, :, 3] > 0
    lab, n = ndimage.label(ndimage.binary_dilation(mask, iterations=2))
    if n > 1:
        areas = ndimage.sum(mask, lab, range(1, n + 1))
        biggest = areas.max()
        keep = [i + 1 for i, a in enumerate(areas) if a >= biggest * keep_ratio]
        cell[~np.isin(lab, keep)] = 0
    ys, xs = np.nonzero(cell[:, :, 3] > 0)
    return cell[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


PLATFORM_END = 30      # largura de cada ponta da plataforma (igual a PlatformEndPixels no jogo)
SEAM_BLEND = 12        # colunas de transição na emenda
BODY_ROWS = (90, 136)  # linhas só de pedras, de onde saem as cores do corpo do chão


def seamless_middle(plat):
    """
    Refaz o meio da plataforma para se repetir sem emenda: escolhe o sítio onde a coluna mais se
    parece com o início do meio e funde as últimas colunas com as que vêm antes do início.
    """
    h, w = plat.shape[:2]
    a = PLATFORM_END
    right = w - PLATFORM_END
    f = plat.astype(float)
    rows = slice(30, min(h, 138))
    best, best_b = None, right
    for b in range(a + 150, right + 1):
        d = np.abs(f[rows, b - 2:b + 2] - f[rows, a - 2:a + 2]).sum()
        if best is None or d < best:
            best, best_b = d, b
    tile = f[:, a:best_b].copy()
    n = tile.shape[1]
    k = min(SEAM_BLEND, a, n // 4)
    for i in range(k):
        t = (i + 1) / (k + 1)
        tile[:, n - k + i] = tile[:, n - k + i] * (1 - t) + f[:, a - k + i] * t
    out = np.concatenate([f[:, :a], tile, f[:, right:]], axis=1)
    return np.clip(out, 0, 255).astype(np.uint8), tile.astype(np.uint8)


def ground_body(tile, width=176, height=128, seed=7):
    """
    Pedras do corpo do chão, desenhadas por código com as cores das pedras da plataforma.
    São células de Voronoi num "toro" (o desenho dá a volta nas quatro margens), por isso a imagem
    repete-se na horizontal e na vertical sem emendas.
    """
    rng = np.random.default_rng(seed)
    band = tile[BODY_ROWS[0]:BODY_ROWS[1], :, :3].reshape(-1, 3).astype(float)
    lum = band.mean(axis=1)
    stone = band[lum > np.percentile(lum, 35)]
    light = np.percentile(stone, 85, axis=0)
    mid = np.percentile(stone, 50, axis=0)
    dark = np.percentile(stone, 15, axis=0)
    mortar = np.percentile(band, 3, axis=0) * 0.8

    # Pontos numa grelha com desvio aleatório (pedras de tamanho parecido, ~30 px)
    cols, rows = 4, 3
    pts = []
    for j in range(rows):
        for i in range(cols):
            x = (i + 0.5 + (j % 2) * 0.5 + rng.uniform(-0.25, 0.25)) * width / cols
            y = (j + 0.5 + rng.uniform(-0.2, 0.2)) * height / rows
            pts.append((x % width, y % height))
    pts = np.array(pts)
    tones = rng.uniform(-0.12, 0.12, len(pts))

    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    d = np.full((len(pts), height, width), 1e9)
    for n, (px, py) in enumerate(pts):
        dx = np.abs(xx - px); dx = np.minimum(dx, width - dx)
        dy = np.abs(yy - py); dy = np.minimum(dy, height - dy)
        d[n] = np.sqrt((dx * 1.0) ** 2 + (dy * 1.25) ** 2)
    order = np.argsort(d, axis=0)
    f1 = np.take_along_axis(d, order[:1], 0)[0]
    f2 = np.take_along_axis(d, order[1:2], 0)[0]
    owner = order[0]
    edge = f2 - f1  # distância à junta

    # Luz de cima à esquerda dentro de cada pedra
    cx, cy = pts[owner, 0], pts[owner, 1]
    rx = xx - cx; rx = (rx + width / 2) % width - width / 2
    ry = yy - cy; ry = (ry + height / 2) % height - height / 2
    # Pedras redondas: mais escuras perto da junta, mais claras em cima à esquerda
    rim = np.sqrt(np.clip(edge / 14.0, 0, 1))
    shade = 0.15 + 0.5 * rim - (rx * 0.5 + ry) / 70.0 + tones[owner]
    shade = np.round(np.clip(shade, 0, 1) * 4) / 4  # poucos tons, como pixel art
    col = np.where(shade[..., None] > 0.5,
                   mid + (light - mid) * ((shade[..., None] - 0.5) * 2),
                   dark + (mid - dark) * (shade[..., None] * 2))
    col = np.where((edge < 3.0)[..., None], mortar, col)
    col = np.where(((edge >= 3.0) & (edge < 4.5))[..., None], dark * 0.8, col)
    out = np.zeros((height, width, 4), np.uint8)
    out[..., :3] = np.clip(col, 0, 255)
    out[..., 3] = 255
    return out


def main():
    img = np.array(Image.open(SRC).convert("RGB"))
    rgba = key_background(img)
    os.makedirs(OUT_DECOR, exist_ok=True)
    os.makedirs(OUT_LEVEL, exist_ok=True)
    for name, box in PIECES.items():
        piece = cut(rgba, box)
        Image.fromarray(piece).save(os.path.join(OUT_DECOR, name + ".png"))
        print(f"{name}: {piece.shape[1]}x{piece.shape[0]}")
    plat, tile = seamless_middle(cut(rgba, PLATFORM))
    Image.fromarray(plat).save(os.path.join(OUT_LEVEL, "platform.png"))
    print(f"platform: {plat.shape[1]}x{plat.shape[0]} (meio de {tile.shape[1]} px)")
    body = ground_body(tile)
    Image.fromarray(body).save(os.path.join(OUT_LEVEL, "ground_body.png"))
    print(f"ground_body: {body.shape[1]}x{body.shape[0]}")


if __name__ == "__main__":
    main()
