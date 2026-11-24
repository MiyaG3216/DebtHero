using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class DialogController : MonoBehaviour
{
    // ===== フィールド =====
    [Header("UI")]
    [SerializeField] private Image _backgroundImage;            // 背景
    [SerializeField] private Image _enemyPortraitImage;         // 敵キャラ立ち絵
    [SerializeField] private RectTransform _enemyPortraitRect;  // 立ち絵位置
    [SerializeField] private TextMeshProUGUI _speakerNameText;  // 話者名表示
    [SerializeField] private TextMeshProUGUI _messageText;      // セリフ
    [SerializeField] private Button _nextButton;                // 次へボタン
    [SerializeField] private Button _skipButton;                // スキップボタン

    [Header("ダメージ演出")]
    [SerializeField] private float _damageShakeIntensity = 25f; // 揺れの強さ
    [SerializeField] private float _sinkDistance = 180f;        // 沈む深さ
    [SerializeField] private float _sinkDuration = 0.8f;        // 沈下と透明化にかかる時間

    private DialogEvent _currentEvent;
    private int _lineIndex = 0;
    private Vector3 _originalPortraitPos;
    private Coroutine _shakeCoroutine;

    // ==== プロパティ ====
    public static DialogTiming CurrentTiming = DialogTiming.Opning;

    // ===== Unityメッセージ =====
    private void Start()
    {
        // 初期位置登録
        if (_enemyPortraitRect != null) _originalPortraitPos = _enemyPortraitRect.localPosition;

        // ボタン
        _nextButton.onClick.AddListener(OnNextButtonClicked);
        if (_skipButton != null) _skipButton.onClick.AddListener(EndDialog);

        // 現在の進行に応じた会話データを取得
        int currentStage = (GameDataManager.Instance != null) ? GameDataManager.Instance.CurrentStageIndex : 0;
        if (StoryScenarioManager.Instance != null) _currentEvent = StoryScenarioManager.Instance.GetCurrentDialog(CurrentTiming, currentStage);

        // もし該当の会話シーンがなければ次のシーンへ
        if (_currentEvent == null || _currentEvent.Lines.Count == 0)
        {
            EndDialog();
            return;
        }

        // 背景と立ち絵の初期セット
        if (_enemyPortraitImage != null && _currentEvent.EnemyPortrait != null)
        {
            _enemyPortraitImage.sprite = _currentEvent.EnemyPortrait;
            _enemyPortraitImage.gameObject.SetActive(true);
        }
        if (_backgroundImage != null && _currentEvent.BackgroundImage != null)
        {
            _backgroundImage.sprite = _currentEvent.BackgroundImage;
            _backgroundImage.gameObject.SetActive(true);
        }

        _lineIndex = 0;
        DisplayCurrentLine();
    }

    // ===== メソッド =====
    // セリフを1行表示
    private void DisplayCurrentLine()
    {
        if (_currentEvent == null || _lineIndex >= _currentEvent.Lines.Count)
        {
            EndDialog();
            return;
        }

        var line = _currentEvent.Lines[_lineIndex];
        _speakerNameText.text = line.SpeakerName;
        _messageText.text = line.Message;

        // 敵を倒したダメージ演出
        if (line.TriggerEnemyDamage)
        {
            PlayEnemyDamageEffect();
        }
    }

    // クリックされたとき次のセリフへ
    private void OnNextButtonClicked()
    {
        _lineIndex++;
        DisplayCurrentLine();
    }

    // 敵のダメージ演出
    private void PlayEnemyDamageEffect()
    {
        if (_enemyPortraitImage == null || _enemyPortraitRect == null) return;

        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(EnemyDamageRoutine());
    }

    private IEnumerator EnemyDamageRoutine()
    {
        // 赤くする
        _enemyPortraitImage.color = Color.red;

        // 左右に揺らす
        float elapsed = 0f;
        float duration = 0.45f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-3f, 3f) * _damageShakeIntensity;
            float offsetY = Random.Range(-3f, 3f) * _damageShakeIntensity * 0.5f;

            _enemyPortraitRect.localPosition = _originalPortraitPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // 色と位置を戻す
        _enemyPortraitRect.localPosition = _originalPortraitPos;
        _enemyPortraitImage.color = Color.wheat;

        // 倒れる（沈む）演出 + 透明化
        elapsed = 0f;
        Vector3 sinkEndPos = _originalPortraitPos - new Vector3(0f, _sinkDistance, 0f);
        while (elapsed< _sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f,1f,elapsed/_sinkDuration);

            // ゆっくり沈む
            _enemyPortraitRect.localPosition = Vector3.Lerp(_originalPortraitPos, sinkEndPos, t);

            // 徐々に透明化
            _enemyPortraitImage.color = new Color(1f, 1f, 1f, 1f - t);

            yield return null;
        }
        _enemyPortraitImage.gameObject.SetActive(false);
    }

    // 会話を終了、次のシーンへ
    private void EndDialog()
    {
        // 会話タイミングに応じて次のシーンを振り分け
        switch (CurrentTiming)
        {
            case DialogTiming.Opning:
            case DialogTiming.BeforeBattle:
                SceneManager.LoadScene("Scene_Battle");
                break;

            case DialogTiming.AfterBattle:
                if (GameDataManager.Instance != null) GameDataManager.Instance.CurrentStageIndex++;
                SceneManager.LoadScene("Scene_Map");
                break;

            case DialogTiming.Ending:
                SceneManager.LoadScene("Scene_Title");
                break;
        }
    }
}
