using UnityEngine;

public enum DiceType
{
    Nomal,      // 通常ダイス
    Odd,        // 奇数が出やすい
    Even,       // 偶数が出やすい
    HighRoller, // 4,5,6が出やすい
    Pinzoro,    // 1が出やすい
    Straight    // 2,3,4,5が出やすい
}

[System.Serializable]
public class Dice
{
    // ===== フィールド =====
    [SerializeField] private DiceType _dicetype = DiceType.Nomal;
    [SerializeField]
    private SealType[] _faceSeals = new SealType[6]
    {
        SealType.None,SealType.None,SealType.None,
        SealType.None,SealType.None,SealType.None,
    };

    // ===== プロパティ =====
    public DiceType Type => _dicetype;
    public int Value { get; private set; } = 1; // 出目
    public bool IsKept { get; set; } = false;   // キープ状態

    // ==== コンストラクタ ====
    public Dice(DiceType type = DiceType.Nomal)
    {
        _dicetype = type;
    }

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    // ダイスタイプ変更
    public void SetDiceType(DiceType newType)
    {
        _dicetype = newType;
    }

    // ダイスロール
    public void Roll()
    {
        if (IsKept) return;

        //  ダイス種類に応じた出目確立の重みづけを取得
        int[] weights = GetWeightsForType(_dicetype);

        Value = GetWeithtedRandom(weights);
    }

    // 各ダイスの重みづけを定義
    private int[] GetWeightsForType(DiceType type)
    {
        switch (type)
        {
            case DiceType.Odd:
                return new int[] { 30, 10, 30, 10, 30, 10 };

            case DiceType.Even:
                return new int[] { 10, 30, 10, 30, 10, 30 };

            case DiceType.HighRoller:
                return new int[] { 10, 10, 10, 30, 30, 30 };

            case DiceType.Pinzoro:
                return new int[] { 50, 10, 10, 10, 10, 10 };

            case DiceType.Straight:
                return new int[] { 10, 20, 20, 20, 20, 10 };

            case DiceType.Nomal:
            default:
                return new int[] { 10, 10, 10, 10, 10, 10 };
        }
    }

    // 重みづけを加味した出目抽選
    private int GetWeithtedRandom(int[] weights)
    {
        int totalWeight = 0;
        foreach (int w in weights) totalWeight += w;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeightSum = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            currentWeightSum += weights[i];
            if (randomValue < currentWeightSum)
            {
                return i + 1;
            }
        }

        return 6;
    }

    // キープ状態変更
    public void ToggleKeep()
    {
        IsKept = !IsKept;
    }

    // 指定した出目の面に貼られているシールを取得
    public SealType GetSealOnFace(int faceValue)
    {
        int index = faceValue - 1;
        if (index >= 0 && index < _faceSeals.Length)
        {
            return _faceSeals[index];
        }

        return SealType.None;
    }

    // 指定した面にシールを張り付ける
    public void AttachSeal(int faceValue, SealType seal)
    {
        int index = faceValue - 1;
        if (index >= 0 && index < _faceSeals.Length)
        {
            _faceSeals[index] = seal;
        }
    }

    // すでに貼ってあるシールを新しいダイスに引き継ぐ
    public void CopySealsFrom(Dice sourceDice)
    {
        for (int i = 0; i < 6; i++)
        {
            _faceSeals[i] = sourceDice._faceSeals[i];
        }
    }

    // 出目を直接書き換える（イカサマ用）
    public void SetValueDirectly(int newValue)
    {
        Value = Mathf.Clamp(newValue, 1, 6);
    }

    public void Reset()
    {
        Value = 1;
        IsKept = false;
    }
}
