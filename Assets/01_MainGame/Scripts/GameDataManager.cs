using UnityEngine;
using System.Collections.Generic;

public class GameDataManager : MonoBehaviour
{
    // ===== フィールド =====
    public static GameDataManager _instance;

    [Header("所持コイン")]
    public int Coins = 5;

    [Header("ダイス編成")]
    public List<Dice> PlayerDiceList = new List<Dice>();

    [Header("進行度")]
    public int CurrentStageIndex = 0;

    [Header("パッシブアイテム")]
    public List<PassiveType> AcquiredPassive = new List<PassiveType>();

    public BossTraitType CurrentFinalBossTrait = BossTraitType.None;

    public bool HasSetupInitialSeals = false;  // 初期シール貼り終えたかフラグ

    public readonly List<StageData> StageRoute = new List<StageData>()
    {
        // Stage 1
        new StageData { StageName = "取り立て屋のしたっぱ", Type = StageType.Battle, TargetQuota = 300, MaxTurns = 3, BossTrait = BossTraitType.None },
        // Stage 2
        new StageData { StageName = "ベテラン取り立て屋", Type = StageType.Battle, TargetQuota = 600, MaxTurns = 3, BossTrait = BossTraitType.None },
        // Shop 1
        new StageData { StageName = "カジノショップ", Type = StageType.Shop },
        // Stage 3 (中ボス)     
        new StageData { StageName = "用心棒【中ボス】", Type = StageType.Battle, TargetQuota = 1000, MaxTurns = 4, BossTrait = BossTraitType.Fee75Percent },
        // Stage 4
        new StageData { StageName = "エリート取り立て屋", Type = StageType.Battle, TargetQuota = 2500, MaxTurns = 4, BossTrait = BossTraitType.None },
        // Stage 5
        new StageData { StageName = "取り立てロボ", Type = StageType.Battle, TargetQuota = 4000, MaxTurns = 5, BossTrait = BossTraitType.None },
        // Stage 6
        new StageData { StageName = "ボスの右腕【中ボス】",Type = StageType.Battle,TargetQuota = 8000,MaxTurns = 5,BossTrait = BossTraitType.Fee75Percent},
        //Shop 2
        new StageData { StageName = "裏カジノショップ", Type = StageType.Shop },
        // Stage 7 (ラスボス)
        new StageData { StageName = "魔王城ボス", Type = StageType.Battle, TargetQuota = 15000, MaxTurns = 6, BossTrait = BossTraitType.None }
    };

    // ===== プロパティ =====
    // どのシーンを再生しても自動生成される
    public static GameDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameDataManager>();

                if (_instance == null)
                {
                    GameObject obj = new GameObject("[PersistentGameManager]");
                    _instance = obj.AddComponent<GameDataManager>();
                    DontDestroyOnLoad(obj);
                }
            }

            return _instance;
        }
    }

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNewGameData();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // ===== メソッド =====
    public void InitializeNewGameData()
    {
        Coins = 1;
        CurrentStageIndex = 0;
        HasSetupInitialSeals = false;

        BossTraitType[] traits =
        {
            BossTraitType.DisableOdd,
            BossTraitType.DisableEven,
            BossTraitType.DisableHighHand,
            BossTraitType.HalfPair
        };
        CurrentFinalBossTrait = traits[Random.Range(0, traits.Length)];

        PlayerDiceList.Clear();
        AcquiredPassive.Clear();

        for (int i = 0; i < 5; i++)
        {
            PlayerDiceList.Add(new Dice(DiceType.Nomal));
        }
    }

    public StageData GetCurrentStageData()
    {
        if (CurrentStageIndex >= 0 && CurrentStageIndex < StageRoute.Count)
        {
            StageData stage = StageRoute[CurrentStageIndex];

            if (CurrentStageIndex == StageRoute.Count - 1) stage.BossTrait = CurrentFinalBossTrait;

            return stage;
        }
        return null;
    }

    public string GetBossHintText()
    {
        switch (CurrentFinalBossTrait)
        {
            case BossTraitType.DisableEven:
                return "本社のボスは【偶数】が含まれる役を【半減】らしい...";

            case BossTraitType.DisableOdd:
                return "本社のボスは【奇数】が含まれる役を【半減】らしい...";

            case BossTraitType.DisableHighHand:
                return "本社のボスは【大技】を【半減】してくるらしい...";

            case BossTraitType.HalfPair:
                return "本社のボスは【ペア系】の役を【65%】に押さえこんでしまうそうだ...";

            default:
                return "ボスに関する情報はまだない...";
        }
    }

    // パッシブアイテムを持っているか判定する
    public bool HasPassive(PassiveType type)
    {
        return AcquiredPassive != null && AcquiredPassive.Contains(type);
    }
}
