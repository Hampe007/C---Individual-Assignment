using TMPro;
using UnityEngine;

namespace GameMenus
{
    public sealed class BrightnessSliderBinding : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Slider slider;
        [SerializeField] private TMP_Text valueLabel;

        private void OnEnable()
        {
            float percentage = BrightnessPreferences.GetBrightness() * 100f;
            slider.SetValueWithoutNotify(percentage);
            ShowValue(percentage);
            slider.onValueChanged.AddListener(Change);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(Change);
            BrightnessPreferences.Save();
        }

        private void Change(float percentage)
        {
            BrightnessPreferences.SetBrightness(percentage / 100f);
            ShowValue(percentage);
        }

        private void ShowValue(float percentage)
        {
            valueLabel.text = $"{Mathf.RoundToInt(percentage)}%";
        }
    }
}
