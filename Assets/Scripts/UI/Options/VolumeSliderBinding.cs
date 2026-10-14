using TMPro;
using UnityEngine;

namespace GameMenus
{
    public sealed class VolumeSliderBinding : MonoBehaviour
    {
        [SerializeField] private AudioVolumeChannel channel;
        [SerializeField] private UnityEngine.UI.Slider slider;
        [SerializeField] private TMP_Text valueLabel;
        private int appliedPercentage;


        private void OnEnable()
        {
            float volume = AudioPreferences.GetVolume(channel);
            appliedPercentage = Mathf.RoundToInt(volume * 100f);
            slider.SetValueWithoutNotify(appliedPercentage);
            ShowValue(appliedPercentage);
            slider.onValueChanged.AddListener(Change);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(Change);
            AudioPreferences.Save();
        }

        private void Change(float percentage)
        {
            int rounded = Mathf.RoundToInt(percentage);
            if (rounded == appliedPercentage)
                return;
            appliedPercentage = rounded;
            AudioPreferences.SetVolume(channel, rounded / 100f);
            ShowValue(rounded);
        }

        private void ShowValue(float percentage)
        {
            valueLabel.text = $"{Mathf.RoundToInt(percentage)}%";
        }
    }
}
