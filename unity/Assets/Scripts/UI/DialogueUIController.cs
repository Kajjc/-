using System;
using System.Text;
using Arena.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.UI
{
    // Строит весь UI диалога кодом (без сцены/префабов) — минимизирует ручную
    // настройку в редакторе и риск сломанных ссылок при демо.
    public class DialogueUIController : MonoBehaviour
    {
        private DialogueEngine engine;
        private Action onRequestNewScenario;

        private RectTransform root;
        private RectTransform portrait;
        private Image portraitImage;
        private GameObject portraitTint;
        private Text opponentRoleText;
        private RectTransform pipsRow;
        private Text opponentText;
        private RectTransform optionsContainer;
        private RectTransform endPanel;
        private RectTransform outcomeBadge;
        private Text endTitleText;
        private Text endSummaryText;
        private RectTransform scoreRow;
        private RectTransform tipsColumn;

        public GameObject Root => root != null ? root.gameObject : null;

        public void StartScenario(ScenarioData scenario, PlayerSkills skills, Action onRequestNewScenario)
        {
            this.onRequestNewScenario = onRequestNewScenario;
            engine = new DialogueEngine(scenario, skills);
            BuildUiIfNeeded();
            root.gameObject.SetActive(true);
            UpdatePortraitAndBackground();
            Render();
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void BuildUiIfNeeded()
        {
            if (root != null) return;

            root = Theme.CreateCanvas(transform, "DialogueCanvas");

            // Портрет: пробует Resources/Portraits/<id сценария>.png (см. docs/team-plan.md);
            // если файла нет — градиентная плашка-заглушка вместо реального портрета.
            portrait = Theme.CreatePanel(root, "Portrait", Theme.Slate);
            portraitImage = portrait.GetComponent<Image>();
            portrait.anchorMin = new Vector2(0.05f, 0.82f);
            portrait.anchorMax = new Vector2(0.16f, 0.95f);
            portrait.offsetMin = Vector2.zero;
            portrait.offsetMax = Vector2.zero;
            var portraitTintRect = Theme.CreatePanel(portrait, "Tint", new Color(Theme.Teal.r, Theme.Teal.g, Theme.Teal.b, 0.5f));
            portraitTintRect.anchorMin = new Vector2(0f, 0f);
            portraitTintRect.anchorMax = new Vector2(1f, 0.5f);
            portraitTintRect.offsetMin = Vector2.zero;
            portraitTintRect.offsetMax = Vector2.zero;
            portraitTint = portraitTintRect.gameObject;

            opponentRoleText = Theme.CreateText(root, "OpponentRole", 18, TextAnchor.UpperLeft, Theme.Parchment);
            var roleRect = opponentRoleText.rectTransform;
            roleRect.anchorMin = new Vector2(0.18f, 0.88f);
            roleRect.anchorMax = new Vector2(0.6f, 0.95f);
            roleRect.offsetMin = Vector2.zero;
            roleRect.offsetMax = Vector2.zero;

            var pipsGo = new GameObject("Pips", typeof(RectTransform));
            pipsGo.transform.SetParent(root, false);
            pipsRow = (RectTransform)pipsGo.transform;
            pipsRow.anchorMin = new Vector2(0.6f, 0.86f);
            pipsRow.anchorMax = new Vector2(0.95f, 0.95f);
            pipsRow.offsetMin = Vector2.zero;
            pipsRow.offsetMax = Vector2.zero;
            var pipsLayout = pipsGo.AddComponent<HorizontalLayoutGroup>();
            pipsLayout.spacing = 18;
            pipsLayout.childAlignment = TextAnchor.MiddleRight;
            pipsLayout.childForceExpandWidth = false;
            pipsLayout.childForceExpandHeight = true;

            opponentText = Theme.CreateText(root, "OpponentLine", 26, TextAnchor.UpperLeft, Theme.Parchment);
            var opponentRect = opponentText.rectTransform;
            opponentRect.anchorMin = new Vector2(0.05f, 0.55f);
            opponentRect.anchorMax = new Vector2(0.95f, 0.8f);
            opponentRect.offsetMin = Vector2.zero;
            opponentRect.offsetMax = Vector2.zero;

            var optionsGo = new GameObject("Options", typeof(RectTransform));
            optionsGo.transform.SetParent(root, false);
            optionsContainer = (RectTransform)optionsGo.transform;
            optionsContainer.anchorMin = new Vector2(0.05f, 0.05f);
            optionsContainer.anchorMax = new Vector2(0.95f, 0.5f);
            optionsContainer.offsetMin = Vector2.zero;
            optionsContainer.offsetMax = Vector2.zero;
            var layout = optionsGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            BuildEndPanel();
        }

        private void BuildEndPanel()
        {
            endPanel = Theme.CreatePanel(root, "EndPanel", new Color(0.05f, 0.06f, 0.08f, 0.98f));
            Theme.StretchFull(endPanel);
            endPanel.gameObject.SetActive(false);

            outcomeBadge = Theme.CreatePanel(endPanel, "OutcomeBadge", Theme.Amber);
            outcomeBadge.anchorMin = new Vector2(0.06f, 0.84f);
            outcomeBadge.anchorMax = new Vector2(0.13f, 0.93f);
            outcomeBadge.offsetMin = Vector2.zero;
            outcomeBadge.offsetMax = Vector2.zero;

            endTitleText = Theme.CreateText(endPanel, "EndTitle", 30, TextAnchor.MiddleLeft, Theme.Parchment);
            var titleRect = endTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.16f, 0.84f);
            titleRect.anchorMax = new Vector2(0.94f, 0.93f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            endSummaryText = Theme.CreateText(endPanel, "EndSummary", 18, TextAnchor.UpperLeft, Theme.Muted);
            var summaryRect = endSummaryText.rectTransform;
            summaryRect.anchorMin = new Vector2(0.06f, 0.72f);
            summaryRect.anchorMax = new Vector2(0.94f, 0.83f);
            summaryRect.offsetMin = Vector2.zero;
            summaryRect.offsetMax = Vector2.zero;

            var scoreLabel = Theme.CreateText(endPanel, "ScoreLabel", 13, TextAnchor.MiddleLeft, Theme.EyebrowMuted);
            scoreLabel.text = "БАЛЛЫ ПО ТЕХНИКАМ";
            var scoreLabelRect = scoreLabel.rectTransform;
            scoreLabelRect.anchorMin = new Vector2(0.06f, 0.65f);
            scoreLabelRect.anchorMax = new Vector2(0.6f, 0.7f);
            scoreLabelRect.offsetMin = Vector2.zero;
            scoreLabelRect.offsetMax = Vector2.zero;

            var scoreRowGo = new GameObject("ScoreRow", typeof(RectTransform));
            scoreRowGo.transform.SetParent(endPanel, false);
            scoreRow = (RectTransform)scoreRowGo.transform;
            scoreRow.anchorMin = new Vector2(0.06f, 0.56f);
            scoreRow.anchorMax = new Vector2(0.94f, 0.64f);
            scoreRow.offsetMin = Vector2.zero;
            scoreRow.offsetMax = Vector2.zero;
            var scoreLayout = scoreRowGo.AddComponent<HorizontalLayoutGroup>();
            scoreLayout.spacing = 10;
            scoreLayout.childAlignment = TextAnchor.MiddleLeft;
            scoreLayout.childForceExpandWidth = false;
            scoreLayout.childForceExpandHeight = true;

            var tipsLabel = Theme.CreateText(endPanel, "TipsLabel", 13, TextAnchor.MiddleLeft, Theme.EyebrowMuted);
            tipsLabel.text = "НАД ЧЕМ ПОРАБОТАТЬ";
            var tipsLabelRect = tipsLabel.rectTransform;
            tipsLabelRect.anchorMin = new Vector2(0.06f, 0.49f);
            tipsLabelRect.anchorMax = new Vector2(0.6f, 0.54f);
            tipsLabelRect.offsetMin = Vector2.zero;
            tipsLabelRect.offsetMax = Vector2.zero;

            var tipsGo = new GameObject("Tips", typeof(RectTransform));
            tipsGo.transform.SetParent(endPanel, false);
            tipsColumn = (RectTransform)tipsGo.transform;
            tipsColumn.anchorMin = new Vector2(0.06f, 0.2f);
            tipsColumn.anchorMax = new Vector2(0.94f, 0.48f);
            tipsColumn.offsetMin = Vector2.zero;
            tipsColumn.offsetMax = Vector2.zero;
            var tipsLayout = tipsGo.AddComponent<VerticalLayoutGroup>();
            tipsLayout.spacing = 8;
            tipsLayout.childForceExpandWidth = true;
            tipsLayout.childForceExpandHeight = false;
            tipsLayout.childControlWidth = true;
            tipsLayout.childControlHeight = true;

            var restartButton = Theme.CreateButton(endPanel, "Пройти ещё раз", new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.08f), Theme.Parchment, RestartScenario);
            restartButton.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
            var restartRect = (RectTransform)restartButton.transform;
            restartRect.anchorMin = new Vector2(0.06f, 0.05f);
            restartRect.anchorMax = new Vector2(0.48f, 0.14f);
            restartRect.offsetMin = Vector2.zero;
            restartRect.offsetMax = Vector2.zero;

            var newScenarioButton = Theme.CreateButton(endPanel, "Другой сценарий", Theme.Amber, Theme.Navy, OnRequestNewScenario);
            newScenarioButton.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
            var newScenarioRect = (RectTransform)newScenarioButton.transform;
            newScenarioRect.anchorMin = new Vector2(0.52f, 0.05f);
            newScenarioRect.anchorMax = new Vector2(0.94f, 0.14f);
            newScenarioRect.offsetMin = Vector2.zero;
            newScenarioRect.offsetMax = Vector2.zero;
        }

        private void Render()
        {
            foreach (Transform child in optionsContainer)
                Destroy(child.gameObject);

            if (engine.IsTerminal)
            {
                endPanel.gameObject.SetActive(true);
                RenderEndScreen();
                return;
            }

            endPanel.gameObject.SetActive(false);
            opponentRoleText.text = engine.Scenario.meta.opponentRole;
            opponentText.text = engine.CurrentNode.opponentLine;
            RenderPips();

            foreach (var option in engine.CurrentNode.options)
            {
                bool available = engine.IsOptionAvailable(option);
                var label = available ? option.text : $"{option.text}   {engine.GetLockLabel(option)}";
                var fill = available
                    ? new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.06f)
                    : new Color(Theme.Coral.r, Theme.Coral.g, Theme.Coral.b, 0.08f);
                var textColor = available ? Theme.Parchment : new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.62f);
                var capturedOption = option;
                var button = Theme.CreateButton(optionsContainer, label, fill, textColor, () => OnOptionChosen(capturedOption));
                button.interactable = available;
                var layoutElement = button.gameObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = 52;
            }
        }

        // Ищет Resources/Portraits/<id>.png и Resources/Backgrounds/<id>.png по id
        // текущего сценария (docs/team-plan.md фиксирует эту конвенцию имён для
        // участника команды, который генерирует ассеты) — если файла нет, остаётся
        // текущая плашка-заглушка, ничего не ломается.
        private void UpdatePortraitAndBackground()
        {
            var id = engine.Scenario.meta.id;
            var portraitSprite = Theme.TryLoadSprite($"Portraits/{id}");
            if (portraitSprite != null)
            {
                portraitImage.sprite = portraitSprite;
                portraitImage.color = Color.white;
                portraitTint.SetActive(false);
            }
            else
            {
                portraitImage.sprite = null;
                portraitImage.color = Theme.Slate;
                portraitTint.SetActive(true);
            }

            Theme.SetCanvasBackground(root, $"Backgrounds/{id}");
        }

        private void RenderPips()
        {
            foreach (Transform child in pipsRow)
                Destroy(child.gameObject);

            AddPipGroup("Напор", "napor", engine.Skills.napor);
            AddPipGroup("Эмпатия", "empatiya", engine.Skills.empatiya);
            AddPipGroup("Логика", "logika", engine.Skills.logika);
        }

        private void AddPipGroup(string label, string skillId, int level)
        {
            var groupGo = new GameObject($"Pip_{skillId}", typeof(RectTransform));
            groupGo.transform.SetParent(pipsRow, false);
            var layout = groupGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            var sizeFit = groupGo.AddComponent<ContentSizeFitter>();
            sizeFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var accent = Theme.ForSkill(skillId);
            var labelText = Theme.CreateText(groupGo.transform, "Label", 13, TextAnchor.MiddleLeft, Theme.Muted);
            labelText.text = label;
            labelText.gameObject.AddComponent<LayoutElement>().minWidth = 60;

            for (int i = 1; i <= 3; i++)
            {
                var dotGo = new GameObject($"Dot{i}", typeof(RectTransform));
                dotGo.transform.SetParent(groupGo.transform, false);
                var dotLayout = dotGo.AddComponent<LayoutElement>();
                dotLayout.minWidth = 9;
                dotLayout.minHeight = 9;
                var image = dotGo.AddComponent<Image>();
                image.color = i <= level ? accent : new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.15f);
            }
        }

        private void OnOptionChosen(DialogueOption option)
        {
            engine.ChooseOption(option);
            Render();
        }

        private void RenderEndScreen()
        {
            var node = engine.CurrentNode;
            outcomeBadge.GetComponent<Image>().color = Theme.ForOutcome(node.outcome);
            endTitleText.text = OutcomeTitle(node.outcome);
            endSummaryText.text = node.summary;

            foreach (Transform child in scoreRow) Destroy(child.gameObject);
            foreach (var kv in engine.TechniqueScores)
            {
                bool positive = kv.Value > 0;
                var accent = positive ? Theme.Sage : Theme.Coral;
                var chipBg = Theme.CreatePanel(scoreRow, "Chip", new Color(accent.r, accent.g, accent.b, 0.16f));
                chipBg.gameObject.AddComponent<LayoutElement>().minWidth = 140;
                var chipText = Theme.CreateText(chipBg, "Label", 14, TextAnchor.MiddleCenter, accent);
                chipText.text = $"{kv.Key} {(positive ? "+" : "")}{kv.Value}";
                Theme.StretchFull(chipText.rectTransform);
            }

            foreach (Transform child in tipsColumn) Destroy(child.gameObject);
            bool anyTip = false;
            foreach (var step in engine.Transcript)
            {
                var opt = step.ChosenOption;
                if (opt.points <= 0 && !string.IsNullOrEmpty(opt.betterAlternative))
                {
                    anyTip = true;
                    var tip = Theme.CreateText(tipsColumn, "Tip", 15, TextAnchor.UpperLeft, new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.85f));
                    tip.text = $"Вместо «{opt.text}» → «{opt.betterAlternative}»";
                    var tipLayout = tip.gameObject.AddComponent<LayoutElement>();
                    tipLayout.minHeight = 40;
                }
            }
            if (!anyTip)
            {
                var tip = Theme.CreateText(tipsColumn, "Tip", 15, TextAnchor.UpperLeft, Theme.Sage);
                tip.text = "Явных слабых реплик не было.";
            }
        }

        private static string OutcomeTitle(string outcome)
        {
            switch (outcome)
            {
                case "win": return "Успех";
                case "compromise": return "Компромисс";
                case "fail": return "Провал";
                default: return outcome;
            }
        }

        private void RestartScenario()
        {
            StartScenario(engine.Scenario, engine.Skills, onRequestNewScenario);
        }

        private void OnRequestNewScenario()
        {
            Hide();
            onRequestNewScenario?.Invoke();
        }
    }
}
