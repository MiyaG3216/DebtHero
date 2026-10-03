using UnityEngine;
using System.Collections.Generic;
using System;

public enum SealType
{
    None,       // なし
    Fire,       // 炎（倍率UP）
    Ice,        // 氷（特性無効）
    Gold,       // 金箔（コイン獲得）
    Iron,       // 鉄（基礎スコア加算）
    Lightning,  // 雷（3ダイス以上のゾロ目強化）
    Solo,       // 孤高（役に不使用の場合3倍）
    Odd,        // 奇数（奇数のみ倍率UP）
    Even,       // 偶数（偶数のみ倍率UP）
    Gamble      // 博打（役アリ1.5倍/役無し0倍）
}

public static class SealCalculator
{
    // ===== フィールド =====
    // 重ね掛けボーナス定義
    private static readonly float[] FireMultipliers = { 1.0f, 1.2f, 1.5f, 2.0f, 2.5f, 3.0f };
    private static readonly int[] GoldCoins = { 0, 1, 3, 5, 7, 10 };
    private static readonly int[] IronAddScores = { 0, 50, 120, 210, 320, 450 };
    private static readonly int[] LightningAddScores = { 0, 100, 220, 360, 520, 700 };
    private static readonly float[] OddMultipliers = { 1.0f, 1.3f, 1.8f, 2.5f, 3.2f, 4.1f };
    private static readonly float[] EvenMultipliers = { 1.0f, 1.3f, 1.8f, 2.5f, 3.2f, 4.1f };

    // シール効果を反映した最終スコアと獲得コインを計算するクラス
    public class CalculationResult
    {
        public int FinalScore;
        public int EarnedCoins;
        public bool DisableBossPassive;
        public string SummaryText;
    }

    // ===== メソッド =====
    public static CalculationResult Calculate(HandEvaluationResult handResult, List<Dice> diceList)
    {
        var result = new CalculationResult();

        // 今回の5つの出目に貼られていたシールを取得
        var activeSeals = new List<SealType>();
        var sealCounts = new Dictionary<SealType, int>();
        foreach (SealType type in Enum.GetValues(typeof(SealType)))
        {
            sealCounts[type] = 0;
        }

        for (int i = 0; i < diceList.Count; i++)
        {
            var dice = diceList[i];
            SealType sealOnFace = dice.GetSealOnFace(dice.Value);
            if (sealOnFace != SealType.None)
            {
                activeSeals.Add(sealOnFace);
                sealCounts[sealOnFace]++;
            }
        }

        // 基礎スコアの計算
        int baseScore = handResult.BaseScore;
        int unusedDiceSum = 0;

        // 孤高と博打シールの適応
        for (int i = 0; i < diceList.Count; i++)
        {
            var dice = diceList[i];
            SealType seal = dice.GetSealOnFace(dice.Value);

            bool isUnused = handResult.UnusedDice.Contains(dice.Value);

            if (isUnused)
            {
                int dicePoint = dice.Value;
                if (seal == SealType.Solo)
                {
                    dicePoint += 10; // 孤高シール出目+10
                }

                unusedDiceSum += dicePoint;
            }
        }

        int totalBaseScore = baseScore + unusedDiceSum;

        // 加算系シール（鉄・雷）の適応
        int ironCount = Mathf.Min(sealCounts[SealType.Iron], 5);
        totalBaseScore += IronAddScores[ironCount];

        int lightningCount = Mathf.Min(sealCounts[SealType.Lightning], 5);
        if (handResult.HandType == HandType.ThreeDice ||
            handResult.HandType == HandType.FourDice ||
            handResult.HandType == HandType.FiveDice)
        {
            totalBaseScore += LightningAddScores[lightningCount];
        }

        // 乗算系シール（炎・奇数・偶数・博打）の適応
        float totalMultiplier = 1.0f;

        // 炎シール
        int fireCount = Mathf.Min(sealCounts[SealType.Fire], 5);
        totalMultiplier *= FireMultipliers[fireCount];

        // 奇数シール
        int oddCount = 0;
        foreach (var dice in diceList)
        {
            if (dice.GetSealOnFace(dice.Value) == SealType.Odd && (dice.Value % 2 != 0))
            {
                oddCount++;
            }
        }
        totalMultiplier *= OddMultipliers[Mathf.Min(oddCount, 5)];

        // 偶数シール
        int evenCount = 0;
        foreach (var dice in diceList)
        {
            if (dice.GetSealOnFace(dice.Value) == SealType.Even && (dice.Value % 2 == 0))
            {
                evenCount++;
            }
        }
        totalMultiplier *= EvenMultipliers[Mathf.Min(evenCount, 5)];

        // 博打シール
        if (sealCounts[SealType.Gamble] > 0)
        {
            if (handResult.HandType != HandType.NoHand)
            {
                totalMultiplier *= 1.5f;    // 役アリなら1.5倍
            }
            else
            {
                totalMultiplier = 0.0f;     // 役ナシなら0倍
            }
        }

        // その他シール（金箔・氷）の適応
        int goldCount = Mathf.Min(sealCounts[SealType.Gold], 5);
        result.EarnedCoins += GoldCoins[goldCount];
        result.DisableBossPassive = sealCounts[SealType.Ice] > 0;

        // 最終スコア産出
        result.FinalScore = Mathf.RoundToInt(totalBaseScore * totalMultiplier);

        result.SummaryText = $"基礎スコア({totalBaseScore}) × 倍率({totalMultiplier:F1}) = {result.FinalScore} G";
        if (result.EarnedCoins > 0) result.SummaryText += $"[+{result.EarnedCoins}コイン]";
        if (result.DisableBossPassive) result.SummaryText += $"[ボス特性無効]";

        return result;
    }
}
