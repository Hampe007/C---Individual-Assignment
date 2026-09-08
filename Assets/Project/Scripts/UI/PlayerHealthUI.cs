using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    
    [Header("Health Bar")]
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _dmgHealthSlider;
    [SerializeField] private TMP_Text _healthText;

    [Header("Animation")]
    [SerializeField, Min(1f)] private float _dmgBarSpeed = 120f;

    private float _targetHealth;
    
    private void Awake()
    {
        if (_playerHealth != null && _healthSlider != null && _dmgHealthSlider != null && _healthText != null)
            return;

        Debug.LogError("PlayerHealthUI is missing required references.");
        enabled = false;
    }

    private void Start()
    {
        UpdateHealth(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
        
        // Make sure both bars start full.
        _dmgHealthSlider.value = _playerHealth.CurrentHealth;
    }

    private void Update()
    {
        if (Mathf.Approximately(_dmgHealthSlider.value, _targetHealth))
            return;

        _dmgHealthSlider.value = Mathf.MoveTowards(_dmgHealthSlider.value, _targetHealth, _dmgBarSpeed * Time.deltaTime);
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
        _dmgHealthSlider.maxValue = maxHealth;
        
        // Main health bar changes instantly.
        _healthSlider.value = currentHealth;
        
        // Ease bar catches up afterwards.
        _targetHealth = currentHealth;
        
        _healthText.text = $"{currentHealth} / {maxHealth}";
    }
}