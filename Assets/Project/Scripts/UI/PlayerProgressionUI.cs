using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerProgressionUI : MonoBehaviour
{
    [SerializeField] private PlayerProgression progression;

    [Header("XP")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private RectTransform feedbackTarget;

    [Header("Feedback")]
    [SerializeField, Min(0.01f)] private float animationDuration = 0.22f;
    [SerializeField, Min(0f)] private float gainScale = 0.08f;
    [SerializeField, Min(0f)] private float lossShake = 7f;
    [SerializeField, Min(0f)] private float emptyShake = 15f;
    [SerializeField, Min(0f)] private float lossScale = 0.12f;
    [SerializeField, Min(0f)] private float emptyScale = 0.24f;
    [SerializeField] private Color gainTint = new(1f, 0.9f, 0.45f, 1f);
    [SerializeField] private Color lossTint = new(1f, 0.5f, 0.42f, 1f);
    [SerializeField] private Color emptyTint = new(0.55f, 0.2f, 0.2f, 1f);

    [Header("Other")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text scoreText;

    private RectTransform target;
    private Vector2 basePosition;
    private Vector3 baseScale;
    private Graphic _graphic;
    private Color baseColor;
    private float feedbackTimer;
    private float feedbackDuration;
    private float feedbackShake;
    private float feedbackScale;
    private Color feedbackTint;
    private bool hasFeedback;

    private void Awake()
    {
        if (progression != null &&
            xpSlider != null &&
            levelText != null &&
            scoreText != null)
        {
            target = feedbackTarget != null ? feedbackTarget : xpSlider.transform as RectTransform;
            if (target != null)
            {
                basePosition = target.anchoredPosition;
                baseScale = target.localScale;
                _graphic = target.GetComponent<Graphic>();
                if (_graphic == null)
                    _graphic = target.GetComponentInChildren<Graphic>();
                if (_graphic != null)
                    baseColor = _graphic.color;
            }
            return;
        }

        Debug.LogError("PlayerProgressionUI is missing required references.");
        enabled = false;
    }

    private void OnEnable()
    {
        progression.XPChanged += UpdateXP;
        progression.XPGained += HandleXPGained;
        progression.XPLost += HandleXPLost;
        progression.XPDepleted += HandleXPDepleted;
        progression.ScoreChanged += UpdateScore;
        progression.LeveledUp += UpdateLevel;
    }

    private void Start()
    {
        UpdateXP(progression.CurrentXP, progression.XPRequired);
        UpdateScore(progression.Score);
        UpdateLevel(progression.Level);
    }

    private void OnDisable()
    {
        if (progression == null)
            return;

        progression.XPChanged -= UpdateXP;
        progression.XPGained -= HandleXPGained;
        progression.XPLost -= HandleXPLost;
        progression.XPDepleted -= HandleXPDepleted;
        progression.ScoreChanged -= UpdateScore;
        progression.LeveledUp -= UpdateLevel;
    }

    private void UpdateXP(int currentXP, int requiredXP)
    {
        xpSlider.maxValue = requiredXP;
        xpSlider.value = currentXP;
    }

    private void Update()
    {
        if (!hasFeedback || target == null)
            return;

        feedbackTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(feedbackTimer / feedbackDuration);
        float envelope = 1f - t;
        float oscillation = Mathf.Sin(t * Mathf.PI * 6f) * envelope;
        target.anchoredPosition = basePosition + Vector2.right * (oscillation * feedbackShake);
        float pulse = Mathf.Sin(t * Mathf.PI) * feedbackScale * envelope;
        target.localScale = Vector3.Scale(baseScale, Vector3.one * (1f + pulse));
        if (_graphic != null)
            _graphic.color = Color.Lerp(baseColor, feedbackTint, envelope * 0.75f);

        if (t < 1f)
            return;

        ResetFeedback();
    }

    private void HandleXPGained(int amount, int requiredXP) => PlayFeedback(0f, gainScale, gainTint, animationDuration);
    private void HandleXPLost(int amount, int requiredXP) => PlayFeedback(lossShake, lossScale, lossTint, animationDuration);
    private void HandleXPDepleted(int previousXP, int requiredXP) => PlayFeedback(emptyShake, emptyScale, emptyTint, animationDuration * 1.35f);

    private void PlayFeedback(float shake, float scale, Color tint, float duration)
    {
        if (target == null)
            return;

        feedbackTimer = 0f;
        feedbackDuration = duration;
        feedbackShake = shake;
        feedbackScale = scale;
        feedbackTint = tint;
        hasFeedback = true;
    }

    private void ResetFeedback()
    {
        target.anchoredPosition = basePosition;
        target.localScale = baseScale;
        if (_graphic != null)
            _graphic.color = baseColor;
        hasFeedback = false;
    }

    private void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score}";
    }

    private void UpdateLevel(int level)
    {
        levelText.text = $"Level {level}";
    }
}
