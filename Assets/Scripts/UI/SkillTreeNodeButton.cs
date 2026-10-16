using System;
using TMPro;
using UnityEngine;

public sealed class SkillTreeNodeButton : MonoBehaviour
{
    [SerializeField] private string nodeId;
    [Header("Scene UI")]
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private UnityEngine.UI.Image background;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private UnityEngine.UI.Image badgeBackground;
    [SerializeField] private TMP_Text levelBadgeText;
    [SerializeField] private UnityEngine.UI.Outline badgeOutline;
    [SerializeField] private UnityEngine.UI.Outline stateOutline;
    [SerializeField] private UnityEngine.UI.Outline focusOutline;
    [SerializeField] private UnityEngine.UI.Outline selectionOutline;
    [Header("Node States")]
    [SerializeField] private Color lockedColor = new(0.07f, 0.06f, 0.08f);
    [SerializeField] private Color availableColor = new(0.4f, 0.025f, 0.05f);
    [SerializeField] private Color maxedColor = new(0.18f, 0.05f, 0.08f);
    [SerializeField] private Color noPointsColor = new(0.16f, 0.06f, 0.08f);
    [SerializeField] private Color lockedBorderColor = new(0.32f, 0.31f, 0.34f);
    [SerializeField] private Color availableBorderColor = new(0.82f, 0.13f, 0.18f);
    [SerializeField] private Color maxedBorderColor = new(0.68f, 0.68f, 0.72f);
    [SerializeField] private Color lockedTextColor = new(0.57f, 0.56f, 0.6f);
    [SerializeField] private Color normalTextColor = new(0.9f, 0.9f, 0.93f);
    [SerializeField] private Color maxedTextColor = new(0.75f, 0.75f, 0.79f);
    private SkillTreeProgression progression;
    private Action<string> onSelected;
    private bool isValid;
    public string NodeId => nodeId;
    public GameObject ButtonObject => button.gameObject;
    public UnityEngine.UI.Button Button => button;
    public bool IsFocusable => isValid;
    public bool IsPurchasable => isValid && progression.CanPurchase(nodeId);
    public Vector2 Position => ((RectTransform)transform).anchoredPosition;

    public void Setup(SkillTreeProgression progression, Action<string> onSelected)
    {
        this.progression = progression;
        this.onSelected = onSelected;
        if (button == null || background == null || nameText == null || statusText == null || badgeBackground == null || levelBadgeText == null || badgeOutline == null || stateOutline == null || focusOutline == null || selectionOutline == null)
        {
            Debug.LogError("Skill tree node '" + nodeId + "' is missing scene UI references.", this);
            enabled = false;
            return;
        }

        SkillTreeNode node = this.progression.GetNode(nodeId);
        if (node == null || node.Upgrade == null)
        {
            Debug.LogError("Skill tree node '" + nodeId + "' is invalid.", this);
            button.interactable = false;
            return;
        }

        isValid = true;
        nameText.text = node.Upgrade.DisplayName;
        focusOutline.enabled = false;
        selectionOutline.enabled = false;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Select);
        Refresh();
    }

    public void Refresh()
    {
        if (progression == null || !isValid)
        {
            return;
        }

        bool maxed = progression.IsMaxed(nodeId);
        SkillTreeNode node = progression.GetNode(nodeId);
        bool repeatable = node.Upgrade.IsRepeatable;
        bool healing = node.Upgrade.Effect == UpgradeEffectType.RestoreHealth;
        levelBadgeText.text = repeatable ? "+" : maxed ? "MAX" : progression.GetLevel(nodeId).ToString();
        badgeBackground.color = maxed ? maxedColor : lockedColor;
        levelBadgeText.color = normalTextColor;
        badgeOutline.enabled = maxed;
        if (maxed)
        {
            statusText.text = "MAXED";
            ApplyStyle(maxedColor, maxedBorderColor, maxedTextColor);
        }
        else if (progression.IsLocked(nodeId))
        {
            statusText.text = healing ? "FULL HP" : "LOCKED";
            ApplyStyle(lockedColor, lockedBorderColor, lockedTextColor);
        }
        else if (progression.CanPurchase(nodeId))
        {
            statusText.text = healing ? "HEAL" : repeatable ? "REPEATABLE" : progression.GetLevel(nodeId) == 0 ? "AVAILABLE" : "UPGRADE";
            ApplyStyle(availableColor, availableBorderColor, normalTextColor);
        }
        else
        {
            statusText.text = "NO POINTS";
            ApplyStyle(noPointsColor, lockedBorderColor, normalTextColor);
        }

        // Every node remains readable, including locked and maxed upgrades.
        button.interactable = true;
    }

    private void ApplyStyle(Color background, Color border, Color text)
    {
        this.background.color = background;
        stateOutline.effectColor = border;
        nameText.color = text;
        statusText.color = text;
    }

    public void SetFocused(bool focused)
    {
        if (isValid)
        {
            focusOutline.enabled = focused;
        }
    }

    public void SetSelected(bool selected)
    {
        if (isValid)
        {
            selectionOutline.enabled = selected;
        }
    }

    public void Focus()
    {
        if (isValid && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(ButtonObject);
        }
    }

    private void Select()
    {
        if (isValid)
        {
            onSelected?.Invoke(nodeId);
        }
    }
}