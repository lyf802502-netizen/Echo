using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VNovelizer.Core.API;
using PrimeTween;

namespace VNovelizer.Core.Commands
{
    /// <summary>
    /// Background transition command. Supports fade-in, cross-fade, and fade-out.
    /// </summary>
    public class BgFadeCommand : VNCommand
    {
        private enum FadeMode
        {
            None,
            FadeIn,
            CrossFade,
            FadeOut
        }

        public override string CommandName { get { return "bgfade"; } }

        private const float DefaultDuration = 1f;
        private const float VisibleAlphaThreshold = 0.001f;

        private Image _front;
        private Image _back;
        private Sprite _targetSprite;
        private string _targetBackgroundName;
        private Tween _fadeTween;
        private FadeMode _mode;

        private bool _isRunning;
        private bool _blockAdvanceInput;

        public override bool BlockAdvanceInput => _isRunning && _blockAdvanceInput;

        public override bool Execute(string args)
        {
            return true;
        }

        public override IEnumerator ExecuteAsync(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                Debug.LogWarning("[BgFade] 参数不能为空。显示/切换格式：bgfade(backgroundName, duration)；淡出格式：bgfade(, duration)");
                yield break;
            }

            //解析指令
            if (!TryParseArguments(args, out string backgroundName, out float fadeOutDuration, 
                                out float blackHoldDuration, out float fadeInDuration, out bool isChapterTransition))
                yield break;

            _isRunning = true;
            _blockAdvanceInput = isChapterTransition;
            _mode = FadeMode.None;
            _targetSprite = null;
            _targetBackgroundName = backgroundName;
            _fadeTween = default;

            yield return WaitForBackgroundImages();

            if (!_isRunning || _front == null || _back == null)
            {
                Cleanup();
                yield break;
            }

            if (isChapterTransition)
            {
                yield return PlayChapterTransition(backgroundName, fadeOutDuration, blackHoldDuration, fadeInDuration);
                Cleanup();
                yield break;
            }

            bool hasTargetBackground = !string.IsNullOrEmpty(backgroundName);
            bool hasCurrentBackground = HasVisibleBackground();

            if (!hasTargetBackground)
            {
                if (!hasCurrentBackground)
                {
                    CommitBlackBackground();
                }
                else //如果当前有背景，则淡出当前背景，最终显示黑屏
                {
                    _mode = FadeMode.FadeOut;
                    NormalizeCurrentBackgroundToFront();
                    yield return FadeAlpha(_front, _front.color.a, 0f, fadeOutDuration);

                    if (_isRunning)
                    {
                        CommitBlackBackground();
                    }
                }

                Cleanup();
                yield break;
            }

            yield return LoadBackground(backgroundName);
            if (!_isRunning || _targetSprite == null)
            {
                Cleanup();
                yield break;
            }

            if (!hasCurrentBackground)
            {
                _mode = FadeMode.FadeIn;
                // [2026-08-13] 首次淡入时 BG_Front 会完全透明；使用 BG_Back 作为黑色底板，
                // 防止透明期间露出 VNGamePlayPanel 根节点的半透明白色 Image 而产生白闪。
                _back.sprite = null;
                _back.color = Color.black;
                _back.gameObject.SetActive(true);
                _front.sprite = _targetSprite;
                _front.color = new Color(1f, 1f, 1f, 0f);
                _front.gameObject.SetActive(true);

                yield return FadeAlpha(_front, 0f, 1f, fadeInDuration);
            }
            else //在当前有背景的情况下，执行交叉淡化，先把当前背景归一化到 BG_Front，然后把目标背景放到 BG_Back，最后淡出 BG_Front。
            {
                _mode = FadeMode.CrossFade;
                NormalizeCurrentBackgroundToFront();

                // [2026-08-13] 相同背景无需交叉淡化，直接恢复不透明状态。
                if (_front.sprite == _targetSprite)
                {
                    CommitTargetBackground();
                    Cleanup();
                    yield break;
                }

                _back.sprite = _targetSprite;
                _back.color = Color.white;
                _back.gameObject.SetActive(true);

                yield return FadeAlpha(_front, 1f, 0f, fadeOutDuration);
            }

            if (_isRunning)
            {
                CommitTargetBackground();
            }

