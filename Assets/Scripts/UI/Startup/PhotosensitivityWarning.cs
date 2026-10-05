using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameMenus
{
    [DefaultExecutionOrder(32000)]
    public sealed class PhotosensitivityWarning : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button continueButton;
        [SerializeField] private UnityEngine.UI.Button exitButton;
        [SerializeField] private CanvasGroup screen;
        [SerializeField] private CanvasGroup content;
        [SerializeField, Min(0.05f)] private float warningFadeInDuration = 0.8f;
        [SerializeField, Min(0.05f)] private float fadeOutDuration = 0.45f;
        [SerializeField, Min(0.05f)] private float fadeInDuration = 1.1f;
        private float previousTimeScale;
        private bool previousAudioPause;
        private float resumeVolume;
        private bool fadingAudio;
        private bool paused;
        private bool ready;

        public static bool Accepted { get; private set; }
        public static bool IsTransitioning { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Accepted = false;
            IsTransitioning = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ShowAtStartup()
        {
            GameObject prefab = Resources.Load<GameObject>("PhotosensitivityWarning");
            if (prefab == null)
            {
                Debug.LogError("The startup photosensitivity warning prefab is missing.");
                return;
            }
            DontDestroyOnLoad(Instantiate(prefab));
        }

        private void Awake()
        {
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            paused = true;
            continueButton.interactable = false;
            exitButton.interactable = false;
            content.alpha = 0f;
            PausePresentation();
        }

        private IEnumerator Start()
        {
            yield return null;
            while (!UnityEngine.Rendering.SplashScreen.isFinished)
            {
                yield return null;
            }
            yield return Fade(content, warningFadeInDuration, 1f);
            while (SubmitHeld())
            {
                yield return null;
            }
            ready = true;
            continueButton.interactable = true;
            exitButton.interactable = true;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void LateUpdate()
        {
            if (paused)
            {
                PausePresentation();
            }
            if (!ready || !Application.isFocused)
            {
                return;
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                ExitGame();
            }
            else if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                Continue();
            }
        }

        private static bool SubmitHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.isPressed
                || Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.buttonEast.isPressed)
                || Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        private static void PausePresentation()
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void Continue()
        {
            if (!ready || Accepted)
            {
                return;
            }
            ready = false;
            IsTransitioning = true;
            content.interactable = false;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            StartCoroutine(Transition());
        }

        private IEnumerator Transition()
        {
            yield return Fade(content, fadeOutDuration);
            yield return new WaitForSecondsRealtime(0.1f);
            if (!BrightnessPreferences.HasCalibrated)
            {
                yield return BrightnessCalibrationScreen.ShowAtStartup();
            }
            resumeVolume = AudioListener.volume;
            fadingAudio = true;
            AudioListener.volume = 0f;
            Accepted = true;
            RestorePresentation();
            yield return null;
            yield return Fade(screen, fadeInDuration);
            Destroy(gameObject);
        }

        private IEnumerator Fade(CanvasGroup group, float duration, float target = 0f)
        {
            float elapsed = 0f;
            float startingAlpha = group.alpha;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                group.alpha = Mathf.SmoothStep(startingAlpha, target, elapsed / Mathf.Max(0.05f, duration));
                if (fadingAudio)
                {
                    AudioListener.volume = resumeVolume * (1f - group.alpha);
                }
            
                yield return null;
            }
            group.alpha = target;
        }

        public void ExitGame()
        {
            if (!ready || Accepted)
            {
                return;
            }
            ready = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            IsTransitioning = false;
            RestorePresentation();
            if (fadingAudio)
            {
                AudioListener.volume = resumeVolume;
            }
        }

        private void RestorePresentation()
        {
            if (!paused)
            {
                return;
            }
            paused = false;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
        }
    }
}
