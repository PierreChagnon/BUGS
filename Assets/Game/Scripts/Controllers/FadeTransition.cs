using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeTransition : MonoBehaviour
{
    public static FadeTransition Instance { get; private set; }

    [SerializeField] private float _defaultDuration = 0.2f;

    CanvasGroup _canvasGroup;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
    }

    public IEnumerator FadeOut(float? duration = null)
    {
        EnsureOverlay();
        yield return FadeTo(1f, duration ?? _defaultDuration);
    }

    public IEnumerator FadeIn(float? duration = null)
    {
        EnsureOverlay();
        yield return FadeTo(0f, duration ?? _defaultDuration);
    }

    void EnsureOverlay()
    {
        if (_canvasGroup != null)
            return;

        var canvasObject = new GameObject("FadeOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var imageObject = new GameObject("FadeOverlay");
        imageObject.transform.SetParent(canvasObject.transform, false);

        var rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var image = imageObject.AddComponent<Image>();
        image.color = Color.black;

        _canvasGroup = imageObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }
}
