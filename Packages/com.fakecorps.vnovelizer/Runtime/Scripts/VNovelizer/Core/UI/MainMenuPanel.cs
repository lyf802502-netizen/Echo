using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PrimeTween;
using VNovelizer.Core.Commands;

/// <summary>
/// 主界面面板
/// </summary>
public class MainMenuPanel : BasePanel
{
    #region 私有变量
    private bool _isStartingGame = false;

    // Vertical Layout Group 只管理各个 Anim 节点，按钮本身的局部坐标可安全用于播放动画。
    private readonly Dictionary<RectTransform, Vector2> _buttonOriginalPositions = new Dictionary<RectTransform, Vector2>();

    private const float ButtonEnterOffset = 900f;
    private const float ButtonEnterDuration = 0.45f;
    private const float ButtonEnterInterval = 0.12f;
    #endregion

    #region UI控件引用
    // [2026-8-21] 目前主菜单场景中新增按钮：
    // 继续游戏（continueGameBtn）、章节选择（chapterSelectBtn）、返回上一级（backToPreviousBtn）、返回主界面（returnToStartBtn）
    [SerializeField] private Button newGameBtn;
    [SerializeField] private Button continueGameBtn;
    [SerializeField] private Button chapterSelectBtn;
    [SerializeField] private Button loadGameBtn;
    [SerializeField] private Button galleryBtn;
    [SerializeField] private Button settingsBtn;
    [SerializeField] private Button backToPreviousBtn;
    [SerializeField] private Button returnToStartBtn;
    #endregion

    #region 初始化
    protected override void Awake()
    {
        base.Awake();
        
        UIManager.GetInstance().Init();
        
        // 初始化控件
        InitializeControls();
        
        // 绑定事件
        BindEvents();
    }
    
    /// <summary>
    /// 初始化控件
    /// </summary>
    private void InitializeControls()
    {
        newGameBtn = GetControl<Button>("NewGameBtn");
        continueGameBtn = GetControl<Button>("ContinueGameBtn");
        chapterSelectBtn = GetControl<Button>("ChapterSelectBtn");
        loadGameBtn = GetControl<Button>("LoadGameBtn");
        galleryBtn = GetControl<Button>("GalleryBtn");
        settingsBtn = GetControl<Button>("SettingsBtn");
        backToPreviousBtn = GetControl<Button>("BackToPreviousBtn");
        returnToStartBtn = GetControl<Button>("ReturnToStartBtn");

        // 检查关键控件是否存在
        if (newGameBtn == null)
            Debug.LogError("[MainMenuPanel] 找不到 NewGameBtn 按钮！");
        if (continueGameBtn == null)
            Debug.LogError("[MainMenuPanel] 找不到 ContinueGameBtn 按钮！");
        if (chapterSelectBtn == null)
            Debug.LogError("[MainMenuPanel] 找不到 ChapterSelectBtn 按钮！");
        if (loadGameBtn == null)
            Debug.LogError("[MainMenuPanel] 找不到 LoadGameBtn 按钮！");
        if (galleryBtn == null)
            Debug.LogWarning("[MainMenuPanel] 找不到 GalleryBtn 按钮（可选）");
        if (settingsBtn == null)
            Debug.LogError("[MainMenuPanel] 找不到 SettingsBtn 按钮！");
        if (backToPreviousBtn == null)
            Debug.LogError("[MainMenuPanel] 找不到 BackToPreviousBtn 按钮！");
        if (returnToStartBtn == null)
            Debug.LogError("[MainMenuPanel] 找不到 ReturnToStartBtn 按钮！");

        CacheButtonOriginalPositions();
    }

    /// <summary>
    /// 记录按钮在各自 Anim 父节点中的正常位置，作为入场动画的终点。
    /// </summary>
    private void CacheButtonOriginalPositions()
    {
        List<Button> menuButtons = new List<Button>();

        AddButtonsInHierarchyOrder(transform.Find("ButtonContainer_1"), menuButtons);
        AddButtonsInHierarchyOrder(transform.Find("ButtonContainer_2"), menuButtons);

        foreach (Button button in menuButtons)
        {
            CacheButtonOriginalPosition(button);
        }
    }

    private void CacheButtonOriginalPosition(Button button)
    {
        if (button != null)
        {
            RectTransform buttonTransform = button.transform as RectTransform;
            _buttonOriginalPositions[buttonTransform] = buttonTransform.anchoredPosition;
        }
    }

