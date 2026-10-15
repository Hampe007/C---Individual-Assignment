using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerProgression _progression;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _bestText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (_panel == null || _playerHealth == null || _progression == null ||
            _scoreText == null || _bestText == null || _levelText == null)
        {
            Debug.LogError("GameOverMenu is missing required scene references.");
            enabled = false;
            return;
        }

        _panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
            _playerHealth.Died += Show;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.Died -= Show;
    }

    public void Show()
    {
        _scoreText.text = _progression.Score.ToString();
        _bestText.text = Mathf.Max(PlayerProgression.HighScore, _progression.Score).ToString();
        _levelText.text = _progression.Level.ToString();
        _panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene(_mainMenuSceneName);
    }
}
