using GameMenus;
using UnityEngine;

public sealed class HealthBarDisplay : MonoBehaviour
{
    [SerializeField] private GameObject worldHealthBar;
    [SerializeField] private GameObject screenHealthBar;

    private void OnEnable()
    {
        HealthBarLocationPreferences.Changed += Apply;
        Apply();
    }

    private void OnDisable() => HealthBarLocationPreferences.Changed -= Apply;

    private void Apply()
    {
        if (worldHealthBar == null || screenHealthBar == null)
        {
            Debug.LogError("HealthBarDisplay is missing a health bar reference.", this);
            return;
        }

        bool onScreen = HealthBarLocationPreferences.OnScreen;
        worldHealthBar.SetActive(!onScreen);
        screenHealthBar.SetActive(onScreen);
    }
}
