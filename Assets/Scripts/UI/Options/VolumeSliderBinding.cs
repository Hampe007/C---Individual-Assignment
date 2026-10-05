using TMPro;
using UnityEngine;

namespace GameMenus
{
    public sealed class VolumeSliderBinding : MonoBehaviour
    {
        [SerializeField] private AudioVolumeChannel channel;
        [SerializeField] private UnityEngine.UI.Slider slider;
        [SerializeField] private TMP_Text valueLabel;


        private void OnEnable()
        {
            float volume = AudioPreferences.GetVolume(channel);
            slider.SetValueWithoutNotify(volume);
            ShowValue(volume);
            slider.onValueChanged.AddListener(Change);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(Change);
            AudioPreferences.Save();
        }

        private void Change(float volume)
        {
            AudioPreferences.SetVolume(channel, volume);
            ShowValue(volume);
        }

        private void ShowValue(float volume)
        {
            valueLabel.text = $"{Mathf.RoundToInt(volume * 100f)}%";
        }
    }
}
