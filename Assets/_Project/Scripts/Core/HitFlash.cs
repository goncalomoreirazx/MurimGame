using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Pisca a branco quando o personagem leva dano e fica intermitente enquanto está invencível.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        public float flashTime = 0.08f;
        public bool blinkWhileInvulnerable = true;
        [Tooltip("Só os sprites dentro deste objeto piscam. Vazio = o próprio objeto.")]
        public Transform visualRoot;

        readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        readonly List<Color> _baseColors = new List<Color>();
        Health _health;
        float _flashUntil;

        void Awake()
        {
            _health = GetComponent<Health>();
            _health.Damaged += OnDamaged;
            CaptureColors();
        }

        void OnDestroy()
        {
            if (_health != null) _health.Damaged -= OnDamaged;
        }

        /// <summary>Volta a ler as cores atuais como cores "normais" (chamar depois de mudar a cor de um sprite).</summary>
        public void CaptureColors()
        {
            _renderers.Clear();
            _baseColors.Clear();
            var root = visualRoot != null ? visualRoot : transform;
            foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (r.CompareTag("EditorOnly")) continue;
                _renderers.Add(r);
                _baseColors.Add(r.color);
            }
        }

        public void SetBaseColor(SpriteRenderer target, Color color)
        {
            int i = _renderers.IndexOf(target);
            if (i >= 0) _baseColors[i] = color;
            target.color = color;
        }

        void OnDamaged(DamageInfo info) => _flashUntil = Time.time + flashTime;

        void LateUpdate()
        {
            bool flashing = Time.time < _flashUntil;
            bool hidden = !flashing && blinkWhileInvulnerable && !_health.IsDead
                          && _health.IsInvulnerable && !_health.ExtraInvulnerable
                          && Mathf.Repeat(Time.time * 16f, 1f) < 0.5f;

            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                Color c = flashing ? Color.white : _baseColors[i];
                if (hidden) c.a *= 0.25f;
                r.color = c;
            }
        }
    }
}
