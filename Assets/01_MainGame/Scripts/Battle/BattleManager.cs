using UnityEngine;

public class BattleManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;
    [SerializeField] private BattleUIManager _battleUIManager;
    [SerializeField] private RewardManager _rewardManager;
    [SerializeField] private CheatingManager _cheatingManager;

    [Header("ステージ設定")]
    [SerializeField] private int _targetQuota = 300;    // 目標スコア
    [SerializeField] private int _maxTurn = 3;          // 制限ターン
    [SerializeField] private BossTraitType _currentBossTrait = BossTraitType.None;  // 現在の敵の特性

    [Header("モニター用")]
    public int CurrentScore = 0;           // 現在の合計スコア
    public int RemainingTurns = 0;         // 残りターン数
    public bool IsBattleActive = false;    // バトル中かどうか

    private ScoreBreakdown _lastBreakdown;  // 最後に提出された内訳

    // ===== プロパティ =====
    public int TargetQuota => _targetQuota;
    public int MaxTurn => _maxTurn;

    // ===== Unityメッセージ =====
    private void Start()
    {
        // GameDataManagerから現在のステージデータを読み込む
        if (GameDataManager.Instance != null)
        {
            StageData stage = GameDataManager.Instance.GetCurrentStageData();

            // Stage 1開始時は先にシール貼り付けを開く
            if(GameDataManager.Instance.CurrentStageIndex == 0 && !GameDataManager.Instance.HasSetupInitialSeals)
            {
                if(stage != null)
                {
                    _targetQuota = stage.TargetQuota;
                    _maxTurn = stage.MaxTurns;
                    _currentBossTrait = stage.BossTrait;
                    CurrentScore = 0;
                    RemainingTurns = _maxTurn;

                    if(_battleUIManager != null)
                    {
                        _battleUIManager.SetupStageInfo(stage.StageName, _currentBossTrait);
                    }
                }

                if(_rewardManager != null)
                {
                    _rewardManager.PrepareRewardSeals();

                    var rewardUI = FindAnyObjectByType<RewardUIManager>();
                    if(rewardUI != null)
                    {
                        rewardUI.OpenInitialSealUI();

                        return;
                    }
                }
            }

            if (stage != null)
            {
                BossTraitType trait = stage.BossTrait;

                if (GameDataManager.Instance.CurrentStageIndex == GameDataManager.Instance.StageRoute.Count - 1)
                {
                    trait = GameDataManager.Instance.CurrentFinalBossTrait;
                }

                StartBattle(stage.TargetQuota, stage.MaxTurns, trait, stage.StageName);

                return;
            }
        }

        // 単体テスト用のフォールバック
        StartBattle(_targetQuota, _maxTurn, _currentBossTrait, "取り立て屋");
    }

    // ===== メソッド =====
    public void StartBattle(int quota, int turns, BossTraitType bossTrait = BossTraitType.None, string stageName = "取り立て屋")
    {
        _targetQuota = quota;
        _maxTurn = turns;
        _currentBossTrait = bossTrait;
        CurrentScore = 0;
        RemainingTurns = _maxTurn;
        IsBattleActive = true;

        if (_cheatingManager != null) _cheatingManager.ResetCheating();

        // UIに適名と特性を反映
        if (_battleUIManager != null)
        {
            _battleUIManager.SetupStageInfo(stageName, _currentBossTrait);
        }

        _diceManager.StartNewTurn();
    }

    // 役確定ボタンが押されたとき：詳細内訳を計算してパネルを開く
    public void OnHandSubmitted(HandEvaluationResult handResult)
    {
        if (!IsBattleActive || handResult == null) return;

        // シール効果計算
        var calcResult = SealCalculator.Calculate(handResult, _diceManager.DiceList,RemainingTurns);

        // ボス特性によるスコア補正
        int finalScore = BossManager.Instance.ApplyBossTrait(
            _currentBossTrait,
            handResult,
            calcResult.FinalScore,
            calcResult.DisableBossPassive);

        // 全内訳データを合成
        _lastBreakdown = new ScoreBreakdown
        {
            HandName = handResult.HandName,
            HandBaseScore = calcResult.HandBaseScore,
            UnusedDiceScore = calcResult.UnusedDiceScore,
            SealAddScore = calcResult.SealAddScore,
            TotalBaseScore = calcResult.TotalBaseScore,
            Multiplier = calcResult.TotalMultiplier,
            BossTraitText = GetBossModifierText(_currentBossTrait, handResult, calcResult.DisableBossPassive),
            FinalScore = finalScore,
            EarnedCoins = calcResult.EarnedCoins
        };

        // UIに詳細パネルを表示させる
        if (_battleUIManager != null)
        {
            _battleUIManager.ShowScoreDetailPanel(_lastBreakdown);
        }
    }

    // プレイヤーがリザルトパネルの「次へ進む」ボタンを押したときに呼ばれる
    public void ConfirmScoreAndAdvanceTurn()
    {
        if (_lastBreakdown == null) return;

        // コイン付与
        if (_lastBreakdown.EarnedCoins > 0)
        {
            if (GameDataManager.Instance != null) GameDataManager.Instance.Coins += _lastBreakdown.EarnedCoins;
        }

        // スコア加算とターン消費
        CurrentScore += _lastBreakdown.FinalScore;
        RemainingTurns--;

        // 勝敗判定
        if (CurrentScore >= _targetQuota)
        {
            // ノルマ達成
            IsBattleActive = false;

            bool isFinalBoss = false;
            if (GameDataManager.Instance != null)
            {
                isFinalBoss = (GameDataManager.Instance.CurrentStageIndex == GameDataManager.Instance.StageRoute.Count - 1);
            }

            bool missionCleared = RemainingTurns >= 1;
            int baseCoins = 0;
            int bonusCoins = 0;

            if (!isFinalBoss)
            {
                baseCoins = 5;
                bonusCoins = missionCleared ? 3 : 0;
                if (GameDataManager.Instance != null) GameDataManager.Instance.Coins += (baseCoins + bonusCoins);
            }

            // リザルトパネルを表示
            if (_battleUIManager != null) _battleUIManager.ShowBattleResultPanel(true, baseCoins, bonusCoins, missionCleared,isFinalBoss);
        }
        else if (RemainingTurns <= 0)
        {
            // 残りターン無し
            IsBattleActive = false;
            if (_battleUIManager != null) _battleUIManager.ShowBattleResultPanel(false, 0, 0, false);
        }
        else
        {
            _diceManager.StartNewTurn();
        }

        _lastBreakdown = null;
    }

    private string GetBossModifierText(BossTraitType trait, HandEvaluationResult handResult, bool isIceActive)
    {
        if (_currentBossTrait == BossTraitType.None || handResult == null) return "";

        if (isIceActive) return "氷シールで特性無効化！";

        switch (trait)
        {
            case BossTraitType.Fee75Percent:
                return "<color=red>みかじめ料 (× 75 %)</color>";

            case BossTraitType.DisableEven:
                if (handResult.UsedDice.Exists(x => x % 2 == 0)) return "<color=red>偶数無効 ( 0 G )</color>";
                break;

            case BossTraitType.DisableOdd:
                if (handResult.UsedDice.Exists(x => x % 2 != 0)) return "<color=red>奇数無効 ( 0 G )</color>";
                break;

            case BossTraitType.DisableHighHand:
                if (handResult.HandType == HandType.FullHouse ||
                    handResult.HandType == HandType.Straight ||
                    handResult.HandType == HandType.FiveDice) return "<color=red>強役つぶし ( 0 G )</color>";
                break;

            case BossTraitType.HalfPair:
                if (handResult.HandType != HandType.Straight) return "<color=red>ペア半減 (× 50 %)</color>";
                break;
        }

        return "";
    }

    public void StartFirstStageBattle()
    {
        if(GameDataManager.Instance != null)
        {
            StageData stage = GameDataManager.Instance.GetCurrentStageData();

            if(stage != null)
            {
                StartBattle(stage.TargetQuota, stage.MaxTurns, stage.BossTrait, stage.StageName);

                return;
            }
        }
        StartBattle(300, 3, BossTraitType.None, "取り立て屋");
    }
}
