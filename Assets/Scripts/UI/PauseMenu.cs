using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _menuPage;
    [SerializeField] private GameObject _optionsPage;
    [SerializeField] private GameObject _confirmationPage;
    [SerializeField] private TMP_Text _confirmationTitle;
    [SerializeField] private TMP_Text _confirmationBody;
    [SerializeField] private UnityEngine.UI.Button _resumeButton;
    [SerializeField] private UnityEngine.UI.Button _optionsBackButton;
    [SerializeField] private UnityEngine.UI.Button _cancelButton;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private GameObject _skillTreePanel;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private static PauseMenu _instance;
    private bool _ownsPause;
    private bool _waitForPointerRelease;
    private int _resumeFrame = -1;
    private float _previousTimeScale;
    private string _destination;
    private GameObject _returnSelection;

    public bool IsOpen => _ownsPause;
    public static bool BlocksGameplayInput => SceneTransition.IsTransitioning ||
        (_instance != null && (_instance._ownsPause ||
        _instance._waitForPointerRelease || Time.frameCount <= _instance._resumeFrame));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake()
    {
        if (_panel == null || _menuPage == null || _optionsPage == null || _confirmationPage == null ||
            _confirmationTitle == null || _confirmationBody == null || _resumeButton == null ||
            _optionsBackButton == null || _cancelButton == null ||
            _playerHealth == null || _skillTreePanel == null || _gameOverPanel == null)
        {
            Debug.LogError("PauseMenu is missing required references.", this);
            enabled = false;
            return;
        }

        _instance = this;
        _panel.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePause();
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (_waitForPointerRelease && (Mouse.current == null ||
            (!Mouse.current.leftButton.isPressed && !Mouse.current.rightButton.isPressed)))
            _waitForPointerRelease = false;

        if (SceneTransition.IsTransitioning || Keyboard.current == null ||
            !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (!_ownsPause)
            Show();
        else if (_optionsPage.activeSelf || _confirmationPage.activeSelf)
            Back();
        else
            Resume();
    }

    private void LateUpdate()
    {
        // Another menu may claim the pause later in the frame (for example, a level-up).
        if (_ownsPause && (OtherMenuOpen || _playerHealth.CurrentHealth <= 0))
            HideWithoutResuming();
    }

    private bool OtherMenuOpen => (_skillTreePanel != null && _skillTreePanel.activeInHierarchy) ||
        (_gameOverPanel != null && _gameOverPanel.activeInHierarchy);

    public void Show()
    {
        if (!isActiveAndEnabled || _ownsPause || SceneTransition.IsTransitioning ||
            Time.timeScale <= 0f || OtherMenuOpen || _playerHealth.CurrentHealth <= 0)
            return;

        _previousTimeScale = Time.timeScale;
        _ownsPause = true;
        Time.timeScale = 0f;
        _panel.SetActive(true);
        SetPage(_menuPage, _resumeButton.gameObject);
    }

    public void Resume()
    {
        if (!CanInteract)
            return;
        ReleasePause();
        // A held UI click must be released before it can set a movement destination.
        _resumeFrame = Time.frameCount;
        _waitForPointerRelease = Mouse.current != null &&
            (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed);
    }

    public void Options()
    {
        if (!CanInteract)
            return;
        RememberSelection();
        SetPage(_optionsPage, _optionsBackButton.gameObject);
    }

    public void Back()
    {
        if (CanInteract)
        {
            _destination = null;
            SetPage(_menuPage, _returnSelection != null ? _returnSelection : _resumeButton.gameObject);
        }
    }

    public void Restart() => AskToLeave(SceneManager.GetActiveScene().name, "RESTART RUN?", "Your current run will be lost. Start again?");

    public void MainMenu() => AskToLeave(_mainMenuSceneName, "LEAVE THIS RUN?", "Your current run will be lost. Return to the main menu?");

    public void Cancel() => Back();

    public void Confirm()
    {
        if (!CanInteract || !_confirmationPage.activeSelf || string.IsNullOrEmpty(_destination))
            return;

        string scene = _destination;
        ReleasePause();
        // Scene loads must not inherit a paused time scale.
        Time.timeScale = 1f;
        SceneTransition.LoadScene(scene);
    }

    private bool CanInteract => _ownsPause && !SceneTransition.IsTransitioning &&
        !OtherMenuOpen && _playerHealth.CurrentHealth > 0;

    private void AskToLeave(string scene, string title, string body)
    {
        if (!CanInteract)
            return;
        _destination = scene;
        RememberSelection();
        _confirmationTitle.text = title;
        _confirmationBody.text = body;
        SetPage(_confirmationPage, _cancelButton.gameObject);
    }

    private void RememberSelection() => _returnSelection = EventSystem.current != null
        ? EventSystem.current.currentSelectedGameObject : null;

    private void SetPage(GameObject page, GameObject selection)
    {
        Select(null);
        _menuPage.SetActive(page == _menuPage);
        _optionsPage.SetActive(page == _optionsPage);
        _confirmationPage.SetActive(page == _confirmationPage);
        Select(selection);
    }

    private void ReleasePause()
    {
        if (!_ownsPause)
            return;
        if (!OtherMenuOpen && _playerHealth != null && _playerHealth.CurrentHealth > 0 && Time.timeScale == 0f)
            Time.timeScale = _previousTimeScale;
        HideWithoutResuming();
    }

    private void HideWithoutResuming()
    {
        _ownsPause = false;
        _destination = null;
        // Preserve focus if the skill-tree menu has already selected its first node.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(_panel.transform))
            Select(null);
        if (_panel != null)
            _panel.SetActive(false);
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current == null)
            return;
        EventSystem.current.SetSelectedGameObject(null);
        if (target != null)
            EventSystem.current.SetSelectedGameObject(target);
    }
}
