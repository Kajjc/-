using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arena.Dialogue
{
    public class ChosenStep
    {
        public string NodeId;
        public string OpponentLine;
        public DialogueOption ChosenOption;
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

        // Человекочитаемая подпись недостающих требований, для показа заблокированной опции в UI.
        public string GetLockLabel(DialogueOption option)
        {
            if (option.requiredSkills == null) return string.Empty;
            var missing = option.requiredSkills.Where(r => skills.GetLevel(r.skill) < r.level);
            var sb = new StringBuilder();
            foreach (var r in missing)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(DisplayName(r.skill)).Append(" ≥ ").Append(r.level);
            }
            return sb.Length > 0 ? $"Требуется: {sb}" : string.Empty;
        }

        private static string DisplayName(string skill)
        {
            switch (skill)
            {
                case "napor": return "Напор";
                case "empatiya": return "Эмпатия";
                case "logika": return "Логика";
                default: return skill;
            }
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
    }
}