            Cleanup();
        }

        private bool TryParseArguments(string args, out string backgroundName, out float fadeOutDuration, 
                                       out float blackHoldDuration, out float fadeInDuration, out bool isChapterTransition)
        {
            // 初始化输出参数
            backgroundName = string.Empty;
            fadeOutDuration = DefaultDuration;
            blackHoldDuration = 0f;
            fadeInDuration = DefaultDuration;
            isChapterTransition = false;

            string[] parts = args.Split(',');

            // 兼容旧语法：
            // bgfade(image)
            // bgfade(image, duration)
            // bgfade(, duration)
            if(parts.Length <= 2)
            {
                backgroundName = parts[0].Trim();

                if(parts.Length == 2 && float.TryParse(parts[1].Trim(), out float parsedDuration))
                {
                    fadeOutDuration = Mathf.Max(0f, parsedDuration);
                }

                fadeInDuration = fadeOutDuration; // 默认淡入时间与淡出时间相同
                return true;
            }

            // 新语法：
            // bgfade(targetImage, fadeOutDuration, blackHoldDuration, fadeInDuration)
            if (parts.Length == 4)
            {
                backgroundName = parts[0].Trim();
                
                if (string.IsNullOrEmpty(backgroundName))
                {
                    Debug.LogWarning("[BgFade] 四参数转场必须指定目标背景。");
                    return false;
                }

                if (float.TryParse(parts[1].Trim(), out float parsedFadeOut))
                {
                    fadeOutDuration = Mathf.Max(0f, parsedFadeOut);
                }

                if (float.TryParse(parts[2].Trim(), out float parsedBlackHold))
                {
                    blackHoldDuration = Mathf.Max(0f, parsedBlackHold);
                }

                if (float.TryParse(parts[3].Trim(), out float parsedFadeIn))
                {
                    fadeInDuration = Mathf.Max(0f, parsedFadeIn);
                }

                isChapterTransition = true;
                return true;
            }

            Debug.LogWarning("[BgFade] 参数格式错误。" +
                             "显示/切换格式：bgfade(backgroundName, duration)；" +
                             "淡出格式：bgfade(, duration)；" +
                             "转场格式：bgfade(targetImage, fadeOutDuration, blackHoldDuration, fadeInDuration)");
            return false;
        }

        private IEnumerator WaitForBackgroundImages()
        {
            float waitTime = 0f;
            const float maxWaitTime = 1f;
            _front = VNAPI.GetBG_F();
            _back = VNAPI.GetBG_B();

            while ((_front == null || _back == null) && waitTime < maxWaitTime)
            {
                yield return null;
                waitTime += Time.deltaTime;
                _front = VNAPI.GetBG_F();
                _back = VNAPI.GetBG_B();
            }

            if (_front == null || _back == null)
            {
                Debug.LogWarning("[BgFade] 未找到 BG_Front 或 BG_Back，已跳过背景过渡。");
                _isRunning = false;
            }
        }

        private IEnumerator LoadBackground(string backgroundName)
        {
            string fullPath = VNProjectConfig.Instance.BackgroundResPath + "/" + backgroundName;
            ResourceRequest request = Resources.LoadAsync<Sprite>(fullPath);
            yield return request;

            if (!_isRunning)
            {
                yield break;
            }

            _targetSprite = request.asset as Sprite;
            if (_targetSprite == null)
            {
                Debug.LogError($"[BgFade] 图片加载失败: {backgroundName} (路径: {fullPath})");
                _isRunning = false;
            }
        }

