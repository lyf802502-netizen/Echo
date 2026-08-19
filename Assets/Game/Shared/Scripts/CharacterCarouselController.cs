using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared
{
    /// <summary>
    /// 角色横向轮播的视觉控制器。
    /// 本脚本不再负责“松手吸附”：列表的移动完全交由 ScrollRect 的拖动与惯性处理。
    /// 它只负责将靠近屏幕中线的角色卡实时放大、变亮，远离中线的角色卡缩小、变暗。
    /// </summary>
    public class CharacterCarouselController : MonoBehaviour
    {
        [Header("必要引用")]
        [Tooltip("CharacterScrollView 上的 ScrollRect。它负责鼠标拖动、触摸滑动和惯性减速。")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("角色的可视区域。此区域局部坐标的 X = 0 即为屏幕视觉中心。")]
        [SerializeField] private RectTransform viewport;

        [Tooltip("承载全部角色卡的 Content。留空时自动使用 ScrollRect 的 Content。")]
        [SerializeField] private RectTransform content;

        [Tooltip("角色卡列表。留空时自动读取 Content 的所有直接子物体。")]
        [SerializeField] private List<RectTransform> cards = new();

        [Header("初始显示")]
        [Tooltip("场景第一次打开时位于中线的角色卡索引。索引从 0 开始：0 是第一张、1 是第二张。")]
        [SerializeField, Min(0)] private int initialCenteredIndex = 0;

        [Header("中线焦点效果")]
        [Tooltip("卡片刚好位于屏幕中线时的缩放。建议略大于 1，例如 1.15。")]
        [SerializeField, Range(0.5f, 1.5f)] private float focusedScale = 1.15f;

        [Tooltip("卡片远离屏幕中线时的最小缩放。")]
        [SerializeField, Range(0.2f, 1f)] private float sideScale = 0.75f;

        [Tooltip("卡片距离中线达到该比例后，视为已经处于两侧状态。数值越小，大小变化越明显。")]
        [SerializeField, Range(0.1f, 1f)] private float sideDistanceRatio = 0.45f;

        [Tooltip("两侧角色卡的最低透明度。")]
        [SerializeField, Range(0.1f, 1f)] private float sideAlpha = 0.5f;

        [Tooltip("两侧角色卡的亮度倍率。1 为原始颜色，0.5 约等于原始颜色的一半亮度。")]
        [SerializeField, Range(0.1f, 1f)] private float sideBrightness = 0.55f;

        // 缓存每张角色卡根节点的 Image 和原始颜色，才能在运行中亮化后恢复美术原色。
        private readonly List<Image> cardImages = new();
        private readonly List<Color> originalImageColors = new();
        private readonly List<CanvasGroup> cardCanvasGroups = new();

        private void Awake()
        {
            scrollRect ??= GetComponent<ScrollRect>();

            if (scrollRect == null)
            {
                Debug.LogError("[CharacterCarousel] 找不到 ScrollRect。请将脚本挂到 CharacterScrollView 上。", this);
                enabled = false;
                return;
            }

            viewport ??= scrollRect.viewport;
            content ??= scrollRect.content;

            if (viewport == null || content == null)
            {
                Debug.LogError("[CharacterCarousel] 请在 Inspector 中绑定 Viewport 与 Content。", this);
                enabled = false;
                return;
            }

            // 不手动填写 Cards 时，Content 下的直接子物体就是全部角色卡。
            if (cards.Count == 0)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    if (content.GetChild(i) is RectTransform card)
                    {
                        cards.Add(card);
                    }
                }
            }

            CacheCardComponents();
        }

        private IEnumerator Start()
        {
            if (cards.Count == 0)
            {
                Debug.LogWarning("[CharacterCarousel] Content 下没有角色卡。", this);
                yield break;
            }

            // 等待一帧，确保 HorizontalLayoutGroup 和 ContentSizeFitter 已计算出最终卡片位置。
            // 这只是初始定位，不会在玩家松手后产生吸附效果。
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            CenterCardImmediately(initialCenteredIndex);
        }

        private void Update()
        {
            UpdateCardVisuals();
        }

        /// <summary>
        /// 收集运行时需要修改的组件。每张角色卡根节点必须保留 Button 自带的 Image。
        /// </summary>
        private void CacheCardComponents()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform card = cards[i];
                if (card == null)
                {
                    continue;
                }

                Image image = card.GetComponent<Image>();
                cardImages.Add(image);
                originalImageColors.Add(image != null ? image.color : Color.white);

                CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = card.gameObject.AddComponent<CanvasGroup>();
                }

                cardCanvasGroups.Add(canvasGroup);
            }
        }

        /// <summary>
        /// 仅在场景打开时将一张卡对准中线。之后不再调用，因此不会形成吸附感。
        /// </summary>
        private void CenterCardImmediately(int index)
        {
            int clampedIndex = Mathf.Clamp(index, 0, cards.Count - 1);
            RectTransform card = cards[clampedIndex];

            if (card == null)
            {
                return;
            }

            // 卡片在 Viewport 局部坐标中的 X 偏移，就是 Content 需要反向移动的距离。
            float offsetFromCenter = viewport.InverseTransformPoint(card.position).x;
            Vector2 position = content.anchoredPosition;
            position.x -= offsetFromCenter;
            content.anchoredPosition = position;

            // 清除可能存在的旧速度，防止 ScrollRect 在初始定位后继续惯性移动。
            scrollRect.StopMovement();
        }

        /// <summary>
        /// 实时计算每张卡距中线的距离。计算结果不是离散的“选中/未选中”，
        /// 而是 0 到 1 的连续值，所以拖动过程中会得到平滑的大小和亮度过渡。
        /// </summary>
        private void UpdateCardVisuals()
        {
            float maxDistance = Mathf.Max(1f, viewport.rect.width * sideDistanceRatio);

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || i >= cardCanvasGroups.Count)
                {
                    continue;
                }

                // 1. 动态计算 Viewport 真正的中心点 X 坐标（无论 UI 的 Pivot 怎么设置，都能精准找准中心）
                float viewportCenterX = (0.5f - viewport.pivot.x) * viewport.rect.width;

                // 2. 计算卡片当前位置与“真实中心”的距离
                float distance = Mathf.Abs(viewport.InverseTransformPoint(cards[i].position).x - viewportCenterX);

                // distance = 0 时 focus 为 1；达到 maxDistance 后 focus 为 0。
                float focus = 1f - Mathf.Clamp01(distance / maxDistance);

                // 使用 SmoothStep 消除线性变化带来的突兀感，使视觉过渡更接近 Cytus 的浏览体验。
                focus = Mathf.SmoothStep(0f, 1f, focus);

                float scale = Mathf.Lerp(sideScale, focusedScale, focus);
                cards[i].localScale = Vector3.one * scale;

                cardCanvasGroups[i].alpha = Mathf.Lerp(sideAlpha, 1f, focus);

                // 保留美术设置的原始颜色，只改变其亮度，避免把不同角色卡强行变成同一种颜色。
                if (i < cardImages.Count && cardImages[i] != null && i < originalImageColors.Count)
                {
                    Color originalColor = originalImageColors[i];
                    float brightness = Mathf.Lerp(sideBrightness, 1f, focus);
                    cardImages[i].color = new Color(
                        originalColor.r * brightness,
                        originalColor.g * brightness,
                        originalColor.b * brightness,
                        originalColor.a);
                }
            }
        }
    }
}
