using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Combate corpo a corpo do jogador: combo de 3 golpes com hitbox, buffer de input,
    /// hitstop e tremor de câmara ao acertar. O dash cancela a recuperação de um golpe.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        enum Phase { None, Startup, Active, Recovery }

        public AttackData[] combo =
        {
            new AttackData { name = "Corte 1", damage = 1, startup = 0.05f, active = 0.08f, recovery = 0.16f,
                             hitboxOffset = new Vector2(0.9f, 0.1f), hitboxSize = new Vector2(1.5f, 1.1f),
                             knockback = new Vector2(3f, 2f), stun = 0.3f, hitstop = 0.04f, shake = 0.04f, lungeSpeed = 3f },
            new AttackData { name = "Corte 2", damage = 1, startup = 0.05f, active = 0.08f, recovery = 0.18f,
                             hitboxOffset = new Vector2(0.9f, 0.1f), hitboxSize = new Vector2(1.5f, 1.1f),
                             knockback = new Vector2(3f, 2f), stun = 0.3f, hitstop = 0.04f, shake = 0.04f, lungeSpeed = 3f },
            new AttackData { name = "Estocada", damage = 2, startup = 0.1f, active = 0.1f, recovery = 0.3f,
                             hitboxOffset = new Vector2(1.1f, 0.1f), hitboxSize = new Vector2(1.9f, 0.9f),
                             knockback = new Vector2(9f, 5f), stun = 0.5f, hitstop = 0.09f, shake = 0.12f, lungeSpeed = 7f, lungeTime = 0.1f },
        };

        [Tooltip("Tempo depois de um golpe acabar em que o próximo ainda continua o combo.")]
        public float comboWindow = 0.3f;
        [Tooltip("Tempo em que um ataque carregado cedo demais fica guardado.")]
        public float inputBuffer = 0.15f;
        [Tooltip("Fração da velocidade de corrida permitida durante um golpe no chão.")]
        [Range(0f, 1f)] public float groundMoveDuringAttack = 0.1f;
        [Range(0f, 1f)] public float airMoveDuringAttack = 0.7f;

        [Tooltip("Visual opcional que aparece enquanto a hitbox está ativa.")]
        public SpriteRenderer slashVisual;

        public bool IsAttacking => _phase != Phase.None;
        /// <summary>O dash só cancela o golpe depois de este ter acertado (fase de recuperação) ou fora de golpes.</summary>
        public bool AllowsDashCancel => _phase == Phase.None || _phase == Phase.Recovery;
        public bool AllowsJump => _phase == Phase.None || _phase == Phase.Recovery;
        public int ComboIndex => _comboIndex;
        /// <summary>True enquanto o golpe ainda está a preparar (antes da hitbox ficar ativa).</summary>
        public bool InStartup => _phase == Phase.Startup;

        PlayerController _controller;
        Health _health;
        Phase _phase;
        float _phaseTimer;
        int _comboIndex;
        int _nextComboIndex;
        float _lastAttackEnd = -10f;
        float _bufferTimer;

        readonly HashSet<Health> _hitThisSwing = new HashSet<Health>();
        readonly List<Health> _buffer = new List<Health>();

        AttackData Current => combo[_comboIndex];

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<Health>();
            if (slashVisual != null) slashVisual.enabled = false;
        }

        public float MoveMultiplier(bool grounded) =>
            IsAttacking && _phase != Phase.Recovery ? (grounded ? groundMoveDuringAttack : airMoveDuringAttack) : 1f;

        void Update()
        {
            if (combo == null || combo.Length == 0) return;

            if (GameInput.AttackPressed) _bufferTimer = inputBuffer;
            else _bufferTimer -= Time.deltaTime;

            if (_health.IsDead)
            {
                CancelAttack();
                return;
            }

            if (_phase == Phase.None)
            {
                if (Time.time - _lastAttackEnd > comboWindow) _nextComboIndex = 0;
                if (_bufferTimer > 0f && _controller.CanAct) StartAttack(_nextComboIndex);
                return;
            }

            _phaseTimer += Time.deltaTime;

            switch (_phase)
            {
                case Phase.Startup:
                    if (_phaseTimer >= Current.startup) EnterActive();
                    break;

                case Phase.Active:
                    DoHits();
                    if (_phaseTimer >= Current.active) EnterRecovery();
                    break;

                case Phase.Recovery:
                    // Encadear: carregar no ataque durante a recuperação passa ao golpe seguinte.
                    if (_bufferTimer > 0f && _nextComboIndex > 0)
                    {
                        StartAttack(_nextComboIndex);
                    }
                    else if (_phaseTimer >= Current.recovery)
                    {
                        _phase = Phase.None;
                        _lastAttackEnd = Time.time;
                    }
                    break;
            }
        }

        void StartAttack(int index)
        {
            _bufferTimer = 0f;
            _comboIndex = Mathf.Clamp(index, 0, combo.Length - 1);
            _nextComboIndex = (_comboIndex + 1) % combo.Length;
            _phase = Phase.Startup;
            _phaseTimer = 0f;
            _hitThisSwing.Clear();

            // Permite virar no início do golpe
            float x = GameInput.MoveX;
            if (Mathf.Abs(x) > 0.1f) _controller.SetFacing(x > 0f ? 1 : -1);

            if (Current.startup <= 0f) EnterActive();
        }

        void EnterActive()
        {
            _phase = Phase.Active;
            _phaseTimer = 0f;
            _controller.Lunge(Current.lungeSpeed, Current.lungeTime);
            _controller.HoverInAir();
            ShowSlash(true);
            DoHits();
        }

        void EnterRecovery()
        {
            _phase = Phase.Recovery;
            _phaseTimer = 0f;
            ShowSlash(false);
        }

        void DoHits()
        {
            var attack = Current;
            int hits = attack.ApplyHits(transform.position, _controller.Facing, _health.team, gameObject, _hitThisSwing, _buffer);
            if (hits > 0)
            {
                HitStop.Do(attack.hitstop);
                CameraFollow.Shake(attack.shake, 0.1f);
            }
        }

        void ShowSlash(bool show)
        {
            if (slashVisual == null) return;
            slashVisual.enabled = show;
            if (!show) return;

            var attack = Current;
            var t = slashVisual.transform;
            // O visual é filho do jogador (não do objeto espelhado), por isso aplicamos a direção aqui.
            t.localPosition = new Vector3(attack.hitboxOffset.x * _controller.Facing, attack.hitboxOffset.y, 0f);
            t.localScale = new Vector3(attack.hitboxSize.x, attack.hitboxSize.y, 1f);
        }

        public void CancelAttack()
        {
            if (_phase == Phase.None) return;
            _phase = Phase.None;
            _lastAttackEnd = Time.time;
            ShowSlash(false);
        }

        void OnDrawGizmos()
        {
            if (combo == null || combo.Length == 0) return;
            var c = GetComponent<PlayerController>();
            int facing = c != null && Application.isPlaying ? c.Facing : 1;
            var attack = combo[Mathf.Clamp(_comboIndex, 0, combo.Length - 1)];
            Gizmos.color = _phase == Phase.Active ? new Color(1f, 0.2f, 0.2f, 0.9f) : new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(attack.HitboxCenter(transform.position, facing), attack.hitboxSize);
        }
    }
}
