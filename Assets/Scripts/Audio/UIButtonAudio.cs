using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISelectHandler, ISubmitHandler
{
    [SerializeField] private SoundSet hoverSfx;
    [SerializeField] private SoundSet clickSfx;

    private int _lastHoverFrame = -1;

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
        if (AudioManager.Instance != null && _lastHoverFrame != Time.frameCount)
        {
            _lastHoverFrame = Time.frameCount;
            AudioManager.Instance.PlaySfx(hoverSfx);
        }
    }

    private void PlayClick()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(clickSfx);
        }
    }
}
