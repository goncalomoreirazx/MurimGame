using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>Barra de vida pequena por cima de um inimigo. Só aparece depois do primeiro golpe.</summary>
    public class WorldHealthBar : MonoBehaviour
    {
        public Health health;
        public SpriteRenderer background;
        public SpriteRenderer fill;
        public float width = 0.9f;
        public bool hideWhenFull = true;

        void LateUpdate()
        {
            if (health == null || fill == null) return;
            float t = health.Normalized;
            bool show = !(hideWhenFull && t >= 1f) && !health.IsDead;
            fill.enabled = show;
            if (background != null) background.enabled = show;
            if (!show) return;

            var s = fill.transform.localScale;
            s.x = width * t;
            fill.transform.localScale = s;
            fill.transform.localPosition = new Vector3(-width * (1f - t) * 0.5f, fill.transform.localPosition.y, 0f);
        }
    }
}
