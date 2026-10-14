using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RewardManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private RewardUIManager _rewardUIManager;

    // 報酬でドロップするシール(9種類)
    private readonly List<SealType> _nomalSealPool = new List<SealType>()
    {
        SealType.Fire,
        SealType.Ice,
        SealType.Gold,
        SealType.Iron,
        SealType.Solo,
        SealType.Gamble,
        SealType.Odd,
        SealType.Even,
        SealType.Lightning
    };

    // ===== プロパティ =====
    public List<SealType> CurrentOfferSeals { get; private set; } = new List<SealType>();
    public int RemainingPicks { get; private set; } = 0;

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    // 報酬フェーズの開始
    public void StartRewardPhase(bool missionCleared)
    {
        // コインの付与
        int rewardCoins = 5;
        if (missionCleared) rewardCoins += 3;

        GameDataManager.Instance.Coins += rewardCoins;

        // 報酬ドロップ9種から5種を重複なしで抽選
        CurrentOfferSeals = _nomalSealPool.OrderBy(x => Random.value).Take(5).ToList();
        RemainingPicks = 3;

        if (_rewardUIManager != null)
        {
            _rewardUIManager.OpenRewardUI();
        }
    }

    // 報酬シールをダイスに貼り付ける
    public bool ApplySealToDice(SealType seal, int diceIndex, int faceValue)
    {
        if (RemainingPicks <= 0) return false;
        if (diceIndex < 0 || diceIndex >= _diceManager.DiceList.Count) return false;
        if (faceValue < 1 || faceValue > 6) return false;

        // ダイスにシールを張り付ける
        _diceManager.DiceList[diceIndex].AttachSeal(faceValue, seal);
        RemainingPicks--;

        CurrentOfferSeals.Remove(seal);

        _diceManager.UpdateAllDiceVisuals();

        return true;
    }
}
