using System;
using UnityEngine;

namespace GameMenus
{
    public static class HealthBarLocationPreferences
    {
        private const string PreferenceKey = "HealthBarOnScreen";
        public static event Action Changed;
        public static bool OnScreen => PlayerPrefs.GetInt(PreferenceKey, 0) != 0;

        public static void SetOnScreen(bool onScreen)
        {
            if (OnScreen == onScreen)
            {
                return;
            }

            PlayerPrefs.SetInt(PreferenceKey, onScreen ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}