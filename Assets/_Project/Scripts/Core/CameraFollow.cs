using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>Câmara que segue o jogador suavemente, com antecipação na direção para onde olha e tremor nos impactos.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1.5f);
        public float lookAhead = 1.5f;
        public float smoothTime = 0.15f;
        [Tooltip("Limites horizontais da câmara (min, max). Iguais = sem limite.")]
        public Vector2 xLimits = Vector2.zero;

        static CameraFollow _main;
        Vector3 _velocity;
        float _shakeUntil;
        float _shakeStrength;

        void Awake() => _main = this;

        public static void Shake(float strength, float duration)
        {
            if (_main == null) return;
            _main._shakeStrength = Mathf.Max(_main._shakeStrength, strength);
            _main._shakeUntil = Mathf.Max(_main._shakeUntil, Time.unscaledTime + duration);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            transform.position = GoalPosition();
            _velocity = Vector3.zero;
        }

        Vector3 GoalPosition()
        {
            float facing = 1f;
            var player = target.GetComponent<PlayerController>();
            if (player != null) facing = player.Facing;

            var goal = new Vector3(target.position.x + offset.x + facing * lookAhead,
                                   target.position.y + offset.y,
                                   transform.position.z);
            if (xLimits.x < xLimits.y) goal.x = Mathf.Clamp(goal.x, xLimits.x, xLimits.y);
            return goal;
        }

        void LateUpdate()
        {
            if (target == null) return;

            var pos = Vector3.SmoothDamp(transform.position, GoalPosition(), ref _velocity, smoothTime);

            if (Time.unscaledTime < _shakeUntil)
                pos += (Vector3)(Random.insideUnitCircle * _shakeStrength);
            else
                _shakeStrength = 0f;

            transform.position = pos;
        }
    }
}
