using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>
    /// Monta por código o nível 1 (Floresta Mística ao Luar) como um nível de plataformas clássico:
    /// portão de início à esquerda, portal da meta à direita, buracos, plataformas (algumas móveis),
    /// orbes de Qi para apanhar, lanternas de checkpoint e inimigos espalhados, com o golem perto do fim.
    ///
    /// Arranca sozinho ao carregar Play em qualquer cena que ainda não tenha um PlayerController.
    /// Para desligar, mete AutoBuild = false (ou cria a tua própria cena com um jogador).
    /// Se a arte não existir em Resources, usa retângulos de cor no lugar dela.
    ///
    /// O desenho do nível está todo em BuildLayout(): é aí que se mudam buracos, plataformas e inimigos.
    /// </summary>
    public class PrototypeArena : MonoBehaviour
    {
        public static bool AutoBuild = true;
        public static PrototypeArena Instance { get; private set; }

        public enum Outcome { Playing, Won, GameOver }

        const string LevelArt = "Levels/Forest/";
        const string PropArt = "Props/";

        // Tamanho dos adereços no mundo (unidades) e medidas da abertura do portão na imagem (px).
        const float GateHeight = 5.6f, GateSink = 0.3f;
        const float GateOpeningLeft = 290f, GateOpeningRight = 530f, GateOpeningBottom = 78f, GateOpeningTop = 518f;
        const float LanternHeight = 2.0f;
        const float OrbHeight = 0.85f;
        const float GroundBottom = -8f;   // fundo dos blocos de chão (bem abaixo do ecrã)
        const float KillY = -7f;          // abaixo disto, caiu num buraco
        const int FallDamage = 2;

        static readonly Vector2 PlayerSize = new Vector2(0.6f, 1.5f);

        static readonly Color GroundColor = new Color(0.23f, 0.2f, 0.22f);
        static readonly Color GroundDeepColor = new Color(0.06f, 0.07f, 0.11f);
        static readonly Color PlatformColor = new Color(0.36f, 0.27f, 0.22f);
        static readonly Color PlayerColor = new Color(0.35f, 0.65f, 1f);
        static readonly Color SkyColor = new Color(0.05f, 0.06f, 0.11f);
        /// <summary>Escurece um pouco o fundo para os personagens se destacarem.</summary>
        static readonly Color BackdropTint = new Color(0.78f, 0.8f, 0.88f);
        static readonly Color ToriiRed = new Color(0.62f, 0.12f, 0.1f);
        static readonly Color ToriiDark = new Color(0.18f, 0.08f, 0.08f);
        /// <summary>Escurece as plataformas e a relva do chão para combinarem com o mapa noturno.</summary>
        static readonly Color PlatformTint = new Color(0.6f, 0.65f, 0.78f);
        /// <summary>Pedras do corpo do chão: mais escuras do que o topo, para o caminho se destacar.</summary>
        static readonly Color GroundBodyTint = new Color(0.3f, 0.32f, 0.43f);

        // Imagem das plataformas (Levels/Forest/platform.png): pontas que não se repetem e altura da relva.
        const float PlatformEndPixels = 30f;   // largura de cada ponta
        const float PlatformWalkPixels = 42f;  // da parte de cima da imagem até onde se pisa
        const float PlatformVisualHeight = 1.2f;

        enum BlockKind { Ground, Platform, Invisible }

        // ---------------------------------------------------------------- estado do nível

        readonly List<EnemyAI> _enemies = new List<EnemyAI>();
        readonly List<QiOrb> _orbs = new List<QiOrb>();
        readonly List<Checkpoint> _checkpoints = new List<Checkpoint>();
        readonly List<Vector2> _checkpointSpawns = new List<Vector2>();

        Transform _level;
        PlayerController _player;
        PhysicsMaterial2D _noFriction;
        Sprite _groundSprite, _ledgeSprite, _backdropSprite, _platformSprite, _groundBodySprite;
        LevelDecor _decor;
        Sprite[] _gateSprite, _lanternOff, _lanternOn, _orbFrames, _sparkleFrames;

        Vector2 _startSpawn;
        float _goalX;
        float _goalGroundY;
        Vector2 _cameraLimits;

        /// <summary>Checkpoint ativo (-1 = início). Mantém-se ao recomeçar depois de morrer.</summary>
        int _checkpointIndex = -1;
        float _startTime;
        float _endTime;

        public Outcome State { get; private set; }
        public PlayerController Player => _player;
        public int OrbsCollected { get; private set; }
        public int OrbsTotal => _orbs.Count;
        public int EnemiesDefeated { get; private set; }
        public int EnemiesTotal { get; private set; }
        public int Falls { get; private set; }
        public float ElapsedTime => (State == Outcome.Playing ? Time.time : _endTime) - _startTime;
        public float LevelStartX => _startSpawn.x;
        public float LevelGoalX => _goalX;
        public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints;
        public string LevelName => "Floresta Mística ao Luar";

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
            _groundSprite = SpriteLoader.LoadCentered(LevelArt + "ground");
            _ledgeSprite = SpriteLoader.LoadCentered(LevelArt + "ledge");
            _backdropSprite = SpriteLoader.LoadCentered(LevelArt + "backdrop");
            LoadPlatformSprites();
            _groundBodySprite = SpriteLoader.LoadCentered(LevelArt + "ground_body");
            _decor = new LevelDecor();

            var bottom = new Vector2(0.5f, 0f);
            var center = new Vector2(0.5f, 0.5f);
            _gateSprite = SpriteLoader.LoadFrames(PropArt + "Gate", "gate", bottom);
            _lanternOff = SpriteLoader.LoadFrames(PropArt + "Lantern", "off_", bottom);
            _lanternOn = SpriteLoader.LoadFrames(PropArt + "Lantern", "on_", bottom);
            _orbFrames = SpriteLoader.LoadFrames(PropArt + "Orb", "orb_", center);
            _sparkleFrames = SpriteLoader.LoadFrames(PropArt + "Sparkle", "sparkle_", center);
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- regras do jogo

        void Update()
        {
            if (_player == null) return;

            if (GameInput.RestartPressed && State != Outcome.Playing)
            {
                if (State == Outcome.Won) _checkpointIndex = -1; // ganhou: recomeça do início
                Build();
                return;
            }

            if (State != Outcome.Playing) return;

            Vector2 p = _player.transform.position;

            // Orbes de Qi
            foreach (var orb in _orbs)
                if (orb != null && orb.TryCollect(p)) OrbsCollected++;

            // Checkpoints (acendem ao passar por eles)
            for (int i = 0; i < _checkpoints.Count; i++)
            {
                if (i <= _checkpointIndex || p.x < _checkpoints[i].transform.position.x) continue;
                _checkpoints[i].Activate();
                _checkpointIndex = i;
            }

            // Inimigos que caem nos buracos desaparecem
            foreach (var e in _enemies)
                if (e != null && e.transform.position.y < KillY) Destroy(e.gameObject);

            // Caiu num buraco
            if (p.y < KillY && !_player.Health.IsDead)
            {
                Falls++;
                _player.Health.LoseHealth(FallDamage);
                if (!_player.Health.IsDead)
                {
                    _player.Teleport(CurrentSpawn());
                    _player.Health.GrantInvulnerability(1.5f);
                    var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
                    if (follow != null) follow.SnapToTarget();
                }
            }

            if (_player.Health.IsDead)
            {
                State = Outcome.GameOver;
                _endTime = Time.time;
                return;
            }

            // Chegou ao portal
            if (p.x >= _goalX - 0.3f && Mathf.Abs(p.y - (_goalGroundY + PlayerSize.y * 0.5f)) < 3f)
            {
                State = Outcome.Won;
                _endTime = Time.time;
                _player.ControlsEnabled = false;
                CameraFollow.Shake(0.08f, 0.25f);
            }
        }

        Vector2 CurrentSpawn() =>
            _checkpointIndex >= 0 && _checkpointIndex < _checkpointSpawns.Count ? _checkpointSpawns[_checkpointIndex] : _startSpawn;

        /// <summary>Apaga e volta a montar o nível (usado também para recomeçar).</summary>
        public void Build()
        {
            if (_level != null) Destroy(_level.gameObject);
            _enemies.Clear();
            _orbs.Clear();
            _checkpoints.Clear();
            _checkpointSpawns.Clear();
            Time.timeScale = 1f;
            State = Outcome.Playing;
            OrbsCollected = 0;
            EnemiesDefeated = 0;
            EnemiesTotal = 0;
            Falls = 0;
            _startTime = Time.time;

            _level = new GameObject("Level").transform;
            _level.SetParent(transform, false);

            EnsureLight();
            _decor.Begin(_level);
            BuildLayout();
            Decorate();

            // Recomeçar depois de morrer: volta ao último checkpoint, que já fica aceso.
            for (int i = 0; i <= _checkpointIndex && i < _checkpoints.Count; i++) _checkpoints[i].Activate();
            if (_checkpointIndex >= 0) _player.Teleport(CurrentSpawn());

            var cam = SetupCamera();
            CreateBackdrop(cam);

            if (GetComponent<PrototypeHUD>() == null) gameObject.AddComponent<PrototypeHUD>();

            Debug.Log($"[Heavenly Warrior] Nível montado: jogador em {(Vector2)_player.transform.position}, " +
                      $"câmara em {(Vector2)cam.transform.position} (tamanho {cam.orthographicSize}), " +
                      $"{EnemiesTotal} inimigos, {_orbs.Count} orbes.");
        }

        // ---------------------------------------------------------------- desenho do nível

        /// <summary>
        /// O nível, da esquerda para a direita. Unidades: 1 = 100 px dos sprites; o jogador tem 1,5 de altura,
        /// salta cerca de 3,3 (5,8 com o salto duplo) e atravessa buracos de ~7 com um salto em corrida.
        /// </summary>
        void BuildLayout()
        {
            const float left = -10f, right = 201f;
            _cameraLimits = new Vector2(0f, 191f);

            // Limites do nível (invisíveis)
            Block("Parede esquerda", new Vector2(left - 0.5f, 10f), new Vector2(1f, 40f), BlockKind.Invisible);
            Block("Parede direita", new Vector2(right + 0.5f, 10f), new Vector2(1f, 40f), BlockKind.Invisible);

            // 1. Início: portão, boneco de treino para aquecer
            Ground(left, 22f, 0f);
            Torii(0f, 0f, portal: false); // o jogador começa à porta
            _startSpawn = new Vector2(0f, PlayerSize.y * 0.5f);
            Enemy(EnemyType.Dummy, 10f, 0f, dummy: true);
            OrbLine(4f, 1.2f, 8f, 1.2f, 4);

            // 2. Primeiro buraco (4 de largura) e o primeiro lobo
            OrbArc(22f, 26f, 1.5f, 2f, 5);
            Ground(26f, 44f, 0f);
            Enemy(EnemyType.Wolf, 36f, 0f);

            // 3. Degrau para cima, com uma plataforma por cima e um jiangshi
            Ground(44f, 60f, 1.5f);
            Platform(52f, 4.5f, 3f);
            OrbLine(51f, 5.4f, 53f, 5.4f, 3);
            Enemy(EnemyType.Jiangshi, 55f, 1.5f, patrol: 2.5f);

            // 4. Buraco grande com duas plataformas pelo meio
            Platform(64f, 2.5f, 3f);
            Platform(69.5f, 3.5f, 3f);
            OrbArc(62.5f, 65.5f, 3.3f, 1f, 3);
            OrbArc(68f, 71f, 4.3f, 1f, 3);
            Ground(74f, 96f, 0f);
            Checkpoint(78f, 0f);
            Enemy(EnemyType.Wolf, 84f, 0f);
            Enemy(EnemyType.Jiangshi, 91f, 0f, patrol: 2f);

            // 5. Buraco com plataforma móvel
            MovingPlatformAt(99f, 109f, 1.5f, 3f, 2.8f);
            OrbLine(100f, 2.6f, 108f, 2.6f, 5);
            Ground(112f, 132f, 0.5f);
            Enemy(EnemyType.Wolf, 118f, 0.5f);
            Platform(126f, 3.75f, 5f);
            Enemy(EnemyType.Jiangshi, 126f, 3.75f, patrol: 1.5f);

            // 6. Pilares com buracos entre eles
            Ground(135f, 139f, 1.5f);
            Ground(142f, 146f, 3f);
            Ground(149f, 153f, 1.5f);
            OrbArc(132f, 135f, 2.2f, 1.2f, 3);
            OrbArc(139f, 142f, 3.8f, 1.2f, 3);
            OrbArc(146f, 149f, 3.8f, 1.2f, 3);

            // 7. Reta final: checkpoint, um lobo, o golem como guarda e o portal da meta
            Ground(153f, right, 0f);
            Checkpoint(156f, 0f);
            Platform(166f, 3f, 4f);
            OrbLine(165f, 3.9f, 167f, 3.9f, 3);
            Enemy(EnemyType.Wolf, 168f, 0f);
            Enemy(EnemyType.Golem, 181f, 0f, patrol: 2f);
            Torii(193f, 0f, portal: true);
            _goalX = 193f;
            _goalGroundY = 0f;

            _player = SpawnPlayer(_startSpawn);
        }

        /// <summary>
        /// Decoração, segmento a segmento (x, altura do chão, altura da peça). Regras para não estragar a leitura:
        /// árvores só lá atrás e escuras; nada em cima das plataformas; as silhuetas da frente ficam abaixo do caminho;
        /// longe dos portões e das lanternas de checkpoint.
        /// </summary>
        void Decorate()
        {
            const DecorLayer Far = DecorLayer.Far, Back = DecorLayer.Back, Near = DecorLayer.Near, Front = DecorLayer.Front;
            void D(string piece, float x, float y, float h, DecorLayer layer, bool flip = false) => _decor.Place(piece, x, y, h, layer, flip);

            // 1. Início (-10 a 22)
            D("tree_green", -7f, 0f, 5.4f, Far);
            D("tree_pine_small", 5.5f, 0f, 4.2f, Far, true);
            D("tree_willow", 16f, 0f, 5.2f, Far);
            D("rock_big", -5.5f, 0f, 1.3f, Back);
            D("rock_round", 7f, 0f, 0.8f, Back);
            D("banner", 20f, 0f, 2.6f, Back);
            D("bush_round", 4f, 0f, 0.75f, Near);
            D("bush_grass", 12.5f, 0f, 0.55f, Near);
            D("bush_flower", 17.5f, 0f, 0.75f, Near, true);
            D("rock_pebble", 14f, 0f, 0.3f, Near);
            D("fern_a", -5f, 0f, 1.1f, Front);
            D("fern_b", 9f, 0f, 1f, Front, true);
            D("fern_a", 18.5f, 0f, 1.1f, Front, true);

            // 2. Depois do primeiro buraco (26 a 44)
            D("tree_sakura", 31f, 0f, 5f, Far);
            D("tree_bamboo", 40.5f, 0f, 5.2f, Far);
            D("rock_tall", 28f, 0f, 1.6f, Back);
            _decor.LampPost(37.5f, 0f);
            D("bush_leafy", 33f, 0f, 0.7f, Near);
            D("bush_grass", 42.5f, 0f, 0.5f, Near, true);
            D("rock_small", 34.2f, 0f, 0.35f, Near);
            D("fern_b", 30f, 0f, 1f, Front);
            D("fern_a", 41f, 0f, 1.1f, Front);

            // 3. Degrau de cima (44 a 60)
            D("tree_pine", 47f, 1.5f, 5.6f, Far);
            D("tree_green", 58f, 1.5f, 4.8f, Far, true);
            D("ruin_pillar", 50f, 1.5f, 2f, Back);
            D("ruin_arch", 56.5f, 1.5f, 2.6f, Back);
            D("bush_round", 45.5f, 1.5f, 0.7f, Near, true);
            D("rock_mossy", 53.3f, 1.5f, 0.45f, Near);
            D("fern_b", 48f, 1.5f, 1f, Front, true);
            D("fern_a", 58.5f, 1.5f, 1.1f, Front);

            // 4. Depois do buraco grande, com o 1.º checkpoint em 78 (74 a 96)
            D("tree_willow", 82f, 0f, 5.4f, Far, true);
            D("tree_pine_small", 92f, 0f, 4.4f, Far);
            _decor.StoneLantern(86.5f, 0f, tall: false);
            D("fence", 89.5f, 0f, 1.05f, Back);
            D("rock_round", 94.5f, 0f, 0.75f, Back, true);
            D("bush_flower", 75.5f, 0f, 0.7f, Near);
            D("bush_leafy", 80.5f, 0f, 0.65f, Near, true);
            D("bush_grass", 95f, 0f, 0.5f, Near);
            D("fern_a", 76f, 0f, 1.1f, Front);
            D("fern_b", 88f, 0f, 1f, Front, true);

            // 5. Depois da plataforma móvel (112 a 132)
            D("tree_bamboo", 114.5f, 0.5f, 5f, Far, true);
            D("tree_green", 122f, 0.5f, 5.2f, Far);
            D("tree_sakura", 130f, 0.5f, 4.6f, Far, true);
            D("rock_flat", 117f, 0.5f, 0.55f, Back);
            D("banner", 129.5f, 0.5f, 2.4f, Back, true);
            D("bush_round", 113.5f, 0.5f, 0.7f, Near);
            D("rock_pebble", 120f, 0.5f, 0.3f, Near);
            D("bush_grass", 124.5f, 0.5f, 0.5f, Near, true);
            D("fern_b", 116f, 0.5f, 1f, Front);
            D("fern_a", 127f, 0.5f, 1.1f, Front, true);

            // 6. Pilares: só coisas pequenas, para não tapar os saltos
            D("bush_leafy", 136.2f, 1.5f, 0.55f, Near);
            D("rock_small", 144.8f, 3f, 0.35f, Near);
            D("bush_grass", 151.5f, 1.5f, 0.45f, Near, true);

            // 7. Reta final, com o 2.º checkpoint em 156, o golem e o portal em 193 (153 a 201)
            D("tree_pine", 160.5f, 0f, 5.8f, Far);
            D("tree_willow", 171f, 0f, 5.2f, Far, true);
            D("tree_green", 178.5f, 0f, 5.6f, Far);
            D("tree_pine_small", 186f, 0f, 4.4f, Far, true);
            D("tree_bamboo", 198.5f, 0f, 5.2f, Far);
            _decor.LampPost(162.5f, 0f);
            D("ruin_arch", 172.5f, 0f, 2.6f, Back);
            D("rock_big", 176.5f, 0f, 1.2f, Back, true);
            D("ruin_pillar", 183.5f, 0f, 1.8f, Back);
            D("banner", 186.5f, 0f, 2.6f, Back);
            _decor.StoneLantern(189.2f, 0f, tall: true);
            _decor.StoneLantern(196.8f, 0f, tall: true);
            D("bush_flower", 158.5f, 0f, 0.7f, Near, true);
            D("rock_pebble", 165f, 0f, 0.3f, Near);
            D("bush_round", 169f, 0f, 0.7f, Near);
            D("bush_grass", 180f, 0f, 0.5f, Near);
            D("rock_mossy", 185f, 0f, 0.45f, Near);
            D("bush_leafy", 199.5f, 0f, 0.65f, Near, true);
            D("fern_a", 161f, 0f, 1.1f, Front);
            D("fern_b", 174f, 0f, 1f, Front, true);
            D("fern_a", 184f, 0f, 1.1f, Front, true);
            D("fern_b", 198f, 0f, 1f, Front);
        }

        // ---------------------------------------------------------------- peças do nível

        /// <summary>Bloco de chão de x0 a x1 com o topo à altura "top" (vai até lá abaixo).</summary>
        void Ground(float x0, float x1, float top)
        {
            var size = new Vector2(x1 - x0, top - GroundBottom);
            Block("Chão", new Vector2((x0 + x1) * 0.5f, (top + GroundBottom) * 0.5f), size, BlockKind.Ground);
        }

        void Platform(float centerX, float top, float width)
        {
            const float thickness = 0.5f;
            Block("Plataforma", new Vector2(centerX, top - thickness * 0.5f), new Vector2(width, thickness), BlockKind.Platform);
        }

        void MovingPlatformAt(float fromX, float toX, float top, float width, float speed)
        {
            const float thickness = 0.5f;
            var go = Block("Plataforma móvel", new Vector2(fromX, top - thickness * 0.5f), new Vector2(width, thickness), BlockKind.Platform);
            go.SetActive(false);
            go.AddComponent<Rigidbody2D>();
            var mp = go.AddComponent<MovingPlatform>();
            mp.pointA = go.transform.position;
            mp.pointB = new Vector2(toX, go.transform.position.y);
            mp.speed = speed;
            go.SetActive(true);
        }

        GameObject Block(string name, Vector2 center, Vector2 size, BlockKind kind)
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
                    if (_platformSprite != null) GroundVisual(go.transform, size);
                    else
                    {
                        // Sem a imagem das plataformas: faixa antiga (caminho + muro) e escuro até ao fundo.
                        const float stripHeight = 4.26f, overhang = 0.26f;
                        if (TiledVisual(go.transform, _groundSprite, size, stripHeight, overhang, 0))
                        {
                            float fill = size.y + overhang - stripHeight;
                            if (fill > 0f)
                                PrototypeSprites.CreateRect(go.transform, "Fundo", new Vector2(0f, -size.y * 0.5f + fill * 0.5f + 0.02f),
                                                            new Vector2(size.x, fill + 0.05f), GroundDeepColor, -1);
                        }
                        else ColorVisual(go.transform, size, GroundColor);
                    }
                    break;
                case BlockKind.Platform:
                    float platformScale = PlatformVisualHeight / (_platformSprite != null ? _platformSprite.bounds.size.y : 1f);
                    if (!TiledVisual(go.transform, _platformSprite, size, PlatformVisualHeight,
                                     PlatformWalkPixels / 100f * platformScale, 1, PlatformTint, 0.3f) &&
                        !TiledVisual(go.transform, _ledgeSprite, size, size.y + 0.25f, 0.1f, 1))
                        ColorVisual(go.transform, size, PlatformColor);
                    break;
            }
            return go;
        }

        /// <summary>
        /// Chão feito das mesmas peças das plataformas: em cima a relva com pedras (platform.png, que se repete
        /// sem emendas e tem as pontas arredondadas nos buracos), por baixo pedras repetíveis (ground_body.png)
        /// que vão escurecendo até ficarem pretas, para o chão não competir com o jogador.
        /// </summary>
        void GroundVisual(Transform parent, Vector2 size)
        {
            float scale = PlatformVisualHeight / _platformSprite.bounds.size.y;
            float walk = PlatformWalkPixels / 100f * scale;
            TiledVisual(parent, _platformSprite, size, PlatformVisualHeight, walk, 0, PlatformTint, 0.3f);

            float top = size.y * 0.5f;                       // topo do collider (local)
            float bodyTop = top + walk - PlatformVisualHeight * 0.8f; // começa por trás das pedras do topo
            float bottom = -size.y * 0.5f;
            float bodyWidth = size.x - 0.1f;

            if (_groundBodySprite != null)
            {
                const float bodyHeight = 3.2f;
                var go = new GameObject("Pedras");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(0f, bodyTop - bodyHeight * 0.5f, 0f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _groundBodySprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(bodyWidth / scale, bodyHeight / scale);
                sr.color = GroundBodyTint;
                sr.sortingOrder = -3;

                // Escurece para baixo
                const float fadeTop = 0.5f, fadeHeight = 2.5f;
                var fade = new GameObject("Sombra");
                fade.transform.SetParent(parent, false);
                fade.transform.localPosition = new Vector3(0f, bodyTop - fadeTop - fadeHeight * 0.5f, 0f);
                fade.transform.localScale = new Vector3(bodyWidth, fadeHeight, 1f);
                var fr = fade.AddComponent<SpriteRenderer>();
                fr.sprite = FadeSprite;
                fr.color = GroundDeepColor;
                fr.sortingOrder = -2;
                bodyTop -= fadeTop + fadeHeight - 0.02f;
            }

            float fill = bodyTop - bottom;
            if (fill > 0f)
                PrototypeSprites.CreateRect(parent, "Fundo", new Vector2(0f, bottom + fill * 0.5f),
                                            new Vector2(bodyWidth, fill), GroundDeepColor, -2);
        }

        static Sprite _fade;

        /// <summary>Gradiente vertical de 1x1 unidade: transparente em cima, opaco em baixo.</summary>
        static Sprite FadeSprite
        {
            get
            {
                if (_fade != null) return _fade;
                const int n = 32;
                var tex = new Texture2D(1, n, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "GroundFade"
                };
                for (int y = 0; y < n; y++)
                {
                    float t = 1f - (y + 0.5f) / n; // y = 0 é o fundo
                    tex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, t)));
                }
                tex.Apply();
                _fade = Sprite.Create(tex, new Rect(0, 0, 1, n), new Vector2(0.5f, 0.5f), n);
                _fade.name = "GroundFade";
                return _fade;
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
        /// Repete um sprite na horizontal para cobrir um bloco. visualHeight é a altura do desenho
        /// e topOverhang quanto ele sobe acima do topo do collider.
        /// </summary>
        bool TiledVisual(Transform parent, Sprite sprite, Vector2 size, float visualHeight, float topOverhang, int order)
            => TiledVisual(parent, sprite, size, visualHeight, topOverhang, order, Color.white, 0f);

        /// <summary>Igual, com uma cor e um pouco mais de largura do que o collider (extraWidth, no total).</summary>
        bool TiledVisual(Transform parent, Sprite sprite, Vector2 size, float visualHeight, float topOverhang, int order,
                         Color tint, float extraWidth)
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
            sr.size = new Vector2((size.x + extraWidth) / scale, sprite.bounds.size.y);
            sr.sortingOrder = order;
            sr.color = tint;
            return true;
        }

        /// <summary>
        /// A imagem da plataforma tem pontas arredondadas: o sprite é criado com bordas (9-slice), para
        /// as pontas ficarem iguais e só o meio se repetir. O chão usa a mesma imagem no topo.
        /// </summary>
        void LoadPlatformSprites()
        {
            var tex = Resources.Load<Texture2D>(LevelArt + "platform");
            if (tex == null)
            {
                Debug.LogWarning($"[Heavenly Warrior] Não encontrei Resources/{LevelArt}platform. As plataformas usam a imagem antiga.");
                return;
            }
            float end = Mathf.Min(PlatformEndPixels, tex.width * 0.25f);
            var border = new Vector4(end, 0f, end, 0f);
            _platformSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                                            CharacterSprites.PixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            _platformSprite.name = "platform";

        }

        /// <summary>Portão: no início (o jogador começa à porta) e na meta (com o portal a brilhar lá dentro).</summary>
        void Torii(float x, float groundY, bool portal)
        {
            var root = new GameObject(portal ? "Meta - Portal" : "Início - Portão").transform;
            root.SetParent(_level, false);
            root.position = new Vector3(x, groundY, 0f);

            if (_gateSprite.Length > 0)
            {
                var gate = _gateSprite[0];
                float scale = GateHeight / gate.bounds.size.y;
                SpriteChild(root, "Portão", gate, new Vector2(0f, -GateSink), scale, 2);

                if (portal)
                {
                    // Brilho atrás do portão, só na abertura (a imagem é transparente lá dentro).
                    float ppu = gate.pixelsPerUnit;
                    float imgWidth = gate.rect.width;
                    float left = (GateOpeningLeft - imgWidth * 0.5f) / ppu * scale;
                    float right = (GateOpeningRight - imgWidth * 0.5f) / ppu * scale;
                    float bottom = -GateSink + GateOpeningBottom / ppu * scale;
                    float top = -GateSink + GateOpeningTop / ppu * scale;
                    var size = new Vector2(right - left + 0.1f, top - bottom + 0.1f);
                    var mid = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
                    var glow = PrototypeSprites.CreateRect(root, "Portal", mid, size, new Color(0.55f, 0.9f, 1f, 0.6f), 1);
                    PrototypeSprites.CreateRect(root, "Núcleo", mid, new Vector2(size.x * 0.45f, size.y * 0.85f),
                                                new Color(0.9f, 1f, 1f, 0.35f), 1);
                    root.gameObject.AddComponent<GoalPortal>().glow = glow;
                }
                return;
            }

            // Sem sprite: torii feito de retângulos
            const float halfSpan = 1.6f, height = 4.2f;
            PrototypeSprites.CreateRect(root, "Pilar esq", new Vector2(-halfSpan, height * 0.5f), new Vector2(0.35f, height), ToriiRed, 2);
            PrototypeSprites.CreateRect(root, "Pilar dir", new Vector2(halfSpan, height * 0.5f), new Vector2(0.35f, height), ToriiRed, 2);
            PrototypeSprites.CreateRect(root, "Nuki", new Vector2(0f, height - 0.7f), new Vector2(halfSpan * 2f + 0.6f, 0.22f), ToriiRed, 3);
            PrototypeSprites.CreateRect(root, "Kasagi", new Vector2(0f, height + 0.1f), new Vector2(halfSpan * 2f + 1.4f, 0.35f), ToriiDark, 3);
            PrototypeSprites.CreateRect(root, "Placa", new Vector2(0f, height - 0.3f), new Vector2(0.5f, 0.6f), ToriiDark, 4);

            if (portal)
            {
                var glow = PrototypeSprites.CreateRect(root, "Portal", new Vector2(0f, (height - 0.8f) * 0.5f),
                                                       new Vector2(halfSpan * 2f - 0.35f, height - 0.8f),
                                                       new Color(0.55f, 0.9f, 1f, 0.55f), 1);
                PrototypeSprites.CreateRect(root, "Núcleo", new Vector2(0f, (height - 0.8f) * 0.5f),
                                            new Vector2(1.2f, height - 1.4f), new Color(0.9f, 1f, 1f, 0.35f), 1);
                root.gameObject.AddComponent<GoalPortal>().glow = glow;
            }
        }

        /// <summary>Lanterna de pedra que acende ao passar (checkpoint).</summary>
        void Checkpoint(float x, float groundY)
        {
            var root = new GameObject("Checkpoint").transform;
            root.SetParent(_level, false);
            root.position = new Vector3(x, groundY, 0f);

            var cp = root.gameObject.AddComponent<Checkpoint>();
            if (_lanternOff.Length > 0)
            {
                float scale = LanternHeight / _lanternOff[0].bounds.size.y;
                cp.body = SpriteChild(root, "Lanterna", _lanternOff[0], new Vector2(0f, -0.1f), scale, 3);
                cp.offFrames = _lanternOff;
                cp.onFrames = _lanternOn.Length > 0 ? _lanternOn : _lanternOff;
            }
            else
            {
                var stone = new Color(0.42f, 0.42f, 0.48f);
                PrototypeSprites.CreateRect(root, "Base", new Vector2(0f, 0.12f), new Vector2(0.6f, 0.25f), stone * 0.8f, 2);
                PrototypeSprites.CreateRect(root, "Pilar", new Vector2(0f, 0.7f), new Vector2(0.22f, 0.95f), stone, 2);
                PrototypeSprites.CreateRect(root, "Caixa", new Vector2(0f, 1.4f), new Vector2(0.6f, 0.5f), stone, 2);
                cp.flame = PrototypeSprites.CreateRect(root, "Chama", new Vector2(0f, 1.4f), new Vector2(0.3f, 0.3f), Color.gray, 3);
                PrototypeSprites.CreateRect(root, "Telhado", new Vector2(0f, 1.72f), new Vector2(0.85f, 0.16f), stone * 0.7f, 3);
            }

            _checkpoints.Add(cp);
            _checkpointSpawns.Add(new Vector2(x + 1f, groundY + PlayerSize.y * 0.5f));
        }

        void Orb(Vector2 position)
        {
            var go = new GameObject("Orbe de Qi");
            go.transform.SetParent(_level, false);
            go.transform.position = position;
            var orb = go.AddComponent<QiOrb>();

            if (_orbFrames.Length > 0)
            {
                float scale = OrbHeight / _orbFrames[0].bounds.size.y;
                orb.body = SpriteChild(go.transform, "Orbe", _orbFrames[0], Vector2.zero, scale, 30);
                orb.frames = _orbFrames;
                orb.sparkleFrames = _sparkleFrames;
            }
            else
            {
                PrototypeSprites.CreateRect(go.transform, "Brilho", Vector2.zero, new Vector2(0.34f, 0.34f), new Color(0.45f, 0.8f, 1f, 0.9f), 30);
                PrototypeSprites.CreateRect(go.transform, "Centro", Vector2.zero, new Vector2(0.14f, 0.14f), new Color(0.95f, 1f, 1f), 31);
            }
            _orbs.Add(orb);
        }

        /// <summary>Filho com um sprite, numa escala uniforme.</summary>
        static SpriteRenderer SpriteChild(Transform parent, string name, Sprite sprite, Vector2 localPos, float scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        void OrbLine(float x0, float y0, float x1, float y1, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                Orb(new Vector2(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t)));
            }
        }

        /// <summary>Arco de orbes por cima de um buraco (mostra o caminho do salto).</summary>
        void OrbArc(float x0, float x1, float baseY, float height, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                Orb(new Vector2(Mathf.Lerp(x0, x1, t), baseY + Mathf.Sin(t * Mathf.PI) * height));
            }
        }

        void CreateBackdrop(Camera cam)
        {
            cam.backgroundColor = SkyColor;
            if (_backdropSprite == null) return;

            var root = new GameObject("Fundo - Floresta Mística");
            root.transform.SetParent(_level, false);

            // Alto o suficiente para cobrir o ecrã em cima dos pilares e mostrar a floresta nos buracos.
            const float height = 18f;
            const float bottom = -3.4f;
            float scale = height / _backdropSprite.bounds.size.y;
            float width = _backdropSprite.bounds.size.x * scale;

            var parallax = root.AddComponent<ParallaxLayer>();
            parallax.factor = new Vector2(0.9f, 0f);

            // Três cópias lado a lado; as das pontas espelhadas, para as junções coincidirem.
            for (int i = -1; i <= 1; i++)
            {
                var go = new GameObject("Fundo " + (i + 2));
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(i * width, 0f, 0f);
                go.transform.localScale = new Vector3(i == 0 ? scale : -scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _backdropSprite;
                sr.color = BackdropTint;
                sr.sortingOrder = -100;
            }

            // Posição inicial escolhida para o fundo ficar centrado quando a câmara está a meio do nível.
            float middle = (_cameraLimits.x + _cameraLimits.y) * 0.5f;
            float camX = cam.transform.position.x;
            float startX = middle - parallax.factor.x * (middle - camX);
            root.transform.position = new Vector3(startX, bottom + height * 0.5f, 0f);
            parallax.Init(cam.transform);
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
            var visual = CreateVisual(go, "Blue", PlayerSize, PlayerColor, 50, out _, out var animator);

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
                                                        new Color(0.7f, 0.95f, 1f, 0.55f), 55);
                combat.slashVisual = slash;
            }

            go.SetActive(true);
            return controller;
        }

        /// <summary>Cria um inimigo com os pés em (x, groundY).</summary>
        void Enemy(EnemyType type, float x, float groundY, bool dummy = false, float patrol = 3f)
        {
            var size = type.Size;
            var go = CharacterBase(type.Name, new Vector2(x, groundY + size.y * 0.5f), size, type.MaxHealth, Team.Enemy);
            var visual = CreateVisual(go, type.SpriteSet, size, type.FallbackColor, 40, out var body, out var animator);
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

            var health = go.GetComponent<Health>();
            if (!dummy)
            {
                EnemiesTotal++;
                health.Died += () => EnemiesDefeated++;
            }

            // Barra de vida
            var barRoot = new GameObject("Barra de vida").transform;
            barRoot.SetParent(go.transform, false);
            barRoot.localPosition = new Vector3(0f, size.y * 0.5f + 0.35f, 0f);
            var bar = go.AddComponent<WorldHealthBar>();
            bar.width = Mathf.Max(0.9f, size.x);
            bar.health = health;
            bar.background = PrototypeSprites.CreateRect(barRoot, "Fundo", Vector2.zero, new Vector2(bar.width + 0.06f, 0.14f),
                                                         new Color(0f, 0f, 0f, 0.7f), 60);
            bar.fill = PrototypeSprites.CreateRect(barRoot, "Vida", Vector2.zero, new Vector2(bar.width, 0.08f),
                                                   new Color(0.9f, 0.2f, 0.25f), 61);

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
            follow.xLimits = _cameraLimits;
            follow.yMin = 2.25f; // não desce atrás do jogador quando ele cai num buraco
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
