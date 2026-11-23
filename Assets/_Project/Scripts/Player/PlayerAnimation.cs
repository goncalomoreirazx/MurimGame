using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>Escolhe a animação do jogador a partir do estado do movimento e do combate.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimation : MonoBehaviour
    {
        public SpriteAnimator animator;
        public float idleFps = 4f;
        public float runFps = 10f;

        PlayerController _controller;
        PlayerCombat _combat;
        Rigidbody2D _rb;

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _combat = GetComponent<PlayerCombat>();
            _rb = GetComponent<Rigidbody2D>();
        }

        void LateUpdate()
        {
            if (animator == null) return;
            var v = _rb.GetVelocity();

            if (_controller.Health.IsDead) { animator.Play("death", 1f, false); return; }
            if (_controller.IsStunned) { animator.Play("hurt", 1f, false); return; }
            if (_controller.IsDashing) { animator.Play("dash", 1f, false); return; }

            if (_combat != null && _combat.IsAttacking)
            {
                string anim = "attack" + (_combat.ComboIndex + 1);
                animator.ShowFrame(anim, _combat.InStartup ? 0 : 1);
                return;
            }

            if (!_controller.IsGrounded) { animator.Play(v.y > 0f ? "jump" : "fall", 1f, false); return; }
            if (Mathf.Abs(v.x) > 0.5f) { animator.Play("run", runFps); return; }
            animator.Play("idle", idleFps);
        }
    }
}
