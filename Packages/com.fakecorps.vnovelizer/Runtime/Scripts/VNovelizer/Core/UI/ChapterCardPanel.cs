using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VNovelizer.Core.Compat;

/// <summary>
/// Chapter title card displayed during a VN transition.
/// The panel itself fills the canvas; Card is the visual panel that slides.
/// </summary>
public class ChapterCardPanel : BasePanel
{
    [SerializeField] private RectTransform card;
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image overlay;
    [SerializeField] private float transitionDuration = 0.6f;
    [SerializeField] private float overlayDuration = 0.45f;

    private Coroutine playCoroutine;

    protected override void Awake()
    {
        base.Awake();

        if (card == null)
            card = transform.Find("Card") as RectTransform;
        if (chapterText == null)
            chapterText = GetControl<TMP_Text>("ChapterText");
        if (titleText == null)
            titleText = GetControl<TMP_Text>("TitleText");
        if (overlay == null)
            overlay = GetControl<Image>("Overlay");

        // [2026-08-29] 动态加载时先把卡片放到屏幕左侧外，避免显示前闪现一帧。
        ResetCardPosition();
    }

    public override void ShowMe()
    {
        gameObject.SetActive(true);
        ResetCardPosition();
        SetOverlayAlpha(0f);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (overlay == null) 
            return;

        var group = overlay.GetComponent<CanvasGroup>();
        if (group == null) 
            group = overlay.gameObject.AddComponent<CanvasGroup>();

        group.alpha = alpha;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private void ResetCardPosition()
    {
        if (card == null) return;

        float cardWidth = card.rect.width;
        float parentWidth = (card.parent as RectTransform)?.rect.width ?? Screen.width;
        Vector2 position = card.anchoredPosition;
        position.x = -(parentWidth + cardWidth) * 0.5f;
        card.anchoredPosition = position;
    }

    public override void HideMe()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        gameObject.SetActive(false);
    }

    public void SetContent(string chapter, string title)
    {
        if (chapterText != null) chapterText.text = chapter ?? string.Empty;
        if (titleText != null) titleText.text = title ?? string.Empty;
    }

    public void Play(string chapter, string title, float holdSeconds, Action onComplete = null)
    {
        SetContent(chapter, title);
        ShowMe();

        if (playCoroutine != null)
            StopCoroutine(playCoroutine);

        playCoroutine = StartCoroutine(PlayRoutine(Mathf.Max(0f, holdSeconds), onComplete));
    }

    private IEnumerator PlayRoutine(float holdSeconds, Action onComplete)
    {
        if (card == null)
        {
            Debug.LogError("[ChapterCardPanel] Card RectTransform not found.");
            playCoroutine = null;
            onComplete?.Invoke();
            yield break;
        }

        float cardWidth = card.rect.width;
        float parentWidth = (card.parent as RectTransform)?.rect.width ?? Screen.width;
        float hiddenX = -(parentWidth + cardWidth) * 0.5f;
        Vector2 visiblePosition = new Vector2(0f, card.anchoredPosition.y);
        Vector2 hiddenPosition = new Vector2(hiddenX, card.anchoredPosition.y);

        AnimationCompat.StopAllByTarget(card);
        card.anchoredPosition = hiddenPosition;

        var overlayGroup = overlay != null ? overlay.GetComponent<CanvasGroup>() : null;
        bool completed = false;

        // 黑色遮罩淡入，同时章节卡片从屏幕左侧滑入
        // Group() 方法用于同时播放多个动画
        // Chain() 方法用于在前一个动画完成后再播放下一个动画
        // ChainDelay() 方法用于在前一个动画完成后延迟一段时间再播放下一个动画
        var sequence = AnimationCompat.CreateSequence()
            .Group(AnimationCompat.AnchoredPositionX(card, visiblePosition.x, transitionDuration, Ease.OutQuad));
        if (overlayGroup != null)
            sequence.Group(AnimationCompat.Alpha(overlayGroup, 1f, overlayDuration, Ease.InQuad));

        // 章节卡片停留一段时间后从左侧滑出
        sequence.ChainDelay(holdSeconds)
             .Chain(AnimationCompat.AnchoredPositionX(card, hiddenPosition.x, transitionDuration, Ease.InQuad));
        // 待章节卡片滑出后，再将黑色遮罩淡出
        if (overlayGroup != null)
            sequence.Chain(AnimationCompat.Alpha(overlayGroup, 0f, overlayDuration, Ease.OutQuad));

        sequence.OnComplete(() => completed = true);

        yield return new WaitUntil(() => completed);

        playCoroutine = null;
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}
