using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VNovelizer.Core.Commands
{
    public class ChapterCommand : VNCommand
    {
        public override string CommandName { get { return "chapter"; } }

        public override bool Execute(string args)
        {
            if(string.IsNullOrWhiteSpace(args))
            {
                Debug.LogError("[ChapterCommand] 章节名称不能为空");
                return false;
            }

            // 解析参数：操作类型, 章节ID
            string[] parts = args.Split(',');

            if(parts.Length < 2)
            {
                Debug.LogError("[ChapterCommand] 参数格式错误，应为 chapter(unlock,CH01) 或 chapter(complete,CH01)");
                return false;
            }

            string action = parts[0].Trim().ToLower();
            string chapterID = parts[1].Trim();

            if(string.IsNullOrEmpty(chapterID))
            {
                Debug.LogError("[ChapterCommand] 章节ID不能为空");
                return false;
            }

            GlobalDataManager dataManager = GlobalDataManager.GetInstance();

            switch (action)
            {
                case "unlock":
                    dataManager.UnlockChapter(chapterID);
                    return true;
                case "complete":
                    dataManager.CompleteChapter(chapterID);
                    return true;
                default:
                    Debug.LogError($"[ChapterCommand] 未知的操作类型: {action}");
                    return false;
            }
        }
    }
}
