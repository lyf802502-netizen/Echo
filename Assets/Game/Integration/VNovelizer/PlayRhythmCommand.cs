using System.Collections;
using Game.Rhythm.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using VNovelizer.Core.API;

namespace VNovelizer.Core.Commands
{
    public class PlayRhythmCommand : VNCommand
    {
        private const string DefaultRhythmScene = "RhythmGame";
        private const string DefaultReturnScene = "VNGamePlay";
        private const float WaitTimeoutSeconds = 5f;

        public override string CommandName => "playrhythm";

        public override bool Execute(string args)
        {
            MonoManager.GetInstance().StartCoroutine(ExecuteAsync(args));
            return true;
        }

        public override IEnumerator ExecuteAsync(string args)
        {
            string rhythmSceneName = DefaultRhythmScene;
            string returnSceneName = DefaultReturnScene;

            if (!string.IsNullOrWhiteSpace(args))
            {
                string[] parts = args.Split(',');
                if (parts.Length >= 1) rhythmSceneName = parts[0].Trim();
                if (parts.Length >= 2) returnSceneName = parts[1].Trim();
            }

            var vm = VNManager.GetInstance();
            int resumeIndex = vm.CurrentLineIndex + 1;
            string scriptName = vm.GetCurrentScriptName();

            if (vm.StoryLines == null || resumeIndex < 0 || resumeIndex >= vm.StoryLines.Count)
            {
                Debug.LogWarning("[PlayRhythmCommand] No next line found.");
                yield break;
            }

            string resumeLineId = vm.StoryLines[resumeIndex]?.ID;
            if (string.IsNullOrWhiteSpace(scriptName) || string.IsNullOrWhiteSpace(resumeLineId))
            {
                Debug.LogError("[PlayRhythmCommand] 无法恢复剧情：当前剧本名或下一行 ID 为空。");
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(rhythmSceneName))
            {
                Debug.LogError($"[PlayRhythmCommand] Scene not in Build Settings: {rhythmSceneName}");
                yield break;
            }

            SceneManager.LoadScene(rhythmSceneName);
            yield return null;

            RhythmSessionController session = null;
            float timeout = WaitTimeoutSeconds;
            while (timeout > 0f && session == null)
            {
                session = Object.FindFirstObjectByType<RhythmSessionController>();
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (session == null)
            {
                Debug.LogError("[PlayRhythmCommand] RhythmSessionController not found.");
                yield return ReturnToStory(returnSceneName, scriptName, resumeLineId, resumeIndex);
                yield break;
            }

            bool finished = false;
            void OnSessionEnd() => finished = true;

            session.SessionCompleted += OnSessionEnd;
            session.SessionFailed += OnSessionEnd;
            //在音游结束前，这段协程会被 WaitUntil 死死卡住，挂起后台，直到触发 OnSessionEnd
            yield return new WaitUntil(() => finished);
            //结束后，通过 -= 来注销事件，防止内存泄漏
            session.SessionCompleted -= OnSessionEnd;
            session.SessionFailed -= OnSessionEnd;

            yield return ReturnToStory(returnSceneName, scriptName, resumeLineId, resumeIndex);
        }

        private IEnumerator ReturnToStory(string returnSceneName, string scriptName, string resumeLineId, int resumeIndex)
        {
            // 这里不要再直接改 CurrentLineIndex。
            // 改为重新走 VNManager.StartGameOnScene(script, lineId) 的正式恢复链，
            // 这样能确保场景切回来后，剧本、UI、立绘和目标行定位都按插件原本流程执行。
            if (SceneManager.GetActiveScene().name != returnSceneName)
            {
                SceneManager.LoadScene(returnSceneName);
                yield return null;
            }

            float timeout = WaitTimeoutSeconds;
            while (timeout > 0f && SceneManager.GetActiveScene().name != returnSceneName)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            // 标记“后续剧情恢复由本命令自行处理”，
            // 防止 ExecuteActionsAndContinue 在命令结束后再次推进或重播当前行。
            VNManager.GetInstance().RequestExternalFlowHandled();
            VNManager.GetInstance().StartGameOnScene(scriptName, resumeLineId);

            timeout = WaitTimeoutSeconds;
            while (timeout > 0f)
            {
                bool panelReady = VNAPI.TryGetGameplayPanel(out _);
                bool lineReady = VNManager.GetInstance().CurrentLineIndex == resumeIndex;
                if (panelReady && lineReady)
                {
                    yield break;
                }

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.LogWarning("[PlayRhythmCommand] 剧情恢复等待超时，已退出等待。");
        }
    }
}
