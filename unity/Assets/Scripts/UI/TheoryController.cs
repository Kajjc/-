using System;
using System.Collections.Generic;
using System.Linq;
using Arena.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.UI
{
    // Экран-справочник по переговорным техникам (гипотеза Г5, docs/feature-hypotheses.md):
    // контент берётся из Resources/TechniqueTaxonomy.json (theory_note_ru/theory_source),
    // ничего не генерируется на лету. Открывается с экрана итога — техники, которые
    // реально встретились в пройденном раунде, поднимаются в начало списка и подсвечены.
    public class TheoryController : MonoBehaviour
    {
        private RectTransform root;
        private RectTransform content;

        public GameObject Root => root != null ? root.gameObject : null;

        public void Show(IEnumerable<string> encounteredIds, Action onBack)
        {
            BuildUiIfNeeded(onBack);
            root.gameObject.SetActive(true);
            Populate(new HashSet<string>(encounteredIds ?? Enumerable.Empty<string>()));
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void BuildUiIfNeeded(Action onBack)
        {
            if (root != null) return;

            root = Theme.CreateCanvas(transform, "TheoryCanvas");

            var eyebrow = Theme.CreateText(root, "Eyebrow", 18, TextAnchor.MiddleLeft, Theme.Teal);
            eyebrow.text = "ТЕОРИЯ И ТЕХНИКИ";
            var eyebrowRect = eyebrow.rectTransform;
            eyebrowRect.anchorMin = new Vector2(0.06f, 0.9f);
            eyebrowRect.anchorMax = new Vector2(0.6f, 0.97f);
            eyebrowRect.offsetMin = Vector2.zero;
            eyebrowRect.offsetMax = Vector2.zero;

            var backButton = Theme.CreateButton(
                root, "← Назад",
                new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.08f),
                Theme.Parchment,
                () =>
                {
                    Hide();
                    onBack?.Invoke();
                });
            backButton.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
            var backRect = (RectTransform)backButton.transform;
            backRect.anchorMin = new Vector2(0.78f, 0.9f);
            backRect.anchorMax = new Vector2(0.94f, 0.97f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            var listRoot = Theme.CreateScrollList(root, "List", out content);
            listRoot.anchorMin = new Vector2(0.06f, 0.05f);
            listRoot.anchorMax = new Vector2(0.94f, 0.86f);
            listRoot.offsetMin = Vector2.zero;
            listRoot.offsetMax = Vector2.zero;
        }

        private void Populate(HashSet<string> encountered)
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);

            var taxonomy = TechniqueTaxonomyLibrary.Load();
            var ordered = taxonomy.techniques
                .OrderByDescending(t => encountered.Contains(t.id))
                .ThenBy(t => t.ru_name);

            foreach (var technique in ordered)
                CreateCard(technique, encountered.Contains(technique.id));
        }

        private void CreateCard(TechniqueInfo technique, bool isHighlighted)
        {
            var backgroundColor = isHighlighted
                ? new Color(Theme.Amber.r, Theme.Amber.g, Theme.Amber.b, 0.14f)
                : new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.05f);
            var card = Theme.CreatePanel(content, $"Card_{technique.id}", backgroundColor);

            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 14, 14);
            cardLayout.spacing = 6;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var titleText = isHighlighted
                ? $"{technique.ru_name}   ·   встретилось в этом прохождении"
                : technique.ru_name;
            var title = Theme.CreateText(card, "Title", 19, TextAnchor.UpperLeft, isHighlighted ? Theme.Amber : Theme.Parchment);
            title.text = titleText;

            var definition = Theme.CreateText(card, "Definition", 15, TextAnchor.UpperLeft, Theme.Muted);
            definition.text = technique.definition_ru;

            var theory = Theme.CreateText(card, "Theory", 15, TextAnchor.UpperLeft, Theme.Muted);
            theory.text = $"{technique.theory_note_ru} — {technique.theory_source}";
        }
    }
}
