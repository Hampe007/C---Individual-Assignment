using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider dmgHealthSlider;
    [SerializeField] private TMP_Text healthText;
    [Header("Animation")]
    [SerializeField, Min(1f)] private float dmgBarSpeed = 120f;
    private float targetHealth;
    private void Awake()
    {
        if (playerHealth != null && healthSlider != null && dmgHealthSlider != null)
        {
            return;
        }

        Debug.LogError("PlayerHealthUI is missing required references.");
        enabled = false;
    }

    private void Start()
    {
        if (playerHealth == null || healthSlider == null || dmgHealthSlider == null)
        {
            return;
        }

        UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        dmgHealthSlider.value = playerHealth.CurrentHealth;
    }

    private void Update()
    {
        if (Mathf.Approximately(dmgHealthSlider.value, targetHealth))
        {
            return;
        }

        dmgHealthSlider.value = Mathf.MoveTowards(dmgHealthSlider.value, targetHealth, dmgBarSpeed * Time.deltaTime);
    }

    private void OnEnable()
    {
        if (playerHealth == null || healthSlider == null || dmgHealthSlider == null)
        {
            return;
        }

        playerHealth.HealthChanged += UpdateHealth;
        UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        dmgHealthSlider.value = playerHealth.CurrentHealth;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealth;
        }
    }

    private void UpdateHealth(int currentHealth, int maxHealth)
    {
        healthSlider.maxValue = maxHealth;
        dmgHealthSlider.maxValue = maxHealth;
        // Main health bar changes instantly.
        healthSlider.value = currentHealth;
        // Ease bar catches up afterwards.
        targetHealth = currentHealth;
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}