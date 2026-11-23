"""
Corta a folha "Portão Oriental e Orbes Mágicos" (ArtSource/portao_lanternas_orbes.png)
em sprites individuais em Assets/_Project/Resources/Props/:

  Gate/gate.png             portão (início e fim do nível)
  Lantern/off_00..01.png    lanterna de checkpoint por acender (chama a tremer)
  Lantern/on_00..01.png     lanterna acesa com energia de Qi
  Orb/orb_00..07.png        orbe de Qi a rodar
  Sparkle/sparkle_00..03    brilho quando se apanha uma orbe

Cada parte é recortada por uma caixa (x0, y0, x1, y1) em píxeis da folha original.
As caixas de cada grupo ficam todas com o mesmo tamanho para os frames não tremerem.

Uso:  python3 Tools/slice_props.py
Precisa de: pip install pillow numpy scipy
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "ArtSource", "portao_lanternas_orbes.png")
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Props")
ALPHA_CUT = 40

ORB_COLS = [(858, 1018), (1022, 1188), (1190, 1358), (1362, 1505)]

GROUPS = {
    "Gate": [("gate", (20, 95, 845, 940))],
    "Lantern": [
        ("off_00", (851, 592, 1015, 940)),
        ("off_01", (1015, 592, 1179, 940)),
        ("on_00", (1179, 592, 1343, 940)),
        ("on_01", (1336, 592, 1500, 940)),
    ],
    "Orb": [(f"orb_{i:02d}", (x0, 118, x1, 318)) for i, (x0, x1) in enumerate(ORB_COLS)]
         + [(f"orb_{i + 4:02d}", (x0, 318, x1, 472)) for i, (x0, x1) in enumerate(ORB_COLS)],
    "Sparkle": [(f"sparkle_{i:02d}", (x0, 476, x1, 552)) for i, (x0, x1) in enumerate(ORB_COLS)],
}

# Os grupos que ficam centrados (orbes e brilhos) ou assentes no chão (portão, lanternas).
CENTERED = {"Orb", "Sparkle"}


def remove_specks(cell, min_area):
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


def main():
    img = np.array(Image.open(SRC).convert("RGBA"))
    total = 0
    for group, parts in GROUPS.items():
        folder = os.path.join(OUT, group)
        os.makedirs(folder, exist_ok=True)
        cells = []
        for name, (x0, y0, x1, y1) in parts:
            cell = img[y0:y1, x0:x1].copy()
            cell[cell[:, :, 3] < ALPHA_CUT] = 0
            # nas lanternas, apaga pedaços das vizinhas que entram na caixa
            cell = remove_specks(cell, 4 if group in CENTERED else 300)
            cells.append((name, cell))

        # mesmo tamanho para todos os frames do grupo
        w = max(c.shape[1] for _, c in cells)
        h = max(c.shape[0] for _, c in cells)
        for name, cell in cells:
            canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
            piece = Image.fromarray(cell)
            ox = (w - cell.shape[1]) // 2
            oy = (h - cell.shape[0]) // 2 if group in CENTERED else h - cell.shape[0]
            canvas.paste(piece, (ox, oy), piece)
            canvas.save(os.path.join(folder, name + ".png"))
            total += 1
        print(f"{group}: {len(cells)} ({w}x{h})")
    print(f"{total} sprites exportados para {OUT}")


if __name__ == "__main__":
    main()
