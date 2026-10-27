using UnityEngine;
using System.Collections.Generic;
using System;

public enum SealType
{
    None,       // なし
    Fire,       // 炎（倍率UP）
    Ice,        // 氷（特性無効+倍率）
    Gold,       // 金箔（コイン獲得）
    Iron,       // 鉄（基礎スコア加算）
    Lightning,  // 雷（3ダイス以上のゾロ目強化）
    Solo,       // 孤高（ダイスに孤高シールのみで2倍）
    Odd,        // 奇数（奇数のみ倍率UP）
    Even,       // 偶数（偶数のみ倍率UP）
    Gamble      // 博打（役アリ1.5倍/役無し0倍）
}

public static class SealCalculator
{
    // ===== フィールド =====
    // 重ね掛けボーナス定義
    private static readonly float[] FireMultipliers = { 1.0f, 1.2f, 2.4f, 3.6f, 4.8f, 6.0f };
    private static readonly float[] IceMultipliers = { 1.0f, 1.1f, 1.21f, 1.34f, 1.47f, 1.61f };
    private static readonly float[] OddMultipliers = { 1.0f, 1.3f, 1.8f, 2.5f, 3.2f, 4.1f };
    private static readonly float[] EvenMultipliers = { 1.0f, 1.3f, 1.8f, 2.5f, 3.2f, 4.1f };
    private static readonly int[] GoldCoins = { 0, 1, 3, 5, 7, 10 };
    private static readonly int[] IronAddScores = { 0, 50, 120, 210, 320, 450 };
    private static readonly int[] LightningAddScores = { 0, 100, 220, 360, 520, 700 };

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
        public bool IsSoloTriggers = false;     // 孤高シール判定
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

        // 加算系シール（鉄・雷）の適応
        int ironCount = Mathf.Min(sealCounts[SealType.Iron], 5);
        int sealAdd = IronAddScores[ironCount];

        int lightningCount = 0;
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
                    if (dice.GetSealOnFace(dice.Value) == SealType.Lightning)
                    {
                        lightningCount++;
                    }
                }
            }
        }
        sealAdd += LightningAddScores[Mathf.Min(lightningCount, 5)];

        result.SealAddScore = sealAdd;

        // 基礎点の小計
        result.TotalBaseScore = result.HandBaseScore + result.UnusedDiceScore + result.SealAddScore;

        // 乗算系シール（炎・奇数・偶数・博打）の適応
        float totalMultiplier = 1.0f;

        // 炎シール
        totalMultiplier *= FireMultipliers[Mathf.Min(sealCounts[SealType.Fire], 5)];

        // 氷シール
        totalMultiplier *= IceMultipliers[Mathf.Min(sealCounts[SealType.Ice], 5)];

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
                        // 貼られているのが博打シールかつ使用ダイスリストに含まれているならリストから削除
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

        // 博打シールの適応
        if (isGambleBusted)
        {
            totalMultiplier *= 0.5f;
        }
        else if (gambleCount > 0)
        {
            totalMultiplier *= 2.5f;
        }

        // 孤高シール
        bool soloTriggered = false;
        foreach (var dice in diceList)
        {
            // 孤高シールが出目になっているダイスをチェック
            if (dice.GetSealOnFace(dice.Value) == SealType.Solo)
            {
                // そのダイスの3面全てチェック
                bool isPureSolo = true;
                int soloFaceCount = 0;
                for (int f = 0; f < 6; f++)
                {
                    SealType s = dice.GetSealOnFace(f);
                    if (s == SealType.Solo) soloFaceCount++;
                    else if (s != SealType.None) isPureSolo = false;
                }

                if (isPureSolo && soloFaceCount > 0)
                {
                    soloTriggered = true;
                    break;
                }
            }
        }

        if (soloTriggered)
        {
            totalMultiplier *= 2.0f;
        }
        result.IsSoloTriggers = soloTriggered;

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
        int goldCount = Mathf.Min(sealCounts[SealType.Gold], 5);

        result.EarnedCoins = GoldCoins[goldCount];
        result.DisableBossPassive = sealCounts[SealType.Ice] > 0;

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
