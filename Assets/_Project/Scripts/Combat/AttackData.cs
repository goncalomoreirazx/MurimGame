using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Dados de um golpe corpo a corpo. Cada golpe tem três fases:
    /// arranque (startup), ativo (a hitbox acerta) e recuperação (recovery).
    /// </summary>
    [Serializable]
    public class AttackData
    {
        public string name = "Golpe";
        public int damage = 1;

        [Header("Tempos (segundos)")]
        public float startup = 0.06f;
        public float active = 0.08f;
        public float recovery = 0.18f;

        [Header("Hitbox (relativa ao personagem, virada para a frente)")]
        public Vector2 hitboxOffset = new Vector2(0.9f, 0f);
        public Vector2 hitboxSize = new Vector2(1.4f, 1.0f);

        [Header("Impacto")]
        public Vector2 knockback = new Vector2(5f, 3f);
        public float stun = 0.3f;
        public float hitstop = 0.05f;
        public float shake = 0.06f;

        [Header("Movimento durante o golpe")]
        [Tooltip("Velocidade do passo em frente quando o golpe fica ativo.")]
        public float lungeSpeed = 4f;
        public float lungeTime = 0.08f;

        public float TotalTime => startup + active + recovery;

        public Vector2 HitboxCenter(Vector2 origin, int facing) =>
            origin + new Vector2(hitboxOffset.x * facing, hitboxOffset.y);

        /// <summary>
        /// Aplica o golpe a todos os alvos da equipa contrária dentro da hitbox.
        /// "alreadyHit" garante que cada alvo só leva um golpe por ataque.
        /// Devolve quantos alvos foram atingidos agora.
        /// </summary>
        public int ApplyHits(Vector2 origin, int facing, Team attackerTeam, GameObject source,
                             HashSet<Health> alreadyHit, List<Health> buffer)
        {
            PhysicsUtil.FindHealthInBox(HitboxCenter(origin, facing), hitboxSize, buffer);
            int hits = 0;
            foreach (var target in buffer)
            {
                if (target.team == attackerTeam || target.IsDead || alreadyHit.Contains(target)) continue;
                alreadyHit.Add(target);

                var info = new DamageInfo
                {
                    Amount = damage,
                    Knockback = new Vector2(knockback.x * facing, knockback.y),
                    Stun = stun,
                    Source = source
                };
                if (target.TakeDamage(info)) hits++;
            }
            return hits;
        }
    }
}
