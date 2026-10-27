using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RewardManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;
    [SerializeField] private RewardUIManager _rewardUIManager;

    // 報酬でドロップするシール(9種類)
    private readonly List<SealType> _allSealPool = new List<SealType>()
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
    // 報酬シールを決定する
    public void PrepareRewardSeals()
    {
        // 報酬ドロップ9種から5種を重複なしで抽選
        CurrentOfferSeals = _allSealPool.OrderBy(x => Random.value).Take(5).ToList();
        RemainingPicks = 3;
    }

    // 報酬フェーズの開始
    public void OpenRewardPhaseUI()
    {
        if (CurrentOfferSeals == null || CurrentOfferSeals.Count == 0 || RemainingPicks <= 0)
        {
            PrepareRewardSeals();
        }

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

        if (_diceManager != null) _diceManager.UpdateAllDiceVisuals();

        return true;
    }
}
