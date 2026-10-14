using UnityEngine;

public enum BossTraitType
{
    None,
    Fee75Percent,
    DisableEven,
    DisableOdd,
    DisableHighHand,
    HalfPair
}

public class BossManager : MonoBehaviour
{
    // ===== フィールド =====
    public static BossManager Instance { get; private set; }

    [Header("ラスボスの特性（ランダム決定）")]
    public BossTraitType CurrentFinalBossTrait;

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DecideFinalBossTrait();
    }

    // ===== メソッド =====
    public void DecideFinalBossTrait()
    {
        BossTraitType[] traits =
        {
            BossTraitType.DisableEven,
            BossTraitType.DisableOdd,
            BossTraitType.DisableHighHand,
            BossTraitType.HalfPair
        };

        CurrentFinalBossTrait = traits[Random.Range(0, traits.Length)];
    }

    public string GetBossHintText()
    {
        switch (CurrentFinalBossTrait)
        {
            case BossTraitType.DisableEven:
                return "本社のボスは【偶数】が含まれる役を認めないらしい...";

            case BossTraitType.DisableOdd:
                return "本社のボスは【奇数】が含まれる役を認めないらしい...";

            case BossTraitType.DisableHighHand:
                return "本社のボスは【大技】を無効にしてくるぞ！";

            case BossTraitType.HalfPair:
                return "本社のボスは【ペア系】の役を半減するらしい...";

            default:
                return "ボスに関する情報はまだない...";
        }
    }

    public int ApplyBossTrait(BossTraitType trait,
                              HandEvaluationResult handResult,
                              int calcuratedScore,
                              bool isIceActive)
    {
        // 特性なしの場合、そのままスコアを返す
        if (trait == BossTraitType.None) return calcuratedScore;

        // 氷シールが適応されている場合は、特性を無効（スコアをそのまま返す）
        if (isIceActive)
        {
            return calcuratedScore;
        }

        int finalScore = calcuratedScore;

        switch (trait)
        {
            case BossTraitType.Fee75Percent:
                finalScore = Mathf.RoundToInt(calcuratedScore * 0.75f);
                break;

            case BossTraitType.DisableEven:     // 役に使ったダイスに偶数が含まれている場合、スコア無効
                if (handResult.UsedDice.Exists(x => x % 2 == 0))
                {
                    finalScore = 0;
                }
                break;

            case BossTraitType.DisableOdd:      // 役に使ったダイスに奇数が含まれている場合、スコア無効
                if (handResult.UsedDice.Exists(x => x % 2 != 0))
                {
                    finalScore = 0;
                }
                break;

            case BossTraitType.HalfPair: // ペア系の役のスコアを半減
                if (handResult.HandType == HandType.OnePair || handResult.HandType == HandType.TwoPair ||
                    handResult.HandType == HandType.ThreeDice || handResult.HandType == HandType.FourDice ||
                    handResult.HandType == HandType.FullHouse || handResult.HandType == HandType.FiveDice)
                {
                    finalScore = Mathf.RoundToInt(calcuratedScore * 0.5f);
                }
                break;

            case BossTraitType.DisableHighHand:
                if (handResult.HandType == HandType.FullHouse || handResult.HandType == HandType.Straight ||
                    handResult.HandType == HandType.FiveDice)
                {
                    finalScore = 0;
                }
                break;
        }

        return finalScore;
    }
}
