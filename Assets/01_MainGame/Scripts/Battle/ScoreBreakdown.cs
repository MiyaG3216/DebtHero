using UnityEngine;

public class ScoreBreakdown
{
    // ===== フィールド =====
    public string HandName;         // 役名
    public int HandBaseScore;       // 役の基礎点
    public int UnusedDiceScore;     // 不使用ダイス合計
    public int SealAddScore;        // シール加算点（鉄・雷）
    public int TotalBaseScore;      // 基礎点の小計
    public float Multiplier;        // シールによる総合倍率
    public string MultiplierDetails;// 倍率の内分け
    public string BossTraitText;    // ボス特性による補正
    public int FinalScore;          // 最終スコア
    public int EarnedCoins;         // 獲得コイン
}
