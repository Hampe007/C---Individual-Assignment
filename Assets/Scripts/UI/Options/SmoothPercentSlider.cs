using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BrightnessCalibration
{
    public sealed class SmoothPercentSlider : Slider
    {
        private const float HoldDelay = 0.4f;
        private const float RampDuration = 0.7f;
        private const float StartSpeed = 8f;
        private const float MaximumSpeed = 60f;

        private int heldDirection;
        private float heldSince;

        public override void OnMove(AxisEventData eventData)
        {
            int direction = eventData.moveDir switch
            {
                MoveDirection.Left => -1,
                MoveDirection.Right => 1,
                _ => 0
            };
            if (direction == 0 || !IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            if (heldDirection == direction)
                return;

            heldDirection = direction;
            heldSince = Time.unscaledTime;
            value = Mathf.Clamp(Mathf.Round(value) + direction, minValue, maxValue);
            eventData.Use();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            heldDirection = 0;
            base.OnDeselect(eventData);
        }

        protected override void Update()
        {
            base.Update();
            if (!Application.isFocused || !IsActive() || !IsInteractable() || EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject)
            {
                heldDirection = 0;
                return;
            }

            int direction = ReadDirection();
            if (direction != heldDirection)
            {
                if (direction == 0)
                    heldDirection = 0;
                return;
            }

            float heldFor = Time.unscaledTime - heldSince;
            if (direction == 0 || heldFor < HoldDelay)
                return;

            float ramp = Mathf.Clamp01((heldFor - HoldDelay) / RampDuration);
            float speed = Mathf.Lerp(StartSpeed, MaximumSpeed, ramp * ramp * (3f - 2f * ramp));
            value = Mathf.Clamp(value + direction * speed * Mathf.Min(Time.unscaledDeltaTime, 0.05f), minValue, maxValue);
        }

        private static int ReadDirection()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool left = keyboard != null && keyboard.leftArrowKey.isPressed;
            bool right = keyboard != null && keyboard.rightArrowKey.isPressed;
            if (gamepad != null)
            {
                left |= gamepad.dpad.left.isPressed || gamepad.leftStick.ReadValue().x < -0.5f;
                right |= gamepad.dpad.right.isPressed || gamepad.leftStick.ReadValue().x > 0.5f;
            }
            return left == right ? 0 : right ? 1 : -1;
        }
    }
}
