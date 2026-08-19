using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using PrimeTween;

public class ConfirmPanel : BasePanel
{
    [SerializeField]private TextMeshProUGUI messageText;
    [SerializeField]private Button yesBtn;
    [SerializeField]private Button noBtn;

    private UnityAction onConfirmCallback;
    private UnityAction onCancelCallback;
    private UnityAction onOkCallback;

    private RectTransform panelRect;
    private Vector2 panelTargetPosition;

    private const float panelEnterOffset = 700f;
    private const float enterDuration = 0.5f;

    protected override void Awake()
    {
        base.Awake();
        messageText = GetControl<TextMeshProUGUI>("Message");
        yesBtn = GetControl<Button>("Yes");
        noBtn = GetControl<Button>("No");

        panelRect = GetComponent<RectTransform>();
        panelTargetPosition = panelRect.anchoredPosition;

        //面板处于初始状态时，其上的按钮不允许被点击
        yesBtn.interactable = false;
        noBtn.interactable = false;

        yesBtn.onClick.AddListener(OnYesClick);
        noBtn.onClick.AddListener(OnNoClick);
    }

    public override void ShowMe()
    {
        PlayEnterAnimation();
    }

    /// <summary>
    /// 显示弹窗
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">内容</param>
    /// <param name="onConfirm">点击确定的回调</param>
    /// <param name="onCancel">点击取消的回调(可选)</param>
    public void Show(string title, string message, UnityAction onConfirm, UnityAction onCancel = null)
    {

        messageText.text = message;
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        ShowMe();
    }

    private void OnYesClick()
    {
        onConfirmCallback?.Invoke();
        ClosePanel();
    }

    private void OnNoClick()
    {
        //点击 No 按钮之后面板上两个按钮立刻失活
        yesBtn.interactable = false;
        noBtn.interactable = false;

        PlayExitAnimation();
    }

    private void ClosePanel()
    {
        UIManager.GetInstance().HidePanel("ConfirmPanel");
    }

    private void PlayEnterAnimation()
    {
        Tween.StopAll(panelRect); // 停止正在进行的动画，确保不会有冲突

        Vector2 startPosition = panelTargetPosition + Vector2.down * panelEnterOffset;
        panelRect.anchoredPosition = startPosition;

        Tween.UIAnchoredPosition(panelRect, panelTargetPosition, enterDuration, Ease.OutBack, useUnscaledTime: true)
            .OnComplete(() =>
            {
                yesBtn.interactable = true;
                noBtn.interactable = true;
            }); //面板移动过程中其上的按钮不允许被点击，直到动画完成后才允许点击
    }

    private void PlayExitAnimation()
    {
        Tween.StopAll(panelRect); // 停止正在进行的动画，确保不会有冲突

        Vector2 endPosition = panelTargetPosition + Vector2.down * panelEnterOffset;

        Tween.UIAnchoredPosition(panelRect, endPosition, enterDuration, Ease.InBack, useUnscaledTime: true)
            .OnComplete(() =>
            {
                onCancelCallback?.Invoke();
                ClosePanel(); //面板退出场景后隐藏
            }); 
    }
}