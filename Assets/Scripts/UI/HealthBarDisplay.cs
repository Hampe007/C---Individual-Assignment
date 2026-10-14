using GameMenus;
using UnityEngine;

public sealed class HealthBarDisplay : MonoBehaviour
{
    [SerializeField] private GameObject _worldHealthBar;
    [SerializeField] private GameObject _screenHealthBar;

    private void OnEnable()
    {
        HealthBarLocationPreferences.Changed += Apply;
        Apply();
    }

    private void OnDisable() => HealthBarLocationPreferences.Changed -= Apply;

    private void Apply()
    {
        if (_worldHealthBar == null || _screenHealthBar == null)
        {
            Debug.LogError("HealthBarDisplay is missing a health bar reference.", this);
            return;
        }

        bool onScreen = HealthBarLocationPreferences.OnScreen;
        _worldHealthBar.SetActive(!onScreen);
        _screenHealthBar.SetActive(onScreen);
    }
}
