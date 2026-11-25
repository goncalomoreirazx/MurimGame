using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>
    /// Imagens do HUD (Resources/UI, recortadas por Tools/slice_hud.py) e as medidas de cada uma.
    /// As barras e as casas das orbes estão em pares do mesmo tamanho: a imagem "vazia" e só a parte cheia,
    /// que se desenha por cima até onde for preciso.
    /// Se faltar alguma imagem, Ready fica false e o HUD usa os retângulos simples de antes.
    /// </summary>
    public class HudArt
    {
        public const string Folder = "UI/";

        // Medidas em píxeis das imagens (x a partir da esquerda, y a partir de cima). Ver Tools/slice_hud.py.
        public const float HpBarLeft = 203f, HpBarRight = 700f, HpBarTop = 96f, HpBarBottom = 124f;
        public const float QiBarLeft = 218f, QiBarRight = 604f;
        public static readonly float[] QiSlotX = { 165f, 217f, 270f, 320f, 372f, 425f, 475f };
        public const float QiSlotY = 79f, QiSlotRadius = 22f;
        /// <summary>Bordas da moldura dourada que não esticam (esquerda, cima, direita, baixo).</summary>
        public static readonly Vector4 PanelBorder = new Vector4(75f, 70f, 75f, 50f);

        public Texture2D HpFrame, HpFill, QiFill, QiPanel, QiLit, Panel, OrbIcon;

        public bool Ready => HpFrame != null && HpFill != null && QiFill != null && QiPanel != null && QiLit != null;

        public static HudArt Load()
        {
            var art = new HudArt
            {
                HpFrame = Get("hp_frame"),
                HpFill = Get("hp_fill"),
                QiFill = Get("qi_fill"),
                QiPanel = Get("qi_panel"),
                QiLit = Get("qi_lit"),
                Panel = Get("panel"),
                OrbIcon = Get("orb_icon"),
            };
            if (!art.Ready)
                Debug.LogWarning($"[Heavenly Warrior] Faltam imagens do HUD em Resources/{Folder}. Vou usar as barras simples.");
            return art;
        }

        static Texture2D Get(string name)
        {
            var tex = Resources.Load<Texture2D>(Folder + name);
            if (tex == null) Debug.LogWarning($"[Heavenly Warrior] Não encontrei Resources/{Folder}{name}.");
            return tex;
        }

        /// <summary>
        /// Desenha só a faixa horizontal [fromPx, toPx] de uma textura, na mesma posição em que
        /// apareceria se a textura inteira fosse desenhada em "full".
        /// </summary>
        public static void DrawSliceX(Rect full, Texture2D tex, float fromPx, float toPx)
        {
            if (tex == null || toPx <= fromPx) return;
            float u0 = fromPx / tex.width, u1 = toPx / tex.width;
            var r = new Rect(full.x + full.width * u0, full.y, full.width * (u1 - u0), full.height);
            GUI.DrawTextureWithTexCoords(r, tex, new Rect(u0, 0f, u1 - u0, 1f));
        }

        /// <summary>Desenha um retângulo [x0..x1, y0..y1] (píxeis, y a partir de cima) de uma textura.</summary>
        public static void DrawSlice(Rect full, Texture2D tex, float x0, float y0, float x1, float y1)
        {
            if (tex == null) return;
            float sx = full.width / tex.width, sy = full.height / tex.height;
            var r = new Rect(full.x + x0 * sx, full.y + y0 * sy, (x1 - x0) * sx, (y1 - y0) * sy);
            var uv = new Rect(x0 / tex.width, 1f - y1 / tex.height, (x1 - x0) / tex.width, (y1 - y0) / tex.height);
            GUI.DrawTextureWithTexCoords(r, tex, uv);
        }

        /// <summary>Moldura em 9 partes: os cantos ficam com o tamanho certo (vezes "scale") e o resto estica.</summary>
        public static void DrawNineSlice(Rect r, Texture2D tex, Vector4 border, float scale)
        {
            if (tex == null) return;
            float l = border.x, t = border.y, rt = border.z, b = border.w;
            float w = tex.width, h = tex.height;
            float L = Mathf.Min(l * scale, r.width * 0.5f), R = Mathf.Min(rt * scale, r.width * 0.5f);
            float T = Mathf.Min(t * scale, r.height * 0.5f), B = Mathf.Min(b * scale, r.height * 0.5f);
            float[] xs = { r.x, r.x + L, r.xMax - R, r.xMax };
            float[] ys = { r.y, r.y + T, r.yMax - B, r.yMax };
            float[] us = { 0f, l, w - rt, w };
            float[] vs = { 0f, t, h - b, h };
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                var dst = new Rect(xs[i], ys[j], xs[i + 1] - xs[i], ys[j + 1] - ys[j]);
                if (dst.width <= 0f || dst.height <= 0f) continue;
                var uv = new Rect(us[i] / w, 1f - vs[j + 1] / h, (us[i + 1] - us[i]) / w, (vs[j + 1] - vs[j]) / h);
                GUI.DrawTextureWithTexCoords(dst, tex, uv);
            }
        }
    }
}
