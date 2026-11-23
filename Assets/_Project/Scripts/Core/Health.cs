using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior
{
    public enum Team { Player, Enemy }

    /// <summary>Informação de um golpe: quanto tira, para onde empurra e quanto tempo atordoa.</summary>
    public struct DamageInfo
    {
        public int Amount;
        public Vector2 Knockback;
        public float Stun;
        public GameObject Source;
    }

    /// <summary>
    /// Vida de qualquer personagem. Os personagens não colidem fisicamente entre si
    /// (para não se empurrarem nem servirem de plataforma); só os golpes os afetam.
    /// </summary>
    public class Health : MonoBehaviour
    {
        public Team team = Team.Enemy;
        public int maxHealth = 5;
        [Tooltip("Segundos de invencibilidade depois de levar um golpe.")]
        public float invulnerableTime = 0.1f;

        /// <summary>Invencibilidade controlada por outro sistema (por exemplo, durante o dash).</summary>
        [NonSerialized] public bool ExtraInvulnerable;

        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        public bool IsInvulnerable => ExtraInvulnerable || Time.time < _invulnerableUntil;
        public float Normalized => maxHealth > 0 ? (float)Current / maxHealth : 0f;

        public event Action<DamageInfo> Damaged;
        public event Action Died;

        static readonly List<Collider2D> CharacterColliders = new List<Collider2D>();

        float _invulnerableUntil;
        Collider2D _collider;

        void Awake()
        {
            Current = maxHealth;
            _collider = GetComponent<Collider2D>();
            if (_collider != null)
            {
                CharacterColliders.RemoveAll(c => c == null);
                foreach (var other in CharacterColliders)
                    Physics2D.IgnoreCollision(_collider, other, true);
                CharacterColliders.Add(_collider);
            }
        }

        void OnDestroy()
        {
            if (_collider != null) CharacterColliders.Remove(_collider);
        }

        /// <summary>Aplica dano. Devolve true se o golpe contou.</summary>
        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || IsInvulnerable || info.Amount <= 0) return false;

            Current = Mathf.Max(0, Current - info.Amount);
            _invulnerableUntil = Time.time + invulnerableTime;
            Damaged?.Invoke(info);
            if (Current == 0) Died?.Invoke();
            return true;
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            _invulnerableUntil = 0f;
        }
    }
}
