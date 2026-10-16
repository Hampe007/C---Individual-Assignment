using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace BrightnessCalibration
{
    public sealed class BrightnessCalibrationScreen : MonoBehaviour
    {
        [Header("Skull calibration")]
        [SerializeField, Range(1, 254)] private int leftLevel = 1;
        [SerializeField, Range(2, 255)] private int rightLevel = 175;
        [SerializeField, Range(1, 254)] private int leftAtMaximum = 24;
        [Header("Reference rig")]
        [SerializeField] private UnityEngine.UI.Image leftHalf;
        [SerializeField] private UnityEngine.UI.Image rightHalf;
        [Header("Screen references")]
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
        private int appliedPercentage;
        private Material leftMaterial;
        private Material rightMaterial;
        public UnityEngine.UI.Image LeftHalf => leftHalf;
        public UnityEngine.UI.Image RightHalf => rightHalf;
        public Camera PreviewCamera => previewCamera;
        public Volume PreviewVolume => previewVolume;

        private void OnValidate()
        {
            rightLevel = Mathf.Clamp(rightLevel, 2, 255);
            leftLevel = Mathf.Clamp(leftLevel, 1, rightLevel - 1);
            leftAtMaximum = Mathf.Clamp(leftAtMaximum, leftLevel, rightLevel - 1);
        }

        public float GetLeftLevel(float brightness)
        {
            float progress = Mathf.InverseLerp(1f, BrightnessPreferences.Maximum, brightness);
            return Mathf.Lerp(leftLevel, leftAtMaximum, Mathf.SmoothStep(0f, 1f, progress));
        }

        public static Material CreateSilhouetteMaterial(UnityEngine.UI.Image image) => new Material(image.material)
        {
            name = "Brightness calibration tint",
            hideFlags = HideFlags.HideAndDontSave
        };
        public static void SetMaterialLevel(Material material, float level)
        {
            // A material color preserves near-black precision lost by UI vertex colors.
            float value = Mathf.Clamp(level, 0f, 255f) / 255f;
            material.SetColor("_Color", new Color(value, value, value, 1f));
        }

        private void ApplyReference(float brightness)
        {
            if (leftMaterial == null)
            {
                leftMaterial = CreateSilhouetteMaterial(leftHalf);
                rightMaterial = CreateSilhouetteMaterial(rightHalf);
                leftHalf.material = leftMaterial;
                rightHalf.material = rightMaterial;
                leftHalf.color = rightHalf.color = Color.white;
            }

            SetMaterialLevel(leftMaterial, GetLeftLevel(brightness));
            SetMaterialLevel(rightMaterial, rightLevel);
        }

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
            slider.minValue = BrightnessPreferences.Minimum * 100f;
            slider.maxValue = BrightnessPreferences.Maximum * 100f;
            slider.SetValueWithoutNotify(Mathf.RoundToInt(BrightnessPreferences.GetBrightness() * 100f));
            appliedPercentage = -1;
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

            if (fromOptions && (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
            {
                Confirm();
            }

            // A mouse click on the background can clear focus; restore it on navigation.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && (Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) || Gamepad.current != null && (Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f)))
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

        private static bool SubmitHeld() => Keyboard.current != null && (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed || Keyboard.current.spaceKey.isPressed) || Gamepad.current != null && Gamepad.current.buttonSouth.isPressed || Mouse.current != null && Mouse.current.leftButton.isPressed;
        private void UpdateValue(float percentage)
        {
            int rounded = Mathf.RoundToInt(percentage);
            if (rounded == appliedPercentage)
            {
                return;
            }

            appliedPercentage = rounded;
            BrightnessPreferences.SetBrightness(rounded / 100f);
            ApplyReference(rounded / 100f);
            valueLabel.text = $"{rounded}%";
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
            if (leftMaterial != null)
            {
                Destroy(leftMaterial);
            }

            if (rightMaterial != null)
            {
                Destroy(rightMaterial);
            }

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
                if (returnSelection != null && returnSelection.activeInHierarchy && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(returnSelection);
                }
            }
        }
    }
}