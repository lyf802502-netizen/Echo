using System.Collections;
using UnityEngine;

namespace VNovelizer.Core.Commands
{
    /// <summary>
    /// Shows a chapter title card and waits until its animation has finished.
    /// Format: chaptercard(chapter, title, holdSeconds)
    /// </summary>
    public class ChapterCardCommand : VNCommand
    {
        public override string CommandName => "chaptercard";

        public override bool BlockAdvanceInput => true;

        public override bool Execute(string args)
        {
            // CommandManager uses ExecuteAsync for story playback.
            return !string.IsNullOrWhiteSpace(args);
        }

        public override IEnumerator ExecuteAsync(string args)
        {
            if (!TryParse(args, out string chapter, out string title, out float holdSeconds))
                yield break;

            if (VNProjectConfig.Instance == null)
            {
                Debug.LogError("[ChapterCardCommand] VNProjectConfig is not available.");
                yield break;
            }

            bool completed = false;
            bool loaded = false;
            //ChapterCardPanel chapterPanel = null;

            // [2026-08-29] 先加载章节卡，让黑色遮罩先覆盖画面，再淡出游戏演出层。
            //UIManager.GetInstance().ShowPanel<ChapterCardPanel>(
            //    "ChapterCardPanel",
            //    VNProjectConfig.Instance.UI_ChapterCardPath,
            //    E_UI_Layer.Top,
            //    panel =>
            //    {
            //        chapterPanel = panel;
            //        loaded = true;
            //    });

            //yield return new WaitUntil(() => loaded);

            //if (chapterPanel == null)
            //    yield break;

            //chapterPanel.Play(chapter, title, holdSeconds, () => completed = true);

            UIManager.GetInstance().ShowPanel<ChapterCardPanel>(
                "ChapterCardPanel",
                VNProjectConfig.Instance.UI_ChapterCardPath,
                E_UI_Layer.Top,
                panel =>
                {
                    loaded = true;
                    if (panel == null)
                    {
                        completed = true;
                        return;
                    }

                    panel.Play(chapter, title, holdSeconds, () => completed = true);
                });

            // ShowPanel loads resources asynchronously, so wait for its callback.
            yield return new WaitUntil(() => completed && loaded);
        }

        private static bool TryParse(string args, out string chapter, out string title, out float holdSeconds)
        {
            chapter = string.Empty;
            title = string.Empty;
            holdSeconds = 0f;

            if (string.IsNullOrWhiteSpace(args))
            {
                Debug.LogError("[ChapterCardCommand] Format: chaptercard(chapter, title, holdSeconds)");
                return false;
            }

            string[] parts = args.Split(',');
            if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                Debug.LogError("[ChapterCardCommand] Format: chaptercard(chapter, title, holdSeconds)");
                return false;
            }

            if (!float.TryParse(parts[2].Trim(), out holdSeconds) || holdSeconds < 0f)
            {
                Debug.LogError("[ChapterCardCommand] holdSeconds must be a non-negative number.");
                return false;
            }

            chapter = parts[0].Trim();
            title = parts[1].Trim();
            return true;
        }
    }
}
