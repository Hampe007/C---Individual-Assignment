using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject menuPage;
    [SerializeField] private GameObject optionsPage;
    [SerializeField] private GameObject confirmationPage;
    [SerializeField] private TMP_Text confirmationTitle;
    [SerializeField] private TMP_Text confirmationBody;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button optionsBackButton;
    [SerializeField] private UnityEngine.UI.Button cancelButton;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject skillTreePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    private static PauseMenu instance;
    private bool ownsPause;
    private bool waitForPointerRelease;
    private int resumeFrame = -1;
    private float previousTimeScale;
    private string destination;
    private GameObject returnSelection;
    public bool IsOpen => ownsPause;
    public static bool BlocksGameplayInput => SceneTransition.IsTransitioning || (instance != null && (instance.ownsPause || instance.waitForPointerRelease || Time.frameCount <= instance.resumeFrame));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
    private void Awake()
    {
        if (panel == null || menuPage == null || optionsPage == null || confirmationPage == null || confirmationTitle == null || confirmationBody == null || resumeButton == null || optionsBackButton == null || cancelButton == null || playerHealth == null || skillTreePanel == null || gameOverPanel == null)
        {
            Debug.LogError("PauseMenu is missing required references.", this);
            enabled = false;
            return;
        }

        instance = this;
        panel.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePause();
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (waitForPointerRelease && (Mouse.current == null || (!Mouse.current.leftButton.isPressed && !Mouse.current.rightButton.isPressed)))
        {
            waitForPointerRelease = false;
        }

        if (SceneTransition.IsTransitioning)
        {
            return;
        }

        bool pausePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        bool backPressed = ownsPause && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        if (!pausePressed && !backPressed)
        {
            return;
        }

        if (!ownsPause)
        {
            Show();
        }
        else if (optionsPage.activeSelf || confirmationPage.activeSelf)
        {
            Back();
        }
        else
        {
            Resume();
        }
    }

    private void LateUpdate()
    {
        // Another menu may claim the pause later in the frame (for example, a level-up).
        if (ownsPause && (OtherMenuOpen || playerHealth.CurrentHealth <= 0))
        {
            HideWithoutResuming();
        }
    }

    private bool OtherMenuOpen => (skillTreePanel != null && skillTreePanel.activeInHierarchy) || (gameOverPanel != null && gameOverPanel.activeInHierarchy);

    public void Show()
    {
        if (!isActiveAndEnabled || ownsPause || SceneTransition.IsTransitioning || Time.timeScale <= 0f || OtherMenuOpen || playerHealth.CurrentHealth <= 0)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0f;
        panel.SetActive(true);
        SetPage(menuPage, resumeButton.gameObject);
    }

    public void Resume()
    {
        if (!CanInteract)
        {
            return;
        }

        ReleasePause();
        // A held UI click must be released before it can set a movement destination.
        resumeFrame = Time.frameCount;
        waitForPointerRelease = Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed);
    }

    public void Options()
    {
        if (!CanInteract)
        {
            return;
        }

        RememberSelection();
        SetPage(optionsPage, optionsBackButton.gameObject);
    }

    public void Back()
    {
        if (CanInteract)
        {
            destination = null;
            SetPage(menuPage, returnSelection != null ? returnSelection : resumeButton.gameObject);
        }
    }

    public void Restart() => AskToLeave(SceneManager.GetActiveScene().name, "RESTART RUN?", "Your current run will be lost. Start again?");
    public void MainMenu() => AskToLeave(mainMenuSceneName, "LEAVE THIS RUN?", "Your current run will be lost. Return to the main menu?");
    public void Cancel() => Back();
    public void Confirm()
    {
        if (!CanInteract || !confirmationPage.activeSelf || string.IsNullOrEmpty(destination))
        {
            return;
        }

        string scene = destination;
        ReleasePause();
        // Scene loads must not inherit a paused time scale.
        Time.timeScale = 1f;
        SceneTransition.LoadScene(scene);
    }

    private bool CanInteract => ownsPause && !SceneTransition.IsTransitioning && !OtherMenuOpen && playerHealth.CurrentHealth > 0;

    private void AskToLeave(string scene, string title, string body)
    {
        if (!CanInteract)
        {
            return;
        }

        destination = scene;
        RememberSelection();
        confirmationTitle.text = title;
        confirmationBody.text = body;
        SetPage(confirmationPage, cancelButton.gameObject);
    }

    private void RememberSelection() => returnSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
    private void SetPage(GameObject page, GameObject selection)
    {
        Select(null);
        menuPage.SetActive(page == menuPage);
        optionsPage.SetActive(page == optionsPage);
        confirmationPage.SetActive(page == confirmationPage);
        Select(selection);
    }

    private void ReleasePause()
    {
        if (!ownsPause)
        {
            return;
        }

        if (!OtherMenuOpen && playerHealth != null && playerHealth.CurrentHealth > 0 && Time.timeScale == 0f)
        {
            Time.timeScale = previousTimeScale;
        }

        HideWithoutResuming();
    }

    private void HideWithoutResuming()
    {
        ownsPause = false;
        destination = null;
        // Preserve focus if the skill-tree menu has already selected its first node.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel.transform))
        {
            Select(null);
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}