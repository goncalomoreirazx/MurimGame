using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>Pequenas ajudas de física 2D partilhadas pelo jogador e pelos inimigos.</summary>
    public static class PhysicsUtil
    {
        static readonly List<Collider2D> Results = new List<Collider2D>(16);

        public static Vector2 GetVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        /// <summary>
        /// Devolve true se existir chão ou parede sólida dentro da caixa.
        /// Ignora triggers, o próprio collider e personagens (tudo o que tem Health).
        /// </summary>
        public static bool IsSolid(Vector2 center, Vector2 size, Collider2D self) => FindSolid(center, size, self) != null;

        /// <summary>Como IsSolid, mas devolve o collider encontrado (ou null).</summary>
        public static Collider2D FindSolid(Vector2 center, Vector2 size, Collider2D self)
        {
            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            Results.Clear();
            Physics2D.OverlapBox(center, size, 0f, filter, Results);
            foreach (var c in Results)
            {
                if (c == null || c == self) continue;
                if (c.GetComponentInParent<Health>() != null) continue;
                return c;
            }
            return null;
        }

        /// <summary>Devolve todos os Health dentro da caixa (cada um só uma vez).</summary>
        public static void FindHealthInBox(Vector2 center, Vector2 size, List<Health> output)
        {
            output.Clear();
            var filter = new ContactFilter2D();
            filter.useTriggers = true;
            Results.Clear();
            Physics2D.OverlapBox(center, size, 0f, filter, Results);
            foreach (var c in Results)
            {
                if (c == null) continue;
                var h = c.GetComponentInParent<Health>();
                if (h != null && !output.Contains(h)) output.Add(h);
            }
        }

        public static T FindFirst<T>() where T : Object
        {
#if UNITY_2022_2_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }
    }
}
