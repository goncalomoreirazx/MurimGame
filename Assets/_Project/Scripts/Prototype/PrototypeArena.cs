using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>
    /// Monta por código o primeiro nível de teste (Floresta Mística ao Luar): fundo com parallax,
    /// chão de pedra, plataformas, o jogador, um boneco de treino e as criaturas
    /// (lobos, jiangshi e um golem). Assim o jogo é jogável numa cena vazia, sem configurar nada no Editor.
    ///
    /// Arranca sozinho ao carregar Play em qualquer cena que ainda não tenha um PlayerController.
    /// Para desligar, mete AutoBuild = false (ou cria a tua própria cena com um jogador).
    /// Se a arte não existir em Resources, usa retângulos de cor no lugar dela.
    /// </summary>
    public class PrototypeArena : MonoBehaviour
    {
        public static bool AutoBuild = true;
        public static PrototypeArena Instance { get; private set; }

        const float GroundTop = 0f;
        const string LevelArt = "Levels/Forest/";
        static readonly Vector2 PlayerSize = new Vector2(0.6f, 1.5f);
        static readonly Vector2 CameraLimits = new Vector2(-12f, 32f);

        static readonly Color GroundColor = new Color(0.23f, 0.2f, 0.22f);
        static readonly Color PlatformColor = new Color(0.36f, 0.27f, 0.22f);
        static readonly Color PlayerColor = new Color(0.35f, 0.65f, 1f);
        static readonly Color SkyColor = new Color(0.09f, 0.1f, 0.16f);
        /// <summary>Escurece um pouco o fundo para os personagens se destacarem.</summary>
        static readonly Color BackdropTint = new Color(0.78f, 0.8f, 0.88f);

        enum BlockKind { Ground, Platform, Invisible }

        readonly List<EnemyAI> _enemies = new List<EnemyAI>();
        Transform _level;
        PlayerController _player;
        PhysicsMaterial2D _noFriction;
        Sprite _groundSprite, _ledgeSprite, _backdropSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (!AutoBuild) return;
            if (PhysicsUtil.FindFirst<PlayerController>() != null) return;
            if (PhysicsUtil.FindFirst<PrototypeArena>() != null) return;
            new GameObject("PrototypeArena").AddComponent<PrototypeArena>();
        }

        void Awake()
        {
            Instance = this;
            _noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            _groundSprite = Resources.Load<Sprite>(LevelArt + "ground");
            _ledgeSprite = Resources.Load<Sprite>(LevelArt + "ledge");
            _backdropSprite = Resources.Load<Sprite>(LevelArt + "backdrop");
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (_player == null) return;
            if (GameInput.RestartPressed && (_player.Health.IsDead || EnemiesLeft == 0))
                Build();
        }

        public int EnemiesLeft
        {
            get
            {
                int n = 0;
                foreach (var e in _enemies)
                    if (e != null && !e.isTrainingDummy && e.CurrentState != EnemyAI.State.Dead) n++;
                return n;
            }
        }

        public PlayerController Player => _player;

        /// <summary>Apaga e volta a montar o nível (usado também para recomeçar).</summary>
        public void Build()
        {
            if (_level != null) Destroy(_level.gameObject);
            _enemies.Clear();
            Time.timeScale = 1f;

            _level = new GameObject("Level").transform;
            _level.SetParent(transform, false);

            EnsureLight();

            // Chão e paredes (as paredes ficam fora do ecrã, só impedem de sair do nível)
            Block("Chão", new Vector2(10f, GroundTop - 2f), new Vector2(64f, 4f), BlockKind.Ground);
            Block("Parede esquerda", new Vector2(-22.5f, 6f), new Vector2(1f, 16f), BlockKind.Invisible);
            Block("Parede direita", new Vector2(42.5f, 6f), new Vector2(1f, 16f), BlockKind.Invisible);

            // Plataformas
            Block("Plataforma 1", new Vector2(6f, 2.0f), new Vector2(4f, 0.5f), BlockKind.Platform);
            Block("Plataforma 2", new Vector2(11.5f, 3.8f), new Vector2(3f, 0.5f), BlockKind.Platform);
            Block("Plataforma 3", new Vector2(20f, 2.5f), new Vector2(6f, 0.5f), BlockKind.Platform);
            Block("Plataforma 4", new Vector2(28f, 4.2f), new Vector2(4f, 0.5f), BlockKind.Platform);
            Block("Degrau", new Vector2(34f, 0.5f), new Vector2(3f, 1f), BlockKind.Platform);

            _player = SpawnPlayer(new Vector2(-4f, GroundTop + PlayerSize.y * 0.5f));

            SpawnEnemy(EnemyType.Dummy, 2f, GroundTop, dummy: true);
            SpawnEnemy(EnemyType.Wolf, 12f, GroundTop);
            SpawnEnemy(EnemyType.Jiangshi, 20f, 2.75f, patrol: 2f);       // em cima da plataforma 3
            SpawnEnemy(EnemyType.Jiangshi, 25f, GroundTop, patrol: 2f);
            SpawnEnemy(EnemyType.Wolf, 29f, GroundTop);
            SpawnEnemy(EnemyType.Golem, 39f, GroundTop, patrol: 1.5f);

            var cam = SetupCamera();
            CreateBackdrop(cam);

            if (GetComponent<PrototypeHUD>() == null) gameObject.AddComponent<PrototypeHUD>();
        }

        // ---------------------------------------------------------------- cenário

        void Block(string name, Vector2 center, Vector2 size, BlockKind kind)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_level, false);
            go.transform.position = center;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = _noFriction;

            switch (kind)
            {
                case BlockKind.Ground:
                    // Faixa de caminho + muro de pedra; o caminho fica um pouco acima do topo do collider,
                    // para os pés assentarem no meio do caminho.
                    if (!TiledVisual(go.transform, _groundSprite, size, size.y + 0.26f, 0.26f, 0))
                        ColorVisual(go.transform, size, GroundColor);
                    break;
                case BlockKind.Platform:
                    if (!TiledVisual(go.transform, _ledgeSprite, size, size.y + 0.25f, 0.1f, 1))
                        ColorVisual(go.transform, size, PlatformColor);
                    break;
            }
        }

        void ColorVisual(Transform parent, Vector2 size, Color color)
        {
            PrototypeSprites.CreateRect(parent, "Visual", Vector2.zero, size, color, 0);
            // Topo mais claro, para se ver bem onde se pisa
            PrototypeSprites.CreateRect(parent, "Topo", new Vector2(0f, size.y * 0.5f - 0.05f),
                                        new Vector2(size.x, 0.1f), color * 1.5f, 1);
        }

        /// <summary>
        /// Repete um sprite na horizontal para cobrir um bloco. A altura visual pode ser maior que o collider
        /// (topOverhang = quanto o desenho sobe acima do topo do collider).
        /// </summary>
        bool TiledVisual(Transform parent, Sprite sprite, Vector2 size, float visualHeight, float topOverhang, int order)
        {
            if (sprite == null) return false;
            var go = new GameObject("Visual");
            go.transform.SetParent(parent, false);

            float scale = visualHeight / sprite.bounds.size.y;
            float top = size.y * 0.5f + topOverhang;
            go.transform.localPosition = new Vector3(0f, top - visualHeight * 0.5f, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(size.x / scale, sprite.bounds.size.y);
            sr.sortingOrder = order;
            return true;
        }

        void CreateBackdrop(Camera cam)
        {
            cam.backgroundColor = SkyColor;
            if (_backdropSprite == null) return;

            var go = new GameObject("Fundo - Floresta Mística");
            go.transform.SetParent(_level, false);

            // Alto o suficiente para cobrir o ecrã mesmo com o jogador em cima das plataformas.
            const float height = 15f;
            const float bottom = GroundTop - 0.4f; // escondido atrás do chão
            float scale = height / _backdropSprite.bounds.size.y;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var parallax = go.AddComponent<ParallaxLayer>();
            parallax.factor = new Vector2(0.85f, 0f);

            // Posição inicial escolhida para o fundo ficar centrado quando a câmara está a meio do nível.
            float middle = (CameraLimits.x + CameraLimits.y) * 0.5f;
            float camX = cam.transform.position.x;
            float startX = middle - parallax.factor.x * (middle - camX);
            go.transform.position = new Vector3(startX, bottom + height * 0.5f, 0f);
            parallax.Init(cam.transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _backdropSprite;
            sr.color = BackdropTint;
            sr.sortingOrder = -100;
        }

        // ---------------------------------------------------------------- personagens

        /// <summary>Cria a base comum de um personagem (desativado, para os componentes serem configurados antes do Awake).</summary>
        GameObject CharacterBase(string name, Vector2 position, Vector2 size, int maxHealth, Team team)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(_level, false);
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.sharedMaterial = _noFriction;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = _noFriction;

            var health = go.AddComponent<Health>();
            health.team = team;
            health.maxHealth = maxHealth;
            return go;
        }

        /// <summary>Cria o visual: sprites da pasta Resources/Characters/&lt;nome&gt; se existirem, senão um retângulo.</summary>
        Transform CreateVisual(GameObject owner, string spriteSet, Vector2 size, Color fallbackColor, int order,
                               out SpriteRenderer body, out SpriteAnimator animator)
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(owner.transform, false);

            var sprites = CharacterSprites.Load(spriteSet);
            if (sprites != null)
            {
                var go = new GameObject("Sprite");
                go.transform.SetParent(visual, false);
                go.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f); // pivot nos pés
                body = go.AddComponent<SpriteRenderer>();
                body.sortingOrder = order;
                var idle = sprites.Get("idle");
                if (idle != null && idle.Length > 0) body.sprite = idle[0];

                animator = owner.AddComponent<SpriteAnimator>();
                animator.target = body;
                animator.sprites = sprites;
            }
            else
            {
                body = PrototypeSprites.CreateRect(visual, "Corpo", Vector2.zero, size, fallbackColor, order);
                // "Olho" para se perceber para onde está virado
                PrototypeSprites.CreateRect(visual, "Olho", new Vector2(size.x * 0.25f, size.y * 0.3f), new Vector2(0.15f, 0.15f),
                                            new Color(0.1f, 0.1f, 0.15f), order + 1);
                animator = null;
            }
            return visual;
        }

        PlayerController SpawnPlayer(Vector2 position)
        {
            var go = CharacterBase("Jogador", position, PlayerSize, 6, Team.Player);
            var visual = CreateVisual(go, "Blue", PlayerSize, PlayerColor, 10, out _, out var animator);

            var flash = go.AddComponent<HitFlash>();
            flash.visualRoot = visual;

            var controller = go.AddComponent<PlayerController>();
            controller.visual = visual;
            var combat = go.AddComponent<PlayerCombat>();

            if (animator != null)
            {
                var anim = go.AddComponent<PlayerAnimation>();
                anim.animator = animator;
            }
            else
            {
                // Sem sprites: mostra a hitbox do golpe como um retângulo semitransparente.
                var slash = PrototypeSprites.CreateRect(go.transform, "Golpe", Vector2.zero, Vector2.one,
                                                        new Color(0.7f, 0.95f, 1f, 0.55f), 15);
                combat.slashVisual = slash;
            }

            go.SetActive(true);
            return controller;
        }

        /// <summary>Cria um inimigo com os pés em (x, groundY).</summary>
        void SpawnEnemy(EnemyType type, float x, float groundY, bool dummy = false, float patrol = 3f)
        {
            var size = type.Size;
            var go = CharacterBase(type.Name, new Vector2(x, groundY + size.y * 0.5f), size, type.MaxHealth, Team.Enemy);
            var visual = CreateVisual(go, type.SpriteSet, size, type.FallbackColor, 5, out var body, out var animator);
            if (animator != null && dummy) body.color = new Color(0.75f, 0.75f, 0.75f);

            var flash = go.AddComponent<HitFlash>();
            flash.visualRoot = visual;
            flash.blinkWhileInvulnerable = false;

            var ai = go.AddComponent<EnemyAI>();
            ai.isTrainingDummy = dummy;
            ai.patrolDistance = patrol;
            ai.patrolSpeed = type.PatrolSpeed;
            ai.chaseSpeed = type.ChaseSpeed;
            ai.detectRange = type.DetectRange;
            ai.loseRange = type.DetectRange + 4f;
            ai.verticalTolerance = Mathf.Max(2.5f, size.y + 0.5f);
            ai.attackRange = type.AttackRange;
            ai.windupTime = type.WindupTime;
            ai.attackCooldown = type.AttackCooldown;
            ai.superArmor = type.SuperArmor;
            ai.knockbackMultiplier = type.KnockbackMultiplier;
            if (type.Attack != null) ai.attack = type.Attack;
            ai.visual = visual;
            ai.tintTarget = body;
            ai.animator = animator;

            // Barra de vida
            var barRoot = new GameObject("Barra de vida").transform;
            barRoot.SetParent(go.transform, false);
            barRoot.localPosition = new Vector3(0f, size.y * 0.5f + 0.35f, 0f);
            var bar = go.AddComponent<WorldHealthBar>();
            bar.width = Mathf.Max(0.9f, size.x);
            bar.health = go.GetComponent<Health>();
            bar.background = PrototypeSprites.CreateRect(barRoot, "Fundo", Vector2.zero, new Vector2(bar.width + 0.06f, 0.14f),
                                                         new Color(0f, 0f, 0f, 0.7f), 20);
            bar.fill = PrototypeSprites.CreateRect(barRoot, "Vida", Vector2.zero, new Vector2(bar.width, 0.08f),
                                                   new Color(0.9f, 0.2f, 0.25f), 21);

            go.SetActive(true);
            _enemies.Add(ai);
        }

        Camera SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.transform.position = new Vector3(0f, 0f, -10f);
            }
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            var p = cam.transform.position;
            if (p.z > -1f) cam.transform.position = new Vector3(p.x, p.y, -10f);

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = _player.transform;
            follow.xLimits = CameraLimits;
            follow.SnapToTarget();
            return cam;
        }

        /// <summary>
        /// No URP 2D os sprites precisam de uma luz 2D para não ficarem pretos. Se o projeto usar URP
        /// e a cena não tiver nenhuma, cria uma luz global. (Por reflexão, para compilar também sem URP.)
        /// </summary>
        void EnsureLight()
        {
            var type = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) return;
#if UNITY_2022_2_OR_NEWER
            if (FindFirstObjectByType(type) != null) return;
#else
            if (FindObjectOfType(type) != null) return;
#endif
            // Fica fora do "Level" para não ser apagada ao recomeçar.
            var go = new GameObject("Global Light 2D");
            go.transform.SetParent(transform, false);
            var light = go.AddComponent(type);
            var prop = type.GetProperty("lightType");
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(light, Enum.Parse(prop.PropertyType, "Global")); }
                catch (Exception e) { Debug.LogWarning("Não foi possível criar a luz global 2D: " + e.Message); }
            }
        }
    }
}
