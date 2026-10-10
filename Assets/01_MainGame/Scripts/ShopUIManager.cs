using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class ShopUIManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private ShopManager _shopManager;
    [SerializeField] private DiceManager _diceManager;

    [Header("ショップパネル")]
    [SerializeField] private GameObject _shopPanel;         // ショップパネル
    [SerializeField] private TextMeshProUGUI _shopTitleText;// ショップ名
    [SerializeField] private TextMeshProUGUI _coinsText;    //  所持コイン
    [SerializeField] private TextMeshProUGUI _bossHintText; // ボス特性のヒント
    [SerializeField] private Button _leaveShopButton;       // 店を出るボタン

    [Header("商品（ダイス）")]
    [SerializeField] private Button _buyOddDiceButton;          // 奇数ダイス
    [SerializeField] private TextMeshProUGUI _oddDiceText;
    [SerializeField] private Button _buyEvenDiceButton;         // 偶数ダイス
    [SerializeField] private TextMeshProUGUI _evenDiceText;
    [SerializeField] private Button _buyStraightDiceButton;     // 連番ダイス
    [SerializeField] private TextMeshProUGUI _straightDiceText;
    [SerializeField] private Button _buyHighRollerDiceButton;   // 高目ダイス
    [SerializeField] private TextMeshProUGUI _highrollerDiceText;
    [SerializeField] private Button _buyPinZoroDiceButton;      // ピンゾロダイス
    [SerializeField] private TextMeshProUGUI _pinzoroDiceText;

    [Header("商品（シール）")]
    [SerializeField] private Button _buyLightningSealButton;    // 雷シール
    [SerializeField] private Button _buyOddSealButton;          // 奇数シール
    [SerializeField] private Button _buyEvenSealButton;         // 偶数シール

    [Header("ダイス選択サブパネル")]
    [SerializeField] private GameObject _slotSelectPanel;           // ダイススロット選択ウィンドウ
    [SerializeField] private TextMeshProUGUI _slotSelectTitleText;
    [SerializeField] private Button _cancelSlotSelectButton;        // 戻るボタン
    [SerializeField] private Button[] _slotButtons;                 // ダイススロット1～5

    [Header("シール貼り付け用サブパネル")]
    [SerializeField] private GameObject _applySealPanel;            // シール貼り付け用パネル
    [SerializeField] private TextMeshProUGUI _applySealTitleText;
    [SerializeField] private Button _cancelApplySealButton;         // 戻るボタン
    [SerializeField] private Button[] _sealDiceTabButtons;          // ダイス切り替えタブ
    [SerializeField] private TextMeshProUGUI[] _sealDiceTabTexts;   // タブテキスト
    [SerializeField] private Button[] _sealFaceButtons;             // ダイス面
    [SerializeField] private TextMeshProUGUI[] _sealFaceTexts;      // ダイス面テキスト

    private DiceType _pendingDiceType;  // 購入予定ダイスタイプ
    private SealType _pendingSealType;  // 購入予定シールタイプ
    private int _selectedDiceIndexForSeal = 0;
    private int _currentVisitCount = 1;

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Start()
    {
        // ダイス購入イベント
        _buyOddDiceButton.onClick.AddListener(() => OpenSlotSelect(DiceType.Odd));
        _buyEvenDiceButton.onClick.AddListener(() => OpenSlotSelect(DiceType.Even));
        _buyStraightDiceButton.onClick.AddListener(() => OpenSlotSelect(DiceType.Straight));
        _buyHighRollerDiceButton.onClick.AddListener(() => OpenSlotSelect(DiceType.HighRoller));
        _buyPinZoroDiceButton.onClick.AddListener(() => OpenSlotSelect(DiceType.Pinzoro));

        // シール購入イベント
        _buyLightningSealButton.onClick.AddListener(() => OpenSealApplyPanel(SealType.Lightning));
        _buyOddSealButton.onClick.AddListener(() => OpenSealApplyPanel(SealType.Odd));
        _buyEvenSealButton.onClick.AddListener(() => OpenSealApplyPanel(SealType.Even));

        // スロット選択ボタン
        for (int i = 0; i < _slotButtons.Length; i++)
        {
            int index = i;
            _slotButtons[i].onClick.AddListener(() => OnSlotSelected(index));
        }

        // シール貼り付け用：ダイスタブ選択
        for (int i = 0; i < _sealDiceTabButtons.Length; i++)
        {
            int index = i;
            _sealDiceTabButtons[i].onClick.AddListener(() => { _selectedDiceIndexForSeal = index; UpdateSealApplyUI(); });
        }

        // シール貼り付け用：面選択
        for (int i = 0; i < _sealFaceButtons.Length; i++)
        {
            int faceVal = i + 1;
            _sealFaceButtons[i].onClick.AddListener(() => OnFaceSelectedForSeal(faceVal));
        }

        // 店を出るボタン
        _leaveShopButton.onClick.AddListener(OnLeaveShopClicked);

        // 戻るボタン
        if (_cancelSlotSelectButton != null) _cancelSlotSelectButton.onClick.AddListener(() => _slotSelectPanel.SetActive(false));
        if (_cancelApplySealButton != null) _cancelApplySealButton.onClick.AddListener(() => _applySealPanel.SetActive(false));

        // 各種パネルを非表示に
        if (_shopPanel != null) _shopPanel.SetActive(false);
        if (_slotSelectPanel != null) _slotSelectPanel.SetActive(false);
        if (_applySealPanel != null) _applySealPanel.SetActive(false);
    }

    // ===== メソッド =====
    public void OpenShopUI(string hint, int visitCount)
    {
        _currentVisitCount = visitCount;

        _shopPanel.SetActive(true);

        if (_slotSelectPanel != null) _slotSelectPanel.SetActive(false);
        if (_applySealPanel != null) _applySealPanel.SetActive(false);

        _shopTitleText.text = (visitCount == 1) ? "【よろずや】" : "【裏カジノショップ】";

        _bossHintText.text = $"<size=40>情報屋の噂話：</size>\n『<b>{hint}</b>』";

        // ショップに応じて表示商品の切り替え
        bool isFirstShop = (visitCount == 1);
        _buyOddDiceButton.gameObject.SetActive(isFirstShop);
        _buyEvenDiceButton.gameObject.SetActive(isFirstShop);
        _buyStraightDiceButton.gameObject.SetActive(!isFirstShop);
        _buyHighRollerDiceButton.gameObject.SetActive(!isFirstShop);
        _buyPinZoroDiceButton.gameObject.SetActive(!isFirstShop);

        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        if (PlayerManager.Instance == null) return;

        int coins = PlayerManager.Instance.Coins;
        _coinsText.text = $"所持金：<color=yellow><b>{coins}</b></color>";

        // 価格テキストの更新
        _oddDiceText.text = $"奇数ダイス\n( {_shopManager.GetDicePrice(DiceType.Odd)} C )";
        _evenDiceText.text = $"偶数ダイス\n( {_shopManager.GetDicePrice(DiceType.Even)} C )";
        _straightDiceText.text = $"連番ダイス\n( {_shopManager.GetDicePrice(DiceType.Straight)} C )";
        _highrollerDiceText.text = $"高目ダイス\n( {_shopManager.GetDicePrice(DiceType.HighRoller)} C )";
        _pinzoroDiceText.text = $"ピンゾロダイス\n( {_shopManager.GetDicePrice(DiceType.Pinzoro)} C )";

        // コイン不足ならボタンをグレーアウト
        _buyOddDiceButton.interactable = (coins >= _shopManager.GetDicePrice(DiceType.Odd));
        _buyEvenDiceButton.interactable = (coins >= _shopManager.GetDicePrice(DiceType.Even));
        _buyStraightDiceButton.interactable = (coins >= _shopManager.GetDicePrice(DiceType.Straight));
        _buyHighRollerDiceButton.interactable = (coins >= _shopManager.GetDicePrice(DiceType.HighRoller));
        _buyPinZoroDiceButton.interactable = (coins >= _shopManager.GetDicePrice(DiceType.Pinzoro));

        int sealPrice = _shopManager.RareSealPrice;
        _buyLightningSealButton.interactable = (coins >= sealPrice);
        _buyOddSealButton.interactable = (coins >= sealPrice);
        _buyEvenSealButton.interactable = (coins >= sealPrice);
    }

    // ダイス購入時：どのスロットと入れ替えるかを選ぶパネルを開く
    private void OpenSlotSelect(DiceType diceType)
    {
        _pendingDiceType = diceType;

        _slotSelectTitleText.text = $"<<color=red>{GetDiceName(diceType)}</color>>をどのダイスと入れ替えますか？";

        if (_slotSelectPanel != null) _slotSelectPanel.SetActive(true);
    }

    // スロットが選ばれたらダイスを購入・入れ替え
    private void OnSlotSelected(int slotIndex)
    {
        bool success = _shopManager.TryBuyDice(_pendingDiceType, slotIndex);

        if (success)
        {
            if (_slotSelectPanel != null) _slotSelectPanel.SetActive(false);
            UpdateShopUI();
        }
    }

    // シール購入(貼り付け用パネルを開く)
    private void OpenSealApplyPanel(SealType seal)
    {
        _pendingSealType = seal;
        _selectedDiceIndexForSeal = 0;
        _applySealTitleText.text = $"<<color=red>{GetSealName(seal)}</color>>をどの面に貼りますか？";

        if (_applySealPanel != null) _applySealPanel.SetActive(true);

        UpdateSealApplyUI();
    }

    // シール貼り付け画面タブ・面表示更新
    private void UpdateSealApplyUI()
    {
        for (int i = 0; i < _sealDiceTabTexts.Length; i++)
        {
            if (i < _diceManager._diceList.Count)
            {
                var dice = _diceManager._diceList[i];

                string isCurrent = (i == _selectedDiceIndexForSeal) ? "▶" : "";

                _sealDiceTabTexts[i].text = $"{isCurrent} ダイス {i + 1}";
            }
        }

        var currentDice = _diceManager._diceList[_selectedDiceIndexForSeal];

        for (int i = 0; i < _sealFaceButtons.Length; i++)
        {
            int faceVal = i + 1;
            SealType currentSeal = currentDice.GetSealOnFace(faceVal);

            string sealText = (currentSeal != SealType.None) ? $"<{GetSealName(currentSeal)}>" : "[空き]";
            _sealFaceTexts[i].text = $"【{faceVal}面】\n{sealText}";
        }
    }

    private void OnFaceSelectedForSeal(int faceVal)
    {
        if (_shopManager.TryBuyAndApplySeal(_pendingSealType, _selectedDiceIndexForSeal, faceVal))
        {
            if (_applySealPanel != null)
            {
                _applySealPanel.SetActive(false);
                UpdateShopUI();
            }
        }
    }

    // 店を出て次のステージへ進む
    private void OnLeaveShopClicked()
    {
        _shopPanel.SetActive(false);
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.OnStageCompleted();
    }

    private string GetSealName(SealType seal)
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
            default: return "";
        }
    }

    private string GetDiceName(DiceType diceType)
    {
        switch (diceType)
        {
            case DiceType.Odd: return "奇数ダイス";
            case DiceType.Even: return "偶数ダイス";
            case DiceType.Straight: return "連番ダイス";
            case DiceType.HighRoller: return "高目ダイス";
            case DiceType.Pinzoro: return "ピンゾロダイス";
            default: return "";
        }
    }
}
