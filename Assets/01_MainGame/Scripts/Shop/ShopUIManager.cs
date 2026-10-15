using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.Tests.CoreCLR.TestJobs;

public class ShopUIManager : MonoBehaviour
{
    private enum ShopModalMode
    {
        None,
        BuyDice,
        ApplyPackSeal
    }

    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private ShopManager _shopManager;

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

    [Header("商品（シールパック）")]
    [SerializeField] private Button[] _packButtons;             // シールパック
    [SerializeField] private TextMeshProUGUI[] _packTexts;      // パック説明

    [Header("ダイスカスタマイズパネル")]
    [SerializeField] private GameObject _customizeModalPanel;   // ダイスカスタマイズパネル
    [SerializeField] private TextMeshProUGUI _modalTitleText;
    [SerializeField] private Button[] _diceTabButtons;          // ダイス切り替えタブ
    [SerializeField] private TextMeshProUGUI[] _diceTabTexts;   // タブテキスト
    [SerializeField] private Button[] _faceButtons;             // ダイス面
    [SerializeField] private TextMeshProUGUI[] _faceTexts;      // ダイス面テキスト
    [SerializeField] private Button _confirmButton;             // 決定ボタン
    [SerializeField] private TextMeshProUGUI _confirmButtonText;// 決定ボタンテキスト
    [SerializeField] private Button _cancelModalButton;         // 戻るボタン


    private ShopModalMode _currentMode = ShopModalMode.None;
    private DiceType _pendingDiceType;  // 購入予定ダイスタイプ
    private int _selectedDiceIndex = 0;
    private int _selectedFaceValue = -1;

    private SealPackData _currentPack;  // 購入予定シールパック
    private int _packSealStepIndex = 0; // パック購入時貼り付け済みシール数

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Start()
    {
        // ダイス購入イベント
        _buyOddDiceButton.onClick.AddListener(() => OpenModalForDice(DiceType.Odd));
        _buyEvenDiceButton.onClick.AddListener(() => OpenModalForDice(DiceType.Even));
        _buyStraightDiceButton.onClick.AddListener(() => OpenModalForDice(DiceType.Straight));
        _buyHighRollerDiceButton.onClick.AddListener(() => OpenModalForDice(DiceType.HighRoller));
        _buyPinZoroDiceButton.onClick.AddListener(() => OpenModalForDice(DiceType.Pinzoro));

        // シール購入イベント
        for (int i = 0; i < _packButtons.Length; i++)
        {
            int index = i;
            _packButtons[i].onClick.AddListener(() => StartPackPurchase(index));
        }

        // ダイスタブ選択
        for (int i = 0; i < _diceTabButtons.Length; i++)
        {
            int index = i;
            _diceTabButtons[i].onClick.AddListener(() => OnDiceTabSelected(index));
        }

        // 面選択
        for (int i = 0; i < _faceButtons.Length; i++)
        {
            int faceVal = i + 1;
            _faceButtons[i].onClick.AddListener(() => OnFaceSelected(faceVal));
        }

        _confirmButton.onClick.AddListener(OnConfirmClicked);
        _cancelModalButton.onClick.AddListener(CloseModal);
        _leaveShopButton.onClick.AddListener(OnLeaveShopClicked);

        if (_shopPanel != null) _shopPanel.SetActive(true);
        if (_customizeModalPanel != null) _customizeModalPanel.SetActive(false);
    }

