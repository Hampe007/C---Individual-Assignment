using TMPro;
using UnityEngine;

public sealed class PlayerProgressionUI : MonoBehaviour
{
    [SerializeField] private PlayerProgression _progression;

    [Header("XP")]
    [SerializeField] private UnityEngine.UI.Slider _xpSlider;

    [Header("Other")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _scoreText;

    private void Awake()
    {
        if (_progression != null &&
            _xpSlider != null &&
            _levelText != null &&
            _scoreText != null)
            return;

        Debug.LogError("PlayerProgressionUI is missing required references.");
        enabled = false;
    }

    private void OnEnable()
    {
        _progression.XPChanged += UpdateXP;
        _progression.ScoreChanged += UpdateScore;
        _progression.LeveledUp += UpdateLevel;
    }

    private void Start()
    {
        UpdateXP(_progression.CurrentXP, _progression.XPRequired);
        UpdateScore(_progression.Score);
        UpdateLevel(_progression.Level);
    }

    private void OnDisable()
    {
        if (_progression == null)
            return;

        _progression.XPChanged -= UpdateXP;
        _progression.ScoreChanged -= UpdateScore;
        _progression.LeveledUp -= UpdateLevel;
    }

    private void UpdateXP(int currentXP, int requiredXP)
    {
        _xpSlider.maxValue = requiredXP;
        _xpSlider.value = currentXP;
    }

    private void UpdateScore(int score)
    {
        _scoreText.text = $"Score: {score}";
    }

    private void UpdateLevel(int level)
    {
        _levelText.text = $"Level {level}";
    }
}
