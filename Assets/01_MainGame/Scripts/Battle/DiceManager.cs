using UnityEngine;
using System.Collections.Generic;


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
    [SerializeField] private float _spawnHight = 9f;    // ダイスの待機する高さ
    [SerializeField] private float _diceSpacing = 2.2f; // ダイス同士の間隔

    [Header("状態")]
    public bool IsWaitingForThrow = false;      // 投げるボタンフラグ

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
    public List<Dice> DiceList = new List<Dice>();
    public int RemainingRerolls;   // 残リロール
    public bool IsTurnActive;

    // ===== プロパティ =====
    public bool IsAnyDiceRolling
    {
        get
        {
            if (_diceViews == null) return false;
            return _diceViews.Exists(v => v != null && v.IsRolling);
        }
    }

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (GameDataManager.Instance != null)
        {
            DiceList = GameDataManager.Instance.PlayerDiceList;
        }
        else
        {
            // 5つのダイスを作成
            DiceList.Clear();
            for (int i = 0; i < 5; i++)
            {
                DiceType type = (1 < _initialDiceTypes.Count) ? _initialDiceTypes[i] : DiceType.Nomal;
                DiceList.Add(new Dice(type));
            }

            // テスト用
            DiceList[0].AttachSeal(1, SealType.Fire);
            DiceList[1].AttachSeal(1, SealType.Gamble);
        }
    }

    // ===== メソッド =====
    public void StartNewTurn()
    {
        int maxRerolls = (GameDataManager.Instance != null && GameDataManager.Instance.HasPassive(PassiveType.DiceCup)) ? 3 : _maxRerollCount;
        RemainingRerolls = maxRerolls;
        IsTurnActive = true;
        IsWaitingForThrow = true;

        UpdateAllDiceVisuals();

        // 全ダイスをリセットして振る
        for (int i = 0; i < DiceList.Count; i++)
        {
            DiceList[i].Reset();
            DiceList[i].Roll();

            if (i < _diceViews.Count && _diceViews[i] != null)
            {
                float zOffset = (i % 2 == 0) ? 0.8f : -0.8f;
                Vector3 spawnPos = new Vector3((i - 2) * _diceSpacing, _spawnHight, zOffset);
                _diceViews[i].PrepareRoll(spawnPos);
            }
        }
    }

    // リロール（キープされていないダイスだけ振り直す）
    public void Reroll()
    {
        if (!IsTurnActive) return;

        if (RemainingRerolls <= 0) return;

        RemainingRerolls--;

        for (int i = 0; i < DiceList.Count; i++)
        {
            // キープされていないダイスだけ再計算＆リロール
            if (!DiceList[i].IsKept)
            {
                DiceList[i].Roll();

                if (i < _diceViews.Count && _diceViews[i] != null)
                {
                    float zOffset = (i % 2 == 0) ? 0.6f : -0.6f;
                    Vector3 spawnPos = new Vector3((i - 2) * 2.2f, 4.5f, zOffset);
                    _diceViews[i].Roll(DiceList[i].Value, spawnPos);
                }
            }
        }
    }

    // ダイスのキープ状態切り替え
    public void ToggleKeepDice(int index)
    {
        if (index < 0 || index >= DiceList.Count) return;

        DiceList[index].ToggleKeep();

        // キープ時物理ダイスも固定する
        if (index < _diceViews.Count && _diceViews[index] != null)
        {
            _diceViews[index].SetKeepState(DiceList[index].IsKept);
        }
    }

    // 現在の出目で役を確定する
    public HandEvaluationResult SubmitHand()
    {
        if (!IsTurnActive)
        {
            return null;
        }

        // 5つのダイスの出目をリスト化
        List<int> currentValues = new List<int>();
        foreach (var dice in DiceList)
        {
            currentValues.Add(dice.Value);
        }

        // 役判定
        HandEvaluationResult result = HandEvaluator.Evaluate(currentValues);

        // ターン終了
        IsTurnActive = false;

        if (_battleManager != null)
        {
            _battleManager.OnHandSubmitted(result);
        }

        return result;
    }

    // イカサマ使用時のダイスの回転演出
    public void AnimateDiceCheat(int diceIndex, int newValue)
    {
        if (diceIndex >= 0 && diceIndex < _diceViews.Count && _diceViews[diceIndex] != null)
        {
            _diceViews[diceIndex].AnimateCheatFaceChange(newValue);
        }
    }

    // ダイスの色とシールの見た目をデータと同期する
    public void UpdateAllDiceVisuals()
    {
        for (int i = 0; i < DiceList.Count; i++)
        {
            if (i < _diceViews.Count && _diceViews[i] != null)
            {
                _diceViews[i].UpdateVisuals(DiceList[i]);
            }
        }
    }

    public void PrepareReroll()
    {
        if (!IsTurnActive || RemainingRerolls <= 0 || IsWaitingForThrow) return;

        RemainingRerolls--;
        IsWaitingForThrow = true;

        for (int i = 0; i < DiceList.Count; i++)
        {
            if (!DiceList[i].IsKept)
            {
                DiceList[i].Roll();

                if (i < _diceViews.Count && _diceViews[i] != null)
                {
                    float zOffset = (i % 2 == 0) ? 0.8f : -0.8f;
                    Vector3 spawnPos = new Vector3((i - 2) * _diceSpacing, _spawnHight, zOffset);
                    _diceViews[i].PrepareRoll(spawnPos);
                }
            }
        }
    }

    public void ThrowDice()
    {
        if (!IsWaitingForThrow) return;
        IsWaitingForThrow = false;

        for (int i = 0; i < DiceList.Count; i++)
        {
            if (!DiceList[i].IsKept)
            {
                if (i < _diceViews.Count && _diceViews[i] != null)
                {
                    _diceViews[i].ThrowDice(DiceList[i].Value);
                }
            }
        }
    }
}
