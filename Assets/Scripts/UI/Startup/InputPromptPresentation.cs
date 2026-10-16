using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameMenus
{
    public sealed class InputPromptPresentation : MonoBehaviour
    {
        [SerializeField] private GameObject[] keyboardObjects;
        [SerializeField] private GameObject[] controllerObjects;
        [SerializeField] private Image southIcon;
        [SerializeField] private Image eastIcon;
        [SerializeField] private TMP_Text southFallbackLabel;
        [SerializeField] private TMP_Text eastFallbackLabel;
        [SerializeField] private RectTransform skipActionLabel;
        [SerializeField] private Sprite xboxSouth;
        [SerializeField] private Sprite xboxEast;
        [SerializeField] private Sprite playStationSouth;
        [SerializeField] private Sprite playStationEast;
        [SerializeField] private Sprite genericButton;
        private static bool initialized;
        private static bool controllerWasLast;
        private bool displayedController;
        private Gamepad displayedGamepad;
        private void OnEnable()
        {
            if (!initialized)
            {
                controllerWasLast = Gamepad.current != null;
                initialized = true;
            }

            TrackInput();
            Refresh(true);
        }

        private void Update()
        {
            TrackInput();
            Refresh(false);
        }

        private static void TrackInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame || mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 16f))
            {
                controllerWasLast = false;
            }

            if (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame || gamepad.dpad.ReadValue().sqrMagnitude > 0.25f || gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f))
            {
                controllerWasLast = true;
            }

            if (gamepad == null)
            {
                controllerWasLast = false;
            }
        }

        private void Refresh(bool force)
        {
            bool showController = controllerWasLast && Gamepad.current != null;
            Gamepad gamepad = showController ? Gamepad.current : null;
            if (!force && showController == displayedController && gamepad == displayedGamepad)
            {
                return;
            }

            displayedController = showController;
            displayedGamepad = gamepad;
            foreach (GameObject prompt in keyboardObjects)
            {
                if (prompt != null)
                {
                    prompt.SetActive(!showController);
                }
            }

            foreach (GameObject prompt in controllerObjects)
            {
                if (prompt != null)
                {
                    prompt.SetActive(showController);
                }
            }

            if (!showController)
            {
                SetSkipLabelWidth(false);
                return;
            }

            string device = (gamepad.layout + " " + gamepad.description.manufacturer + " " + gamepad.description.product).ToLowerInvariant();
            bool playStation = device.Contains("sony") || device.Contains("playstation") || device.Contains("dualshock") || device.Contains("dualsense");
            bool xbox = !playStation && (device.Contains("xbox") || device.Contains("xinput"));
            if (southIcon != null)
            {
                southIcon.sprite = playStation ? playStationSouth : xbox ? xboxSouth : genericButton;
            }

            if (eastIcon != null)
            {
                eastIcon.sprite = playStation ? playStationEast : xbox ? xboxEast : genericButton;
            }

            bool unknownLayout = !playStation && !xbox;
            if (southFallbackLabel != null)
            {
                southFallbackLabel.gameObject.SetActive(unknownLayout);
            }

            if (eastFallbackLabel != null)
            {
                eastFallbackLabel.gameObject.SetActive(unknownLayout);
            }

            if (southIcon != null && southIcon.transform.parent is RectTransform southContainer)
            {
                southContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, unknownLayout ? 125f : 44f);
            }

            if (eastIcon != null && eastIcon.transform.parent is RectTransform eastContainer)
            {
                eastContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, unknownLayout ? 125f : 44f);
            }

            SetSkipLabelWidth(unknownLayout);
        }

        private void SetSkipLabelWidth(bool unknownLayout)
        {
            if (skipActionLabel == null)
            {
                return;
            }

            Vector2 position = skipActionLabel.anchoredPosition;
            position.x = unknownLayout ? 145f : 66f;
            skipActionLabel.anchoredPosition = position;
            skipActionLabel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, unknownLayout ? 180f : 220f);
        }
    }
}