using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SealPackData
{
    public string PackName;         // パック名
    public string Description;      // 説明
    public int Price = 6;           // 価格
    public List<SealType> Seals;    // 内容（どのシールが含まれるか）
}

public class ShopManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private ShopUIManager _shopUIManager;

    [Header("価格設定")]
    [SerializeField] private int _straightDicePrice = 7;    // 連番ダイスの価格
    [SerializeField] private int _highRollerDicePrice = 7;  // 高目ダイスの価格
    [SerializeField] private int _lowRollerDicePrice = 7;   // 低目ダイスの価格
    [SerializeField] private int _oddDicePrice = 8;         // 奇数ダイスの価格
    [SerializeField] private int _evenDicePrice = 8;        // 偶数ダイスの価格

    [Header("シールパック一覧")]
    [SerializeField]
    private List<SealPackData> _sealPacks = new List<SealPackData>()
    {
        new SealPackData
        {
            PackName = "エレメントパック",
            Description = "炎・氷・雷の属性セット",
            Price = 6,
            Seals = new List<SealType>(){SealType.Fire,SealType.Ice,SealType.Lightning}
        },
        new SealPackData
        {
            PackName = "鉱物パック",
            Description = "鉄・金箔・孤高の堅実セット",
            Price = 6,
            Seals = new List<SealType>(){SealType.Iron,SealType.Gold,SealType.Solo}
        },
        new SealPackData
        {
            PackName = "ギャンブルパック",
            Description = "奇数・偶数・博打の賭け狂いセット",
            Price = 6,
            Seals = new List<SealType>(){SealType.Odd,SealType.Even,SealType.Gamble}
        }
    };

    [Header("情報屋のヒント")]
    public string CurrentBossHint = "";

    public int ShopVisitCount = 0;     // ショップ訪問回数（ラインナップに影響）

    // ===== プロパティ =====
    public List<SealPackData> SealPacks => _sealPacks;

    // ===== Unityメッセージ =====
    private void Start()
    {
        string hint = "ボスの情報はまだない...";

        if (GameDataManager.Instance != null)
        {
            hint = GameDataManager.Instance.GetBossHintText();
        }

        OpenShop(hint);
    }

    // ===== メソッド =====
    // ショップを開く
    public void OpenShop(string bossHintText, int visitCount = 1)
    {
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

    // シール貼り付け(1枚)
    public void ApplySeal(SealType seal, int slotIndex, int faceValue)
    {
        var diceList = GetPlayerDiceList();

        if (diceList == null) return;

        diceList[slotIndex].AttachSeal(faceValue, seal);
    }

    // パック購入時のコイン消費
    public void SpendPackCoins(int price)
    {
        if (GameDataManager.Instance != null) GameDataManager.Instance.Coins -= price;
    }

    // ダイスの値段を取得
    public int GetDicePrice(DiceType type)
    {
        int price = 8;
        switch (type)
        {
            case DiceType.Odd:
                price = _oddDicePrice;
                break;
            case DiceType.Even:
                price = _evenDicePrice;
                break;
            case DiceType.Straight:
                price = _straightDicePrice;
                break;
            case DiceType.HighRoller:
                price = _highRollerDicePrice;
                break;
            case DiceType.LowRoller:
                price = _lowRollerDicePrice;
                break;
        }

        // VIP会員証を適応
        if(GameDataManager.Instance != null&&GameDataManager.Instance.HasPassive(PassiveType.VipCard))
        {
            price = Mathf.CeilToInt(price * 0.5f);
        }

        return price;
    }

    // シールパックの価格取得
    public int GetPackPrice(SealPackData pack)
    {
        int price = pack.Price;
        if(GameDataManager.Instance != null && GameDataManager.Instance.HasPassive(PassiveType.VipCard))
        {
            price = Mathf.CeilToInt(price * 0.5f);
        }

        return price;
    }

    // ダイスの説明文を取得
    public string GetDiceDescription(DiceType type)
    {
        switch (type)
        {
            case DiceType.Odd: return "1,3,5 が出やすい";
            case DiceType.Even: return "2,4,6 が出やすい";
            case DiceType.Straight: return "2,3,4,5 がすこし出やすい";
            case DiceType.HighRoller: return "4,5,6 が出やすい";
            case DiceType.LowRoller: return "1,2,3 が出やすい";
            default: return "";
        }
    }

    // ダイスリストを取得
    private List<Dice> GetPlayerDiceList()
    {
        return GameDataManager.Instance != null ? GameDataManager.Instance.PlayerDiceList : null;
    }
}
