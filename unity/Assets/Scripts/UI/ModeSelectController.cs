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

            root = Theme.CreateCanvas(transform, "ModeSelectCanvas", showMenuButton: false);
            Theme.SetCanvasBackground(root, "Background/title");

            var logoGo = new GameObject("Logo", typeof(RectTransform));
            logoGo.transform.SetParent(root, false);
            var logoRect = (RectTransform)logoGo.transform;
            logoRect.anchorMin = new Vector2(0.32f, 0.51f);
            logoRect.anchorMax = new Vector2(0.68f, 0.95f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;
            var logoImage = logoGo.AddComponent<UnityEngine.UI.Image>();
            logoImage.sprite = Theme.TryLoadSprite("Icons/arena_logo");
            logoImage.preserveAspect = true;

            var title = Theme.CreateText(root, "Title", 40, TextAnchor.MiddleCenter, Theme.Parchment);
            title.text = "С чего начнём?";
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.1f, 0.36f);
            titleRect.anchorMax = new Vector2(0.9f, 0.58f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Theme.CreateOptionCard(root, 0.06f, 0.34f, 0.08f, 0.41f,
                "Тренировка",
                "Сразу к переговорам — без теста. Сценарий подберётся автоматически.",
                Theme.Sage,
                () => onTraining?.Invoke());

            Theme.CreateOptionCard(root, 0.36f, 0.64f, 0.08f, 0.41f,
                "Тестирование",
                "18 вопросов, ~2 минуты. Получи рекомендованный сценарий по навыкам.",
                Theme.Amber,
                () => onTesting?.Invoke());

            Theme.CreateOptionCard(root, 0.66f, 0.94f, 0.08f, 0.41f,
                "Настройки администратора",
                "Уровень навыков и параметры сценария вручную — для тренера.",
                Theme.Teal,
                () => onAdmin?.Invoke());
        }
    }
}