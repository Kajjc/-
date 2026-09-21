using System.Collections.Generic;

namespace Arena.Dialogue
{
    // Гипотеза Г8 (docs/feature-hypotheses.md): накопленный профиль переговорщика
    // за все прогоны в рамках одной открытой сессии, без сохранения на диск —
    // сессия не обязана переживать перезапуск (см. правило демо в docs/roadmap.md).
    // Владелец — DialogueUIController, который сам не пересоздаётся между "Пройти
    // ещё раз"/"Другой сценарий", поэтому один экземпляр естественно копит данные
    // по всем прогонам, пока не будет перезагружена сцена/страница.
    public class NegotiatorProfile
    {
        private readonly List<PlayerSkills> runs = new List<PlayerSkills>();
        private readonly Dictionary<string, int> techniqueScoresTotal = new Dictionary<string, int>();

        public int RunCount => runs.Count;
        public IReadOnlyDictionary<string, int> TechniqueScoresTotal => techniqueScoresTotal;

        public void RecordRun(PlayerSkills skills, Dictionary<string, int> techniqueScores)
        {
            runs.Add(skills);
            foreach (var kv in techniqueScores)
            {
                techniqueScoresTotal.TryGetValue(kv.Key, out var current);
                techniqueScoresTotal[kv.Key] = current + kv.Value;
            }
        }

        public (float napor, float empatiya, float logika) AverageSkills()
        {
            if (runs.Count == 0) return (0f, 0f, 0f);
            float napor = 0f, empatiya = 0f, logika = 0f;
            foreach (var s in runs)
            {
                napor += s.napor;
                empatiya += s.empatiya;
                logika += s.logika;
            }
            return (napor / runs.Count, empatiya / runs.Count, logika / runs.Count);
        }
    }
}
