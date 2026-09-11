using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTreeNodeButton : MonoBehaviour
{
    [SerializeField] private string _nodeId;

    [Header("UI")]
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _statusText;

    [Header("Colors")]
    [SerializeField] private Color _lockedColor = Color.gray;
    [SerializeField] private Color _availableColor = Color.white;
    [SerializeField] private Color _purchasedColor = Color.green;

    private SkillTreeProgression _progression;
    private Action<string> _onSelected;

    public string NodeId => _nodeId;
    public GameObject ButtonObject => _button.gameObject;
    public bool IsInteractable => _button.interactable;

    public void Setup(SkillTreeProgression progression, Action<string> onSelected)
    {
        _progression = progression;
        _onSelected = onSelected;

        SkillTreeNode node = _progression.GetNode(_nodeId);

        if (node == null || node.Upgrade == null)
        {
            Debug.LogError($"Skill tree node '{_nodeId}' is invalid.");
            _button.interactable = false;
            return;
        }

        _nameText.text = node.Upgrade.DisplayName;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(Select);

        Refresh();
    }

    public void Refresh()
    {
        if (_progression == null)
            return;

        if (_progression.IsPurchased(_nodeId))
        {
            _statusText.text = "Purchased";
            _background.color = _purchasedColor;
            _button.interactable = false;
            return;
        }

        if (_progression.CanPurchase(_nodeId))
        {
            _statusText.text = "Available";
            _background.color = _availableColor;
            _button.interactable = true;
            return;
        }

        _statusText.text = "Locked";
        _background.color = _lockedColor;
        _button.interactable = false;
    }

    private void Select()
    {
        _onSelected?.Invoke(_nodeId);
    }
}