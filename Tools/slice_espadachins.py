"""
Corta a folha "Espadachins Elementais" em frames individuais para o Unity.

A folha tem 2 personagens (azul em cima, vermelho em baixo), cada um com 3 linhas
de 10 colunas. Em cada linha, as colunas são:
  0-1 parado | 2 guarda | 3 arranque/dash | 4-5 corrida/salto | 6 preparar golpe
  7 corte com efeito | 8 recuo/dano (linha 3: efeito especial) | 9 morto

Cada frame é colado numa tela de tamanho fixo com os pés do personagem no mesmo
ponto (centro em baixo), para o pivot ser igual em todos os sprites.

Uso:  python3 Tools/slice_espadachins.py
Precisa de: pip install pillow numpy scipy
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "ArtSource", "espadachins_elementais.png")
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Characters")

CANVAS_W, CANVAS_H = 400, 200     # tamanho de cada frame exportado (px)
ANCHOR_X, BASELINE = 200, 8       # pés ficam em (200, 8 px acima do fundo)
ALPHA_CUT = 40                    # remove o halo semitransparente da folha

# Faixas verticais das 6 linhas (y0, y1) e limites aproximados das colunas.
ROWS = [(14, 196), (190, 362), (352, 512), (514, 684), (676, 838), (826, 1004)]
COLS = [0, 138, 272, 400, 548, 676, 828, 1015, 1245, 1368, 1535]

# Que frames usar em cada animação: (linha dentro do personagem, coluna)
ANIMS = {
    "idle":    [(0, 0), (1, 0), (2, 0), (1, 0)],
    "run":     [(0, 4), (0, 5), (1, 4), (1, 5)],
    "jump":    [(1, 5)],
    "fall":    [(2, 5)],
    "dash":    [(0, 3)],
    "attack1": [(0, 6), (0, 7)],
    "attack2": [(1, 6), (1, 7)],
    "attack3": [(2, 6), (2, 7)],
    "hurt":    [(0, 8)],
    "death":   [(0, 9)],
    "special": [(2, 8)],
}
CHARACTERS = {"Blue": 0, "Red": 3}   # primeira linha de cada personagem

# A personagem vermelha tem a coluna 8 com um efeito grande em vez do recuo.
OVERRIDES = {
    "Red": {"hurt": [(1, 2)], "special": [(0, 8)]},
}


def remove_specks(cell, min_area=80):
    """Apaga pedacinhos soltos que vêm das linhas vizinhas (precisa de scipy; sem ele, salta)."""
    try:
        from scipy import ndimage
    except ImportError:
        return cell
    mask = cell[:, :, 3] > 0
    lab, n = ndimage.label(mask)
    if n == 0:
        return cell
    areas = ndimage.sum(mask, lab, range(1, n + 1))
    for i, area in enumerate(areas, start=1):
        if area < min_area:
            cell[lab == i] = 0
    return cell


def best_cut(occ, x, radius=22):
    lo, hi = max(1, x - radius), min(len(occ) - 1, x + radius)
    if lo >= hi:
        return x
    return lo + int(np.argmin(occ[lo:hi]))


def main():
    img = np.array(Image.open(SRC).convert("RGBA"))
    alpha = img[:, :, 3].astype(int)
    total = 0
    for name, first_row in CHARACTERS.items():
        anims = dict(ANIMS)
        anims.update(OVERRIDES.get(name, {}))
        for anim, frames in anims.items():
            folder = os.path.join(OUT, name)
            os.makedirs(folder, exist_ok=True)
            for i, (r, c) in enumerate(frames):
                y0, y1 = ROWS[first_row + r]
                band = alpha[y0:y1]
                occ = (band > 100).sum(0)
                cuts = [0] + [best_cut(occ, x) for x in COLS[1:-1]] + [img.shape[1]]
                x0, x1 = cuts[c], cuts[c + 1]

                cell = img[y0:y1, x0:x1].copy()
                cell[cell[:, :, 3] < ALPHA_CUT] = 0
                cell = remove_specks(cell)
                mask = cell[:, :, 3] > 100
                ys, xs = np.nonzero(mask)
                if len(xs) == 0:
                    raise RuntimeError(f"frame vazio: {name} {anim} {r},{c}")
                bottom = ys.max()
                # Centro do corpo: média x da metade de baixo (pernas), ignora efeitos/espada.
                top = ys.min()
                legs = ys > top + (bottom - top) * 0.55
                cx = int(np.median(xs[legs])) if legs.any() else int(np.median(xs))
                if anim in ("death",):
                    cx = int((xs.min() + xs.max()) / 2)

                piece = Image.fromarray(cell)
                px = ANCHOR_X - cx
                py = CANVAS_H - BASELINE - 1 - bottom
                canvas = Image.new("RGBA", (CANVAS_W, CANVAS_H), (0, 0, 0, 0))
                canvas.paste(piece, (px, py), piece)
                canvas.save(os.path.join(folder, f"{anim}_{i:02d}.png"))
                total += 1
    print(f"{total} frames exportados para {OUT}")


if __name__ == "__main__":
    main()
