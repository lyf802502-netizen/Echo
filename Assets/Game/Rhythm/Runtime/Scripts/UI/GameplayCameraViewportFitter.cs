using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class GameplayCameraViewportFitter : MonoBehaviour
{
    [Header("Gameplay Safe Area")]
    [SerializeField] private Vector2 designResolution = new Vector2(1024f, 768f);

    private Camera cachedCamera;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        ApplyViewport();
    }

    private void OnEnable()
    {
        ApplyViewport();
    }

    private void LateUpdate()
    {
        ApplyViewport();
    }

    private void OnDisable()
    {
        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        if (cachedCamera != null)
        {
            cachedCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    private void OnValidate()
    {
        ApplyViewport();
    }

    private void ApplyViewport()
    {
        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        if (cachedCamera == null || designResolution.x <= 0f || designResolution.y <= 0f)
        {
            return;
        }

        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        float targetAspect = designResolution.x / designResolution.y;
        float currentAspect = screenWidth / screenHeight;

        if (currentAspect > targetAspect)
        {
            float normalizedWidth = targetAspect / currentAspect;
            float xOffset = (1f - normalizedWidth) * 0.5f;
            cachedCamera.rect = new Rect(xOffset, 0f, normalizedWidth, 1f);
            return;
        }

        float normalizedHeight = currentAspect / targetAspect;
        float yOffset = (1f - normalizedHeight) * 0.5f;
        cachedCamera.rect = new Rect(0f, yOffset, 1f, normalizedHeight);
    }
}
