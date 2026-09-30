using UnityEngine;

[System.Serializable]
public class Dice
{
    // ===== フィールド =====
    // ===== プロパティ =====
    public int Value { get; private set; } = 1;
    public bool IsKept { get; set; } = false;

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    public void Roll()
    {
        if (IsKept) return;

        Value = Random.Range(1, 7);
    }

    public void ToggleKeep()
    {
        IsKept = !IsKept;
    }

    public void Reset()
    {
        Value = 1;
        IsKept = false;
    }

}
