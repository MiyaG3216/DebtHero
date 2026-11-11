using UnityEngine;
using System.Collections.Generic;
using System;

public enum SealType
{
    None,       // なし
    Blaze,      // ブレイズ（倍率UP）
    Frost,      // フロスト（特性無効+倍率）
    Treasure,   // トレジャー（コイン獲得）
    Legion,     // レギオン（基礎スコア加算）
    Spark,      // スパーク（3ダイス以上のゾロ目強化）
    Maverick,   // マーベリック（ダイスに孤高シールのみで2倍）
    Symmetry,   // シンメトリー（偶数のみ倍率UP）
    Asymmetry,  // アシンメトリー（奇数のみ倍率UP）
    Gamble      // ギャンブル（役アリ1.5倍/役無し0倍）
}

public static class SealCalculator
{
    // ===== フィールド =====
    // 重ね掛けボーナス定義
    private static readonly float[] BlazeMultipliers = { 1.0f, 1.2f, 2.4f, 3.6f, 4.8f, 6.0f };
    private static readonly float[] FrostMultipliers = { 1.0f, 1.1f, 1.21f, 1.34f, 1.47f, 1.61f };
    private static readonly float[] AsymmetryMultipliers = { 1.0f, 1.3f, 1.8f, 2.5f, 3.2f, 4.1f };
    private static readonly float[] SymmetryMultipliers = { 1.0f, 1.3f, 1.8f, 2.5f, 3.2f, 4.1f };
    private static readonly int[] TreasureCoins = { 0, 1, 3, 5, 7, 10 };
    private static readonly int[] SparkAddScores = { 0, 100, 220, 360, 520, 700 };

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
        public bool DisableBossPassive = false; // フロストシール判定
        public bool IsSoloTriggers = false;     // マーベリックシール判定
    }

    // ===== メソッド =====
    public static CalculationResult Calculate(HandEvaluationResult handResult, List<Dice> diceList, int remainingTurns = 0)
    {
        var result = new CalculationResult();

        if (handResult == null || diceList == null) return result;

        // 中ボス戦後は基礎スコアが2倍
        int handScore = handResult.BaseScore;
        if (GameDataManager.Instance != null && GameDataManager.Instance.HasPassive(PassiveType.GoldCard))
        {
            // ゴールドカードを持っていれば役スコア2倍
            handScore *= 2;
        }
        result.HandBaseScore = handScore;

        // 今回の5つの出目に貼られていたシールを取得
        var sealCounts = new Dictionary<SealType, int>();
        foreach (SealType type in Enum.GetValues(typeof(SealType)))
        {
            sealCounts[type] = 0;
        }

        for (int i = 0; i < diceList.Count; i++)
        {
            SealType seal = diceList[i].GetSealOnFace(diceList[i].Value);
            if (seal != SealType.None)
            {
                sealCounts[seal]++;
            }
        }

        // 余りダイスの計算
        int unusedDiceSum = 0;
        if (handResult.UnusedDice != null)
        {
            foreach (int val in handResult.UnusedDice)
            {
                unusedDiceSum += val;
            }
        }
        result.UnusedDiceScore = unusedDiceSum;

        // 加算系シールの適応
        // レギオンシール
        int totalLegionOnAllDice = 0;
        int addScore = 50;
        foreach(var dice in diceList)
        {
            for (int f = 1; f < 6; f++)
            {
                if(dice.GetSealOnFace(f) == SealType.Legion)
                {
                    totalLegionOnAllDice++;
                }
            }
        }
        int activeLgionCount = sealCounts[SealType.Legion];
        int LegionAdd = (totalLegionOnAllDice * addScore) * activeLgionCount;

        // スパークシール
        int sparkCount = 0;
        if (handResult.HandType != HandType.NoHand && handResult.HandType != HandType.Straight)
        {
            // 役に使われた出目をコピー
            var remainingUseDice = new List<int>(handResult.UsedDice);

            for (int i = 0; i < diceList.Count; i++)
            {
                var dice = diceList[i];

                // このダイスが役の役に使われているかをチェック
                if (remainingUseDice.Contains(dice.Value))
                {
                    remainingUseDice.Remove(dice.Value);

                    // かつ、その出目に雷シールが貼られていればカウント
                    if (dice.GetSealOnFace(dice.Value) == SealType.Spark)
                    {
                        sparkCount++;
                    }
                }
            }
        }
        int sparkAdd = SparkAddScores[Mathf.Min(sparkCount, 5)];

        // 加算合計
        result.SealAddScore = LegionAdd + sparkAdd;

        // 基礎点の小計
        result.TotalBaseScore = result.HandBaseScore + result.UnusedDiceScore + result.SealAddScore;

        // 乗算系シールの適応
        float totalMultiplier = 1.0f;

        // ブレイズシール
        totalMultiplier *= BlazeMultipliers[Mathf.Min(sealCounts[SealType.Blaze], 5)];

        // フロストシール
        totalMultiplier *= FrostMultipliers[Mathf.Min(sealCounts[SealType.Frost], 5)];

        // アシンメトリーシール
        int AsymmetryCount = 0;
        foreach (var dice in diceList)
        {
            if (dice.GetSealOnFace(dice.Value) == SealType.Asymmetry && (dice.Value % 2 != 0))
            {
                AsymmetryCount++;
            }
        }
        totalMultiplier *= AsymmetryMultipliers[Mathf.Min(AsymmetryCount, 5)];

        // シンメトリーシール
        int SymmetryCount = 0;
        foreach (var dice in diceList)
        {
            if (dice.GetSealOnFace(dice.Value) == SealType.Symmetry && (dice.Value % 2 == 0))
            {
                SymmetryCount++;
            }
        }
        totalMultiplier *= SymmetryMultipliers[Mathf.Min(SymmetryCount, 5)];

        // ギャンブルシール
        int gambleCount = 0;
        bool isGambleBusted = false;

        if (handResult.HandType == HandType.NoHand)
        {
            for (int i = 0; i < diceList.Count; i++)
            {
                if (diceList[i].GetSealOnFace(diceList[i].Value) == SealType.Gamble)
                {
                    // 役なしかつ博打シールが提出された場合バースト判定（スコア0）
                    isGambleBusted = true;
                    break;
                }
            }
        }
        else
        {
            // 役ありの場合、役使用ダイスと不使用ダイスを仕分け
            var remainingDice = new List<int>(handResult.UsedDice);

            for (int i = 0; i < diceList.Count; i++)
            {
                // 各ダイスの貼られているシールをチェック
                var dice = diceList[i];
                SealType seal = dice.GetSealOnFace(dice.Value);

                if (seal == SealType.Gamble)
                {
                    if (remainingDice.Contains(dice.Value))
                    {
                        // 貼られているのがギャンブルシールかつ使用ダイスリストに含まれているならリストから削除
                        remainingDice.Remove(dice.Value);
                        gambleCount++;
                    }
                    else
                    {
                        // 使用ダイスリストに含まれなければバースト
                        isGambleBusted = true;
                    }
                }
            }
        }

        // ギャンブルシールの適応
        if (isGambleBusted)
        {
            totalMultiplier *= 0.5f;
        }
        else if (gambleCount > 0)
        {
            totalMultiplier *= 2.5f;
        }

        // マーベリックシール
        bool maverickTriggered = false;
        foreach (var dice in diceList)
        {
            // 孤高シールが出目になっているダイスをチェック
            if (dice.GetSealOnFace(dice.Value) == SealType.Maverick)
            {
                // そのダイスの3面全てチェック
                bool isPureMaverick = true;
                int maverickFaceCount = 0;
                for (int f = 0; f < 6; f++)
                {
                    SealType s = dice.GetSealOnFace(f);
                    if (s == SealType.Maverick) maverickFaceCount++;
                    else if (s != SealType.None) isPureMaverick = false;
                }

                if (isPureMaverick && maverickFaceCount > 0)
                {
                    maverickTriggered = true;
                    break;
                }
            }
        }

        if (maverickTriggered)
        {
            totalMultiplier *= 2.0f;
        }
        result.IsSoloTriggers = maverickTriggered;

        // フルハウス成立時の効果適応
        if(handResult.HandType == HandType.FullHouse)
        {
            totalMultiplier *= 1.5f;
        }

        // ストレート成立時の効果適応
        if(handResult.HandType == HandType.Straight)
        {
            totalMultiplier *= 2.0f;
        }

        // パッシブ（督促状）の適応
        if (GameDataManager.Instance != null && GameDataManager.Instance.HasPassive(PassiveType.FinalNotice))
        {
            if (remainingTurns == 1) totalMultiplier *= 3.0f;
        }

        // パッシブ（預金通帳）の適応
        if(GameDataManager.Instance!= null && GameDataManager.Instance.HasPassive(PassiveType.PassBook))
        {
            int coins = GameDataManager.Instance.Coins;
            float bonus = (coins / 2) * 0.1f;
            totalMultiplier *= (1.0f + bonus);
        }

        result.TotalMultiplier = totalMultiplier;

        // その他シール（金箔・氷）の適応
        int goldCount = Mathf.Min(sealCounts[SealType.Treasure], 5);

        result.EarnedCoins = TreasureCoins[goldCount];
        result.DisableBossPassive = sealCounts[SealType.Frost] > 0;

        // ストレート成立時のコイン獲得
        if(handResult.HandType == HandType.Straight)
        {
            result.EarnedCoins += 3;
        }

        // 最終スコア産出
        result.FinalScore = Mathf.RoundToInt(result.TotalBaseScore * result.TotalMultiplier);

        return result;
    }
}
