using System;
using UnityEngine;

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

            Theme.CreateOptionCard(root, 0.1f, 0.47f, 0.22f, 0.55f,
                "Тренировка",
                "Пройти тест навыков (18 вопросов, ~2 минуты) и сразу начать переговоры.",
                Theme.Teal,
                () => onTraining?.Invoke());

            Theme.CreateOptionCard(root, 0.53f, 0.9f, 0.22f, 0.55f,
                "Административный режим",
                "Задать уровень навыков и параметры сценария вручную — без теста, для тренера.",
                Theme.Amber,
                () => onAdmin?.Invoke());
        }
    }
}
