using System;

namespace Arena.Dialogue
{
    // Контракт зафиксирован в docs/skill-test.md: 1 = слабо, 2 = умеренно, 3 = сильно.
    [Serializable]
    public class PlayerSkills
    {
        public int napor = 2;
        public int empatiya = 2;
        public int logika = 2;

        public int GetLevel(string skill)
        {
            switch (skill)
            {
                case "napor": return napor;
                case "empatiya": return empatiya;
                case "logika": return logika;
                default: return 0;
            }
        }
    }
}
