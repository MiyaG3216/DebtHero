using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.ShaderKeywordFilter;

public class MapProgressManager : MonoBehaviour
{
    // ===== フィールド =====
    [Header("プレイヤーアイコン")]
    [SerializeField] private RectTransform _playerPiece;

    [Header("マスの位置")]
    [SerializeField] private List<RectTransform> _tileNodes = new List<RectTransform>();

    [Header("UIテキスト＆ボタン")]
    [SerializeField] private TextMeshProUGUI _currentDestinationText;   // 目的地の情報
    [SerializeField] private TextMeshProUGUI _coinsText;
    [SerializeField] private Button _enterStageButton;                  // ステージ侵入ボタン

    [Header("アニメーション設定")]
    [SerializeField] private float _jumpDuration = 0.4f;    // ジャンプ時間
    [SerializeField] private float _jumpHight = 50f;        // ジャンプ高さ

    private int _targetStageIndex = 0;

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Start()
    {
        _enterStageButton.onClick.AddListener(OnEnterStageClicked);
        _enterStageButton.gameObject.SetActive(false);

        if (GameDataManager.Instance != null && _coinsText != null)
        {
            _coinsText.text = $"所持金：<color=yellow>{GameDataManager.Instance.Coins} G</color>";
        }

        // 目的地のステージ番号を取得
        _targetStageIndex = (GameDataManager.Instance != null) ? GameDataManager.Instance.CurrentStageIndex : 0;

        // コマのアニメーション開始
        StartCoroutine(MovePlayerPieceRoutine());
    }

    // ===== メソッド =====
    private IEnumerator MovePlayerPieceRoutine()
    {
        // 前回の位置
        int fromNodeIndex = Mathf.Clamp(_targetStageIndex, 0, _tileNodes.Count - 1);

        // 今回の目的地
        int toNodeIndex = Mathf.Clamp(_targetStageIndex + 1, 1, _tileNodes.Count - 1);

        // 初期位置に配置
        _playerPiece.position = _tileNodes[fromNodeIndex].position;
        yield return new WaitForSeconds(0.3f);

        // 前のマスから次のマスへジャンプ
        Vector3 startPos = _tileNodes[fromNodeIndex].position;
        Vector3 targetPos = _tileNodes[toNodeIndex].position;

        float elapsed = 0f;

        while (elapsed < _jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _jumpDuration;

            // 水平移動
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // 高さを変化させて放物線軌道に
            currentPos.y += Mathf.Sin(t * Mathf.PI) * _jumpHight;

            _playerPiece.position = currentPos;
            yield return null;
        }

        _playerPiece.position = targetPos;

        UpdateDestinationInfo();
    }

    private void UpdateDestinationInfo()
    {
        StageData stage = (GameDataManager.Instance != null) ? GameDataManager.Instance.GetCurrentStageData() : null;

        if (stage != null)
        {
            if (stage.Type == StageType.Battle)
            {
                string traitInfo = (stage.BossTrait != BossTraitType.None) ? $"[{stage.BossTrait}]" : "";
                _currentDestinationText.text = $"【{stage.StageName}】\n【返済ノルマ】{stage.TargetQuota} G\n【制限時間】{stage.MaxTurns} ターン";
                _enterStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "返済開始！";
            }
            else if (stage.Type == StageType.Shop)
            {
                _currentDestinationText.text = $"【{stage.StageName}】\nダイスやシールの購入ができる";
                _enterStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "入店する";
            }
        }

        _enterStageButton.gameObject.SetActive(true);
    }

    private void OnEnterStageClicked()
    {
        _enterStageButton.interactable = false;

        StageData stage = (GameDataManager.Instance != null) ? GameDataManager.Instance.GetCurrentStageData() : null;

        if (stage == null) return;

        // ステージタイプに応じてシーンをロード
        if (stage.Type == StageType.Battle)
        {
            SceneManager.LoadScene("Scene_Battle");
        }
        else if (stage.Type == StageType.Shop)
        {
            SceneManager.LoadScene("Scene_Shop");
        }
    }
}
