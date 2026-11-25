using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>
    /// HUD (IMGUI): moldura com o retrato e as barras de vida e de Qi, painel das orbes, tempo,
    /// barra de progresso do nível, título no início e ecrãs de vitória e de derrota.
    /// Usa as imagens de Resources/UI (ver HudArt); sem elas, desenha barras simples.
    /// </summary>
    [RequireComponent(typeof(PrototypeArena))]
    public class PrototypeHUD : MonoBehaviour
    {
        public bool showControls = true;
        public float titleTime = 3.5f;

        PrototypeArena _arena;
        GUIStyle _label, _labelRight, _big, _small, _smallRight, _barText;
        float _titleUntil;
        PlayerController _lastPlayer;
        HudArt _art;
        float _shownHp = 1f, _trailHp = 1f, _trailHoldUntil, _shownQi, _orbPulseUntil;
        int _lastOrbs;

        void Awake() => _arena = GetComponent<PrototypeArena>();

        void OnGUI()
        {
            if (_arena == null || _arena.Player == null) return;
            EnsureStyles();
            if (_art == null) _art = HudArt.Load();

            if (_lastPlayer != _arena.Player)
            {
                _lastPlayer = _arena.Player;
                _titleUntil = Time.unscaledTime + titleTime;
                _shownHp = _trailHp = _arena.Player.Health.Normalized;
                _shownQi = 0f;
            }

            float scale = Mathf.Max(1f, Screen.height / 720f);
            UpdateBars();

            float leftBottom, rightWidth;
            if (_art.Ready)
            {
                leftBottom = DrawHealthArt(scale);
                rightWidth = DrawQiArt(scale);
            }
            else
            {
                leftBottom = DrawHealthSimple(scale);
                rightWidth = 220f * scale;
            }

            DrawProgress(scale, 400f * scale, rightWidth);

            if (CharacterSprites.Missing.Count > 0)
            {
                GUI.Label(new Rect(20f * scale, leftBottom + 4f * scale, Screen.width - 40 * scale, 26 * scale),
                          "<color=#ffb347>Sem sprites para: " + string.Join(", ", CharacterSprites.Missing) +
                          " (ver a Console)</color>", _small);
            }

            if (showControls && _arena.State == PrototypeArena.Outcome.Playing)
            {
                GUI.Label(new Rect(20f * scale, Screen.height - 48 * scale, Screen.width, 44 * scale),
                    "A/D mover   Espaço saltar (2x)   J/clique atacar   Shift dash\n" +
                    "Comando: analógico, A saltar, X atacar, B dash", _small);
            }

            // Título no início
            if (_arena.State == PrototypeArena.Outcome.Playing && Time.unscaledTime < _titleUntil)
            {
                float a = Mathf.Clamp01((_titleUntil - Time.unscaledTime) / 0.6f);
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 120 * scale),
                          $"Nível 1 · {_arena.LevelName}\n<size={Mathf.RoundToInt(18 * scale)}>Chega ao portal no fim da floresta</size>", _big);
                GUI.color = old;
            }

            switch (_arena.State)
            {
                case PrototypeArena.Outcome.Won:
                    Panel(scale, "Nível concluído!",
                          $"Tempo  {FormatTime(_arena.ElapsedTime)}\n" +
                          $"Orbes de Qi  {_arena.OrbsCollected}/{_arena.OrbsTotal}\n" +
                          $"Inimigos derrotados  {_arena.EnemiesDefeated}/{_arena.EnemiesTotal}\n" +
                          $"Quedas  {_arena.Falls}\n\nR para jogar outra vez");
                    break;
                case PrototypeArena.Outcome.GameOver:
                    Panel(scale, "Caíste em combate", "R para voltar ao último checkpoint");
                    break;
            }
        }

        // ---------------------------------------------------------------- barras

        /// <summary>
        /// A vida mostrada acompanha a real depressa; o "rasto" claro fica um bocadinho para trás quando
        /// levas dano, para se ver quanto perdeste.
        /// </summary>
        void UpdateBars()
        {
            if (Event.current.type != EventType.Repaint) return;
            float dt = Time.unscaledDeltaTime;
            float hp = _arena.Player.Health.Normalized;
            float qi = _arena.OrbsTotal > 0 ? (float)_arena.OrbsCollected / _arena.OrbsTotal : 0f;

            if (hp < _shownHp - 0.001f) _trailHoldUntil = Time.unscaledTime + 0.4f;
            _shownHp = Mathf.MoveTowards(_shownHp, hp, dt * 3f);
            if (hp >= _trailHp) _trailHp = hp;
            else if (Time.unscaledTime > _trailHoldUntil) _trailHp = Mathf.MoveTowards(_trailHp, hp, dt * 0.8f);
            _trailHp = Mathf.Max(_trailHp, _shownHp);

            if (_arena.OrbsCollected > _lastOrbs) _orbPulseUntil = Time.unscaledTime + 0.35f;
            _lastOrbs = _arena.OrbsCollected;
            _shownQi = Mathf.MoveTowards(_shownQi, qi, dt * 1.5f);
        }

        /// <summary>Moldura com o retrato: barra vermelha = vida, barra azul = Qi apanhado no nível.</summary>
        float DrawHealthArt(float scale)
        {
            var health = _arena.Player.Health;
            float w = 380f * scale;
            var frame = new Rect(10f * scale, 6f * scale, w, w * _art.HpFrame.height / _art.HpFrame.width);
            GUI.DrawTexture(frame, _art.HpFrame);

            var old = GUI.color;
            // Rasto do dano
            GUI.color = new Color(1f, 0.92f, 0.8f, 0.5f);
            HudArt.DrawSliceX(frame, _art.HpFill, 0f, BarEnd(_art.HpFill, HudArt.HpBarLeft, HudArt.HpBarRight, _trailHp));
            // Vida (pisca quando está quase a acabar)
            float pulse = health.Current <= 2 && !health.IsDead ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f) : 1f;
            GUI.color = new Color(pulse, pulse, pulse, 1f);
            HudArt.DrawSliceX(frame, _art.HpFill, 0f, BarEnd(_art.HpFill, HudArt.HpBarLeft, HudArt.HpBarRight, _shownHp));
            GUI.color = old;
            HudArt.DrawSliceX(frame, _art.QiFill, 0f, BarEnd(_art.QiFill, HudArt.QiBarLeft, HudArt.QiBarRight, _shownQi));

            // Números da vida em cima da barra
            float sx = frame.width / _art.HpFrame.width;
            var bar = new Rect(frame.x + HudArt.HpBarLeft * sx, frame.y + HudArt.HpBarTop * sx,
                               (HudArt.HpBarRight - HudArt.HpBarLeft) * sx, (HudArt.HpBarBottom - HudArt.HpBarTop) * sx);
            ShadowLabel(bar, $"{health.Current}/{health.maxHealth}", _barText);
            return frame.yMax;
        }

        static float BarEnd(Texture2D tex, float left, float right, float t)
        {
            if (t <= 0.001f) return 0f;
            if (t >= 0.999f) return tex.width;
            return Mathf.Lerp(left, right, t);
        }

        /// <summary>Painel das orbes no canto direito: as 7 casas acendem à medida que apanhas o Qi do nível.</summary>
        float DrawQiArt(float scale)
        {
            float w = 290f * scale;
            float h = w * _art.QiPanel.height / _art.QiPanel.width;
            var panel = new Rect(Screen.width - w - 10f * scale, 8f * scale, w, h);
            GUI.DrawTexture(panel, _art.QiPanel);

            int slots = HudArt.QiSlotX.Length;
            int total = _arena.OrbsTotal, got = _arena.OrbsCollected;
            int lit = total <= 0 ? 0 : got >= total ? slots : Mathf.FloorToInt((float)got * slots / total);
            float r = HudArt.QiSlotRadius + 1f;
            for (int i = 0; i < lit; i++)
            {
                float cx = HudArt.QiSlotX[i];
                HudArt.DrawSlice(panel, _art.QiLit, cx - r, HudArt.QiSlotY - r, cx + r, HudArt.QiSlotY + r);
            }

            // Brilho na orbe grande quando apanhas uma
            if (_art.OrbIcon != null && Time.unscaledTime < _orbPulseUntil)
            {
                float k = (_orbPulseUntil - Time.unscaledTime) / 0.35f;
                float size = h * (0.9f + 0.35f * (1f - k));
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, k * 0.8f);
                GUI.DrawTexture(new Rect(panel.x + h * 0.55f - size * 0.5f, panel.y + h * 0.5f - size * 0.5f, size, size), _art.OrbIcon);
                GUI.color = old;
            }

            // Contador e tempo por baixo do painel
            var line = new Rect(panel.x, panel.yMax - 2f * scale, panel.width - 12f * scale, 26f * scale);
            ShadowLabel(line, $"Qi  {got}/{total}", _labelRight);
            line.y += 22f * scale;
            ShadowLabel(line, FormatTime(_arena.ElapsedTime), _smallRight);
            return w;
        }

        /// <summary>HUD simples (sem as imagens de Resources/UI).</summary>
        float DrawHealthSimple(float scale)
        {
            var health = _arena.Player.Health;
            float x = 20f * scale, y = 18f * scale, w = 220f * scale, h = 18f * scale;
            DrawRect(new Rect(x - 2, y - 2, w + 4, h + 4), new Color(0f, 0f, 0f, 0.7f));
            DrawRect(new Rect(x, y, w * _shownHp, h), new Color(0.3f, 0.85f, 0.45f));
            GUI.Label(new Rect(x + 6, y - 2, w, h + 4), $"Vida {health.Current}/{health.maxHealth}", _small);
            GUI.Label(new Rect(x, y + h + 6 * scale, 400 * scale, 26 * scale), $"Qi {_arena.OrbsCollected}/{_arena.OrbsTotal}", _label);
            GUI.Label(new Rect(Screen.width - 220 * scale, y - 2, 200 * scale, 30 * scale), FormatTime(_arena.ElapsedTime), _labelRight);
            return y + h + 30f * scale;
        }

        void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);
            style.normal.textColor = old;
            GUI.Label(r, text, style);
        }

        /// <summary>Linha no topo do ecrã: do portão de início ao portal, com o jogador e os checkpoints.</summary>
        void DrawProgress(float scale, float leftWidth, float rightWidth)
        {
            float room = Screen.width - 2f * Mathf.Max(leftWidth, rightWidth) - 60f * scale;
            float w = Mathf.Min(420f * scale, Screen.width * 0.4f, room);
            if (w < 80f * scale) return; // ecrã estreito: não cabe
            float x = (Screen.width - w) * 0.5f, y = 26f * scale;
            float start = _arena.LevelStartX, end = _arena.LevelGoalX;

            DrawRect(new Rect(x, y, w, 4 * scale), new Color(1f, 1f, 1f, 0.35f));
            foreach (var cp in _arena.Checkpoints)
            {
                float t = Mathf.InverseLerp(start, end, cp.transform.position.x);
                DrawRect(new Rect(x + w * t - 3 * scale, y - 4 * scale, 6 * scale, 12 * scale),
                         cp.IsActive ? new Color(1f, 0.7f, 0.3f) : new Color(0.6f, 0.6f, 0.65f));
            }
            DrawRect(new Rect(x + w - 4 * scale, y - 8 * scale, 8 * scale, 20 * scale), new Color(0.55f, 0.9f, 1f));

            float p = Mathf.InverseLerp(start, end, _arena.Player.transform.position.x);
            DrawRect(new Rect(x + w * p - 6 * scale, y - 5 * scale, 12 * scale, 14 * scale), new Color(0.35f, 0.65f, 1f));
        }

        void Panel(float scale, string title, string body)
        {
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.45f));
            float pw = 480 * scale, ph = 310 * scale;
            var r = new Rect((Screen.width - pw) * 0.5f, (Screen.height - ph) * 0.5f, pw, ph);
            if (_art != null && _art.Panel != null) HudArt.DrawNineSlice(r, _art.Panel, HudArt.PanelBorder, scale * 0.8f);
            else DrawRect(r, new Color(0.06f, 0.07f, 0.12f, 0.92f));
            GUI.Label(new Rect(r.x, r.y + 20 * scale, r.width, 50 * scale), title, _big);
            GUI.Label(new Rect(r.x, r.y + 80 * scale, r.width, r.height - 90 * scale), body, _labelCenter);
        }

        GUIStyle _labelCenter;

        void EnsureStyles()
        {
            if (_label != null) return;
            int s = Mathf.RoundToInt(Mathf.Max(1f, Screen.height / 720f));
            _label = new GUIStyle(GUI.skin.label) { fontSize = 18 * s, richText = true };
            _label.normal.textColor = Color.white;
            _labelRight = new GUIStyle(_label) { alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold };
            _labelCenter = new GUIStyle(_label) { alignment = TextAnchor.UpperCenter };
            _small = new GUIStyle(_label) { fontSize = 14 * s };
            _smallRight = new GUIStyle(_small) { alignment = TextAnchor.UpperRight };
            _barText = new GUIStyle(_label) { fontSize = 12 * s, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _big = new GUIStyle(_label) { fontSize = 34 * s, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            float s = seconds - m * 60f;
            return $"{m}:{s:00.0}";
        }

        static void DrawRect(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
