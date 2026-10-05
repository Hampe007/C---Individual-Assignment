using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GameMenus
{
    public static class BrightnessPreferences
    {
        private const string PreferenceKey = "Display.Brightness";
        public const float Minimum = 0.5f;
        public const float Maximum = 3f;

        public static float GetBrightness()
        {
            return Mathf.Clamp(PlayerPrefs.GetFloat(PreferenceKey, 1f), Minimum, Maximum);
        }

        public static void SetBrightness(float brightness)
        {
            PlayerPrefs.SetFloat(PreferenceKey, Mathf.Clamp(brightness, Minimum, Maximum));
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            SceneManager.sceneLoaded -= ApplyToScene;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += ApplyToScene;
        }

        private static void ApplyToScene(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private static void Apply()
        {
            float exposureOffset = Mathf.Log(GetBrightness(), 2f);
            foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!volume.isGlobal || volume.sharedProfile == null || !volume.sharedProfile.TryGet(out ColorAdjustments authored) || !authored.active || !authored.postExposure.overrideState)
                {
                    continue;
                }

                // Always start from the authored exposure to avoid accumulating slider changes.
                // Volume.profile clones the asset, so player preferences never modify project assets.
                if (volume.profile.TryGet(out ColorAdjustments runtime))
                {
                    runtime.postExposure.Override(authored.postExposure.value + exposureOffset);
                }
            }
        }
    }
}
