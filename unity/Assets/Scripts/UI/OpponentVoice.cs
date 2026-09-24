using System.Collections.Generic;
using Arena.Dialogue;
using UnityEngine;

namespace Arena.UI
{
    // Гипотеза "голос оппонента" (обсуждение по мотивам Graveyard Keeper):
    // короткие вокализации вместо полноценной озвучки, один звук на слово по
    // ходу печати реплики (см. DialogueUIController.TypeOpponentLine). Банк звуков —
    // нарезка из четырёх образцов gibberish-голоса (по одному на настроение), которые
    // выбрал автор проекта: scripts/slice_voice.py режет каждую запись-бормотание на
    // куски 0.1-0.6 с по паузам и провалам громкости, чтобы кусок звучал как одно
    // "слово". Исходные записи лежат вне Assets — assets-src/voices/.
    //
    // Пул источников, а не один AudioSource: PlayOneShot корректно накладывает
    // несколько одновременных воспроизведений на одном источнике, но питч
    // читается с источника в реальном времени — если менять его на лету между
    // соседними блипами, уже играющие блипы будут
    // слышимо "перепитчованы" на середине. Пул из нескольких источников с
    // раздельным питчем на каждый блип устраняет этот эффект.
    public class OpponentVoice : MonoBehaviour
    {
        private const int PoolSize = 6;

        // Neutral звучит только на первой реплике разговора — отдельный банк ради
        // одной реплики не нужен, берём банк "спокоен".
        private static readonly Dictionary<OpponentMood, string> FolderByMood = new Dictionary<OpponentMood, string>
        {
            { OpponentMood.Neutral, "calm" },
            { OpponentMood.Calm, "calm" },
            { OpponentMood.Pleased, "pleased" },
            { OpponentMood.Wary, "wary" },
            { OpponentMood.Irritated, "irritated" },
        };

        // Базовый питч/разброс/громкость на настроение. Сами образцы уже разные по
        // характеру (их подбирали под настроения), поэтому питч почти не сдвигаем — только
        // слегка "светлее" у довольного; разброс нужен, чтобы одни и те же несколько
        // кусков не звучали механически. Громкость — доля от выровненных по RMS клипов.
        private static readonly Dictionary<OpponentMood, (float pitch, float jitter, float volume)> Tuning = new Dictionary<OpponentMood, (float, float, float)>
        {
            { OpponentMood.Neutral, (1.0f, 0.06f, 0.55f) },
            { OpponentMood.Calm, (1.0f, 0.06f, 0.55f) },
            { OpponentMood.Pleased, (1.04f, 0.07f, 0.6f) },
            { OpponentMood.Wary, (1.0f, 0.06f, 0.55f) },
            { OpponentMood.Irritated, (1.0f, 0.07f, 0.6f) },
        };

        private readonly Dictionary<OpponentMood, AudioClip[]> clipsByMood = new Dictionary<OpponentMood, AudioClip[]>();
        // Последний сыгранный кусок по настроению: клипов в банке мало (3-7), и один и
        // тот же дважды подряд звучал бы как заевшая пластинка.
        private readonly Dictionary<OpponentMood, int> lastClipIndex = new Dictionary<OpponentMood, int>();
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

            int index = Random.Range(0, clips.Length);
            if (clips.Length > 1 && lastClipIndex.TryGetValue(mood, out var last) && index == last)
                index = (index + 1 + Random.Range(0, clips.Length - 1)) % clips.Length;
            lastClipIndex[mood] = index;
            var clip = clips[index];
            var tuning = Tuning[mood];

            var source = pool[nextSource];
            nextSource = (nextSource + 1) % PoolSize;

            source.pitch = tuning.pitch * Random.Range(1f - tuning.jitter, 1f + tuning.jitter);
            source.PlayOneShot(clip, tuning.volume);
        }
    }
}
