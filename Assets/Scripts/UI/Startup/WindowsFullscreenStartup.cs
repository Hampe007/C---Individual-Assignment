using UnityEngine;

namespace GameMenus
{
    internal static class WindowsFullscreenStartup
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Unity restores the last window mode independently of the Player Settings defaults.
            Resolution desktop = Screen.currentResolution;
            Screen.SetResolution(desktop.width, desktop.height, FullScreenMode.FullScreenWindow);
        }
#endif
    }
}
