using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace GameMenus
{
    public sealed class BrightnessCalibrationScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup presentation;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Volume previewVolume;
        [SerializeField] private UnityEngine.UI.RawImage referenceImage;
        [SerializeField] private UnityEngine.UI.Slider slider;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private UnityEngine.UI.Button confirmButton;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private UnityEngine.UI.Button resetButton;
        private static BrightnessCalibrationScreen instance;
        private RenderTexture previewTexture;
        private GameObject returnSelection;
        private float previousTimeScale;
        private bool previousAudioPause;
        private bool fromOptions;
        private bool ready;
        private bool closing;

        public static bool IsOpen => instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => instance = null;

        public static IEnumerator ShowAtStartup()
        {
            BrightnessCalibrationScreen screen = Open(false, null);
            while (screen != null)
            {
                yield return null;
            }
        }

        public static void ShowFromOptions(GameObject returnSelection) => Open(true, returnSelection);

        private static BrightnessCalibrationScreen Open(bool fromOptions, GameObject returnSelection)
        {
            if (instance != null)
            {
                return instance;
            }
            GameObject prefab = Resources.Load<GameObject>("BrightnessCalibration");
            if (prefab == null)
            {
                Debug.LogError("The brightness calibration prefab is missing.");
                return null;
            }
            if (!BrightnessPreferences.HasCalibrated)
            {
                BrightnessPreferences.SetBrightness(Mathf.Max(1f, BrightnessPreferences.GetBrightness()));
            }
            instance = Instantiate(prefab).GetComponent<BrightnessCalibrationScreen>();
            DontDestroyOnLoad(instance.gameObject);
            instance.fromOptions = fromOptions;
            instance.returnSelection = returnSelection;
            instance.confirmLabel.text = fromOptions ? "Done" : "Continue";
            instance.StartCoroutine(instance.Present());
            return instance;
        }

        private void Awake()
        {
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            PausePresentation();
            presentation.alpha = 0f;
            presentation.interactable = false;
            confirmButton.interactable = false;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            previewTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
            {
                name = "Brightness Calibration Preview",
                antiAliasing = 1
            };
            previewTexture.Create();
            previewCamera.targetTexture = previewTexture;
            referenceImage.texture = previewTexture;
            slider.SetValueWithoutNotify(BrightnessPreferences.GetBrightness() * 100f);
            UpdateValue(slider.value);
            slider.onValueChanged.AddListener(UpdateValue);
            confirmButton.onClick.AddListener(Confirm);
            resetButton.onClick.AddListener(ResetToRecommended);
        }

        private IEnumerator Present()
        {
            yield return Fade(1f);
            while (SubmitHeld())
            {
                yield return null;
            }
            ready = true;
            presentation.interactable = true;
            confirmButton.interactable = true;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(slider.gameObject);
            }
        }

        private void Update()
        {
            if (!ready || !Application.isFocused)
            {
                return;
            }
            if (fromOptions && (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
                || Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
            {
                Confirm();
            }
            // A mouse click on the background can clear focus; restore it on navigation.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null
                && (Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame
                    || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                    || Gamepad.current != null && (Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f
                        || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f)))
            {
                EventSystem.current.SetSelectedGameObject(slider.gameObject);
            }
        }

        private void LateUpdate() => PausePresentation();

        private static void PausePresentation()
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        private static bool SubmitHeld() => Keyboard.current != null
            && (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed || Keyboard.current.spaceKey.isPressed)
            || Gamepad.current != null && Gamepad.current.buttonSouth.isPressed
            || Mouse.current != null && Mouse.current.leftButton.isPressed;

        private void UpdateValue(float percentage)
        {
            BrightnessPreferences.SetBrightness(percentage / 100f);
            valueLabel.text = $"{Mathf.RoundToInt(percentage)}%";
        }

        private void ResetToRecommended() => slider.value = 100f;

        public void Confirm()
        {
            if (!ready || closing)
            {
                return;
            }
            ready = false;
            closing = true;
            presentation.interactable = false;
            confirmButton.interactable = false;
            BrightnessPreferences.ConfirmCalibration();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            StartCoroutine(Close());
        }

        private IEnumerator Close()
        {
            yield return Fade(0f);
            Destroy(gameObject);
        }

        private IEnumerator Fade(float target)
        {
            float start = presentation.alpha;
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                presentation.alpha = Mathf.SmoothStep(start, target, elapsed / 0.25f);
                yield return null;
            }
            presentation.alpha = target;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            slider.onValueChanged.RemoveListener(UpdateValue);
            confirmButton.onClick.RemoveListener(Confirm);
            resetButton.onClick.RemoveListener(ResetToRecommended);
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            previewCamera.enabled = false;
            previewCamera.targetTexture = null;
            referenceImage.texture = null;
            if (previewTexture != null)
            {
                previewTexture.Release();
                Destroy(previewTexture);
            }
            if (previewVolume.HasInstantiatedProfile())
            {
                VolumeProfile runtimeProfile = previewVolume.profile;
                foreach (VolumeComponent component in runtimeProfile.components)
                {
                    Destroy(component);
                }
                Destroy(runtimeProfile);
            }
            if (fromOptions)
            {
                foreach (BrightnessSliderBinding binding in FindObjectsByType<BrightnessSliderBinding>(FindObjectsSortMode.None))
                {
                    binding.Refresh();
                }
                if (returnSelection != null && returnSelection.activeInHierarchy && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(returnSelection);
                }
            }
        }
    }
}
