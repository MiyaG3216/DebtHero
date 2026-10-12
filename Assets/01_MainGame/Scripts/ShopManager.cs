using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;
    [SerializeField] private ShopUIManager _shopUIManager;

    [Header("価格設定")]
    [SerializeField] private int _oddDicePrice = 8;         // 奇数ダイスの価格
    [SerializeField] private int _evenDicePrice = 8;        // 偶数ダイスの価格
    [SerializeField] private int _straightDicePrice = 8;    // 連番ダイスの価格
    [SerializeField] private int _highRollerDicePrice = 9;  // 高目ダイスの価格
    [SerializeField] private int _pinzoroDicePrice = 10;    // ピンゾロダイスの価格
    [SerializeField] private int _rareSealPrice = 5;        // レアシールの価格

    private readonly List<SealType> _rareSealPool = new List<SealType>()
    {
        SealType.Lightning,
        SealType.Odd,
        SealType.Even
    };

    [Header("情報屋のヒント")]
    public string CurrentBossHint = "";

    public int ShopVisitCount = 0;     // ショップ訪問回数（ラインナップに影響）

    // ===== プロパティ =====
    public int RareSealPrice => _rareSealPrice;

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    // ショップを開く
    public void OpenShop(string bossHintText)
    {
        ShopVisitCount++;

        CurrentBossHint = bossHintText;

        if (_shopUIManager != null) _shopUIManager.OpenShopUI(CurrentBossHint,ShopVisitCount);
    }

    //新しいダイスを購入して入れ替える（シールは引き継ぐ）
    public bool TryBuyDice(DiceType newDiceType, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _diceManager.DiceList.Count) return false;

        // コイン消費
        int price = GetDicePrice(newDiceType);
        if (!PlayerManager.Instance.TrySpendCoins(price)) return false;

        var targetDice = _diceManager.DiceList[slotIndex];

        // ダイスの種類だけ変更
        targetDice.SetDiceType(newDiceType);

        _diceManager.UpdateAllDiceVisuals();

        return true;
    }

    // シールを購入してダイスに貼る
    public bool TryBuyAndApplySeal(SealType seal, int slotIndex, int faceValue)
    {
        if (slotIndex < 0 || slotIndex >= _diceManager.DiceList.Count) return false;
        if (faceValue < 1 || faceValue > 6) return false;

        // コイン消費
        if (!PlayerManager.Instance.TrySpendCoins(_rareSealPrice)) return false;

        // シールの貼り付け
        _diceManager.DiceList[slotIndex].AttachSeal(faceValue, seal);

        _diceManager.UpdateAllDiceVisuals();

        return true;
    }

    public int GetDicePrice(DiceType type)
    {
        switch (type)
        {
            case DiceType.Odd: return _oddDicePrice;
            case DiceType.Even: return _evenDicePrice;
            case DiceType.Straight: return _straightDicePrice;
            case DiceType.HighRoller: return _highRollerDicePrice;
            case DiceType.Pinzoro: return _pinzoroDicePrice;
            default: return 8;
        }
    }
}
