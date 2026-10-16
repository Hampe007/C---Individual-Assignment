using System;
using TMPro;
using UnityEngine;

public sealed class SkillTreeNodeButton : MonoBehaviour
{
    [SerializeField] private string _nodeId;

    [Header("Scene UI")]
    [SerializeField] private UnityEngine.UI.Button _button;
    [SerializeField] private UnityEngine.UI.Image _background;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private UnityEngine.UI.Image _badgeBackground;
    [SerializeField] private TMP_Text _levelBadgeText;
    [SerializeField] private UnityEngine.UI.Outline _badgeOutline;
    [SerializeField] private UnityEngine.UI.Outline _stateOutline;
    [SerializeField] private UnityEngine.UI.Outline _focusOutline;
    [SerializeField] private UnityEngine.UI.Outline _selectionOutline;

    [Header("Node States")]
    [SerializeField] private Color _lockedColor = new(0.07f, 0.06f, 0.08f);
    [SerializeField] private Color _availableColor = new(0.4f, 0.025f, 0.05f);
    [SerializeField] private Color _maxedColor = new(0.18f, 0.05f, 0.08f);
    [SerializeField] private Color _noPointsColor = new(0.16f, 0.06f, 0.08f);
    [SerializeField] private Color _lockedBorderColor = new(0.32f, 0.31f, 0.34f);
    [SerializeField] private Color _availableBorderColor = new(0.82f, 0.13f, 0.18f);
    [SerializeField] private Color _maxedBorderColor = new(0.68f, 0.68f, 0.72f);
    [SerializeField] private Color _lockedTextColor = new(0.57f, 0.56f, 0.6f);
    [SerializeField] private Color _normalTextColor = new(0.9f, 0.9f, 0.93f);
    [SerializeField] private Color _maxedTextColor = new(0.75f, 0.75f, 0.79f);

    private SkillTreeProgression _progression;
    private Action<string> _onSelected;
    private bool _isValid;

    public string NodeId => _nodeId;
    public GameObject ButtonObject => _button.gameObject;
    public UnityEngine.UI.Button Button => _button;
    public bool IsFocusable => _isValid;
    public bool IsPurchasable => _isValid && _progression.CanPurchase(_nodeId);
    public Vector2 Position => ((RectTransform)transform).anchoredPosition;

    public void Setup(SkillTreeProgression progression, Action<string> onSelected)
    {
        _progression = progression;
        _onSelected = onSelected;

        if (_button == null || _background == null || _nameText == null || _statusText == null ||
            _badgeBackground == null || _levelBadgeText == null || _badgeOutline == null || _stateOutline == null ||
            _focusOutline == null || _selectionOutline == null)
        {
            Debug.LogError("Skill tree node '" + _nodeId + "' is missing scene UI references.", this);
            enabled = false;
            return;
        }

        SkillTreeNode node = _progression.GetNode(_nodeId);
        if (node == null || node.Upgrade == null)
        {
            Debug.LogError("Skill tree node '" + _nodeId + "' is invalid.", this);
            _button.interactable = false;
            return;
        }

        _isValid = true;
        _nameText.text = node.Upgrade.DisplayName;
        _focusOutline.enabled = false;
        _selectionOutline.enabled = false;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(Select);
        Refresh();
    }

    public void Refresh()
    {
        if (_progression == null || !_isValid)
            return;

        bool maxed = _progression.IsMaxed(_nodeId);
        SkillTreeNode node = _progression.GetNode(_nodeId);
        bool repeatable = node.Upgrade.IsRepeatable;
        bool healing = node.Upgrade.Effect == UpgradeEffectType.RestoreHealth;
        _levelBadgeText.text = repeatable ? "+" : maxed ? "MAX" : _progression.GetLevel(_nodeId).ToString();
        _badgeBackground.color = maxed ? _maxedColor : _lockedColor;
        _levelBadgeText.color = _normalTextColor;
        _badgeOutline.enabled = maxed;

        if (maxed)
        {
            _statusText.text = "MAXED";
            ApplyStyle(_maxedColor, _maxedBorderColor, _maxedTextColor);
        }
        else if (_progression.IsLocked(_nodeId))
        {
            _statusText.text = healing ? "FULL HP" : "LOCKED";
            ApplyStyle(_lockedColor, _lockedBorderColor, _lockedTextColor);
        }
        else if (_progression.CanPurchase(_nodeId))
        {
            _statusText.text = healing ? "HEAL" : repeatable ? "REPEATABLE" :
                _progression.GetLevel(_nodeId) == 0 ? "AVAILABLE" : "UPGRADE";
            ApplyStyle(_availableColor, _availableBorderColor, _normalTextColor);
        }
        else
        {
            _statusText.text = "NO POINTS";
            ApplyStyle(_noPointsColor, _lockedBorderColor, _normalTextColor);
        }

        // Every node remains readable, including locked and maxed upgrades.
        _button.interactable = true;
    }

    private void ApplyStyle(Color background, Color border, Color text)
    {
        _background.color = background;
        _stateOutline.effectColor = border;
        _nameText.color = text;
        _statusText.color = text;
    }

    public void SetFocused(bool focused)
    {
        if (_isValid)
            _focusOutline.enabled = focused;
    }

    public void SetSelected(bool selected)
    {
        if (_isValid)
            _selectionOutline.enabled = selected;
    }

    public void Focus()
    {
        if (_isValid && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(ButtonObject);
    }

    private void Select()
    {
        if (_isValid)
            _onSelected?.Invoke(_nodeId);
    }
}
