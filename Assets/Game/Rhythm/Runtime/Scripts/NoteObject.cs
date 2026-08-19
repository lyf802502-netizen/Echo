using Game.Rhythm.Runtime;
using SonicBloom.Koreo;
using UnityEngine;

public class NoteObject : MonoBehaviour
{
    public SpriteRenderer visuals;
    public Sprite[] noteSprites;

    private KoreographyEvent trackedEvent;
    private LaneController laneController;
    private RhythmSessionController gameController;

    public bool isLongNoteStart;
    public bool isLongNoteEnd;

    public int hitOffset;

    private void Start()
    {
    }

    private void Update()
    {
        if (gameController == null || gameController.isPauseState)
        {
            return;
        }

        if (!HasRuntimeBindings())
        {
            return;
        }

        UpdatePositon();
        GetHitOffset();
    }

    public void Initialize(
        KoreographyEvent koreographyEvent,
        int noteNum,
        LaneController laneController,
        RhythmSessionController rhythmGameController,
        bool isLongNoteStart,
        bool isLongNoteEnd)
    {
        trackedEvent = koreographyEvent;
        this.laneController = laneController;
        gameController = rhythmGameController;
        this.isLongNoteStart = isLongNoteStart;
        this.isLongNoteEnd = isLongNoteEnd;

        int spriteNum = noteNum;
        if (this.isLongNoteStart)
        {
            spriteNum += 6;
        }
        else if (this.isLongNoteEnd)
        {
            spriteNum += 12;
        }

        visuals.sprite = noteSprites[spriteNum - 1];
    }

    private bool HasRuntimeBindings()
    {
        return trackedEvent != null && laneController != null && gameController != null;
    }

    public void ResetNote()
    {
        trackedEvent = null;
        laneController = null;
        gameController = null;
        isLongNoteStart = false;
        isLongNoteEnd = false;
        hitOffset = 0;
    }

    public void ReturnToPool()
    {
        if (gameController != null)
        {
            gameController.ReturnNoteObjectToPool(this);
        }

        ResetNote();
    }

    public void OnHit()
    {
        ReturnToPool();
    }

    public void UpdatePositon()
    {
        Vector3 targetPositon = laneController.TargetPosition;
        targetPositon.z -=
            (gameController.DelayedSampleTime - trackedEvent.StartSample) /
            (float)gameController.SampleRate *
            gameController.noteSpeed;

        transform.position = targetPositon;
    }

    private void GetHitOffset()
    {
        int curTime = gameController.DelayedSampleTime;
        int noteTime = trackedEvent.StartSample;
        int hitWindow = gameController.HitWindowSampleWidth;

        hitOffset = hitWindow - Mathf.Abs(noteTime - curTime);
    }

    public bool IsNoteMissed()
    {
        // 未绑定运行时对象时，说明这个音符已经被回收，不再参与 miss 判定。
        if (!enabled || !HasRuntimeBindings())
        {
            return false;
        }

        int curTime = gameController.DelayedSampleTime;
        int noteTime = trackedEvent.StartSample;
        int hitWindow = gameController.HitWindowSampleWidth;
        return curTime - noteTime > hitWindow;
    }

    public int IsNoteHittable()
    {
        // 每次按键都重新按当前时间计算命中结果，避免沿用上一帧的旧 hitOffset。
        if (!HasRuntimeBindings())
        {
            return 0;
        }

        GetHitOffset();

        if (hitOffset >= 0)
        {
            return hitOffset >= 2000 && hitOffset <= 9000 ? 2 : 1;
        }

        return 0;
    }
}
