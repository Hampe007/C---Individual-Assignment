using System;
using UnityEngine;

public sealed class PlayerProgression : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";
    [SerializeField] private HordeManager _hordeManager;

    [Header("Leveling")]
    [SerializeField, Min(1)] private int _startingXPRequired = 10;
    [SerializeField, Min(1f)] private float _xpGrowth = 1.25f;

    [Header("Damage Penalty")]
    [SerializeField, Range(0f, 1f)] private float _damageXPLossPercent = 0.03f;

    private int _level = 1;
    private int _currentXP;
    private int _xpRequired;
    private int _score;
    private int _skillPoints;

    public int Level => _level;
    public int CurrentXP => _currentXP;
    public int XPRequired => _xpRequired;
    public int Score => _score;
    public int SkillPoints => _skillPoints;

    public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);

    public void SaveHighScore()
    {
        if (_score <= HighScore)
            return;

        PlayerPrefs.SetInt(HighScoreKey, _score);
        PlayerPrefs.Save();
    }

    public event Action<int, int> XPChanged;
    public event Action<int, int> XPGained;
    public event Action<int, int> XPLost;
    public event Action<int, int> XPDepleted;
    public event Action<int> ScoreChanged;
    public event Action<int> LeveledUp;

    private void Awake()
    {
        if (_hordeManager == null)
        {
            Debug.LogError("PlayerProgression is missing HordeManager.");
            enabled = false;
            return;
        }

        _xpRequired = _startingXPRequired;
    }

    private void OnEnable()
    {
        _hordeManager.EnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        if (_hordeManager != null)
            _hordeManager.EnemyDied -= HandleEnemyDied;
    }

    public void SpendSkillPoint()
    {
        if (_skillPoints <= 0)
            return;

        _skillPoints--;
    }

    private void HandleEnemyDied(int xpReward, int scoreReward)
    {
        AddScore(scoreReward);
        AddXP(xpReward);
    }

    public void LoseXPFromDamage()
    {
        if (_currentXP <= 0 || _xpRequired <= 0 || _damageXPLossPercent <= 0f)
            return;

        int oldXP = _currentXP;
        int loss = Mathf.Max(1, Mathf.RoundToInt(_xpRequired * _damageXPLossPercent));
        _currentXP = Mathf.Max(0, _currentXP - loss);

        if (_currentXP == oldXP)
            return;

        XPChanged?.Invoke(_currentXP, _xpRequired);

        if (_currentXP == 0)
            XPDepleted?.Invoke(oldXP, _xpRequired);
        else
            XPLost?.Invoke(oldXP - _currentXP, _xpRequired);
    }

    private void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        int startingXP = _currentXP;
        _currentXP += amount;

        while (_currentXP >= _xpRequired)
        {
            _currentXP -= _xpRequired;

            _level++;
            _skillPoints++;

            _xpRequired = Mathf.CeilToInt(_xpRequired * _xpGrowth);

            LeveledUp?.Invoke(_level);
        }

        XPChanged?.Invoke(_currentXP, _xpRequired);
        XPGained?.Invoke(Mathf.Max(0, _currentXP - startingXP), _xpRequired);
    }

    private void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        _score += amount;
        ScoreChanged?.Invoke(_score);
    }
}
