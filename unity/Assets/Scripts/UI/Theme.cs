using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arena.UI
{
    // Общая палитра и билдеры UI-примитивов — единый визуальный язык между
    // экраном теста навыков, админ-конфигом и диалогом. Палитра и назначение
    // цветов зафиксированы в docs/visual-style-guide.md.
    public static class Theme
    {
        public const int ReferenceWidth = 1280;
        public const int ReferenceHeight = 800;

        public static readonly Color Navy = Hex("#16233F");
        public static readonly Color Slate = Hex("#2E4368");
        public static readonly Color Teal = Hex("#4C8FA6");
        public static readonly Color Amber = Hex("#E8A33D");
        public static readonly Color Coral = Hex("#D9694F");
        public static readonly Color Sage = Hex("#5FA378");
        public static readonly Color Parchment = Hex("#F3EFE7");
        public static readonly Color Ink = Hex("#23262B");

        // Слегка осветлённый Slate — используется вместо чистого Navy/Slate там,
        // где нужен видимый на тёмном фоне холодный акцент (пипсы навыка "Логика").
        public static readonly Color LogikaAccent = Color.Lerp(Slate, Color.white, 0.3f);

        // Вторичный текст (подписи, описания) — достаточно непрозрачный, чтобы
        // реально читаться на тёмном фоне, но темнее основного белого текста.
        public static readonly Color Muted = new Color(Parchment.r, Parchment.g, Parchment.b, 0.94f);
        // Мелкие служебные CAPS-подписи (короткие заголовки полей типа "СФЕРА") —
        // минимально приглушены. Не использовать для длинного текста (абзацев,
        // цитат) — на таком объёме текста заметно теряет контраст.
        public static readonly Color EyebrowMuted = new Color(Parchment.r, Parchment.g, Parchment.b, 0.8f);

        private static TMP_FontAsset cachedFont;

        public static Color ForSkill(string skill)
        {
            switch (skill)
            {
                case "napor": return Coral;
                case "empatiya": return Teal;
                case "logika": return LogikaAccent;
                default: return Parchment;
            }
        }

        public static Color ForOutcome(string outcome)
        {
            switch (outcome)
            {
                case "win": return Sage;
                case "compromise": return Amber;
                case "fail": return Coral;
                default: return Parchment;
            }
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // К1 (docs/feature-hypotheses.md): встроенный LegacyRuntime.ttf не содержит
        // кириллических глифов на WebGL (подтверждено 18.09 — весь русский текст
        // пропадал в билде). UIFont SDF.asset — статический TMP-атлас, собранный
        // Assets/Editor/TmpFontBuilder.cs из системного Segoe UI, покрывает Basic
        // Latin + Cyrillic + пунктуацию, реально встречающуюся в контенте игры.
        public static TMP_FontAsset FontAsset()
        {
            if (cachedFont == null)
                cachedFont = Resources.Load<TMP_FontAsset>("Fonts/UIFont SDF");
            return cachedFont;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                default: return TextAlignmentOptions.Center;
            }
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform CreateCanvas(Transform parent, string name)
        {
            EnsureEventSystem();

            var canvasGo = new GameObject(name);
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = (RectTransform)canvasGo.transform;
            var background = CreatePanel(root, "Background", Navy);
            StretchFull(background);
            return root;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return (RectTransform)go.transform;
        }

        public static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static TMP_Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = FontAsset();
            text.fontSize = fontSize;
            text.alignment = ToTmpAlignment(anchor);
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        // Обычная кнопка (реплика диалога, CTA) — прямоугольная плашка с текстом слева.
        public static Button CreateButton(Transform parent, string label, Color fill, Color textColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Button_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = fill;
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            var text = CreateText(go.transform, "Label", 22, TextAnchor.MiddleLeft, textColor);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18, 6);
            textRect.offsetMax = new Vector2(-18, -6);
            text.text = label;

            return button;
        }

        // Чип-переключатель (сфера/тон/уровень сложности в админ-конфиге) — пилюля,
        // которая явно показывает состояние "выбрано"/"не выбрано".
        public static Button CreateChip(Transform parent, string label, UnityEngine.Events.UnityAction onClick, out Image background, out TMP_Text text)
        {
            var go = new GameObject($"Chip_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            background = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            if (onClick != null) button.onClick.AddListener(onClick);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 48;
            layoutElement.minWidth = 135;

            text = CreateText(go.transform, "Label", 20, TextAnchor.MiddleCenter, Parchment);
            var textRect = text.rectTransform;
            StretchFull(textRect);
            text.text = label;

            SetChipSelected(background, text, false);
            return button;
        }

        // Пытается загрузить сгенерированный ассет по конвенции имён из docs/team-plan.md
        // (Resources/Portraits|Backgrounds|Icons/<имя>.png) — null, если файла ещё нет.
        public static Sprite TryLoadSprite(string resourcePath) => Resources.Load<Sprite>(resourcePath);

        // Ищет дочернюю панель "Background" (её создаёт CreateCanvas) и либо ставит
        // на неё реальный фон, либо возвращает к сплошному Navy, если ассета нет —
        // безопасно вызывать повторно (например, при смене сценария).
        public static void SetCanvasBackground(RectTransform canvasRoot, string resourcePath)
        {
            var backgroundTransform = canvasRoot.Find("Background");
            if (backgroundTransform == null) return;
            var image = backgroundTransform.GetComponent<Image>();
            var sprite = TryLoadSprite(resourcePath);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
                image.color = Navy;
            }
        }

        private const float ScrollbarWidth = 10f;
        private const float ScrollbarGap = 6f;

        // Вертикальный прокручиваемый список (экран "Теория и техники", экран итога):
        // возвращает корневой RectTransform (для позиционирования на экране) и через
        // out — Content, куда добавлять элементы списка сверху вниз. Скроллбар справа
        // виден постоянно — чтобы было очевидно, что ниже есть ещё содержимое.
        public static RectTransform CreateScrollList(Transform parent, string name, out RectTransform content)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform));
            scrollGo.transform.SetParent(parent, false);
            var scrollRoot = (RectTransform)scrollGo.transform;
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = ScrollbarGap;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = (RectTransform)viewportGo.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-(ScrollbarWidth + ScrollbarGap), 0f);
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            // Без явного sizeDelta=0 RectTransform наследует дефолтные 100x100 от
            // создания — при растянутых по X анкорах это делает Content на 100px
            // шире вьюпорта и обрезает текст по обеим сторонам под RectMask2D.
            content.sizeDelta = Vector2.zero;

            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 12;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = CreateVerticalScrollbar(scrollGo.transform);

            return scrollRoot;
        }

        private static Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            var barGo = new GameObject("Scrollbar", typeof(RectTransform));
            barGo.transform.SetParent(parent, false);
            var barRect = (RectTransform)barGo.transform;
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 1f);
            barRect.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            barRect.anchoredPosition = Vector2.zero;

            var track = barGo.AddComponent<Image>();
            track.color = new Color(Parchment.r, Parchment.g, Parchment.b, 0.08f);

            var scrollbar = barGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var slidingAreaGo = new GameObject("SlidingArea", typeof(RectTransform));
            slidingAreaGo.transform.SetParent(barGo.transform, false);
            StretchFull((RectTransform)slidingAreaGo.transform);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(slidingAreaGo.transform, false);
            var handleRect = (RectTransform)handleGo.transform;
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = new Vector2(1f, 0.2f);
            handleRect.sizeDelta = Vector2.zero;
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = new Color(Amber.r, Amber.g, Amber.b, 0.65f);

            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            return scrollbar;
        }

        public static void SetChipSelected(Image background, TMP_Text text, bool selected)
        {
            if (selected)
            {
                background.color = new Color(Amber.r, Amber.g, Amber.b, 0.22f);
                text.color = Amber;
            }
            else
            {
                background.color = new Color(Parchment.r, Parchment.g, Parchment.b, 0.06f);
                text.color = Parchment;
            }
        }
    }
}
