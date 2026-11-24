using UnityEngine;
using System.Collections.Generic;

// 会話が発生するタイミング
public enum DialogTiming
{
    Opning,         // ゲーム開始時
    BeforeBattle,   // バトル前
    AfterBattle,    // バトル後
    Ending          // ラスボス撃破後
}

// セリフデータ
[System.Serializable]
public class DialogLine
{
    public string SpeakerName;              // 話者名
    [TextArea(2, 4)] public string Message; // セリフ
    public bool TriggerEnemyDamage;         // ダメージ演出トリガー
}

// 会話イベント
[System.Serializable]
public class DialogEvent
{
    public string EventName;        // イベント名
    public DialogTiming Timing;     // 発生タイミング
    public int TargetStageIndex;    // 対象のステージ番号
    public Sprite EnemyPortrait;    // 敵キャラ立ち絵
    public Sprite BackgroundImage;  // 背景
    public List<DialogLine> Lines = new List<DialogLine>();
}