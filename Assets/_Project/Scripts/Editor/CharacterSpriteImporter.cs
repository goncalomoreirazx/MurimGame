using UnityEditor;
using UnityEngine;

namespace HeavenlyWarrior.EditorTools
{
    /// <summary>
    /// Configura automaticamente os frames dos personagens quando entram no projeto
    /// (pasta Assets/_Project/Resources/Characters): Sprite, 100 px por unidade,
    /// pivot nos pés, sem compressão e sem mipmaps.
    /// Só atua na primeira importação, por isso podes mudar as definições à mão depois.
    /// </summary>
    public class CharacterSpriteImporter : AssetPostprocessor
    {
        const string Folder = "Assets/_Project/Resources/Characters/";
        public const float PixelsPerUnit = 100f;
        // Os frames têm 200 px de altura com os pés a 8 px do fundo (ver Tools/slice_espadachins.py).
        static readonly Vector2 FeetPivot = new Vector2(0.5f, 8f / 200f);

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // A folha original não segue uma grelha de píxeis exata, por isso Bilinear fica mais limpo.
            // Quando houver pixel art "a sério" (1 píxel = 1 píxel), muda para FilterMode.Point.
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = FeetPivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
