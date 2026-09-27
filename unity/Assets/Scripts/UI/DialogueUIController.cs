using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arena.Dialogue;
using TMPro;
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

        // Гипотеза Г8 (docs/feature-hypotheses.md): этот контроллер не пересоздаётся
        // между "Пройти ещё раз"/"Другой сценарий" (только engine пересоздаётся в
        // StartScenario), поэтому один и тот же профиль естественно копит данные по
        // всем прогонам за сессию, без отдельного хранилища на уровне GameFlow.
        private readonly NegotiatorProfile profile = new NegotiatorProfile();

        // Гипотеза Г5 (docs/feature-hypotheses.md) — экран итога передаёт сюда теги
        // техник, реально встретившихся в прохождении; связывает GameFlow.
        public Action<IEnumerable<string>> OnOpenTheory;

        private RectTransform root;
        private RectTransform portrait;
        private Image portraitImage;
        private GameObject portraitTint;
        private Image moodBarImage;
        private Image moodDotImage;
        private TMP_Text moodText;
        private TMP_Text opponentRoleText;
        private RectTransform pipsRow;
        private TMP_Text opponentText;
        private RectTransform optionsContainer;
        private RectTransform endPanel;
        private RectTransform outcomeBadge;
        private TMP_Text endTitleText;
        private RectTransform endContent;

        // Гипотеза К2 (docs/feature-hypotheses.md): плавный fade между репликами
        // вместо мгновенной подмены текста — интерфейс ощущается более "живым".
        private CanvasGroup opponentTextGroup;
        private CanvasGroup optionsGroup;
        private bool isTransitioning;
        private const float FadeDuration = 0.15f;

        // Гипотеза "голос оппонента": печать реплики по буквам + блип-звук на
        // каждое слово (см. TypeOpponentLine, OpponentVoice.cs). Скорость
        // откалибрована по реальной длине реплик в сценариях (docs/, медиана —
        // 99 символов/16 слов, p90 — 157/26, редкие выбросы до 484 символов) —
        // не быстрее и не медленнее, чем читается вслух в разумном темпе, с
        // потолком общей длительности для редких очень длинных реплик.
        private OpponentVoice voice;
        private Coroutine typeCoroutine;
        private const float CharsPerSecond = 50f;
        private const float MaxTypeDuration = 4.5f;

        // Шапка диалога: портрет слева, текстовая колонка справа. Пропорции арта 1.2:1
        // (scripts/prepare_portrait.py) — при смене там менять и здесь.
        private const float PortraitLeft = 0.05f;
        private const float PortraitRight = 0.215f;
        private const float PortraitTop = 0.955f;
        private const float PortraitAspect = 1.2f;
        private const float TextColumnLeft = 0.235f;
        // Правый край роли/подписи настроения и левый край полосы навыков (иконка +
        // название + пипсы на каждый навык — она шире прежней полосы без иконок).
        // Между ними зазор 0.02, чтобы длинная роль не упиралась в иконки.
        private const float TextColumnRight = 0.61f;
        private const float PipsLeft = 0.63f;
        private const float OutcomeIconSize = 72f;

        // Гипотеза Ю3 (docs/feature-hypotheses.md): реплики продублированы цифрами
        // и доступны с клавиатуры — быстрее и увереннее на питч-сессии, чем клики
        // мышью. Список в том же порядке, что и кнопки на экране.
        private readonly List<DialogueOption> currentOptions = new List<DialogueOption>();

        public GameObject Root => root != null ? root.gameObject : null;

        public void StartScenario(ScenarioData scenario, PlayerSkills skills, Action onRequestNewScenario)
        {
            this.onRequestNewScenario = onRequestNewScenario;
            engine = new DialogueEngine(scenario, skills);
            BuildUiIfNeeded();
            root.gameObject.SetActive(true);
            UpdateBackground();
            Render();
        }

        public void Hide()
        {
            // Иначе печать реплики (и блипы голоса) продолжались бы в фоне после
            // ухода с экрана диалога (например, по кнопке "Меню" посреди фразы) —
            // Canvas скрывается, а корутина на этом же MonoBehaviour живёт дальше.
            if (typeCoroutine != null)
            {
                StopCoroutine(typeCoroutine);
                typeCoroutine = null;
            }
            if (root != null) root.gameObject.SetActive(false);
        }

        private void BuildUiIfNeeded()
        {
            if (root != null) return;

            voice = gameObject.AddComponent<OpponentVoice>();

            root = Theme.CreateCanvas(transform, "DialogueCanvas");

            // Портрет: Resources/Portraits/<домен>_<настроение>.png (см. docs/team-plan.md);
            // если файла нет — плашка-заглушка вместо реального портрета.
            //
            // Раскладка шапки: портрет — левая колонка, а вся текстовая колонка (роль,
            // настроение, реплика оппонента) стоит правее него. Ширина портрета — доля
            // канваса, а высоту выводит из неё AspectRatioFitter по пропорциям арта.
            // Раньше слот задавался процентами по обеим осям, и в окне не 16:10 (например,
            // в очень широком окне Game в редакторе, ~2.4:1) он растягивался вместе с
            // картинкой — лица выходили сплюснутыми. Реплика оппонента раньше начиналась
            // под портретом и при более высоком портрете наползала бы на него; теперь она
            // правее портрета целиком, поэтому пересечение невозможно при любой форме окна.
            portrait = Theme.CreatePanel(root, "Portrait", Theme.Slate);
            portraitImage = portrait.GetComponent<Image>();
            portrait.anchorMin = new Vector2(PortraitLeft, PortraitTop);
            portrait.anchorMax = new Vector2(PortraitRight, PortraitTop);
            portrait.pivot = new Vector2(0.5f, 1f);
            portrait.offsetMin = Vector2.zero;
            portrait.offsetMax = Vector2.zero;
            var portraitFitter = portrait.gameObject.AddComponent<AspectRatioFitter>();
            portraitFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            portraitFitter.aspectRatio = PortraitAspect;
            var portraitTintRect = Theme.CreatePanel(portrait, "Tint", new Color(Theme.Teal.r, Theme.Teal.g, Theme.Teal.b, 0.5f));
            portraitTintRect.anchorMin = new Vector2(0f, 0f);
            portraitTintRect.anchorMax = new Vector2(1f, 0.5f);
            portraitTintRect.offsetMin = Vector2.zero;
            portraitTintRect.offsetMax = Vector2.zero;
            portraitTint = portraitTintRect.gameObject;

            // Гипотеза Ю7-вариант-А: акцентная полоса поверх нижнего края портрета
            // (тот же приём, что AccentBar у карточек на экране выбора режима) +
            // короткая подпись под именем роли. Обновляется в Render() по
            // GetOpponentMood(). Полоса — ребёнок portrait (не root), в его же
            // локальных координатах, поэтому не зависит от того, есть ли уже
            // настоящий арт портрета — виден он и на плашке-заглушке, и поверх
            // реального изображения; подпись — в свободном промежутке между
            // именем роли и репликой оппонента, а не под портретом (там места
            // впритык — первая строка реплики перекрывала бы её, что и
            // обнаружилось на скриншоте живого прогона).
            var moodBarRect = Theme.CreatePanel(portrait, "MoodBar", Theme.Teal);
            moodBarRect.anchorMin = new Vector2(0f, 0f);
            moodBarRect.anchorMax = new Vector2(1f, 0.1f);
            moodBarRect.offsetMin = Vector2.zero;
            moodBarRect.offsetMax = Vector2.zero;
            moodBarImage = moodBarRect.GetComponent<Image>();

            // Текстовая колонка справа от портрета, сверху вниз: роль -> подпись
            // настроения -> реплика оппонента. Правая граница роли/настроения
            // (TextColumnRight) оставляет зазор до полосы навыков (PipsLeft), которая
            // прижата к правому краю.
            //
            // Подпись настроения: цвет настроения на тёмном фоне читался плохо (бирюзовый
            // "Спокоен" сливался со сценой), поэтому сам текст — светлый Parchment, как
            // роль и реплика, а настроение несёт цветная точка слева от него (плюс полоса
            // под портретом).
            var moodChip = new GameObject("MoodChip", typeof(RectTransform));
            moodChip.transform.SetParent(root, false);
            var moodChipRect = (RectTransform)moodChip.transform;
            moodChipRect.anchorMin = new Vector2(TextColumnLeft, 0.84f);
            moodChipRect.anchorMax = new Vector2(TextColumnRight, 0.885f);
            moodChipRect.offsetMin = Vector2.zero;
            moodChipRect.offsetMax = Vector2.zero;

            var moodDotRect = Theme.CreatePanel(moodChipRect, "MoodDot", Theme.Teal);
            moodDotRect.anchorMin = new Vector2(0f, 0.5f);
            moodDotRect.anchorMax = new Vector2(0f, 0.5f);
            moodDotRect.pivot = new Vector2(0f, 0.5f);
            moodDotRect.anchoredPosition = Vector2.zero;
            moodDotRect.sizeDelta = new Vector2(16f, 16f);
            moodDotImage = moodDotRect.GetComponent<Image>();

            moodText = Theme.CreateText(moodChipRect, "MoodLabel", 20, TextAnchor.MiddleLeft, Theme.Parchment);
            var moodTextRect = moodText.rectTransform;
            Theme.StretchFull(moodTextRect);
            moodTextRect.offsetMin = new Vector2(26f, 0f);

            // Подпись роли бывает длинной ("непосредственный руководитель — тот же, с кем
            // несколько месяцев назад уже обсуждали повышение" — две-три строки), поэтому
            // размер шрифта подстраивается под коробку, а не вылезает на подпись настроения.
            opponentRoleText = Theme.CreateText(root, "OpponentRole", 20, TextAnchor.UpperLeft, Theme.Parchment);
            opponentRoleText.enableAutoSizing = true;
            opponentRoleText.fontSizeMin = 15;
            opponentRoleText.fontSizeMax = 20;
            var roleRect = opponentRoleText.rectTransform;
            roleRect.anchorMin = new Vector2(TextColumnLeft, 0.885f);
            roleRect.anchorMax = new Vector2(TextColumnRight, PortraitTop);
            roleRect.offsetMin = Vector2.zero;
            roleRect.offsetMax = Vector2.zero;

            var pipsGo = new GameObject("Pips", typeof(RectTransform));
            pipsGo.transform.SetParent(root, false);
            pipsRow = (RectTransform)pipsGo.transform;
            pipsRow.anchorMin = new Vector2(PipsLeft, 0.86f);
            pipsRow.anchorMax = new Vector2(0.95f, 0.95f);
            pipsRow.offsetMin = Vector2.zero;
            pipsRow.offsetMax = Vector2.zero;
            var pipsLayout = pipsGo.AddComponent<HorizontalLayoutGroup>();
            pipsLayout.spacing = 12;
            pipsLayout.childAlignment = TextAnchor.MiddleRight;
            pipsLayout.childForceExpandWidth = false;
            pipsLayout.childForceExpandHeight = true;

            // Реплика правее портрета и выше блока ответов. Коробка уже прежней (0.72
            // вместо 0.9 ширины канваса), но выше (0.5-0.83 вместо 0.55-0.8), а шрифт
            // автоматически уменьшается с 28 до 20, если очень длинная реплика (в
            // сценариях бывает до ~480 символов) иначе не влезла бы и полезла на ответы.
            opponentText = Theme.CreateText(root, "OpponentLine", 28, TextAnchor.UpperLeft, Theme.Parchment);
            opponentText.enableAutoSizing = true;
            opponentText.fontSizeMin = 20;
            opponentText.fontSizeMax = 28;
            var opponentRect = opponentText.rectTransform;
            opponentRect.anchorMin = new Vector2(TextColumnLeft, 0.5f);
            opponentRect.anchorMax = new Vector2(0.95f, 0.83f);
            opponentRect.offsetMin = Vector2.zero;
            opponentRect.offsetMax = Vector2.zero;
            opponentTextGroup = opponentText.gameObject.AddComponent<CanvasGroup>();

            var optionsGo = new GameObject("Options", typeof(RectTransform));
            optionsGo.transform.SetParent(root, false);
            optionsContainer = (RectTransform)optionsGo.transform;
            optionsContainer.anchorMin = new Vector2(0.05f, 0.05f);
            optionsContainer.anchorMax = new Vector2(0.95f, 0.5f);
            optionsContainer.offsetMin = Vector2.zero;
            optionsContainer.offsetMax = Vector2.zero;
            optionsGroup = optionsGo.AddComponent<CanvasGroup>();
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

            // Значок исхода: Resources/Icons/outcome_<win|compromise|fail>.png (цветной
            // квадрат акцента исхода, если файла нет — RenderEndScreen). Фиксированный
            // квадрат, а не доли канваса: раньше плашка была процентной и на нестандартном
            // окне вытягивалась бы вместе с картинкой. Верхний левый угол — там же, где был
            // прежний бейдж, заголовок исхода стоит правее.
            outcomeBadge = Theme.CreatePanel(endPanel, "OutcomeBadge", Theme.Amber);
            outcomeBadge.anchorMin = new Vector2(0.06f, 0.93f);
            outcomeBadge.anchorMax = new Vector2(0.06f, 0.93f);
            outcomeBadge.pivot = new Vector2(0f, 1f);
            outcomeBadge.anchoredPosition = Vector2.zero;
            outcomeBadge.sizeDelta = new Vector2(OutcomeIconSize, OutcomeIconSize);

            endTitleText = Theme.CreateText(endPanel, "EndTitle", 32, TextAnchor.MiddleLeft, Theme.Parchment);
            var titleRect = endTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.16f, 0.84f);
            titleRect.anchorMax = new Vector2(0.94f, 0.93f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Единый прокручиваемый блок (описание исхода -> баллы -> сильные
            // стороны -> над чем поработать) вместо жёстких процентных зон —
            // гипотеза Ю2 (docs/feature-hypotheses.md), методология —
            // docs/eval-rubric.md §3.2. Раньше описание исхода (node.summary)
            // рисовалось в отдельном блоке с фиксированной высотой на глаз —
            // после углубления сценариев длинные сводки стали переполнять эту
            // высоту и наезжать на разделы ниже (баллы по техникам и т.д.),
            // которые сами не сдвигались. Теперь оно — первая строка того же
            // прокручиваемого списка, высота считается автоматически.
            var scrollRoot = Theme.CreateScrollList(endPanel, "EndScroll", out endContent);
            scrollRoot.anchorMin = new Vector2(0.06f, 0.16f);
            scrollRoot.anchorMax = new Vector2(0.94f, 0.82f);
            scrollRoot.offsetMin = Vector2.zero;
            scrollRoot.offsetMax = Vector2.zero;

            var restartButton = Theme.CreateButton(endPanel, "Пройти ещё раз", new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.08f), Theme.Parchment, RestartScenario);
            restartButton.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Center;
            var restartRect = (RectTransform)restartButton.transform;
            restartRect.anchorMin = new Vector2(0.06f, 0.05f);
            restartRect.anchorMax = new Vector2(0.32f, 0.14f);
            restartRect.offsetMin = Vector2.zero;
            restartRect.offsetMax = Vector2.zero;

            var theoryButton = Theme.CreateButton(endPanel, "Теория и техники", new Color(Theme.Teal.r, Theme.Teal.g, Theme.Teal.b, 0.18f), Theme.Parchment, OnOpenTheoryClicked);
            theoryButton.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Center;
            var theoryRect = (RectTransform)theoryButton.transform;
            theoryRect.anchorMin = new Vector2(0.35f, 0.05f);
            theoryRect.anchorMax = new Vector2(0.61f, 0.14f);
            theoryRect.offsetMin = Vector2.zero;
            theoryRect.offsetMax = Vector2.zero;

            var newScenarioButton = Theme.CreateButton(endPanel, "Другой сценарий", Theme.Amber, Theme.Navy, OnRequestNewScenario);
            newScenarioButton.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Center;
            var newScenarioRect = (RectTransform)newScenarioButton.transform;
            newScenarioRect.anchorMin = new Vector2(0.64f, 0.05f);
            newScenarioRect.anchorMax = new Vector2(0.94f, 0.14f);
            newScenarioRect.offsetMin = Vector2.zero;
            newScenarioRect.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (root == null || !root.gameObject.activeInHierarchy) return;
            if (endPanel.gameObject.activeSelf || currentOptions.Count == 0 || isTransitioning) return;

            for (int i = 0; i < currentOptions.Count && i < 9; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i) && !Input.GetKeyDown(KeyCode.Keypad1 + i))
                    continue;
                var option = currentOptions[i];
                if (engine.IsOptionAvailable(option))
                    OnOptionChosen(option);
                return;
            }
        }

        private void Render()
        {
            foreach (Transform child in optionsContainer)
                Destroy(child.gameObject);
            currentOptions.Clear();

            if (engine.IsTerminal)
            {
                endPanel.gameObject.SetActive(true);
                RenderEndScreen();
                return;
            }

            endPanel.gameObject.SetActive(false);
            opponentRoleText.text = engine.Scenario.meta.opponentRole;
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);
            typeCoroutine = StartCoroutine(TypeOpponentLine(engine.CurrentNode.opponentLine));
            RenderPips();
            RenderMood();

            // Гипотеза Ю1 (docs/feature-hypotheses.md): одна короткая строка перед
            // первым выбором, чтобы серые/заблокированные реплики не читались как
            // баг — исчезает сама после первого хода, лишнего экрана не создаёт.
            if (engine.Transcript.Count == 0)
            {
                var hint = Theme.CreateText(optionsContainer, "Hint", 16, TextAnchor.MiddleLeft, Theme.EyebrowMuted);
                hint.text = "Серые реплики пока недоступны — рядом с ними указано, какого навыка не хватает.";
                hint.gameObject.AddComponent<LayoutElement>().minHeight = 26;
            }

            foreach (var option in engine.CurrentNode.options)
            {
                currentOptions.Add(option);
                int number = currentOptions.Count;
                bool available = engine.IsOptionAvailable(option);
                var capturedOption = option;
                CreateOptionButton(number, option, available, () => OnOptionChosen(capturedOption));
            }
        }

        // Гипотеза Ю5 (docs/feature-hypotheses.md): вместо текста "Требуется: Логика ≥2"
        // — цветная иконка навыка (или, пока реальных ассетов нет, цветной квадрат того
        // же акцента, что и пипсы навыков) + "≥N". Считывается быстрее, чем текст целиком.
        private void CreateOptionButton(int number, DialogueOption option, bool available, UnityEngine.Events.UnityAction onClick)
        {
            var fill = available
                ? new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.06f)
                : new Color(Theme.Coral.r, Theme.Coral.g, Theme.Coral.b, 0.08f);

            var go = new GameObject($"Option_{number}", typeof(RectTransform));
            go.transform.SetParent(optionsContainer, false);
            var image = go.AddComponent<Image>();
            image.color = fill;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = available;
            if (onClick != null) button.onClick.AddListener(onClick);
            go.AddComponent<LayoutElement>().minHeight = 58;

            var rowLayout = go.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 6, 6);
            rowLayout.spacing = 10;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;

            var textColor = available ? Theme.Parchment : new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.62f);
            var label = Theme.CreateText(go.transform, "Label", 22, TextAnchor.MiddleLeft, textColor);
            label.text = $"{number}.  {option.text}";
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            if (!available)
                foreach (var req in engine.GetMissingRequirements(option))
                    AddSkillRequirementBadge(go.transform, req);
        }

        private void AddSkillRequirementBadge(Transform parent, SkillRequirement requirement)
        {
            var badgeGo = new GameObject($"Req_{requirement.skill}", typeof(RectTransform));
            badgeGo.transform.SetParent(parent, false);
            var badgeLayout = badgeGo.AddComponent<HorizontalLayoutGroup>();
            badgeLayout.spacing = 4;
            badgeLayout.childAlignment = TextAnchor.MiddleLeft;
            badgeLayout.childForceExpandWidth = false;
            badgeLayout.childForceExpandHeight = true;
            badgeLayout.childControlWidth = true;
            badgeLayout.childControlHeight = true;
            badgeGo.AddComponent<LayoutElement>().minWidth = 108;

            // Иконка навыка на светлой плашке (Theme.CreateSkillIcon; пока файла нет —
            // цветной квадрат акцента навыка) + уровень цветом навыка. Иконка 68 выше
            // минимальной строки опции (58) — строка с заблокированной репликой при этом
            // вырастает до ~80, остальные остаются прежними.
            Theme.CreateSkillIcon(badgeGo.transform, requirement.skill, 68f);

            var levelText = Theme.CreateText(badgeGo.transform, "Level", 26, TextAnchor.MiddleLeft, Theme.ForSkill(requirement.skill));
            levelText.text = $"≥{requirement.level}";
        }

        // Ищет Resources/Backgrounds/<id>.png по домену текущего сценария
        // (docs/team-plan.md фиксирует конвенцию имён) — если файла нет, остаётся
        // сплошной Navy, ничего не ломается. Портрет теперь обновляется отдельно,
        // на каждом ходу, — см. UpdatePortraitSprite (зависит от настроения).
        private void UpdateBackground()
        {
            // По домену (сфере), а не по конкретному сценарию: с Г2 на каждый домен
            // приходится 3 сценария (лёгкий/средний/сложный) с одним и тем же
            // персонажем/обстановкой — незачем просить команду рисовать 3x ассетов.
            var domainKey = DomainKeyForSphere(engine.Scenario.meta.sphere) ?? engine.Scenario.meta.id;
            Theme.SetCanvasBackground(root, $"Backgrounds/{domainKey}", scrimAlpha: 0.6f);
        }

        // Гипотеза Ю7: портрет по настроению (Resources/Portraits/<домен>_<настроение>.png,
        // например hr_irritated.png; настроений пять — neutral/calm/pleased/wary/irritated).
        // Реальные портреты пока только у HR; у остальных доменов схематичные заглушки
        // четырёх настроений без neutral. Цепочка отката: файл нужного настроения ->
        // для neutral файл calm (иначе на старте каждого разговора у ещё не нарисованных
        // доменов была бы плашка-заглушка) -> прежний общий Portraits/<домен>.png ->
        // плашка-заглушка. Ничего не ломается по мере добавления ассетов постепенно.
        private void UpdatePortraitSprite(string moodSuffix)
        {
            var domainKey = DomainKeyForSphere(engine.Scenario.meta.sphere) ?? engine.Scenario.meta.id;
            var sprite = Theme.TryLoadSprite($"Portraits/{domainKey}_{moodSuffix}")
                ?? (moodSuffix == "neutral" ? Theme.TryLoadSprite($"Portraits/{domainKey}_calm") : null)
                ?? Theme.TryLoadSprite($"Portraits/{domainKey}");
            if (sprite != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.color = Color.white;
                portraitTint.SetActive(false);
            }
            else
            {
                portraitImage.sprite = null;
                portraitImage.color = Theme.Slate;
                portraitTint.SetActive(true);
            }
        }

        private void RenderPips()
        {
            foreach (Transform child in pipsRow)
                Destroy(child.gameObject);

            AddPipGroup("Напор", "napor", engine.Skills.napor);
            AddPipGroup("Эмпатия", "empatiya", engine.Skills.empatiya);
            AddPipGroup("Логика", "logika", engine.Skills.logika);
        }

        // Гипотеза Ю7-вариант-А: видимая реакция оппонента на последний ход игрока
        // (docs/feature-hypotheses.md). Полоса под портретом + короткая подпись,
        // цвет — уже принятая в docs/visual-style-guide.md семантика (Sage=win,
        // Amber=compromise, Coral=fail), без единого нового арт-ассета.
        private void RenderMood()
        {
            var mood = engine.GetOpponentMood();
            Color color;
            string label;
            string moodSuffix;
            switch (mood)
            {
                case OpponentMood.Neutral:
                    // Холодный серо-голубой — не совпадает ни с одним из четырёх
                    // смысловых акцентов (Sage/Amber/Coral/Teal), читается как "реакции ещё нет".
                    color = Theme.LogikaAccent;
                    label = "Нейтрален";
                    moodSuffix = "neutral";
                    break;
                case OpponentMood.Pleased:
                    color = Theme.Sage;
                    label = "Доволен";
                    moodSuffix = "pleased";
                    break;
                case OpponentMood.Wary:
                    color = Theme.Amber;
                    label = "Насторожен";
                    moodSuffix = "wary";
                    break;
                case OpponentMood.Irritated:
                    color = Theme.Coral;
                    label = "Раздражён";
                    moodSuffix = "irritated";
                    break;
                default:
                    color = Theme.Teal;
                    label = "Спокоен";
                    moodSuffix = "calm";
                    break;
            }

            moodBarImage.color = color;
            moodDotImage.color = color;
            moodText.text = label;
            UpdatePortraitSprite(moodSuffix);
        }

        private void AddPipGroup(string label, string skillId, int level)
        {
            var groupGo = new GameObject($"Pip_{skillId}", typeof(RectTransform));
            groupGo.transform.SetParent(pipsRow, false);
            var layout = groupGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            var sizeFit = groupGo.AddComponent<ContentSizeFitter>();
            sizeFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var accent = Theme.ForSkill(skillId);

            // Иконка + название: пипсы — единственное место, где игрок узнаёт, какая
            // иконка какому навыку соответствует (у заблокированных реплик рядом с
            // иконкой только "≥N", без названия — Ю5). Иконка крупная (52), поэтому
            // название и уровень стоят справа от неё столбиком (название над пипсами),
            // а не в одну строку — иначе три группы не помещаются в полосу навыков.
            Theme.CreateSkillIcon(groupGo.transform, skillId, 52f);

            var columnGo = new GameObject("NameAndLevel", typeof(RectTransform));
            columnGo.transform.SetParent(groupGo.transform, false);
            var column = columnGo.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4;
            column.childAlignment = TextAnchor.MiddleLeft;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            column.childControlWidth = true;
            column.childControlHeight = true;

            var labelText = Theme.CreateText(columnGo.transform, "Label", 15, TextAnchor.MiddleLeft, Theme.Muted);
            labelText.text = label;

            var dotsGo = new GameObject("Dots", typeof(RectTransform));
            dotsGo.transform.SetParent(columnGo.transform, false);
            var dotsLayout = dotsGo.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.spacing = 4;
            dotsLayout.childAlignment = TextAnchor.MiddleLeft;
            dotsLayout.childForceExpandWidth = false;
            dotsLayout.childForceExpandHeight = false;
            dotsLayout.childControlWidth = true;
            dotsLayout.childControlHeight = true;

            for (int i = 1; i <= 3; i++)
            {
                var dotGo = new GameObject($"Dot{i}", typeof(RectTransform));
                dotGo.transform.SetParent(dotsGo.transform, false);
                var dotLayout = dotGo.AddComponent<LayoutElement>();
                dotLayout.minWidth = 9;
                dotLayout.preferredWidth = 9;
                dotLayout.minHeight = 24;
                dotLayout.preferredHeight = 24;
                var image = dotGo.AddComponent<Image>();
                image.color = i <= level ? accent : new Color(Theme.Parchment.r, Theme.Parchment.g, Theme.Parchment.b, 0.15f);
            }
        }

        private void OnOptionChosen(DialogueOption option)
        {
            if (isTransitioning) return;
            engine.ChooseOption(option);
            StartCoroutine(RenderWithFade());
        }

        // К2: реплика и варианты ответа гаснут, меняются под скрытием и проявляются
        // заново — вместо мгновенной подмены текста при выборе реплики.
        private IEnumerator RenderWithFade()
        {
            isTransitioning = true;
            yield return StartCoroutine(FadeTo(0f));
            Render();
            yield return StartCoroutine(FadeTo(1f));
            isTransitioning = false;
        }

        private IEnumerator FadeTo(float target)
        {
            float start = opponentTextGroup.alpha;
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(start, target, t / FadeDuration);
                opponentTextGroup.alpha = alpha;
                optionsGroup.alpha = alpha;
                yield return null;
            }
            opponentTextGroup.alpha = target;
            optionsGroup.alpha = target;
        }

        // Печатает реплику оппонента по буквам (TMP.maxVisibleCharacters, а не
        // пересборка строки — переживает любую разметку без ручного парсинга) и
        // проигрывает блип голоса на каждое НОВОЕ слово, а не на каждый символ:
        // у большинства клипов в банке длительность от ~0.1 до ~1.6с — по блипу
        // на символ они бы наложились друг на друга сплошным гулом. Опции ответа
        // при этом не ждут конца печати — доступны сразу после Render(), как и
        // раньше, чтобы не замедлять принятие решения игроком.
        private IEnumerator TypeOpponentLine(string line)
        {
            opponentText.text = line;
            opponentText.maxVisibleCharacters = 0;
            opponentText.ForceMeshUpdate();

            if (string.IsNullOrEmpty(line))
                yield break;

            float delay = 1f / CharsPerSecond;
            if (line.Length * delay > MaxTypeDuration)
                delay = MaxTypeDuration / line.Length;

            var wait = new WaitForSeconds(delay);
            bool atWordStart = true;
            var mood = engine.GetOpponentMood();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                opponentText.maxVisibleCharacters = i + 1;
                if (char.IsWhiteSpace(c))
                {
                    atWordStart = true;
                }
                else if (atWordStart)
                {
                    atWordStart = false;
                    voice.PlayBlip(mood);
                }
                yield return wait;
            }
        }

        private void RenderEndScreen()
        {
            var node = engine.CurrentNode;
            var outcomeImage = outcomeBadge.GetComponent<Image>();
            var outcomeSprite = Theme.TryLoadSprite($"Icons/outcome_{node.outcome}");
            outcomeImage.sprite = outcomeSprite;
            outcomeImage.preserveAspect = outcomeSprite != null;
            outcomeImage.color = outcomeSprite != null ? Color.white : Theme.ForOutcome(node.outcome);
            endTitleText.text = OutcomeTitle(node.outcome);

            foreach (Transform child in endContent) Destroy(child.gameObject);

            var taxonomy = TechniqueTaxonomyLibrary.Load();
            var byId = new Dictionary<string, TechniqueInfo>();
            foreach (var t in taxonomy.techniques) byId[t.id] = t;
            var domainKey = DomainKeyForSphere(engine.Scenario.meta.sphere);

            // Бейджу "Повтор" и остальным достижениям нужен уже обновлённый
            // NegotiatorProfile (в частности RunCount, включающий этот прогон) —
            // поэтому запись в профиль перенесена в самое начало, до рендера, и
            // выполняется ДО оценки бейджей (а не после, как раньше в этом методе).
            profile.RecordRun(engine.Skills, engine.TechniqueScores, engine.Transcript.Select(s => s.ChosenOption.technique));
            var newlyEarnedBadges = EvaluateNewlyEarnedBadges();

            Theme.CreateText(endContent, "Summary", 20, TextAnchor.UpperLeft, Theme.Muted).text = node.summary;

            AddArchetypeSection(byId);
            AddBadgesSection(newlyEarnedBadges);

            AddSectionLabel("БАЛЛЫ ПО ТЕХНИКАМ");
            AddScoreChipsRow();

            AddTechniqueTimelineSection();

            AddSectionLabel("ОБЩАЯ СТРАТЕГИЯ");
            AddPlainLine(BuildStrategyNarrative(byId), Theme.Parchment);

            AddNegotiatorProfileSection(byId);

            AddSectionLabel("СИЛЬНЫЕ СТОРОНЫ");
            var strengths = SelectHighlights(wantStrengths: true, domainKey, byId);
            if (strengths.Count == 0)
                AddPlainLine("Сильных сторон пока не зафиксировано — начните с раздела ниже.", Theme.Muted);
            else
                foreach (var step in strengths) AddQuoteCard(step, byId, isStrength: true);

            AddSectionLabel("НАД ЧЕМ ПОРАБОТАТЬ");
            var improvements = SelectHighlights(wantStrengths: false, domainKey, byId);
            if (improvements.Count == 0)
                AddPlainLine("Явных слабых реплик не было.", Theme.Sage);
            else
                foreach (var step in improvements) AddQuoteCard(step, byId, isStrength: false);
        }

        // Деление таксономии на две семьи техник (docs/technique-taxonomy.json) —
        // ровно техники с диапазоном только "+" и только "-", без пересечений и
        // без нейтральных: соответствует делению Fisher & Ury на принципиальные
        // переговоры vs позиционный торг. Используется в BuildStrategyNarrative (Ю6).
        private static readonly HashSet<string> PrincipledTechniqueIds = new HashSet<string>
        {
            "state_interest", "objective_criteria", "open_question", "active_listening",
            "de_escalate", "package_deal", "batna_leverage", "anchor_with_flex", "recover"
        };

        private static readonly HashSet<string> PositionalTechniqueIds = new HashSet<string>
        {
            "position_push", "escalate", "personal_attack", "empty_threat", "vague_claim", "give_up"
        };

        // Гипотеза "Профиль переговорщика (архетип)": та же таксономия из 15 техник,
        // но перегруппирована по стилю поведения на 5 архетипов вместо 2 семей выше
        // (принципиальные/позиционные — про качество аргументации, архетип — про
        // манеру вести разговор). Архетип определяется по ЧАСТОТЕ выбора техники, а
        // не по сумме баллов — так нагляднее для игрока ("5 раз уступил"), и разбиение
        // на принципиальные/позиционные ещё и не мешает: обе семьи представлены и
        // среди "хороших" архетипов (Аналитик/Дипломат/Стратег), и Уступчивый с
        // Агрессором целиком состоят из позиционных техник.
        private static readonly Dictionary<string, string> ArchetypeForTechnique = new Dictionary<string, string>
        {
            { "objective_criteria", "Аналитик" }, { "open_question", "Аналитик" },
            { "batna_leverage", "Аналитик" }, { "recover", "Аналитик" },

            { "state_interest", "Дипломат" }, { "active_listening", "Дипломат" },
            { "de_escalate", "Дипломат" },

            { "package_deal", "Стратег" }, { "anchor_with_flex", "Стратег" },

            { "escalate", "Агрессор" }, { "personal_attack", "Агрессор" },
            { "empty_threat", "Агрессор" }, { "position_push", "Агрессор" },

            { "give_up", "Уступчивый" }, { "vague_claim", "Уступчивый" },
        };

        private static readonly Dictionary<string, string> ArchetypeDescription = new Dictionary<string, string>
        {
            { "Аналитик", "Вы опираетесь на факты, вопросы и реальные альтернативы — не на давление и не на уступки." },
            { "Дипломат", "Вы называете интересы, слушаете оппонента и снимаете напряжение вместо того, чтобы спорить." },
            { "Стратег", "Вы предпочитаете пакетные решения и якорение с гибкостью — не голый торг по одному пункту." },
            { "Агрессор", "Вы чаще давите и повышаете напряжение, чем ищете компромисс." },
            { "Уступчивый", "Вы чаще уступаете без встречного условия, чем отстаиваете свою позицию." },
        };

        // Если отрыв лидера от второго места меньше этой доли от всех тегированных
        // выборов — называть один архетип было бы натяжкой, честнее показать
        // "смешанный стиль".
        private const float MixedArchetypeMarginShare = 0.15f;

        private static (string archetype, int total) DetermineArchetype(IReadOnlyDictionary<string, int> techniqueCounts)
        {
            var bucketCounts = new Dictionary<string, int>();
            int total = 0;
            foreach (var kv in techniqueCounts)
            {
                if (!ArchetypeForTechnique.TryGetValue(kv.Key, out var archetype)) continue;
                bucketCounts.TryGetValue(archetype, out var current);
                bucketCounts[archetype] = current + kv.Value;
                total += kv.Value;
            }
            if (total == 0) return (null, 0);

            string best = null, second = null;
            int bestCount = -1, secondCount = -1;
            foreach (var kv in bucketCounts)
            {
                if (kv.Value > bestCount)
                {
                    second = best; secondCount = bestCount;
                    best = kv.Key; bestCount = kv.Value;
                }
                else if (kv.Value > secondCount)
                {
                    second = kv.Key; secondCount = kv.Value;
                }
            }

            if (second != null && (bestCount - secondCount) / (float)total < MixedArchetypeMarginShare)
                return (null, total);

            return (best, total);
        }

        // Архетип за один прогон — считается по engine.Transcript, показывается
        // сразу после сводки исхода, до разбора по баллам: это самый "шарибельный"
        // заголовочный результат экрана, поэтому стоит первым, а не в конце.
        private void AddArchetypeSection(Dictionary<string, TechniqueInfo> byId)
        {
            var techniqueCounts = new Dictionary<string, int>();
            foreach (var step in engine.Transcript)
            {
                var id = step.ChosenOption.technique;
                if (string.IsNullOrEmpty(id)) continue;
                techniqueCounts.TryGetValue(id, out var current);
                techniqueCounts[id] = current + 1;
            }

            var (archetype, total) = DetermineArchetype(techniqueCounts);
            if (total == 0) return;

            AddSectionLabel("ПРОФИЛЬ ПЕРЕГОВОРЩИКА (АРХЕТИП)");

            if (archetype == null)
            {
                AddPlainLine("Смешанный стиль — ни один архетип пока не преобладает явно.", Theme.Muted);
                return;
            }

            var exampleStep = engine.Transcript
                .Where(s => !string.IsNullOrEmpty(s.ChosenOption.technique)
                    && ArchetypeForTechnique.TryGetValue(s.ChosenOption.technique, out var a) && a == archetype)
                .OrderByDescending(s => Math.Abs(s.ChosenOption.points))
                .FirstOrDefault();

            var accent = exampleStep != null && exampleStep.ChosenOption.points > 0 ? Theme.Sage : Theme.Coral;

            var badge = Theme.CreatePanel(endContent, "ArchetypeBadge", new Color(accent.r, accent.g, accent.b, 0.1f));
            var badgeLayout = badge.gameObject.AddComponent<VerticalLayoutGroup>();
            badgeLayout.padding = new RectOffset(16, 16, 10, 10);
            badgeLayout.spacing = 4;
            badgeLayout.childForceExpandWidth = true;
            badgeLayout.childForceExpandHeight = false;
            badgeLayout.childControlWidth = true;
            badgeLayout.childControlHeight = true;
            badge.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Theme.CreateText(badge, "Name", 24, TextAnchor.UpperLeft, accent).text = archetype;
            if (ArchetypeDescription.TryGetValue(archetype, out var description))
                Theme.CreateText(badge, "Description", 17, TextAnchor.UpperLeft, Theme.Parchment).text = description;

            if (exampleStep != null)
                AddQuoteCard(exampleStep, byId, isStrength: exampleStep.ChosenOption.points > 0);
        }

        // Идея "Микро-достижения (бейджи)": маленькие разовые награды поверх уже
        // существующих данных (Transcript/TechniqueScores/NegotiatorProfile) — не
        // новая механика подсчёта, а другой взгляд на те же самые числа. Каждый
        // бейдж показывается только один раз за сессию (см. NegotiatorProfile.
        // UnlockedBadgeIds), а не при каждом повторном выполнении условия.
        private class BadgeDefinition
        {
            public string Id;
            public string Name;
            public string Description;
        }

        private static readonly List<BadgeDefinition> BadgeDefinitions = new List<BadgeDefinition>
        {
            new BadgeDefinition { Id = "first_deal", Name = "Первая сделка", Description = "Успешно завершили переговоры — впервые за эту сессию." },
            new BadgeDefinition { Id = "listener", Name = "Слушатель", Description = "Задали 3 и более открытых вопроса за один разговор." },
            new BadgeDefinition { Id = "firm", Name = "Твёрдый", Description = "Ни разу не уступили без встречного условия." },
            new BadgeDefinition { Id = "explorer", Name = "Исследователь", Description = "Использовали реплику, доступную только при прокачанном навыке." },
            new BadgeDefinition { Id = "persistent", Name = "Повтор", Description = "Прошли третий сценарий за эту сессию." },
        };

        // Вызывается после profile.RecordRun(...) — RunCount должен уже включать
        // этот прогон, иначе "Повтор" сработает на ходу позже, чем должен.
        private List<string> EvaluateNewlyEarnedBadges()
        {
            var earned = new List<string>();

            bool IsNew(string id) => !profile.UnlockedBadgeIds.Contains(id);

            if (engine.CurrentNode.outcome == "win" && IsNew("first_deal"))
                earned.Add("first_deal");

            int openQuestionCount = engine.Transcript.Count(s => s.ChosenOption.technique == "open_question");
            if (openQuestionCount >= 3 && IsNew("listener"))
                earned.Add("listener");

            int taggedCount = engine.Transcript.Count(s => !string.IsNullOrEmpty(s.ChosenOption.technique));
            bool neverGaveUp = !engine.Transcript.Any(s => s.ChosenOption.technique == "give_up");
            if (taggedCount >= 5 && neverGaveUp && IsNew("firm"))
                earned.Add("firm");

            bool usedGatedOption = engine.Transcript.Any(s => s.ChosenOption.requiredSkills != null && s.ChosenOption.requiredSkills.Length > 0);
            if (usedGatedOption && IsNew("explorer"))
                earned.Add("explorer");

            if (profile.RunCount >= 3 && IsNew("persistent"))
                earned.Add("persistent");

            foreach (var id in earned) profile.UnlockedBadgeIds.Add(id);
            return earned;
        }

        private void AddBadgesSection(List<string> earnedBadgeIds)
        {
            if (earnedBadgeIds.Count == 0) return;

            AddSectionLabel("НОВЫЕ ДОСТИЖЕНИЯ");

            var rowGo = new GameObject("BadgesRow", typeof(RectTransform));
            rowGo.transform.SetParent(endContent, false);
            var grid = rowGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(240, 90);
            grid.spacing = new Vector2(12, 12);
            grid.childAlignment = TextAnchor.MiddleLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            rowGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var id in earnedBadgeIds)
            {
                var def = BadgeDefinitions.FirstOrDefault(b => b.Id == id);
                if (def != null) AddBadgeCard(rowGo.transform, def);
            }
        }

        // Гипотеза "Микро-достижения": иконка — Resources/Icons/badge_<id>.png,
        // пока схематичная Pillow-заглушка (та же идея, что портреты настроения
        // Ю7) — если файла нет, просто остаётся цветной квадрат-плейсхолдер.
        private void AddBadgeCard(Transform parent, BadgeDefinition def)
        {
            var card = Theme.CreatePanel(parent, $"Badge_{def.Id}", new Color(Theme.Amber.r, Theme.Amber.g, Theme.Amber.b, 0.12f));
            var layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 10;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(card, false);
            iconGo.AddComponent<LayoutElement>().minWidth = 40;
            var iconImage = iconGo.AddComponent<Image>();
            var sprite = Theme.TryLoadSprite($"Icons/badge_{def.Id}");
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.color = new Color(Theme.Amber.r, Theme.Amber.g, Theme.Amber.b, 0.4f);
            }

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(card, false);
            textGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var textLayout = textGo.AddComponent<VerticalLayoutGroup>();
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childAlignment = TextAnchor.MiddleLeft;

            Theme.CreateText(textGo.transform, "Name", 16, TextAnchor.UpperLeft, Theme.Amber).text = def.Name;
            Theme.CreateText(textGo.transform, "Description", 13, TextAnchor.UpperLeft, Theme.Muted).text = def.Description;
        }

        // Общий подсчёт для BuildStrategyNarrative (Ю6, один прогон) и
        // BuildSessionProfileNarrative (Г8, сумма по всем прогонам сессии) —
        // раньше эта логика была только внутри BuildStrategyNarrative.
        private (string principledName, string positionalName, float principledShare, int total) AnalyzeTechniqueFamilies(
            IReadOnlyDictionary<string, int> techniqueScores, Dictionary<string, TechniqueInfo> byId)
        {
            int principledSum = 0, positionalSum = 0;
            string topPrincipledId = null;
            int topPrincipledScore = int.MinValue;
            string topPositionalId = null;
            int topPositionalScore = int.MaxValue;

            foreach (var kv in techniqueScores)
            {
                if (PrincipledTechniqueIds.Contains(kv.Key))
                {
                    principledSum += kv.Value;
                    if (kv.Value > topPrincipledScore) { topPrincipledScore = kv.Value; topPrincipledId = kv.Key; }
                }
                else if (PositionalTechniqueIds.Contains(kv.Key))
                {
                    positionalSum += kv.Value;
                    if (kv.Value < topPositionalScore) { topPositionalScore = kv.Value; topPositionalId = kv.Key; }
                }
            }

            int total = principledSum + Mathf.Abs(positionalSum);
            string principledName = topPrincipledId != null && byId.TryGetValue(topPrincipledId, out var pInfo) ? pInfo.ru_name : topPrincipledId;
            string positionalName = topPositionalId != null && byId.TryGetValue(topPositionalId, out var nInfo) ? nInfo.ru_name : topPositionalId;
            float principledShare = total == 0 ? 0f : principledSum / (float)total;
            return (principledName, positionalName, principledShare, total);
        }

        // Гипотеза Ю6 (docs/feature-hypotheses.md): не только теги+баллы, а один
        // абзац о стратегии в целом — детерминированный шаблон по доле принципиальных
        // vs позиционных техник за весь прогон (без LLM), усиливает уже сделанный Ю2.
        private string BuildStrategyNarrative(Dictionary<string, TechniqueInfo> byId)
        {
            var (principledName, positionalName, principledShare, total) = AnalyzeTechniqueFamilies(engine.TechniqueScores, byId);
            if (total == 0)
                return "За это прохождение накопилось слишком мало данных для общего разбора стратегии — пройдите сценарий ещё раз.";

            if (principledShare >= 0.75f)
                return $"В целом вы вели принципиальные переговоры (Fisher & Ury): опирались на интересы и объективные критерии, а не на давление. Сильнее всего сработала техника «{principledName}» — держите этот подход и в следующих раундах.";

            if (principledShare <= 0.25f)
                return $"В этом прохождении преобладал позиционный торг — чаще всего проявлялась «{positionalName}». По Гарвардскому методу такой подход обычно вредит отношениям и не даёт лучшего результата: попробуйте в следующий раз чаще опираться на объективные критерии и открытые вопросы.";

            return $"Стратегия получилась смешанной: сильная сторона — «{principledName}», но эпизодами проявлялся позиционный паттерн «{positionalName}». Если убрать эти срывы, результат станет заметно увереннее.";
        }

        // Гипотеза Г8 (docs/feature-hypotheses.md): та же классификация техник, что
        // и в Ю6, но по сумме за все прогоны сессии, а не за один раунд — показывает,
        // устойчив ли стиль игрока или он сильно колеблется от сценария к сценарию.
        private string BuildSessionProfileNarrative(Dictionary<string, TechniqueInfo> byId)
        {
            var (principledName, positionalName, principledShare, total) = AnalyzeTechniqueFamilies(profile.TechniqueScoresTotal, byId);
            if (total == 0)
                return "За эту сессию пока недостаточно данных о технике разговора.";

            if (principledShare >= 0.75f)
                return $"Во всех прогонах этой сессии вы устойчиво держитесь принципиальных переговоров — ярче всего это раскрыла техника «{principledName}».";

            if (principledShare <= 0.25f)
                return $"Во всех прогонах этой сессии преобладает позиционный торг — чаще всего это «{positionalName}». Стоит осознанно потренировать открытые вопросы и опору на объективные критерии.";

            return $"За сессию стратегия колеблется: то принципиальный подход («{principledName}»), то позиционный откат («{positionalName}»).";
        }

        // Гипотеза Ю9 (docs/feature-hypotheses.md): цветная полоса-таймлайн техник
        // по ходу разговора (та же классификация принципиальные/позиционные, что и
        // в Ю6/Г8) вместо только суммарных баллов — видно, где именно в разговоре
        // был провал или удача, а не только итоговый баланс.
        private void AddTechniqueTimelineSection()
        {
            var steps = engine.Transcript.Where(s => !string.IsNullOrEmpty(s.ChosenOption.technique)).ToList();
            if (steps.Count == 0) return;

            AddSectionLabel("ДИНАМИКА ПО ХОДУ РАЗГОВОРА");

            var rowGo = new GameObject("TechniqueTimeline", typeof(RectTransform));
            rowGo.transform.SetParent(endContent, false);
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            rowGo.AddComponent<LayoutElement>().minHeight = 16;

            foreach (var step in steps)
            {
                Color segmentColor;
                if (PrincipledTechniqueIds.Contains(step.ChosenOption.technique)) segmentColor = Theme.Sage;
                else if (PositionalTechniqueIds.Contains(step.ChosenOption.technique)) segmentColor = Theme.Coral;
                else segmentColor = Theme.Muted;
                var segment = Theme.CreatePanel(rowGo.transform, "Segment", segmentColor);
                segment.GetComponent<Image>().raycastTarget = false;
            }

            AddPlainLine("Слева направо — по порядку ходов. Зелёное — принципиальная техника, красное — позиционный торг.", Theme.EyebrowMuted);
        }

        // Гипотеза Г8 (docs/feature-hypotheses.md): накопленный профиль переговорщика
        // за сессию — радар по трём навыкам (усреднённым по всем пройденным сценариям)
        // + абзац о доминирующей семье техник за все прогоны. Показывается только
        // начиная со 2-го завершённого сценария за сессию — на первом прогоне
        // "накопленному" профилю ещё не из чего складываться.
        private void AddNegotiatorProfileSection(Dictionary<string, TechniqueInfo> byId)
        {
            if (profile.RunCount < 2) return;

            AddSectionLabel($"ПРОФИЛЬ ПЕРЕГОВОРЩИКА — ПРОЙДЕНО СЦЕНАРИЕВ: {profile.RunCount}");

            var rowGo = new GameObject("ProfileRow", typeof(RectTransform));
            rowGo.transform.SetParent(endContent, false);
            var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 20;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowGo.AddComponent<LayoutElement>().minHeight = 190;

            var radarWrapGo = new GameObject("Radar", typeof(RectTransform));
            radarWrapGo.transform.SetParent(rowGo.transform, false);
            var radarWrapLayout = radarWrapGo.AddComponent<LayoutElement>();
            radarWrapLayout.minWidth = 190;
            radarWrapLayout.minHeight = 190;

            var chartGo = new GameObject("Chart", typeof(RectTransform));
            chartGo.transform.SetParent(radarWrapGo.transform, false);
            var chartRect = (RectTransform)chartGo.transform;
            chartRect.anchorMin = new Vector2(0.1f, 0.22f);
            chartRect.anchorMax = new Vector2(0.9f, 0.85f);
            chartRect.offsetMin = Vector2.zero;
            chartRect.offsetMax = Vector2.zero;
            var chart = chartGo.AddComponent<RadarChart>();
            chart.color = new Color(Theme.Teal.r, Theme.Teal.g, Theme.Teal.b, 0.55f);
            chart.raycastTarget = false;

            var (napor, empatiya, logika) = profile.AverageSkills();
            chart.SetValues(napor, empatiya, logika);

            AddRadarAxisLabel(radarWrapGo.transform, "Напор", new Vector2(0f, 0.85f), new Vector2(1f, 1f), TextAnchor.MiddleCenter);
            AddRadarAxisLabel(radarWrapGo.transform, "Эмпатия", new Vector2(0f, 0f), new Vector2(0.5f, 0.22f), TextAnchor.LowerLeft);
            AddRadarAxisLabel(radarWrapGo.transform, "Логика", new Vector2(0.5f, 0f), new Vector2(1f, 0.22f), TextAnchor.LowerRight);

            var textGo = new GameObject("ProfileText", typeof(RectTransform));
            textGo.transform.SetParent(rowGo.transform, false);
            textGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var text = Theme.CreateText(textGo.transform, "Text", 17, TextAnchor.UpperLeft, Theme.Parchment);
            Theme.StretchFull(text.rectTransform);
            text.text = BuildSessionArchetypeLine() + "\n\n" + BuildSessionProfileNarrative(byId);
        }

        // Тот же архетип, что и AddArchetypeSection, но по накопленной за сессию
        // частоте техник (NegotiatorProfile.TechniqueCountsTotal) вместо одного
        // прогона — стабильнее на нескольких сценариях подряд.
        private string BuildSessionArchetypeLine()
        {
            var (archetype, total) = DetermineArchetype(profile.TechniqueCountsTotal);
            if (total == 0) return "Архетип за сессию: пока недостаточно данных.";
            return archetype == null
                ? "Архетип за сессию: смешанный стиль."
                : $"Архетип за сессию: «{archetype}».";
        }

        private void AddRadarAxisLabel(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, TextAnchor anchor)
        {
            var text = Theme.CreateText(parent, "AxisLabel", 13, anchor, Theme.EyebrowMuted);
            text.text = label;
            var rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Гипотеза Ю2 (docs/feature-hypotheses.md), методология docs/eval-rubric.md §3.2:
        // strengths — points>0, сортировка по убыванию points, при равенстве сначала
        // центральные для домена техники; improvements — points<=0 с непустым
        // betterAlternative, по возрастанию points. В обоих случаях — дедупликация
        // по тегу техники (один самый яркий пример) и лимит 3.
        private List<ChosenStep> SelectHighlights(bool wantStrengths, string domainKey, Dictionary<string, TechniqueInfo> byId)
        {
            IEnumerable<ChosenStep> filtered = wantStrengths
                ? engine.Transcript.Where(s => s.ChosenOption.points > 0)
                : engine.Transcript.Where(s => s.ChosenOption.points <= 0 && !string.IsNullOrEmpty(s.ChosenOption.betterAlternative));

            var ordered = wantStrengths
                ? filtered.OrderByDescending(s => s.ChosenOption.points)
                    .ThenByDescending(s => IsCentral(s.ChosenOption.technique, domainKey, byId))
                : filtered.OrderBy(s => s.ChosenOption.points);

            var result = new List<ChosenStep>();
            var seenTags = new HashSet<string>();
            foreach (var step in ordered)
            {
                if (!seenTags.Add(step.ChosenOption.technique)) continue;
                result.Add(step);
                if (result.Count >= 3) break;
            }
            return result;
        }

        private static bool IsCentral(string techniqueId, string domainKey, Dictionary<string, TechniqueInfo> byId)
        {
            if (domainKey == null || !byId.TryGetValue(techniqueId, out var info) || info.domain_centrality == null)
                return false;
            string value;
            switch (domainKey)
            {
                case "hr": value = info.domain_centrality.hr; break;
                case "sales": value = info.domain_centrality.sales; break;
                case "procurement": value = info.domain_centrality.procurement; break;
                default: value = null; break;
            }
            return value == "central";
        }

        private static string DomainKeyForSphere(string sphere)
        {
            switch (sphere)
            {
                case "HR": return "hr";
                case "B2B-продажи": return "sales";
                case "Закупки": return "procurement";
                default: return null;
            }
        }

        private void AddSectionLabel(string text)
        {
            var label = Theme.CreateText(endContent, "SectionLabel", 15, TextAnchor.MiddleLeft, Theme.EyebrowMuted);
            label.text = text;
            label.gameObject.AddComponent<LayoutElement>().minHeight = 24;
        }

        private void AddPlainLine(string text, Color color)
        {
            Theme.CreateText(endContent, "Line", 17, TextAnchor.UpperLeft, color).text = text;
        }

        // Сетка вместо строки: после углубления сценариев (docs/feature-hypotheses.md,
        // задача "углубить разговоры") за один проход может накопиться до 8-10 разных
        // тегов техник — нерастягивающийся HorizontalLayoutGroup вылезал бы за экран,
        // GridLayoutGroup сам переносит лишние чипы на следующую строку.
        private void AddScoreChipsRow()
        {
            var rowGo = new GameObject("ScoreRow", typeof(RectTransform));
            rowGo.transform.SetParent(endContent, false);
            var grid = rowGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(175, 36);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.MiddleLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            rowGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var kv in engine.TechniqueScores)
            {
                bool positive = kv.Value > 0;
                var accent = positive ? Theme.Sage : Theme.Coral;
                var chipBg = Theme.CreatePanel(rowGo.transform, "Chip", new Color(accent.r, accent.g, accent.b, 0.16f));
                var chipText = Theme.CreateText(chipBg, "Label", 16, TextAnchor.MiddleCenter, accent);
                chipText.text = $"{kv.Key} {(positive ? "+" : "")}{kv.Value}";
                Theme.StretchFull(chipText.rectTransform);
            }
        }

        private void AddQuoteCard(ChosenStep step, Dictionary<string, TechniqueInfo> byId, bool isStrength)
        {
            var opt = step.ChosenOption;
            byId.TryGetValue(opt.technique, out var info);
            var accent = isStrength ? Theme.Sage : Theme.Coral;

            var card = Theme.CreatePanel(endContent, isStrength ? "Strength" : "Improvement", new Color(accent.r, accent.g, accent.b, 0.08f));
            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(16, 16, 10, 10);
            cardLayout.spacing = 4;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var tag = Theme.CreateText(card, "Tag", 14, TextAnchor.UpperLeft, accent);
            tag.text = info != null ? info.ru_name.ToUpperInvariant() : opt.technique;

            var quote = Theme.CreateText(card, "Quote", 17, TextAnchor.UpperLeft, Theme.Parchment);
            quote.text = $"«{opt.text}»";

            if (!isStrength && !string.IsNullOrEmpty(opt.betterAlternative))
            {
                var better = Theme.CreateText(card, "Better", 17, TextAnchor.UpperLeft, Theme.Muted);
                better.text = $"Лучше: {opt.betterAlternative}";
            }

            if (info != null && !string.IsNullOrEmpty(info.theory_note_ru))
            {
                var theory = Theme.CreateText(card, "Theory", 15, TextAnchor.UpperLeft, Theme.EyebrowMuted);
                theory.text = info.theory_note_ru;
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

        private void OnOpenTheoryClicked()
        {
            Hide();
            OnOpenTheory?.Invoke(engine.TechniqueScores.Keys);
        }
    }
}
