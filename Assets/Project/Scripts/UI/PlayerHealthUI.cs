using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private TMP_Text _healthText;

    private void Awake()
    {
        if (_playerHealth != null && _healthSlider != null && _healthText != null)
            return;

        Debug.LogError("PlayerHealthUI is missing required references.");
        enabled = false;
    }

    private void Start()
    {
        UpdateHealth(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
    }

    private void OnEnable()
    {
        _playerHealth.HealthChanged += UpdateHealth;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.HealthChanged -= UpdateHealth;
    }

    private void UpdateHealth(int currentHealth, int maxHealth)
    {
        _healthSlider.maxValue = maxHealth;
        _healthSlider.value = currentHealth;
        _healthText.text = $"{currentHealth} / {maxHealth}";
    }
    
}