namespace Arena.Dialogue
{
    public struct SkillTestQuestion
    {
        public string Text;
        public string Skill;

        public SkillTestQuestion(string text, string skill)
        {
            Text = text;
            Skill = skill;
        }
    }

    // 18 утверждений и порядок показа — см. docs/skill-test.md (методология и
    // психометрическая основа). Контракт: сумма 6-9 -> уровень 1, 10-13 -> 2, 14-18 -> 3.
    public static class SkillTestData
    {
        public static readonly SkillTestQuestion[] Questions =
        {
            // 1-6: Напор
            new SkillTestQuestion("Если оппонент называет условие или цену первым, я обычно сразу называю встречное требование, не дожидаясь его следующего хода.", "napor"),
            new SkillTestQuestion("Мне легко сказать «нет» и настоять на своём, даже если собеседник давит на меня.", "napor"),
            new SkillTestQuestion("Я не боюсь сразу озвучить, чего хочу добиться, с самого начала разговора.", "napor"),
            new SkillTestQuestion("Если пауза в разговоре затягивается, я скорее возьму инициативу и предложу своё условие, чем буду ждать.", "napor"),
            new SkillTestQuestion("Мне комфортно повторить свою позицию ещё раз, даже если собеседник её уже отклонил.", "napor"),
            new SkillTestQuestion("Я готов уйти из переговоров, если условия не устраивают, а не соглашаться «лишь бы закрыть вопрос».", "napor"),
            // 7-12: Эмпатия
            new SkillTestQuestion("Я легко замечаю, когда собеседник расстроен или напряжён, даже если он это не говорит прямо.", "empatiya"),
            new SkillTestQuestion("Мне важно понять, что на самом деле беспокоит другую сторону, прежде чем предлагать решение.", "empatiya"),
            new SkillTestQuestion("Я часто мысленно ставлю себя на место оппонента, чтобы понять его мотивы.", "empatiya"),
            new SkillTestQuestion("Если вижу, что собеседник разочарован, мне хочется признать это вслух, прежде чем продолжать спор.", "empatiya"),
            new SkillTestQuestion("Я замечаю изменение тона голоса или манеры речи собеседника и реагирую на это.", "empatiya"),
            new SkillTestQuestion("Мне легче договориться, когда я сначала показываю, что услышал позицию другого человека.", "empatiya"),
            // 13-18: Логика
            new SkillTestQuestion("Мне нравится раскладывать сложную ситуацию на факты и цифры, прежде чем принимать решение.", "logika"),
            new SkillTestQuestion("Я предпочитаю подкреплять свою позицию конкретными данными или объективными критериями, а не только личным мнением.", "logika"),
            new SkillTestQuestion("Мне интересно разбираться в деталях условия сделки, а не просто соглашаться на общие формулировки.", "logika"),
            new SkillTestQuestion("Я люблю заранее продумать несколько аргументов на случай возражений собеседника.", "logika"),
            new SkillTestQuestion("Мне проще убедить кого-то через логичную цепочку рассуждений, чем через эмоции.", "logika"),
            new SkillTestQuestion("Я трачу время на то, чтобы сравнить варианты по критериям, прежде чем сделать выбор.", "logika"),
        };

        // Перемешанный порядок показа (1-индексация из docs/skill-test.md, приложение).
        public static readonly int[] DisplayOrder =
        {
            7, 13, 1, 9, 16, 4, 11, 2, 14, 8, 5, 17, 10, 3, 18, 6, 12, 15
        };

        public static readonly string[] AnswerLabels =
        {
            "Не характерно для меня",
            "Отчасти характерно",
            "Характерно для меня"
        };

        public static int LevelFromSum(int sum)
        {
            if (sum <= 9) return 1;
            if (sum <= 13) return 2;
            return 3;
        }
    }
}
