using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace GameMenus
{
    public sealed class MenuOptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
    {
        private MainMenuDirector director;
        [SerializeField, TextArea] private string description;
        [SerializeField] private MenuOption option;
        [SerializeField] private GameObject marker;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_ColorGradient normalGradient;
        [SerializeField] private TMP_ColorGradient highlightGradient;


        public string Description => description;
        public void SetDirector(MainMenuDirector director) => this.director = director;

        public void Activate()
        {
            director.Activate(option);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            director.Highlight(option);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject)
            {
                director.Highlight(MenuOption.None);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            director.Highlight(option);
        }

        public void SetHighlighted(bool highlighted)
        {
            marker.SetActive(highlighted);
            label.colorGradientPreset = highlighted ? highlightGradient : normalGradient;
        }

        public MenuOption Option => option;
    }
}
