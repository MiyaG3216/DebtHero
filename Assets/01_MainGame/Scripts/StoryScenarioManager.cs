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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===== メソッド =====
    public DialogEvent GetCurrentDialog(DialogTiming timing, int stageIndex)
    {
        return _scenarioList.Find(x => x.Timing == timing && x.TargetStageIndex == stageIndex);
    }
}
