using System;

namespace Arena.Dialogue
{
    [Serializable]
    public class SkillRequirement
    {
        public string skill;
        public int level;
    }

    [Serializable]
    public class DialogueOption
    {
        public string text;
        public SkillRequirement[] requiredSkills;
        public string technique;
        public int points;
        public string next;
        public string betterAlternative;
    }

    [Serializable]
    public class DialogueNode
    {
        public string id;
        public string opponentLine;
        public DialogueOption[] options;
        public string outcome;
        public string summary;
    }

    [Serializable]
    public class ScenarioMeta
    {
        public string id;
        public string sphere;
        public string topic;
        public int difficulty;
        public string tone;
        public string opponentRole;
    }

    [Serializable]
    public class ScenarioData
    {
        public ScenarioMeta meta;
        public string startNode;
        public DialogueNode[] nodes;
    }
}
