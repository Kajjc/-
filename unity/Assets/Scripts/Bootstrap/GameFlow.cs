using System.Collections.Generic;
using Arena.Dialogue;
using Arena.UI;
using UnityEngine;

namespace Arena.Bootstrap
{
    // Оркестратор полного флоу: выбор режима -> (тест навыков ИЛИ ручная настройка
    // навыков+сценария в админ-режиме) -> диалог -> фидбек -> (повтор того же сценария
    // или возврат к настройке для нового, в том же режиме). Один персистентный
    // GameObject держит все четыре экрана-контроллера и просто переключает, какой активен.
    public class GameFlow : MonoBehaviour
    {
        private List<ScenarioData> library;
        private PlayerSkills playerSkills;
        private bool adminMode;

        private ModeSelectController modeSelect;
        private SkillTestController skillTest;
        private AdminConfigController adminConfig;
        private DialogueUIController dialogue;
        private TheoryController theory;

        public void Begin()
        {
            library = ScenarioLibrary.LoadAll();
            if (library.Count == 0)
            {
                // К4 (docs/feature-hypotheses.md): раньше здесь был только
                // Debug.LogError и return — на билде без консоли (WebGL-демо для
                // жюри) это выглядело как чёрный/пустой экран без единой подсказки,
                // что пошло не так. Теперь ошибка видна прямо в игре.
                Debug.LogError("Не найдено ни одного сценария в Resources/Scenarios — движку нечего показывать.");
                ShowFatalError("Не удалось загрузить ни одного сценария.\n\nПроверьте файлы в Resources/Scenarios — возможно, один из них повреждён или папка пуста.");
                return;
            }

            modeSelect = gameObject.AddComponent<ModeSelectController>();
            skillTest = gameObject.AddComponent<SkillTestController>();
            adminConfig = gameObject.AddComponent<AdminConfigController>();
            dialogue = gameObject.AddComponent<DialogueUIController>();
            theory = gameObject.AddComponent<TheoryController>();

            dialogue.OnOpenTheory = encounteredIds => theory.Show(encounteredIds, () => dialogue.Root.SetActive(true));

            modeSelect.Show(OnTrainingSelected, OnAdminSelected);
        }

        private void OnTrainingSelected()
        {
            adminMode = false;
            modeSelect.Hide();
            skillTest.Show(OnSkillsReadyFromTest);
        }

        private void OnAdminSelected()
        {
            adminMode = true;
            modeSelect.Hide();
            adminConfig.Show(library, new PlayerSkills(), showSkillEditor: true, OnConfigConfirmed);
        }

        private void OnSkillsReadyFromTest(PlayerSkills skills)
        {
            playerSkills = skills;
            adminConfig.Show(library, playerSkills, showSkillEditor: false, OnConfigConfirmed);
        }

        private void OnConfigConfirmed(ScenarioData scenario, PlayerSkills skills)
        {
            playerSkills = skills;
            dialogue.StartScenario(scenario, playerSkills, OnRequestNewScenario);
        }

        private void OnRequestNewScenario()
        {
            adminConfig.Show(library, playerSkills, showSkillEditor: adminMode, OnConfigConfirmed);
        }

        // К4 (docs/feature-hypotheses.md): любое фатальное состояние на старте
        // (сейчас — пустая библиотека сценариев) показывает понятный текст вместо
        // тишины, чтобы демо не могло незаметно "зависнуть" на скрытой ошибке.
        private void ShowFatalError(string message)
        {
            var root = Theme.CreateCanvas(transform, "FatalErrorCanvas");
            var text = Theme.CreateText(root, "Message", 22, TextAnchor.MiddleCenter, Theme.Coral);
            text.text = message;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.1f, 0.35f);
            rect.anchorMax = new Vector2(0.9f, 0.65f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
