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
  Assets/_Project/Resources/Levels/Forest/platform.png  bloco de relva e pedra das plataformas

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


def main():
    img = np.array(Image.open(SRC).convert("RGB"))
    rgba = key_background(img)
    os.makedirs(OUT_DECOR, exist_ok=True)
    os.makedirs(OUT_LEVEL, exist_ok=True)
    for name, box in PIECES.items():
        piece = cut(rgba, box)
        Image.fromarray(piece).save(os.path.join(OUT_DECOR, name + ".png"))
        print(f"{name}: {piece.shape[1]}x{piece.shape[0]}")
    plat = cut(rgba, PLATFORM)
    Image.fromarray(plat).save(os.path.join(OUT_LEVEL, "platform.png"))
    print(f"platform: {plat.shape[1]}x{plat.shape[0]}")


if __name__ == "__main__":
    main()
