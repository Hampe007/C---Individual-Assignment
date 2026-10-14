using UnityEngine;
using UnityEngine.EventSystems;

namespace GameMenus
{
    [RequireComponent(typeof(UnityEngine.UI.Toggle))]
    public sealed class HealthBarLocationToggle : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] private GameObject focusMarker;
        private UnityEngine.UI.Toggle _toggle;

        private void Awake() => _toggle = GetComponent<UnityEngine.UI.Toggle>();

        private void OnEnable()
        {
            _toggle.SetIsOnWithoutNotify(HealthBarLocationPreferences.OnScreen);
            _toggle.onValueChanged.AddListener(HealthBarLocationPreferences.SetOnScreen);
            HealthBarLocationPreferences.Changed += Refresh;
        }

        private void OnDisable()
        {
            _toggle.onValueChanged.RemoveListener(HealthBarLocationPreferences.SetOnScreen);
            HealthBarLocationPreferences.Changed -= Refresh;
            if (focusMarker != null)
                focusMarker.SetActive(false);
        }

        private void Refresh() => _toggle.SetIsOnWithoutNotify(HealthBarLocationPreferences.OnScreen);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && _toggle.IsInteractable())
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (focusMarker != null)
                focusMarker.SetActive(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (focusMarker != null)
                focusMarker.SetActive(false);
        }
    }
}