    // ===== メソッド =====
    public void OpenShopUI(string hint, int visitCount)
    {
        _shopPanel.SetActive(true);
        if (_customizeModalPanel != null) _customizeModalPanel.SetActive(false);

        _shopTitleText.text = (visitCount == 1) ? "【よろずや】" : "【裏カジノショップ】";
        _bossHintText.text = $"<size=40>情報屋の噂話：</size>\n『<b>{hint}</b>』";

        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        int coins = (GameDataManager.Instance != null) ? GameDataManager.Instance.Coins : 0;
        _coinsText.text = $"所持金：<color=yellow><b>{coins} C</b></color>";

        if (_shopManager != null)
        {
            // 価格テキストの更新
            UpdateDiceButtonDisplay(_buyOddDiceButton, _oddDiceText, DiceType.Odd, coins);
            UpdateDiceButtonDisplay(_buyEvenDiceButton, _evenDiceText, DiceType.Even, coins);
            UpdateDiceButtonDisplay(_buyStraightDiceButton, _straightDiceText, DiceType.Straight, coins);
            UpdateDiceButtonDisplay(_buyHighRollerDiceButton, _highrollerDiceText, DiceType.HighRoller, coins);
            UpdateDiceButtonDisplay(_buyPinZoroDiceButton, _pinzoroDiceText, DiceType.Pinzoro, coins);

            // シールパック購入ボタン
            for (int i = 0; i < _packButtons.Length; i++)
            {
                if (i < _shopManager.SealPacks.Count)
                {
                    _packButtons[i].gameObject.SetActive(true);

                    var pack = _shopManager.SealPacks[i];
                    _packTexts[i].text = $"{pack.PackName}( {pack.Price} C )\n{pack.Description}";
                    _packButtons[i].interactable = (coins >= pack.Price);
                }
                else
                {
                    _packButtons[i].gameObject.SetActive(false);
                }
            }
        }

    }

    private void UpdateDiceButtonDisplay(Button btn, TextMeshProUGUI txt, DiceType type, int coins)
    {
        int price = _shopManager.GetDicePrice(type);
        string desc = _shopManager.GetDiceDescription(type);

        txt.text = $"{GetDiceTypeName(type)}( {price} C)\n{desc}";

        btn.interactable = (coins >= price);
    }

    // ダイス購入時開く
    private void OpenModalForDice(DiceType diceType)
    {
        _currentMode = ShopModalMode.BuyDice;
        _pendingDiceType = diceType;
        _selectedDiceIndex = 0;
        _selectedFaceValue = -1;

        _modalTitleText.text = $"<<color=red>{GetDiceTypeName(diceType)}</color>>をどのダイスと置き替えますか？";
        _confirmButtonText.text = "決定";
        _confirmButton.gameObject.SetActive(true);
        _cancelModalButton.interactable = true;

        _customizeModalPanel.SetActive(true);
        UpdateModalUI();
    }

    // パック購入フロー
    private void StartPackPurchase(int packIndex)
    {
        if (packIndex >= _shopManager.SealPacks.Count) return;

        _currentPack = _shopManager.SealPacks[packIndex];
        _packSealStepIndex = 0;
        _currentMode = ShopModalMode.ApplyPackSeal;

        _selectedDiceIndex = 0;
        _selectedFaceValue = -1;

        _customizeModalPanel.SetActive(true);
        SetupPackSealStep();
    }

    private void SetupPackSealStep()
    {
        SealType currentSeal = _currentPack.Seals[_packSealStepIndex];
        int stepNumber = _packSealStepIndex + 1;

        _modalTitleText.text = $"[{stepNumber}/3枚目]【{GetSealName(currentSeal)}】を貼る面を選ぼう。";
        _confirmButtonText.text = (stepNumber < 3) ? "貼る" : "完了";

        // 1枚目貼る前のみ戻る可能
        _cancelModalButton.interactable = (_packSealStepIndex == 0);

        _selectedFaceValue = -1;
        UpdateModalUI();
    }

    // ダイスタブ選択時
    private void OnDiceTabSelected(int index)
    {
        _selectedDiceIndex = index;
        _selectedFaceValue = -1;

        UpdateModalUI();
    }

    // 面がクリックされた
    private void OnFaceSelected(int faceValue)
    {
        if (_currentMode == ShopModalMode.ApplyPackSeal)
        {
            _selectedFaceValue = faceValue;
            UpdateModalUI();
        }
    }

