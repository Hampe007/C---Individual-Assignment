using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private GameOverMenu _gameOverMenu;

    private PlayerHealth _playerHealth;

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();

        if (_gameOverMenu == null)
            _gameOverMenu = FindFirstObjectByType<GameOverMenu>();

        if (_gameOverMenu != null)
            return;

        Debug.LogError("PlayerDeathHandler could not find GameOverMenu.");
        enabled = false;
    }

    private void OnEnable()
    {
        _playerHealth.Died += HandleDeath;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.Died -= HandleDeath;
    }

    private void HandleDeath()
    {
        _gameOverMenu.Show();
    }
}