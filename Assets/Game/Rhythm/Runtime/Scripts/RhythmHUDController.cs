using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Rhythm.Runtime
{
    public class RhythmHUDController : MonoBehaviour
    {
        [Header("绑定对象")]
        public RhythmSessionController sessionController;

        [Header("UI")]
        public Slider slider;
        public Text scoreText;
        public Image hitLevelImage;
        public Text comboText;
        public GameObject gameOverUI;
        public GameObject failedUI;

        [Header("Game Over Results")]
        public TMP_Text gameOverScoreText;
        public TMP_Text gameOverMaxComboText;
        public TMP_Text gameOverAccuracyText;

        [Header("Animation")]
        public Animator hitLevelImageAnimation;
        public Animator comboTextAnimation;

        [Header("Resources")]
        public List<Sprite> hitLevelSprites;

        [Header("显示开关")]
        [Tooltip("剧情模式下是否显示分数")]
        public bool forceShowScore = false;

        [Tooltip("剧情模式下是否显示血条")]
        public bool forceShowHpBar = false;

        [Tooltip("剧情模式下是否显示 GameOver 面板")]
        public bool forceShowGameOver = false;

        private float hitLevelSpriteTimer;

        private void OnEnable()
        {
            BindSession();
        }

        private void OnDisable()
        {
            UnbindSession();
        }

        private void Start()
        {
            RefreshFromSession();
        }

        private void Update()
        {
            if (hitLevelImage != null && hitLevelImage.gameObject.activeSelf)
            {
                if (hitLevelSpriteTimer > 0f)
                {
                    hitLevelSpriteTimer -= Time.deltaTime;
                }
                else
                {
                    HideHitLevelSprite();
                }
            }
        }

        private void BindSession()
        {
            if (sessionController == null)
            {
                sessionController = FindAnyObjectByType<RhythmSessionController>();
            }

            if (sessionController == null)
            {
                return;
            }

            //音乐得分变化
            sessionController.ScoreChanged += OnScoreChanged;
            //血量变化
            sessionController.HpChanged += OnHpChanged;
            //连击数变化
            sessionController.ComboChanged += OnComboChanged;
            
            sessionController.SessionStatisticsChanged += RefreshGameOverResults;
            //命中等级变化
            sessionController.HitLevelChanged += OnHitLevelChanged;
            //暂停状态变化
            sessionController.PauseChanged += OnPauseChanged;
            //一首歌结束，显示结束面板
            sessionController.SessionCompleted += ShowCompletionPanel;
            //一首歌失败，显示失败面板
            sessionController.SessionFailed += ShowFailurePanel;

            // 如果 HUD 自己没填命中图片资源，就自动复用会话控制器上的配置。
            if ((hitLevelSprites == null || hitLevelSprites.Count == 0) &&
                sessionController.hitLevelSprites != null &&
                sessionController.hitLevelSprites.Count > 0)
            {
                hitLevelSprites = sessionController.hitLevelSprites;
            }

            ApplyModeVisibility();
            RefreshFromSession();
        }

        private void UnbindSession()
        {
            if (sessionController == null)
            {
                return;
            }

            sessionController.ScoreChanged -= OnScoreChanged;
            sessionController.HpChanged -= OnHpChanged;
            sessionController.ComboChanged -= OnComboChanged;
            sessionController.SessionStatisticsChanged -= RefreshGameOverResults;
            sessionController.HitLevelChanged -= OnHitLevelChanged;
            sessionController.PauseChanged -= OnPauseChanged;
            sessionController.SessionCompleted -= ShowCompletionPanel;
            sessionController.SessionFailed -= ShowFailurePanel;
        }

        private void RefreshFromSession()
        {
            if (sessionController == null)
            {
                return;
            }

            OnScoreChanged(sessionController.score);
            OnHpChanged(sessionController.hp);
            OnComboChanged(sessionController.comboNum);
            RefreshGameOverResults();

            if (hitLevelImage != null)
            {
                hitLevelImage.gameObject.SetActive(false);
            }

            if (gameOverUI != null)
            {
                gameOverUI.SetActive(false);
            }
        }

        private void ApplyModeVisibility()
        {
            if (sessionController == null)
            {
                return;
            }

            bool showScore = sessionController.showScore || forceShowScore;
            bool showHpBar = (sessionController.showHpBar || forceShowHpBar) && sessionController.useHpSystem;

            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(showScore);
            }

            if (slider != null)
            {
                slider.gameObject.SetActive(showHpBar);
            }

            if (gameOverUI != null)
            {
                // 这里默认隐藏，真正的失败面板由 SessionFailed 事件统一打开。
                gameOverUI.SetActive(false);
            }
        }

        private void OnScoreChanged(int score)
        {
            if (scoreText == null)
            {
                return;
            }

            if (sessionController != null && !(sessionController.showScore || forceShowScore))
            {
                return;
            }

            scoreText.text = "Score: " + score;
        }

        private void OnHpChanged(int hp)
        {
            if (slider == null)
            {
                return;
            }

            if (sessionController != null && !(sessionController.useHpSystem && (sessionController.showHpBar || forceShowHpBar)))
            {
                return;
            }

            slider.value = Mathf.Clamp01(hp / (float)Mathf.Max(sessionController.maxHp, 1));
        }

        private void OnComboChanged(int combo)
        {
            if (comboText == null)
            {
                return;
            }

            // 这里保留 combo 显示逻辑，剧情模式也可以继续展示连击反馈。
            if (combo > 0)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = combo.ToString();

                if (comboTextAnimation != null)
                {
                    comboTextAnimation.SetTrigger("IsNoteHittable");
                }
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }

        private void OnHitLevelChanged(int hitLevel)
        {
            if (hitLevelImage == null || hitLevelSprites == null || hitLevel < 0 || hitLevel >= hitLevelSprites.Count)
            {
                return;
            }

            // Great / Perfect / Miss 这类反馈在两种模式下都保留。
            hitLevelSpriteTimer = 1f;
            hitLevelImage.sprite = hitLevelSprites[hitLevel];
            hitLevelImage.SetNativeSize();
            hitLevelImage.gameObject.SetActive(true);

            if (hitLevelImageAnimation != null)
            {
                hitLevelImageAnimation.SetTrigger("IsNoteHittable");
            }
        }

        private void OnPauseChanged(bool isPaused)
        {
            // 这里先留空，后续如果要做暂停遮罩可以直接接这个事件。
        }

        private bool ShouldShowEndPanels()
        {
            return forceShowGameOver ||
                  (sessionController != null && 
                   sessionController.mode == GameMode.MusicGame);
        }

        private void ShowCompletionPanel()
        {
            if (gameOverUI == null || !ShouldShowEndPanels())
            {
                return;
            }

            RefreshGameOverResults();
            gameOverUI.SetActive(true);
        }

        private void ShowFailurePanel()
        {
            if (!ShouldShowEndPanels())
            {
                return;
            }

            if (failedUI == null)
            {
                EnsureFailedPanel();
            }

            failedUI.SetActive(true);
        }

        private void RefreshGameOverResults()
        {
            if (sessionController == null || gameOverUI == null)
            {
                return;
            }

            if(gameOverScoreText == null || gameOverMaxComboText == null || gameOverAccuracyText == null)
            {
                Debug.LogWarning("[RhythmHUDController] Game Over UI Text components are not assigned. Please assign them in the Inspector.");
            }

            gameOverScoreText.text = "SCORE：" + sessionController.score.ToString("D5");

            gameOverMaxComboText.text = "MAX COMBO：" + sessionController.maxCombo;

            gameOverAccuracyText.text = "ACCURACY：" + sessionController.Accuracy.ToString("F2", CultureInfo.InvariantCulture) + "%";
        }

        private void HideHitLevelSprite()
        {
            if (hitLevelImage != null)
            {
                hitLevelImage.gameObject.SetActive(false);
            }
        }

        //创建游戏失败面板（如果场景中不存在失败面板的话）
        private void EnsureFailedPanel()
        {
            if (failedUI != null || gameOverUI == null || sessionController == null)
            {
                return;
            }

            GameObject panel = new GameObject("Panel_Failed", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(gameOverUI.transform.parent, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateFailedText(panel.transform, "Text_Failed", "FAILED", font, 120f, 48);
            CreateFailedButton(panel.transform, "Button_Replay", "REPLAY", font, 15f, sessionController.Replay);
            CreateFailedButton(panel.transform, "Button_ReturnMain", "MAIN MENU", font, -95f, sessionController.ReturnToMain);

            failedUI = panel;
            failedUI.SetActive(false);
        }

        private void CreateFailedText(Transform parent, string objectName, string content, Font font, float yPosition, int fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, yPosition);
            rectTransform.sizeDelta = new Vector2(620f, 55f);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = content;
        }

        private void CreateFailedButton(Transform parent, string objectName, string label, Font font, float yPosition, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, yPosition);
            rectTransform.sizeDelta = new Vector2(260f, 72f);

            buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);
            CreateFailedText(buttonObject.transform, "Text", label, font, 0f, 28);
        }

        public void HideComboText()
        {
            if (comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }
}
