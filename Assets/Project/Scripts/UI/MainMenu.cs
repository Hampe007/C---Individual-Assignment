using UnityEngine;

public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string _gameSceneName = "Game";

    public void Play()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene(_gameSceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
