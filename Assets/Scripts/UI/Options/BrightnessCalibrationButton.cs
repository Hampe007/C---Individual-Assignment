using UnityEngine;

namespace GameMenus
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class BrightnessCalibrationButton : MonoBehaviour
    {
        private void OnEnable() => GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Open);
        private void OnDisable() => GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(Open);
        private void Open() => BrightnessCalibrationScreen.ShowFromOptions(gameObject);
    }
}
