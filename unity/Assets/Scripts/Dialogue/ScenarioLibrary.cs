using System.Collections.Generic;
using UnityEngine;

namespace Arena.Dialogue
{
    // Библиотека сценариев: грузит все JSON из Resources/Scenarios и подбирает
    // подходящий по тегам, которые задаёт админ-экран (сфера/сложность/тон).
    // "Подбор" вместо "генерации" — осознанное решение MVP без LLM (см. план проекта).
    public static class ScenarioLibrary
    {
        private const string ResourceFolder = "Scenarios";

        public static List<ScenarioData> LoadAll()
        {
            var textAssets = Resources.LoadAll<TextAsset>(ResourceFolder);
            var result = new List<ScenarioData>();
            foreach (var asset in textAssets)
            {
                ScenarioData data = null;
                try
                {
                    data = JsonUtility.FromJson<ScenarioData>(asset.text);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Не удалось разобрать сценарий '{asset.name}': {e.Message}");
                }

                if (data?.meta != null && data.nodes != null && data.nodes.Length > 0)
                    result.Add(data);
                else
                    Debug.LogError($"Сценарий '{asset.name}' пуст или повреждён — пропущен.");
            }
            return result;
        }

        // Точное совпадение по сфере/тону весит больше, чем близость по сложности.
        public static ScenarioData Pick(List<ScenarioData> library, string sphere, int difficulty, string tone)
        {
            if (library == null || library.Count == 0) return null;

            ScenarioData best = null;
            int bestScore = int.MinValue;
            foreach (var scenario in library)
            {
                int score = 0;
                if (!string.IsNullOrEmpty(sphere) && scenario.meta.sphere == sphere) score += 10;
                if (!string.IsNullOrEmpty(tone) && scenario.meta.tone == tone) score += 5;
                score -= Mathf.Abs(scenario.meta.difficulty - difficulty);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = scenario;
                }
            }
            return best;
        }
    }
}
