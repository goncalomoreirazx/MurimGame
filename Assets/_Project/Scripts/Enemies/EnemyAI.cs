using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Inimigo simples: patrulha perto do ponto inicial, persegue o jogador quando o vê,
    /// prepara o golpe (fica vermelho, dá tempo para reagir) e ataca.
    /// Cada golpe recebido interrompe o que estava a fazer.
    /// Com "isTrainingDummy" fica parado, não ataca e volta à vida pouco depois de morrer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        public enum State { Patrol, Chase, Windup, Attack, Recover, Stunned, Dead }

        [Header("Movimento")]
        public float patrolSpeed = 1.8f;
        public float chaseSpeed = 3.6f;
        public float patrolDistance = 3f;
        public float gravityScale = 4f;

        [Header("Deteção")]
        public float detectRange = 7f;
        public float loseRange = 11f;
        public float verticalTolerance = 2.5f;

        [Header("Ataque")]
        public float attackRange = 1.3f;
        public float windupTime = 0.45f;
        public float attackCooldown = 0.9f;
        public AttackData attack = new AttackData
        {
            name = "Golpe inimigo", damage = 1, startup = 0f, active = 0.12f, recovery = 0.5f,
            hitboxOffset = new Vector2(0.8f, 0.1f), hitboxSize = new Vector2(1.3f, 1.0f),
            knockback = new Vector2(7f, 5f), stun = 0.35f, hitstop = 0.06f, shake = 0.1f,
            lungeSpeed = 5f, lungeTime = 0.1f
        };

        [Header("Resistência")]
        [Tooltip("Se ativo, os golpes não interrompem o inimigo enquanto prepara ou dá um ataque.")]
        public bool superArmor;
        [Tooltip("Multiplica o empurrão recebido (0 = não se mexe, 1 = normal).")]
        [Range(0f, 1f)] public float knockbackMultiplier = 1f;

        [Header("Outros")]
        public bool isTrainingDummy;
        public float dummyRespawnTime = 1.5f;
        public float corpseTime = 1.2f;

        [Header("Visual")]
        public Transform visual;
        public SpriteRenderer tintTarget;
        public Color windupColor = new Color(1f, 0.35f, 0.25f);
        public SpriteAnimator animator;

        public State CurrentState => _state;
        public int Facing => _facing;

        Rigidbody2D _rb;
        BoxCollider2D _col;
        Health _health;
        HitFlash _flash;
        State _state = State.Patrol;
        float _stateTimer;
        float _cooldownTimer;
        int _facing = -1;
        Vector2 _home;
        Color _baseTint = Color.white;

        readonly HashSet<Health> _hitThisSwing = new HashSet<Health>();
        readonly List<Health> _buffer = new List<Health>();

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _health = GetComponent<Health>();
            _flash = GetComponent<HitFlash>();

            _rb.gravityScale = gravityScale;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            _health.team = Team.Enemy;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;

            _home = transform.position;
            if (tintTarget != null) _baseTint = tintTarget.color;
            SetFacing(_facing);
        }

        void OnDestroy()
        {
            if (_health == null) return;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        void Update()
        {
            _stateTimer -= Time.deltaTime;
            _cooldownTimer -= Time.deltaTime;
            UpdateAnimation();

            if (_state == State.Dead)
            {
                if (_stateTimer <= 0f)
                {
                    if (isTrainingDummy) Revive();
                    else Destroy(gameObject);
                }
                return;
            }

            if (isTrainingDummy)
            {
                if (_state == State.Stunned && _stateTimer <= 0f) SetState(State.Patrol);
                return;
            }

            var player = PlayerController.Instance;
            bool playerAlive = player != null && !player.Health.IsDead;
            Vector2 toPlayer = playerAlive ? (Vector2)(player.transform.position - transform.position) : Vector2.zero;

            switch (_state)
            {
                case State.Patrol:
                    if (playerAlive && CanSee(toPlayer, detectRange)) SetState(State.Chase);
                    break;

                case State.Chase:
                    if (!playerAlive || !CanSee(toPlayer, loseRange)) { SetState(State.Patrol); break; }
                    SetFacing(toPlayer.x >= 0f ? 1 : -1);
                    if (Mathf.Abs(toPlayer.x) <= attackRange && _cooldownTimer <= 0f)
                    {
                        SetState(State.Windup, windupTime);
                        SetTint(windupColor);
                    }
                    break;

                case State.Windup:
                    if (_stateTimer <= 0f)
                    {
                        SetTint(_baseTint);
                        _hitThisSwing.Clear();
                        SetState(State.Attack, attack.active);
                    }
                    break;

                case State.Attack:
                    DoHits();
                    if (_stateTimer <= 0f)
                    {
                        _cooldownTimer = attackCooldown;
                        SetState(State.Recover, attack.recovery);
                    }
                    break;

                case State.Recover:
                case State.Stunned:
                    if (_stateTimer <= 0f) SetState(playerAlive ? State.Chase : State.Patrol);
                    break;
            }
        }

        void FixedUpdate()
        {
            var v = _rb.GetVelocity();

            switch (_state)
            {
                case State.Patrol:
                    if (isTrainingDummy) { v.x = 0f; break; }
                    float offset = transform.position.x - _home.x;
                    if (offset > patrolDistance) SetFacing(-1);
                    else if (offset < -patrolDistance) SetFacing(1);
                    if (Blocked()) SetFacing(-_facing);
                    v.x = Blocked() ? 0f : _facing * patrolSpeed;
                    break;

                case State.Chase:
                    v.x = Blocked() ? 0f : _facing * chaseSpeed;
                    break;

                case State.Attack:
                    v.x = Blocked() ? 0f : _facing * attack.lungeSpeed;
                    break;

                case State.Windup:
                case State.Recover:
                case State.Dead:
                    v.x = Mathf.MoveTowards(v.x, 0f, 40f * Time.fixedDeltaTime);
                    break;

                case State.Stunned:
                    v.x = Mathf.MoveTowards(v.x, 0f, 20f * Time.fixedDeltaTime);
                    break;
            }

            _rb.SetVelocity(v);
        }

        bool CanSee(Vector2 toPlayer, float range) =>
            Mathf.Abs(toPlayer.x) <= range && Mathf.Abs(toPlayer.y) <= verticalTolerance;

        /// <summary>Há uma parede à frente ou acaba o chão (para não cair das plataformas).</summary>
        bool Blocked()
        {
            Bounds b = _col.bounds;
            float frontX = _facing > 0 ? b.max.x + 0.1f : b.min.x - 0.1f;
            bool wall = PhysicsUtil.IsSolid(new Vector2(frontX, b.center.y), new Vector2(0.1f, b.size.y * 0.6f), _col);
            bool groundAhead = PhysicsUtil.IsSolid(new Vector2(frontX, b.min.y - 0.15f), new Vector2(0.1f, 0.2f), _col);
            bool grounded = PhysicsUtil.IsSolid(new Vector2(b.center.x, b.min.y - 0.05f), new Vector2(b.size.x * 0.9f, 0.1f), _col);
            return wall || (grounded && !groundAhead);
        }

        void DoHits()
        {
            int hits = attack.ApplyHits(transform.position, _facing, _health.team, gameObject, _hitThisSwing, _buffer);
            if (hits > 0)
            {
                HitStop.Do(attack.hitstop);
                CameraFollow.Shake(attack.shake, 0.12f);
            }
        }

        void OnDamaged(DamageInfo info)
        {
            if (_health.IsDead) return;
            bool armored = superArmor && (_state == State.Windup || _state == State.Attack);
            if (armored) return; // aguenta o golpe e continua o ataque (só pisca)
            SetTint(_baseTint);
            SetState(State.Stunned, info.Stun);
            _rb.SetVelocity(info.Knockback * knockbackMultiplier);
            if (info.Source != null) SetFacing(info.Source.transform.position.x >= transform.position.x ? 1 : -1);
        }

        void OnDied()
        {
            SetTint(_baseTint);
            SetState(State.Dead, isTrainingDummy ? dummyRespawnTime : corpseTime);
            if (tintTarget != null && _flash != null) _flash.SetBaseColor(tintTarget, new Color(0.45f, 0.45f, 0.45f, 0.8f));
        }

        void Revive()
        {
            _health.ResetHealth();
            SetTint(_baseTint);
            SetState(State.Patrol);
        }

        void SetState(State s, float duration = 0f)
        {
            _state = s;
            _stateTimer = duration;
        }

        void SetTint(Color c)
        {
            if (tintTarget == null) return;
            if (_flash != null) _flash.SetBaseColor(tintTarget, c);
            else tintTarget.color = c;
        }

        void SetFacing(int dir)
        {
            _facing = dir >= 0 ? 1 : -1;
            if (visual != null)
            {
                var s = visual.localScale;
                s.x = Mathf.Abs(s.x) * _facing;
                visual.localScale = s;
            }
        }

        void UpdateAnimation()
        {
            if (animator == null) return;
            switch (_state)
            {
                case State.Dead: animator.Play("death", 1f, false); break;
                case State.Stunned: animator.Play("hurt", 1f, false); break;
                case State.Windup: animator.ShowFrame("attack1", 0); break;
                case State.Attack:
                case State.Recover: animator.ShowFrame("attack1", 1); break;
                default:
                    bool moving = Mathf.Abs(_rb.GetVelocity().x) > 0.3f;
                    if (moving) animator.Play("run", _state == State.Chase ? 10f : 7f);
                    else animator.Play("idle", 4f);
                    break;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(detectRange * 2f, verticalTolerance * 2f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attack.HitboxCenter(transform.position, Application.isPlaying ? _facing : -1), attack.hitboxSize);
        }
    }
}
