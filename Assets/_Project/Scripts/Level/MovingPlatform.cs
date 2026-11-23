using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Plataforma que vai e volta entre dois pontos. O jogador em cima é levado com ela
    /// (o PlayerController lê a Velocity).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        public Vector2 pointA;
        public Vector2 pointB;
        public float speed = 2.5f;
        [Tooltip("Pausa em cada ponta (segundos).")]
        public float waitTime = 0.4f;

        public Vector2 Velocity { get; private set; }

        Rigidbody2D _rb;
        bool _towardsB = true;
        float _wait;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_wait > 0f)
            {
                _wait -= dt;
                Velocity = Vector2.zero;
                return;
            }

            Vector2 target = _towardsB ? pointB : pointA;
            Vector2 next = Vector2.MoveTowards(_rb.position, target, speed * dt);
            Velocity = (next - _rb.position) / dt;
            _rb.MovePosition(next);

            if ((next - target).sqrMagnitude < 0.0001f)
            {
                _towardsB = !_towardsB;
                _wait = waitTime;
            }
        }
    }
}
