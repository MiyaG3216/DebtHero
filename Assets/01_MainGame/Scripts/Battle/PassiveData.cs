using UnityEngine;

public enum PassiveType
{
    GoldCard,       // ゴールドカード(役スコア2倍)
    VipCard,        // VIP会員証(ショップ半額)
    MagicianSet,    // マジシャンセット(イカサマ2回)
    FinalNotice,    // 督促状(残り1ターン3倍)
    PassBook,       // 預金通帳(所持コイン*0.1倍UP)
    DiceCup         // 高級ダイスカップ(リロール+1回)
}

public static class PassiveHelper
{
    // ===== メソッド =====
    public static string GetName(PassiveType type)
    {
        switch (type)
        {
            case PassiveType.GoldCard: return "ゴールドカード";
            case PassiveType.VipCard: return "VIP会員証";
            case PassiveType.MagicianSet: return "マジシャンセット";
            case PassiveType.FinalNotice: return "督促状";
            case PassiveType.PassBook: return "預金通帳";
            case PassiveType.DiceCup: return "高級ダイスカップ";
            default: return "";
        }
    }

    public static string GetDescription(PassiveType type)
    {
        switch (type)
        {
            case PassiveType.GoldCard: return "役の金額が【2倍】になる";
            case PassiveType.VipCard: return "ショップの全商品が【半額】になる";
            case PassiveType.MagicianSet: return "イカサマが【2回】まで使用できる";
            case PassiveType.FinalNotice: return "残り1ターンの時、返済金額が【3倍】になる";
            case PassiveType.PassBook: return "返済金額が【所持コイン × 0.05倍】増加する";
            case PassiveType.DiceCup: return "リロール回数が【4回】になる";
            default: return "";
        }
    }
}
