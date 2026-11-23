using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Lanterna de pedra que acende quando o jogador passa: passa a ser o ponto onde volta.
    /// Com sprites, anima "offFrames" (chama normal) e, depois de ativada, "onFrames" (energia de Qi).
    /// Sem sprites, muda só a cor da "flame".
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        public SpriteRenderer body;
        public Sprite[] offFrames;
        public Sprite[] onFrames;
        public float fps = 6f;

        [Header("Sem sprites")]
        public SpriteRenderer flame;
        public Color offColor = new Color(0.25f, 0.25f, 0.3f);
        public Color onColor = new Color(1f, 0.7f, 0.3f);

        public bool IsActive { get; private set; }

        float _activatedAt;
        Vector3 _bodyScale;

        void Start()
        {
            if (body != null) _bodyScale = body.transform.localScale;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            _activatedAt = Time.time;
            CameraFollow.Shake(0.04f, 0.1f);
        }

        void Update()
        {
            var frames = IsActive && onFrames != null && onFrames.Length > 0 ? onFrames : offFrames;
            if (body != null && frames != null && frames.Length > 0)
            {
                int i = Mathf.FloorToInt(Time.time * fps + transform.position.x) % frames.Length;
                body.sprite = frames[Mathf.Abs(i)];
                // pequeno "pulo" ao acender
                float pop = IsActive ? Mathf.Clamp01(1f - (Time.time - _activatedAt) / 0.35f) : 0f;
                body.transform.localScale = _bodyScale * (1f + 0.15f * Mathf.Sin(pop * Mathf.PI));
            }

            if (flame == null) return;
            if (!IsActive) { flame.color = offColor; return; }
            float flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 13f) * Mathf.Sin(Time.time * 7.3f);
            flame.color = onColor * flicker;
        }
    }
}
