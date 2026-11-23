"""
Prepara a arte do primeiro nível para o Unity:

1. Corta a folha de criaturas (ArtSource/criaturas.png) em frames individuais:
   Wolf (lobo), Jiangshi (zombie saltitante) e Golem (golem de madeira),
   em Assets/_Project/Resources/Characters/<Nome>/<animação>_<nn>.png.
2. Divide o fundo (ArtSource/floresta_mistica_luar.png) em:
   - backdrop.png: cenário de fundo (céu, montanhas, floresta)
   - ground.png:   faixa de caminho + muro de pedra, sem emenda, para repetir no chão
   - ledge.png:    faixa fina do topo do muro, para as plataformas
   em Assets/_Project/Resources/Levels/Forest/.

Os frames são detetados como "manchas" opacas na folha (componentes ligados), por isso
os efeitos que saem do corpo continuam no frame certo. Os números em FRAMES referem-se à
lista que o script imprime com --list.

Uso:  python3 Tools/slice_level1.py           (exporta tudo)
      python3 Tools/slice_level1.py --list    (mostra as manchas numeradas)
Precisa de: pip install pillow numpy scipy
"""
import os
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CREATURES = os.path.join(ROOT, "ArtSource", "criaturas.png")
BACKGROUND = os.path.join(ROOT, "ArtSource", "floresta_mistica_luar.png")
OUT_CHARS = os.path.join(ROOT, "Assets", "_Project", "Resources", "Characters")
OUT_LEVEL = os.path.join(ROOT, "Assets", "_Project", "Resources", "Levels", "Forest")

ALPHA_CUT = 40
BASELINE = 8          # os pés ficam 8 px acima do fundo de cada frame (o importador usa isto)
MARGIN = 6

# Cada frame é uma lista de manchas (índices da lista ordenada, ver --list).
# Criaturas desenhadas viradas para a esquerda: são espelhadas para ficarem todas viradas
# para a direita, como o jogador (o jogo espelha o sprite quando o personagem vira).
FACES_LEFT = {"Golem"}

FRAMES = {
    "Wolf": {
        "idle":    [[0], [1], [9], [10]],
        "run":     [[11], [12], [13], [12]],
        "attack1": [[5], [6]],          # 0 = rugido (aviso), 1 = investida
        "hurt":    [[7]],
        "death":   [[8]],
    },
    "Jiangshi": {
        "idle":    [[19], [20]],
        "run":     [[20], [21]],
        "attack1": [[22], [23, 24, 25]],  # 0 = preparar, 1 = garra com energia verde
        "hurt":    [[26, 27]],
        "death":   [[28]],
    },
    "Golem": {
        "idle":    [[29], [30]],
        "run":     [[29], [30]],
        "attack1": [[30], [31]],        # 0 = levantar, 1 = esmagar o chão
        "hurt":    [[32]],
        "death":   [[33]],
    },
}


def blobs(img):
    alpha = img[:, :, 3]
    mask = alpha > 100
    lab, _ = ndimage.label(ndimage.binary_dilation(mask, iterations=3))
    out = []
    for i, sl in enumerate(ndimage.find_objects(lab), start=1):
        if sl is None:
            continue
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if h * w > 2000:
            out.append((sl, i))
    # ordenar por linha da folha e depois da esquerda para a direita
    out.sort(key=lambda b: (((b[0][0].start + b[0][0].stop) // 2) // 240, b[0][1].start))
    return out, lab


def export_creatures():
    img = np.array(Image.open(CREATURES).convert("RGBA"))
    found, lab = blobs(img)
    if "--list" in sys.argv:
        for n, (sl, _) in enumerate(found):
            print(n, "y", sl[0].start, sl[0].stop, "x", sl[1].start, sl[1].stop)
        return 0

    total = 0
    for name, anims in FRAMES.items():
        folder = os.path.join(OUT_CHARS, name)
        os.makedirs(folder, exist_ok=True)
        pieces = []
        for anim, frames in anims.items():
            for i, ids in enumerate(frames):
                labels = [found[k][1] for k in ids]
                keep = np.isin(lab, labels)
                cell = img.copy()
                cell[~keep] = 0
                cell[cell[:, :, 3] < ALPHA_CUT] = 0
                ys, xs = np.nonzero(cell[:, :, 3] > 100)
                y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
                # centro do corpo = maior mancha do frame (o corpo), metade de baixo
                body = max(labels, key=lambda l: int((lab == l).sum()))
                main = lab == body
                mys, mxs = np.nonzero(main & (cell[:, :, 3] > 100))
                legs = mys > mys.min() + (mys.max() - mys.min()) * 0.55
                cx = int(np.median(mxs[legs])) if legs.any() else int(np.median(mxs))
                if anim == "death":
                    cx = int((mxs.min() + mxs.max()) / 2)
                pieces.append((anim, i, cell[y0:y1, x0:x1], cx - x0, y1 - y0))

        # tela igual para todos os frames da criatura, pés no centro em baixo
        half_w = max(max(cx, p.shape[1] - cx) for _, _, p, cx, _ in pieces) + MARGIN
        height = max(h for *_, h in pieces) + BASELINE + MARGIN
        width = half_w * 2
        for anim, i, piece, cx, h in pieces:
            canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
            p = Image.fromarray(piece)
            canvas.paste(p, (half_w - cx, height - BASELINE - h), p)
            if name in FACES_LEFT:
                canvas = canvas.transpose(Image.FLIP_LEFT_RIGHT)
            canvas.save(os.path.join(folder, f"{anim}_{i:02d}.png"))
            total += 1
        print(f"{name}: {len(pieces)} frames ({width}x{height})")
    return total


def seamless(strip, blend=240):
    """Mistura as pontas para a faixa repetir sem emenda visível."""
    s = strip.astype(np.float32)
    w = s.shape[1]
    left = s[:, :blend].copy()
    right = s[:, w - blend:].copy()
    t = np.linspace(0, 1, blend)[None, :, None]
    # a ponta direita passa gradualmente a ser igual ao início
    s[:, w - blend:] = right * (1 - t) + left * t
    return s[:, : w - blend].clip(0, 255).astype(np.uint8)


def export_background():
    os.makedirs(OUT_LEVEL, exist_ok=True)
    bg = np.array(Image.open(BACKGROUND).convert("RGBA"))
    # Linha onde começa o caminho de pedra (topo do chão jogável) na imagem original.
    path_y = 776
    Image.fromarray(bg[:path_y + 14]).save(os.path.join(OUT_LEVEL, "backdrop.png"))
    Image.fromarray(seamless(bg[path_y:])).save(os.path.join(OUT_LEVEL, "ground.png"))
    Image.fromarray(seamless(bg[path_y:path_y + 60])).save(os.path.join(OUT_LEVEL, "ledge.png"))
    print("Fundo: backdrop, ground, ledge")


if __name__ == "__main__":
    n = export_creatures()
    if "--list" not in sys.argv:
        export_background()
