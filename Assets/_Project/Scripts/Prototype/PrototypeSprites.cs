using UnityEngine;

namespace HeavenlyWarrior.Prototype
{
    /// <summary>
    /// Sprites provisórios gerados por código (quadrados de cor), para o protótipo funcionar
    /// sem nenhum asset. Serão substituídos pela pixel art.
    /// </summary>
    public static class PrototypeSprites
    {
        static Sprite _square;

        /// <summary>Quadrado branco de 1x1 unidades com pivot ao centro.</summary>
        public static Sprite Square
        {
            get
            {
                if (_square != null) return _square;
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "PrototypeSquare"
                };
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                tex.Apply();
                _square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                _square.name = "PrototypeSquare";
                return _square;
            }
        }

        /// <summary>Cria um filho com um retângulo de cor. Devolve o SpriteRenderer.</summary>
        public static SpriteRenderer CreateRect(Transform parent, string name, Vector2 localPos, Vector2 size, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
