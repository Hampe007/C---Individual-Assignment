using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private Outline _focusOutline;
    private Vector3 _normalScale;
    private bool _isValid;
    private bool _focused;

    public string NodeId => _nodeId;
    public GameObject ButtonObject => _button.gameObject;
    public Button Button => _button;
    public bool IsFocusable => _isValid;
    public bool IsPurchasable => _isValid && _progression.CanPurchase(_nodeId);
    public Vector2 Position => ((RectTransform)transform).anchoredPosition;

    public void Setup(SkillTreeProgression progression, Action<string> onSelected)
    {
        _progression = progression;
        _onSelected = onSelected;
        _normalScale = transform.localScale;

        SkillTreeNode node = _progression.GetNode(_nodeId);

        if (node == null || node.Upgrade == null)
        {
            Debug.LogError($"Skill tree node '{_nodeId}' is invalid.");
            _button.interactable = false;
            return;
        }

        _isValid = true;
        _nameText.text = node.Upgrade.DisplayName;
        _button.transition = Selectable.Transition.None;

        _focusOutline = _background.GetComponent<Outline>();
        if (_focusOutline == null)
            _focusOutline = _background.gameObject.AddComponent<Outline>();

        _focusOutline.effectColor = new Color(1f, 0.72f, 0.12f, 1f);
        _focusOutline.effectDistance = new Vector2(5f, -5f);
        _focusOutline.useGraphicAlpha = false;
        _focusOutline.enabled = false;

        SkillTreeNodeHover hover = _button.GetComponent<SkillTreeNodeHover>();
        if (hover == null)
            hover = _button.gameObject.AddComponent<SkillTreeNodeHover>();
        hover.SetOwner(this);

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
            _button.interactable = true;
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
        _button.interactable = true;
    }

    public void SetFocused(bool focused)
    {
        if (!_isValid)
            return;

        if (_focused == focused)
            return;

        _focused = focused;
        _focusOutline.enabled = focused;
        transform.localScale = focused ? _normalScale * 1.04f : _normalScale;
        if (focused)
            transform.SetAsLastSibling();
    }

    public void Focus()
    {
        if (_isValid && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(ButtonObject);
    }

    private void Select()
    {
        if (IsPurchasable)
            _onSelected?.Invoke(_nodeId);
    }
}

// A Button handles pointer events itself, so the hover listener belongs on that same object.
public sealed class SkillTreeNodeHover : MonoBehaviour, IPointerEnterHandler
{
    private SkillTreeNodeButton _owner;

    public void SetOwner(SkillTreeNodeButton owner) => _owner = owner;

    public void OnPointerEnter(PointerEventData eventData) => _owner?.Focus();
}
