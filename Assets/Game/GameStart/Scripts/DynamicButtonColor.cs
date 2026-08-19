using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PrimeTween;

public class DynamicButtonColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("按钮图片")]
    public Image buttonImage;

    [Header("颜色设置")]
    public Color pinkColor = new Color(1f, 0.75f, 0.8f);      // 粉红
    public Color purpleColor = new Color(0.6f, 0.2f, 0.8f);   // 紫色
    public Color blueColor = Color.blue;                      // 蓝色
    public Color greenColor = Color.green;                    // 绿色
    public Color lightBlueColor = new Color(0.68f, 0.85f, 0.9f); // 浅蓝色

    [Header("动画时间")]
    public float idleFadeDuration = 1.5f; // 待机状态下，每种颜色切换花费的时间
    public float interactDuration = 0.2f; // 鼠标交互时，颜色切换的速度

    private Sequence idleSequence;
    private bool isHovering = false;
    private bool isPressed = false;

    // Start is called before the first frame update
    void Start()
    {
        if(buttonImage == null)
            buttonImage = GetComponent<Image>();

        buttonImage.color = pinkColor; // 初始颜色为粉红色

        PlayIdleAnimation();
    }

    /// <summary>
    /// 播放按钮待机动画，循环切换颜色
    /// </summary>
    private void PlayIdleAnimation()
    {
        // 播放新动画前，先停止可能正在运行的旧动画，防止冲突
        idleSequence.Stop();

        // 创建一个无限循环 (-1) 的动画序列
        idleSequence = Sequence.Create(cycles: -1)
            // 粉红变紫色
            .Chain(Tween.Color(buttonImage, purpleColor, idleFadeDuration, ease: Ease.Linear))
            // 紫色变蓝色
            .Chain(Tween.Color(buttonImage, blueColor, idleFadeDuration, ease: Ease.Linear))
            // 蓝色变回粉红色，完成闭环
            .Chain(Tween.Color(buttonImage, pinkColor, idleFadeDuration, ease: Ease.Linear));
    }

    // --- 下面是鼠标交互事件的监听 ---

    // 1. 鼠标悬停 (Hover)
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (!isPressed) // 只有在没按下的情况下，悬停才显示浅蓝色
        {
            idleSequence.Stop(); // 打断待机动画
            Tween.Color(buttonImage, lightBlueColor, interactDuration);
        }
    }

    // 2. 鼠标移出 (Exit)
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (!isPressed)
        {
            // 鼠标移出且没有按下时，恢复待机循环动画
            PlayIdleAnimation();
        }
    }

    // 3. 鼠标按下 (Press)
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        idleSequence.Stop(); // 确保打断待机动画
        Tween.Color(buttonImage, pinkColor, interactDuration); // 变成粉色
    }

    // 4. 鼠标抬起 (Release)
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        // 抬起鼠标时，需要判断鼠标当前是否还在按钮上
        if (isHovering)
        {
            Tween.Color(buttonImage, lightBlueColor, interactDuration); // 还在按钮上，恢复悬停的浅蓝色
        }
        else
        {
            //PlayIdleAnimation(); // 已经移出按钮，恢复待机动画
        }
    }
}
