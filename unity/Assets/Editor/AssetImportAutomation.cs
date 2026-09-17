using UnityEditor;

namespace Arena.EditorTools
{
    // Автоматически настраивает PNG под Resources/Portraits|Backgrounds|Icons как
    // UI-спрайт — чтобы можно было просто положить сгенерированный файл в нужную
    // папку, не открывая Import Settings руками. Часть флоу для участника команды,
    // который занимается генерацией ассетов (docs/team-plan.md).
    public class AssetImportAutomation : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Portraits/") &&
                !assetPath.Contains("/Resources/Backgrounds/") &&
                !assetPath.Contains("/Resources/Icons/"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
        }
    }
}
