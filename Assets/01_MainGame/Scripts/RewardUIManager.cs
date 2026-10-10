using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEditor.ShaderKeywordFilter;

public class RewardUIManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private RewardManager _rewardManager;
    [SerializeField] private DiceManager _diceManager;

    [Header("パネル")]
    [SerializeField] private GameObject _rewardPanel;       // パネル
    [SerializeField] private TextMeshProUGUI _headerText;   // ヘッダーテキスト（残りピック数）
    [SerializeField] private Button _nextStageButton;       // 次へ進む

    [Header("提示シール")]
    [SerializeField] private List<Button> _sealCardButtons;
    [SerializeField] private List<TextMeshProUGUI> _sealCardText;

    [Header("ダイス選択タブ 5個")]
    [SerializeField] private List<Button> _diceTabButtons;
    [SerializeField] private List<TextMeshProUGUI> _diceTabTexts;

    [Header("ダイス面ボタン 6面")]
    [SerializeField] private List<Button> _faceButtons;
    [SerializeField] private List<TextMeshProUGUI> _faceTexts;

    private SealType _selectedSeal = SealType.None;
    private int _selectedDiceIndex = 0;             // 選択中のダイス

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Start()
    {
        // シールカードのクリックイベント登録
        for (int i = 0; i < _sealCardButtons.Count; i++)
        {
            int index = i;
            _sealCardButtons[i].onClick.AddListener(() => OnSealCardClicked(index));
        }

        // ダイスタブのクリックイベント登録
        for (int i = 0; i < _diceTabButtons.Count; i++)
        {
            int index = i;
            _diceTabButtons[i].onClick.AddListener(() => OnDiceTabClicked(index));
        }

        // 面ボタンのクリックイベント
        for (int i = 0; i < _faceButtons.Count; i++)
        {
            int index = i + 1;
            _faceButtons[i].onClick.AddListener(() => OnFaceButtonClicked(index));
        }

        if (_nextStageButton != null)
        {
            _nextStageButton.onClick.AddListener(OnNextStageClicked);
        }

        if (_rewardPanel != null) _rewardPanel.SetActive(false);

    }

    // ===== メソッド =====
    public void OpenRewardUI()
    {
        _selectedSeal = SealType.None;
        _selectedDiceIndex = 0;

        _rewardPanel.SetActive(true);
        UpdateRewardUI();
    }

    public void UpdateRewardUI()
    {
        if (_rewardManager == null || _diceManager == null) return;

        // ヘッダーテキスト
        int remaining = _rewardManager.RemainingPicks;

        _headerText.text = (remaining > 0) ?
            $"【勝利報酬】シールを選んでダイスに貼ってください。(残り：<b>{remaining} 枚</b>)" :
            "【貼り付け完了】『次へ進む』ボタンを押してください。";

        // 提示シールカード
        for (int i = 0; i < _sealCardButtons.Count; i++)
        {
            if (i < _rewardManager.CurrentOfferSeals.Count)
            {
                _sealCardButtons[i].gameObject.SetActive(true);

                SealType seal = _rewardManager.CurrentOfferSeals[i];

                string isSelected = (_selectedSeal == seal) ? "\n【選択中】" : "";

                _sealCardText[i].text = $"<b>{GetSealDisplayName(seal)}</b>\n{GetSealDescription(seal)}{isSelected}";

                _sealCardButtons[i].interactable = remaining > 0;
            }
            else
            {
                _sealCardButtons[i].gameObject.SetActive(false);
            }
        }

        // ダイスタブの表示
        for (int i = 0; i < _diceTabButtons.Count; i++)
        {
            if (i < _diceManager._diceList.Count)
            {
                var dice = _diceManager._diceList[i];

                string isCurrent = (i == _selectedDiceIndex) ? "▶" : "";

                _diceTabTexts[i].text = $"{isCurrent}ダイス[{i + 1}]\n({GetDiceTypeName(dice.Type)})";
            }
        }

        // 選択中ダイスの1-6の面表示
        var currentDice = _diceManager._diceList[_selectedDiceIndex];

        for (int i = 0; i < _faceButtons.Count; i++)
        {
            int faceVal = i + 1;

            SealType sealOnFace = currentDice.GetSealOnFace(faceVal);

            string sealText = (sealOnFace != SealType.None) ? $"<{GetSealDisplayName(sealOnFace)}>" : "[空き]";

            _faceTexts[i].text = $"<b>【{faceVal}の面】</b>\n{sealText}";

            // シールが選択されていて、ピック数が残っていれば貼れる
            _faceButtons[i].interactable = (_selectedSeal != SealType.None) && (remaining > 0);
        }

        // 『次へ進む』ボタン
        if (_nextStageButton != null)
        {
            _nextStageButton.interactable = (remaining <= 0);
        }
    }

    // シールカードがクリックされた時のイベント
    private void OnSealCardClicked(int cardIndex)
    {
        if (cardIndex < _rewardManager.CurrentOfferSeals.Count)
        {
            _selectedSeal = _rewardManager.CurrentOfferSeals[cardIndex];
            UpdateRewardUI();
        }
    }

    // ダイスタブがクリックされたときのイベント
    private void OnDiceTabClicked(int diceIndex)
    {
        _selectedDiceIndex = diceIndex;
        UpdateRewardUI();
    }

    // 面ボタンがクリックされたときのイベント（シールを貼る）
    private void OnFaceButtonClicked(int faceValue)
    {
        if (_selectedSeal == SealType.None) return;

        // シールを貼り付け
        bool success = _rewardManager.ApplySealToDice(_selectedSeal, _selectedDiceIndex, faceValue);

        if (success)
        {
            _selectedSeal = SealType.None;  // 貼れたら選択を解除

            UpdateRewardUI();
        }
    }

    private void OnNextStageClicked()
    {
        _rewardPanel.SetActive(false);

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnStageCompleted();
        }
    }

    // 日本語表示ヘルパー（シール名）
    private string GetSealDisplayName(SealType seal)
    {
        switch (seal)
        {
            case SealType.Fire: return "炎シール";
            case SealType.Ice: return "氷シール";
            case SealType.Gold: return "金箔シール";
            case SealType.Iron: return "鉄シール";
            case SealType.Solo: return "孤高シール";
            case SealType.Gamble: return "博打シール";
            case SealType.Lightning: return "雷シール";
            case SealType.Odd: return "奇数シール";
            case SealType.Even: return "偶数シール";
            default: return "なし";
        }
    }

    // 日本語表示ヘルパー（シール効果）
    private string GetSealDescription(SealType seal)
    {
        switch (seal)
        {
            case SealType.Fire: return "返済額を( 1.2 ～ 3.0 )倍";
            case SealType.Ice: return "ボスを凍結し、特性を無効化";
            case SealType.Gold: return "コインを( 1 ～ 10 )枚獲得";
            case SealType.Iron: return "返済額に( 50 ～ 450 )G 加算";
            case SealType.Solo: return "役に使用されなかったら、返済額に 10 G 加算";
            case SealType.Gamble: return "役成立で返済額を 1.5 倍、不成立で返済額 0 G";
            case SealType.Lightning: return "ゾロ目系役に使用されると返済額( 100 ～ 700 )G 加算";
            case SealType.Odd: return "奇数目に貼られているなら、返済額を( 1.3 ～ 4.1 )倍";
            case SealType.Even: return "偶数目に貼られているなら、返済額を( 1.3 ～ 4.1 )倍";
            default: return "";
        }
    }

    // 日本語表示ヘルパー（ダイス名）
    private string GetDiceTypeName(DiceType type)
    {
        switch (type)
        {
            case DiceType.Odd: return "奇数";
            case DiceType.Even: return "偶数";
            case DiceType.HighRoller: return "高目";
            case DiceType.Pinzoro: return "ピンゾロ";
            case DiceType.Straight: return "連番";
            default: return "通常";
        }
    }
}
