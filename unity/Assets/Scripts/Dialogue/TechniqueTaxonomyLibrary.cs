using System;
using UnityEngine;

namespace Arena.Dialogue
{
    [Serializable]
    public class DomainCentrality
    {
        public string hr;
        public string sales;
        public string procurement;
    }

    [Serializable]
    public class TechniqueInfo
    {
        public string id;
        public string ru_name;
        public string definition_ru;
        public int[] points_range;
        public DomainCentrality domain_centrality;
        public string signal_ru;
        public string theory_note_ru;
        public string theory_source;
    }

    [Serializable]
    public class TechniqueTaxonomy
    {
        public string _comment;
        public string[] _domainCentralityValues;
        public TechniqueInfo[] techniques;
    }

    // Загружает Resources/TechniqueTaxonomy.json (зеркало docs/technique-taxonomy.json,
    // держать в синхроне — см. комментарий в обоих файлах) для экрана "Теория и техники".
    public static class TechniqueTaxonomyLibrary
    {
        private static TechniqueTaxonomy cached;

        public static TechniqueTaxonomy Load()
        {
            if (cached != null) return cached;

            var asset = Resources.Load<TextAsset>("TechniqueTaxonomy");
            if (asset == null)
            {
                Debug.LogError("Resources/TechniqueTaxonomy.json не найден.");
                cached = new TechniqueTaxonomy { techniques = new TechniqueInfo[0] };
                return cached;
            }

            cached = JsonUtility.FromJson<TechniqueTaxonomy>(asset.text);
            return cached;
        }
    }
}
