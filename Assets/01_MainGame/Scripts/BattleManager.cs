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

    [Header("モニター用")]
    public int _currentScore = 0;           // 現在の合計スコア
    public int _remainingTurns = 0;         // 残りターン数
    public bool _isBattleActive = false;    // バトル中かどうか

    // ===== プロパティ =====
    public int TargetQuota => _targetQuota;
    public int MaxTurn => _maxTurn;

    // ===== Unityメッセージ =====
    private void Start()
    {
        StartChallange(_targetQuota, _maxTurn);
    }

    // ===== メソッド =====
    public void StartChallange(int quota, int turns)
    {
        _targetQuota = quota;
        _maxTurn = turns;
        _currentScore = 0;
        _remainingTurns = _maxTurn;
        _isBattleActive = true;

        if(_cheatingManager != null) _cheatingManager.ResetCheating();

        _diceManager.StartNewTurn();
    }

    public void OnHandSubmitted(HandEvaluationResult handResult)
    {
        if (!_isBattleActive || handResult == null) return;

        // シール効果を含めた最終スコアと獲得コインを計算
        var calcResult = SealCalculator.Calculate(handResult, _diceManager._diceList);

        // 獲得スコア
        int earnedScore = calcResult.FinalScore;

        _currentScore += earnedScore;
        _remainingTurns--;

        Debug.Log($"提出：役[{handResult.HandName}] → {calcResult.SummaryText}");

        // 勝敗判定
        if (_currentScore >= _targetQuota)
        {
            // ノルマ達成
            _isBattleActive = false;
            if (_battleUIManager != null) _battleUIManager.ShowBattleResult(true);

            // サブミッション判定(残りターン1以上で達成)
            bool missinonCleared = _remainingTurns >= 1;

            // 報酬フェーズを開始
            if(_rewardManager != null)
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
    }
}
