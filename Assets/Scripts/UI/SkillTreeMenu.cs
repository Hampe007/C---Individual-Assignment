using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SkillTreeMenu : MonoBehaviour
{
    [SerializeField] private PlayerProgression _playerProgression;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private SkillTreeProgression _skillTreeProgression;

    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _skillPointsText;
    [SerializeField] private SkillTreeNodeButton[] _nodeButtons;
    [SerializeField] private RectTransform _nodesContainer;
    [SerializeField] private RectTransform _detailsPanel;
    [SerializeField] private TMP_Text _detailName;
    [SerializeField] private TMP_Text _detailLevel;
    [SerializeField] private TMP_Text _detailDescription;
    [SerializeField] private TMP_Text _detailHint;
    [SerializeField] private TMP_Text _detailRequirements;
    [SerializeField] private TMP_Text _actionText;
    [SerializeField] private UnityEngine.UI.Button _actionButton;
    [SerializeField] private UnityEngine.UI.Button _continueButton;
    private SkillTreeNodeButton _lastFocusedNode;
    private string _selectedNodeId;

    private void Awake()
    {
        if (_playerProgression == null ||
            _playerHealth == null ||
            _skillTreeProgression == null ||
            _panel == null ||
            _skillPointsText == null ||
            _nodesContainer == null || _detailsPanel == null ||
            _detailName == null || _detailLevel == null ||
            _detailDescription == null || _detailHint == null || _detailRequirements == null ||
            _actionText == null || _actionButton == null || _continueButton == null ||
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

        _actionButton.onClick.AddListener(PurchaseSelected);
        _continueButton.onClick.AddListener(Close);
        if (!_actionButton.TryGetComponent<SkillTreeActionButton>(out _))
        {
            _actionButton.gameObject.AddComponent<SkillTreeActionButton>();
        }
        if (!_continueButton.TryGetComponent<SkillTreeActionButton>(out _))
        {
            _continueButton.gameObject.AddComponent<SkillTreeActionButton>();
        }
        _panel.SetActive(false);
    }

    private void Update()
    {
        bool toggle = (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) ||
                      (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
        if (!_panel.activeSelf)
        {
            if (toggle && Time.timeScale > 0f && !PauseMenu.BlocksGameplayInput)
                Show(_playerProgression.Level);
            return;
        }

        if (toggle || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
        {
            Close();
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        bool keyboardNavigation = Keyboard.current != null &&
                                  (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame ||
                                   Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame);
        bool controllerNavigation = Gamepad.current != null &&
                                    (Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.25f ||
                                     Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.25f);
        if (eventSystem.currentSelectedGameObject == null && (keyboardNavigation || controllerNavigation))
        {
            (_lastFocusedNode != null && _lastFocusedNode.IsFocusable
                ? _lastFocusedNode
                : FindAvailableNode(null))?.Focus();
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        for (int i = 0; i < _nodeButtons.Length; i++)
        {
            SkillTreeNodeButton node = _nodeButtons[i];
            bool focused = node.IsFocusable && node.ButtonObject == selected;
            node.SetFocused(focused);
            if (focused && _lastFocusedNode != node)
            {
                _lastFocusedNode = node;
                ConfigureNavigation();
            }
        }
    }

    private void Start()
    {
        for (int i = 0; i < _nodeButtons.Length; i++)
            _nodeButtons[i].Setup(_skillTreeProgression, SelectNode);

        ConfigureNavigation();
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
        Canvas.ForceUpdateCanvases();
        Time.timeScale = 0f;

        _selectedNodeId = null;
        for (int i = 0; i < _nodeButtons.Length; i++)
            _nodeButtons[i].SetSelected(false);
        Refresh();
        ConfigureNavigation();
        FocusAvailableNode(null);
    }

    private void SelectNode(string nodeId)
    {
        _selectedNodeId = nodeId;
        for (int i = 0; i < _nodeButtons.Length; i++)
            _nodeButtons[i].SetSelected(_nodeButtons[i].NodeId == nodeId);
        RefreshDetails();
        ConfigureNavigation();
    }

    private void PurchaseSelected()
    {
        if (_playerHealth.CurrentHealth <= 0 ||
            !_skillTreeProgression.TryPurchase(_selectedNodeId))
            return;

        Refresh();
        ConfigureNavigation();
        if (_playerProgression.SkillPoints <= 0)
            _continueButton.Select();
    }

    private void Refresh()
    {
        _skillPointsText.text = $"Skill Points: {_playerProgression.SkillPoints}";

        for (int i = 0; i < _nodeButtons.Length; i++)
            _nodeButtons[i].Refresh();

        RefreshDetails();
    }

    private void RefreshDetails()
    {
        SkillTreeNode node = _skillTreeProgression.GetNode(_selectedNodeId);
        if (node == null || node.Upgrade == null)
        {
            _detailName.text = "Select an upgrade";
            _detailLevel.text = "";
            _detailDescription.text = "";
            _detailHint.text = "Click an upgrade or press Confirm to inspect it.";
            _detailRequirements.text = "";
            _actionText.text = "Select";
            _actionButton.interactable = false;
            return;
        }

        int level = _skillTreeProgression.GetLevel(_selectedNodeId);
        _detailName.text = node.Upgrade.DisplayName;
        bool healing = node.Upgrade.Effect == UpgradeEffectType.RestoreHealth;
        _detailLevel.text = healing
            ? $"Repeatable | HP: {_playerHealth.CurrentHealth} / {_playerHealth.MaxHealth}"
            : node.Upgrade.IsRepeatable ? "Repeatable" : $"Level {level} / {node.Upgrade.MaxLevel}";
        _detailDescription.text = node.Upgrade.Description;
        _detailRequirements.text = "";

        if (_skillTreeProgression.IsMaxed(_selectedNodeId))
        {
            _actionText.text = "Maxed";
            _detailHint.text = "This upgrade is complete.";
            _actionButton.interactable = false;
        }
        else if (_skillTreeProgression.IsLocked(_selectedNodeId))
        {
            _actionText.text = healing ? "Full HP" : "Locked";
            _detailHint.text = "Requirements not met yet.";
            _detailRequirements.text = string.Join("\n", _skillTreeProgression.GetUnmetRequirements(_selectedNodeId));
            _actionButton.interactable = false;
        }
        else
        {
            _actionText.text = healing ? "Heal 50%" : level == 0 ? "Unlock" : "Upgrade";
            _actionButton.interactable = _skillTreeProgression.CanPurchase(_selectedNodeId);
            _detailHint.text = _playerProgression.SkillPoints > 0
                ? "Cost: 1 skill point"
                : "Earn a skill point to continue.";
        }
    }

    private void ConfigureNavigation()
    {
        for (int i = 0; i < _nodeButtons.Length; i++)
        {
            SkillTreeNodeButton node = _nodeButtons[i];
            if (!node.IsFocusable)
                continue;

            Navigation navigation = node.Button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = FindNeighbor(node, Vector2.up)?.Button;
            navigation.selectOnDown = FindNeighbor(node, Vector2.down)?.Button;
            navigation.selectOnLeft = FindNeighbor(node, Vector2.left)?.Button;
            navigation.selectOnRight = FindNeighbor(node, Vector2.right)?.Button ??
                                       (_actionButton.interactable ? _actionButton : _continueButton);
            node.Button.navigation = navigation;
        }

        Navigation actionNavigation = _actionButton.navigation;
        actionNavigation.mode = Navigation.Mode.Explicit;
        actionNavigation.selectOnLeft = _lastFocusedNode?.Button;
        actionNavigation.selectOnRight = _continueButton;
        _actionButton.navigation = actionNavigation;

        Navigation continueNavigation = _continueButton.navigation;
        continueNavigation.mode = Navigation.Mode.Explicit;
        continueNavigation.selectOnLeft = _actionButton.interactable ? _actionButton : _lastFocusedNode?.Button;
        _continueButton.navigation = continueNavigation;
    }

    private SkillTreeNodeButton FindNeighbor(SkillTreeNodeButton from, Vector2 direction)
    {
        SkillTreeNodeButton best = null;
        float bestRowGap = float.MaxValue;
        float bestDistance = float.MaxValue;
        bool vertical = direction.y != 0f;

        for (int i = 0; i < _nodeButtons.Length; i++)
        {
            SkillTreeNodeButton candidate = _nodeButtons[i];
            if (candidate == from || !candidate.IsFocusable)
                continue;

            Vector2 delta = candidate.Position - from.Position;
            float along = Vector2.Dot(delta, direction);
            if (along <= 1f || (vertical && Mathf.Abs(delta.x) > 40f))
                continue;

            float rowGap = vertical ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
            if (rowGap < bestRowGap - 1f ||
                (Mathf.Abs(rowGap - bestRowGap) <= 1f && along < bestDistance))
            {
                best = candidate;
                bestRowGap = rowGap;
                bestDistance = along;
            }
        }

        return best;
    }

    private SkillTreeNodeButton FindAvailableNode(SkillTreeNodeButton from)
    {
        SkillTreeNodeButton best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _nodeButtons.Length; i++)
        {
            SkillTreeNodeButton candidate = _nodeButtons[i];
            if (!candidate.IsPurchasable)
                continue;

            if (from == null)
            {
                if (best == null || candidate.Position.y > best.Position.y + 1f ||
                    (Mathf.Abs(candidate.Position.y - best.Position.y) <= 1f &&
                     candidate.Position.x < best.Position.x))
                    best = candidate;
            }
            else
            {
                float distance = (candidate.Position - from.Position).sqrMagnitude;
                if (best == null || distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
        }

        return best;
    }

    private void FocusAvailableNode(SkillTreeNodeButton from)
    {
        SkillTreeNodeButton node = FindAvailableNode(from);
        if (node == null)
            return;

        _lastFocusedNode = node;
        ConfigureNavigation();
        node.Focus();
    }

    private void HandlePlayerDied()
    {
        _panel.SetActive(false);
        ClearFocus();
    }

    private void Close()
    {
        _panel.SetActive(false);
        ClearFocus();

        if (_playerHealth.CurrentHealth > 0)
            Time.timeScale = 1f;
    }

    private void ClearFocus()
    {
        for (int i = 0; i < _nodeButtons.Length; i++)
            _nodeButtons[i].SetFocused(false);

        _lastFocusedNode = null;
        _selectedNodeId = null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
