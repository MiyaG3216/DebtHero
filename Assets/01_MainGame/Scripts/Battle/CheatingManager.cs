using UnityEngine;

public class CheatingManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;
    [SerializeField] private BattleManager _battleManager;

    [Header("イカサマのコスト設定")]
    [SerializeField] private int _extraRollCost = 1;
    [SerializeField] private int _diceFlipCost = 2;
    [SerializeField] private int _directSetCost = 4;

    [Header("状態")]
    public int CheatUseCount = 0;

    // ===== プロパティ =====
    public int MaxCheats => (GameDataManager.Instance != null && GameDataManager.Instance.HasPassive(PassiveType.MagicianSet)) ? 2 : 1;
    public int RemainingCheats => Mathf.Max(0, MaxCheats - CheatUseCount);
    public bool CanCheatAny => RemainingCheats > 0;

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    public void ResetCheating()
    {
        CheatUseCount = 0;
    }

    // イカサマが使用可能かの判定
    private bool CanUseCgeat(int cost)
    {
        if (!_battleManager.IsBattleActive) return false;
        if (CheatUseCount >= MaxCheats) return false;
        if (GameDataManager.Instance != null && GameDataManager.Instance.Coins < cost) return false;

        return true;
    }

    // 【追加リロール】1コイン消費
    public bool TryUseExtraReroll()
    {
        if (!CanUseCgeat(_extraRollCost)) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_extraRollCost)) return false;

        _diceManager.RemainingRerolls++;
        CheatUseCount++;

        return true;
    }

    // 【ダイスフリップ】2コイン消費
    public bool TryUseDiceFlip(int diceIndex)
    {
        if (!CanUseCgeat(_diceFlipCost)) return false;
        if (diceIndex < 0 || diceIndex >= _diceManager.DiceList.Count) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_diceFlipCost)) return false;

        var targetDice = _diceManager.DiceList[diceIndex];
        int originalValue = targetDice.Value;

        // ダイスの裏面を計算
        int flippedValue = 7 - originalValue;
        targetDice.SetValueDirectly(flippedValue);

        // ダイス回転アニメーションを再生
        _diceManager.AnimateDiceCheat(diceIndex, flippedValue);

        CheatUseCount++;

        return true;
    }

    public bool TryUseDirectSet(int diceIndex, int targetValue)
    {
        if (!CanUseCgeat(_directSetCost)) return false;
        if (diceIndex < 0 || diceIndex >= _diceManager.DiceList.Count) return false;
        if (targetValue < 1 || targetValue > 6) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_directSetCost)) return false;

        var targetDice = _diceManager.DiceList[diceIndex];
        int originalValue = targetDice.Value;

        targetDice.SetValueDirectly(targetValue);

        // ダイス回転アニメーションを再生
        _diceManager.AnimateDiceCheat(diceIndex, targetValue);

        CheatUseCount++;

        return true;
    }
}
