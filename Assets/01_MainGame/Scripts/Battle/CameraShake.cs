using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // ===== フィールド =====
    public static CameraShake Instance { get; private set; }

    private Vector3 _originalPos;
    private Coroutine _shakeCoroutine;

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _originalPos = transform.localPosition;
    }

    // ===== メソッド =====
    public void Shake(float intensity = 0.2f,float duration = 0.25f)
    {
        if(_shakeCoroutine != null)
        {
            // 動きを止め元の位置に戻す
            StopCoroutine( _shakeCoroutine );
            transform.localPosition = _originalPos;
        }

        _shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity,float duration)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            // 揺れを徐々に小さくする
            elapsed += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed /  duration);

            // カメラをランダムに小刻みに揺らす
            float offsetX = Random.Range(-1f,1f) * intensity * damper;
            float offsetY = Random.Range(-1f,1f) * intensity * damper;

            transform.localPosition = _originalPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }
        transform.localPosition = _originalPos;
        _shakeCoroutine = null;
    }
}
