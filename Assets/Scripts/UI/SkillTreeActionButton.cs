using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button), typeof(Outline))]
public sealed class SkillTreeActionButton : MonoBehaviour, IPointerEnterHandler
{
    private Button button;
    private Outline outline;
    private Color normalBackground;
    private Color normalOutline;
    private Vector2 normalOutlineDistance;

    private void Awake()
    {
        button = GetComponent<Button>();
        outline = GetComponent<Outline>();
        normalBackground = button.targetGraphic.color;
        normalOutline = outline.effectColor;
        normalOutlineDistance = outline.effectDistance;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f);
        button.colors = colors;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.IsInteractable() && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    private void LateUpdate()
    {
        bool focused = button.IsInteractable() && EventSystem.current != null &&
                       EventSystem.current.currentSelectedGameObject == gameObject;
        button.targetGraphic.color = focused ? new Color(0.48f, 0.035f, 0.07f) : normalBackground;
        outline.effectColor = focused ? new Color(1f, 0.92f, 0.72f) : normalOutline;
        outline.effectDistance = focused ? new Vector2(3f, -3f) : normalOutlineDistance;
    }

    private void OnDisable()
    {
        if (button == null)
        {
            return;
        }

        button.targetGraphic.color = normalBackground;
        outline.effectColor = normalOutline;
        outline.effectDistance = normalOutlineDistance;
    }
}
