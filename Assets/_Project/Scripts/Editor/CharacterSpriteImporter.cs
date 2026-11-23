using System.IO;
using UnityEditor;
using UnityEngine;

namespace HeavenlyWarrior.EditorTools
{
    /// <summary>
    /// Configura automaticamente a arte do protótipo quando entra no projeto:
    /// - Assets/_Project/Resources/Characters: frames de personagens, pivot nos pés.
    /// - Assets/_Project/Resources/Levels: fundos e chão, pivot ao centro, prontos para repetir (Tiled).
    /// Tudo como Sprite, 100 px por unidade, sem compressão e sem mipmaps.
    /// Só atua na primeira importação, por isso podes mudar as definições à mão depois.
    /// </summary>
    public class CharacterSpriteImporter : AssetPostprocessor
    {
        const string CharactersFolder = "Assets/_Project/Resources/Characters/";
        const string LevelsFolder = "Assets/_Project/Resources/Levels/";
        public const float PixelsPerUnit = 100f;
        // Os frames têm os pés 8 px acima do fundo (ver Tools/slice_*.py).
        const float FeetPixels = 8f;

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            bool character = path.StartsWith(CharactersFolder);
            bool level = path.StartsWith(LevelsFolder);
            if (!character && !level) return;

            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // A arte não segue uma grelha de píxeis exata, por isso Bilinear fica mais limpo.
            // Quando houver pixel art "a sério" (1 píxel = 1 píxel), muda para FilterMode.Point.
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // necessário para o modo Tiled
            if (character)
            {
                int height = ReadPngHeight(path);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, height > 0 ? FeetPixels / height : 0f);
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
            }
            importer.SetTextureSettings(settings);
        }

        /// <summary>Lê a altura de um PNG diretamente do cabeçalho (o Unity ainda não a sabe nesta fase).</summary>
        static int ReadPngHeight(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var header = new byte[24];
                    if (stream.Read(header, 0, 24) < 24 || header[1] != 'P' || header[2] != 'N' || header[3] != 'G') return 0;
                    return (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                }
            }
            catch (IOException)
            {
                return 0;
            }
        }
    }
}
