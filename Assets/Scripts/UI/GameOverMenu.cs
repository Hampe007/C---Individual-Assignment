using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerProgression progression;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    private void Awake()
    {
        if (panel == null || playerHealth == null || progression == null || scoreText == null || bestText == null || levelText == null)
        {
            Debug.LogError("GameOverMenu is missing required scene references.");
            enabled = false;
            return;
        }

        panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += Show;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= Show;
        }
    }

    public void Show()
    {
        scoreText.text = progression.Score.ToString();
        bestText.text = Mathf.Max(PlayerProgression.HighScore, progression.Score).ToString();
        levelText.text = progression.Level.ToString();
        panel.SetActive(true);
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
        SceneTransition.LoadScene(mainMenuSceneName);
    }
}