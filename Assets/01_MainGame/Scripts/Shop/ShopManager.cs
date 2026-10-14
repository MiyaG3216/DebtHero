using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
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
    private void Start()
    {
        string hint = "ボスの情報はまだない...";
        int visitCount = 1;

        if (GameDataManager.Instance != null)
        {
            if (GameDataManager.Instance.CurrentStageIndex > 4)
            {
                visitCount = 2;
            }

            hint = GameDataManager.Instance.GetBossHintText();
        }

        OpenShop(hint, visitCount);
    }

    // ===== メソッド =====
    // ショップを開く
    public void OpenShop(string bossHintText, int visitCount = 1)
    {
        ShopVisitCount = visitCount;

        CurrentBossHint = bossHintText;

        if (_shopUIManager != null) _shopUIManager.OpenShopUI(CurrentBossHint, ShopVisitCount);
    }

    //新しいダイスを購入して入れ替える（シールは引き継ぐ）
    public bool TryBuyDice(DiceType newDiceType, int slotIndex)
    {
        var diceList = GetPlayerDiceList();

        if (diceList == null || slotIndex < 0 || slotIndex >= diceList.Count) return false;

        // コイン消費
        int price = GetDicePrice(newDiceType);
        if (GameDataManager.Instance == null || GameDataManager.Instance.Coins < price) return false;
        GameDataManager.Instance.Coins -= price;

        // ダイスの種類だけ変更
        diceList[slotIndex].SetDiceType(newDiceType);

        return true;
    }

    // シールを購入してダイスに貼る
    public bool TryBuyAndApplySeal(SealType seal, int slotIndex, int faceValue)
    {
        var diceList = GetPlayerDiceList();

        if (diceList == null || slotIndex < 0 || slotIndex >= diceList.Count) return false;
        if (faceValue < 1 || faceValue > 6) return false;

        // コイン消費
        if (GameDataManager.Instance == null || GameDataManager.Instance.Coins < _rareSealPrice) return false;
        GameDataManager.Instance.Coins -= _rareSealPrice;

        // シールの貼り付け
        diceList[slotIndex].AttachSeal(faceValue, seal);

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

    // ダイスリストを取得
    private List<Dice> GetPlayerDiceList()
    {
        return GameDataManager.Instance != null ? GameDataManager.Instance.PlayerDiceList : null;
    }
}
