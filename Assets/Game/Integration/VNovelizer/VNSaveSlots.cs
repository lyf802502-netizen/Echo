using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VNSaveSlots
{
    // 0 到 58：玩家手动存档
    public const int ManualSaveSlotCount = 59;
    // 59：系统自动存档，用于“继续游戏”
    public const int ContinueSaveSlotIndex = 59;
}
