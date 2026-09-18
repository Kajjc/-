using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.UI
{
    // Первый экран флоу: кто сейчас за экраном (docs/skill-test.md, "Режим «Игрок»"
    // vs "Режим «Администратор»"). Тренировка ведёт к тесту навыков (навыки задаёт
    // сам обучаемый). Административный режим — прямой выбор уровня по каждому навыку
    // и параметров сценария вручную, без теста (например, тренер готовит конкретный
    // кейс под конкретного обучаемого).
    public class ModeSelectController : MonoBehaviour
    {
        private RectTransform root;

        public GameObject Root => root != null ? root.gameObject : null;

        public void Show(Action onTraining, Action onAdmin)
        {
            BuildUiIfNeeded(onTraining, onAdmin);
            root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void BuildUiIfNeeded(Action onTraining, Action onAdmin)
        {
            if (root != null) return;

            root = Theme.CreateCanvas(transform, "ModeSelectCanvas");
            Theme.SetCanvasBackground(root, "Backgrounds/title");

            var eyebrow = Theme.CreateText(root, "Eyebrow", 20, TextAnchor.MiddleCenter, Theme.Amber);
            eyebrow.text = "АРЕНА ПЕРЕГОВОРОВ";
            var eyebrowRect = eyebrow.rectTransform;
            eyebrowRect.anchorMin = new Vector2(0.1f, 0.68f);
            eyebrowRect.anchorMax = new Vector2(0.9f, 0.75f);
            eyebrowRect.offsetMin = Vector2.zero;
            eyebrowRect.offsetMax = Vector2.zero;

            var title = Theme.CreateText(root, "Title", 30, TextAnchor.MiddleCenter, Theme.Parchment);
            title.text = "С чего начнём?";
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.1f, 0.58f);
            titleRect.anchorMax = new Vector2(0.9f, 0.68f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            BuildOption(
                anchorMinX: 0.1f, anchorMaxX: 0.47f,
                title: "Тренировка",
                subtitle: "Пройти тест навыков (18 вопросов, ~2 минуты) и сразу начать переговоры.",
                accent: Theme.Teal,
                onClick: onTraining);

            BuildOption(
                anchorMinX: 0.53f, anchorMaxX: 0.9f,
                title: "Административный режим",
                subtitle: "Задать уровень навыков и параметры сценария вручную — без теста, для тренера.",
                accent: Theme.Amber,
                onClick: onAdmin);
        }

        private void BuildOption(float anchorMinX, float anchorMaxX, string title, string subtitle, Color accent, Action onClick)
        {
            var card = Theme.CreatePanel(root, $"Option_{title}", new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.05f));
            card.anchorMin = new Vector2(anchorMinX, 0.22f);
            card.anchorMax = new Vector2(anchorMaxX, 0.55f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            var accentBar = Theme.CreatePanel(card, "AccentBar", accent);
            accentBar.anchorMin = new Vector2(0f, 0.92f);
            accentBar.anchorMax = new Vector2(1f, 1f);
            accentBar.offsetMin = Vector2.zero;
            accentBar.offsetMax = Vector2.zero;

            var titleText = Theme.CreateText(card, "Title", 24, TextAnchor.UpperLeft, Theme.Parchment);
            titleText.text = title;
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.08f, 0.7f);
            titleRect.anchorMax = new Vector2(0.92f, 0.9f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var subtitleText = Theme.CreateText(card, "Subtitle", 19, TextAnchor.UpperLeft, Theme.Muted);
            subtitleText.text = subtitle;
            var subtitleRect = subtitleText.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.08f, 0.1f);
            subtitleRect.anchorMax = new Vector2(0.92f, 0.62f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
