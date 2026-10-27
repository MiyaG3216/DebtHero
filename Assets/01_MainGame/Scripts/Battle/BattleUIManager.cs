using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    private enum CheatMode
    {
        None,
        Flip,
        DirectSet
    }

    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private DiceManager _diceManager;
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private CheatingManager _cheatingManager;
    [SerializeField] private RewardManager _rewardManager;

    [Header("画面上部")]
    [SerializeField] private TextMeshProUGUI _stageNameText;    // ステージ名
    [SerializeField] private TextMeshProUGUI _bossTraitText;    // ボス特性
    [SerializeField] private Slider _quotaSlider;               // 返済メーター(スライダー)
    [SerializeField] private TextMeshProUGUI _quotaText;        // 目標スコア
    [SerializeField] private TextMeshProUGUI _turnsText;        // 残りターン
    [SerializeField] private GameObject _passiveDisplayObject;  // 所持パッシブ表示アイコン
    [SerializeField] private TextMeshProUGUI _passiveDisplayText;

    [Header("画面下部")]
    [SerializeField] private Button _rerollButton;              // リロールボタン
    [SerializeField] private TextMeshProUGUI _rerollButtonText; // リロールボタン文字
    [SerializeField] private Button _submitButton;              // 役確定ボタン
    [SerializeField] private Button _throwButton;               // 投げるボタン

    [Header("画面中央")]
    [SerializeField] private TextMeshProUGUI _coinsText;        // 所持コイン
    [SerializeField] private Button _cheatRerollButton;         // 追加リロールボタン
    [SerializeField] private Button _cheatFlipButton;           // フリップボタン
    [SerializeField] private Button _cheatSetButton;            // 目押しボタン

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

    [Header("イカサマ操作UI")]
    [SerializeField] private TextMeshProUGUI _cheatRemainingText;
    [SerializeField] private GameObject _cheatBunnerPanel;
    [SerializeField] private TextMeshProUGUI _cheatGuideText;
    [SerializeField] private Button _cancelCheatButton;
    [SerializeField] private GameObject _cheatFaceSelectGroup;
    [SerializeField] private Button[] _cheatFaceButtons;

    [Header("リザルト画面")]
    [SerializeField] private GameObject _battleResultPanel;
    [SerializeField] private TextMeshProUGUI _resultTitleText;
    [SerializeField] private TextMeshProUGUI _resultDescText;
    [SerializeField] private GameObject _resultCoinGroup;
    [SerializeField] private TextMeshProUGUI _resultBaseCoinText;
    [SerializeField] private TextMeshProUGUI _resultBonusCoinText;
    [SerializeField] private TextMeshProUGUI _resultTotalCoinText;
    [SerializeField] private Button _resultNextButton;
    [SerializeField] private Button _resultRetryButton;             // リスタートボタン

    private CheatMode _currentCheatMode = CheatMode.None;
    private int _targetDiceIndex = -1;

    private bool _isFinalBossDefeated = false;  // ラスボス撃破フラグ

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (_throwButton != null) _throwButton.onClick.AddListener(OnThrowClicked);

        // 各ボタンにクリックイベントを登録
        _rerollButton.onClick.AddListener(OnRerollClicked);
        _submitButton.onClick.AddListener(OnSubmitClicked);

        // 詳細パネルの次へボタン
        if (_detailConfirmButton != null)
        {
            _detailConfirmButton.onClick.AddListener(OnDetailConfirmClicked);
        }

        // イカサマボタンのイベント登録（フリップと目押しはとりあえず1番ダイスを対象に発動）
        if (_cheatingManager != null)
        {
            _cheatRerollButton.onClick.AddListener(OnCheatRerollClicked);
            _cheatFlipButton.onClick.AddListener(OnStartFlipCheat);
            _cheatSetButton.onClick.AddListener(OnStartDirectSetCheat);
        }

        // 目押しの出目選択ボタン
        for (int i = 0; i < _cheatFaceButtons.Length; i++)
        {
            int faceVal = i + 1;
            _cheatFaceButtons[i].onClick.AddListener(() => OnCheatFaceSelected(faceVal));
        }

        if (_cancelCheatButton != null)
        {
            _cancelCheatButton.onClick.AddListener(CancelCheat);
        }

        // リザルトパネルのボタン登録
        if (_resultNextButton != null) _resultNextButton.onClick.AddListener(OnResultNextClicked);
        if (_resultRetryButton != null) _resultRetryButton.onClick.AddListener(OnResultRetryClicked);

        // 初期UIの更新
        if (_scoreDetailPanel != null) _scoreDetailPanel.SetActive(false);
        if (_cheatBunnerPanel != null) _cheatBunnerPanel.SetActive(false);
        if (_cheatFaceSelectGroup != null) _cheatFaceSelectGroup.SetActive(false);
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
            _quotaSlider.value = _battleManager.CurrentScore;
        }

        if (_quotaText != null)
        {
            _quotaText.text = $"返済額：<b>{_battleManager.CurrentScore}</b> / {_battleManager.TargetQuota} G";
        }

        // 残りターン数
        if (_turnsText != null)
        {
            _turnsText.text = $"残りターン：<b>{_battleManager.RemainingTurns}</b>";
        }

        // 所持コイン
        if (_coinsText != null && PlayerManager.Instance != null)
        {
            _coinsText.text = $"所持コイン：<b>{PlayerManager.Instance.Coins} C</b>";
        }

        // リロール・役確定ボタン(ダイスロール中、詳細パネルOP中、イカサマ中は非表示にする)
        bool isRolling = _diceManager.IsAnyDiceRolling;
        bool isWaitingThrow = _diceManager.IsWaitingForThrow;
        bool isDetailOpen = _scoreDetailPanel != null && _scoreDetailPanel.activeSelf;
        bool isCheating = _currentCheatMode != CheatMode.None;

        if (_throwButton != null)
        {
            _throwButton.gameObject.SetActive(_battleManager.IsBattleActive && isWaitingThrow && !isDetailOpen && !isCheating);
        }

        bool showControls = _battleManager.IsBattleActive && !isRolling && !isWaitingThrow && !isDetailOpen && !isCheating;

        _rerollButton.gameObject.SetActive(showControls);
        _submitButton.gameObject.SetActive(showControls);

        if (showControls)
        {
            _rerollButtonText.text = $"リロール\n(あと{_diceManager.RemainingRerolls}回)";
            _rerollButton.interactable = _diceManager.RemainingRerolls > 0;
            _submitButton.interactable = true;
        }

        // イカサマボタンの活性・非活性
        UpdateCheatButtons(showControls);

        // 所持パッシブの表示
        UpdatePassiveDisplay();
    }

    // リロールボタンが押されたとき
    private void OnRerollClicked()
    {
        _diceManager.PrepareReroll();

        UpdateUI();
    }

    // 役確定ボタンが押されたとき
    private void OnSubmitClicked()
    {
        _diceManager.SubmitHand();
    }

    // 投げるボタンが押されたとき
    private void OnThrowClicked()
    {
        _diceManager.ThrowDice();

        UpdateUI();
    }

    private void OnResultRetryClicked()
    {
        _battleResultPanel.SetActive(false);

        _resultRetryButton.gameObject.SetActive(false);

        // 現在のステージをリトライする
        if (GameDataManager.Instance != null)
        {
            StageData stage = GameDataManager.Instance.GetCurrentStageData();

            if (stage != null)
            {
                BossTraitType trait = stage.BossTrait;

                if (GameDataManager.Instance.CurrentStageIndex == GameDataManager.Instance.StageRoute.Count - 1)
                {
                    trait = GameDataManager.Instance.CurrentFinalBossTrait;
                }

                _battleManager.StartBattle(stage.TargetQuota, stage.MaxTurns, trait, stage.StageName);
                UpdateUI();
                return;
            }
        }

        UpdateUI();
    }

    // リザルトパネルで「次へ」を押したとき
    private void OnResultNextClicked()
    {
        _battleResultPanel.SetActive(false);

        // ラスボス戦後はエンディングへ（未実装）
        if (_isFinalBossDefeated)
        {
            return;
        }

        // 中ボス戦クリア時はパッシブアイテム獲得パネルを先に開く
        bool isMidBoss = false;
        if (GameDataManager.Instance != null)
        {
            StageData stage = GameDataManager.Instance.GetCurrentStageData();
            if (stage != null && stage.BossTrait == BossTraitType.Fee75Percent)
            {
                isMidBoss = true;
            }
        }

        if (_rewardManager != null)
        {
            // 報酬シールを決定
            _rewardManager.PrepareRewardSeals();

            // 中ボス戦の場合は先にパッシブ獲得を開く
            if (isMidBoss)
            {
                var rewardUI = FindAnyObjectByType<RewardUIManager>();
                if (rewardUI != null)
                {
                    rewardUI.OpenPassiveRewardUI();
                    return;
                }
            }

            // それ以外はシール報酬を開く
            _rewardManager.OpenRewardPhaseUI();
        }
    }

    private void OnDetailConfirmClicked()
    {
        _scoreDetailPanel.SetActive(false);
        _battleManager.ConfirmScoreAndAdvanceTurn();
        UpdateUI();
    }

    public void ShowBattleResultPanel(bool isWin, int baseCoin, int bonusCoin, bool missionCleared, bool isFinalBoss = false)
    {
        if (_battleResultPanel == null) return;
        _isFinalBossDefeated = isFinalBoss && isWin;

        if (isWin)
        {
            if (_isFinalBossDefeated)
            {
                _resultTitleText.text = "借金完全返済！ GAME CLEAR";
                _resultDescText.text = "魔王城本社のボスをダイスで粉砕した！";

                if (_resultCoinGroup != null) _resultCoinGroup.SetActive(false);
                _resultNextButton.GetComponentInChildren<TextMeshProUGUI>().text = "エンディングへ";
            }
            else
            {
                _resultTitleText.text = "返済完了！ STAGE CLEAR";
                _resultDescText.text = "取り立て屋を追い返した！";

                // コイン内訳表示
                if (_resultCoinGroup != null) _resultCoinGroup.SetActive(true);
                if (_resultBaseCoinText != null) _resultBaseCoinText.text = $"・返済報酬：+ <b><color=yellow>{baseCoin}</color></b> C";
                if (_resultBonusCoinText != null) _resultBonusCoinText.text = missionCleared ? $"・ミッションボーナス：+  <b><color=yellow>{bonusCoin}</color></b> C" : "・ミッションボーナス：未達成";
                if (_resultTotalCoinText != null) _resultTotalCoinText.text = $"・合計獲得コイン：+  <b><color=yellow>{baseCoin + bonusCoin}</color></b> C";

                _resultNextButton.GetComponentInChildren<TextMeshProUGUI>().text = "次へ";
            }

            _resultNextButton.gameObject.SetActive(true);
            _resultRetryButton.gameObject.SetActive(false);
        }
        else
        {
            _resultTitleText.text = "返済失敗... GAME OVER";
            _resultDescText.text = "期限内に返済できなかった...";

            if (_resultCoinGroup != null) _resultCoinGroup.SetActive(false);

            _resultNextButton.gameObject.SetActive(false);
            _resultRetryButton.gameObject.SetActive(true);
        }

        _battleResultPanel.SetActive(true);
        UpdateUI();
    }

    public void ShowScoreDetailPanel(ScoreBreakdown breakdown)
    {
        if (_scoreDetailPanel == null) return;

        _detailHandNameText.text = $"成立役：<b>{breakdown.HandName}</b>";
        _detailHandBaseScoreText.text = $"役の金額：<b>{breakdown.HandBaseScore} G</b>";
        _detailUnusedDiceScoreText.text = $"役未使用ダイス：<b>{breakdown.UnusedDiceScore} G</b>";
        _detailSealAddScoreText.text = $"シール加算：<b>{breakdown.SealAddScore} G</b>";
        _detailTotalBaseText.text = $"小計：<b>{breakdown.TotalBaseScore} G</b>";
        _detailMultiplierText.text = $"倍率：<b>× {breakdown.Multiplier:F2} 倍</b>";
        _detailBossModifierText.text = (breakdown.BossTraitText != "") ? $"ボス補正：<size=65>{breakdown.BossTraitText}</size>" : "";
        _detailCoinsText.text = (breakdown.EarnedCoins > 0) ? $"獲得コイン：+ {breakdown.EarnedCoins} 枚" : "";
        _detailFinalScoreText.text = $"返済金額：<color=yellow><size=80><b>+ {breakdown.FinalScore} G</b></size></color>";

        _scoreDetailPanel.SetActive(true);
        UpdateUI();
    }

    // イカサマボタン群の制御
    private void UpdateCheatButtons(bool canInteract)
    {
        if (_cheatingManager == null || PlayerManager.Instance == null) return;

        int coins = (GameDataManager.Instance != null) ? GameDataManager.Instance.Coins : 0;
        int remaining = _cheatingManager.RemainingCheats;
        bool hasRemaining = _cheatingManager.CanCheatAny;


        // 残り回数テキストの更新
        if (_cheatRemainingText != null)
        {
            string turnPrefix = _cheatingManager.IsBossBattle ? "今ターン" : "残り";
            if(hasRemaining)
            {
                _cheatRemainingText.text = $"イカサマ <color=red>({turnPrefix} {remaining} 回)</color>";
            }
            else
            {
                _cheatRemainingText.text = "イカサマ <color=blue>(使用済み)</color>";
            }
        }

        // ボタンの活性化(残り回数またはコイン不足で不活性)
        bool canCheat = canInteract && hasRemaining;
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
                _bossTraitText.text = $"<color=red>【特性】\n{GetTraitName(trait)}</color>";
            }
            else
            {
                _bossTraitText.gameObject.SetActive(false);
            }
        }
    }

    private void UpdatePassiveDisplay()
    {
        if (_passiveDisplayObject == null || _passiveDisplayText == null) return;

        // パッシブを所持していれば表示、なければ非表示
        if (GameDataManager.Instance != null && GameDataManager.Instance.AcquiredPassive.Count > 0)
        {
            _passiveDisplayObject.SetActive(true);

            List<string> passiveInfos = new List<string>();
            foreach (var p in GameDataManager.Instance.AcquiredPassive)
            {
                passiveInfos.Add($"【{PassiveHelper.GetName(p)}】\n\n{PassiveHelper.GetDescription(p)}");
            }
            _passiveDisplayText.text = string.Join("", passiveInfos);
        }
        else
        {
            _passiveDisplayObject.SetActive(false);
        }
    }

    private string GetTraitName(BossTraitType trait)
    {
        switch (trait)
        {
            case BossTraitType.Fee75Percent: return "みかじめ料(返済額 25％ カット)";
            case BossTraitType.DisableEven: return "偶数無効（2,4,6を含む役で返済不可）";
            case BossTraitType.DisableOdd: return "奇数無効（1,3,5を含む役で返済不可）";
            case BossTraitType.DisableHighHand: return "強役つぶし（FH・St・FDで返済不可）";
            case BossTraitType.HalfPair: return "ペア半減(St以外での返済額を50％カット)";
            default: return "";
        }
    }

    // 追加リロールボタンクリック時処理
    private void OnCheatRerollClicked()
    {
        if (_cheatingManager.TryUseExtraReroll()) UpdateUI();
    }

    // フリップボタンクリック時処理
    private void OnStartFlipCheat()
    {
        _currentCheatMode = CheatMode.Flip;
        _targetDiceIndex = -1;

        _cheatGuideText.text = "裏返したいダイスを選んでください。";

        _cheatBunnerPanel.SetActive(true);

        if (_cheatFaceSelectGroup != null) _cheatFaceSelectGroup.SetActive(false);

        UpdateUI();
    }

    // 目押しボタンクリック時処理
    private void OnStartDirectSetCheat()
    {
        _currentCheatMode = CheatMode.DirectSet;
        _targetDiceIndex = -1;

        _cheatGuideText.text = "目を変更したいダイスを選んでください。";

        _cheatBunnerPanel.SetActive(true);

        if (_cheatFaceSelectGroup != null) _cheatFaceSelectGroup.SetActive(false);

        UpdateUI();
    }

    public void Handle3DDiceClicked(int diceIndex)
    {
        // イカサマ中でなければキープの切り替え
        if (_currentCheatMode == CheatMode.None)
        {
            _diceManager.ToggleKeepDice(diceIndex);
            return;
        }

        // フリップ中
        if (_currentCheatMode == CheatMode.Flip)
        {
            _cheatingManager.TryUseDiceFlip(diceIndex);
            EndCheat();
        }

        if (_currentCheatMode == CheatMode.DirectSet)
        {
            _targetDiceIndex = diceIndex;

            _cheatGuideText.text = $"ダイス {diceIndex + 1} をどの目に変更しますか？";

            if (_cheatFaceSelectGroup != null) _cheatFaceSelectGroup.SetActive(true);
        }
    }

    private void OnCheatFaceSelected(int faceVal)
    {
        if (_targetDiceIndex >= 0)
        {
            _cheatingManager.TryUseDirectSet(_targetDiceIndex, faceVal);
            EndCheat();
        }
    }

    private void CancelCheat()
    {
        EndCheat();
    }

    private void EndCheat()
    {
        _currentCheatMode = CheatMode.None;
        _targetDiceIndex = -1;

        if (_cheatBunnerPanel != null) _cheatBunnerPanel.SetActive(false);
        if (_cheatFaceSelectGroup != null) _cheatFaceSelectGroup.SetActive(false);

        UpdateUI();
    }
}
