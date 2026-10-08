using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;

    [Header("価格設定")]
    [SerializeField] private int _dicePrice = 8;        // ダイスの価格
    [SerializeField] private int _rareSealPrice = 5;    // レアシールの価格

    private readonly List<SealType> _rareSealPool = new List<SealType>()
    {
        SealType.Lightning,
        SealType.Odd,
        SealType.Even
    };

    [Header("情報屋のヒント")]
    public string _currentBossHint = "ボスに関する情報はまだ入っていない...";

    // ===== プロパティ =====

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    // ショップを開く
    public void OpenShop(string bossHintText)
    {
        _currentBossHint = bossHintText;
        Debug.Log("【裏ショップ】へようこそ");
        Debug.Log($"情報屋の噂話：{_currentBossHint}");
        Debug.Log($"所持コイン：{PlayerManager.Instance.Coins} C");
        Debug.Log($"[1] 奇数ダイス購入 (8C)  | [2] ピンゾロダイス購入 (8C)");
        Debug.Log($"[3] 奇数シール購入 (5C)  | [4] 雷シール購入 (5C)");
    }

    //新しいダイスを購入して入れ替える（シールは引き継ぐ）
    public bool TryBuyDice(DiceType newDiceType, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _diceManager._diceList.Count) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_dicePrice)) return false;

        var targetDice = _diceManager._diceList[slotIndex];
        DiceType oldType = targetDice.Type;

        // ダイスの種類だけ変更
        targetDice.SetDiceType(newDiceType);

        Debug.Log($"ダイス購入成功。スロット[{slotIndex + 1}]を<{oldType}> → <{newDiceType}>に変更しました");
        return true;
    }

    // シールを購入してダイスに貼る
    public bool TryBuySeal(SealType seal, int slotIndex, int faceValue)
    {
        if (slotIndex < 0 || slotIndex >= _diceManager._diceList.Count) return false;
        if (faceValue < 1 || faceValue > 6) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_rareSealPrice)) return false;

        // シールの貼り付け
        _diceManager._diceList[slotIndex].AttachSeal(faceValue, seal);

        Debug.Log($"シール購入成功。スロット[{slotIndex + 1}]の【{faceValue}の面】に<{seal}>を貼りました。");
        return true;
    }
}
