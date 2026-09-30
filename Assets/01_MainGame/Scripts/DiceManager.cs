using UnityEngine;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using System.Runtime.ExceptionServices;
using Unity.Collections.LowLevel.Unsafe;
using JetBrains.Annotations;
using System.Reflection;

public class DiceManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private BattleManager _battleManager;

    [Header("設定")]
    [SerializeField] private int _maxRerollCount = 2;   // 最大リロール可能回数

    [Header("確認用")]
    public List<Dice> _diceList = new List<Dice>();
    public int _remainingRerolls;   // 残リロール
    public bool _isTurnActive;

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Awake()
    {
        // 5つのダイスを作成
        _diceList.Clear();
        for (int i = 0; i < 5; i++)
        {
            _diceList.Add(new Dice());
        }
    }

    private void Start()
    {
        // 開始時に1ターン目を開始
        StartNewTurn();
    }

    // ===== メソッド =====
    public void StartNewTurn()
    {
        ClearConsole();

        _remainingRerolls = _maxRerollCount;
        _isTurnActive = true;

        // 全ダイスをリセットして振る
        foreach (var dice in _diceList)
        {
            dice.Reset();
            dice.Roll();
        }

        Debug.Log("【新しいターン開始】");
        LogDiceStatus();
    }

    // リロール（キープされていないダイスだけ振り直す）
    public void Reroll()
    {
        if (!_isTurnActive)
        {
            Debug.LogWarning("ターンが開始されていません。");
            return;
        }

        if (_remainingRerolls <= 0)
        {
            Debug.LogWarning("リロール回数が残っていません。役を確定してください。");
            return;
        }

        _remainingRerolls--;

        foreach (var dice in _diceList)
        {
            dice.Roll();
        }

        Debug.Log($"【リロール実行】残り回数：{_remainingRerolls}");
        LogDiceStatus();
    }

    // ダイスのキープ状態切り替え
    public void ToggleKeepDice(int index)
    {
        if (index < 0 || index >= _diceList.Count) return;

        _diceList[index].ToggleKeep();
        Debug.Log($"ダイス[{index + 1}]のキープ状態：{(_diceList[index].IsKept ? "キープ中[Lock]" : "フリー")}");
    }

    // 現在のダイスの状態をコンソールに出力
    public void LogDiceStatus()
    {
        string result = "出目：";

        for (int i = 0; i < _diceList.Count; i++)
        {
            string keepMark = _diceList[i].IsKept ? "[K]" : "";
            result += $"({i + 1}番目：{_diceList[i].Value}{keepMark})";
        }
        Debug.Log(result);
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

        // 判定結果をコンソールに出力
        //Debug.Log("====================");
        //Debug.Log($"【役判定】：{result.HandName}");
        //Debug.Log($"【役基本スコア】：{result.BaseScore}点");
        //Debug.Log($"【役以外の出目合計】：{result.UnusedDiceSum}点（内訳：[{string.Join(",", result.UnusedDice)}]）");
        //Debug.Log($"【暫定スコア】：{result.BaseScore + result.UnusedDiceSum}点");
        //Debug.Log("====================");

        // ターン終了
        _isTurnActive = false;

        if(_battleManager != null)
        {
            _battleManager.OnHandSubmitted(result);
        }

        return result;
    }

    // ★UnityEditorのログを削除
    private void ClearConsole()
    {
#if UNITY_EDITOR
        var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
        var clearMethod = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        clearMethod?.Invoke(null, null);
#endif
    }
}
