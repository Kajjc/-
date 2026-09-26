using System;
using System.Collections.Generic;
using System.Linq;
using Arena.Bootstrap;
using Arena.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.UI
{
    // Экран настройки контекста (п.2 ТЗ, обязательная конфигурируемость): сфера/
    // сложность/тон реально фильтруют библиотеку сценариев через ScenarioLibrary.Pick,
    // а не просто украшают экран — превью снизу всегда показывает, что будет подобрано.
    // В административном режиме (docs/skill-test.md, "Режим «Администратор»") сюда же
    // добавляется прямой выбор уровня каждого навыка — без прохождения теста.
    public class AdminConfigController : MonoBehaviour
    {
        private const int DifficultyMax = 3;
        private const float FieldStep = 0.16f;
        private const float FieldTop = 0.77f;

        private static readonly string[] SkillIds = { "napor", "empatiya", "logika" };
        private static readonly string[] SkillLabels = { "Напор", "Эмпатия", "Логика" };

        private RectTransform root;
        private RectTransform toneRow;
        private TMP_Text rolePreviewText;
        private TMP_Text scenarioPreviewText;
        private TMP_Text difficultyValueText;

        private readonly List<(string value, Image bg, TMP_Text txt)> sphereChips = new List<(string, Image, TMP_Text)>();
        private readonly List<(string value, Image bg, TMP_Text txt)> toneChips = new List<(string, Image, TMP_Text)>();
        private readonly List<Image> difficultyDots = new List<Image>();
        private readonly Dictionary<string, List<(int level, Image bg, TMP_Text txt)>> skillButtons = new Dictionary<string, List<(int, Image, TMP_Text)>>();
        private readonly List<(GameMode mode, Image bg, TMP_Text txt)> gameModeButtons = new List<(GameMode, Image, TMP_Text)>();

        private List<ScenarioData> library;
        private bool showSkillEditor;
        private PlayerSkills workingSkills;
        private string selectedSphere;
        private string selectedTone;
        private int selectedDifficulty;
        private GameMode selectedGameMode;
        private ScenarioData previewScenario;
        private Action<ScenarioData, PlayerSkills, GameMode> onConfirmed;

        public GameObject Root => root != null ? root.gameObject : null;

        public void Show(List<ScenarioData> library, PlayerSkills skills, bool showSkillEditor, GameMode initialGameMode, Action<ScenarioData, PlayerSkills, GameMode> onConfirmed)
        {
            this.library = library;
            this.showSkillEditor = showSkillEditor;
            this.onConfirmed = onConfirmed;
            workingSkills = new PlayerSkills { napor = skills.napor, empatiya = skills.empatiya, logika = skills.logika };
            selectedGameMode = initialGameMode;

            selectedSphere = library[0].meta.sphere;
            selectedTone = library[0].meta.tone;
            selectedDifficulty = library[0].meta.difficulty;

            BuildUiIfNeeded();
            var logoGo = new GameObject("Logo", typeof(RectTransform));
            logoGo.transform.SetParent(root, false);
            var logoRect = (RectTransform)logoGo.transform;
            logoRect.anchorMin = new Vector2(0.38f, 0.32f);
            logoRect.anchorMax = new Vector2(0.62f, 0.68f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;
            var logoImage = logoGo.AddComponent<UnityEngine.UI.Image>();
            logoImage.sprite = Theme.TryLoadSprite("Icons/arena_white");
            logoImage.preserveAspect = true;
            RebuildToneChips();
            root.gameObject.SetActive(true);
            if (showSkillEditor) UpdateSkillButtonsVisual();
            UpdateGameModeButtonsVisual();
            UpdatePreview();
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void BuildUiIfNeeded()
        {
            if (root != null) return;

            root = Theme.CreateCanvas(transform, "AdminConfigCanvas");
            Theme.SetCanvasBackground(root, "Background/admin");
            var fullBackdrop = Theme.CreatePanel(root, "FullBackdrop", new Color(Theme.Navy.r, Theme.Navy.g, Theme.Navy.b, 0.25f));
            var adminPanel = Theme.CreatePanel(root, "AdminPanel", new Color(Theme.Navy.r, Theme.Navy.g, Theme.Navy.b, 0.25f));
            adminPanel.anchorMin = new Vector2(0.0f, 0.0f);
            adminPanel.anchorMax = new Vector2(1f, 1f);
            adminPanel.offsetMin = Vector2.zero;
            adminPanel.offsetMax = Vector2.zero;
            Theme.StretchFull(fullBackdrop);
            

            var eyebrow = Theme.CreateText(root, "Eyebrow", 28, TextAnchor.MiddleLeft, Theme.Teal);
            eyebrow.text = showSkillEditor ? "АДМИНИСТРАТОР — НАСТРОЙКА КЕЙСА" : "НАСТРОЙКА СЦЕНАРИЯ";
            var eyebrowRect = eyebrow.rectTransform;
            eyebrowRect.anchorMin = new Vector2(0.06f, 0.9f);
            eyebrowRect.anchorMax = new Vector2(0.7f, 0.97f);
            eyebrowRect.offsetMin = Vector2.zero;
            eyebrowRect.offsetMax = Vector2.zero;

            float y = FieldTop;

            if (showSkillEditor)
            {
                BuildSkillsRow(y);
                y -= FieldStep;
            }

            BuildChipRow("Сфера", DistinctInOrder(library.Select(s => s.meta.sphere)), y, sphereChips, v =>
            {
                selectedSphere = v;
            
                if (!library.Any(s => s.meta.sphere == selectedSphere && s.meta.tone == selectedTone))
                {
                    var firstToneForSphere = library.FirstOrDefault(s => s.meta.sphere == selectedSphere)?.meta.tone;
                    if (firstToneForSphere != null) selectedTone = firstToneForSphere;
                }
                RebuildToneChips();
                UpdatePreview();
            });
            y -= FieldStep;

            BuildDifficultyRow(y);
            BuildGameModeRow(y);
            y -= FieldStep;

            // Более высокая строка, чем у остальных чипов: "напористый / скептический"
            // при увеличенном шрифте переносится на 2 строки, и стандартных 0.06
            // высоты не хватает — текст вылезал бы за пределы плашки чипа. Сами чипы
            // не создаются здесь — только контейнер и подпись; набор тонов зависит от
            // selectedSphere и пересобирается в RebuildToneChips (вызывается из Show()
            // и при смене сферы), а не строится один раз на весь список тонов сразу.
            BuildFieldLabel("ТОН СОБЕСЕДНИКА", y);
            var toneRowGo = new GameObject("ТонСобеседникаRow", typeof(RectTransform));
            toneRowGo.transform.SetParent(root, false);
            toneRow = (RectTransform)toneRowGo.transform;
            toneRow.anchorMin = new Vector2(0.06f, y + 0.06f);
            toneRow.anchorMax = new Vector2(0.94f, y + 0.07f);
            toneRow.offsetMin = Vector2.zero;
            toneRow.offsetMax = Vector2.zero;
            var toneLayout = toneRowGo.AddComponent<HorizontalLayoutGroup>();
            toneLayout.spacing = 12;
            toneLayout.childForceExpandWidth = false;
            toneLayout.childForceExpandHeight = true;
            toneLayout.childControlWidth = true;
            toneLayout.childControlHeight = true;
            toneLayout.childAlignment = TextAnchor.MiddleLeft;
            var previewStrip = Theme.CreatePanel(root, "PreviewStrip", new Color(1f, 1f, 1f, 0.12f));

            previewStrip.anchorMin = new Vector2(0.06f, 0.06f);
            previewStrip.anchorMax = new Vector2(0.94f, 0.22f);
            previewStrip.offsetMin = Vector2.zero;
            previewStrip.offsetMax = Vector2.zero;

            scenarioPreviewText = Theme.CreateText(previewStrip, "ScenarioText", 22, TextAnchor.UpperLeft, Theme.Parchment);
            var previewTextRect = scenarioPreviewText.rectTransform;
            previewTextRect.anchorMin = new Vector2(0.03f, 0.52f);
            previewTextRect.anchorMax = new Vector2(0.68f, 0.94f);
            previewTextRect.offsetMin = Vector2.zero;
            previewTextRect.offsetMax = Vector2.zero;

            rolePreviewText = Theme.CreateText(previewStrip, "RoleText", 17, TextAnchor.UpperLeft, Theme.Muted);
            var roleTextRect = rolePreviewText.rectTransform;
            roleTextRect.anchorMin = new Vector2(0.03f, 0.06f);
            roleTextRect.anchorMax = new Vector2(0.68f, 0.5f);
            roleTextRect.offsetMin = Vector2.zero;
            roleTextRect.offsetMax = Vector2.zero;

            var ctaRect = Theme.CreatePanel(previewStrip, "CtaSlot", new Color(0, 0, 0, 0));
            ctaRect.anchorMin = new Vector2(0.7f, 0.15f);
            ctaRect.anchorMax = new Vector2(0.97f, 0.85f);
            ctaRect.offsetMin = Vector2.zero;
            ctaRect.offsetMax = Vector2.zero;
            var ctaButton = Theme.CreateButton(ctaRect, "Начать переговоры →", Theme.Amber, Theme.Navy, OnStartClicked);
            var ctaLabel = ctaButton.GetComponentInChildren<TMP_Text>();
            ctaLabel.alignment = TextAlignmentOptions.Center;
            Theme.StretchFull((RectTransform)ctaButton.transform);
        }

        private void BuildSkillsRow(float y)
        {
            BuildFieldLabel("НАВЫКИ ИГРОКА", y);

            var rowGo = new GameObject("SkillsRow", typeof(RectTransform));
            rowGo.transform.SetParent(root, false);
            var row = (RectTransform)rowGo.transform;
            row.anchorMin = new Vector2(0.06f, y);
            row.anchorMax = new Vector2(0.94f, y + 0.06f);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int s = 0; s < SkillIds.Length; s++)
            {
                var skillId = SkillIds[s];
                var groupGo = new GameObject($"Skill_{skillId}", typeof(RectTransform));
                groupGo.transform.SetParent(row, false);
                var groupLayout = groupGo.AddComponent<HorizontalLayoutGroup>();
                groupLayout.spacing = 6;
                groupLayout.childAlignment = TextAnchor.MiddleLeft;
                groupLayout.childForceExpandWidth = false;
                groupLayout.childForceExpandHeight = true;
                groupLayout.childControlWidth = true;
                groupLayout.childControlHeight = true;

                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(groupGo.transform, false);
                var iconLE = iconGo.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 50;
                iconLE.preferredHeight = 50;
                iconLE.minWidth = 50;
                iconLE.minHeight = 50;
                var iconImage = iconGo.AddComponent<Image>();
                var sprite = Theme.TryLoadSprite($"Icons/skill_{skillId}");
                if (sprite != null)
                {
                    iconImage.sprite = sprite;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
                else
                {
                    iconImage.color = Theme.ForSkill(skillId);
                }

                var labelText = Theme.CreateText(groupGo.transform, "Label", 20, TextAnchor.MiddleLeft, Theme.Muted);
                labelText.text = SkillLabels[s];
                labelText.gameObject.AddComponent<LayoutElement>().minWidth = 92;

                var buttons = new List<(int, Image, TMP_Text)>();
                for (int level = 1; level <= 3; level++)
                {
                    int capturedLevel = level;
                    string capturedSkillId = skillId;
                    var btnGo = new GameObject($"{skillId}_{level}", typeof(RectTransform));
                    btnGo.transform.SetParent(groupGo.transform, false);
                    btnGo.AddComponent<LayoutElement>().minWidth = 38;
                    var bg = btnGo.AddComponent<Image>();
                    var button = btnGo.AddComponent<Button>();
                    button.targetGraphic = bg;
                    button.onClick.AddListener(() => SetSkillLevel(capturedSkillId, capturedLevel));
                    var txt = Theme.CreateText(btnGo.transform, "Label", 18, TextAnchor.MiddleCenter, Theme.Parchment);
                    txt.text = level.ToString();
                    Theme.StretchFull(txt.rectTransform);
                    buttons.Add((level, bg, txt));
                }
                skillButtons[skillId] = buttons;
            }
        }

        private void SetSkillLevel(string skillId, int level)
        {
            switch (skillId)
            {
                case "napor": workingSkills.napor = level; break;
                case "empatiya": workingSkills.empatiya = level; break;
                case "logika": workingSkills.logika = level; break;
            }
            UpdateSkillButtonsVisual();
        }

        private void UpdateSkillButtonsVisual()
        {
            foreach (var skillId in SkillIds)
            {
                int current = workingSkills.GetLevel(skillId);
                var accent = Theme.ForSkill(skillId);
                foreach (var (level, bg, txt) in skillButtons[skillId])
                {
                    bool selected = level == current;
                    bg.color = selected ? accent : new Color(1f, 1f, 1f, 0.12f);
                    txt.color = selected ? Theme.Navy : Theme.Parchment;
                }
            }
        }

        private void BuildFieldLabel(string label, float y, float anchorMinX = 0.06f, float anchorMaxX = 0.7f)
        {
            var labelText = Theme.CreateText(root, $"{label}Label", 18, TextAnchor.MiddleLeft, Theme.EyebrowMuted);
            labelText.text = label;
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(anchorMinX, y + 0.075f);
            labelRect.anchorMax = new Vector2(anchorMaxX, y + 0.16f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        // Пользователь попросил разместить переключатель "Тренировка/Обучение"
        // именно на этом экране, в свободном месте справа от "СЛОЖНОСТЬ" (было
        // изначально сделано отдельным экраном перед тестом навыков, но это не
        // подошло — переключатель нужен здесь, на экране настройки кейса, где он
        // виден и админу, и обычному игроку после теста). Пока оба варианта ведут
        // по одинаковому пути — переключатель только запоминает выбор в GameFlow
        // через onConfirmed, реальная разница в поведении добавится позже.
        private void BuildGameModeRow(float y)
        {
            BuildFieldLabel("РЕЖИМ", y, anchorMinX: 0.73f, anchorMaxX: 0.94f);

            var rowGo = new GameObject("GameModeRow", typeof(RectTransform));
            rowGo.transform.SetParent(root, false);
            var row = (RectTransform)rowGo.transform;
            row.anchorMin = new Vector2(0.73f, y + 0.03f);
            row.anchorMax = new Vector2(0.94f, y + 0.06f + 0.03f);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            AddGameModeButton(rowGo.transform, "Тренировка", GameMode.Training);
            AddGameModeButton(rowGo.transform, "Обучение", GameMode.Learning);
        }

        private void AddGameModeButton(Transform parent, string label, GameMode mode)
        {
            var go = new GameObject($"Mode_{mode}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var bg = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() =>
            {
                selectedGameMode = mode;
                UpdateGameModeButtonsVisual();
            });
            var text = Theme.CreateText(go.transform, "Label", 17, TextAnchor.MiddleCenter, Theme.Parchment);
            text.text = label;
            Theme.StretchFull(text.rectTransform);
            gameModeButtons.Add((mode, bg, text));
        }

        private void UpdateGameModeButtonsVisual()
        {
            foreach (var (mode, bg, txt) in gameModeButtons)
            {
                bool selected = mode == selectedGameMode;
                bg.color = selected ? Theme.Amber : new Color(1f, 1f, 1f, 0.12f);
                txt.color = selected ? Theme.Navy : Theme.Parchment;
            }
        }

        private void BuildChipRow(string label, List<string> values, float y, List<(string, Image, TMP_Text)> registry, Action<string> onSelect, float rowHeight = 0.06f)
        {
            BuildFieldLabel(label.ToUpperInvariant(), y);

            var rowGo = new GameObject($"{label}Row", typeof(RectTransform));
            rowGo.transform.SetParent(root, false);
            var row = (RectTransform)rowGo.transform;
            row.anchorMin = new Vector2(0.06f, y + 0.03f);
            row.anchorMax = new Vector2(0.94f, y + 0.03f + rowHeight);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            foreach (var value in values)
            {
                var captured = value;
                Theme.CreateChip(row, value, () => SelectChip(registry, captured, onSelect), out var bg, out var text);
                registry.Add((value, bg, text));
            }
        }

        private void SelectChip(List<(string value, Image bg, TMP_Text txt)> registry, string selected, Action<string> onSelect)
        {
            foreach (var (value, bg, txt) in registry)
                Theme.SetChipSelected(bg, txt, value == selected);
            onSelect(selected);
        }

        // По просьбе пользователя (21.09): раньше список тонов строился один раз по
        // всей библиотеке сразу, поэтому для сферы с одним тоном показывались ещё три
        // чипа, которые ничего не меняли при клике (ScenarioLibrary.Pick всё равно
        // откатывался на единственный существующий сценарий сферы). Теперь список
        // всегда фильтруется по selectedSphere — сколько у сферы реально есть тонов,
        // столько чипов и показывается.
        private void RebuildToneChips()
        {
            foreach (Transform child in toneRow) Destroy(child.gameObject);
            toneChips.Clear();

            var tonesForSphere = DistinctInOrder(library.Where(s => s.meta.sphere == selectedSphere).Select(s => s.meta.tone));
            foreach (var value in tonesForSphere)
            {
                var captured = value;
                Theme.CreateChip(toneRow, value, () => SelectChip(toneChips, captured, v =>
                {
                    selectedTone = v;
                    UpdatePreview();
                }), out var bg, out var text);
                toneChips.Add((value, bg, text));
            }
        }

        private void BuildDifficultyRow(float y)
        {
            BuildFieldLabel("СЛОЖНОСТЬ", y);

            var rowGo = new GameObject("DifficultyRow", typeof(RectTransform));
            rowGo.transform.SetParent(root, false);
            var difficultyRow = (RectTransform)rowGo.transform;
            difficultyRow.anchorMin = new Vector2(0.06f, y + 0.03f);
            difficultyRow.anchorMax = new Vector2(0.4f, y + 0.06f + 0.03f);
            difficultyRow.offsetMin = Vector2.zero;
            difficultyRow.offsetMax = Vector2.zero;
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            for (int i = 1; i <= DifficultyMax; i++)
            {
                int value = i;
                var dotGo = new GameObject($"Dot{i}", typeof(RectTransform));
                dotGo.transform.SetParent(difficultyRow, false);
                var dotLayout = dotGo.AddComponent<LayoutElement>();
                dotLayout.minWidth = 26;
                dotLayout.minHeight = 18;
                var dotImage = dotGo.AddComponent<Image>();
                var button = dotGo.AddComponent<Button>();
                button.targetGraphic = dotImage;
                button.onClick.AddListener(() =>
                {
                    selectedDifficulty = value;
                    UpdatePreview();
                });
                difficultyDots.Add(dotImage);
            }
        }

        private void UpdatePreview()
        {
            foreach (var (value, bg, txt) in sphereChips) Theme.SetChipSelected(bg, txt, value == selectedSphere);
            foreach (var (value, bg, txt) in toneChips) Theme.SetChipSelected(bg, txt, value == selectedTone);
            for (int i = 0; i < difficultyDots.Count; i++)
                difficultyDots[i].color = (i + 1) <= selectedDifficulty
                    ? Theme.Amber
                    : new Color(1f, 1f, 1f, 0.12f);
            if (difficultyValueText != null) difficultyValueText.text = $"{selectedDifficulty} из {DifficultyMax}";

            previewScenario = ScenarioLibrary.Pick(library, selectedSphere, selectedDifficulty, selectedTone);
            if (previewScenario == null) return;

            scenarioPreviewText.text = $"{previewScenario.meta.sphere} — {previewScenario.meta.topic}";
            rolePreviewText.text = Theme.Capitalize(previewScenario.meta.opponentRole);
        }

        private void OnStartClicked()
        {
            if (previewScenario == null) return;
            Hide();
            onConfirmed?.Invoke(previewScenario, workingSkills, selectedGameMode);
        }

        private static List<string> DistinctInOrder(IEnumerable<string> values)
        {
            var seen = new HashSet<string>();
            var result = new List<string>();
            foreach (var v in values)
                if (seen.Add(v)) result.Add(v);
            return result;
        }
    }
}
