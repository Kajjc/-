using UnityEditor;
using UnityEngine;

namespace Arena.EditorTools
{
    // Брендинг сборок: вместо стандартной заставки Unity ("Made with Unity" на сером
    // фоне) — наша картинка с логотипом; осмысленное имя продукта (заголовок окна
    // Windows-сборки и вкладки браузера вместо "unity"/"DefaultCompany"); собственный
    // шаблон страницы WebGL (Assets/WebGLTemplates/Arena — экран загрузки с той же
    // картинкой и логотипом). Настройки лежат в ProjectSettings и уже закоммичены —
    // метод нужен, чтобы их можно было воспроизвести/перенастроить одной командой:
    //   Unity.exe -batchmode -quit -projectPath unity -executeMethod Arena.EditorTools.BrandingSetup.Apply
    // или из меню Arena -> Apply Branding.
    public static class BrandingSetup
    {
        private const string LogoPath = "Assets/Resources/Icons/logo.png";
        private const string BackgroundPath = "Assets/Resources/Backgrounds/loading.png";
        private const float LogoSeconds = 2.5f;

        [MenuItem("Arena/Apply Branding")]
        public static void Apply()
        {
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            var background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (logo == null || background == null)
                throw new System.Exception($"Не найдены спрайты заставки: {LogoPath} / {BackgroundPath}");

            PlayerSettings.companyName = "Arena Peregovorov";
            PlayerSettings.productName = "Арена переговоров";

            // Заставка Windows/standalone: наша картинка фоном + логотип поверх, без
            // логотипа Unity. Статичная (без "наезда" камеры) — картинка и так спокойная.
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(LogoSeconds, logo) };
            PlayerSettings.SplashScreen.background = background;
            PlayerSettings.SplashScreen.blurBackgroundImage = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.086f, 0.137f, 0.247f, 1f); // Navy #16233F
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;
            PlayerSettings.SplashScreen.overlayOpacity = 0.3f;

            PlayerSettings.WebGL.template = "PROJECT:Arena";

            AssetDatabase.SaveAssets();
            Debug.Log($"[Branding] product={PlayerSettings.productName}, splash.show={PlayerSettings.SplashScreen.show}, " +
                      $"showUnityLogo={PlayerSettings.SplashScreen.showUnityLogo}, logos={PlayerSettings.SplashScreen.logos.Length}, " +
                      $"background={(PlayerSettings.SplashScreen.background != null)}, webglTemplate={PlayerSettings.WebGL.template}");
        }
    }
}
