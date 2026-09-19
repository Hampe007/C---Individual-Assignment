using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (_panel == null || _playerHealth == null)
        {
            Debug.LogError("GameOverMenu requires a panel and player health.");
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
        _panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}
