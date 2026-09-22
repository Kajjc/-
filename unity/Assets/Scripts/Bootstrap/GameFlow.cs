using System.Collections.Generic;
using Arena.Dialogue;
using Arena.UI;
using UnityEngine;

namespace Arena.Bootstrap
{
    // Оркестратор полного флоу:
    //   Тренировка   → автозапуск лёгкого сценария (без теста, без настройки)
    //   Тестирование → тест навыков (с пропуском) → настройка → сценарий
    //   Админ        → ручная настройка + редактируемые навыки
    // Далее → диалог → фидбек → (повтор или возврат к настройке).
    public class GameFlow : MonoBehaviour
    {
        private List<ScenarioData> library;
        private PlayerSkills playerSkills;
        private bool adminMode;
        private GameMode gameMode = GameMode.Training;

        public GameMode CurrentGameMode => gameMode;

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
            Theme.OnRequestMainMenu = ReturnToMainMenu;

            modeSelect.Show(OnTrainingSelected, OnAdminSelected, OnTestingSelected);
        }

        private void ReturnToMainMenu()
        {
            skillTest.Hide();
            adminConfig.Hide();
            dialogue.Hide();
            theory.Hide();
            modeSelect.Show(OnTrainingSelected, OnAdminSelected, OnTestingSelected);
        }

        // «Тренировка» — сразу в игру. Без теста, без экрана настройки:
        // автоподбор лёгкого сценария + дефолтные навыки 2/2/2.
        private void OnTrainingSelected()
        {
            adminMode = false;
            modeSelect.Hide();
            playerSkills = new PlayerSkills { napor = 2, empatiya = 2, logika = 2 };

            var scenario = PickTrainingScenario();
            dialogue.StartScenario(scenario, playerSkills, OnRequestNewScenario);
        }

        // Автоподбор: первый лёгкий сценарий в библиотеке; если такого нет —
        // просто первый. На практике это hr_salary_*_easy — мягкий вход для новичка.
        private ScenarioData PickTrainingScenario()
        {
            foreach (var s in library)
                if (s.meta.difficulty == 1)
                    return s;
            return library[0];
        }

        // «Тестирование» — тест навыков с возможностью пропустить.
        private void OnTestingSelected()
        {
            adminMode = false;
            modeSelect.Hide();
            skillTest.Show(
                onComplete: OnSkillsReadyFromTest,
                onSkip: () =>
                {
                    playerSkills = new PlayerSkills { napor = 2, empatiya = 2, logika = 2 };
                    adminConfig.Show(library, playerSkills, showSkillEditor: false, gameMode, OnConfigConfirmed);
                });
        }

        private void OnAdminSelected()
        {
            adminMode = true;
            modeSelect.Hide();
            adminConfig.Show(library, new PlayerSkills(), showSkillEditor: true, gameMode, OnConfigConfirmed);
        }

        private void OnSkillsReadyFromTest(PlayerSkills skills)
        {
            playerSkills = skills;
            adminConfig.Show(library, playerSkills, showSkillEditor: false, gameMode, OnConfigConfirmed);
        }

        private void OnConfigConfirmed(ScenarioData scenario, PlayerSkills skills, GameMode mode)
        {
            playerSkills = skills;
            gameMode = mode;
            dialogue.StartScenario(scenario, playerSkills, OnRequestNewScenario);
        }

        private void OnRequestNewScenario()
        {
            adminConfig.Show(library, playerSkills, showSkillEditor: adminMode, gameMode, OnConfigConfirmed);
        }

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