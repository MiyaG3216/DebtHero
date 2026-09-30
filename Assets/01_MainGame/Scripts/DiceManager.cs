using UnityEngine;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using System.Runtime.ExceptionServices;
using Unity.Collections.LowLevel.Unsafe;
using JetBrains.Annotations;

public class DiceManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("設定")]
    [SerializeField] private int _maxRerollCount = 2;

    [Header("確認用")]
    public List<Dice> _diceList = new List<Dice>();
    public int _remainingRerolls;
    public bool _isTurnActive;

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Awake()
    {
        _diceList.Clear();
        for (int i = 0; i < 5; i++)
        {
            _diceList.Add(new Dice());
        }
    }

    private void Start()
    {
        StartNewTurn();
    }

    // ===== メソッド =====
    public void StartNewTurn()
    {
        _remainingRerolls = _maxRerollCount;
        _isTurnActive = true;

        foreach (var dice in _diceList)
        {
            dice.Reset();
            dice.Roll();
        }

        Debug.Log("【新しいターン開始】");
        LogDiceStatus();
    }

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

    public void ToggleKeepDice(int index)
    {
        if (index < 0 || index >= _diceList.Count) return;

        _diceList[index].ToggleKeep();
        Debug.Log($"ダイス[{index+1}]のキープ状態：{(_diceList[index].IsKept ? "キープ中[Lock]" : "フリー")}");
    }

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
}
