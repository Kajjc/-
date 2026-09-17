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
    }
}
