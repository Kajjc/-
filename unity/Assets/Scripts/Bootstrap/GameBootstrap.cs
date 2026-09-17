using UnityEngine;

namespace Arena.Bootstrap
{
    // [RuntimeInitializeOnLoadMethod] запускает игру автоматически при входе в Play/сборке,
    // независимо от содержимого сцены — не нужно вручную собирать сцену/префабы в редакторе.
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            var runnerGo = new GameObject("ArenaBootstrapRunner");
            Object.DontDestroyOnLoad(runnerGo);
            runnerGo.AddComponent<GameFlow>().Begin();
        }
    }
}
