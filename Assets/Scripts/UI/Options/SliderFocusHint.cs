using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrightnessCalibration
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class SliderFocusHint : MonoBehaviour
    {
        [SerializeField] private Slider[] sliders;
        private TMP_Text label;

        private void Awake() => label = GetComponent<TMP_Text>();

        private void Update()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            bool focused = false;
            if (selected != null)
            {
                foreach (Slider slider in sliders)
                {
                    if (slider != null && slider.gameObject == selected)
                    {
                        focused = true;
                        break;
                    }
                }
            }

            label.enabled = focused;
        }
    }
}
