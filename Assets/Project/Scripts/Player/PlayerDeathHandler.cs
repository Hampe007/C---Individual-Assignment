using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;

    private void Awake()
    {
        if (_playerHealth != null)
            return;

        Debug.LogError("PlayerDeathHandler requires PlayerHealth.");
        enabled = false;
    }

    private void OnEnable()
    {
        _playerHealth.Died += RestartGame;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.Died -= RestartGame;
    }

    private void RestartGame()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
}