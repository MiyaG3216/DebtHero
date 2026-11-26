using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // ===== フィールド =====
    [SerializeField] private Button _startButton;

    // ===== Unityメッセージ =====
    private void Start()
    {
        if (_startButton != null) _startButton.onClick.AddListener(OnStartClicked);

        // BGMやSEの再生
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
            //SoundManager.Instance.PlayBGM(BgmType.None);
        }
    }

    // ===== メソッド =====
    private void OnStartClicked()
    {
        _startButton.interactable = false;

        // ゲームデータを初期化
        if (GameDataManager.Instance != null) GameDataManager.Instance.InitializeNewGameData();

        // オープニングへ移動
        DialogController.CurrentTiming = DialogTiming.Opning;
        SceneManager.LoadScene("Scene_Dialog");
    }
}
