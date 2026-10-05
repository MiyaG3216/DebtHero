using UnityEditorInternal;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // ===== フィールド =====
    public static PlayerManager Instance { get; private set; }

    [Header("プレイヤーデータ")]
    [SerializeField] private int _coins = 0;

    // ===== プロパティ =====
    public int Coins => _coins;

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===== メソッド =====
    // コイン加算
    public void AddCoins(int amount)
    {
        _coins += amount;
        Debug.Log($"[コイン獲得] + {amount} C (所持コイン：{_coins} C)");
    }

    // コイン消費
    public bool TrySpendCoins(int amount)
    {
        if(_coins >= amount)
        {
            _coins -= amount;
            Debug.Log($"[コイン消費] - {amount} C (所持コイン：{_coins} C)");
            return true;
        }

        Debug.Log("コインが足りません！");
        return false;
    }
}
