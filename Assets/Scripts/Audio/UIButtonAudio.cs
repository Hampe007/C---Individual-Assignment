using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISelectHandler, ISubmitHandler
{
    [SerializeField] private SoundSet _hoverSFX;
    [SerializeField] private SoundSet _clickSFX;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayClick();
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlayHover();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClick();
    }

    private void PlayHover()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(_hoverSFX);
    }

    private void PlayClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(_clickSFX);
    }
}