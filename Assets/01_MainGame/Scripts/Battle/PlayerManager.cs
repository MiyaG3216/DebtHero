using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // ===== フィールド =====
    public static PlayerManager Instance { get; private set; }

    private int _debugCoins = 0;

    // ===== プロパティ =====
    public int Coins
    {
        get => GameDataManager.Instance != null ? GameDataManager.Instance.Coins : _debugCoins;
        set
        {
            if (GameDataManager.Instance != null) GameDataManager.Instance.Coins = value;
            else _debugCoins = value;
        }
    }

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    // ===== メソッド =====
    // コイン加算
    public void AddCoins(int amount)
    {
        Coins += amount;
    }

    // コイン消費
    public bool TrySpendCoins(int amount)
    {
        if (Coins >= amount)
        {
            Coins -= amount;
            return true;
        }

        return false;
    }
}
