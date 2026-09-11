using TMPro;
using UnityEngine;

public sealed class PlayerProgressionUI : MonoBehaviour
{
    [SerializeField] private PlayerProgression _progression;

    [Header("XP")]
    [SerializeField] private RectTransform _xpFill;

    [Header("Other")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _scoreText;

    private void Awake()
    {
        if (_progression != null &&
            _xpFill != null &&
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
        float progress = requiredXP > 0 ? (float)currentXP / requiredXP : 0f;
        progress = Mathf.Clamp01(progress);

        float halfWidth = progress * 0.5f;

        _xpFill.anchorMin = new Vector2(0.5f - halfWidth, 0f);
        _xpFill.anchorMax = new Vector2(0.5f + halfWidth, 1f);

        _xpFill.offsetMin = Vector2.zero;
        _xpFill.offsetMax = Vector2.zero;
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