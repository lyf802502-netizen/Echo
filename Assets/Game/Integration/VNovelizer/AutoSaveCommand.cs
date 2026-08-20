using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VNovelizer.Core.Commands
{
    public class AutoSaveCommand : VNCommand
    {
        public override string CommandName { get { return "autosave"; } }

        public override bool Execute(string args)
        {
            VNManager manager = VNManager.GetInstance();

            if (manager == null)
            {
                Debug.LogError("[AutoSaveCommand] VNManager 不存在，无法自动存档");
                return false;
            }

            manager.SaveGame(VNSaveSlots.ContinueSaveSlotIndex);
            Debug.Log($"[AutoSaveCommand] 自动存档完成，槽位: {VNSaveSlots.ContinueSaveSlotIndex}");
            return true;
        }
    }
}
