using System.Collections.Generic;
using Arena.Dialogue;
using UnityEngine;

namespace Arena.UI
{
    // Гипотеза "голос оппонента" (обсуждение по мотивам Graveyard Keeper):
    // короткие вокализации вместо полноценной озвучки, один блип на слово по
    // ходу печати реплики (см. DialogueUIController.TypeOpponentLine). Банк
    // звуков — купленный бесплатный ассет "Dialog Text - Sound Effects" (AD
    // Sounds), отобран по 4 подпапкам из него под конкретные настроения после
    // акустического анализа (питч/громкость) исходных файлов, не наугад по
    // названию — см. docs/feature-hypotheses.md.
    //
    // Пул источников, а не один AudioSource: PlayOneShot корректно накладывает
    // несколько одновременных воспроизведений на одном источнике, но питч
    // читается с источника в реальном времени — если менять его на лету между
    // соседними блипами, уже играющие (более длинные, до ~1.6с) блипы будут
    // слышимо "перепитчованы" на середине. Пул из нескольких источников с
    // раздельным питчем на каждый блип устраняет этот эффект.
    public class OpponentVoice : MonoBehaviour
    {
        private const int PoolSize = 6;

        private static readonly Dictionary<OpponentMood, string> FolderByMood = new Dictionary<OpponentMood, string>
        {
            { OpponentMood.Calm, "calm" },
            { OpponentMood.Pleased, "pleased" },
            { OpponentMood.Wary, "wary" },
            { OpponentMood.Irritated, "irritated" },
        };

        // Базовый питч/разброс/громкость на настроение — тот же принцип, что
        // проверялся на синтезированном прототипе (доволен выше и чуть громче,
        // раздражён ниже и резче), но поверх настоящих записанных вокализаций,
        // а не синтеза.
        private static readonly Dictionary<OpponentMood, (float pitch, float jitter, float volume)> Tuning = new Dictionary<OpponentMood, (float, float, float)>
        {
            { OpponentMood.Calm, (1.0f, 0.05f, 0.55f) },
            { OpponentMood.Pleased, (1.18f, 0.08f, 0.6f) },
            { OpponentMood.Wary, (0.95f, 0.07f, 0.5f) },
            { OpponentMood.Irritated, (0.85f, 0.1f, 0.65f) },
        };

        private readonly Dictionary<OpponentMood, AudioClip[]> clipsByMood = new Dictionary<OpponentMood, AudioClip[]>();
        private AudioSource[] pool;
        private int nextSource;

        private void Awake()
        {
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject($"BlipSource{i}");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                pool[i] = source;
            }

            foreach (var kv in FolderByMood)
                clipsByMood[kv.Key] = Resources.LoadAll<AudioClip>($"Audio/Blips/{kv.Value}");
        }

        public void PlayBlip(OpponentMood mood)
        {
            if (!clipsByMood.TryGetValue(mood, out var clips) || clips.Length == 0) return;

            var clip = clips[Random.Range(0, clips.Length)];
            var tuning = Tuning[mood];

            var source = pool[nextSource];
            nextSource = (nextSource + 1) % PoolSize;

            source.pitch = tuning.pitch * Random.Range(1f - tuning.jitter, 1f + tuning.jitter);
            source.PlayOneShot(clip, tuning.volume);
        }
    }
}
