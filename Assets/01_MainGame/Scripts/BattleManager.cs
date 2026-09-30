using UnityEngine;

public class BattleManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;

    [Header("ステージ設定")]
    [SerializeField] private int _targetQuota = 300;    // 目標スコア
    [SerializeField] private int _maxTurn = 3;          // 制限ターン

    [Header("モニター用")]
    public int _currentScore = 0;           // 現在の合計スコア
    public int _remainingTurns = 0;         // 残りターン数
    public bool _isBattleActive = false;    // バトル中かどうか

    // ===== プロパティ =====

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

        Debug.Log("====================");
        Debug.Log($"【取り立て屋 出現！】");
        Debug.Log($"【返済ノルマ】：{_targetQuota} G  | 制限ターン：{_remainingTurns} 回");
        Debug.Log("====================");

        _diceManager.StartNewTurn();
    }

    public void OnHandSubmitted(HandEvaluationResult handResult)
    {
        if (!_isBattleActive || handResult == null) return;

        // 獲得スコア
        int earnedScore = handResult.BaseScore + handResult.UnusedDiceSum;

        _currentScore += earnedScore;
        _remainingTurns--;

        Debug.Log("--------------------");
        Debug.Log($"【提出結果】 役：{handResult.HandName} → 獲得金額：+{earnedScore}G");
        Debug.Log($"【返済状況】：{_currentScore} / {_targetQuota} G  | 残りターン：{_remainingTurns} 回");
        Debug.Log("--------------------");

        // 勝敗判定
        if(_currentScore >= _targetQuota)
        {
            // ノルマ達成
            _isBattleActive =false;
            Debug.Log("$$$$$$$$$$$$$$$$$$$$");
            Debug.Log($"【完済成功！！STAGE CLEAR】");
            Debug.Log($"取り立て屋を追い返した！(Spaceキーで次の挑戦を開始)");
            Debug.Log("$$$$$$$$$$$$$$$$$$$$");
        }
        else if (_remainingTurns <= 0)
        {
            // 残りターン無し
            _isBattleActive = false;
            Debug.Log("XXXXXXXXXXXXXXXXXXXX");
            Debug.Log($"【完済失敗... GAME OVER】");
            Debug.Log($"借金を返せなかった...(Spaceキーでリトライ)");
            Debug.Log("XXXXXXXXXXXXXXXXXXXX");
        }
        else
        {
            Debug.Log(">> ノルマ未達成！次のターンを開始します。");
            _diceManager.StartNewTurn();
        }
    }
}
