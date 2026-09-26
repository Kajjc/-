using System;
using System.Collections.Generic;
using Arena.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.UI
{
    // Экран после теста навыков: показывает результат (3 навыка) и
    // рекомендованный сценарий по уровню сложности, соответствующему навыкам.
    // Кнопка «Настроить» ведёт в обычный админ-экран с уже подставленными навыками.
    public class TestResultController : MonoBehaviour
    {
        private RectTransform root;
        private Action<ScenarioData, PlayerSkills> onStart;
        private Action<PlayerSkills> onConfigure;

        public GameObject Root => root != null ? root.gameObject : null;

        public void Show(PlayerSkills skills, List<ScenarioData> library,
                         Action<ScenarioData, PlayerSkills> onStart,
                         Action<PlayerSkills> onConfigure)
        {
            this.onStart = onStart;
            this.onConfigure = onConfigure;

            var recommended = PickRecommended(skills, library);
            BuildUiIfNeeded();
            Render(skills, recommended);
            root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        // Автоподбор: считаем, сколько навыков ≥2, и берём сценарий этой сложности.
        // 0–1 сильных навыков → easy (1), 2 → medium (2), 3 → hard (3).
        // Fallback на первый в библиотеке, если ничего не нашли.
        private static ScenarioData PickRecommended(PlayerSkills skills, List<ScenarioData> library)
        {
            int difficulty = 1;
            if (skills.napor >= 2) difficulty++;
            if (skills.empatiya >= 2) difficulty++;
            if (skills.logika >= 2) difficulty++;
            difficulty = Mathf.Clamp(difficulty, 1, 3);

            foreach (var s in library)
                if (s.meta.difficulty == difficulty)
                    return s;
            return library[0];
        }

        private void BuildUiIfNeeded()
        {
            if (root != null) return;

            root = Theme.CreateCanvas(transform, "TestResultCanvas");

            var eyebrow = Theme.CreateText(root, "Eyebrow", 28, TextAnchor.MiddleCenter, Theme.Amber);
            eyebrow.text = "РЕЗУЛЬТАТ ТЕСТА";
            var eyebrowRect = eyebrow.rectTransform;
            eyebrowRect.anchorMin = new Vector2(0.1f, 0.86f);
            eyebrowRect.anchorMax = new Vector2(0.9f, 0.94f);
            eyebrowRect.offsetMin = Vector2.zero;
            eyebrowRect.offsetMax = Vector2.zero;

            var title = Theme.CreateText(root, "Title", 26, TextAnchor.MiddleCenter, Theme.Parchment);
            title.text = "Твои навыки";
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.1f, 0.76f);
            titleRect.anchorMax = new Vector2(0.9f, 0.85f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
        }

        private void Render(PlayerSkills skills, ScenarioData recommended)
        {
            // Удаляем предыдущие ряды при повторном вызове
            var oldRow = root.Find("SkillsRow");
            if (oldRow != null) Destroy(oldRow.gameObject);
            var oldRec = root.Find("RecommendedCard");
            if (oldRec != null) Destroy(oldRec.gameObject);

            // --- Три чипа с уровнями навыков ---
            var rowGo = new GameObject("SkillsRow", typeof(RectTransform));
            rowGo.transform.SetParent(root, false);
            var row = (RectTransform)rowGo.transform;
            row.anchorMin = new Vector2(0.15f, 0.52f);
            row.anchorMax = new Vector2(0.85f, 0.74f);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            AddSkillChip(row, "Напор", "napor", skills.napor, Theme.ForSkill("napor"));
            AddSkillChip(row, "Эмпатия", "empatiya", skills.empatiya, Theme.ForSkill("empatiya"));
            AddSkillChip(row, "Логика", "logika", skills.logika, Theme.ForSkill("logika"));

            // --- Рекомендованная карточка ---
            var card = Theme.CreatePanel(root, "RecommendedCard",
                new Color(Theme.Amber.r, Theme.Amber.g, Theme.Amber.b, 0.12f));
            card.anchorMin = new Vector2(0.1f, 0.20f);
            card.anchorMax = new Vector2(0.9f, 0.48f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            var recLabel = Theme.CreateText(card, "RecLabel", 18, TextAnchor.MiddleLeft, Theme.Amber);
            recLabel.text = "РЕКОМЕНДУЕМ";
            var recLabelRect = recLabel.rectTransform;
            recLabelRect.anchorMin = new Vector2(0.05f, 0.72f);
            recLabelRect.anchorMax = new Vector2(0.95f, 0.92f);
            recLabelRect.offsetMin = Vector2.zero;
            recLabelRect.offsetMax = Vector2.zero;

            var recTitle = Theme.CreateText(card, "RecTitle", 24, TextAnchor.MiddleLeft, Theme.Parchment);
            recTitle.text = $"{recommended.meta.sphere} — {recommended.meta.topic}";
            var recTitleRect = recTitle.rectTransform;
            recTitleRect.anchorMin = new Vector2(0.05f, 0.42f);
            recTitleRect.anchorMax = new Vector2(0.95f, 0.70f);
            recTitleRect.offsetMin = Vector2.zero;
            recTitleRect.offsetMax = Vector2.zero;

            var recRole = Theme.CreateText(card, "RecRole", 17, TextAnchor.MiddleLeft, Theme.Muted);
            recRole.text = $"Оппонент: {Theme.Capitalize(recommended.meta.opponentRole)}";
            var recRoleRect = recRole.rectTransform;
            recRoleRect.anchorMin = new Vector2(0.05f, 0.08f);
            recRoleRect.anchorMax = new Vector2(0.95f, 0.38f);
            recRoleRect.offsetMin = Vector2.zero;
            recRoleRect.offsetMax = Vector2.zero;

            // --- Кнопки ---
            var startBtn = Theme.CreateButton(root, "Начать переговоры →",
                Theme.Amber, Theme.Navy,
                () => { Hide(); onStart?.Invoke(recommended, skills); });
            var startRect = (RectTransform)startBtn.transform;
            startRect.anchorMin = new Vector2(0.52f, 0.06f);
            startRect.anchorMax = new Vector2(0.88f, 0.15f);
            startRect.offsetMin = Vector2.zero;
            startRect.offsetMax = Vector2.zero;

            var configBtn = Theme.CreateButton(root, "Настроить сценарий",
                new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.06f),
                Theme.Muted,
                () => { Hide(); onConfigure?.Invoke(skills); });
            var configRect = (RectTransform)configBtn.transform;
            configRect.anchorMin = new Vector2(0.12f, 0.06f);
            configRect.anchorMax = new Vector2(0.48f, 0.15f);
            configRect.offsetMin = Vector2.zero;
            configRect.offsetMax = Vector2.zero;
        }

        private void AddSkillChip(Transform parent, string label, string skillId, int level, Color accent)
        {
            var go = new GameObject($"Chip_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(accent.r, accent.g, accent.b, 0.18f);

            // Иконка навыка сверху
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = new Vector2(0.35f, 0.52f);
            iconRect.anchorMax = new Vector2(0.65f, 0.94f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
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
                iconImage.color = accent;
            }

            var txt = Theme.CreateText(go.transform, "Label", 22, TextAnchor.UpperCenter, accent);
            txt.text = $"{label}\n{level}";
            var txtRect = txt.rectTransform;
            txtRect.anchorMin = new Vector2(0f, 0.04f);
            txtRect.anchorMax = new Vector2(1f, 0.52f);
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
        }
    }
}