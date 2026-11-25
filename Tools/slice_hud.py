#!/usr/bin/env python3
"""
Recorta a folha de interface (ArtSource/interface_hud.png) para Assets/_Project/Resources/UI.

A folha já tem transparência. Para as barras poderem encher e esvaziar no jogo, cada peça sai em duas
versões do mesmo tamanho:
  hp_frame.png  moldura com retrato, barra vermelha (vida) e azul (Qi) vazias
  hp_fill.png   só o vermelho da barra de vida
  qi_fill.png   só o azul da barra de Qi
  qi_panel.png  painel de orbes com as 7 casas apagadas
  qi_lit.png    só as 7 orbes acesas
  panel.png     moldura dourada para os ecrãs de vitória e derrota
  orb_icon.png  orbe pequena
Da segunda folha (ArtSource/interface_topo.png) sai a barra do mapa no topo do ecrã:
  map_frame.png   moldura com a paisagem
  map_start.png   portão do início       map_goal.png   portão da meta
  map_lantern_off.png / map_lantern_on.png  checkpoints por acender e acesos
  map_player.png  marcador do jogador
As posições das barras e das casas (em píxeis destas imagens) estão no topo de Prototype/HudArt.cs:
se mudares as caixas aqui, atualiza-as lá.

Uso: python3 Tools/slice_hud.py
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "ArtSource", "interface_hud.png")
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "UI")

HP_BOX = (40, 10, 775, 240)          # retrato + barras
RED_BAR = (243, 106, 740, 134)       # interior da barra de vida
BLUE_BAR = (258, 145, 644, 174)      # interior da barra de Qi
QI_BOX = (825, 80, 1440, 225)        # painel das orbes
QI_SLOTS = [990, 1042, 1095, 1145, 1197, 1250, 1300]
QI_SLOT_Y, QI_SLOT_R = 159, 22
PANEL_BOX = (405, 845, 1035, 1000)
ORB_BOX = (818, 316, 903, 391)

EMPTY = np.array([0.16, 0.13, 0.2])      # cor da barra vazia (multiplica o brilho)
SLOT_OFF = np.array([0.16, 0.2, 0.32])   # casa apagada


def crop(a, box):
    x0, y0, x1, y1 = box
    return a[y0:y1, x0:x1].copy()


def bar_masks(a):
    r, g, b, al = [a[..., i].astype(float) for i in range(4)]
    red = (r > 90) & (r > g * 1.4) & (r > b * 1.4) & (al > 0)
    blue = (b > 90) & (b > r * 1.3) & (al > 0)
    return red, blue


def in_box(shape, box, origin):
    m = np.zeros(shape[:2], bool)
    x0, y0, x1, y1 = box
    ox, oy = origin
    m[y0 - oy:y1 - oy, x0 - ox:x1 - ox] = True
    return m


def save(arr, name):
    Image.fromarray(arr.astype(np.uint8), "RGBA").save(os.path.join(OUT, name))
    print(f"  {name} {arr.shape[1]}x{arr.shape[0]}")


def split(part, mask, dark):
    """Devolve (vazio, só-a-parte) para uma máscara."""
    empty = part.copy()
    lum = part[..., :3].astype(float).mean(axis=2, keepdims=True)
    shade = np.clip(lum * 1.4, 0, 255) * dark
    empty[..., :3] = np.where(mask[..., None], shade, part[..., :3])
    only = np.zeros_like(part)
    only[mask] = part[mask]
    return empty, only


TOP_SRC = os.path.join(ROOT, "ArtSource", "interface_topo.png")
TOP_PIECES = {
    "map_frame": (18, 38, 1519, 258),
    "map_lantern_off": (50, 285, 174, 481),
    "map_lantern_on": (420, 287, 559, 481),
    "map_player": (1023, 329, 1124, 474),
    "map_start": (35, 525, 376, 781),
    "map_goal": (401, 533, 652, 781),
}


def main():
    os.makedirs(OUT, exist_ok=True)
    a = np.array(Image.open(SRC).convert("RGBA"))
    print("A recortar", SRC)

    hp = crop(a, HP_BOX)
    red, blue = bar_masks(hp)
    origin = HP_BOX[:2]
    # o brilho branco na ponta de cada barra também faz parte do enchimento
    white = (hp[..., :3].min(axis=2) > 190) & (hp[..., 3] > 0)
    red = (red | white) & in_box(hp.shape, RED_BAR, origin)
    blue = (blue | white) & in_box(hp.shape, BLUE_BAR, origin)
    frame, hp_fill = split(hp, red, EMPTY)
    frame, qi_fill = split(frame, blue, EMPTY)
    # as barras cheias usam as cores originais (o "frame" já tem as duas vazias)
    qi_fill = np.zeros_like(hp)
    qi_fill[blue] = hp[blue]
    save(frame, "hp_frame.png")
    save(hp_fill, "hp_fill.png")
    save(qi_fill, "qi_fill.png")

    qi = crop(a, QI_BOX)
    yy, xx = np.mgrid[0:qi.shape[0], 0:qi.shape[1]]
    slots = np.zeros(qi.shape[:2], bool)
    for cx in QI_SLOTS:
        slots |= (xx - (cx - QI_BOX[0])) ** 2 + (yy - (QI_SLOT_Y - QI_BOX[1])) ** 2 <= QI_SLOT_R ** 2
    slots &= qi[..., 3] > 0
    panel, lit = split(qi, slots, SLOT_OFF)
    save(panel, "qi_panel.png")
    save(lit, "qi_lit.png")

    save(crop(a, PANEL_BOX), "panel.png")
    save(crop(a, ORB_BOX), "orb_icon.png")

    if os.path.exists(TOP_SRC):
        top = np.array(Image.open(TOP_SRC).convert("RGBA"))
        for name, box in TOP_PIECES.items():
            save(crop(top, box), name + ".png")
    else:
        print("  (sem", TOP_SRC, "- não fiz a barra do mapa)")

    # Medidas para o HudArt.cs (relativas a cada imagem)
    def rel(box, o):
        return (box[0] - o[0], box[1] - o[1], box[2] - o[0], box[3] - o[1])
    print("RED_BAR", rel(RED_BAR, origin), "BLUE_BAR", rel(BLUE_BAR, origin))
    print("QI slots x", [c - QI_BOX[0] for c in QI_SLOTS], "y", QI_SLOT_Y - QI_BOX[1], "r", QI_SLOT_R)


if __name__ == "__main__":
    main()
