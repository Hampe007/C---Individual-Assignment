using TMPro;
using UnityEngine;

namespace BrightnessCalibration
{
    public sealed class BrightnessSliderBinding : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Slider slider;
        [SerializeField] private TMP_Text valueLabel;
        private int appliedPercentage;
        private void OnEnable()
        {
            slider.minValue = BrightnessPreferences.Minimum * 100f;
            slider.maxValue = BrightnessPreferences.Maximum * 100f;
            Refresh();
            BrightnessPreferences.Changed += Refresh;
            slider.onValueChanged.AddListener(Change);
        }

        public void Refresh()
        {
            appliedPercentage = Mathf.RoundToInt(BrightnessPreferences.GetBrightness() * 100f);
            slider.SetValueWithoutNotify(appliedPercentage);
            ShowValue(appliedPercentage);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(Change);
            BrightnessPreferences.Changed -= Refresh;
            BrightnessPreferences.Save();
        }

        private void Change(float percentage)
        {
            int rounded = Mathf.RoundToInt(percentage);
            if (rounded == appliedPercentage)
            {
                return;
            }

            appliedPercentage = rounded;
            BrightnessPreferences.SetBrightness(rounded / 100f);
            ShowValue(rounded);
        }

        private void ShowValue(float percentage)
        {
            valueLabel.text = $"{Mathf.RoundToInt(percentage)}%";
        }
    }
}