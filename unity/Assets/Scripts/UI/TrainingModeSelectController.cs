using System;
using Arena.Bootstrap;
using UnityEngine;

namespace Arena.UI
{
    // Второй экран флоу, сразу после выбора "Тренировка" на ModeSelectController.
    // Пока оба варианта ведут по абсолютно одинаковому пути (тест навыков -> диалог
    // -> фидбек) — разница только в выбранном GameMode, который GameFlow запоминает
    // для будущих обучающих функций, ещё не определённых (запрос пользователя, 21.09).
    public class TrainingModeSelectController : MonoBehaviour
    {
        private RectTransform root;

        public GameObject Root => root != null ? root.gameObject : null;

        public void Show(Action<GameMode> onChosen)
        {
            BuildUiIfNeeded(onChosen);
            root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void BuildUiIfNeeded(Action<GameMode> onChosen)
        {
            if (root != null) return;

            root = Theme.CreateCanvas(transform, "TrainingModeSelectCanvas");
            Theme.SetCanvasBackground(root, "Backgrounds/title");

            var title = Theme.CreateText(root, "Title", 30, TextAnchor.MiddleCenter, Theme.Parchment);
            title.text = "Тренировка или обучение?";
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.1f, 0.58f);
            titleRect.anchorMax = new Vector2(0.9f, 0.68f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Theme.CreateOptionCard(root, 0.1f, 0.47f, 0.22f, 0.55f,
                "Тренировка",
                "Тест навыков и сразу переговоры — обычный режим, как и раньше.",
                Theme.Teal,
                () => onChosen?.Invoke(GameMode.Training));

            Theme.CreateOptionCard(root, 0.53f, 0.9f, 0.22f, 0.55f,
                "Обучение",
                "Тот же тренажёр — дополнительные обучающие материалы появятся здесь позже.",
                Theme.Amber,
                () => onChosen?.Invoke(GameMode.Learning));
        }
    }
}
