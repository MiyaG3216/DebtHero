using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

public class DiceManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private BattleManager _battleManager;

    [Header("3Dダイスの表示オブジェクト")]
    [SerializeField] private List<DiceView3D> _diceViews = new List<DiceView3D>();

    [Header("設定")]
    [SerializeField] private int _maxRerollCount = 2;   // 最大リロール可能回数

    [Header("ダイスタイプ")]
    [SerializeField]
    private List<DiceType> _initialDiceTypes = new List<DiceType>()
    {
        DiceType.Nomal,
        DiceType.Nomal,
        DiceType.Nomal,
        DiceType.Nomal,
        DiceType.Nomal
    };

    [Header("確認用")]
    public List<Dice> _diceList = new List<Dice>();
    public int _remainingRerolls;   // 残リロール
    public bool _isTurnActive;

    // ===== プロパティ =====
    public bool IsAnyDiceRolling
    {
        get
        {
            if (_diceViews == null) return false;
            return _diceViews.Exists(v => v != null && v._isRolling);
        }
    }

    // ===== Unityメッセージ =====
    private void Awake()
    {
        // 5つのダイスを作成
        _diceList.Clear();
        for (int i = 0; i < 5; i++)
        {
            DiceType type = (1 < _initialDiceTypes.Count) ? _initialDiceTypes[i] : DiceType.Nomal;
            _diceList.Add(new Dice(type));
        }

        // テスト用
        _diceList[0].AttachSeal(1, SealType.Fire);
        _diceList[1].AttachSeal(1, SealType.Gamble);
    }

    private void Start()
    {
        // 開始時に1ターン目を開始
        StartNewTurn();
    }

    // ===== メソッド =====
    public void StartNewTurn()
    {
        _remainingRerolls = _maxRerollCount;
        _isTurnActive = true;

        // 全ダイスをリセットして振る
        for (int i = 0; i < _diceList.Count; i++)
        {
            _diceList[i].Reset();
            _diceList[i].Roll();

            if (i < _diceViews.Count && _diceViews[i] != null)
            {
                float zOffset = (i % 2 == 0) ? 0.6f : -0.6f;
                Vector3 spawnPos = new Vector3((i - 2) * 1.8f, 4.5f, zOffset);
                _diceViews[i].Roll(_diceList[i].Value, spawnPos);
            }
        }
    }

    // リロール（キープされていないダイスだけ振り直す）
    public void Reroll()
    {
        if (!_isTurnActive) return;

        if (_remainingRerolls <= 0) return;

        _remainingRerolls--;

        for (int i = 0; i < _diceList.Count; i++)
        {
            // キープされていないダイスだけ再計算＆リロール
            if (!_diceList[i].IsKept)
            {
                _diceList[i].Roll();

                if (i < _diceViews.Count && _diceViews[i] != null)
                {
                    float zOffset = (i % 2 == 0) ? 0.6f : -0.6f;
                    Vector3 spawnPos = new Vector3((i - 2) * 2.2f, 4.5f, zOffset);
                    _diceViews[i].Roll(_diceList[i].Value, spawnPos);
                }
            }
        }
    }

    // ダイスのキープ状態切り替え
    public void ToggleKeepDice(int index)
    {
        if (index < 0 || index >= _diceList.Count) return;

        _diceList[index].ToggleKeep();

        // キープ時物理ダイスも固定する
        if (index < _diceViews.Count && _diceViews[index] != null)
        {
            _diceViews[index].SetKeepState(_diceList[index].IsKept);
        }
    }

    // 現在の出目で役を確定する
    public HandEvaluationResult SubmitHand()
    {
        if (!_isTurnActive)
        {
            Debug.LogWarning("ターンが終了しています");
            return null;
        }

        // 5つのダイスの出目をリスト化
        List<int> currentValues = new List<int>();
        foreach (var dice in _diceList)
        {
            currentValues.Add(dice.Value);
        }

        // 役判定
        HandEvaluationResult result = HandEvaluator.Evaluate(currentValues);

        // ターン終了
        _isTurnActive = false;

        if (_battleManager != null)
        {
            _battleManager.OnHandSubmitted(result);
        }

        return result;
    }

    // イカサマ使用時のダイスの回転演出
    public void AnimateDiceCheat(int diceIndex,int newValue)
    {
        if(diceIndex >= 0 && diceIndex < _diceViews.Count && _diceViews[diceIndex] != null)
        {
            _diceViews[diceIndex].AnimateCheatFaceChange(newValue);
        }
    }
}
