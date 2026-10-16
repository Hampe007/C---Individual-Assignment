using System;
using UnityEngine;

public sealed class PlayerProgression : MonoBehaviour
{
    [SerializeField] private HordeManager hordeManager;
    [Header("Survival Score")]
    [SerializeField, Min(0)] private int scorePerSecond = 1;
    [Header("Leveling")]
    [SerializeField, Min(1)] private int startingXPRequired = 10;
    [SerializeField, Min(1f)] private float xpGrowth = 1.25f;
    [Header("Damage Penalty")]
    [SerializeField, Range(0f, 1f)] private float damageXPLossPercent = 0.015f;
    private int level;
    private int currentXP;
    private int xpRequired;
    private int score;
    private int skillPoints;
    private float survivalSeconds;
    private PlayerHealth playerHealth;
    public int Level => level;
    public int CurrentXP => currentXP;
    public int XPRequired => xpRequired;
    public int Score => score;
    public int SkillPoints => skillPoints;
    public static int HighScore => GameProgress.HighScore;

    public void SaveHighScore()
    {
        GameProgress.SaveHighScore(score);
    }

    public event Action<int, int> XPChanged;
    public event Action<int, int> XPGained;
    public event Action<int, int> XPLost;
    public event Action<int, int> XPDepleted;
    public event Action<int> ScoreChanged;
    public event Action<int> LeveledUp;
    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (hordeManager == null)
        {
            Debug.LogError("PlayerProgression is missing HordeManager.");
            enabled = false;
            return;
        }

        xpRequired = startingXPRequired;
    }

    private void OnEnable()
    {
        hordeManager.EnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        if (hordeManager != null)
        {
            hordeManager.EnemyDied -= HandleEnemyDied;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || !hordeManager.IsReady || scorePerSecond <= 0 || (playerHealth != null && playerHealth.CurrentHealth <= 0))
        {
            return;
        }

        survivalSeconds += Time.deltaTime;
        int wholeSeconds = Mathf.FloorToInt(survivalSeconds);
        if (wholeSeconds <= 0)
        {
            return;
        }

        // Keep fractional seconds so scoring stays consistent across frame rates.
        survivalSeconds -= wholeSeconds;
        AddScore(wholeSeconds * scorePerSecond);
    }

    public void SpendSkillPoint()
    {
        if (skillPoints <= 0)
        {
            return;
        }

        skillPoints--;
    }

    private void HandleEnemyDied(int xpReward, int scoreReward)
    {
        AddScore(scoreReward);
        AddXP(xpReward);
    }

    public void LoseXPFromDamage()
    {
        if (currentXP <= 0 || xpRequired <= 0 || damageXPLossPercent <= 0f)
        {
            return;
        }

        int oldXP = currentXP;
        int loss = Mathf.Max(1, Mathf.RoundToInt(xpRequired * damageXPLossPercent));
        currentXP = Mathf.Max(0, currentXP - loss);
        if (currentXP == oldXP)
        {
            return;
        }

        XPChanged?.Invoke(currentXP, xpRequired);
        if (currentXP == 0)
        {
            XPDepleted?.Invoke(oldXP, xpRequired);
        }
        else
        {
            XPLost?.Invoke(oldXP - currentXP, xpRequired);
        }
    }

    private void AddXP(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int startingXP = currentXP;
        currentXP += amount;
        while (currentXP >= xpRequired)
        {
            currentXP -= xpRequired;
            level++;
            skillPoints++;
            xpRequired = Mathf.CeilToInt(xpRequired * xpGrowth);
            LeveledUp?.Invoke(level);
        }

        XPChanged?.Invoke(currentXP, xpRequired);
        XPGained?.Invoke(Mathf.Max(0, currentXP - startingXP), xpRequired);
    }

    private void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        score += amount;
        ScoreChanged?.Invoke(score);
    }
}