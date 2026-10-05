using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace GameMenus
{
    public sealed class MainMenuDirector : MonoBehaviour
    {
        private const string FirstVisitKey = "LivingMenu.TutorialPromptDismissed";
        private static bool introPlayedThisSession;

        [SerializeField] private MainMenuSettings settings;
        [SerializeField] private MainMenuWorld world;
        [SerializeField] private MainMenuView view;
        [SerializeField] private BloodScreenGraphic bloodScreen;
        [SerializeField] private MainMenuActions actions;
        private Coroutine sequence;
        private MenuOption highlight;
        private MenuOption modalOption;
        private float heldTime;
        private float holdStarted = -1f;
        private float nextIdle;
        private bool requireSkipRelease;
        private bool skipDiscovered;

        public MenuPhase Phase { get; private set; } = MenuPhase.Warning;
        public float SkipProgress => heldTime / Mathf.Max(0.1f, settings.skipHoldDuration);


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            introPlayedThisSession = false;
            SceneTransition.MainMenuOverride = null;
        }

        private void Awake()
        {
            view.gameObject.SetActive(true);
            if (!introPlayedThisSession)
            {
                view.ShowMenu(false);
                world.SetChase(0, 0f);
            }
        }

        private IEnumerator Start()
        {
            while (!PhotosensitivityWarning.Accepted || PhotosensitivityWarning.IsTransitioning)
            {
                yield return null;
            }
            requireSkipRelease = true;
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SceneTransition.MainMenuOverride = gameObject.scene.name;
            view.Initialize(this);
            bloodScreen.SetEffect(0f);
            if (introPlayedThisSession)
            {
                world.PrepareMenu(settings.GetReaction(MenuOption.None));
                EnterMenu();
            }
            else
            {
                sequence = StartCoroutine(Intro());
            }
        }

        private void Update()
        {
            if (PhotosensitivityWarning.IsTransitioning || BrightnessCalibrationScreen.IsOpen)
            {
                return;
            }
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            bool held = keyboard != null && (keyboard.escapeKey.isPressed || keyboard.spaceKey.isPressed);
            held |= mouse != null && mouse.leftButton.isPressed;
            if (requireSkipRelease)
            {
                if (!held)
                {
                    requireSkipRelease = false;
                }
            
                return;
            }
            if (Phase == MenuPhase.Intro)
            {
                if (held || keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) || mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    skipDiscovered = true;
                }
            
                if (!held)
                {
                    holdStarted = -1f;
                }
                else if (holdStarted < 0f)
                {
                    holdStarted = Time.unscaledTime;
                }
            
                heldTime = held ? Time.unscaledTime - holdStarted : 0f;
                view.SetSkip(SkipProgress, skipDiscovered);
                if (heldTime >= settings.skipHoldDuration)
                {
                    SkipIntro();
                }
            
                return;
            }
            if (Phase == MenuPhase.Modal && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseModal();
            }
            if (Phase != MenuPhase.Menu)
            {
                return;
            }
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && ((keyboard != null && (keyboard.downArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)) || (Gamepad.current != null && (Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f))))
            {
                view.FocusMenu();
            }
            bool input = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
            input |= Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f || Mouse.current.leftButton.wasPressedThisFrame);
            input |= Gamepad.current != null && (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f || Gamepad.current.buttonSouth.wasPressedThisFrame);
            if (input)
            {
                ScheduleIdle();
            }
            else if (Time.unscaledTime >= nextIdle)
            {
                world.IdleEvent();
                ScheduleIdle();
            }
        }

        private IEnumerator Intro()
        {
            Phase = MenuPhase.Intro;
            for (int pass = 0; pass < 3; pass++)
            {
                float duration = pass == 0 ? settings.chasePassDuration : pass == 1 ? settings.returnPassDuration : settings.finalPassDuration;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    world.SetChase(pass, elapsed / duration);
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            
                world.SetChase(pass, 1f);
                if (pass < 2 && settings.passPause > 0f)
                {
                    yield return new WaitForSecondsRealtime(settings.passPause);
                }
            }
            world.BeginCatch();
            float catchElapsed = 0f;
            bool lensHit = false;
            float impactStarted = 0f;
            while (catchElapsed < settings.catchDuration)
            {
                world.CatchPlayer(catchElapsed / settings.catchDuration);
                if (world.ImpactStarted)
                {
                    if (!lensHit)
                    {
                        Vector3 origin = Camera.main.WorldToViewportPoint(world.ImpactPosition);
                        bloodScreen.SetImpactOrigin(origin);
                        lensHit = true;
                        impactStarted = catchElapsed;
                    }
                    bloodScreen.SetEffect(Mathf.Lerp(0.24f, 0.9f, Mathf.Clamp01((catchElapsed - impactStarted) / 0.4f)));
                }
                catchElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            world.CatchPlayer(1f);
            yield return RevealMenu(false);
        }

        public void SkipIntro()
        {
            if (Phase != MenuPhase.Intro)
            {
                return;
            }
            if (sequence != null)
            {
                StopCoroutine(sequence);
            }
            requireSkipRelease = true;
            sequence = StartCoroutine(RevealMenu(true));
        }

        private IEnumerator RevealMenu(bool skipped)
        {
            Phase = MenuPhase.Revealing;
            view.SetSkip(0f, false);
            yield return Cover(skipped ? 0.3f : settings.bloodCoverDuration);
            world.PrepareMenu(settings.GetReaction(MenuOption.None));
            introPlayedThisSession = true;
            yield return new WaitForSecondsRealtime(0.15f);
            view.ShowMenu(true);
            yield return Drain(skipped ? 0.7f : settings.bloodDrainDuration);
            EnterMenu();
        }

        private void EnterMenu()
        {
            Phase = MenuPhase.Menu;
            view.ShowMenu(true);
            Highlight(MenuOption.None);
            ScheduleIdle();
            if (!PlayerPrefs.HasKey(FirstVisitKey))
            {
                PlayerPrefs.SetInt(FirstVisitKey, 1);
                PlayerPrefs.Save();
                Phase = MenuPhase.Modal;
                modalOption = MenuOption.Tutorial;
                view.ShowModal("FIRST TIME IN THE NIGHT?", settings.tutorialAvailable
                        ? "A little preparation can keep you alive. Would you like to play the tutorial?"
                        : "The tutorial is still being prepared. You can return to it from the menu when it becomes available.",
                    "Play tutorial", "Go to main menu", false, settings.tutorialAvailable);
            }
        }

        public void Highlight(MenuOption option)
        {
            if (Phase != MenuPhase.Menu)
            {
                return;
            }
            highlight = option;
            view.Highlight(option);
            world.React(settings.GetReaction(option));
            ScheduleIdle();
        }

        public void Activate(MenuOption option)
        {
            if (Phase != MenuPhase.Menu)
            {
                return;
            }
            Highlight(option);
            if (option == MenuOption.Play)
            {
                StartDeparture(settings.gameScene);
                return;
            }
            if (option == MenuOption.Quit)
            {
                modalOption = option;
                HideForAction();
                sequence = StartCoroutine(ApproachQuit());
                return;
            }
            MenuReaction reaction = settings.GetReaction(option);
            if (reaction != null)
            {
                world.SelectCamera(reaction.pressedCameraIndex);
            }
            Phase = MenuPhase.Modal;
            modalOption = option;
            switch (option)
            {
                case MenuOption.Tutorial:
                    view.ShowModal("PREPARE FOR THE NIGHT", settings.tutorialAvailable
                            ? "Learn movement, combat, and the art of surviving the horde."
                            : "The tutorial will be added after the living menu is finished.",
                        settings.tutorialAvailable ? "Play tutorial" : null, "Back");
                    break;
                case MenuOption.Options:
                    view.ShowModal("SOUND OF THE NIGHT", "", null, "Back", true);
                    break;
                case MenuOption.Credits:
                    view.ShowModal("BEHIND THE NIGHT", settings.credits, null, "Back");
                    break;
            }
        }

        public void ConfirmModal()
        {
            if (Phase != MenuPhase.Modal)
            {
                return;
            }
            if (modalOption == MenuOption.Quit)
            {
                view.CloseModal(false);
                HideForAction();
                sequence = StartCoroutine(LeaveQuit());
            }
            else if (modalOption == MenuOption.Tutorial && settings.tutorialAvailable)
            {
                StartDeparture(settings.tutorialScene);
            }
        }

        public void CloseModal()
        {
            if (Phase != MenuPhase.Modal)
            {
                return;
            }
            if (modalOption == MenuOption.Quit)
            {
                view.CloseModal(false);
                HideForAction();
                sequence = StartCoroutine(ReturnFromQuit());
                return;
            }
            AudioPreferences.Save();
            Phase = MenuPhase.Menu;
            view.CloseModal();
            Highlight(highlight);
            ScheduleIdle();
        }

        private void StartDeparture(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Phase = MenuPhase.Modal;
                modalOption = MenuOption.None;
                view.ShowModal("SCENE UNAVAILABLE", $"Add {sceneName} to the active build profile before opening it.", null, "Back");
                return;
            }
            Phase = MenuPhase.Departing;
            view.ShowMenu(false);
            view.SetSkip(0f, false);
            sequence = StartCoroutine(Depart(sceneName));
        }

        private IEnumerator Depart(string sceneName)
        {
            if (sceneName == settings.gameScene)
            {
                yield return actions.Depart();
                if (!actions.ReachedDestination)
                {
                    yield return ReturnFromQuit();
                    yield break;
                }
            }
            AudioPreferences.Save();
            SceneTransition.LoadScene(sceneName);
        }

        private void HideForAction()
        {
            Phase = MenuPhase.Acting;
            view.ShowMenu(false);
            view.SetSkip(0f, false);
        }

        private IEnumerator ApproachQuit()
        {
            yield return actions.ApproachGate();
            if (!actions.ReachedDestination)
            {
                yield return ReturnFromQuit();
                yield break;
            }
            view.SetQuitLayout(true);
            Phase = MenuPhase.Modal;
            view.ShowModal("LEAVE THE NIGHT?", "The horde will still be here when you return.", "Quit game", "Stay");
        }

        private IEnumerator LeaveQuit()
        {
            yield return actions.LeaveGate();
            if (!actions.ReachedDestination)
            {
                yield return ReturnFromQuit();
            }
        }

        private IEnumerator ReturnFromQuit()
        {
            yield return actions.ReturnHome();
            view.SetQuitLayout(false);
            Phase = MenuPhase.Menu;
            view.ShowMenu(true);
            Highlight(MenuOption.None);
        }

        private IEnumerator Cover(float duration)
        {
            bloodScreen.raycastTarget = true;
            float startingCoverage = bloodScreen.Coverage;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                bloodScreen.SetEffect(Mathf.Lerp(startingCoverage, 1f, elapsed / duration));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            bloodScreen.SetEffect(1f);
        }

        private IEnumerator Drain(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                bloodScreen.SetEffect(1f, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            bloodScreen.SetEffect(0f);
            bloodScreen.raycastTarget = false;
        }

        private void ScheduleIdle()
        {
            nextIdle = Time.unscaledTime + Random.Range(settings.idleEventInterval.x, settings.idleEventInterval.y);
        }
    }

    public enum MenuPhase { Intro, Revealing, Menu, Modal, Departing, Acting, Warning }
}
