using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;
    [SerializeField] private BattleManager _battleManager;

    [Header("現在情報表示")]
    [SerializeField] private TextMeshProUGUI _quotaText;    // 目標スコア
    [SerializeField] private TextMeshProUGUI _scoreText;    // 現在スコア
    [SerializeField] private TextMeshProUGUI _turnsText;    // 残りターン
    [SerializeField] private TextMeshProUGUI _messageText;  // 役結果や勝敗メッセージ

    [Header("ボタン")]
    [SerializeField] private Button _rerollButton;              // リロールボタン
    [SerializeField] private TextMeshProUGUI _rerollButtonText; // リロールボタン文字
    [SerializeField] private Button _submitButton;              // 役確定ボタン
    [SerializeField] private Button _restartButton;             // リスタートボタン

    [Header("ダイスUI")]
    [SerializeField] private List<Button> _diceButtons;         // ダイス
    [SerializeField] private List<TextMeshProUGUI> _diceTexts;  // ダイスの文字

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Start()
    {
        // 各ボタンにクリックイベントを登録
        _rerollButton.onClick.AddListener(OnRerollClicked);
        _submitButton.onClick.AddListener(OnSubmitClicked);
        _restartButton.onClick.AddListener(OnRestartClicked);

        for (int i = 0; i < _diceButtons.Count; i++)
        {
            int index = i;
            _diceButtons[i].onClick.AddListener(() => OnDiceClicked(index));
        }

        // 初期ＵＩの更新
        _messageText.text = "バトル開始！";
        _restartButton.gameObject.SetActive(false);
        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    // ===== メソッド =====
    public void UpdateUI()
    {
        if (_battleManager == null || _diceManager == null) return;

        // バトル情報の更新
        _quotaText.text = $"目標スコア：{_battleManager.TargetQuota} G";
        _scoreText.text = $"現在スコア：{_battleManager._currentScore} G";
        _turnsText.text = $"残りターン：{_battleManager._remainingTurns} / {_battleManager.MaxTurn}";

        // ダイス情報の更新
        for (int i = 0; i < _diceManager._diceList.Count; i++)
        {
            if (i < _diceTexts.Count)
            {
                var dice = _diceManager._diceList[i];
                string keepStatus = dice.IsKept ? "\n<color=red>[Lock]</color>" : "";
                _diceTexts[i].text = $"<b>{dice.Value}</b>\n<size=18>({GetDiceTypeName(dice.Type)})\n({GetSealTypeName(dice.GetSealOnFace(dice.Value))})</size>{keepStatus}";
            }
        }

        // リロールボタンの更新
        _rerollButtonText.text = $"リロール\n(あと{_diceManager._remainingRerolls}回)";
        _rerollButton.interactable = _battleManager._isBattleActive && _diceManager._remainingRerolls > 0;
        _submitButton.interactable = _battleManager._isBattleActive;
    }

    // ダイスボタンがクリックされたとき（キープ切り替え）
    private void OnDiceClicked(int index)
    {
        if (!_battleManager._isBattleActive) return;

        _diceManager.ToggleKeepDice(index);

        UpdateUI();
    }

    // リロールボタンが押されたとき
    private void OnRerollClicked()
    {
        _diceManager.Reroll();

        UpdateUI();
    }

    // 役確定ボタンが抑えたとき
    private void OnSubmitClicked()
    {
        var result = _diceManager.SubmitHand();

        if (result != null && _battleManager._isBattleActive)
        {
            _messageText.text = $"役：<b>{result.HandName}</b> (+{result.BaseScore + result.UnusedDiceSum} G)";
        }

        UpdateUI();
    }

    private void OnRestartClicked()
    {
        _messageText.text = "バトル開始！";
        _restartButton.gameObject.SetActive(false);

        // 現在のステージをリトライする
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.RetryCurrentStage();
        }
        else
        {
            _battleManager.StartBattle(300, 3); // フォールバック
        }

        UpdateUI();
    }

    // 勝敗メッセージの表示用
    public void ShowBattleResult(bool isWin)
    {
        if (isWin)
        {
            _messageText.text = "<color=green>★ 完済成功！ STAGE CLEAR ★</color>";
            _restartButton.gameObject.SetActive(false);
        }
        else
        {
            _messageText.text = "<color=red>× 完済失敗... GAME OVER ×</color>";
            _restartButton.gameObject.SetActive(true);
        }
    }

    private string GetDiceTypeName(DiceType type)
    {
        switch (type)
        {
            case DiceType.Odd: return "奇数";
            case DiceType.Even: return "偶数";
            case DiceType.HighRoller: return "高目";
            case DiceType.Pinzoro: return "ピンゾロ";
            case DiceType.Straight: return "連番";
            default: return "ノーマル";
        }
    }

    private string GetSealTypeName(SealType type)
    {
        switch (type)
        {
            case SealType.Fire: return "炎";
            case SealType.Ice: return "氷";
            case SealType.Iron: return "鉄";
            case SealType.Gold: return "金箔";
            case SealType.Lightning: return "雷";
            case SealType.Solo: return "孤高";
            case SealType.Odd: return "偶数";
            case SealType.Even: return "奇数";
            case SealType.Gamble: return "博打";
            default: return "";
        }
    }

}
