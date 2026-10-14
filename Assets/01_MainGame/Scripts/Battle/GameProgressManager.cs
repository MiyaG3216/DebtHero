using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum StageType
{
    Battle,
    Shop
}

// 1つのステージのデータ定義
[System.Serializable]
public class StageData
{
    public string StageName;        // 敵の名前/ショップ名 
    public StageType Type;          // ステージタイプ（バトルorショップ）
    public int TargetQuota;         // 目標スコア
    public int MaxTurns;            // 制限ターン
    public BossTraitType BossTrait; // ボス特性（中ボス、ラスボス以外はNone）
}

public class GameProgressManager : MonoBehaviour
{
    // ===== フィールド =====
    public static GameProgressManager Instance { get; private set; }

    [Header("参照")]
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private RewardManager _rewardManager;
    [SerializeField] private DiceManager _diceManager;

    [Header("全ステージの√定義")]
    [SerializeField] private List<StageData> _stageList = new List<StageData>();

    [Header("進行状況")]
    public int CurrentStageIndex = 0;
    public bool IsGameCleared = false;

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        SetUpDefaultStages();
    }

    // ===== メソッド =====
    private void SetUpDefaultStages()
    {
        _stageList.Clear();

        // Stage 1
        _stageList.Add(new StageData { StageName = "取り立て屋のしたっぱ", Type = StageType.Battle, TargetQuota = 200, MaxTurns = 3, BossTrait = BossTraitType.None });

        // Stage 2
        _stageList.Add(new StageData { StageName = "ベテラン取り立て屋", Type = StageType.Battle, TargetQuota = 400, MaxTurns = 3, BossTrait = BossTraitType.None });

        // Shop 1
        _stageList.Add(new StageData { StageName = "カジノショップ", Type = StageType.Shop });

        // Stage 3 (中ボス)     
        _stageList.Add(new StageData { StageName = "用心棒【中ボス】", Type = StageType.Battle, TargetQuota = 600, MaxTurns = 4, BossTrait = BossTraitType.Fee75Percent });

        // Stage 4
        _stageList.Add(new StageData { StageName = "エリート取り立て屋", Type = StageType.Battle, TargetQuota = 800, MaxTurns = 4, BossTrait = BossTraitType.None });

        // Stage 5
        _stageList.Add(new StageData { StageName = "取り立てロボ", Type = StageType.Battle, TargetQuota = 1200, MaxTurns = 5, BossTrait = BossTraitType.None });

        //Shop 2
        _stageList.Add(new StageData { StageName = "裏カジノショップ", Type = StageType.Shop });

        // Stage 6 (ラスボス)
        _stageList.Add(new StageData { StageName = "魔王城ボス", Type = StageType.Battle, TargetQuota = 2000, MaxTurns = 6, BossTrait = BossTraitType.None });
    }

    public void StartNewGame()
    {
        CurrentStageIndex = 0;
        IsGameCleared = false;

        // ラスボスの特性抽選
        BossManager.Instance.DecideFinalBossTrait();

        ExecuteCurrentStage();
    }

    public void ExecuteCurrentStage()
    {
        if (CurrentStageIndex >= _stageList.Count)
        {
            OnGameClear();
            return;
        }

        var currentStage = _stageList[CurrentStageIndex];

        if (currentStage.Type == StageType.Battle)
        {
            BossTraitType trait = currentStage.BossTrait;

            // ラスボス戦の場合は抽選された特性をセットする
            if (CurrentStageIndex == _stageList.Count - 1)
            {
                trait = BossManager.Instance.CurrentFinalBossTrait;
            }

            _battleManager.StartBattle(currentStage.TargetQuota, currentStage.MaxTurns, trait, currentStage.StageName);
        }
        else if (currentStage.Type == StageType.Shop)
        {
            SceneManager.LoadScene("Scene_Shop");
        }
    }

    // 現在のステージをやり直す（リトライ）
    public void RetryCurrentStage()
    {
        ExecuteCurrentStage();
    }

    // 最初からやり直す()
    public void RestartGame()
    {
        StartNewGame();
    }

    public void OnStageCompleted()
    {
        CurrentStageIndex++;
        ExecuteCurrentStage();
    }

    private void OnGameClear()
    {
        IsGameCleared = true;
    }
}
