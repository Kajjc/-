using System;
using System.Collections.Generic;
using System.Linq;

namespace Arena.Dialogue
{
    public class ChosenStep
    {
        public string NodeId;
        public string OpponentLine;
        public DialogueOption ChosenOption;
    }

    // Гипотеза Ю7-вариант-А (docs/feature-hypotheses.md): видимая реакция оппонента
    // на последнюю реплику игрока — без LLM и без новых полей в JSON сценариев,
    // чисто по знаку/силе points только что выбранной опции.
    // Neutral — только самое начало разговора, пока игрок ещё не сделал ход и
    // реакции нет; дальше всегда одно из четырёх остальных. Calm — не "старт", а
    // сдержанная реакция на обычный ход (+1 или 0).
    public enum OpponentMood
    {
        Neutral,
        Calm,
        Pleased,
        Wary,
        Irritated
    }

    // Обходит статичное дерево диалога: гейтит опции по навыкам игрока,
    // копит очки по тегам техник, ведёт транскрипт выбранных реплик для экрана фидбека.
    public class DialogueEngine
    {
        private readonly PlayerSkills skills;
        private readonly Dictionary<string, DialogueNode> nodesById;

        public ScenarioData Scenario { get; }
        public PlayerSkills Skills => skills;
        public DialogueNode CurrentNode { get; private set; }
        public Dictionary<string, int> TechniqueScores { get; } = new Dictionary<string, int>();
        public List<ChosenStep> Transcript { get; } = new List<ChosenStep>();

        public bool IsTerminal => !string.IsNullOrEmpty(CurrentNode.outcome);

        public DialogueEngine(ScenarioData scenario, PlayerSkills skills)
        {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (scenario.nodes == null || scenario.nodes.Length == 0)
                throw new ArgumentException("Сценарий не содержит узлов.", nameof(scenario));

            this.Scenario = scenario;
            this.skills = skills ?? new PlayerSkills();

            nodesById = new Dictionary<string, DialogueNode>();
            foreach (var node in scenario.nodes)
                nodesById[node.id] = node;

            if (!nodesById.TryGetValue(scenario.startNode, out var startNode))
                throw new ArgumentException($"startNode '{scenario.startNode}' не найден среди узлов сценария.");
            CurrentNode = startNode;
        }

        public bool IsOptionAvailable(DialogueOption option)
        {
            if (option.requiredSkills == null) return true;
            foreach (var req in option.requiredSkills)
            {
                if (skills.GetLevel(req.skill) < req.level) return false;
            }
            return true;
        }

        // Недостающие требования по опции — для UI, который рисует их иконкой навыка
        // нужного цвета + "≥N" (гипотеза Ю5, docs/feature-hypotheses.md), а не текстом.
        public IEnumerable<SkillRequirement> GetMissingRequirements(DialogueOption option)
        {
            if (option.requiredSkills == null) return Enumerable.Empty<SkillRequirement>();
            return option.requiredSkills.Where(r => skills.GetLevel(r.skill) < r.level);
        }

        public void ChooseOption(DialogueOption option)
        {
            if (IsTerminal)
                throw new InvalidOperationException("Диалог уже завершён — нельзя выбрать реплику.");
            if (!CurrentNode.options.Contains(option))
                throw new ArgumentException("Опция не принадлежит текущему узлу.");
            if (!IsOptionAvailable(option))
                throw new InvalidOperationException("Опция заблокирована требованиями к навыкам.");

            Transcript.Add(new ChosenStep
            {
                NodeId = CurrentNode.id,
                OpponentLine = CurrentNode.opponentLine,
                ChosenOption = option
            });

            if (!string.IsNullOrEmpty(option.technique))
            {
                TechniqueScores.TryGetValue(option.technique, out var current);
                TechniqueScores[option.technique] = current + option.points;
            }

            if (!nodesById.TryGetValue(option.next, out var nextNode))
                throw new ArgumentException($"Узел '{option.next}', на который ссылается опция, не найден.");
            CurrentNode = nextNode;
        }

        // Реакция на только что сделанный ход игрока — по баллу последней выбранной
        // опции. До первого хода (на входе, до выбора) оппонент нейтрален.
        public OpponentMood GetOpponentMood()
        {
            if (Transcript.Count == 0) return OpponentMood.Neutral;

            int lastPoints = Transcript[Transcript.Count - 1].ChosenOption.points;
            if (lastPoints >= 2) return OpponentMood.Pleased;
            if (lastPoints <= -2) return OpponentMood.Irritated;
            if (lastPoints == -1) return OpponentMood.Wary;
            return OpponentMood.Calm;
        }
    }
}
