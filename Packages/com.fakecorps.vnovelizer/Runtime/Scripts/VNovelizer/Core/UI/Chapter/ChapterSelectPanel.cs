using UnityEngine;
using UnityEngine.UI;

public class ChapterSelectPanel : BasePanel
{
    private Button closeButton;
    private ChapterNode[] chapterNodes;

    protected override void Awake()
    {
        base.Awake();

        closeButton = GetControl<Button>("CloseBtn");
        chapterNodes = GetComponentsInChildren<ChapterNode>(true);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClick);
        else
            Debug.LogError("[ChapterSelectPanel] 找不到 CloseBtn");

        if (chapterNodes == null || chapterNodes.Length == 0)
            Debug.LogError("[ChapterSelectPanel] 没有找到任何 ChapterNode 组件");
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (chapterNodes == null)
            return;

        foreach (ChapterNode node in chapterNodes)
        {
            if (node != null)
                node.Refresh();
        }
    }

    private void OnCloseButtonClick()
    {
        UIManager.GetInstance().HidePanel("ChapterSelectPanel");
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseButtonClick);
    }
}
