using UnityEditor;
using UnityEngine;

namespace HeavenlyWarrior.EditorTools
{
    /// <summary>
    /// Configura automaticamente a arte do protótipo quando entra no projeto:
    /// - Resources/Characters: frames de personagens.
    /// - Resources/Levels: fundos e chão.
    /// Tudo como Sprite, 100 px por unidade, sem compressão, sem mipmaps e sem redimensionar.
    ///
    /// As imagens configuradas ficam marcadas; as que foram importadas antes deste script existir
    /// (por exemplo, quando havia erros de compilação) são corrigidas na importação seguinte.
    /// O menu "Heavenly Warrior > Reimportar arte" força isso para todas.
    ///
    /// O jogo cria os sprites a partir das texturas (ver CharacterSprites), por isso o pivot e os
    /// píxeis por unidade não dependem destas definições; aqui trata-se da qualidade da imagem.
    /// </summary>
    public class CharacterSpriteImporter : AssetPostprocessor
    {
        // Funciona em qualquer sítio do projeto, desde que esteja numa pasta Resources/Characters ou Resources/Levels.
        static bool IsGameArt(string path)
        {
            path = path.Replace('\\', '/');
            return path.Contains("/Resources/Characters/") || path.Contains("/Resources/Levels/");
        }
        const string Marker = "heavenly-warrior-art-v2";

        // Mudar este número faz o Unity reimportar as texturas com as regras novas.
        public override uint GetVersion() => 2;

        void OnPreprocessTexture()
        {
            if (!IsGameArt(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
            if (importer.userData == Marker) return; // já configurada (e possivelmente ajustada à mão)

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            // A arte não segue uma grelha de píxeis exata, por isso Bilinear fica mais limpo.
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.userData = Marker;
        }

        [MenuItem("Heavenly Warrior/Reimportar arte")]
        static void ReimportArt()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsGameArt(path) && AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.userData = "";
                    importer.SaveAndReimport();
                    count++;
                }
            }
            Debug.Log($"[Heavenly Warrior] {count} imagens reimportadas.");
        }
    }
}
