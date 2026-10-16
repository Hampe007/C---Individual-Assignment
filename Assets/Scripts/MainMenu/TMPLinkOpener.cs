using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameMenus
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TMPLinkOpener : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text text;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging)
            {
                return;
            }

            int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
            if (index < 0)
            {
                return;
            }

            string url = text.textInfo.linkInfo[index].GetLinkID();
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                Application.OpenURL(uri.AbsoluteUri);
            }
        }
    }
}
