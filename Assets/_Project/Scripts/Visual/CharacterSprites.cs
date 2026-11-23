using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Conjunto de animações de um personagem, carregado de Resources/Characters/&lt;Nome&gt;.
    /// Cada ficheiro chama-se &lt;animação&gt;_&lt;nn&gt;.png (por exemplo run_00, run_01...).
    ///
    /// Os sprites são sempre criados a partir das texturas, com 100 px por unidade e o pivot nos pés.
    /// Assim aparecem corretamente mesmo que as imagens tenham sido importadas com outras definições
    /// (por exemplo como "Default" em vez de "Sprite", ou com o pivot ao centro).
    /// </summary>
    public class CharacterSprites
    {
        public const float PixelsPerUnit = 100f;
        /// <summary>Os frames têm os pés 8 px acima do fundo (ver Tools/slice_*.py).</summary>
        public const float FeetPixels = 8f;

        readonly Dictionary<string, Sprite[]> _anims = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, CharacterSprites> Cache = new Dictionary<string, CharacterSprites>();

        public bool IsEmpty => _anims.Count == 0;
        public int FrameCount { get; private set; }

        /// <summary>Nomes das pastas que não tinham imagens (o HUD mostra um aviso).</summary>
        public static readonly List<string> Missing = new List<string>();

        /// <summary>Devolve null se a pasta não tiver imagens.</summary>
        public static CharacterSprites Load(string characterName)
        {
            if (Cache.TryGetValue(characterName, out var cached) && cached != null) return cached;

            string path = "Characters/" + characterName;
            var textures = Resources.LoadAll<Texture2D>(path);
            var set = new CharacterSprites();
            var groups = new Dictionary<string, List<Sprite>>();
            bool resized = false;

            foreach (var tex in textures)
            {
                if (tex == null) continue;
                var sprite = SpriteLoader.FromTexture(tex, new Vector2(0.5f, FeetPixels / Mathf.Max(1, tex.height)));
                if (Mathf.IsPowerOfTwo(tex.width) && Mathf.IsPowerOfTwo(tex.height))
                    resized = true; // provavelmente redimensionada pelo "Non-Power of 2" da importação Default

                int cut = tex.name.LastIndexOf('_');
                string anim = cut > 0 ? tex.name.Substring(0, cut) : tex.name;
                if (!groups.TryGetValue(anim, out var list)) groups[anim] = list = new List<Sprite>();
                list.Add(sprite);
                set.FrameCount++;
            }

            foreach (var pair in groups)
            {
                pair.Value.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                set._anims[pair.Key] = pair.Value.ToArray();
            }

            if (set.IsEmpty)
            {
                if (!Missing.Contains(characterName)) Missing.Add(characterName);
                Debug.LogWarning($"[Heavenly Warrior] Não encontrei imagens em Assets/_Project/Resources/{path}. " +
                                 "O personagem vai aparecer como um retângulo de cor. Confirma que a pasta existe e tem os .png.");
                return null;
            }

            Missing.Remove(characterName);
            string anims = string.Join(", ", groups.Keys);
            Debug.Log($"[Heavenly Warrior] {characterName}: {set.FrameCount} frames ({anims})");
            if (resized)
                Debug.LogWarning($"[Heavenly Warrior] As imagens de {characterName} parecem redimensionadas pela importação. " +
                                 "Usa o menu Heavenly Warrior > Reimportar arte para as voltar a configurar.");

            Cache[characterName] = set;
            return set;
        }

        public Sprite[] Get(string anim) => _anims.TryGetValue(anim, out var frames) ? frames : null;
        public bool Has(string anim) => _anims.ContainsKey(anim);
    }

    /// <summary>Cria sprites a partir de texturas de Resources, independentemente das definições de importação.</summary>
    public static class SpriteLoader
    {
        public static Sprite FromTexture(Texture2D tex, Vector2 pivot)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot,
                                       CharacterSprites.PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            return sprite;
        }

        /// <summary>Carrega uma imagem de Resources como sprite com o pivot ao centro (null se não existir).</summary>
        public static Sprite LoadCentered(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning($"[Heavenly Warrior] Não encontrei a imagem Resources/{path}. Vou usar uma cor no lugar dela.");
                return null;
            }
            return FromTexture(tex, new Vector2(0.5f, 0.5f));
        }
    }
}
