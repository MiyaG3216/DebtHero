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

    // 報酬でドロップするシール(6種類)
    private readonly List<SealType> _nomalSealPool = new List<SealType>()
    {
        SealType.Fire,
        SealType.Ice,
        SealType.Gold,
        SealType.Iron,
        SealType.Solo,
        SealType.Gamble
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
        int rewardCoins = 3;
        if (missionCleared)
        {
            rewardCoins += 2;
            Debug.Log("サブミッション達成！ ボーナスコイン + 2 C");
        }

        PlayerManager.Instance.AddCoins(rewardCoins);

        // 報酬ドロップ6種から4種を重複なしで抽選
        CurrentOfferSeals = _nomalSealPool.OrderBy(x => Random.value).Take(4).ToList();
        RemainingPicks = 2;

        if (_rewardUIManager != null)
        {
            _rewardUIManager.OpenRewardUI();
        }
    }

    // 報酬シールをダイスに貼り付ける
    public bool ApplySealToDice(SealType seal, int diceIndex, int faceValue)
    {
        if (RemainingPicks <= 0) return false;
        if (diceIndex < 0 || diceIndex >= _diceManager._diceList.Count) return false;
        if (faceValue < 1 || faceValue > 6) return false;

        // ダイスにシールを張り付ける
        _diceManager._diceList[diceIndex].AttachSeal(faceValue, seal);
        RemainingPicks--;

        CurrentOfferSeals.Remove(seal);

        return true;
    }
}
