using UnityEngine;
using TMPro;

public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string _gameSceneName = "Game";

    private void Awake()
    {
        TMP_Text highScoreText = transform.Find("HighScoreText")?.GetComponent<TMP_Text>();

        if (highScoreText != null)
            highScoreText.text = $"HIGH SCORE: {PlayerProgression.HighScore}";
    }

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