    // パネル全体のUI更新
    private void UpdateModalUI()
    {
        var diceList = (GameDataManager.Instance != null) ? GameDataManager.Instance.PlayerDiceList : null;
        if (diceList == null) return;

        // ダイスタブの更新
        for (int i = 0; i < _diceTabButtons.Length; i++)
        {
            if (i < diceList.Count)
            {
                var dice = diceList[i];
                string isCurrent = (i == _selectedDiceIndex) ? "▶" : "";
                string typeName = GetDiceTypeName(dice.Type);

                _diceTabTexts[i].text = $"{isCurrent} ダイス {i + 1}\n[{typeName}]";
            }
        }

        // 6面の表示
        var currentDice = diceList[_selectedDiceIndex];
        for (int i = 0; i < _faceButtons.Length; i++)
        {
            int faceVal = i + 1;
            SealType seal = currentDice.GetSealOnFace(faceVal);

            string sealText = (seal != SealType.None) ? $"<size=30><{GetSealName(seal)}></size>" : "[空き]";

            // シールモードで選択中の面をハイライト
            string selectedMark = (_currentMode == ShopModalMode.ApplyPackSeal && _selectedFaceValue == faceVal) ?
                "\n<color=yellow>【選択中】</color>" : "";

            _faceTexts[i].text = $"{faceVal}\n{sealText}{selectedMark}";

            // ダイス購入時は面クリック不可、シール購入時はクリック可
            _faceButtons[i].interactable = (_currentMode == ShopModalMode.ApplyPackSeal);
        }

        // 決定ボタンの活性化
        if (_currentMode == ShopModalMode.BuyDice)
        {
            // ダイス購入時：ダイススロットを選んでいれば活性化
            _confirmButton.interactable = true;
        }
        else if (_currentMode == ShopModalMode.ApplyPackSeal)
        {
            // シール購入時：貼る面を選択すると活性化
            _confirmButton.interactable = (_selectedFaceValue >= 1 && _selectedFaceValue <= 6);
        }

    }

    // 決定ボタンが押されたとき
    private void OnConfirmClicked()
    {
        if (_currentMode == ShopModalMode.BuyDice)
        {
            if (_shopManager.TryBuyDice(_pendingDiceType, _selectedDiceIndex))
            {
                CloseModal();
            }
        }
        else if (_currentMode == ShopModalMode.ApplyPackSeal)
        {
            // 現在のシールを張り付け
            SealType seal = _currentPack.Seals[_packSealStepIndex];
            _shopManager.ApplySeal(seal,_selectedDiceIndex,_selectedFaceValue);

            _packSealStepIndex++;

            if(_packSealStepIndex < _currentPack.Seals.Count)
            {
                // まだ貼り付けできるシールが残っているなら次のシール貼り付けへ
                SetupPackSealStep();
            }
            else
            {
                // 3枚すべて貼り付けたらコインを消費
                _shopManager.SpendPackCoins(_currentPack.Price);
                CloseModal();
            }
        }
    }

    private void CloseModal()
    {
        _currentMode = ShopModalMode.None;
        _currentPack = null;
        _packSealStepIndex = 0;
        _selectedFaceValue = -1;

        if (_customizeModalPanel != null) _customizeModalPanel.SetActive(false);

        UpdateShopUI();
    }

    // 店を出て次のステージへ進む
    private void OnLeaveShopClicked()
    {
        _shopPanel.SetActive(false);

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentStageIndex++;
            SceneManager.LoadScene("Scene_Map");
        }
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

    private string GetDiceTypeName(DiceType diceType)
    {
        switch (diceType)
        {
            case DiceType.Odd: return "奇数ダイス";
            case DiceType.Even: return "偶数ダイス";
            case DiceType.Straight: return "連番ダイス";
            case DiceType.HighRoller: return "高目ダイス";
            case DiceType.Pinzoro: return "ピンゾロダイス";
            default: return "通常ダイス";
        }
    }
}
