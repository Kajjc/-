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
        // Отдельно от очков — гипотеза "Профиль переговорщика (архетип)" считает
        // архетип по ЧАСТОТЕ выбора техники, а не по сумме баллов, поэтому нужен
        // отдельный счётчик количества выборов на каждый тег.
        private readonly Dictionary<string, int> techniqueCountsTotal = new Dictionary<string, int>();
        // Идея "Микро-достижения (бейджи)": каждый бейдж показывается как "новый"
        // только один раз за сессию — здесь хранится, какие уже были показаны.
        private readonly HashSet<string> unlockedBadgeIds = new HashSet<string>();

        public int RunCount => runs.Count;
        public IReadOnlyDictionary<string, int> TechniqueScoresTotal => techniqueScoresTotal;
        public IReadOnlyDictionary<string, int> TechniqueCountsTotal => techniqueCountsTotal;
        public HashSet<string> UnlockedBadgeIds => unlockedBadgeIds;

        public void RecordRun(PlayerSkills skills, Dictionary<string, int> techniqueScores, IEnumerable<string> techniqueSequence)
        {
            runs.Add(skills);
            foreach (var kv in techniqueScores)
            {
                techniqueScoresTotal.TryGetValue(kv.Key, out var current);
                techniqueScoresTotal[kv.Key] = current + kv.Value;
            }
            foreach (var id in techniqueSequence)
            {
                if (string.IsNullOrEmpty(id)) continue;
                techniqueCountsTotal.TryGetValue(id, out var current);
                techniqueCountsTotal[id] = current + 1;
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
