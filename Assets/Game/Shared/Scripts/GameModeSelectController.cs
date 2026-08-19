using System.Collections;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 控制模式选择界面的按钮入场动画与场景跳转。
/// </summary>
public class GameModeSelectController : MonoBehaviour
{
    [Header("模式按钮")]
    [SerializeField] private Button storyModeButton;
    [SerializeField] private Button musicGameModeButton;

    [Header("入场动画")]
    [SerializeField, Min(0.1f)] private float enterDuration = 1.15f;
    [SerializeField, Min(0f)] private float extraOutsideDistance = 80f;

    [Header("目标场景")]
    [SerializeField] private string storyModeSceneName = "VNMainMenu";
    [SerializeField] private string musicGameModeSceneName = "Game";

    private RectTransform storyModeRect;
    private RectTransform musicGameModeRect;
    private Vector2 storyModeTargetPosition;
    private Vector2 musicGameModeTargetPosition;
    private Tween storyModeTween;
    private Tween musicGameModeTween;
    private bool isLoadingScene;

    private void Awake()
    {
        // 未手动绑定时，按当前场景中的对象名称自动查找。
        storyModeButton ??= FindButton("StoryModeButton");
        musicGameModeButton ??= FindButton("MusicGameModeButton");

        if (storyModeButton == null || musicGameModeButton == null)
        {
            Debug.LogError("[GameModeSelect] 找不到模式按钮，请在 Inspector 中绑定两个 Button。", this);
            enabled = false;
            return;
        }

        storyModeRect = storyModeButton.GetComponent<RectTransform>();
        musicGameModeRect = musicGameModeButton.GetComponent<RectTransform>();

        // 记录美术布局中设置好的最终拼合位置。
        storyModeTargetPosition = storyModeRect.anchoredPosition;
        musicGameModeTargetPosition = musicGameModeRect.anchoredPosition;

        // 动画播放期间不允许点击，避免尚未到位就发生场景跳转。
        storyModeButton.interactable = false;
        musicGameModeButton.interactable = false;

        storyModeButton.onClick.AddListener(EnterStoryMode);
        musicGameModeButton.onClick.AddListener(EnterMusicGameMode);

        MoveButtonsOutsideScreen();
    }

    private void Start()
    {
        StartCoroutine(PlayEnterAnimation());
    }

    private void OnDisable()
    {
        // 场景切换或对象禁用时停止未完成的动画。
        if (storyModeTween.isAlive)
        {
            storyModeTween.Stop();
        }

        if (musicGameModeTween.isAlive)
        {
            musicGameModeTween.Stop();
        }
    }

    private Button FindButton(string buttonName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    private void MoveButtonsOutsideScreen()
    {
        RectTransform canvasRect = transform as RectTransform;
        float canvasHeight = canvasRect != null ? canvasRect.rect.height : Screen.height;

        // 画布高度加上按钮自身高度，确保不同分辨率下按钮都完全位于屏幕外。
        float storyOffset = canvasHeight + storyModeRect.rect.height + extraOutsideDistance;
        float musicGameOffset = canvasHeight + musicGameModeRect.rect.height + extraOutsideDistance;

        storyModeRect.anchoredPosition = storyModeTargetPosition + Vector2.up * storyOffset;
        musicGameModeRect.anchoredPosition = musicGameModeTargetPosition + Vector2.down * musicGameOffset;
    }

    private IEnumerator PlayEnterAnimation()
    {
        // OutExpo 的节奏为前快后慢，形成从上下飞入后缓慢停稳的感觉。
        storyModeTween = Tween.Custom(
            storyModeRect.anchoredPosition,
            storyModeTargetPosition,
            enterDuration,
            position => storyModeRect.anchoredPosition = position,
            ease: Easing.Overshoot(1.0f));

        musicGameModeTween = Tween.Custom(
            musicGameModeRect.anchoredPosition,
            musicGameModeTargetPosition,
            enterDuration,
            position => musicGameModeRect.anchoredPosition = position,
            ease: Easing.Overshoot(1.0f));

        // 两个按钮同时开始且时长相同，等待其中一个完成即可。
        yield return storyModeTween.ToYieldInstruction();

        storyModeRect.anchoredPosition = storyModeTargetPosition;
        musicGameModeRect.anchoredPosition = musicGameModeTargetPosition;
        storyModeButton.interactable = true;
        musicGameModeButton.interactable = true;
    }

    private void EnterStoryMode()
    {
        LoadScene(storyModeSceneName);
    }

    private void EnterMusicGameMode()
    {
        LoadScene(musicGameModeSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (isLoadingScene || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        isLoadingScene = true;
        SceneManager.LoadScene(sceneName);
    }
}
