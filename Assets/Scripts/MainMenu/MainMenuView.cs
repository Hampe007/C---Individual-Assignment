using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameMenus
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup menu;
        [SerializeField] private CanvasGroup readability;
        [SerializeField] private GameObject skip;
        [SerializeField] private RadialProgressGraphic skipProgress;
        [SerializeField] private GameObject modal;
        [SerializeField] private CanvasGroup modalGroup;
        [SerializeField] private CanvasGroup skipGroup;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.4f;
        [SerializeField] private TMP_Text modalTitle;
        [SerializeField] private TMP_Text modalBody;
        [SerializeField] private GameObject optionsContent;
        [SerializeField] private UnityEngine.UI.Button primaryButton;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private UnityEngine.UI.Button backButton;
        [SerializeField] private UnityEngine.UI.Button optionsBackButton;
        [SerializeField] private TMP_Text backLabel;
        [SerializeField] private TMP_Text status;
        [SerializeField, TextArea] private string defaultStatus = "Stay quiet. You are not alone.";
        private MenuOptionButton[] buttons;
        [SerializeField] private TMP_Text highScore;
        [SerializeField] private RectTransform modalPanel;
        [SerializeField] private Vector2 quitModalPosition = new(-450f, 0f);

        private GameObject previousSelection;
        private Vector2 modalPosition;
        private Vector2 skipPosition;
        private Coroutine menuFade;
        private Coroutine modalFade;
        private Coroutine skipFade;
        private bool initialized;
        private bool menuVisible;
        private bool modalVisible;
        private bool skipVisible;


        public void Initialize(MainMenuDirector director)
        {
            buttons = GetComponentsInChildren<MenuOptionButton>(true);
            modalPosition = modalPanel.anchoredPosition;
            skipPosition = ((RectTransform)skip.transform).anchoredPosition;
            foreach (MenuOptionButton button in buttons)
            {
                button.SetDirector(director);
            }
            highScore.text = "HIGH SCORE  /  " + PlayerProgression.HighScore;
            primaryButton.onClick.AddListener(director.ConfirmModal);
            backButton.onClick.AddListener(director.CloseModal);
            optionsBackButton.onClick.AddListener(director.CloseModal);
            ShowMenu(false);
            modalGroup.alpha = 0f;
            modalGroup.interactable = false;
            modalGroup.blocksRaycasts = false;
            skipGroup.alpha = 0f;
            skipGroup.interactable = false;
            skipGroup.blocksRaycasts = false;
            modal.SetActive(false);
            skip.SetActive(false);
            initialized = true;
        }

        public void ShowMenu(bool visible)
        {
            if (initialized && menuVisible == visible)
            {
                return;
            }
            menuVisible = visible;
            menu.interactable = false;
            menu.blocksRaycasts = false;
            if (initialized)
            {
                StartFade(ref menuFade, menu, visible);
            }
            else
            {
                SetOpacity(menu, visible ? 1f : 0f);
            }
            if (!visible && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void SetSkip(float progress, bool visible)
        {
            skipProgress.Progress = progress;
            if (skipVisible == visible)
            {
                return;
            }
            skipVisible = visible;
            StartFade(ref skipFade, skipGroup, visible, true);
        }

        private void StartFade(ref Coroutine routine, CanvasGroup group, bool visible, bool animateSkip = false)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            group.gameObject.SetActive(true);
            routine = StartCoroutine(Fade(group, visible, animateSkip));
        }

        private IEnumerator Fade(CanvasGroup group, bool visible, bool animateSkip)
        {
            float start = group.alpha;
            float elapsed = 0f;
            var rect = (RectTransform)group.transform;
            while (elapsed < fadeDuration)
            {
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                SetOpacity(group, Mathf.Lerp(start, visible ? 1f : 0f, eased));
                if (animateSkip && visible)
                {
                    rect.anchoredPosition = skipPosition + Vector2.down * (1f - eased) * 14f;
                    rect.localScale = Vector3.one * (Mathf.Lerp(0.92f, 1f, eased) + Mathf.Sin(progress * Mathf.PI) * 0.04f);
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            SetOpacity(group, visible ? 1f : 0f);
            if (animateSkip)
            {
                rect.anchoredPosition = skipPosition;
                rect.localScale = Vector3.one;
            }
            if (group == menu)
            {
                menu.interactable = visible && !modalVisible;
                menu.blocksRaycasts = visible;
            }
            else if (!visible)
            {
                group.gameObject.SetActive(false);
            }
        }

        private void SetOpacity(CanvasGroup group, float alpha)
        {
            group.alpha = alpha;
            if (group == menu && readability != null)
            {
                readability.alpha = alpha;
            }
        }

        public void Highlight(MenuOption option)
        {
            status.text = defaultStatus;
            foreach (MenuOptionButton button in buttons)
            {
                bool highlighted = button.Option == option;
                button.SetHighlighted(highlighted);
                if (highlighted)
                {
                    status.text = button.Description;
                }
            }
        }

        public void ShowModal(string title, string body, string primary, string back, bool options = false, bool primaryEnabled = true)
        {
            if (EventSystem.current != null)
            {
                previousSelection = EventSystem.current.currentSelectedGameObject;
            }
            modalTitle.text = title;
            modalBody.text = body;
            optionsContent.SetActive(options);
            modalPanel.gameObject.SetActive(!options);
            primaryButton.gameObject.SetActive(!string.IsNullOrEmpty(primary));
            primaryButton.interactable = primaryEnabled;
            primaryLabel.text = primary;
            backLabel.text = back;
            modal.SetActive(true);
            modalVisible = true;
            modalGroup.interactable = true;
            modalGroup.blocksRaycasts = true;
            StartFade(ref modalFade, modalGroup, true);
            menu.interactable = false;
            if (EventSystem.current != null)
            {
                GameObject selection = backButton.gameObject;
                if (primaryEnabled && !string.IsNullOrEmpty(primary))
                {
                    selection = primaryButton.gameObject;
                }
            
                if (options)
                {
                    selection = optionsBackButton.gameObject;
                }
            
                EventSystem.current.SetSelectedGameObject(selection);
            }
        }

        public void CloseModal(bool restoreSelection = true)
        {
            modalVisible = false;
            modalGroup.interactable = false;
            modalGroup.blocksRaycasts = false;
            StartFade(ref modalFade, modalGroup, false);
            menu.interactable = menuVisible && menu.alpha >= 1f;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(restoreSelection && menuVisible ? previousSelection : null);
            }
        }

        public void SetQuitLayout(bool quitting)
        {
            modalPanel.anchoredPosition = quitting ? quitModalPosition : modalPosition;
        }

        public void FocusMenu()
        {
            if (EventSystem.current != null && buttons.Length > 0)
            {
                EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
            }
        }
    }
}
