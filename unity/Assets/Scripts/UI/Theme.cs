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
        public static readonly Color Muted = new Color(Parchment.r, Parchment.g, Parchment.b, 0.85f);
        // Мелкие служебные CAPS-подписи (заголовки полей) — минимально приглушены.
        public static readonly Color EyebrowMuted = new Color(Parchment.r, Parchment.g, Parchment.b, 0.68f);

        private static Font cachedFont;

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

        public static Font Font()
        {
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
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

        public static Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Font();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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

            var text = CreateText(go.transform, "Label", 20, TextAnchor.MiddleLeft, textColor);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18, 4);
            textRect.offsetMax = new Vector2(-18, -4);
            text.text = label;

            return button;
        }

        // Чип-переключатель (сфера/тон/уровень сложности в админ-конфиге) — пилюля,
        // которая явно показывает состояние "выбрано"/"не выбрано".
        public static Button CreateChip(Transform parent, string label, UnityEngine.Events.UnityAction onClick, out Image background, out Text text)
        {
            var go = new GameObject($"Chip_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            background = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            if (onClick != null) button.onClick.AddListener(onClick);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 44;
            layoutElement.minWidth = 120;

            text = CreateText(go.transform, "Label", 18, TextAnchor.MiddleCenter, Parchment);
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

        public static void SetChipSelected(Image background, Text text, bool selected)
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
