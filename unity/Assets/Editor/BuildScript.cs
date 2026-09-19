using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Arena.EditorTools
{
    // Билд-скрипт для CLI (-executeMethod), чтобы проверять сборки без ручных
    // кликов в редакторе — в частности, пробную WebGL-сборку на риск потери
    // кириллицы в легаси-шрифте (см. отчёт demo-reliability-checker).
    public static class BuildScript
    {
        private const string OutputPath = "WebGL-Build";

        public static void BuildWebGL()
        {
            // Без сжатия — сторонние статические хостинги (itch.io и т.п.) не всегда
            // отдают .gz/.br файлы с корректным Content-Encoding, из-за чего загрузчик
            // Unity зависает/не может распаковать билд (наблюдалось на itch.io: iframe
            // падает по таймауту "took too long to respond"). Билд крупнее, зато
            // работает на любом хостинге без специальной настройки заголовков сервера.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            var scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception($"WebGL build failed: {report.summary.result}, errors: {report.summary.totalErrors}");
        }

        // Резервный запускаемый билд под Windows — на случай сбоев/недоступности
        // публичного WebGL-хостинга (itch.io периодически подвисает, см.
        // docs/roadmap.md), не требует браузера/сети.
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Windows-Build/ArenaPeregovorov.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception($"Windows build failed: {report.summary.result}, errors: {report.summary.totalErrors}");
        }
    }
}
