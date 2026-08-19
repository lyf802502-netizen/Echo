using System.Collections.Generic;
using Game.Rhythm.Runtime;
using SonicBloom.Koreo;
using UnityEngine;

public class LaneController : MonoBehaviour
{
    private RhythmSessionController gameController;

    [Tooltip("此音轨对应的事件编号")]
    public int laneIndex;

    public Vector3 TargetPosition => transform.position;

    [Tooltip("此音轨对应的键盘按键")]
    public KeyCode keyboardButton;

    public Transform targetVisuals;
    public Transform targetTopPosition;
    public Transform targetBottomPosition;

    public GameObject downVisual;

    private readonly List<KoreographyEvent> laneEvents = new List<KoreographyEvent>();
    private readonly Queue<NoteObject> trackedNotes = new Queue<NoteObject>();

    private int pendingEventIndex = 0;

    public bool isLongNote;
    public float longNoteTimer;
    public GameObject longNoteHitEffect;

    private void Start()
    {
    }

    private void Update()
    {
        if (gameController == null)
        {
            return;
        }

        if (gameController.isPauseState || !gameController.isPlaying)
        {
            return;
        }

        while (trackedNotes.Count > 0)
        {
            NoteObject pendingNote = trackedNotes.Peek();
            if (pendingNote == null)
            {
                trackedNotes.Dequeue();
                continue;
            }

            if (!pendingNote.IsNoteMissed())
            {
                break;
            }

            // miss 的收尾统一在 Lane 内部完成，避免音符自己回收后队列还残留旧引用。
            if (pendingNote.isLongNoteEnd)
            {
                isLongNote = false;
                longNoteTimer = 0f;
                if (downVisual != null)
                {
                    downVisual.SetActive(false);
                }

                if (longNoteHitEffect != null)
                {
                    longNoteHitEffect.SetActive(false);
                }
            }

            gameController.RegisterMiss();
            trackedNotes.Dequeue();
            pendingNote.ReturnToPool();
        }

        CheckSpawnNext();

        if (Input.GetKeyDown(keyboardButton))
        {
            CheckNoteHit();
            if (downVisual != null)
            {
                downVisual.SetActive(true);
            }
        }
        else if (Input.GetKey(keyboardButton))
        {
            if (isLongNote)
            {
                if (longNoteTimer >= 0.15f)
                {
                    if (longNoteHitEffect != null && !longNoteHitEffect.activeSelf)
                    {
                        gameController.ChangeHitLevelSprite(2);
                        CreateHitLongEffect();
                    }

                    longNoteTimer = 0f;
                }
                else
                {
                    longNoteTimer += Time.deltaTime;
                }
            }
        }
        else if (Input.GetKeyUp(keyboardButton))
        {
            if (downVisual != null)
            {
                downVisual.SetActive(false);
            }

            if (isLongNote && longNoteHitEffect != null)
            {
                longNoteHitEffect.SetActive(false);
                CheckNoteHit();
            }
        }
    }

    public void Initialize(RhythmSessionController controller)
    {
        gameController = controller;
    }

    public bool IsIDMatched(int noteID)
    {
        return noteID == laneIndex;
    }

    public void AddEventToLane(KoreographyEvent koreographyEvent)
    {
        laneEvents.Add(koreographyEvent);
    }

    private int GetSpawnSampleOffset()
    {
        float spawnToTargetDistance = targetTopPosition.position.z - transform.position.z;
        float spawnToTargetTime = spawnToTargetDistance / gameController.noteSpeed;
        return (int)(spawnToTargetTime * gameController.SampleRate);
    }

    private void CheckSpawnNext()
    {
        int samplesToTarget = GetSpawnSampleOffset();
        int currentTime = gameController.DelayedSampleTime;

        while (pendingEventIndex < laneEvents.Count &&
               laneEvents[pendingEventIndex].StartSample < currentTime + samplesToTarget)
        {
            KoreographyEvent koreoEvent = laneEvents[pendingEventIndex];
            int noteNum = koreoEvent.GetIntValue();
            NoteObject newNoteObj = gameController.GetNoteObject();

            bool isLongNoteStart = false;
            bool isLongNoteEnd = false;
            //判断该音符是否为长按音符的起始或结束部分，若是则将其标记为长按音符
            if (noteNum > 6)
            {
                isLongNoteStart = true;
                noteNum -= 6;

                if (noteNum > 6)
                {
                    isLongNoteEnd = true;
                    isLongNoteStart = false;
                    noteNum -= 6;
                }
            }

            //初始化新音符对象，设置其属性并加入队列
            newNoteObj.Initialize(koreoEvent, noteNum, this, gameController, isLongNoteStart, isLongNoteEnd);
            trackedNotes.Enqueue(newNoteObj);
            pendingEventIndex++;
        }
    }

    private void CreateDownEffect()
    {
        GameObject downEffectGo =
            gameController.GetEffectObject(gameController.downEffectObjectPool, gameController.clickDownEffect);
        downEffectGo.transform.position = targetVisuals.position;
    }

    private void CreateHitEffect()
    {
        GameObject hitEffectGo =
            gameController.GetEffectObject(gameController.hitEffectObjectPool, gameController.hitNoteEffect);
        hitEffectGo.transform.position = targetVisuals.position;
    }

    private void CreateHitLongEffect()
    {
        if (longNoteHitEffect == null)
        {
            return;
        }

        longNoteHitEffect.SetActive(true);
        longNoteHitEffect.transform.position = targetVisuals.position;
    }

    public void ResetForNewSession()
    {
        // 每次新开局前都要把上一局遗留的队列和长按状态清掉，
        // 否则第二次开始时会继续沿用上一局的判定进度。
        isLongNote = false;
        longNoteTimer = 0f;
        pendingEventIndex = 0;

        if (downVisual != null)
        {
            downVisual.SetActive(false);
        }

        if (longNoteHitEffect != null)
        {
            longNoteHitEffect.SetActive(false);
        }

        while (trackedNotes.Count > 0)
        {
            NoteObject note = trackedNotes.Dequeue();
            if (note != null)
            {
                note.ReturnToPool();
            }
        }
    }

    public void CheckNoteHit()
    {
        if (trackedNotes.Count <= 0)
        {
            CreateDownEffect();
            return;
        }

        NoteObject noteObject = trackedNotes.Peek();
        if (noteObject == null)
        {
            CreateDownEffect();
            return;
        }

        int hitLevel = noteObject.IsNoteHittable();
        if (hitLevel <= 0)
        {
            // 没有进入有效判定窗时，只播放按下反馈，不提前吃掉音符。
            CreateDownEffect();
            return;
        }

        trackedNotes.Dequeue();
        gameController.RegisterHit(hitLevel);

        if (noteObject.isLongNoteStart)
        {
            isLongNote = true;
            CreateHitLongEffect();
        }
        else if (noteObject.isLongNoteEnd)
        {
            isLongNote = false;
            if (longNoteHitEffect != null)
            {
                longNoteHitEffect.SetActive(false);
            }
        }
        else
        {
            CreateHitEffect();
        }

        noteObject.OnHit();
    }
}
