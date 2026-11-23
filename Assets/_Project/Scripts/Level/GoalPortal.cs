using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>Portal da meta: pulsa para chamar a atenção.</summary>
    public class GoalPortal : MonoBehaviour
    {
        public SpriteRenderer glow;
        public Color color = new Color(0.55f, 0.9f, 1f, 0.55f);

        void Update()
        {
            if (glow == null) return;
            float p = 0.75f + 0.25f * Mathf.Sin(Time.time * 3f);
            var c = color;
            c.a *= p;
            glow.color = c;
        }
    }
}
