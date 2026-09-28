using UnityEditor;
using UnityEngine;

namespace Arena.EditorTools
{
    // Брендинг сборок: вместо стандартной заставки Unity ("Made with Unity" на сером
    // фоне) — наша картинка с логотипом; осмысленное имя продукта (заголовок окна
    // Windows-сборки и вкладки браузера вместо "unity"/"DefaultCompany"); собственный
    // шаблон страницы WebGL (Assets/WebGLTemplates/Arena — экран загрузки с той же
    // картинкой и логотипом). Настройки лежат в ProjectSettings и уже закоммичены —
    // методы нужны, чтобы их можно было воспроизвести одной командой:
    //   Unity.exe -batchmode -quit -projectPath unity -executeMethod Arena.EditorTools.BrandingSetup.Apply
    //     — всё сразу (заставка + имя продукта + шаблон WebGL), меню Arena -> Apply Branding;
    //   Unity.exe -batchmode -quit -projectPath unity -executeMethod Arena.EditorTools.BrandingSetup.ApplySplash
    //     — только заставка, остальные настройки не трогает, меню Arena -> Apply Splash Screen Only.
    // ProjectSettings.asset легко перезаписать целиком из открытого редактора со старыми
    // настройками (так 27.09 заставка уже откатывалась на стандартную) — после слияний
    // стоит проверить, что m_ShowUnitySplashLogo по-прежнему 0.
    public static class BrandingSetup
    {
        private const string LogoPath = "Assets/Resources/Icons/logo.png";
        private const string BackgroundPath = "Assets/Resources/Backgrounds/loading.png";
        private const float LogoSeconds = 2.5f;

        [MenuItem("Arena/Apply Branding")]
        public static void Apply()
        {
            PlayerSettings.companyName = "Arena Peregovorov";
            PlayerSettings.productName = "Арена переговоров";
            PlayerSettings.WebGL.template = "PROJECT:Arena";
            ApplySplash();
        }

        // Заставка Windows/standalone (и та же заставка внутри канваса в WebGL после
        // загрузки страницы): наша картинка фоном + логотип поверх, без логотипа Unity.
        // Статичная (без "наезда" камеры) — картинка и так спокойная.
        [MenuItem("Arena/Apply Splash Screen Only")]
        public static void ApplySplash()
        {
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            var background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (logo == null || background == null)
                throw new System.Exception($"Не найдены спрайты заставки: {LogoPath} / {BackgroundPath}");

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(LogoSeconds, logo) };
            PlayerSettings.SplashScreen.background = background;
            PlayerSettings.SplashScreen.blurBackgroundImage = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.086f, 0.137f, 0.247f, 1f); // Navy #16233F
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;
            PlayerSettings.SplashScreen.overlayOpacity = 0.3f;

            AssetDatabase.SaveAssets();
            Debug.Log($"[Branding] product={PlayerSettings.productName}, splash.show={PlayerSettings.SplashScreen.show}, " +
                      $"showUnityLogo={PlayerSettings.SplashScreen.showUnityLogo}, logos={PlayerSettings.SplashScreen.logos.Length}, " +
                      $"background={(PlayerSettings.SplashScreen.background != null)}, webglTemplate={PlayerSettings.WebGL.template}");
        }
    }
}
