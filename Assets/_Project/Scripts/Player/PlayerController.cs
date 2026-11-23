using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Movimento do jogador: correr, saltar (com coyote time, buffer e altura variável),
    /// salto duplo e dash com invencibilidade.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Corrida")]
        public float moveSpeed = 8f;
        public float groundAcceleration = 90f;
        public float groundDeceleration = 110f;
        public float airAcceleration = 60f;
        public float airDeceleration = 35f;

        [Header("Salto")]
        public float jumpVelocity = 16f;
        [Tooltip("Saltos extra no ar (1 = salto duplo).")]
        public int airJumps = 1;
        public float airJumpVelocity = 14f;
        [Tooltip("Ao largar o botão de salto a subir, a velocidade vertical é multiplicada por isto.")]
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.45f;
        [Tooltip("Tempo depois de sair de uma plataforma em que ainda se pode saltar.")]
        public float coyoteTime = 0.1f;
        [Tooltip("Tempo em que um salto carregado antes de tocar no chão ainda conta.")]
        public float jumpBufferTime = 0.12f;
        public float gravityScale = 4f;
        public float fallGravityMultiplier = 1.6f;
        public float maxFallSpeed = 22f;

        [Header("Dash")]
        public float dashSpeed = 20f;
        public float dashTime = 0.15f;
        public float dashCooldown = 0.4f;

        [Header("Dano recebido")]
        public float hurtInvulnerability = 0.8f;

        public int Facing { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public bool IsDashing => _dashTimer > 0f;
        public bool IsStunned => _stunTimer > 0f;
        public Health Health => _health;
        /// <summary>Pode atacar/dar dash/mudar de direção.</summary>
        public bool CanAct => !IsDashing && !IsStunned && !_health.IsDead;

        /// <summary>Desligado durante o ecrã de vitória ou cenas: o jogador fica parado.</summary>
        [System.NonSerialized] public bool ControlsEnabled = true;

        /// <summary>Posição para onde o jogador volta ao recomeçar.</summary>
        public Vector2 SpawnPoint { get; set; }

        /// <summary>Opcional: o visual que é espelhado quando o jogador vira.</summary>
        public Transform visual;

        Rigidbody2D _rb;
        BoxCollider2D _col;
        Health _health;
        PlayerCombat _combat;

        float _moveInput;
        bool _jumpHeld;
        float _jumpBufferTimer;
        float _coyoteTimer;
        int _airJumpsLeft;
        bool _jumpCutAvailable;

        float _dashTimer;
        float _dashCooldownTimer;
        bool _airDashAvailable = true;
        int _dashDirection = 1;

        float _stunTimer;
        float _lungeTimer;
        float _lungeSpeed;

        void Awake()
        {
            Instance = this;
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _health = GetComponent<Health>();
            _combat = GetComponent<PlayerCombat>();

            _rb.gravityScale = gravityScale;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _health.team = Team.Player;
            _health.invulnerableTime = hurtInvulnerability;
            _health.Damaged += OnDamaged;

            SpawnPoint = transform.position;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_health != null) _health.Damaged -= OnDamaged;
        }

        void Update()
        {
            if (_health.IsDead || !ControlsEnabled)
            {
                _moveInput = 0f;
                _jumpHeld = false;
                _jumpBufferTimer = 0f;
                return;
            }

            _moveInput = GameInput.MoveX;
            _jumpHeld = GameInput.JumpHeld;

            if (GameInput.JumpPressed) _jumpBufferTimer = jumpBufferTime;
            else _jumpBufferTimer -= Time.deltaTime;

            _dashCooldownTimer -= Time.deltaTime;

            bool attacking = _combat != null && _combat.IsAttacking;

            if (GameInput.DashPressed && TryStartDash()) return;

            if (CanAct && !attacking && Mathf.Abs(_moveInput) > 0.1f)
                SetFacing(_moveInput > 0f ? 1 : -1);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var v = _rb.GetVelocity();

            var ground = v.y <= 0.1f ? FindGround() : null;
            IsGrounded = ground != null;

            // Plataformas móveis levam o jogador com elas
            var platform = ground != null ? ground.GetComponent<MovingPlatform>() : null;
            if (platform != null) _rb.position += platform.Velocity * dt;

            if (IsGrounded)
            {
                _coyoteTimer = coyoteTime;
                _airJumpsLeft = airJumps;
                _airDashAvailable = true;
            }
            else
            {
                _coyoteTimer -= dt;
            }

            if (_health.IsDead)
            {
                v.x = Mathf.MoveTowards(v.x, 0f, groundDeceleration * dt);
                _rb.gravityScale = gravityScale;
                _rb.SetVelocity(v);
                return;
            }

            if (IsDashing)
            {
                _dashTimer -= dt;
                v = new Vector2(_dashDirection * dashSpeed, 0f);
                _rb.gravityScale = 0f;
                if (_dashTimer <= 0f)
                {
                    _health.ExtraInvulnerable = false;
                    v.x *= 0.4f;
                }
                _rb.SetVelocity(v);
                return;
            }

            if (IsStunned)
            {
                _stunTimer -= dt;
                _rb.gravityScale = gravityScale;
                v.x = Mathf.MoveTowards(v.x, 0f, airDeceleration * dt);
                _rb.SetVelocity(ClampFall(v));
                return;
            }

            // Corrida horizontal
            float multiplier = _combat != null ? _combat.MoveMultiplier(IsGrounded) : 1f;
            float target = _moveInput * moveSpeed * multiplier;
            bool accelerating = Mathf.Abs(target) > 0.01f;
            float rate = IsGrounded
                ? (accelerating ? groundAcceleration : groundDeceleration)
                : (accelerating ? airAcceleration : airDeceleration);

            if (_lungeTimer > 0f)
            {
                _lungeTimer -= dt;
                v.x = Facing * _lungeSpeed;
            }
            else
            {
                v.x = Mathf.MoveTowards(v.x, target, rate * dt);
            }

            // Salto
            bool canStartJump = _combat == null || _combat.AllowsJump;
            if (_jumpBufferTimer > 0f && canStartJump)
            {
                if (_coyoteTimer > 0f)
                {
                    v.y = jumpVelocity;
                    StartJump();
                }
                else if (_airJumpsLeft > 0)
                {
                    _airJumpsLeft--;
                    v.y = airJumpVelocity;
                    StartJump();
                }
            }

            // Altura variável: largar o botão corta a subida
            if (_jumpCutAvailable && !_jumpHeld && v.y > 0f)
            {
                v.y *= jumpCutMultiplier;
                _jumpCutAvailable = false;
            }
            if (v.y <= 0f) _jumpCutAvailable = false;

            _rb.gravityScale = v.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
            _rb.SetVelocity(ClampFall(v));
        }

        void StartJump()
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _jumpCutAvailable = true;
            IsGrounded = false;
            if (_combat != null) _combat.CancelAttack();
        }

        Vector2 ClampFall(Vector2 v)
        {
            v.y = Mathf.Max(v.y, -maxFallSpeed);
            return v;
        }

        bool TryStartDash()
        {
            if (_health.IsDead || IsDashing || IsStunned || _dashCooldownTimer > 0f) return false;
            if (!IsGrounded && !_airDashAvailable) return false;
            if (_combat != null && !_combat.AllowsDashCancel) return false;

            if (_combat != null) _combat.CancelAttack();
            if (!IsGrounded) _airDashAvailable = false;

            _dashDirection = Mathf.Abs(_moveInput) > 0.1f ? (_moveInput > 0f ? 1 : -1) : Facing;
            SetFacing(_dashDirection);
            _dashTimer = dashTime;
            _dashCooldownTimer = dashCooldown + dashTime;
            _lungeTimer = 0f;
            _health.ExtraInvulnerable = true;
            return true;
        }

        Collider2D FindGround()
        {
            Bounds b = _col.bounds;
            var center = new Vector2(b.center.x, b.min.y - 0.04f);
            var size = new Vector2(b.size.x * 0.9f, 0.08f);
            return PhysicsUtil.FindSolid(center, size, _col);
        }

        public void SetFacing(int dir)
        {
            if (dir == 0) return;
            Facing = dir > 0 ? 1 : -1;
            if (visual != null)
            {
                var s = visual.localScale;
                s.x = Mathf.Abs(s.x) * Facing;
                visual.localScale = s;
            }
        }

        /// <summary>Pequeno avanço usado pelos golpes.</summary>
        public void Lunge(float speed, float duration)
        {
            if (!IsGrounded) return;
            _lungeSpeed = speed;
            _lungeTimer = duration;
        }

        /// <summary>Usado pelos golpes aéreos: trava a queda por um instante.</summary>
        public void HoverInAir()
        {
            if (IsGrounded) return;
            var v = _rb.GetVelocity();
            if (v.y < 0f) _rb.SetVelocity(new Vector2(v.x, 0f));
        }

        void OnDamaged(DamageInfo info)
        {
            _dashTimer = 0f;
            _lungeTimer = 0f;
            _health.ExtraInvulnerable = false;
            _stunTimer = info.Stun;
            if (_combat != null) _combat.CancelAttack();
            _rb.SetVelocity(info.Knockback);
            CameraFollow.Shake(0.15f, 0.15f);
        }

        /// <summary>Põe o jogador numa posição, parado, sem mexer na vida (usado depois de cair num buraco).</summary>
        public void Teleport(Vector2 position)
        {
            transform.position = position;
            _rb.position = position;
            _rb.SetVelocity(Vector2.zero);
            _dashTimer = 0f;
            _stunTimer = 0f;
            _lungeTimer = 0f;
            _health.ExtraInvulnerable = false;
            if (_combat != null) _combat.CancelAttack();
        }

        /// <summary>Volta ao ponto inicial com a vida cheia.</summary>
        public void Respawn()
        {
            transform.position = SpawnPoint;
            _rb.SetVelocity(Vector2.zero);
            _dashTimer = 0f;
            _stunTimer = 0f;
            _lungeTimer = 0f;
            _health.ExtraInvulnerable = false;
            _health.ResetHealth();
            if (_combat != null) _combat.CancelAttack();
            SetFacing(1);
        }

        void OnDrawGizmosSelected()
        {
            var c = GetComponent<BoxCollider2D>();
            if (c == null) return;
            Bounds b = c.bounds;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - 0.04f), new Vector3(b.size.x * 0.9f, 0.08f));
        }
    }
}
