using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SkillTreeMenu : MonoBehaviour
{
    [SerializeField] private PlayerProgression _playerProgression;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private SkillTreeProgression _skillTreeProgression;

    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _skillPointsText;
    [SerializeField] private SkillTreeNodeButton[] _nodeButtons;

    private void Awake()
    {
        if (_playerProgression == null ||
            _playerHealth == null ||
            _skillTreeProgression == null ||
            _panel == null ||
            _skillPointsText == null ||
            _nodeButtons == null ||
            _nodeButtons.Length == 0)
        {
            Debug.LogError("SkillTreeMenu is missing required references.");
            enabled = false;
            return;
        }

        for (int i = 0; i < _nodeButtons.Length; i++)
        {
            if (_nodeButtons[i] != null)
                continue;

            Debug.LogError($"Skill tree button at index {i} is missing.");
            enabled = false;
            return;
        }

        _panel.SetActive(false);
    }

    private void Start()
    {
        for (int i = 0; i < _nodeButtons.Length; i++)
            _nodeButtons[i].Setup(_skillTreeProgression, SelectNode);
    }

    private void OnEnable()
    {
        _playerProgression.LeveledUp += Show;
        _playerHealth.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (_playerProgression != null)
            _playerProgression.LeveledUp -= Show;

        if (_playerHealth != null)
            _playerHealth.Died -= HandlePlayerDied;
    }

    private void Show(int level)
    {
        if (_playerHealth.CurrentHealth <= 0)
            return;

        if (_playerProgression.SkillPoints <= 0)
            return;

        if (!_skillTreeProgression.HasAvailableNode())
            return;

        _panel.SetActive(true);
        Time.timeScale = 0f;

        Refresh();
        SelectFirstAvailableNode();
    }

    private void SelectNode(string nodeId)
    {
        if (_playerHealth.CurrentHealth <= 0)
            return;

        if (!_skillTreeProgression.TryPurchase(nodeId))
            return;

        Refresh();

        if (_playerProgression.SkillPoints <= 0)
        {
            Close();
            return;
        }

        if (!_skillTreeProgression.HasAvailableNode())
        {
            Close();
            return;
        }

        SelectFirstAvailableNode();
    }

    private void Refresh()
    {
        _skillPointsText.text = $"Skill Points: {_playerProgression.SkillPoints}";

        for (int i = 0; i < _nodeButtons.Length; i++)
            _nodeButtons[i].Refresh();
    }

    private void SelectFirstAvailableNode()
    {
        if (EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);

        for (int i = 0; i < _nodeButtons.Length; i++)
        {
            if (!_nodeButtons[i].IsInteractable)
                continue;

            EventSystem.current.SetSelectedGameObject(_nodeButtons[i].ButtonObject);
            return;
        }
    }

    private void HandlePlayerDied()
    {
        _panel.SetActive(false);
    }

    private void Close()
    {
        _panel.SetActive(false);

        if (_playerHealth.CurrentHealth > 0)
            Time.timeScale = 1f;
    }
}