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
    [SerializeField] private CheatingManager _cheatingManager;

    [Header("画面上部")]
    [SerializeField] private TextMeshProUGUI _stageNameText;        // ステージ名
    [SerializeField] private TextMeshProUGUI _bossTraitText;    // ボス特性
    [SerializeField] private Slider _quotaSlider;               // 返済メーター(スライダー)
    [SerializeField] private TextMeshProUGUI _quotaText;        // 目標スコア
    [SerializeField] private TextMeshProUGUI _turnsText;        // 残りターン

    [Header("画面下部")]
    [SerializeField] private Button _rerollButton;              // リロールボタン
    [SerializeField] private TextMeshProUGUI _rerollButtonText; // リロールボタン文字
    [SerializeField] private Button _submitButton;              // 役確定ボタン
    [SerializeField] private Button _restartButton;             // リスタートボタン

    [Header("画面中央")]
    [SerializeField] private TextMeshProUGUI _coinsText;        // 所持コイン
    [SerializeField] private Button _cheatRerollButton;         // 追加リロールボタン
    [SerializeField] private Button _cheatFlipButton;           // フリップボタン
    [SerializeField] private Button _cheatSetButton;            // 目押しボタン
    [SerializeField] private TextMeshProUGUI _messageText;

    [Header("スコア詳細パネル")]
    [SerializeField] private GameObject _scoreDetailPanel;
    [SerializeField] private TextMeshProUGUI _detailHandNameText;       // 役名
    [SerializeField] private TextMeshProUGUI _detailHandBaseScoreText;  // 役の基礎点
    [SerializeField] private TextMeshProUGUI _detailUnusedDiceScoreText;// 不使用ダイス合計
    [SerializeField] private TextMeshProUGUI _detailSealAddScoreText;   // 不使用ダイス合計
    [SerializeField] private TextMeshProUGUI _detailTotalBaseText;      // 基礎点の小計
    [SerializeField] private TextMeshProUGUI _detailMultiplierText;     // シール倍率
    [SerializeField] private TextMeshProUGUI _detailBossModifierText;   // ボス特性補正
    [SerializeField] private TextMeshProUGUI _detailCoinsText;          // 獲得コイン
    [SerializeField] private TextMeshProUGUI _detailFinalScoreText;     // 最終スコア
    [SerializeField] private Button _detailConfirmButton;               // 次へボタン

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Start()
    {
        // 各ボタンにクリックイベントを登録
        _rerollButton.onClick.AddListener(OnRerollClicked);
        _submitButton.onClick.AddListener(OnSubmitClicked);
        _restartButton.onClick.AddListener(OnRestartClicked);

        // 詳細パネルの次へボタン
        if (_detailConfirmButton != null)
        {
            _detailConfirmButton.onClick.AddListener(OnDetailConfirmClicked);
        }

        // イカサマボタンのイベント登録（フリップと目押しはとりあえず1番ダイスを対象に発動）
        if (_cheatingManager != null)
        {
            _cheatRerollButton.onClick.AddListener(() => { _cheatingManager.TryUseExtraReroll(); UpdateUI(); });
            _cheatFlipButton.onClick.AddListener(() => { _cheatingManager.TryUseDiceFlip(0); UpdateUI(); });
            _cheatSetButton.onClick.AddListener(() => { _cheatingManager.TryUseDirectSet(0, 6); UpdateUI(); });
        }

        // 初期UIの更新
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

        // 返済メーターの更新
        if (_quotaSlider != null)
        {
            _quotaSlider.maxValue = _battleManager.TargetQuota;
            _quotaSlider.value = _battleManager._currentScore;
        }

        if (_quotaText != null)
        {
            _quotaText.text = $"返済額：<b>{_battleManager._currentScore}</b> / {_battleManager.TargetQuota} G";
        }

        // 残りターン数
        if (_turnsText != null)
        {
            _turnsText.text = $"残りターン：<b>{_battleManager._remainingTurns}</b>";
        }

        // 所持コイン
        if (_coinsText != null && PlayerManager.Instance != null)
        {
            _coinsText.text = $"所持コイン：<b>{PlayerManager.Instance.Coins} C</b>";
        }

        // リロール・役確定ボタン(ダイスロール中、詳細パネルOP中は非表示にする)
        bool isRolling = _diceManager.IsAnyDiceRolling;
        bool isPanelOpen = _scoreDetailPanel != null && _scoreDetailPanel.activeSelf;
        bool showControls = _battleManager._isBattleActive && !isRolling && !isPanelOpen;

        _rerollButton.gameObject.SetActive(showControls);
        _submitButton.gameObject.SetActive(showControls);

        if (showControls)
        {
            _rerollButtonText.text = $"リロール\n(あと{_diceManager._remainingRerolls}回)";
            _rerollButton.interactable = _diceManager._remainingRerolls > 0;
            _submitButton.interactable = true;
        }

        // イカサマボタンの活性・非活性
        UpdateCheatButtons();
    }

    // リロールボタンが押されたとき
    private void OnRerollClicked()
    {
        _diceManager.Reroll();

        UpdateUI();
    }

    // 役確定ボタンが押されたとき
    private void OnSubmitClicked()
    {
        _diceManager.SubmitHand();
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

        UpdateUI();
    }

    private void OnDetailConfirmClicked()
    {
        _scoreDetailPanel.SetActive(false);
        _battleManager.ConfirmScoreAndAdvanceTurn();
        UpdateUI();
    }

    public void ShowScoreDetailPanel(ScoreBreakdown breakdown)
    {
        if (_scoreDetailPanel == null) return;

        _detailHandNameText.text = $"成立役：<b>{breakdown.HandName}</b>";
        _detailHandBaseScoreText.text = $"役の金額：<b>{breakdown.HandBaseScore} G</b>";
        _detailUnusedDiceScoreText.text = $"役未使用ダイス：<b>{breakdown.UnusedDiceScore} G</b>";
        _detailSealAddScoreText.text = $"シール加算：<b>{breakdown.SealAddScore} G</b>";
        _detailTotalBaseText.text = $"返済額 小計：<b>{breakdown.TotalBaseScore} G</b>";
        _detailMultiplierText.text = $"シール倍率：<b>× {breakdown.Multiplier:F1} 倍</b>";
        _detailBossModifierText.text = (breakdown.BossTraitText != "補正なし") ? $"ボス補正：{breakdown.BossTraitText}" : "";
        _detailCoinsText.text = (breakdown.EarnedCoins > 0) ? $"獲得コイン：+ {breakdown.EarnedCoins} 枚" : "";
        _detailFinalScoreText.text = $"返済金額 総計：<color=yellow><size=80><b>+ {breakdown.FinalScore} G</b></size></color>";

        _scoreDetailPanel.SetActive(true);
        UpdateUI();
    }

    // 勝敗メッセージの表示用 (後で消す)
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

    // イカサマボタン群の制御
    private void UpdateCheatButtons()
    {
        if (_cheatingManager == null || PlayerManager.Instance == null) return;

        bool canCheat = _battleManager._isBattleActive && !_cheatingManager._hasUsedCheatThisBattle;
        int coins = PlayerManager.Instance.Coins;

        _cheatRerollButton.interactable = canCheat && (coins >= 1);
        _cheatFlipButton.interactable = canCheat && (coins >= 2);
        _cheatSetButton.interactable = canCheat && (coins >= 4);
    }

    // ステージ開始時に適情報と特性をセットする(BattleManagerから呼ぶ用)
    public void SetupStageInfo(string stageName, BossTraitType trait)
    {
        if (_stageNameText != null) _stageNameText.text = $"【{stageName}】";

        if (_bossTraitText != null)
        {
            if (trait != BossTraitType.None)
            {
                _bossTraitText.gameObject.SetActive(true);
                _bossTraitText.text = $"<color=red>特性；{GetTraitName(trait)}</color>";
            }
            else
            {
                _bossTraitText.gameObject.SetActive(false);
            }
        }
    }

    private string GetTraitName(BossTraitType trait)
    {
        switch (trait)
        {
            case BossTraitType.Fee75Percent: return "みかじめ料(返済額 25％ カット)";
            case BossTraitType.DisableEven: return "偶数無効（2,4,6を含む役で返済不可）";
            case BossTraitType.DisableOdd: return "奇数無効（1,3,5を含む役で返済不可）";
            case BossTraitType.DisableHighHand: return "強役つぶし（フルハウス・ストレート・ファイブダイスで返済不可）";
            case BossTraitType.HalfPair: return "ペア半減(ストレート以外の役での返済額を50％カット)";
            default: return "";
        }
    }
}