    private void AddButtonsInHierarchyOrder(Transform container, List<Button> result)
    {
        if (container == null)
        {
            return;
        }

        // 获取每个按钮容器下的所有 Button 组件，按层级顺序添加到结果列表中
        Button[] buttons = container.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            result.Add(button);
        }
    }

    /// <summary>
    /// 绑定事件
    /// </summary>
    private void BindEvents()
    {
        if (newGameBtn != null)
            newGameBtn.onClick.AddListener(OnNewGameBtnClick);
        if (continueGameBtn != null)
            continueGameBtn.onClick.AddListener(OnContinueGameBtnClick);
        if (chapterSelectBtn != null)
            chapterSelectBtn.onClick.AddListener(OnChapterSelectBtnClick);
        if (loadGameBtn != null)
            loadGameBtn.onClick.AddListener(OnLoadGameBtnClick);
        if (galleryBtn != null)
            galleryBtn.onClick.AddListener(OnGalleryBtnClick);
        if (settingsBtn != null)
            settingsBtn.onClick.AddListener(OnSettingsBtnClick);
        if (backToPreviousBtn != null)
            backToPreviousBtn.onClick.AddListener(OnBackToPreviousBtnClick);
        if (returnToStartBtn != null)
            returnToStartBtn.onClick.AddListener(OnReturnToStartBtnClick);
    }
    
    /// <summary>
    /// 解绑事件（用于清理）
    /// </summary>
    private void UnbindEvents()
    {
        if (newGameBtn != null)
            newGameBtn.onClick.RemoveListener(OnNewGameBtnClick);
        if (continueGameBtn != null)
            continueGameBtn.onClick.RemoveListener(OnContinueGameBtnClick);
        if (chapterSelectBtn != null)
            chapterSelectBtn.onClick.RemoveListener(OnChapterSelectBtnClick);
        if (loadGameBtn != null)
            loadGameBtn.onClick.RemoveListener(OnLoadGameBtnClick);
        if (galleryBtn != null)
            galleryBtn.onClick.RemoveListener(OnGalleryBtnClick);
        if (settingsBtn != null)
            settingsBtn.onClick.RemoveListener(OnSettingsBtnClick);
        if (backToPreviousBtn != null)
            backToPreviousBtn.onClick.RemoveListener(OnBackToPreviousBtnClick);
        if (returnToStartBtn != null)
            returnToStartBtn.onClick.RemoveListener(OnReturnToStartBtnClick);
    }
    
    #endregion
    
    #region Unity生命周期
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // 确保UIManager已初始化（主菜单场景可能没有初始化UIManager）
        if (UIManager.GetInstance() != null && UIManager.GetInstance().canvas == null)
        {
            UIManager.GetInstance().Init();
        }
        
        // 每次打开面板时刷新存档状态
        RefreshSaveButtonState();

        PlayButtonEnterAnimation();
    }
    
    public override void ShowMe()
    {
        gameObject.SetActive(true);
        
        // 刷新存档按钮状态
        RefreshSaveButtonState();
    }
    
    public override void HideMe()
    {
        gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        StopButtonEnterAnimation();

        // 清理事件监听
        UnbindEvents();
    }

    #endregion

    #region 按钮入场动画

    /// <summary>
    /// 让按钮从各自 Anim 父节点的左侧依次飞入。
    /// ButtonContainer 的 Vertical Layout Group 不会干预这些按钮的局部坐标。
    /// </summary>
    private void PlayButtonEnterAnimation()
    {
        List<Button> menuButtons = new List<Button>();
        Transform buttonContainer1 = transform.Find("ButtonContainer_1");
        Transform buttonContainer2 = transform.Find("ButtonContainer_2");

        AddButtonsInHierarchyOrder(buttonContainer1, menuButtons);
        AddButtonsInHierarchyOrder(buttonContainer2, menuButtons);

        for (int i = 0; i < menuButtons.Count; i++)
        {
            if (menuButtons[i] == null)
                continue;

            RectTransform buttonTransform = menuButtons[i].transform as RectTransform;
            if (!_buttonOriginalPositions.TryGetValue(buttonTransform, out Vector2 targetPosition))
                continue;

            // 面板重复打开时，先停止上一次尚未结束的动画，防止两个动画同时修改位置。
            Tween.StopAll(buttonTransform);

            // 按钮在 Anim 节点内左移，终点仍是它原本的局部位置。
            Vector2 startPosition = targetPosition + Vector2.down * ButtonEnterOffset;
            buttonTransform.anchoredPosition = startPosition;

            Tween.UIAnchoredPosition(buttonTransform, startPosition, targetPosition, ButtonEnterDuration, Ease.OutBack,
                                     startDelay: i * ButtonEnterInterval, useUnscaledTime: true);
        }
    }

    /// <summary>
    /// 停止本面板按钮的入场动画，避免面板销毁后仍保留 Tween。
    /// </summary>
    private void StopButtonEnterAnimation()
    {
        foreach (RectTransform buttonTransform in _buttonOriginalPositions.Keys)
        {
            Tween.StopAll(buttonTransform);
        }
    }

    #endregion

    #region 按钮事件处理

    /// <summary>
    /// 新游戏按钮点击
    /// </summary>
    // private void OnNewGameBtnClick() //你没协程啊？
    // {
    //     if (VNManager.GetInstance() == null)
    //     {
    //         Debug.LogError("[MainMenuPanel] VNManager 未初始化！");
    //         return;
    //     }
    //     
    //     // 从配置中读取默认剧本名称和行ID
    //     string defaultScriptName = "Test101"; // 默认值
    //     string defaultLineID = ""; // 默认从开头开始
    //     
    //     if (VNProjectConfig.Instance != null)
    //     {
    //         defaultScriptName = string.IsNullOrEmpty(VNProjectConfig.Instance.DefaultScriptName) 
    //             ? "Test101" 
    //             : VNProjectConfig.Instance.DefaultScriptName;
    //         defaultLineID = VNProjectConfig.Instance.DefaultLineID ?? "";
    //     }
    //     else
    //     {
    //         Debug.LogWarning("[MainMenuPanel] VNProjectConfig 未找到，使用默认值");
    //     }
    //     
    //     // 隐藏主菜单（VNManager.StartGame() 会自动显示游戏面板）
    //     UIManager.GetInstance().HidePanel("MainMenuPanel");
    //     
    //     // 启动游戏（VNManager.StartGame() 内部会调用 ShowPanel<VNGameplayPanel>）
    //     VNManager.GetInstance().StartGame(defaultScriptName, defaultLineID);
    //     
    //     Debug.Log($"[MainMenuPanel] 开始新游戏: 剧本={defaultScriptName}, 行ID={defaultLineID}");
    // }


    /// <summary>
    /// 游戏按钮点击
    /// </summary>
    private void OnNewGameBtnClick()
    {
        if (_isStartingGame)
            return;

        if (VNManager.GetInstance() == null)
        {
            Debug.LogError("[MainMenuPanel] VNManager 未初始化！");
            return;
        }

        // 从配置中读取默认剧本名称和行ID
        string defaultScriptName = "Test101";
        string defaultLineID = "";

        if (VNProjectConfig.Instance != null)
        {
            defaultScriptName = string.IsNullOrEmpty(VNProjectConfig.Instance.DefaultScriptName)
                ? "Test101"
                : VNProjectConfig.Instance.DefaultScriptName;
            defaultLineID = VNProjectConfig.Instance.DefaultLineID ?? "";
        }
        else
        {
            Debug.LogWarning("[MainMenuPanel] VNProjectConfig 未找到，使用默认值");
        }

        StartCoroutine(StartNewGameFlow(defaultScriptName, defaultLineID));
    }

    /// <summary>
    /// 协程方法，按照顺序执行事件流，私有方法
    /// </summary>
    /// <param name="scriptName"></param>
    /// <param name="lineID"></param>
    /// <returns></returns>
    private IEnumerator StartNewGameFlow(string scriptName, string lineID)
    {
        _isStartingGame = true;

        // 先禁用主菜单交互，防止重复点击
        SetMenuInteractable(false);

        Debug.Log($"[MainMenuPanel] 开始新游戏流程: 剧本={scriptName}, 行ID={lineID}");

        // 1. 先显示常驻加载界面
        UIManager.GetInstance().ShowPanel<LoadingProgressPanel>(
            "LoadingProgressPanel",
            VNProjectConfig.Instance.UI_LoadingPath,
            E_UI_Layer.System,
            null
        );

        // 2. 强制刷新 UI，并至少等一帧，让 loading 真正显示到屏幕上
        Canvas.ForceUpdateCanvases();
        yield return null;
        yield return new WaitForEndOfFrame();

        // New Game must always start from a clean history, even when replaying the same script.
        GlobalDataManager.GetInstance().ClearHistoryLog();

        // 3. 此时再隐藏主菜单自己，但不要用 HidePanel 销毁
        HideMe();

        // 4. 再开始游戏逻辑
        VNManager.GetInstance().StartGame(scriptName, lineID);
    }

    /// <summary>
    /// 关闭目录的点击功能，私有方法
    /// </summary>
    /// <param name="interactable"></param> 
    private void SetMenuInteractable(bool interactable)
    {
        if (newGameBtn != null) newGameBtn.interactable = interactable;
        if (continueGameBtn != null) continueGameBtn.interactable = interactable;
        if (chapterSelectBtn != null) chapterSelectBtn.interactable = interactable;
        if (loadGameBtn != null) loadGameBtn.interactable = interactable;
        if (galleryBtn != null) galleryBtn.interactable = interactable;
        if (settingsBtn != null) settingsBtn.interactable = interactable;
        if (backToPreviousBtn != null) backToPreviousBtn.interactable = interactable;
        if (returnToStartBtn != null) returnToStartBtn.interactable = interactable;
    }

    /// <summary>
    /// 继续游戏按钮点击
    /// </summary>
    private void OnContinueGameBtnClick()
    {
        if (SaveManager.GetInstance() == null)
        {
            Debug.LogError("[MainMenuPanel] SaveManager 未初始化");
            return;
        }

        SaveData saveData = SaveManager.GetInstance().LoadGame(VNSaveSlots.ContinueSaveSlotIndex);

        if (saveData == null)
        {
            Debug.LogWarning("[MainMenuPanel] 没有可用的自动存档");
            RefreshSaveButtonState();
            return;
        }

        HideMe();

        VNManager.GetInstance().ContinueGame(saveData);

        Debug.Log("[MainMenuPanel] 正在加载自动存档");
    }

    /// <summary>
    /// 章节选择按钮点击
    /// </summary>
    private void OnChapterSelectBtnClick()
    {
        // 显示章节选择面板
        UIManager.GetInstance().ShowPanel<ChapterSelectPanel>(
            "ChapterSelectPanel",
            "VNovelizerRes/VNPrefabs/UI/ChapterSelect",
            E_UI_Layer.Middle,
            null
        );
    }

    /// <summary>
    /// 加载游戏按钮点击
    /// </summary>
    private void OnLoadGameBtnClick()
    {
        if (!loadGameBtn.interactable)
        {
            Debug.LogWarning("[MainMenuPanel] 没有可用的存档");
            return;
        }
        
        // 显示存档加载面板
        UIManager.GetInstance().ShowPanel<SaveLoadPanel>(
            "SaveLoadPanel", 
            VNProjectConfig.Instance.UI_SaveLoadPath, 
            E_UI_Layer.Middle, 
            (panel) =>
            {
                if (panel != null)
                {
                    panel.SetMode(SaveLoadPanel.Mode.Load);
                }
            }
        );
    }
    
    /// <summary>
    /// 画廊按钮点击
    /// </summary>
    private void OnGalleryBtnClick()
    {
        // 显示画廊面板
        // 注意：GalleryPanel 的路径可能需要从配置中读取
        string galleryPath = VNProjectConfig.Instance.UI_GalleryPath; // 临时使用Settings路径
        UIManager.GetInstance().ShowPanel<GalleryPanel>(
            "GalleryPanel", 
            galleryPath, 
            E_UI_Layer.Middle, 
            null
        );
    }
    
    /// <summary>
    /// 设置按钮点击
    /// </summary>
    private void OnSettingsBtnClick()
    {
        // 显示设置面板
        UIManager.GetInstance().ShowPanel<SettingsPanel>(
            "SettingsPanel", 
            VNProjectConfig.Instance.UI_SettingsPath, 
            E_UI_Layer.Middle, 
            null
        );
    }
    
    /// <summary>
    /// 返回上一级按钮点击
    /// </summary>
    private void OnBackToPreviousBtnClick()
    {
        SceneManager.LoadScene("GameModeSelectScene");
    }
    
    /// <summary>
    /// 返回开始菜单按钮点击
    /// </summary>
    private void OnReturnToStartBtnClick()
    {
        SceneManager.LoadScene("GameStartScene");
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 刷新存档按钮状态
    /// </summary>
    private void RefreshSaveButtonState()
    {
        if (loadGameBtn == null) return;
        
        // 检查是否存在存档
        bool hasSave = false;
        // 检查是否存在可继续的存档
        bool hasContinueSave = false;

        if (SaveManager.GetInstance() != null)
        {
            var saveDataList = SaveManager.GetInstance().GetAllSaveData();
            hasSave = saveDataList != null && saveDataList.Count > 0;
            hasContinueSave = SaveManager.GetInstance().IsSaveExists(VNSaveSlots.ContinueSaveSlotIndex);
        }
        
        loadGameBtn.interactable = hasSave;

        continueGameBtn.interactable = hasContinueSave;

        if (hasSave)
        {
            Debug.Log("[MainMenuPanel] 检测到存档，加载按钮已启用");
        }
        else
        {
            Debug.Log("[MainMenuPanel] 未检测到存档，加载按钮已禁用");
        }

        if (hasContinueSave)
        {
            Debug.Log("[MainMenuPanel] 检测到可继续的存档，继续游戏按钮已启用");
        }
        else
        {
            Debug.Log("[MainMenuPanel] 未检测到可继续的存档，继续游戏按钮已禁用");
        }
    }

    #endregion
}
