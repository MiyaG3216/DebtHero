using UnityEngine;
using System.Collections.Generic;
using System.Xml;

public class MissionManager : MonoBehaviour
{
    private static MissionManager _instance;

    public static MissionManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<MissionManager>();
            return _instance;
        }
    }

    // ===== フィールド =====
    [Header("現在の状況")]
    // ターン数ミッション
    public bool IsTurnMissionCleared = false;
    public int TurnMissionReward = 3;
    public int StageMaxTurns = 3;

    // 役ミッション
    public HandType TargetHandType = HandType.NoHand;
    public string TargetHandName = "";
    public int HandMissionReward = 1;
    public bool IsHandMissionCleared = false;

    // 数字ミッション
    public int TargetNumber = 1;
    public int RequiredNumberCount = 3;
    public int CurrentNumberCount = 0;
    public int NumberMissionReward = 2;
    public bool IsNumberMissionCleared = false;

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    // ===== メソッド =====
    // ステージ開始時にミッションを生成
    public void GenerateMissions(int maxTurns)
    {
        // ターンミッション初期化
        IsHandMissionCleared = false;
        TurnMissionReward = 3;
        StageMaxTurns = maxTurns;

        // ショップ1訪問をチェック
        bool isBeforeShop1 = false;
        if (GameDataManager.Instance != null) isBeforeShop1 = GameDataManager.Instance.CurrentStageIndex < 2;

        // 役ミッション抽選(重み付き)
        TargetHandType = LotteryHandMission(isBeforeShop1);
        IsHandMissionCleared = false;
        SetHandMissionRewardAndName(TargetHandType);

        // 数字ミッション抽選
        TargetNumber = Random.Range(1, 7);
        RequiredNumberCount = maxTurns - 1;
        CurrentNumberCount = 0;
        IsNumberMissionCleared = false;
        NumberMissionReward = 2;
    }

    // 役ミッションの重み付き抽選
    private HandType LotteryHandMission(bool isEaelyStage)
    {
        // 役と重みの対応リスト
        List<KeyValuePair<HandType, int>> pool = new List<KeyValuePair<HandType, int>>();

        // 序盤は高難度役が出現せず、中盤以降高難度役が出現
        if (isEaelyStage)
        {
            pool.Add(new KeyValuePair<HandType, int>(HandType.OnePair, 35));
            pool.Add(new KeyValuePair<HandType, int>(HandType.TwoPair, 35));
            pool.Add(new KeyValuePair<HandType, int>(HandType.ThreeDice, 30));
        }
        else
        {
            pool.Add(new KeyValuePair<HandType, int>(HandType.ThreeDice, 25));
            pool.Add(new KeyValuePair<HandType, int>(HandType.FullHouse, 25));
            pool.Add(new KeyValuePair<HandType, int>(HandType.FourDice, 20));
            pool.Add(new KeyValuePair<HandType, int>(HandType.Straight, 20));
            pool.Add(new KeyValuePair<HandType, int>(HandType.FiveDice, 10));
        }

        // 重みの合計を計算
        int totalWeight = 0;
        foreach (var pair in pool) totalWeight += pair.Value;

        int randomVal = Random.Range(0, totalWeight);
        int currentSum = 0;
        foreach (var pair in pool)
        {
            currentSum += pair.Value;
            if (randomVal < currentSum) return pair.Key;
        }

        return HandType.OnePair;
    }

    // 役名と報酬ヘルパー
    private void SetHandMissionRewardAndName(HandType type)
    {
        switch (type)
        {
            case HandType.OnePair:
                TargetHandName = "ワンペア";
                HandMissionReward = 1;
                break;

            case HandType.TwoPair:
                TargetHandName = "ツーペア";
                HandMissionReward = 1;
                break;

            case HandType.ThreeDice:
                TargetHandName = "スリーダイス";
                HandMissionReward = 2;
                break;

            case HandType.FourDice:
                TargetHandName = "フォーダイス";
                HandMissionReward = 2;
                break;

            case HandType.FullHouse:
                TargetHandName = "フルハウス";
                HandMissionReward = 3;
                break;

            case HandType.Straight:
                TargetHandName = "ストレート";
                HandMissionReward = 3;
                break;

            case HandType.FiveDice:
                TargetHandName = "ファイブダイス";
                HandMissionReward = 4;
                break;
        }
    }

    // 確定時に役と数字の進捗をチェック
    public void OnHandSubmitted(HandEvaluationResult handResult, List<Dice> diceList)
    {
        // 役ミッション判定
        if (handResult != null && handResult.HandType == TargetHandType)
        {
            IsHandMissionCleared = true;
        }

        // 数字ミッション判定
        if (diceList != null && !IsNumberMissionCleared)
        {
            for (int i = 0; i < diceList.Count; i++)
            {
                if (diceList[i].Value == TargetNumber)
                {
                    CurrentNumberCount++;
                }
            }

            if (CurrentNumberCount >= RequiredNumberCount)
            {
                IsNumberMissionCleared = true;
            }
        }
    }

    // ターンミッション判定
    public void CheckTrunMission(int remainingTurn)
    {
        IsTurnMissionCleared = (remainingTurn >= 1);
    }

    // 達成したミッション数
    public int GetClearedCount()
    {
        int count = 0;
        if (IsTurnMissionCleared) count++;
        if (IsHandMissionCleared) count++;
        if (IsNumberMissionCleared) count++;
        return count;
    }

    public int GetTotalBonusCoins()
    {
        int total = 0;
        if (IsTurnMissionCleared) total += TurnMissionReward;
        if (IsHandMissionCleared) total += HandMissionReward;
        if (IsNumberMissionCleared) total += NumberMissionReward;
        return total;
    }
}
