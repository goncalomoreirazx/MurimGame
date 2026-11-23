using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>Camadas da decoração, de trás para a frente.</summary>
    public enum DecorLayer
    {
        /// <summary>Árvores atrás do chão, bem escurecidas e azuladas (parecem mais longe).</summary>
        Far,
        /// <summary>Pedras, ruínas e lanternas no caminho, atrás dos personagens.</summary>
        Back,
        /// <summary>Arbustos e pedrinhas em cima do chão, ainda atrás dos personagens.</summary>
        Near,
        /// <summary>Silhuetas quase pretas à frente de tudo, em baixo do ecrã.</summary>
        Front,
    }

    /// <summary>
    /// Decoração do nível (árvores, arbustos, pedras, ruínas) carregada de Resources/Decor.
    /// Cada peça é um .png com o nome usado em Place (tree_pine, bush_round, rock_big...).
    ///
    /// O mapa é escuro, por isso tudo é tingido conforme a camada: quanto mais atrás, mais escuro
    /// e mais azul. Assim a decoração não rouba atenção ao jogador, aos inimigos nem às plataformas.
    /// Se faltar alguma imagem, essa peça simplesmente não aparece (com um aviso na Console).
    /// </summary>
    public class LevelDecor
    {
        public const string Folder = "Decor";

        struct LayerStyle
        {
            public int order;
            public Color tint;
            public float lift; // quanto a base sobe em relação ao topo do chão (na Front: quanto o topo da peça sobe)
        }

        static readonly Dictionary<DecorLayer, LayerStyle> Styles = new Dictionary<DecorLayer, LayerStyle>
        {
            { DecorLayer.Far,   new LayerStyle { order = -20, tint = new Color(0.34f, 0.39f, 0.53f), lift = 0.18f } },
            { DecorLayer.Back,  new LayerStyle { order = -5,  tint = new Color(0.48f, 0.53f, 0.68f), lift = 0.1f } },
            { DecorLayer.Near,  new LayerStyle { order = 3,   tint = new Color(0.5f, 0.58f, 0.7f),   lift = 0.02f } },
            { DecorLayer.Front, new LayerStyle { order = 70,  tint = new Color(0.05f, 0.07f, 0.11f), lift = 0.1f } },
        };

        /// <summary>Luz quente das lanternas: fraca, para dar ambiente sem clarear o mapa.</summary>
        static readonly Color GlowColor = new Color(1f, 0.62f, 0.3f, 0.22f);

        readonly Dictionary<string, Sprite> _pieces = new Dictionary<string, Sprite>();
        readonly HashSet<string> _warned = new HashSet<string>();
        static Sprite _glow;

        public Transform Root { get; private set; }
        public int Count => _pieces.Count;

        public LevelDecor()
        {
            foreach (var tex in Resources.LoadAll<Texture2D>(Folder))
                if (tex != null) _pieces[tex.name] = SpriteLoader.FromTexture(tex, new Vector2(0.5f, 0f));

            if (_pieces.Count == 0)
                Debug.LogWarning($"[Heavenly Warrior] Não encontrei a decoração em Resources/{Folder}. O nível fica sem árvores nem pedras.");
            else
                Debug.Log($"[Heavenly Warrior] Decoração: {_pieces.Count} peças.");
        }

        /// <summary>Começa a decoração de um nível novo (o pai é apagado com o nível).</summary>
        public void Begin(Transform level)
        {
            Root = new GameObject("Decoração").transform;
            Root.SetParent(level, false);
        }

        public bool Has(string piece) => _pieces.ContainsKey(piece);

        /// <summary>
        /// Põe uma peça com a base em (x, groundY) e a altura indicada (unidades do mundo).
        /// Devolve null se a imagem não existir.
        /// </summary>
        public SpriteRenderer Place(string piece, float x, float groundY, float height, DecorLayer layer, bool flip = false)
        {
            if (Root == null) return null;
            if (!_pieces.TryGetValue(piece, out var sprite))
            {
                if (_pieces.Count > 0 && _warned.Add(piece))
                    Debug.LogWarning($"[Heavenly Warrior] Falta a peça de decoração Resources/{Folder}/{piece}.png.");
                return null;
            }

            var style = Styles[layer];
            float scale = height / sprite.bounds.size.y;
            var go = new GameObject(piece);
            go.transform.SetParent(Root, false);
            // As silhuetas da frente ficam com as pontas só um pouco acima do caminho, para não taparem os pés.
            float baseY = layer == DecorLayer.Front ? groundY + style.lift - height : groundY + style.lift;
            go.transform.position = new Vector3(x, baseY, 0f);
            go.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = style.tint;
            sr.sortingOrder = style.order;
            return sr;
        }

        /// <summary>Halo quente à volta de uma luz (lanterna, candeeiro).</summary>
        public void Glow(Vector2 position, float radius, int order)
        {
            if (Root == null) return;
            var go = new GameObject("Luz");
            go.transform.SetParent(Root, false);
            go.transform.position = position;
            float size = radius * 2f;
            var sprite = GlowSprite;
            float scale = size / sprite.bounds.size.x;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = GlowColor;
            sr.sortingOrder = order;
        }

        /// <summary>Candeeiro de madeira com a luz acesa na lanterna pendurada.</summary>
        public void LampPost(float x, float groundY, float height = 2.6f, bool flip = false)
        {
            var sr = Place("lamp_post", x, groundY, height, DecorLayer.Back, flip);
            if (sr == null) return;
            float w = sr.bounds.size.x;
            float lanternX = x + (flip ? -0.26f : 0.26f) * w;
            Glow(new Vector2(lanternX, sr.transform.position.y + height * 0.55f), 1.3f, -6);
        }

        /// <summary>Lanterna de pedra (decorativa, não é checkpoint) com uma luz fraquinha.</summary>
        public void StoneLantern(float x, float groundY, bool tall)
        {
            float height = tall ? 2.2f : 1.6f;
            var sr = Place(tall ? "stone_lantern_tall" : "stone_lantern_short", x, groundY, height, DecorLayer.Back);
            if (sr == null) return;
            Glow(new Vector2(x, sr.transform.position.y + height * (tall ? 0.6f : 0.64f)), 0.9f, -6);
        }

        /// <summary>Textura redonda e suave gerada por código (branca ao centro, transparente na borda).</summary>
        static Sprite GlowSprite
        {
            get
            {
                if (_glow != null) return _glow;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "DecorGlow"
                };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(t * t * 255));
                }
                tex.SetPixels32(px);
                tex.Apply();
                _glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                _glow.name = "DecorGlow";
                return _glow;
            }
        }
    }
}
