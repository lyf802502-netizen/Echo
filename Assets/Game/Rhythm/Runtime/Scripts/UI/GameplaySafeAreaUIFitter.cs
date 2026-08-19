using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class GameplaySafeAreaUIFitter : MonoBehaviour
{
    [Header("Gameplay Safe Area")]
    [SerializeField] private Vector2 designResolution = new Vector2(1024f, 768f);

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplyLayout();
    }

    private void OnEnable()
    {
        ApplyLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

    private void OnValidate()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (rectTransform == null || parentRect == null || designResolution.x <= 0f || designResolution.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(
            parentRect.rect.width / designResolution.x,
            parentRect.rect.height / designResolution.y);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = designResolution;
        rectTransform.localScale = Vector3.one * scale;
    }
}
