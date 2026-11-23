using System.Collections.Generic;
using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Conjunto de animações de um personagem, carregado de Resources/Characters/&lt;Nome&gt;.
    /// Cada ficheiro chama-se &lt;animação&gt;_&lt;nn&gt;.png (por exemplo run_00, run_01...).
    /// </summary>
    public class CharacterSprites
    {
        readonly Dictionary<string, Sprite[]> _anims = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, CharacterSprites> Cache = new Dictionary<string, CharacterSprites>();

        public bool IsEmpty => _anims.Count == 0;

        /// <summary>Devolve null se a pasta não tiver sprites.</summary>
        public static CharacterSprites Load(string characterName)
        {
            if (Cache.TryGetValue(characterName, out var cached)) return cached;

            var sprites = Resources.LoadAll<Sprite>("Characters/" + characterName);
            var set = new CharacterSprites();
            var groups = new Dictionary<string, List<Sprite>>();
            foreach (var s in sprites)
            {
                int cut = s.name.LastIndexOf('_');
                string anim = cut > 0 ? s.name.Substring(0, cut) : s.name;
                if (!groups.TryGetValue(anim, out var list)) groups[anim] = list = new List<Sprite>();
                list.Add(s);
            }
            foreach (var pair in groups)
            {
                pair.Value.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                set._anims[pair.Key] = pair.Value.ToArray();
            }

            var result = set.IsEmpty ? null : set;
            Cache[characterName] = result;
            return result;
        }

        public Sprite[] Get(string anim) => _anims.TryGetValue(anim, out var frames) ? frames : null;
        public bool Has(string anim) => _anims.ContainsKey(anim);
    }
}
