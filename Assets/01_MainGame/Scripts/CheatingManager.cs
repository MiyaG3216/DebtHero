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
    // 今回のバトルでイカサマを使用したかどうか
    public bool _hasUsedCheatThisBattle = false;

    // ===== プロパティ =====

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    public void ResetCheating()
    {
        _hasUsedCheatThisBattle = false;
    }

    // イカサマが使用可能かの判定
    private bool CanUseCgeat(int cost)
    {
        if (!_battleManager._isBattleActive)
        {
            Debug.LogWarning("バトル中にしかイカサマは使用できません");
            return false;
        }

        if (_hasUsedCheatThisBattle)
        {
            Debug.LogWarning("この戦闘でイカサマを使用済みです！");
            return false;
        }

        if (PlayerManager.Instance.Coins < cost)
        {
            Debug.LogWarning($"コインが足りません！ (必要:{cost} / 所持:{PlayerManager.Instance.Coins})");
            return false;
        }

        return true;
    }

    // 【追加リロール】1コイン消費
    public bool TryUseExtraReroll()
    {
        if (!CanUseCgeat(_extraRollCost)) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_extraRollCost)) return false;

        _diceManager._remainingRerolls++;
        _hasUsedCheatThisBattle = true;

        Debug.Log("イカサマ発動:追加リロール");
        return true;
    }

    // 【ダイスフリップ】2コイン消費
    public bool TryUseDiceFlip(int diceIndex)
    {
        if (!CanUseCgeat(_diceFlipCost)) return false;
        if (diceIndex < 0 || diceIndex >= _diceManager._diceList.Count) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_diceFlipCost)) return false;

        var targetDice = _diceManager._diceList[diceIndex];
        int originalValue = targetDice.Value;

        // ダイスの裏面を計算
        int flippedValue = 7 - originalValue;
        targetDice.SetValueDirectly(flippedValue);

        // ダイス回転アニメーションを再生
        _diceManager.AnimateDiceCheat(diceIndex, flippedValue);

        _hasUsedCheatThisBattle = true;

        Debug.Log("イカサマ発動:フリップ");
        return true;
    }

    public bool TryUseDirectSet(int diceIndex, int targetValue)
    {
        if (!CanUseCgeat(_directSetCost)) return false;
        if (diceIndex < 0 || diceIndex >= _diceManager._diceList.Count) return false;
        if (targetValue < 1 || targetValue > 6) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_directSetCost)) return false;

        var targetDice = _diceManager._diceList[diceIndex];
        int originalValue = targetDice.Value;

        targetDice.SetValueDirectly(targetValue);

        // ダイス回転アニメーションを再生
        _diceManager.AnimateDiceCheat(diceIndex, targetValue);

        _hasUsedCheatThisBattle = true;


        Debug.Log("イカサマ発動:目押し");
        return true;
    }
}
