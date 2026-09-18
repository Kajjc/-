using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Arena.EditorTools
{
    // Генерирует статический TMP SDF-шрифт с кириллицей (гипотеза К1,
    // docs/feature-hypotheses.md): встроенный в WebGL-билд LegacyRuntime.ttf не
    // содержит кириллических глифов (подтверждено 18.09 headless-скриншотом —
    // весь русский текст пропадал, оставались только цифры/пунктуация).
    //
    // Источник — системный Segoe UI (Assets/Fonts/_SourceOnly/, вне git: это не
    // наш шрифт для распространения). AtlasPopulationMode.Static — итоговый
    // .asset не хранит ссылку на исходный файл и не пытается достраивать атлас
    // в рантайме, поэтому лицензионный вопрос закрыт и WebGL ни от чего не зависит.
    //
    // Запускать БЕЗ -quit в командной строке: если проект ещё не касался TMP,
    // рантайм-шейдеры ("TextMeshPro/Mobile/Distance Field" и т.п.) физически не
    // существуют в проекте до первого импорта "TMP Essential Resources.unitypackage",
    // а AssetDatabase.ImportPackage асинхронный — сборку шрифта продолжаем только
    // в его колбэке, после чего процесс завершает сам себя через EditorApplication.Exit.
    public static class TmpFontBuilder
    {
        private const string SourceFontPath = "Assets/Fonts/_SourceOnly/SegoeUI.ttf";
        private const string OutputAssetPath = "Assets/Resources/Fonts/UIFont SDF.asset";

        public static void Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null)
            {
                BuildFontAsset();
                EditorApplication.Exit(0);
                return;
            }

            string packagePath = Path.GetFullPath("Packages/com.unity.ugui")
                + "/Package Resources/TMP Essential Resources.unitypackage";

            AssetDatabase.importPackageCompleted += OnEssentialsImported;
            AssetDatabase.importPackageFailed += OnEssentialsImportFailed;
            AssetDatabase.ImportPackage(packagePath, false);
        }

        private static void OnEssentialsImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnEssentialsImported;
            AssetDatabase.importPackageFailed -= OnEssentialsImportFailed;

            try
            {
                BuildFontAsset();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        private static void OnEssentialsImportFailed(string packageName, string errorMessage)
        {
            AssetDatabase.importPackageCompleted -= OnEssentialsImported;
            AssetDatabase.importPackageFailed -= OnEssentialsImportFailed;
            Debug.LogError($"Import of {packageName} failed: {errorMessage}");
            EditorApplication.Exit(1);
        }

        private static void BuildFontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
                throw new System.Exception($"Source font not found at {SourceFontPath}");

            // TryAddCharacters отказывается работать в режиме Static (проверяет
            // m_AtlasPopulationMode и молча возвращает всё как "missing") — поэтому
            // создаём как Dynamic, заполняем атлас статическим набором символов,
            // и только потом фиксируем как Static + вручную отвязываем ссылку на
            // исходный шрифт через SerializedObject (m_SourceFontFile — internal
            // сеттер, недоступен напрямую из другой сборки). Без этого шрифт
            // Windows остался бы сериализованной зависимостью .asset-файла и мог
            // попасть в WebGL-билд как встроенный ресурс — а его нельзя
            // распространять как часть проекта.
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 2048,
                atlasHeight: 2048,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
                throw new System.Exception("TMP_FontAsset.CreateFontAsset вернул null — проверьте \"Include Font Data\" в настройках импорта шрифта.");

            bool allAdded = fontAsset.TryAddCharacters(BuildCharacterSet(), out string missing);

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            var serialized = new SerializedObject(fontAsset);
            serialized.FindProperty("m_SourceFontFile").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var dir = Path.GetDirectoryName(OutputAssetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            foreach (var atlas in fontAsset.atlasTextures)
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!allAdded)
                throw new System.Exception($"TMP-атлас не покрывает все нужные символы, не хватает: {missing}");

            Debug.Log($"TMP font asset создан: {OutputAssetPath}, символов в таблице: {fontAsset.characterTable.Count}");
        }

        // Basic Latin (пробел..тильда) + весь блок Cyrillic (U+0400-04FF) +
        // спецсимволы, реально встречающиеся в контенте игры (сценарии + UI-строки,
        // проверено скриптом по всем .json/.cs в Assets/Resources и Assets/Scripts):
        // § « · » — ← → ≥.
        private static string BuildCharacterSet()
        {
            var sb = new StringBuilder();
            for (int c = 0x0020; c <= 0x007E; c++) sb.Append((char)c);
            for (int c = 0x0400; c <= 0x04FF; c++) sb.Append((char)c);
            sb.Append("§«·»—←→≥");
            return sb.ToString();
        }
    }
}
