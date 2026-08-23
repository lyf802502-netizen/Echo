using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterNode : MonoBehaviour
{
    [Header("章节数据")]
    [SerializeField] private string chapterId = "CH01";
    [SerializeField] private string chapterTitle = "第一章";
    [SerializeField] private string scriptFileName = "Echo_Chapter1";
    [SerializeField] private string startLineId = "1";

    [Header("节点 UI")]
    [SerializeField] private Button chapterButton;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        if (chapterButton == null)
            chapterButton = GetComponent<Button>();

        if (statusText == null && transform.parent != null)
            statusText = transform.parent.Find("Chapter01Status")?.GetComponent<TMP_Text>();

        if (chapterButton != null)
            chapterButton.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (string.IsNullOrWhiteSpace(chapterId))
            return;

        bool isUnlocked = GlobalDataManager.GetInstance().IsChapterUnlocked(chapterId);
        bool isCompleted = GlobalDataManager.GetInstance().IsChapterCompleted(chapterId);

        if (chapterButton != null)
            chapterButton.interactable = isUnlocked;

        if (statusText != null)
        {
            if (isCompleted)
                statusText.text = chapterTitle + "\n\n已完成";
            else if (isUnlocked)
                statusText.text = chapterTitle + "\n\n开始";
            else
                statusText.text = chapterTitle + "\n\n未解锁";
        }
    }

    private void OnClick()
    {
        if (!GlobalDataManager.GetInstance().IsChapterUnlocked(chapterId))
            return;

        UIManager.GetInstance().HidePanel("ChapterSelectPanel");
        VNManager.GetInstance().StartGame(scriptFileName, startLineId);
    }

    private void OnDestroy()
    {
        if (chapterButton != null)
            chapterButton.onClick.RemoveListener(OnClick);
    }
}
