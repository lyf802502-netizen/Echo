using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 历史记录面板 (优化版)
/// </summary>
public class HistoryPanel : BasePanel
{
    // [2026-08-31] 今日修复说明：历史记录打开时需等待异步条目和布局完成，再定位到底部。
    // [2026-08-31] 同时使用 CanvasGroup 隐藏刷新过程，避免玩家看到内容从顶部跳到底部。
    // [2026-08-31] 鼠标滚轮交由 Unity ScrollRect 处理，避免与 Elastic 模式重复滚动。
    // UI组件
    [SerializeField] private Button closeButton;
    private ScrollRect historyScrollView;
    private RectTransform contentTransform;
    private CanvasGroup panelCanvasGroup;

    // 记录当前正在显示的 Item 列表，以便回收
    private List<GameObject> activeItems = new List<GameObject>();

    // 预制体加载路径 (作为对象池的 Key)
    private string itemResPath;
    private int refreshVersion;
    // [2026-08-31] 每次刷新使用独立批次编号，旧批次回调不得修改新面板。
    private readonly Dictionary<int, int> pendingItemLoads = new Dictionary<int, int>();
    private Coroutine scrollCoroutine;

    protected override void Awake()
    {
        base.Awake();

        //获取组件
        closeButton = GetControl<Button>("H_Close");
        historyScrollView = GetControl<ScrollRect>("H_Scroll View");
        panelCanvasGroup = GetComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClick);
        else
            Debug.LogError("[HistoryPanel] 找不到 Close Button (H_Close)!");

        if (historyScrollView != null)
            contentTransform = historyScrollView.content;
        else
            Debug.LogError("[HistoryPanel] 找不到 ScrollRect (H_Scroll View)!");

        itemResPath = VNProjectConfig.Instance.UI_HistoryPath + "/HistoryItem";
        Debug.Log($"[HistoryPanel] 预制体加载路径: {itemResPath}");
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        // 每次打开面板时设置状态
        GameStateManager.GetInstance().SetState(GameState.History);
        
