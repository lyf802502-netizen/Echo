using System;
using System.Collections.Generic;
using SonicBloom.Koreo;
using SonicBloom.Koreo.Players;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Rhythm.Runtime
{
    public enum GameMode
    {
        Story,
        MusicGame
    }

    public class RhythmSessionController : MonoBehaviour
    {
        public event Action<int> ScoreChanged;
        public event Action<int> HpChanged;
        public event Action<int> ComboChanged;
        public event Action SessionStatisticsChanged;
        public event Action<int> HitLevelChanged;
        public event Action<bool> PauseChanged;
        public event Action SessionCompleted;
        public event Action SessionFailed;

        [Header("Mode")]
        [Tooltip("剧情模式不扣血；独立模式可以失败。")]
        public GameMode mode = GameMode.Story;

        [Tooltip("是否启用血量系统。")]
        public bool useHpSystem = false;

        [Tooltip("是否显示分数。")]
        public bool showScore = false;

        [Tooltip("是否显示血条。")]
        public bool showHpBar = false;

        [Tooltip("歌曲自然播放结束后是否自动结束本局。")]
        public bool autoCompleteOnSongEnd = true;

        [Header("Scene Names")]
        [Tooltip("独立音游模式下的重开场景名。")]
        public string replaySceneName = "";

        [Tooltip("独立音游模式下的主菜单场景名。")]
        public string mainMenuSceneName = "";

        [Header("Koreography")]
        [Tooltip("用于读取 Koreography 事件轨的 Event ID。")]
        [EventID]
        public string eventID;

        [Tooltip("音符移动速度，单位：m/s。")]
        public float noteSpeed = 1f;

        [Tooltip("命中判定窗口，单位：毫秒。")]
        [Range(8f, 300f)]
        public float hitWindowRangeInMS = 120f;

        [Tooltip("进入场景后是否自动开始音游。")]
        public bool autoStartOnSceneLoad = true;

        [Tooltip("是否通过剧情事件触发开始音游。")]
        public bool listenForStoryEvent = false;

        [Header("Audio")]
        public AudioSource audioSource;
        public GameObject musicPlayer;

        [Header("Note Lanes")]
        public List<LaneController> noteLanes = new List<LaneController>();
        public NoteObject noteObject;

        [Header("Effects")]
        public GameObject clickDownEffect;
        public GameObject hitNoteEffect;
        public Stack<GameObject> downEffectObjectPool = new Stack<GameObject>();
        public Stack<GameObject> hitEffectObjectPool = new Stack<GameObject>();
        public Stack<GameObject> hitLongEffectObjectPool = new Stack<GameObject>();

        [Header("Resources")]
        public List<Sprite> hitLevelSprites;
        public Koreography koreographyAsset;

        [Header("Score And HP")]
        public int maxHp = 10;
        public int missDamage = 2;

        [Tooltip("一首谱面全 Perfect 时达到的固定满分。")]
        [Min(1)] public int maxScore = 100000;

        [Tooltip("Great 相对 Perfect 的准确率和分数权重。")]
        [Range(0f, 1f)] public float greatWeight = 0.7f;

        [Space]
        public int comboNum;
        public int score;
        public int maxCombo;
        public int perfectCount;
        public int greatCount;
        public int missCount;
        public int totalNoteCount;
        public int hp = 10;
        public bool isPauseState;
        public bool gameStart;
        public bool isPlaying;

        [Tooltip("正式播放前的前摇时间。")]
        public float leadInTime;

        public float HitWindowSizeInUnits => (hitWindowRangeInMS * 0.001f) * noteSpeed;
        public int HitWindowSampleWidth => hitWindowRangeInSamples;
        public int SampleRate => playingKoreo != null ? playingKoreo.SampleRate : 0;
        public int DelayedSampleTime => playingKoreo != null ? playingKoreo.GetLatestSampleTime() - (int)(SampleRate * leadInTimeLeft) : 0;
        public bool IsStoryMode => mode == GameMode.Story;
        public float Accuracy => totalNoteCount > 0
            ? (perfectCount + greatCount * greatWeight) / totalNoteCount * 100f
            : 0f;

        private int hitWindowRangeInSamples;
        private float leadInTimeLeft;
        private float timeLeftToPlay;
        private bool hasSessionEnded;

        private readonly Stack<NoteObject> noteObjectPool = new Stack<NoteObject>();
        private Koreography playingKoreo;
        private SimpleMusicPlayer simpleMusicPlayer;

        private void OnEnable()
        {
            if (listenForStoryEvent)
            {
                // 预留给剧情命令桥接。
            }
        }

        private void OnDisable()
        {
            if (listenForStoryEvent)
            {
                // 预留给剧情命令桥接解绑。
            }
        }

        private void Start()
        {
            ResetRuntimeState();
            CachePlayers();
            BindLanes();
            LoadKoreographyData();

            if (autoStartOnSceneLoad)
            {
                StartRhythmGame();
            }
        }

        private void Update()
        {
            if (hasSessionEnded || isPauseState)
            {
                return;
            }

            if (timeLeftToPlay > 0f)
            {
                timeLeftToPlay -= Time.unscaledDeltaTime;
                if (timeLeftToPlay <= 0f)
                {
                    timeLeftToPlay = 0f;
                    BeginPlayback();
                }
            }

            if (leadInTimeLeft > 0f)
            {
                leadInTimeLeft = Mathf.Max(leadInTimeLeft - Time.unscaledDeltaTime, 0f);
            }

            // 歌曲自然播完后统一走“完成本局”，剧情模式和独立模式共用同一出口。
            if (gameStart && autoCompleteOnSongEnd && HasPlaybackFinished())
            {
                CompleteSession();
            }
        }

        public void StartSession()
        {
            StartRhythmGame();
        }

        public void StartRhythmGame()
        {
            if (isPlaying)
            {
                return;
            }

            ResetSessionValues();
            ResetLaneStates();
            ResetPlaybackState();

            hasSessionEnded = false;
            isPauseState = false;
            gameStart = false;
            isPlaying = true;

            InitializeLeadInTime();

            // 新开局先同步一次 HUD，避免沿用上一局残留数据。
            ScoreChanged?.Invoke(score);
            HpChanged?.Invoke(hp);
            ComboChanged?.Invoke(comboNum);
            PauseChanged?.Invoke(false);
        }

        public void CompleteSession()
        {
            if (hasSessionEnded)
            {
                return;
            }

            hasSessionEnded = true;
            isPlaying = false;
            gameStart = false;
            isPauseState = false;

            // 正常结算时也彻底停掉播放器，保证下一次一定从歌曲开头重新开始。
            StopPlayback(resetAudioTime: true);
            ClearAllSpawnedNotes();
            PauseChanged?.Invoke(false);
            SessionCompleted?.Invoke();
        }

        public void FailSession()
        {
            if (hasSessionEnded)
            {
                return;
            }

            hasSessionEnded = true;
            isPlaying = false;
            gameStart = false;
            isPauseState = true;

            // 失败时也直接停止播放，避免重新开始时沿用上一局的播放进度。
            StopPlayback(resetAudioTime: true);
            ClearAllSpawnedNotes();
            PauseChanged?.Invoke(true);
            SessionFailed?.Invoke();
        }

        public void RegisterMiss()
        {
            // Miss 一定会打断连击，并广播 Miss 提示给 HUD。
            ChangeHitLevelSprite(0);
            ResetCombo();
            missCount++;
            SessionStatisticsChanged?.Invoke();

            // 剧情模式不扣血，只保留判定反馈。
            if (!useHpSystem || IsStoryMode)
            {
                return;
            }

            ReduceHP(missDamage);
        }

        public void RegisterHit(int hitLevel)
        {
            if (hitLevel <= 0)
            {
                RegisterMiss();
                return;
            }

            // 所有命中反馈统一从这里发出，HUD 只负责显示。
            ChangeHitLevelSprite(hitLevel);

            if (hitLevel >= 2)
            {
                perfectCount++;
            }
            else
            {
                greatCount++;
            }

            UpdateNormalizedScore();
            ScoreChanged?.Invoke(score);

            comboNum++;
            maxCombo = Mathf.Max(maxCombo, comboNum);
            ComboChanged?.Invoke(comboNum);
            SessionStatisticsChanged?.Invoke();
        }

        public void ResetCombo()
        {
            comboNum = 0;
            ComboChanged?.Invoke(comboNum);
        }

        [Obsolete]
        public void HideComboText()
        {
            // 保留旧接口兼容，实际显示由 HUD 负责。
            ResetCombo();
        }

        [Obsolete]
        public void UpdateScoreText(int addNum)
        {
            // 保留旧接口，避免场景里仍有旧按钮或动画事件引用时报错。
            score += addNum;
            ScoreChanged?.Invoke(score);
        }

        public void UpdateHP()
        {
            ReduceHP(missDamage);
        }

        public void ReduceHP(int amount)
        {
            if (!useHpSystem || IsStoryMode)
            {
                return;
            }

            hp = Mathf.Max(hp - amount, 0);
            HpChanged?.Invoke(hp);

            if (hp <= 0)
            {
                FailSession();
            }
        }

        public void ChangeHitLevelSprite(int hitLevel)
        {
            HitLevelChanged?.Invoke(hitLevel);
        }

        public void TogglePause()
        {
            if (!gameStart || hasSessionEnded)
            {
                return;
            }

            if (isPauseState)
            {
                PlayMusic();
            }
            else
            {
                PauseMusic();
            }
        }

        public void PauseMusic()
        {
            if (simpleMusicPlayer != null)
            {
                simpleMusicPlayer.Pause();
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
            }

            isPauseState = true;
            PauseChanged?.Invoke(true);
        }

        public void PlayMusic()
        {
            if (simpleMusicPlayer != null)
            {
                simpleMusicPlayer.Play();
            }

            if (audioSource != null)
            {
                audioSource.UnPause();
            }

            isPauseState = false;
            PauseChanged?.Invoke(false);
        }

        public void Replay()
        {
            string sceneName = string.IsNullOrEmpty(replaySceneName)
                ? SceneManager.GetActiveScene().name
                : replaySceneName;
            SceneManager.LoadScene(sceneName);
        }

        public void ReturnToMain()
        {
            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        public NoteObject GetNoteObject()
        {
            NoteObject note;
            if (noteObjectPool.Count > 0)
            {
                note = noteObjectPool.Pop();
            }
            else
            {
                note = Instantiate(noteObject);
            }

            note.transform.position = Vector3.one * 2f;
            note.gameObject.SetActive(true);
            note.enabled = true;
            return note;
        }

        public void ReturnNoteObjectToPool(NoteObject obj)
        {
            if (obj == null)
            {
                return;
            }

            obj.gameObject.SetActive(false);
            obj.enabled = false;
            noteObjectPool.Push(obj);
        }

        public GameObject GetEffectObject(Stack<GameObject> pool, GameObject effectObject)
        {
            GameObject effect;
            if (pool.Count > 0)
            {
                effect = pool.Pop();
            }
            else
            {
                effect = Instantiate(effectObject);
            }

            effect.SetActive(true);
            return effect;
        }

        public void ReturnEffectObjectToPool(GameObject obj, Stack<GameObject> pool)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetActive(false);
            pool.Push(obj);
        }

        private void ResetRuntimeState()
        {
            leadInTimeLeft = 0f;
            timeLeftToPlay = 0f;
            hasSessionEnded = false;
            isPauseState = false;
            isPlaying = false;
            gameStart = false;
            score = 0;
            comboNum = 0;
            maxCombo = 0;
            perfectCount = 0;
            greatCount = 0;
            missCount = 0;
            totalNoteCount = 0;
            hp = maxHp;
        }

        private void ResetSessionValues()
        {
            score = 0;
            comboNum = 0;
            maxCombo = 0;
            perfectCount = 0;
            greatCount = 0;
            missCount = 0;
            hp = maxHp;
        }

        private void ResetLaneStates()
        {
            for (int i = 0; i < noteLanes.Count; i++)
            {
                if (noteLanes[i] != null)
                {
                    noteLanes[i].ResetForNewSession();
                }
            }
        }

        private void ClearAllSpawnedNotes()
        {
            // 当前工程里，所有已生成但尚未结算的音符都挂在各自 Lane 的队列中，
            // 结束会话时统一清空，避免第二次开局继续沿用上一局的残留状态。
            ResetLaneStates();
        }

        private void ResetPlaybackState()
        {
            // 二次开始前要把播放器和 AudioSource 一起归零，
            // 否则会出现“判定从头开始，但音乐从中间继续播”的错位。
            leadInTimeLeft = 0f;
            timeLeftToPlay = 0f;

            StopPlayback(resetAudioTime: true);

            if (simpleMusicPlayer != null && koreographyAsset != null)
            {
                simpleMusicPlayer.LoadSong(koreographyAsset, 0, false);
            }
        }

        private void CachePlayers()
        {
            if (musicPlayer != null)
            {
                simpleMusicPlayer = musicPlayer.GetComponent<SimpleMusicPlayer>();
            }

            if (simpleMusicPlayer == null)
            {
                simpleMusicPlayer = GetComponentInChildren<SimpleMusicPlayer>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (simpleMusicPlayer != null && koreographyAsset != null)
            {
                simpleMusicPlayer.LoadSong(koreographyAsset, 0, false);
            }
        }

        private void BindLanes()
        {
            for (int i = 0; i < noteLanes.Count; i++)
            {
                if (noteLanes[i] != null)
                {
                    noteLanes[i].Initialize(this);
                }
            }
        }

        private void LoadKoreographyData()
        {
            if (Koreographer.Instance == null)
            {
                Debug.LogError("RhythmSessionController: Koreographer instance is not available.");
                enabled = false;
                return;
            }

            playingKoreo = koreographyAsset != null ? koreographyAsset : Koreographer.Instance.GetKoreographyAtIndex(0);
            if (playingKoreo == null)
            {
                Debug.LogError("RhythmSessionController: no Koreography asset is loaded.");
                enabled = false;
                return;
            }

            hitWindowRangeInSamples = Mathf.RoundToInt(0.001f * hitWindowRangeInMS * SampleRate);

            KoreographyTrack rhythmTrack = playingKoreo.GetTrackByID(eventID);
            if (rhythmTrack == null)
            {
                Debug.LogError($"RhythmSessionController: track '{eventID}' was not found.");
                enabled = false;
                return;
            }

            List<KoreographyEvent> rawEvents = rhythmTrack.GetAllEvents();
            totalNoteCount = rawEvents.Count;
            for (int i = 0; i < rawEvents.Count; i++)
            {
                int noteID = rawEvents[i].GetIntValue();

                for (int j = 0; j < noteLanes.Count; j++)
                {
                    if (noteLanes[j] == null)
                    {
                        continue;
                    }

                    int laneId = noteID;
                    if (laneId > 6)
                    {
                        laneId -= 6;
                        if (laneId > 6)
                        {
                            laneId -= 6;
                        }
                    }

                    if (noteLanes[j].IsIDMatched(laneId))
                    {
                        noteLanes[j].AddEventToLane(rawEvents[i]);
                        break;
                    }
                }
            }
        }

        private void InitializeLeadInTime()
        {
            if (leadInTime > 0f)
            {
                leadInTimeLeft = leadInTime;
                timeLeftToPlay = leadInTime;
                return;
            }

            leadInTimeLeft = 0f;
            timeLeftToPlay = 0f;
            BeginPlayback();
        }

        private void BeginPlayback()
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }

            if (simpleMusicPlayer != null && !simpleMusicPlayer.IsPlaying)
            {
                simpleMusicPlayer.Play();
            }

            gameStart = true;
            isPauseState = false;
            PauseChanged?.Invoke(false);
        }

        private bool HasPlaybackFinished()
        {
            if (simpleMusicPlayer != null)
            {
                return !simpleMusicPlayer.IsPlaying;
            }

            if (audioSource != null)
            {
                return !audioSource.isPlaying;
            }

            return false;
        }

        private void StopPlayback(bool resetAudioTime)
        {
            if (simpleMusicPlayer != null)
            {
                simpleMusicPlayer.Stop();
            }

            if (audioSource != null)
            {
                audioSource.Stop();
                if (resetAudioTime)
                {
                    audioSource.time = 0f;
                }
            }
        }

        private void UpdateNormalizedScore()
        {
            if (totalNoteCount <= 0)
            {
                score = 0;
                return;
            }

            float weightedHits = perfectCount + greatCount * greatWeight;
            score = Mathf.Clamp(Mathf.RoundToInt(maxScore * weightedHits / totalNoteCount), 0, maxScore);
        }
    }
}
