using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>Lanterna de pedra que acende quando o jogador passa: passa a ser o ponto onde volta.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public SpriteRenderer flame;
        public Color offColor = new Color(0.25f, 0.25f, 0.3f);
        public Color onColor = new Color(1f, 0.7f, 0.3f);

        public bool IsActive { get; private set; }

        void Start()
        {
            if (flame != null) flame.color = offColor;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            CameraFollow.Shake(0.04f, 0.1f);
        }

        void Update()
        {
            if (flame == null) return;
            if (!IsActive) { flame.color = offColor; return; }
            float flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 13f) * Mathf.Sin(Time.time * 7.3f);
            flame.color = onColor * flicker;
        }
    }
}
