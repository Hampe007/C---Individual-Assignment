using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GameMenus
{
    public static class BrightnessPreferences
    {
        private const string PreferenceKey = "Display.Brightness";
        private const string CalibrationKey = "Display.BrightnessCalibrated";
        private const string ScaleVersionKey = "Display.BrightnessScaleVersion";
        private const int CurrentVersion = 2;
        private const float BaselineMultiplier = 1.5f;
        public static bool HasCalibrated => PlayerPrefs.GetInt(CalibrationKey, 0) == CurrentVersion;
        public const float Minimum = 0.5f;
        public const float Maximum = 3f;

        public static float GetBrightness()
        {
            MigrateScale();
            return Mathf.Clamp(PlayerPrefs.GetFloat(PreferenceKey, 1f), Minimum, Maximum);
        }

        public static void SetBrightness(float brightness)
        {
            MigrateScale();
            PlayerPrefs.SetFloat(PreferenceKey, Mathf.Clamp(brightness, Minimum, Maximum));
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        public static void ConfirmCalibration()
        {
            PlayerPrefs.SetInt(CalibrationKey, CurrentVersion);
            Save();
        }

        private static void MigrateScale()
        {
            if (PlayerPrefs.GetInt(ScaleVersionKey, 0) >= CurrentVersion)
            {
                return;
            }
            if (PlayerPrefs.HasKey(PreferenceKey))
            {
                float migrated = PlayerPrefs.GetFloat(PreferenceKey) / BaselineMultiplier;
                PlayerPrefs.SetFloat(PreferenceKey, Mathf.Clamp(migrated, Minimum, Maximum));
            }
            PlayerPrefs.SetInt(ScaleVersionKey, CurrentVersion);
            Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            SceneManager.sceneLoaded -= ApplyToScene;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            MigrateScale();
            SceneManager.sceneLoaded += ApplyToScene;
        }

        private static void ApplyToScene(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private static void Apply()
        {
            float exposureOffset = Mathf.Log(GetBrightness() * BaselineMultiplier, 2f);
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
