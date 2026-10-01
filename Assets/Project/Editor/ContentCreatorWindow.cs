using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ContentCreatorWindow : EditorWindow
{
    private const string UxmlPath = "Assets/Project/Editor/ContentCreator.uxml";
    private const string EnemyFolder = "Assets/Project/Data/Enemies";
    private const string UpgradeFolder = "Assets/Project/Data/Upgrades";

    private DropdownField enemyBehaviour;
    private DropdownField enemyTier;
    private DropdownField upgradeEffect;
    private VisualElement swarmSettings;
    private VisualElement chargerSettings;
    private VisualElement bruteSettings;
    private Label enemyStatus;
    private Label upgradeStatus;

    [MenuItem("Tools/Content Creator")]
    public static void Open()
    {
        ContentCreatorWindow window = GetWindow<ContentCreatorWindow>();
        window.titleContent = new GUIContent("Content Creator");
        window.minSize = new Vector2(390f, 600f);
    }

    public void CreateGUI()
    {
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        if (visualTree == null)
        {
            rootVisualElement.Add(new Label($"Could not load {UxmlPath}"));
            return;
        }

        visualTree.CloneTree(rootVisualElement);

        enemyBehaviour = rootVisualElement.Q<DropdownField>("enemyBehaviour");
        enemyTier = rootVisualElement.Q<DropdownField>("enemyTier");
        upgradeEffect = rootVisualElement.Q<DropdownField>("upgradeEffect");
        swarmSettings = rootVisualElement.Q<VisualElement>("swarmSettings");
        chargerSettings = rootVisualElement.Q<VisualElement>("chargerSettings");
        bruteSettings = rootVisualElement.Q<VisualElement>("bruteSettings");
        enemyStatus = rootVisualElement.Q<Label>("enemyStatus");
        upgradeStatus = rootVisualElement.Q<Label>("upgradeStatus");

        enemyBehaviour.choices = new System.Collections.Generic.List<string> { "Swarm", "Charger", "Brute" };
        enemyBehaviour.value = "Swarm";
        enemyBehaviour.RegisterValueChangedCallback(_ => UpdateBehaviourFields());

        enemyTier.choices = new System.Collections.Generic.List<string> { "Tier1", "Tier2", "Tier3" };
        enemyTier.value = "Tier1";

        ObjectField enemyPrefab = rootVisualElement.Q<ObjectField>("enemyPrefab");
        enemyPrefab.objectType = typeof(GameObject);
        enemyPrefab.allowSceneObjects = false;

        upgradeEffect.choices = new System.Collections.Generic.List<string>(Enum.GetNames(typeof(UpgradeEffectType)));
        upgradeEffect.value = UpgradeEffectType.MoveSpeed.ToString();

        rootVisualElement.Q<IntegerField>("enemyHealth").value = 30;
        rootVisualElement.Q<FloatField>("enemyMoveSpeed").value = 3f;
        rootVisualElement.Q<IntegerField>("enemyDamage").value = 10;
        rootVisualElement.Q<IntegerField>("enemyXpReward").value = 1;
        rootVisualElement.Q<IntegerField>("enemyScoreReward").value = 10;
        rootVisualElement.Q<FloatField>("upgradeValue").value = 1f;
        rootVisualElement.Q<IntegerField>("upgradeMaxLevel").value = 5;

        rootVisualElement.Q<Button>("createEnemyButton").clicked += CreateEnemyAsset;
        rootVisualElement.Q<Button>("createUpgradeButton").clicked += CreateUpgradeAsset;
        SetFloatDefault("swarmAttackRange", 1.3f);
        SetFloatDefault("swarmAttackCooldown", 1f);
        SetFloatDefault("chargeRange", 10f);
        SetFloatDefault("chargeSpeed", 12f);
        SetFloatDefault("chargeDuration", 0.55f);
        SetFloatDefault("chargeTelegraphDuration", 0.65f);
        SetFloatDefault("chargeRecoveryDuration", 0.8f);
        SetFloatDefault("chargeCooldown", 5f);
        SetFloatDefault("chargeHitRange", 1.5f);
        SetFloatDefault("slamRange", 3f);
        SetFloatDefault("slamWindupDuration", 1.2f);
        SetFloatDefault("slamAttackDuration", 0.25f);
        SetFloatDefault("slamRecoveryDuration", 1.5f);
        SetFloatDefault("slamAttackCooldown", 2.5f);
        UpdateBehaviourFields();
    }

    private void UpdateBehaviourFields()
    {
        SetVisible(swarmSettings, enemyBehaviour.value == "Swarm");
        SetVisible(chargerSettings, enemyBehaviour.value == "Charger");
        SetVisible(bruteSettings, enemyBehaviour.value == "Brute");
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        element.EnableInClassList("is-hidden", !visible);
    }

    private void CreateEnemyAsset()
    {
        string assetName = rootVisualElement.Q<TextField>("enemyAssetName").value.Trim();
        string displayName = rootVisualElement.Q<TextField>("enemyDisplayName").value.Trim();
        GameObject prefab = rootVisualElement.Q<ObjectField>("enemyPrefab").value as GameObject;

        if (!TryGetAssetPath(EnemyFolder, assetName, out string path, out string error))
        {
            SetStatus(enemyStatus, error, false);
            return;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            SetStatus(enemyStatus, "Enter a display name.", false);
            return;
        }

        if (prefab == null)
        {
            SetStatus(enemyStatus, "Choose a prefab for the enemy.", false);
            return;
        }

        Type definitionType = enemyBehaviour.value switch
        {
            "Charger" => typeof(ChargerEnemyDefinition),
            "Brute" => typeof(BruteEnemyDefinition),
            _ => typeof(SwarmEnemyDefinition)
        };

        EnemyDefinition definition = CreateInstance(definitionType) as EnemyDefinition;
        SerializedObject serialized = new SerializedObject(definition);
        SetString(serialized, "_displayName", displayName);
        SetEnum(serialized, "_tier", enemyTier.index);
        SetObject(serialized, "_prefab", prefab);
        SetInt(serialized, "_health", Mathf.Max(1, IntValue("enemyHealth", 30)));
        SetFloat(serialized, "_moveSpeed", Mathf.Max(0f, FloatValue("enemyMoveSpeed", 3f)));
        SetInt(serialized, "_damage", Mathf.Max(1, IntValue("enemyDamage", 10)));
        SetInt(serialized, "_xpReward", Mathf.Max(0, IntValue("enemyXpReward", 1)));
        SetInt(serialized, "_scoreReward", Mathf.Max(0, IntValue("enemyScoreReward", 10)));

        switch (enemyBehaviour.value)
        {
            case "Charger":
                SetFloat(serialized, "_chargeRange", NonNegative("chargeRange", 10f));
                SetFloat(serialized, "_chargeSpeed", NonNegative("chargeSpeed", 12f));
                SetFloat(serialized, "_chargeDuration", NonNegative("chargeDuration", 0.55f));
                SetFloat(serialized, "_telegraphDuration", NonNegative("chargeTelegraphDuration", 0.65f));
                SetFloat(serialized, "_recoveryDuration", NonNegative("chargeRecoveryDuration", 0.8f));
                SetFloat(serialized, "_chargeCooldown", NonNegative("chargeCooldown", 5f));
                SetFloat(serialized, "_hitRange", NonNegative("chargeHitRange", 1.5f));
                break;
            case "Brute":
                SetFloat(serialized, "_slamRange", NonNegative("slamRange", 3f));
                SetFloat(serialized, "_windupDuration", NonNegative("slamWindupDuration", 1.2f));
                SetFloat(serialized, "_attackDuration", NonNegative("slamAttackDuration", 0.25f));
                SetFloat(serialized, "_recoveryDuration", NonNegative("slamRecoveryDuration", 1.5f));
                SetFloat(serialized, "_attackCooldown", NonNegative("slamAttackCooldown", 2.5f));
                break;
            default:
                SetFloat(serialized, "_attackRange", NonNegative("swarmAttackRange", 1.3f));
                SetFloat(serialized, "_attackCooldown", NonNegative("swarmAttackCooldown", 1f));
                break;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        SaveAsset(definition, path, enemyStatus, "Enemy asset created.");
    }

    private void CreateUpgradeAsset()
    {
        string assetName = rootVisualElement.Q<TextField>("upgradeAssetName").value.Trim();
        string displayName = rootVisualElement.Q<TextField>("upgradeDisplayName").value.Trim();

        if (!TryGetAssetPath(UpgradeFolder, assetName, out string path, out string error))
        {
            SetStatus(upgradeStatus, error, false);
            return;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            SetStatus(upgradeStatus, "Enter a display name.", false);
            return;
        }

        UpgradeDefinition definition = CreateInstance<UpgradeDefinition>();
        SerializedObject serialized = new SerializedObject(definition);
        SetString(serialized, "_displayName", displayName);
        SetString(serialized, "_description", rootVisualElement.Q<TextField>("upgradeDescription").value);
        SetEnum(serialized, "_effect", upgradeEffect.index);
        SetFloat(serialized, "_value", FloatValue("upgradeValue", 1f));
        SetInt(serialized, "_maxLevel", Mathf.Max(1, IntValue("upgradeMaxLevel", 5)));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SaveAsset(definition, path, upgradeStatus, "Upgrade asset created.");
    }

    private static bool TryGetAssetPath(string folder, string assetName, out string path, out string error)
    {
        path = null;
        error = null;

        if (string.IsNullOrWhiteSpace(assetName))
        {
            error = "Enter an asset name.";
            return false;
        }

        if (assetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || assetName.Contains("/") || assetName.Contains("\\"))
        {
            error = "Asset name contains invalid characters.";
            return false;
        }

        if (!AssetDatabase.IsValidFolder(folder))
        {
            error = $"Asset folder does not exist: {folder}";
            return false;
        }

        path = $"{folder}/{assetName}.asset";
        if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path))
        {
            error = $"An asset named '{assetName}' already exists in {folder}.";
            return false;
        }

        return true;
    }

    private static void SaveAsset(ScriptableObject asset, string path, Label status, string successMessage)
    {
        try
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            SetStatus(status, successMessage, true);
        }
        catch (Exception exception)
        {
            DestroyImmediate(asset);
            SetStatus(status, $"Could not create asset: {exception.Message}", false);
        }
    }

    private static void SetStatus(Label label, string message, bool success)
    {
        label.text = message;
        label.EnableInClassList("status-success", success);
        label.EnableInClassList("status-error", !success);
    }

    private int IntValue(string name, int fallback)
    {
        IntegerField field = rootVisualElement.Q<IntegerField>(name);
        return field != null ? field.value : fallback;
    }

    private float FloatValue(string name, float fallback)
    {
        FloatField field = rootVisualElement.Q<FloatField>(name);
        return field != null ? field.value : fallback;
    }

    private float NonNegative(string name, float fallback) => Mathf.Max(0f, FloatValue(name, fallback));

    private static void SetString(SerializedObject serialized, string propertyName, string value)
    {
        serialized.FindProperty(propertyName).stringValue = value;
    }

    private static void SetInt(SerializedObject serialized, string propertyName, int value)
    {
        serialized.FindProperty(propertyName).intValue = value;
    }

    private static void SetFloat(SerializedObject serialized, string propertyName, float value)
    {
        serialized.FindProperty(propertyName).floatValue = value;
    }

    private static void SetEnum(SerializedObject serialized, string propertyName, int index)
    {
        serialized.FindProperty(propertyName).enumValueIndex = index;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        serialized.FindProperty(propertyName).objectReferenceValue = value;
    }

    private void SetFloatDefault(string name, float value)
    {
        rootVisualElement.Q<FloatField>(name).value = value;
    }
}
