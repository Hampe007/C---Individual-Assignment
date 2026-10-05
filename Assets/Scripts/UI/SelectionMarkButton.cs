using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class SelectionMarkButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject marker;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_ColorGradient normalGradient;
    [SerializeField] private TMP_ColorGradient highlightGradient;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        SetHighlighted(false);
    }

    private void OnEnable()
    {
        SetHighlighted(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null && button.IsInteractable())
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlighted(false);
    }

    private void SetHighlighted(bool highlighted)
    {
        if (marker == null || label == null)
        {
            return;
        }

        bool active = highlighted && (button == null || button.IsInteractable());
        marker.SetActive(active);
        label.colorGradientPreset = active ? highlightGradient : normalGradient;
        label.color = normalGradient.topLeft;
        label.enableVertexGradient = true;
    }
}
