using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DiceTooltipUI : MonoBehaviour
{
    public static DiceTooltipUI Instance { get; private set; }

    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private GameObject _tooltipPanel;      // パネル
    [SerializeField] private RectTransform _tooltipRect;    // パネルのRecttransform
    [SerializeField] private TextMeshProUGUI _titleText;    // ダイス名・種類・出目
    [SerializeField] private TextMeshProUGUI _sealListText; // シール一覧

    [Header("位置オフセット")]
    [SerializeField] private Vector2 _offset = new Vector2(25f, -25f);  // カーソルからのずれ

    private bool _isShowing = false;

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_tooltipPanel != null && _tooltipRect == null)
        {
            _tooltipRect = _tooltipPanel.GetComponent<RectTransform>();
        }

        Hide();
    }

    private void Update()
    {
        if (!_isShowing || _tooltipPanel == null) return;

        // マウス座標を取得して追従させる
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            _tooltipRect.position = mousePos + _offset;
        }
    }

    // ===== メソッド =====
    // パネルを表示する
    public void Show(Dice dice, int diceIndex)
    {
        if (dice == null || _tooltipPanel == null) return;

        // タイトル行
        string typeName = GetDiceTypeName(dice.Type);
        _titleText.text = $"{typeName} [出目：{dice.Value}]";

        // シール一覧を2行で表示
        string line1 = $"{FormatFaceSeal(dice, 1)}  {FormatFaceSeal(dice, 2)}  {FormatFaceSeal(dice, 3)}";
        string line2 = $"{FormatFaceSeal(dice, 4)}  {FormatFaceSeal(dice, 5)}  {FormatFaceSeal(dice, 6)}";

        _sealListText.text = $"{line1}\n{line2}";

        _tooltipPanel.SetActive(true);
        _isShowing = true;
    }

    // パネルを非表示にする
    public void Hide()
    {
        if (_tooltipPanel != null) _tooltipPanel.SetActive(false);
        _isShowing = false;
    }

    // シール表示
    private string FormatFaceSeal(Dice dice, int faceValue)
    {
        SealType seal = dice.GetSealOnFace(faceValue);
        if (seal != SealType.None)
        {
            return $"{faceValue}:{GetSealName(seal)}";
        }
        return $"{faceValue}:[空き]";
    }

    // シール名取得
    private string GetSealName(SealType seal)
    {
        switch (seal)
        {
            case SealType.Fire:return "炎";
            case SealType.Ice:return "氷";
            case SealType.Gold:return "金箔";
            case SealType.Iron:return "鉄";
            case SealType.Solo:return "孤高";
            case SealType.Gamble:return "博打";
            case SealType.Lightning:return "雷";
            case SealType.Odd:return "奇数";
            case SealType.Even:return "偶数";
            default:return "";
        }
    }

    private string GetDiceTypeName(DiceType type)
    {
        switch (type)
        {
            case DiceType.Odd:return "奇数ダイス";
            case DiceType.Even:return "偶数ダイス";
            case DiceType.HighRoller:return "高目ダイス";
            case DiceType.Straight:return "連番ダイス";
            case DiceType.LowRoller:return "低目ダイス";
            default:return "";
        }
    }
}
