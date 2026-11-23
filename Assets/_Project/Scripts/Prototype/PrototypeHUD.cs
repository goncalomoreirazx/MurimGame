using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>
    /// HUD provisório (IMGUI): vida, orbes de Qi, tempo, barra de progresso do nível,
    /// título no início e ecrãs de vitória e de derrota.
    /// </summary>
    [RequireComponent(typeof(PrototypeArena))]
    public class PrototypeHUD : MonoBehaviour
    {
        public bool showControls = true;
        public float titleTime = 3.5f;

        PrototypeArena _arena;
        GUIStyle _label, _labelRight, _big, _small;
        float _titleUntil;
        PlayerController _lastPlayer;

        void Awake() => _arena = GetComponent<PrototypeArena>();

        void OnGUI()
        {
            if (_arena == null || _arena.Player == null) return;
            EnsureStyles();

            if (_lastPlayer != _arena.Player)
            {
                _lastPlayer = _arena.Player;
                _titleUntil = Time.unscaledTime + titleTime;
            }

            float scale = Mathf.Max(1f, Screen.height / 720f);
            var health = _arena.Player.Health;

            // Vida
            float x = 20f * scale, y = 18f * scale, w = 220f * scale, h = 18f * scale;
            DrawRect(new Rect(x - 2, y - 2, w + 4, h + 4), new Color(0f, 0f, 0f, 0.7f));
            DrawRect(new Rect(x, y, w * health.Normalized, h), new Color(0.3f, 0.85f, 0.45f));
            GUI.Label(new Rect(x + 6, y - 2, w, h + 4), $"Vida {health.Current}/{health.maxHealth}", _small);

            // Orbes e tempo
            GUI.Label(new Rect(x, y + h + 6 * scale, 400 * scale, 26 * scale),
                      $"Qi {_arena.OrbsCollected}/{_arena.OrbsTotal}", _label);
            GUI.Label(new Rect(Screen.width - 220 * scale, y - 2, 200 * scale, 30 * scale), FormatTime(_arena.ElapsedTime), _labelRight);

            DrawProgress(scale);

            if (CharacterSprites.Missing.Count > 0)
            {
                GUI.Label(new Rect(x, y + h + 34 * scale, Screen.width - 40 * scale, 26 * scale),
                          "<color=#ffb347>Sem sprites para: " + string.Join(", ", CharacterSprites.Missing) +
                          " (ver a Console)</color>", _small);
            }

            if (showControls && _arena.State == PrototypeArena.Outcome.Playing)
            {
                GUI.Label(new Rect(x, Screen.height - 48 * scale, Screen.width, 44 * scale),
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

        /// <summary>Linha no topo do ecrã: do portão de início ao portal, com o jogador e os checkpoints.</summary>
        void DrawProgress(float scale)
        {
            float w = Mathf.Min(420f * scale, Screen.width * 0.4f);
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
            float pw = 460 * scale, ph = 300 * scale;
            var r = new Rect((Screen.width - pw) * 0.5f, (Screen.height - ph) * 0.5f, pw, ph);
            DrawRect(r, new Color(0.06f, 0.07f, 0.12f, 0.92f));
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
