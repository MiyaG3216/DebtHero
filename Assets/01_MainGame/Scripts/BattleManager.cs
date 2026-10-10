using TMPro.EditorUtilities;
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
    public int _currentScore = 0;           // 現在の合計スコア
    public int _remainingTurns = 0;         // 残りターン数
    public bool _isBattleActive = false;    // バトル中かどうか

    private ScoreBreakdown _lastBreakdown;  // 最後に提出された内訳

    // ===== プロパティ =====
    public int TargetQuota => _targetQuota;
    public int MaxTurn => _maxTurn;

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    public void StartBattle(string stageName, int quota, int turns, BossTraitType bossTrait = BossTraitType.None)
    {
        _targetQuota = quota;
        _maxTurn = turns;
        _currentBossTrait = bossTrait;
        _currentScore = 0;
        _remainingTurns = _maxTurn;
        _isBattleActive = true;

        if (_cheatingManager != null) _cheatingManager.ResetCheating();

        // UIに適名と特性を反映
        if (_battleUIManager != null)
        {
            string enemyName = (GameProgressManager.Instance != null) ? $"{stageName}" : "借金取り";
            _battleUIManager.SetupStageInfo(enemyName, _currentBossTrait);
        }

        _diceManager.StartNewTurn();
    }

    // 役確定ボタンが押されたとき：詳細内訳を計算してパネルを開く
    public void OnHandSubmitted(HandEvaluationResult handResult)
    {
        if (!_isBattleActive || handResult == null) return;

        // シール効果計算
        var calcResult = SealCalculator.Calculate(handResult, _diceManager._diceList);

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
            BossTraitText = GetBossModifierText(calcResult.DisableBossPassive),
            FinalScore = finalScore,
            EarnedCoins = calcResult.EarnedCoins
        };

        // UIに詳細パネルを表示させる
        if (_battleUIManager != null)
        {
            _battleUIManager.ShowScoreDetailPanel(_lastBreakdown);
        }

        Debug.Log($"[内訳チェック] 役点:{calcResult.HandBaseScore} / 余り:{calcResult.UnusedDiceScore} / シール加算:{calcResult.SealAddScore} / 小計:{calcResult.TotalBaseScore}");
    }

    // プレイヤーがリザルトパネルの「次へ進む」ボタンを押したときに呼ばれる
    public void ConfirmScoreAndAdvanceTurn()
    {
        if (_lastBreakdown == null) return;

        // コイン付与
        if (_lastBreakdown.EarnedCoins > 0)
        {
            PlayerManager.Instance.AddCoins(_lastBreakdown.EarnedCoins);
        }

        // スコア加算とターン消費
        _currentScore += _lastBreakdown.FinalScore;
        _remainingTurns--;

        // 勝敗判定
        if (_currentScore >= _targetQuota)
        {
            // ノルマ達成
            _isBattleActive = false;
            if (_battleUIManager != null) _battleUIManager.ShowBattleResult(true);

            // サブミッション判定(残りターン1以上で達成)
            bool missinonCleared = _remainingTurns >= 1;

            // 報酬フェーズを開始
            if (_rewardManager != null)
            {
                _rewardManager.StartRewardPhase(missinonCleared);
            }
        }
        else if (_remainingTurns <= 0)
        {
            // 残りターン無し
            _isBattleActive = false;
            if (_battleUIManager != null) _battleUIManager.ShowBattleResult(false);
        }
        else
        {
            _diceManager.StartNewTurn();
        }

        _lastBreakdown = null;
    }

    private string GetBossModifierText(bool isIceActive)
    {
        if (_currentBossTrait == BossTraitType.None) return "補正なし";

        if (isIceActive) return "氷シールで特性無効化！";

        switch (_currentBossTrait)
        {
            case BossTraitType.Fee75Percent: return "<color=red>みかじめ料 (× 75 %)</color>";
            case BossTraitType.DisableEven: return "<color=red>偶数無効 ( 0 G )</color>";
            case BossTraitType.DisableOdd: return "<color=red>奇数無効 ( 0 G )</color>";
            case BossTraitType.DisableHighHand: return "<color=red>強役つぶし ( 0 G )</color>";
            case BossTraitType.HalfPair: return "<color=red>ペア半減 (× 50 %)</color>";
            default: return "";
        }
    }
}
