using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string _gameSceneName = "Game";

    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_gameSceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}