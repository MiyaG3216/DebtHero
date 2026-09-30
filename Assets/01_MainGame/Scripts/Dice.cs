using UnityEngine;

[System.Serializable]
public class Dice
{
    // ===== フィールド =====
    // ===== プロパティ =====
    public int Value { get; private set; } = 1; // 出目
    public bool IsKept { get; set; } = false;   // キープ状態

    // ===== Unityメッセージ =====

    // ===== メソッド =====
    // ダイスロール
    public void Roll()
    {
        if (IsKept) return;

        Value = Random.Range(1, 7);
    }

    // キープ状態変更
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
