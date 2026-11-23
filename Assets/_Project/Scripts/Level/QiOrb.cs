using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>Orbe de Qi para apanhar (como as moedas/anéis): flutua e desaparece ao tocar no jogador.</summary>
    public class QiOrb : MonoBehaviour
    {
        public float pickupRadius = 0.7f;
        public float bobHeight = 0.12f;

        Vector3 _base;
        float _phase;
        bool _collected;
        float _collectTime;

        public bool Collected => _collected;

        void Start()
        {
            _base = transform.position;
            _phase = transform.position.x * 0.7f;
        }

        /// <summary>Devolve true na frame em que é apanhado.</summary>
        public bool TryCollect(Vector2 playerCenter)
        {
            if (_collected) return false;
            if (((Vector2)transform.position - playerCenter).sqrMagnitude > pickupRadius * pickupRadius) return false;
            _collected = true;
            _collectTime = Time.time;
            return true;
        }

        void Update()
        {
            if (_collected)
            {
                // sobe e encolhe antes de desaparecer
                float t = (Time.time - _collectTime) / 0.25f;
                transform.position += Vector3.up * Time.deltaTime * 4f;
                transform.localScale = Vector3.one * Mathf.Max(0f, 1f - t);
                if (t >= 1f) gameObject.SetActive(false);
                return;
            }
            transform.position = _base + Vector3.up * Mathf.Sin(Time.time * 3f + _phase) * bobHeight;
            transform.localRotation = Quaternion.Euler(0f, 0f, 45f + Mathf.Sin(Time.time * 2f + _phase) * 10f);
        }
    }
}
