using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameMenus
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TMPLinkOpener : Selectable, IPointerClickHandler, IPointerMoveHandler, ISubmitHandler
    {
        private TMP_Text text;
        private ScrollRect scroll;
        private int focusedLink;
        private int hoveredLink = -1;
        private bool selected;
        private static readonly Color32 Highlight = new(225, 85, 103, 255);

        protected override void Awake()
        {
            base.Awake();
            text = GetComponent<TMP_Text>();
            transition = Transition.None;
            text.OnPreRenderText += HighlightLink;
        }

        protected override void OnDestroy()
        {
            if (text != null) text.OnPreRenderText -= HighlightLink;
            base.OnDestroy();
        }

        protected override void OnDisable()
        {
            selected = false;
            hoveredLink = -1;
            base.OnDisable();
        }

        public void Configure(ScrollRect creditsScroll, Button back)
        {
            scroll = creditsScroll;
            focusedLink = 0;
            hoveredLink = -1;
            navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = back, selectOnDown = back, selectOnLeft = back, selectOnRight = back };
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = this, selectOnDown = this };
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            selected = true;
            hoveredLink = -1;
            RevealLink();
            RefreshHighlight();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            selected = false;
            RefreshHighlight();
        }

        public override void OnMove(AxisEventData eventData)
        {
            int step = eventData.moveDir == MoveDirection.Up || eventData.moveDir == MoveDirection.Left ? -1 : 1;
            int next = focusedLink + step;
            if (next < 0 || next >= text.textInfo.linkCount)
            {
                base.OnMove(eventData);
                return;
            }
            focusedLink = next;
            hoveredLink = -1;
            RevealLink();
            RefreshHighlight();
            eventData.Use();
        }

        public void OnSubmit(BaseEventData eventData) => OpenLink(focusedLink);

        public void OnPointerMove(PointerEventData eventData)
        {
            int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.enterEventCamera);
            if (hoveredLink == index) return;
            hoveredLink = index;
            RefreshHighlight();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            hoveredLink = -1;
            RefreshHighlight();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging) return;
            int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
            if (index < 0) return;
            focusedLink = index;
            RefreshHighlight();
            OpenLink(index);
        }

        private void OpenLink(int index)
        {
            if (!IsActive() || !IsInteractable() || index < 0 || index >= text.textInfo.linkCount) return;
            string url = text.textInfo.linkInfo[index].GetLinkID();
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                Application.OpenURL(uri.AbsoluteUri);
        }

        private void RefreshHighlight()
        {
            if (text != null && text.isActiveAndEnabled) text.ForceMeshUpdate();
        }

        private void HighlightLink(TMP_TextInfo info)
        {
            int index = hoveredLink >= 0 ? hoveredLink : selected ? focusedLink : -1;
            if (index < 0 || index >= info.linkCount) return;
            TMP_LinkInfo link = info.linkInfo[index];
            for (int i = link.linkTextfirstCharacterIndex; i < link.linkTextfirstCharacterIndex + link.linkTextLength; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;
                Color32[] colors = info.meshInfo[character.materialReferenceIndex].colors32;
                for (int vertex = 0; vertex < 4; vertex++) colors[character.vertexIndex + vertex] = Highlight;
            }
        }

        private void RevealLink()
        {
            text.ForceMeshUpdate();
            if (scroll == null || focusedLink >= text.textInfo.linkCount) return;
            TMP_LinkInfo link = text.textInfo.linkInfo[focusedLink];
            var first = text.textInfo.characterInfo[link.linkTextfirstCharacterIndex];
            var last = text.textInfo.characterInfo[link.linkTextfirstCharacterIndex + link.linkTextLength - 1];
            var viewport = scroll.viewport;
            float top = viewport.InverseTransformPoint(text.transform.TransformPoint(new Vector3(0, first.ascender, 0))).y;
            float bottom = viewport.InverseTransformPoint(text.transform.TransformPoint(new Vector3(0, last.descender, 0))).y;
            float offset = top > viewport.rect.yMax - 12 ? viewport.rect.yMax - 12 - top : bottom < viewport.rect.yMin + 12 ? viewport.rect.yMin + 12 - bottom : 0;
            scroll.StopMovement();
            scroll.content.anchoredPosition += Vector2.up * offset;
        }
    }
}
