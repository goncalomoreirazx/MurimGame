using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>HUD provisório (IMGUI): vida do jogador, controlos e mensagens de fim.</summary>
    [RequireComponent(typeof(PrototypeArena))]
    public class PrototypeHUD : MonoBehaviour
    {
        public bool showControls = true;

        PrototypeArena _arena;
        GUIStyle _label;
        GUIStyle _big;

        void Awake() => _arena = GetComponent<PrototypeArena>();

        void OnGUI()
        {
            if (_arena == null || _arena.Player == null) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _label.normal.textColor = Color.white;
                _big = new GUIStyle(_label) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }

            var health = _arena.Player.Health;

            // Barra de vida
            const float x = 20f, y = 20f, w = 240f, h = 18f;
            DrawRect(new Rect(x - 2, y - 2, w + 4, h + 4), new Color(0f, 0f, 0f, 0.7f));
            DrawRect(new Rect(x, y, w * health.Normalized, h), new Color(0.3f, 0.85f, 0.45f));
            GUI.Label(new Rect(x + 6, y - 2, w, h + 4), $"Vida {health.Current}/{health.maxHealth}", _label);

            GUI.Label(new Rect(x, y + 26, 400, 24), $"Inimigos: {_arena.EnemiesLeft}", _label);

            if (showControls)
            {
                GUI.Label(new Rect(x, Screen.height - 54, 900, 50),
                    "A/D ou setas: mover   Espaço: saltar (2x no ar)   J ou clique: atacar (combo de 3)   Shift ou L: dash\n" +
                    "Comando: analógico, A saltar, X atacar, B dash", _label);
            }

            string message = null;
            if (health.IsDead) message = "Caíste em combate\nR para recomeçar";
            else if (_arena.EnemiesLeft == 0) message = "Arena limpa!\nR para recomeçar";

            if (message != null)
                GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 120), message, _big);
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
