using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SkillTreeMenu : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SkillTreeProgression skillTreeProgression;
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text skillPointsText;
    [SerializeField] private SkillTreeNodeButton[] nodeButtons;
    [SerializeField] private RectTransform nodesContainer;
    [SerializeField] private RectTransform detailsPanel;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailLevel;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailHint;
    [SerializeField] private TMP_Text detailRequirements;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private UnityEngine.UI.Button actionButton;
    [SerializeField] private UnityEngine.UI.Button continueButton;
    private SkillTreeNodeButton lastFocusedNode;
    private string selectedNodeId;
    private void Awake()
    {
        if (playerProgression == null || playerHealth == null || skillTreeProgression == null || panel == null || skillPointsText == null || nodesContainer == null || detailsPanel == null || detailName == null || detailLevel == null || detailDescription == null || detailHint == null || detailRequirements == null || actionText == null || actionButton == null || continueButton == null || nodeButtons == null || nodeButtons.Length == 0)
        {
            Debug.LogError("SkillTreeMenu is missing required references.");
            enabled = false;
            return;
        }

        for (int i = 0; i < nodeButtons.Length; i++)
        {
            if (nodeButtons[i] != null)
            {
                continue;
            }

            Debug.LogError($"Skill tree button at index {i} is missing.");
            enabled = false;
            return;
        }

        actionButton.onClick.AddListener(PurchaseSelected);
        continueButton.onClick.AddListener(Close);
        if (!actionButton.TryGetComponent<SkillTreeActionButton>(out _))
        {
            actionButton.gameObject.AddComponent<SkillTreeActionButton>();
        }

        if (!continueButton.TryGetComponent<SkillTreeActionButton>(out _))
        {
            continueButton.gameObject.AddComponent<SkillTreeActionButton>();
        }

        panel.SetActive(false);
    }

    private void Update()
    {
        bool toggle = (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
        if (!panel.activeSelf)
        {
            if (toggle && Time.timeScale > 0f && !PauseMenu.BlocksGameplayInput)
            {
                Show(playerProgression.Level);
            }

            return;
        }

        if (toggle || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
        {
            Close();
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        bool keyboardNavigation = Keyboard.current != null && (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame);
        bool controllerNavigation = Gamepad.current != null && (Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.25f || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.25f);
        if (eventSystem.currentSelectedGameObject == null && (keyboardNavigation || controllerNavigation))
        {
            (lastFocusedNode != null && lastFocusedNode.IsFocusable ? lastFocusedNode : FindAvailableNode(null))?.Focus();
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            SkillTreeNodeButton node = nodeButtons[i];
            bool focused = node.IsFocusable && node.ButtonObject == selected;
            node.SetFocused(focused);
            if (focused && lastFocusedNode != node)
            {
                lastFocusedNode = node;
                ConfigureNavigation();
            }
        }
    }

    private void Start()
    {
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            nodeButtons[i].Setup(skillTreeProgression, SelectNode);
        }

        ConfigureNavigation();
    }

    private void OnEnable()
    {
        playerProgression.LeveledUp += Show;
        playerHealth.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (playerProgression != null)
        {
            playerProgression.LeveledUp -= Show;
        }

        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
        }
    }

    private void Show(int level)
    {
        if (playerHealth.CurrentHealth <= 0)
        {
            return;
        }

        if (playerProgression.SkillPoints <= 0)
        {
            return;
        }

        if (!skillTreeProgression.HasAvailableNode())
        {
            return;
        }

        panel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Time.timeScale = 0f;
        selectedNodeId = null;
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            nodeButtons[i].SetSelected(false);
        }

        Refresh();
        ConfigureNavigation();
        FocusAvailableNode(null);
    }

    private void SelectNode(string nodeId)
    {
        selectedNodeId = nodeId;
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            nodeButtons[i].SetSelected(nodeButtons[i].NodeId == nodeId);
        }

        RefreshDetails();
        ConfigureNavigation();
    }

    private void PurchaseSelected()
    {
        if (playerHealth.CurrentHealth <= 0 || !skillTreeProgression.TryPurchase(selectedNodeId))
        {
            return;
        }

        Refresh();
        ConfigureNavigation();
        if (playerProgression.SkillPoints <= 0)
        {
            continueButton.Select();
        }
    }

    private void Refresh()
    {
        skillPointsText.text = $"Skill Points: {playerProgression.SkillPoints}";
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            nodeButtons[i].Refresh();
        }

        RefreshDetails();
    }

    private void RefreshDetails()
    {
        SkillTreeNode node = skillTreeProgression.GetNode(selectedNodeId);
        if (node == null || node.Upgrade == null)
        {
            detailName.text = "Select an upgrade";
            detailLevel.text = "";
            detailDescription.text = "";
            detailHint.text = "Click an upgrade or press Confirm to inspect it.";
            detailRequirements.text = "";
            actionText.text = "Select";
            actionButton.interactable = false;
            return;
        }

        int level = skillTreeProgression.GetLevel(selectedNodeId);
        detailName.text = node.Upgrade.DisplayName;
        bool healing = node.Upgrade.Effect == UpgradeEffectType.RestoreHealth;
        detailLevel.text = healing ? $"Repeatable | HP: {playerHealth.CurrentHealth} / {playerHealth.MaxHealth}" : node.Upgrade.IsRepeatable ? "Repeatable" : $"Level {level} / {node.Upgrade.MaxLevel}";
        detailDescription.text = node.Upgrade.Description;
        detailRequirements.text = "";
        if (skillTreeProgression.IsMaxed(selectedNodeId))
        {
            actionText.text = "Maxed";
            detailHint.text = "This upgrade is complete.";
            actionButton.interactable = false;
        }
        else if (skillTreeProgression.IsLocked(selectedNodeId))
        {
            actionText.text = healing ? "Full HP" : "Locked";
            detailHint.text = "Requirements not met yet.";
            detailRequirements.text = string.Join("\n", skillTreeProgression.GetUnmetRequirements(selectedNodeId));
            actionButton.interactable = false;
        }
        else
        {
            actionText.text = healing ? "Heal 50%" : level == 0 ? "Unlock" : "Upgrade";
            actionButton.interactable = skillTreeProgression.CanPurchase(selectedNodeId);
            detailHint.text = playerProgression.SkillPoints > 0 ? "Cost: 1 skill point" : "Earn a skill point to continue.";
        }
    }

    private void ConfigureNavigation()
    {
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            SkillTreeNodeButton node = nodeButtons[i];
            if (!node.IsFocusable)
            {
                continue;
            }

            Navigation navigation = node.Button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = FindNeighbor(node, Vector2.up)?.Button;
            navigation.selectOnDown = FindNeighbor(node, Vector2.down)?.Button;
            navigation.selectOnLeft = FindNeighbor(node, Vector2.left)?.Button;
            navigation.selectOnRight = FindNeighbor(node, Vector2.right)?.Button ?? (actionButton.interactable ? actionButton : continueButton);
            node.Button.navigation = navigation;
        }

        Navigation actionNavigation = actionButton.navigation;
        actionNavigation.mode = Navigation.Mode.Explicit;
        actionNavigation.selectOnLeft = lastFocusedNode?.Button;
        actionNavigation.selectOnRight = continueButton;
        actionButton.navigation = actionNavigation;
        Navigation continueNavigation = continueButton.navigation;
        continueNavigation.mode = Navigation.Mode.Explicit;
        continueNavigation.selectOnLeft = actionButton.interactable ? actionButton : lastFocusedNode?.Button;
        continueButton.navigation = continueNavigation;
    }

    private SkillTreeNodeButton FindNeighbor(SkillTreeNodeButton from, Vector2 direction)
    {
        SkillTreeNodeButton best = null;
        float bestRowGap = float.MaxValue;
        float bestDistance = float.MaxValue;
        bool vertical = direction.y != 0f;
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            SkillTreeNodeButton candidate = nodeButtons[i];
            if (candidate == from || !candidate.IsFocusable)
            {
                continue;
            }

            Vector2 delta = candidate.Position - from.Position;
            float along = Vector2.Dot(delta, direction);
            if (along <= 1f || (vertical && Mathf.Abs(delta.x) > 40f))
            {
                continue;
            }

            float rowGap = vertical ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
            if (rowGap < bestRowGap - 1f || (Mathf.Abs(rowGap - bestRowGap) <= 1f && along < bestDistance))
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
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            SkillTreeNodeButton candidate = nodeButtons[i];
            if (!candidate.IsPurchasable)
            {
                continue;
            }

            if (from == null)
            {
                if (best == null || candidate.Position.y > best.Position.y + 1f || (Mathf.Abs(candidate.Position.y - best.Position.y) <= 1f && candidate.Position.x < best.Position.x))
                {
                    best = candidate;
                }
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
        {
            return;
        }

        lastFocusedNode = node;
        ConfigureNavigation();
        node.Focus();
    }

    private void HandlePlayerDied()
    {
        panel.SetActive(false);
        ClearFocus();
    }

    private void Close()
    {
        panel.SetActive(false);
        ClearFocus();
        if (playerHealth.CurrentHealth > 0)
        {
            Time.timeScale = 1f;
        }
    }

    private void ClearFocus()
    {
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            nodeButtons[i].SetFocused(false);
        }

        lastFocusedNode = null;
        selectedNodeId = null;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}