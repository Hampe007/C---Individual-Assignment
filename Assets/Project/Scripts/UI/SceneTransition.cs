using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTransition : MonoBehaviour
{
    private const float FadeDuration = 0.8f;
    private const float MaximumFadeDelta = 1f / 30f;
    private const int OverlaySortingOrder = short.MaxValue;

    private static SceneTransition _instance;

    private CanvasGroup _canvasGroup;
    private bool _isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (_instance != null)
            return;

        GameObject transitionObject = new(nameof(SceneTransition));
        DontDestroyOnLoad(transitionObject);

        _instance = transitionObject.AddComponent<SceneTransition>();
        _instance.CreateOverlay();
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("A scene name is required for a scene transition.");
            return;
        }

        if (_instance == null)
            Create();

        _instance.BeginLoad(sceneName);
    }

    private void BeginLoad(string sceneName)
    {
        if (_isTransitioning)
            return;

        StartCoroutine(LoadSceneWithFade(sceneName));
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        _isTransitioning = true;
        _canvasGroup.blocksRaycasts = true;

        yield return FadeTo(1f);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!loadOperation.isDone)
            yield return null;

        // Let the new scene render once while the screen is still black.
        // This also avoids using the scene-load frame's large delta in the fade.
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        yield return FadeTo(0f);

        _canvasGroup.blocksRaycasts = false;
        _isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < FadeDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDelta);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / FadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }

    private void CreateOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortingOrder;

        gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        GameObject fadeObject = new("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        fadeObject.transform.SetParent(transform, false);

        RectTransform fadeTransform = fadeObject.GetComponent<RectTransform>();
        fadeTransform.anchorMin = Vector2.zero;
        fadeTransform.anchorMax = Vector2.one;
        fadeTransform.offsetMin = Vector2.zero;
        fadeTransform.offsetMax = Vector2.zero;

        UnityEngine.UI.Image fadeImage = fadeObject.GetComponent<UnityEngine.UI.Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;
    }
}