        // 每次打开面板时刷新数据；滚动定位必须等异步条目和布局都完成。
        // [2026-08-31] 先隐藏面板，直到条目生成、布局刷新和滚动定位全部完成。
        int currentVersion = ++refreshVersion;
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
        }
        RefreshHistoryDisplay(currentVersion);

        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);
        scrollCoroutine = StartCoroutine(ScrollToBottom(currentVersion));
    }

    /// <summary>
    /// 刷新历史记录显示
    /// </summary>
    private void RefreshHistoryDisplay(int currentVersion)
    {
        if (contentTransform == null) return;

        // [2026-08-31] 记录本批次待完成的异步加载数量，ScrollToBottom 会等待它归零。
        pendingItemLoads[currentVersion] = 0;

        // 1. 回收旧对象到对象池
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            GameObject item = activeItems[i];
            // 推回对象池，Key 为资源路径
            PoolManager.GetInstance().PushObj(itemResPath, item);
        }
        activeItems.Clear();

        // 2. 从 GlobalDataManager 获取所有历史数据
        List<HistoryEntry> logs = GlobalDataManager.GetInstance().GetHistoryLog();
        if (logs == null || logs.Count == 0)
        {
            Debug.Log("[HistoryPanel] 读取到 0 条历史记录");
            return;
        }
        Debug.Log($"[HistoryPanel] 读取到 {logs.Count} 条历史记录");

        // 3. 生成新条目
        for (int i = 0; i < logs.Count; i++)
        {
            HistoryEntry current = logs[i];
            HistoryEntry prev = (i > 0) ? logs[i - 1] : null;

            // 从对象池获取对象 (异步/同步)
            pendingItemLoads[currentVersion]++;
            PoolManager.GetInstance().GetObj(itemResPath, (obj) =>
            {
                if (pendingItemLoads.ContainsKey(currentVersion))
                    pendingItemLoads[currentVersion]--;

                // 面板已重新刷新或关闭时，丢弃旧批次的异步结果。
                // [2026-08-31] 面板关闭或重新刷新后，旧回调只归还对象，不得写入当前 Content。
                if (currentVersion != refreshVersion || !isActiveAndEnabled)
                {
                    if (obj != null)
                        PoolManager.GetInstance().PushObj(itemResPath, obj);
                    return;
                }

                if (obj == null)
                    return;

                // 初始化 Item
                SetupHistoryItem(obj, current, prev);

                // 加入活跃列表
                activeItems.Add(obj);
            });
        }
    }

    /// <summary>
    /// 设置单个历史条目的显示内容
    /// </summary>
    private void SetupHistoryItem(GameObject itemObj, HistoryEntry entry, HistoryEntry prevEntry)
    {
        // 设置父物体
        itemObj.transform.SetParent(contentTransform);

        // 重置变换属性 (非常重要，对象池取出来的可能会乱)
        itemObj.transform.localScale = Vector3.one;
        itemObj.transform.localPosition = new Vector3(itemObj.transform.localPosition.x, itemObj.transform.localPosition.y, 0);
        itemObj.transform.localRotation = Quaternion.identity;

        // 查找子组件 (根据你的Prefab层级结构)
        Transform speakerBox = itemObj.transform.Find("H_SpeakerBox");
        // 注意：这里用 GetControl<TMP_Text> 可能找不到子物体的组件，建议直接 GetComponent
        TMP_Text speakerText = speakerBox.Find("H_SpeakerText").GetComponent<TMP_Text>();

        Transform contentTrans = itemObj.transform.Find("H_Content");
        TMP_Text dialogueText = contentTrans.Find("H_DialogueBox/H_Dialogue").GetComponent<TMP_Text>();
        Button replayButton = contentTrans.Find("H_Replay").GetComponent<Button>();

        // [2026-08-21] 历史记录按当前条目的说话人独立决定姓名框，不能复用上一条记录的显示状态。
        // 空姓名和 "hide" 表示旁白/隐藏说话人；对象池复用时同时清空残留姓名文本。
        bool hasSpeaker = !string.IsNullOrWhiteSpace(entry.Speaker) &&
                          !string.Equals(entry.Speaker.Trim(), "hide", System.StringComparison.OrdinalIgnoreCase);

        if (!hasSpeaker)
        {
            speakerBox.gameObject.SetActive(false);
            speakerText.text = string.Empty;
        }
        else
        {
            speakerBox.gameObject.SetActive(true);
            speakerText.text = entry.Speaker;
        }

        // 填充对话内容
        // Pooled history items may keep stale TMP settings, so force wrapping every time.
        dialogueText.enableWordWrapping = true;
        dialogueText.overflowMode = TextOverflowModes.Overflow;
        dialogueText.text = entry.Text;

        // 处理 Replay 按钮 
        // 先移除旧的监听器，防止复用时点击一次触发多次
        replayButton.onClick.RemoveAllListeners();

        if (!string.IsNullOrEmpty(entry.VoiceID))
        {
            replayButton.gameObject.SetActive(true);
            replayButton.onClick.AddListener(() => {
                VoiceManager.GetInstance().PlayVoice(entry.VoiceID);
            });
        }
        else
        {
            replayButton.gameObject.SetActive(false);
        }

        // 强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemObj.GetComponent<RectTransform>());
    }

    /// <summary>
    /// 滚动到底部
    /// </summary>
    private IEnumerator ScrollToBottom(int currentVersion)
    {
        // [2026-08-31] 等待所有条目加载完成，防止 Content 高度继续变化导致底部定位失效。
        // 等待本次刷新发起的所有异步加载回调完成。
        while (currentVersion == refreshVersion && pendingItemLoads.TryGetValue(currentVersion, out int pending) && pending > 0)
            yield return null;

        if (currentVersion != refreshVersion || !isActiveAndEnabled)
            yield break;

        // ContentSizeFitter/VerticalLayoutGroup 可能还需要一个完整帧。
        // [2026-08-31] 在帧末等待布局系统完成，再计算 ScrollRect 的有效滚动范围。
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        if (contentTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform.GetComponent<RectTransform>());

        // ScrollRect 的 verticalNormalizedPosition: 0 = 底部, 1 = 顶部。
        if (historyScrollView != null)
        {
            historyScrollView.StopMovement();
            // [2026-08-31] verticalNormalizedPosition = 0 表示底部，即最新一条历史记录。
            historyScrollView.verticalNormalizedPosition = 0f;
            // 布局组件可能在下一帧再次改写位置，再确认一次。
            yield return null;
            Canvas.ForceUpdateCanvases();
            historyScrollView.verticalNormalizedPosition = 0f;
        }
        // [2026-08-31] 所有内容准备完成后才显示面板，消除打开时从顶部到底部的跳变。
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
        }
        scrollCoroutine = null;
        pendingItemLoads.Remove(currentVersion);
    }

    private void OnCloseButtonClick()
    {
        Debug.Log("我进来了");
        UIManager.GetInstance().HidePanel("HistoryPanel");
        GameStateManager.GetInstance().RestoreState();
    }

    private void OnDestroy()
    {
        // 清理资源
        activeItems.Clear();
        pendingItemLoads.Clear();
        if (GameStateManager.GetInstance() != null && 
            GameStateManager.GetInstance().CurrentState == GameState.History)
        {
            GameStateManager.GetInstance().RestoreState();
            Debug.Log("[HistoryPanel] 面板被Destroy，已恢复游戏状态");
        }
    }
}

/// <summary>
/// 历史记录条目数据结构 (需要放在 HistoryPanel 外面或作为内部类)
/// 注意：使用公共字段而不是属性，以确保LitJson能正确序列化
/// </summary>
[System.Serializable]
public class HistoryEntry
{
    public string Speaker;
    public string Text;
    public string VoiceID;

    public HistoryEntry() { }

    public HistoryEntry(string speaker, string text, string voiceID = null)
    {
        Speaker = speaker;
        Text = text;
        VoiceID = voiceID;
    }
}
