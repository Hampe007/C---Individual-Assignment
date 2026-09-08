using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (_panel == null)
        {
            Debug.LogError("GameOverMenu requires a panel.");
            enabled = false;
            return;
        }

        _panel.SetActive(false);
    }

    public void Show()
    {
        _panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Debug.Log("Restart");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("MainMenu");
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}