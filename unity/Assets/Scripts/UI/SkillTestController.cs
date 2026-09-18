using System;
using Arena.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.UI
{
    // Экран теста навыков (docs/skill-test.md, режим «Игрок»): 18 утверждений
    // в перемешанном порядке, 3-балльная шкала, на выходе — PlayerSkills.
    public class SkillTestController : MonoBehaviour
    {
        private RectTransform root;
        private Text progressText;
        private RectTransform progressFill;
        private RectTransform progressTrack;
        private Text statementText;
        private RectTransform answersContainer;

        private Action<PlayerSkills> onComplete;
        private int questionIndex;
        private int naporSum, empatiyaSum, logikaSum;

        public GameObject Root => root != null ? root.gameObject : null;

        public void Show(Action<PlayerSkills> onComplete)
        {
            this.onComplete = onComplete;
            questionIndex = 0;
            naporSum = 0;
            empatiyaSum = 0;
            logikaSum = 0;

            BuildUiIfNeeded();
            root.gameObject.SetActive(true);
            RenderQuestion();
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void BuildUiIfNeeded()
        {
            if (root != null) return;

            root = Theme.CreateCanvas(transform, "SkillTestCanvas");

            var panel = Theme.CreatePanel(root, "Content", new Color(0, 0, 0, 0));
            Theme.StretchFull(panel);

            var header = Theme.CreatePanel(panel, "Header", new Color(0, 0, 0, 0));
            header.anchorMin = new Vector2(0.06f, 0.86f);
            header.anchorMax = new Vector2(0.94f, 0.95f);
            header.offsetMin = Vector2.zero;
            header.offsetMax = Vector2.zero;

            var eyebrow = Theme.CreateText(header, "Eyebrow", 20, TextAnchor.MiddleLeft, Theme.Amber);
            eyebrow.text = "ТЕСТ НАВЫКОВ";
            var eyebrowRect = eyebrow.rectTransform;
            eyebrowRect.anchorMin = new Vector2(0f, 0f);
            eyebrowRect.anchorMax = new Vector2(0.5f, 1f);
            eyebrowRect.offsetMin = Vector2.zero;
            eyebrowRect.offsetMax = Vector2.zero;

            progressText = Theme.CreateText(header, "Progress", 18, TextAnchor.MiddleRight, Theme.Muted);
            var progressTextRect = progressText.rectTransform;
            progressTextRect.anchorMin = new Vector2(0.5f, 0f);
            progressTextRect.anchorMax = new Vector2(1f, 1f);
            progressTextRect.offsetMin = Vector2.zero;
            progressTextRect.offsetMax = Vector2.zero;

            progressTrack = Theme.CreatePanel(panel, "ProgressTrack", new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.16f));
            progressTrack.anchorMin = new Vector2(0.06f, 0.83f);
            progressTrack.anchorMax = new Vector2(0.94f, 0.845f);
            progressTrack.offsetMin = Vector2.zero;
            progressTrack.offsetMax = Vector2.zero;

            progressFill = Theme.CreatePanel(progressTrack, "ProgressFill", Theme.Amber);
            progressFill.anchorMin = new Vector2(0f, 0f);
            progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;

            var statementCard = Theme.CreatePanel(panel, "StatementCard", new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.05f));
            statementCard.anchorMin = new Vector2(0.12f, 0.42f);
            statementCard.anchorMax = new Vector2(0.88f, 0.75f);
            statementCard.offsetMin = Vector2.zero;
            statementCard.offsetMax = Vector2.zero;

            statementText = Theme.CreateText(statementCard, "Statement", 28, TextAnchor.MiddleCenter, Theme.Parchment);
            var statementRect = statementText.rectTransform;
            statementRect.anchorMin = new Vector2(0.06f, 0.1f);
            statementRect.anchorMax = new Vector2(0.94f, 0.9f);
            statementRect.offsetMin = Vector2.zero;
            statementRect.offsetMax = Vector2.zero;

            var answersGo = new GameObject("Answers", typeof(RectTransform));
            answersGo.transform.SetParent(panel, false);
            answersContainer = (RectTransform)answersGo.transform;
            answersContainer.anchorMin = new Vector2(0.12f, 0.15f);
            answersContainer.anchorMax = new Vector2(0.88f, 0.35f);
            answersContainer.offsetMin = Vector2.zero;
            answersContainer.offsetMax = Vector2.zero;
            var layout = answersGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
        }

        private void RenderQuestion()
        {
            foreach (Transform child in answersContainer)
                Destroy(child.gameObject);

            progressText.text = $"Вопрос {questionIndex + 1} из {SkillTestData.DisplayOrder.Length}";
            float fraction = (float)questionIndex / SkillTestData.DisplayOrder.Length;
            progressFill.anchorMax = new Vector2(fraction, 1f);

            int questionNumber = SkillTestData.DisplayOrder[questionIndex];
            var question = SkillTestData.Questions[questionNumber - 1];
            statementText.text = $"«{question.Text}»";

            for (int i = 0; i < SkillTestData.AnswerLabels.Length; i++)
            {
                int value = i + 1;
                Theme.CreateButton(
                    answersContainer,
                    $"{value}  {SkillTestData.AnswerLabels[i]}",
                    new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.05f),
                    Theme.Parchment,
                    () => OnAnswer(question.Skill, value));
            }
        }

        private void OnAnswer(string skill, int value)
        {
            switch (skill)
            {
                case "napor": naporSum += value; break;
                case "empatiya": empatiyaSum += value; break;
                case "logika": logikaSum += value; break;
            }

            questionIndex++;
            if (questionIndex >= SkillTestData.DisplayOrder.Length)
                Finish();
            else
                RenderQuestion();
        }

        private void Finish()
        {
            var skills = new PlayerSkills
            {
                napor = SkillTestData.LevelFromSum(naporSum),
                empatiya = SkillTestData.LevelFromSum(empatiyaSum),
                logika = SkillTestData.LevelFromSum(logikaSum)
            };
            Hide();
            onComplete?.Invoke(skills);
        }
    }
}
