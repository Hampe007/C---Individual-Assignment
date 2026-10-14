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
    private RectTransform _nodesContainer;
    private SkillTreeNodeButton _lastFocusedNode;

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

        _nodesContainer = (RectTransform)_nodeButtons[0].transform.parent;
        _panel.SetActive(false);
    }

    private void Update()
    {
        if (!_panel.activeSelf)
            return;

        FitNodes();

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        if (eventSystem.currentSelectedGameObject == null && Gamepad.current != null &&
            (Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.25f ||
             Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.25f))
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
            if (focused)
                _lastFocusedNode = node;
        }
    }

    private void FitNodes()
    {
        // Fit only the skill grid, preserving the existing HUD's canvas scale.
        Rect bounds = ((RectTransform)_panel.transform).rect;
        float scale = Mathf.Clamp(Mathf.Min(bounds.width / 1920f, bounds.height / 1080f), 0.1f, 1f);
        Vector3 size = Vector3.one * scale;
        if (_nodesContainer.localScale != size)
            _nodesContainer.localScale = size;
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
        FitNodes();
        Time.timeScale = 0f;

        Refresh();
        FocusAvailableNode(null);
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

        SkillTreeNodeButton purchasedNode = null;
        for (int i = 0; i < _nodeButtons.Length; i++)
        {
            if (_nodeButtons[i].NodeId == nodeId)
            {
                purchasedNode = _nodeButtons[i];
                break;
            }
        }

        FocusAvailableNode(purchasedNode);
    }

    private void Refresh()
    {
        _skillPointsText.text = $"Skill Points: {_playerProgression.SkillPoints}";

        for (int i = 0; i < _nodeButtons.Length; i++)
            _nodeButtons[i].Refresh();
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
            navigation.selectOnRight = FindNeighbor(node, Vector2.right)?.Button;
            node.Button.navigation = navigation;
        }
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
    }
}
