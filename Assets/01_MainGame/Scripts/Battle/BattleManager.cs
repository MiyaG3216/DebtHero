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
        if (GameDataManager.Instance != null)
        {
            StageData stage = GameDataManager.Instance.GetCurrentStageData();
            if (stage != null)
            {
                bool isFinalBoss = (GameDataManager.Instance.CurrentStageIndex == GameDataManager.Instance.StageRoute.Count - 1);

                // ラスボス以外はミッションを生成してパネルを開く
                if (!isFinalBoss)
                {
                    _targetQuota = stage.TargetQuota;
                    _maxTurn = stage.MaxTurns;
                    _currentBossTrait  =stage.BossTrait;
                    CurrentScore = 0;
                    RemainingTurns = _maxTurn;

                    // UIにステージ情報を反映
                    if(_battleUIManager != null)
                    {
                        _battleUIManager.SetupStageInfo(stage.StageName, _currentBossTrait);
                        _battleUIManager.UpdateUI();
                    }

                    // ミッションを生成
                    if (MissionManager.Instance != null) MissionManager.Instance.GenerateMissions(stage.MaxTurns);

                    // ミッションパネルを開く
                    if (_battleUIManager != null)
                    {
                        _battleUIManager.OpenMissionPanel(() =>
                        {
                            // パネルを閉じた後の処理
                            if (GameDataManager.Instance.CurrentStageIndex == 0 && !GameDataManager.Instance.HasSetupInitialSeals)
                            {
                                // ステージ1 かつ 初期シール未設定なら初期シール画面を開く
                                OpenInitialSealFlow();
                            }
                            else
                            {
                                // それ以外ならバトル開始
                                StartBattle(stage.TargetQuota, stage.MaxTurns, stage.BossTrait, stage.StageName);
                            }
                        });
                        return;     // パネルが閉じるまで待機
                    }
                }

                // ラスボス戦
                StartBattle(stage.TargetQuota, stage.MaxTurns, stage.BossTrait, stage.StageName);
                return;
            }
        }
        // 単体テスト用のフォールバック
        StartBattle(_targetQuota, _maxTurn, _currentBossTrait, "取り立て屋");
    }

    // ===== メソッド =====
    private void OpenInitialSealFlow()
    {
        if (_rewardManager != null)
        {
            _rewardManager.PrepareRewardSeals();
            var rewardUI = FindAnyObjectByType<RewardUIManager>();
            if (rewardUI != null) rewardUI.OpenInitialSealUI();
        }
    }

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

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnHandSubmitted(handResult, _diceManager.DiceList);
        }

        // シール効果計算
        var calcResult = SealCalculator.Calculate(handResult, _diceManager.DiceList, RemainingTurns);

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

            bool isFinalBoss = GameDataManager.Instance != null &&
                               GameDataManager.Instance.CurrentStageIndex == GameDataManager.Instance.StageRoute.Count - 1;

            int baseCoins = 0;
            int missionBobus = 0;
            int passiveCoins = 0;

            // ラスボス戦以外でコイン獲得
            if (!isFinalBoss)
            {
                baseCoins = 5;

                // ミッションボーナス判定
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.CheckTrunMission(RemainingTurns);
                    missionBobus = MissionManager.Instance.GetTotalBonusCoins();
                }

                // パッシブ【金属探知機】
                if (GameDataManager.Instance != null &&
                    GameDataManager.Instance.HasPassive(PassiveType.MetalDerector))
                {
                    passiveCoins = 3;
                }

                // コイン付与
                if (GameDataManager.Instance != null) GameDataManager.Instance.Coins += (baseCoins + missionBobus + passiveCoins);
            }

            // リザルトパネルを表示
            if (_battleUIManager != null) _battleUIManager.ShowBattleResultPanel(true, baseCoins, missionBobus, passiveCoins, isFinalBoss);
        }
        else if (RemainingTurns <= 0)
        {
            // 残りターン無し
            IsBattleActive = false;
            if (_battleUIManager != null) _battleUIManager.ShowBattleResultPanel(false, 0, 0, 0, false);
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
                if (handResult.UsedDice.Exists(x => x % 2 == 0)) return "<color=red>偶数半減 (× 50 %)</color>";
                break;

            case BossTraitType.DisableOdd:
                if (handResult.UsedDice.Exists(x => x % 2 != 0)) return "<color=red>奇数半減 (× 50 %)</color>";
                break;

            case BossTraitType.DisableHighHand:
                if (handResult.HandType == HandType.FullHouse ||
                    handResult.HandType == HandType.Straight ||
                    handResult.HandType == HandType.FiveDice) return "<color=red>強役半減 (× 50 %)</color>";
                break;

            case BossTraitType.HalfPair:
                if (handResult.HandType != HandType.Straight) return "<color=red>ペア軽減 (× 70 %)</color>";
                break;
        }

        return "";
    }

    public void StartFirstStageBattle()
    {
        if (GameDataManager.Instance != null)
        {
            StageData stage = GameDataManager.Instance.GetCurrentStageData();

            if (stage != null)
            {
                StartBattle(stage.TargetQuota, stage.MaxTurns, stage.BossTrait, stage.StageName);

                return;
            }
        }
        StartBattle(300, 3, BossTraitType.None, "取り立て屋");
    }
}
