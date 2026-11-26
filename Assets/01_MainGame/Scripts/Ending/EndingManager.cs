using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class EndingManager : MonoBehaviour
{
    // ===== フィールド =====
    [SerializeField] private Button _returnTitleButton;

    // ===== Unityメッセージ =====
    private void Start()
    {
        if (_returnTitleButton != null) _returnTitleButton.onClick.AddListener(OnReturnTitleClicked);

        // BGMやSEの再生
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
            //SoundManager.Instance.PlayBGM(BgmType.None);
            //SoundManager.Instance.PlaySE(SeType.StageClear);
        }
    }

    // ===== メソッド =====
    private void OnReturnTitleClicked()
    {
        _returnTitleButton.interactable = false;

        // タイトルシーンへ移動
        SceneManager.LoadScene("Scene_Title");
    }
}
