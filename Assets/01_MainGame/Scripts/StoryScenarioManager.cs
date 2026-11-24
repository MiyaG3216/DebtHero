using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StoryScenarioManager : MonoBehaviour
{
    // ===== フィールド =====
    public static StoryScenarioManager _instance;

    [Header("会話シナリオ一覧")]
    [SerializeField] private List<DialogEvent> _scenarioList = new List<DialogEvent>();

    // ===== プロパティ =====
    public static StoryScenarioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<StoryScenarioManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[StoryScenarioManager]");
                    _instance = obj.AddComponent<StoryScenarioManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SetupDefaultScenarios();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===== メソッド =====
    private void SetupDefaultScenarios()
    {
        if (_scenarioList.Count > 0) return;

        // オープニング
        var op = new DialogEvent { EventName = "オープニング", Timing = DialogTiming.Opning, TargetStageIndex = 0 };
        op.Lines.Add(new DialogLine { SpeakerName = "したっぱ", Message = "おいおい、待ちわびたぜ借金野郎！\n耳をそろえて返してもらおうかぁ！？" });
        op.Lines.Add(new DialogLine { SpeakerName = "主人公", Message = "（身に覚えのない莫大な借金…！？\nだがこの街の掟はダイスゲームの強さが全てだ！）" });
        op.Lines.Add(new DialogLine { SpeakerName = "主人公", Message = "金ならダイスで稼いでやる！\nてめぇのボスがいる本社までカチコミだ！！" });
        _scenarioList.Add(op);

        // Stage 1 撃破後
        var after1 = new DialogEvent { EventName = "したっぱ撃退！", Timing = DialogTiming.AfterBattle, TargetStageIndex = 0 };
        op.Lines.Add(new DialogLine { SpeakerName = "したっぱ", Message = "ぐわぁーー！？\nな、なんだこの強さはぁーー！？", TriggerEnemyDamage = true });
        after1.Lines.Add(new DialogLine { SpeakerName = "主人公", Message = "へっ、口ほどにもねえな。\n巻き上げた金でダイスを強化して次へ進むぞ！" });
        _scenarioList.Add(after1);

        // Stage 2 戦闘前
        var before2 = new DialogEvent { EventName = "アニキ登場", Timing = DialogTiming.BeforeBattle, TargetStageIndex = 1 };
        before2.Lines.Add(new DialogLine { SpeakerName = "アニキ", Message = "したっぱが世話になったようだな……。\nだがワシの取り立ては甘くないぞ、小僧。" });
        _scenarioList.Add(before2);

        // Stage 2 撃破後
        var after2 = new DialogEvent { EventName = "アニキ撃退！", Timing = DialogTiming.AfterBattle, TargetStageIndex = 1 };
        after2.Lines.Add(new DialogLine { SpeakerName = "アニキ", Message = "バ、バカな……！\nこのワシがダイス勝負で負けるだと……！？", TriggerEnemyDamage = true });
        after2.Lines.Add(new DialogLine { SpeakerName = "主人公", Message = "よし、この先には裏カジノショップがあるな。\nイカサマダイスを仕入れて中ボスに備えよう！" });
        _scenarioList.Add(after2);
    }

    public DialogEvent GetCurrentDialog(DialogTiming timing, int stageIndex)
    {
        return _scenarioList.Find(x => x.Timing == timing && x.TargetStageIndex == stageIndex);
    }
}
