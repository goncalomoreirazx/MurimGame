using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>Receita de um tipo de inimigo do protótipo: sprites, tamanho, vida e ataque.</summary>
    public class EnemyType
    {
        public string Name;
        /// <summary>Pasta em Resources/Characters.</summary>
        public string SpriteSet;
        public Vector2 Size = new Vector2(0.7f, 1.5f);
        public Color FallbackColor = new Color(0.9f, 0.3f, 0.35f);
        public int MaxHealth = 5;
        public float PatrolSpeed = 1.8f;
        public float ChaseSpeed = 3.6f;
        public float DetectRange = 7f;
        public float AttackRange = 1.3f;
        public float WindupTime = 0.45f;
        public float AttackCooldown = 0.9f;
        public bool SuperArmor;
        public float KnockbackMultiplier = 1f;
        public AttackData Attack;

        /// <summary>Lobo demoníaco: rápido, pouca vida, investida curta.</summary>
        public static EnemyType Wolf => new EnemyType
        {
            Name = "Lobo", SpriteSet = "Wolf", Size = new Vector2(0.8f, 1.5f),
            MaxHealth = 4, PatrolSpeed = 2.4f, ChaseSpeed = 5.2f, DetectRange = 8f,
            AttackRange = 1.6f, WindupTime = 0.35f, AttackCooldown = 1.0f,
            Attack = new AttackData
            {
                name = "Investida", damage = 1, startup = 0f, active = 0.15f, recovery = 0.55f,
                hitboxOffset = new Vector2(0.7f, 0f), hitboxSize = new Vector2(1.2f, 1.0f),
                knockback = new Vector2(7f, 4f), stun = 0.35f, hitstop = 0.05f, lungeSpeed = 9f
            }
        };

        /// <summary>Jiangshi (vampiro saltitante): lento, golpe de energia com bom alcance.</summary>
        public static EnemyType Jiangshi => new EnemyType
        {
            Name = "Jiangshi", SpriteSet = "Jiangshi", Size = new Vector2(0.7f, 1.9f),
            MaxHealth = 5, PatrolSpeed = 1.2f, ChaseSpeed = 2.4f, DetectRange = 7f,
            AttackRange = 1.9f, WindupTime = 0.55f, AttackCooldown = 1.2f,
            Attack = new AttackData
            {
                name = "Garra espiritual", damage = 1, startup = 0f, active = 0.15f, recovery = 0.6f,
                hitboxOffset = new Vector2(1.0f, 0.2f), hitboxSize = new Vector2(1.9f, 1.6f),
                knockback = new Vector2(6f, 4f), stun = 0.4f, hitstop = 0.06f, lungeSpeed = 2f
            }
        };

        /// <summary>Golem de madeira: muito resistente, lento, não é interrompido durante o ataque.</summary>
        public static EnemyType Golem => new EnemyType
        {
            Name = "Golem", SpriteSet = "Golem", Size = new Vector2(1.4f, 2.6f),
            FallbackColor = new Color(0.35f, 0.6f, 0.3f),
            MaxHealth = 14, PatrolSpeed = 0.9f, ChaseSpeed = 1.6f, DetectRange = 8f,
            AttackRange = 2.2f, WindupTime = 0.8f, AttackCooldown = 1.6f,
            SuperArmor = true, KnockbackMultiplier = 0.15f,
            Attack = new AttackData
            {
                name = "Esmagar", damage = 2, startup = 0f, active = 0.18f, recovery = 0.9f,
                hitboxOffset = new Vector2(1.2f, -0.6f), hitboxSize = new Vector2(2.8f, 1.4f),
                knockback = new Vector2(10f, 8f), stun = 0.5f, hitstop = 0.1f, shake = 0.2f, lungeSpeed = 0f
            }
        };

        /// <summary>Boneco de treino (a espadachim vermelha, parada).</summary>
        public static EnemyType Dummy => new EnemyType
        {
            Name = "Boneco de treino", SpriteSet = "Red", Size = new Vector2(0.6f, 1.5f),
            FallbackColor = new Color(0.75f, 0.7f, 0.55f), MaxHealth = 8,
            Attack = new AttackData()
        };
    }
}
