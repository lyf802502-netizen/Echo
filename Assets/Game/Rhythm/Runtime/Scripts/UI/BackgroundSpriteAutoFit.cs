using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundSpriteAutoFit : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private string fallbackCameraName = "BackgroundCamera";

    [Header("Scale")]
    [SerializeField] private Vector3 baseLocalScale = Vector3.one;
    [SerializeField] private bool updateInEditMode = true;

    private SpriteRenderer cachedSpriteRenderer;

    private void Reset()
    {
        cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        targetCamera = FindTargetCamera();
        baseLocalScale = transform.localScale;
        FitToCamera();
    }

    private void Awake()
    {
        CacheReferences();
        FitToCamera();
    }

    private void OnEnable()
    {
        CacheReferences();
        FitToCamera();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying && !updateInEditMode)
        {
            return;
        }

        FitToCamera();
    }

    private void OnValidate()
    {
        CacheReferences();

        if (baseLocalScale == Vector3.zero)
        {
            baseLocalScale = Vector3.one;
        }

        FitToCamera();
    }

    public void FitToCamera()
    {
        CacheReferences();

        Camera fitCamera = targetCamera != null ? targetCamera : FindTargetCamera();
        if (fitCamera == null || cachedSpriteRenderer == null || cachedSpriteRenderer.sprite == null)
        {
            return;
        }

        Vector2 visibleSize = GetCameraVisibleSize(fitCamera, transform.position);
        Vector2 spriteSize = cachedSpriteRenderer.sprite.bounds.size;

        if (visibleSize.x <= 0f || visibleSize.y <= 0f || spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float fitScale = Mathf.Max(visibleSize.x / spriteSize.x, visibleSize.y / spriteSize.y);

        Vector3 newScale = new Vector3(
            GetScaledAxis(baseLocalScale.x, fitScale),
            GetScaledAxis(baseLocalScale.y, fitScale),
            Mathf.Approximately(baseLocalScale.z, 0f) ? transform.localScale.z : baseLocalScale.z);

        transform.localScale = newScale;
    }

    private void CacheReferences()
    {
        if (cachedSpriteRenderer == null)
        {
            cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private Camera FindTargetCamera()
    {
        if (!string.IsNullOrWhiteSpace(fallbackCameraName))
        {
            Camera[] camerasByName = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < camerasByName.Length; i++)
            {
                Camera cameraCandidate = camerasByName[i];
                if (cameraCandidate != null && cameraCandidate.name == fallbackCameraName)
                {
                    return cameraCandidate;
                }
            }
        }

        int layerMask = 1 << gameObject.layer;
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cameraCandidate = cameras[i];
            if (cameraCandidate == null || !cameraCandidate.enabled)
            {
                continue;
            }

            if ((cameraCandidate.cullingMask & layerMask) != 0)
            {
                return cameraCandidate;
            }
        }

        return null;
    }

    private static Vector2 GetCameraVisibleSize(Camera fitCamera, Vector3 targetPosition)
    {
        if (fitCamera.orthographic)
        {
            float height = fitCamera.orthographicSize * 2f;
            return new Vector2(height * fitCamera.aspect, height);
        }

        float distance = Mathf.Abs(Vector3.Dot(targetPosition - fitCamera.transform.position, fitCamera.transform.forward));
        distance = Mathf.Max(distance, 0.01f);
        float halfFov = fitCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float perspectiveHeight = 2f * distance * Mathf.Tan(halfFov);
        return new Vector2(perspectiveHeight * fitCamera.aspect, perspectiveHeight);
    }

    private static float GetScaledAxis(float baseAxisScale, float fitScale)
    {
        float sign = Mathf.Approximately(baseAxisScale, 0f) ? 1f : Mathf.Sign(baseAxisScale);
        float magnitude = Mathf.Max(Mathf.Abs(baseAxisScale), 0.0001f);
        return sign * magnitude * fitScale;
    }
}
