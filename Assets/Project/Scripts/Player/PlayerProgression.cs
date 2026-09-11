using System;
using UnityEngine;

public sealed class PlayerProgression : MonoBehaviour
{
    [SerializeField] private HordeManager _hordeManager;

    [Header("Leveling")]
    [SerializeField, Min(1)] private int _startingXPRequired = 10;
    [SerializeField, Min(1f)] private float _xpGrowth = 1.25f;

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

    public event Action<int, int> XPChanged;
    public event Action<int> ScoreChanged;
    public event Action<int> LeveledUp;
    public event Action<int> SkillPointsChanged;

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
        SkillPointsChanged?.Invoke(_skillPoints);
    }

    private void HandleEnemyDied(EnemyDeathData deathData)
    {
        AddScore(deathData.ScoreReward);
        AddXP(deathData.XPReward);
    }

    private void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        _currentXP += amount;

        while (_currentXP >= _xpRequired)
        {
            _currentXP -= _xpRequired;

            _level++;
            _skillPoints++;

            _xpRequired = Mathf.CeilToInt(_xpRequired * _xpGrowth);

            SkillPointsChanged?.Invoke(_skillPoints);
            LeveledUp?.Invoke(_level);
        }

        XPChanged?.Invoke(_currentXP, _xpRequired);
    }

    private void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        _score += amount;
        ScoreChanged?.Invoke(_score);
    }
}