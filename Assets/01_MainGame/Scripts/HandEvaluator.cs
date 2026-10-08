using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public enum HandType
{
    NoHand,     // 役なし
    OnePair,    // ワンペア
    TwoPair,    // ツーペア
    ThreeDice,  // スリーダイス
    FourDice,   // フォーダイス
    FullHouse,  // フルハウス
    Straight,   // ストレート
    FiveDice    // ファイブダイス
}

public class HandEvaluationResult
{
    // ===== プロパティ =====
    public HandType HandType { get; set; }  // 成立した役
    public string HandName { get; set; }    // 役の名前
    public int BaseScore { get; set; }      // 役の基本スコア
    public int UnusedDiceSum { get; set; }  // 役に使わなかったダイスの出目合計
    public List<int> UsedDice { get; set; } = new List<int>();      // 役に使ったダイス
    public List<int> UnusedDice { get; set; } = new List<int>();    // 役に使わなかったダイス
}

public static class HandEvaluator
{
    // 5つの出目から役を判定する
    public static HandEvaluationResult Evaluate(List<int> diceValues)
    {
        if (diceValues == null || diceValues.Count != 5)
        {
            Debug.Log("役判定には5つのダイスが必要です。");
            return null;
        }

        // 出目を昇順ソート
        var sortedDice = diceValues.OrderBy(x => x).ToList();

        // 出目ごとの出現個数を集計
        var groups = sortedDice.GroupBy(x => x)
                             .OrderByDescending(g => g.Count())
                             .ThenByDescending(g => g.Key)
                             .ToList();

        // ファイブダイス
        if (groups.Count == 1)
        {
            return new HandEvaluationResult
            {
                HandType = HandType.FiveDice,
                HandName = "ファイブダイス",
                BaseScore = 600,
                UnusedDiceSum = 0,
                UsedDice = sortedDice,
                UnusedDice = new List<int>()
            };
        }

        // ストレート
        if (groups.Count == 5)
        {
            bool isLowStraight = sortedDice.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 });
            bool isHighStraight = sortedDice.SequenceEqual(new List<int> { 2, 3, 4, 5, 6 });

            if (isLowStraight || isHighStraight)
            {
                return new HandEvaluationResult
                {
                    HandType = HandType.Straight,
                    HandName = "ストレート",
                    BaseScore = 300,
                    UnusedDiceSum = 0,
                    UsedDice = sortedDice,
                    UnusedDice = new List<int>()
                };
            }
        }

        // フォーダイス
        if (groups[0].Count() == 4)
        {
            var used = groups[0].ToList();
            var unused = groups[1].ToList();

            return new HandEvaluationResult
            {
                HandType = HandType.FourDice,
                HandName = "フォーダイス",
                BaseScore = 150,
                UnusedDiceSum = unused.Sum(),
                UsedDice = used,
                UnusedDice = unused
            };
        }

        // フルハウス
        if(groups[0].Count() == 3 && groups[1].Count() == 2)
        {
            return new HandEvaluationResult
            {
                HandType = HandType.FullHouse,
                HandName = "フルハウス",
                BaseScore = 200,
                UnusedDiceSum = 0,
                UsedDice = sortedDice,
                UnusedDice = new List<int>()
            };
        }

        // スリーダイス
        if(groups[0].Count() == 3)
        {
            var used = groups[0].ToList();
            var unused = sortedDice.Where(x => !used.Contains(x) || used.Remove(x) == false).ToList();

            // 余りダイスを正確に抽出
            var unusedList = new List<int>(sortedDice);
            foreach (var d in groups[0]) unusedList.Remove(d);

            return new HandEvaluationResult
            {
                HandType = HandType.ThreeDice,
                HandName = "スリーダイス",
                BaseScore = 100,
                UnusedDiceSum = unusedList.Sum(),
                UsedDice = groups[0].ToList(),
                UnusedDice = unusedList
            };
        }

        // ツーペア
        if (groups[0].Count() == 2 && groups[1].Count() == 2)
        {
            var usedList = groups[0].Concat(groups[1]).ToList();
            var unusedList = groups[2].ToList();

            return new HandEvaluationResult
            {
                HandType = HandType.TwoPair,
                HandName = "ツーペア",
                BaseScore = 60,
                UnusedDiceSum = unusedList.Sum(),
                UsedDice = usedList,
                UnusedDice = unusedList
            };
        }

        // ワンペア
        if(groups[0].Count() == 2)
        {
            var usedList = groups[0].ToList();
            var unusedList = new List<int>(sortedDice);
            foreach (var d in usedList) unusedList.Remove(d);

            return new HandEvaluationResult
            {
                HandType = HandType.OnePair,
                HandName = "ワンペア",
                BaseScore = 30,
                UnusedDiceSum = unusedList.Sum(),
                UsedDice = usedList,
                UnusedDice = unusedList
            };
        }

        // 役なし
        return new HandEvaluationResult
        {
            HandType = HandType.NoHand,
            HandName = "ノーハンド（役なし）",
            BaseScore = 0,
            UnusedDiceSum = sortedDice.Sum(),
            UsedDice = new List<int>(),
            UnusedDice = sortedDice
        };
    }
}