        // [新增] [2026-08-14] 章节转场动画：淡出当前背景 -> 黑屏停留 -> 淡入目标背景
        private IEnumerator PlayChapterTransition(string backgroundName, float fadeOutDuration, float blackHoldDuration, float fadeInDuration)
        {
            yield return LoadBackground(backgroundName);

            if(!_isRunning || _targetSprite == null)
            {
                yield break;
            }

            bool hasCurrentBackground = HasVisibleBackground();

            if(hasCurrentBackground)
            {
                NormalizeCurrentBackgroundToFront();
            }
            else
            {
                _front.sprite = null;
                _front.color = new Color(1f, 1f, 1f, 0f);
                _front.gameObject.SetActive(false);
            }

            // 黑色底板必须在淡出前出现，防止透明时露出 UI 的默认底色
            _back.sprite = null;
            _back.color = Color.black;
            _back.gameObject.SetActive(true);

            if(hasCurrentBackground)
            {
                _mode = FadeMode.FadeOut;
                yield return FadeAlpha(_front, _front.color.a, 0f, fadeOutDuration);

                if(!_isRunning)
                {
                    yield break;
                }
            }

            if (blackHoldDuration > 0f)
            {
                yield return new WaitForSeconds(blackHoldDuration);
            }

            if (!_isRunning)
            {
                yield break;
            }

            _mode = FadeMode.FadeIn;
            _front.sprite = _targetSprite;
            _front.color = new Color(1f, 1f, 1f, 0f);
            _front.gameObject.SetActive(true);

            yield return FadeAlpha(_front, 0f, 1f, fadeInDuration);

            if (_isRunning)
            {
                CommitTargetBackground();
            }
        }

        private IEnumerator FadeAlpha(Image image, float from, float to, float duration)
        {
            SetAlpha(image, from);
            if (duration <= 0f)
            {
                SetAlpha(image, to);
                yield break;
            }

            bool completed = false;
            _fadeTween = Tween.Alpha(image, from, to, duration)
                .OnComplete(() => completed = true);

            while (_isRunning && !completed)
            {
                yield return null;
            }
        }

        private bool HasVisibleBackground()
        {
            return IsVisible(_front) || IsVisible(_back);
        }

        private bool IsVisible(Image image)
        {
            return image != null && image.gameObject.activeInHierarchy && image.sprite != null && image.color.a > VisibleAlphaThreshold;
        }

        private void NormalizeCurrentBackgroundToFront()
        {
            if (!IsVisible(_front) && IsVisible(_back))
            {
                _front.sprite = _back.sprite;
            }

            _front.color = Color.white;
            _front.gameObject.SetActive(true);
            _back.gameObject.SetActive(false);
        }

        private void CommitTargetBackground()
        {
            if (_front != null && _targetSprite != null)
            {
                _front.sprite = _targetSprite;
                _front.color = Color.white;
                _front.gameObject.SetActive(true);
            }

            if (_back != null)
            {
                _back.gameObject.SetActive(false);
            }

            // [2026-08-13] 仅在视觉状态提交后同步剧本背景键名，避免加载失败或 Sprite 改名时状态与画面不一致。
            VNManager.GetInstance().UpdateCurrentBG_OnlyData(_targetBackgroundName);
        }

        private void CommitBlackBackground()
        {
            if (_front != null)
            {
                _front.sprite = null;
                _front.color = Color.black;
                _front.gameObject.SetActive(true);
            }

            if (_back != null)
            {
                _back.sprite = null;
                _back.gameObject.SetActive(false);
            }

            // [2026-08-13] 空目标 bgfade(, duration) 的标准结束状态是黑屏。
            VNManager.GetInstance().UpdateCurrentBG_OnlyData("black");
        }

        private void SetAlpha(Image image, float alpha)
        {
            if (image == null) return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private void Cleanup()
        {
            _isRunning = false;
            _blockAdvanceInput = false;
            _mode = FadeMode.None;
            _targetSprite = null;
            _targetBackgroundName = null;
            _fadeTween = default;
            _front = null;
            _back = null;
        }

        public override void Simulate(string args)
        {
            if(!TryParseArguments(args, out string backgroundName, out _, out _, out _, out _))
            {
                return;
            }

            // [2026-08-13] 预演时不播放动画，空目标按淡出后的黑屏状态处理。
            VNAPI.UpdateBGData(string.IsNullOrEmpty(backgroundName) ? "black" : backgroundName);
        }

        public override void Interrupt()
        {
            if (!_isRunning) return;

            if (_fadeTween.isAlive)
            {
                _fadeTween.Stop();
            }

            // [2026-08-13] 玩家跳过时直接落到当前模式的最终画面，避免残留半透明背景。
            if (_mode == FadeMode.FadeOut)
            {
                CommitBlackBackground();
            }
            else if (_targetSprite != null)
            {
                CommitTargetBackground();
            }

            Cleanup();
        }
    }
}
