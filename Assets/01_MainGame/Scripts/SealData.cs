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
        public int HandBaseScore = 0;           // 役の基本スコア
        public int UnusedDiceScore = 0;         // 役に使わなかった目の合計
        public int SealAddScore = 0;            // シールによる基礎点加算
        public int TotalBaseScore = 0;          // 基礎点の小計
        public float TotalMultiplier = 1.0f;    // シール倍率総計
        public int FinalScore = 0;              // 最終スコア
        public int EarnedCoins = 0;             // 獲得コイン
        public bool DisableBossPassive = false; // 氷シール使用したかどうか
    }

    // ===== メソッド =====
    public static CalculationResult Calculate(HandEvaluationResult handResult, List<Dice> diceList)
    {
        var result = new CalculationResult();
        result.HandBaseScore = handResult.BaseScore;

        // 今回の5つの出目に貼られていたシールを取得
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
                sealCounts[sealOnFace]++;
            }
        }

        // 余りダイスの計算
        int unusedDiceSum = 0;
        if (handResult.UnusedDice != null)
        {
            foreach (int unusedVal in handResult.UnusedDice)
            {
                unusedDiceSum += unusedVal;
            }
        }

        // 孤高シールの適応
        for (int i = 0; i < diceList.Count; i++)
        {
            var dice = diceList[i];

            if (dice.GetSealOnFace(dice.Value) == SealType.Solo)
            {
                if (handResult.UnusedDice != null && handResult.UnusedDice.Contains(dice.Value))
                {
                    unusedDiceSum += 10;
                }
            }
        }
        result.UnusedDiceScore = unusedDiceSum;

        // 加算系シール（鉄・雷）の適応
        int ironCount = Mathf.Min(sealCounts[SealType.Iron], 5);
        int sealAdd = IronAddScores[ironCount];

        int lightningCount = Mathf.Min(sealCounts[SealType.Lightning], 5);
        if (handResult.HandType == HandType.OnePair ||
            handResult.HandType == HandType.TwoPair ||
            handResult.HandType == HandType.ThreeDice ||
            handResult.HandType == HandType.FullHouse ||
            handResult.HandType == HandType.FourDice ||
            handResult.HandType == HandType.FiveDice)
        {
            sealAdd += LightningAddScores[lightningCount];
        }

        result.SealAddScore = sealAdd;

        // 基礎点の小計
        result.TotalBaseScore = result.HandBaseScore + result.UnusedDiceScore + result.SealAddScore;

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
            totalMultiplier = (handResult.HandType != HandType.NoHand) ? totalMultiplier * 1.5f : 0.0f;
        }

        // その他シール（金箔・氷）の適応
        int goldCount = Mathf.Min(sealCounts[SealType.Gold], 5);
        result.EarnedCoins += GoldCoins[goldCount];
        result.DisableBossPassive = sealCounts[SealType.Ice] > 0;

        // 最終スコア産出
        result.FinalScore = Mathf.RoundToInt(result.TotalBaseScore * result.TotalMultiplier);

        return result;
    }
}
