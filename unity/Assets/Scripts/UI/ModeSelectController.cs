using System;
using UnityEngine;

namespace Arena.UI
{
    // Главное меню: три входа — Тренировка (сразу в игру), Тестирование
    // (тест → рекомендация → игра), Настройки администратора (ручная настройка).
    public class ModeSelectController : MonoBehaviour
    {
        private RectTransform root;
        private Action onTraining;
        private Action onTesting;
        private Action onAdmin;

        public GameObject Root => root != null ? root.gameObject : null;

        // onTesting опционально — существующие вызовы Show(a, b) продолжают работать,
        // просто карточка «Тестирование» будет вести по тому же пути, что Тренировка.
        public void Show(Action onTraining, Action onAdmin, Action onTesting = null)
        {
            this.onTraining = onTraining;
            this.onTesting = onTesting ?? onTraining;
            this.onAdmin = onAdmin;
            BuildUiIfNeeded();
            root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void BuildUiIfNeeded()
        {
            if (root != null) return;

            root = Theme.CreateCanvas(transform, "ModeSelectCanvas", showMenuButton: true);
            Theme.SetCanvasBackground(root, "Backgrounds/title", scrimAlpha: 0.2f);

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

            // Три карточки: каждая ~28% ширины, промежутки по 2%.
            Theme.CreateOptionCard(root, 0.06f, 0.34f, 0.22f, 0.55f,
                "Тренировка",
                "Сразу к переговорам — без теста. Сценарий подберётся автоматически.",
                Theme.Sage,
                () => onTraining?.Invoke());

            Theme.CreateOptionCard(root, 0.36f, 0.64f, 0.22f, 0.55f,
                "Тестирование",
                "18 вопросов, ~2 минуты. Получи рекомендованный сценарий по навыкам.",
                Theme.Amber,
                () => onTesting?.Invoke());

            Theme.CreateOptionCard(root, 0.66f, 0.94f, 0.22f, 0.55f,
                "Настройки администратора",
                "Уровень навыков и параметры сценария вручную — для тренера.",
                Theme.Teal,
                () => onAdmin?.Invoke());
        }
    }
}