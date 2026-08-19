using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PrimeTween;

public class GameStartPanel : BasePanel
{
    #region UI 控件引用
    [SerializeField]
    private Button startGameButton;
    [SerializeField]
    private Button quitGameButton;
    #endregion

    #region 界面初始化
    protected override void Awake()
    {
        base.Awake();

        UIManager.GetInstance().Init();

        // 初始化控件
        InitializeControls();

        BindEvents();
    }

    private void InitializeControls()
    {
        startGameButton = GetControl<Button>("Play");
        quitGameButton = GetControl<Button>("Exit");

        if (startGameButton == null)
            Debug.Log("[GameStartPanel] 找不到 StartGameBtn 按钮！");
        if (quitGameButton == null)
            Debug.Log("[GameStartPanel] 找不到 QuitGameBtn 按钮！");
    }

    private void BindEvents()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameBtnClick);
        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(OnQuitGameBtnClick);
    }

    private void UnbindEvents()
    {
        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(OnStartGameBtnClick);
        if (quitGameButton != null)
            quitGameButton.onClick.RemoveListener(OnQuitGameBtnClick);
    }
    #endregion

    #region Unity 生命周期
    private void OnDestroy()
    {
        // 清理事件监听
        UnbindEvents();
    }
    #endregion

    #region 按钮事件处理
    private void OnStartGameBtnClick()
    {
        SceneManager.LoadScene("GameModeSelectScene");
    }

    private void OnQuitGameBtnClick()
    {
        // 显示确认对话框
        string confirmPath = VNProjectConfig.Instance.UI_ConfirmPath;
        UIManager.GetInstance().ShowPanel<ConfirmPanel>(
            "ConfirmPanel",
            confirmPath,
            E_UI_Layer.System,
            (panel) =>
            {
                if (panel != null)
                {
                    panel.Show(
                        "退出游戏",
                        "确定要退出游戏吗？",
                        () =>
                        {
                            // 确定退出
                            Debug.Log("[GameStartPanel] 退出游戏");
                            Application.Quit();

                            // 在编辑器中，Application.Quit() 不会生效，使用这个替代
                            #if UNITY_EDITOR
                            UnityEditor.EditorApplication.isPlaying = false;
                            #endif
                        },
                        null // 取消无需操作
                    );
                }
            }
        );
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        
    }
}
