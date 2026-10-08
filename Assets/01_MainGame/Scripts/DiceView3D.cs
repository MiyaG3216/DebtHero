using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class DiceView3D : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private DiceManager _diceManager;

    [Header("物理誘導パラメータ")]
    [SerializeField] private float _maxTorqueStrength = 30f;   // 目的面への最大誘導力
    [SerializeField] private float _angularDamping = 1f;    // 回転の減速
    [SerializeField] private float _bounceForce = 10f;
    [SerializeField] private float _diceRepelForce = 15f;  // ダイス同士の反発力

    [Header("キープ演出用設定")]
    [SerializeField] private float _keepLiftHight = 0.4f;

    [Header("状態")]
    [SerializeField] private int _diceIndex = 0;    // ダイススロットの何番目か(0~4)

    public bool _isRolling = false;
    public bool _isKept = false;

    private int _targetValue = 1;
    private bool _isGuiding = false;
    private float _guideWeight = 0f;    // 誘導の強さ(0.0 ~ 1.0)
    private int _bounceCount = 0;

    private Vector3 _landedPosition;    // 着地した位置

    // 各出目がダイスのローカル座標でどの方向を向いているか
    private static readonly Vector3[] LocalFaceDirections = new Vector3[6]
    {
        Vector3.forward,
        Vector3.up,
        Vector3.left,
        Vector3.right,
        Vector3.down,
        Vector3.back,
    };

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        _rb.maxAngularVelocity = 40f;   // 高速回転を許可
    }

    private void OnMouseDown()
    {
        // 転がっている最中はクリック無効
        if (_isRolling) return;

        // DiceManaagerにキープ切り替えを通知
        if (_diceManager != null)
        {
            _diceManager.ToggleKeepDice(_diceIndex);
        }
    }

    private void FixedUpdate()
    {
        if (!_isGuiding) return;

        // 毎フレーム現在のダイスの向きから目標角度をリアルタイム計算
        Quaternion targetRotation = CalculateTargetRotationPreservingYaw(_targetValue);

        // 現在の角度から目標角度への回転差分を計算
        Quaternion deltaRot = targetRotation * Quaternion.Inverse(transform.rotation);
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;

        if (Mathf.Abs(angle) > 0.5f)
        {
            // _guideWeightを掛けて、誘導力を徐々に強める
            float currentStrength = _maxTorqueStrength * _guideWeight;

            // 目標へ向けるばねの力 + 行き過ぎを防ぐブレーキ力
            Vector3 torque = axis.normalized * (angle * Mathf.Deg2Rad * currentStrength) - (_rb.angularVelocity * _angularDamping);
            _rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isRolling || _bounceCount >= 2) return;

        // ダイス同士が接触した時の処理
        DiceView3D otherDIce = collision.gameObject.GetComponent<DiceView3D>();
        if (otherDIce != null)
        {
            Vector3 pushDir = transform.position - collision.transform.position;
            pushDir.y = 0;

            // 重なって完全に同じ位置にいる場合のフォールバック
            if (pushDir.sqrMagnitude < 0.001f)
            {
                pushDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            }

            pushDir.Normalize();

            // 相手から離れる方向へ押し出す力を加える
            _rb.AddForce(pushDir * _diceRepelForce, ForceMode.Impulse);
        }

        // 床に接触したときの処理
        if (_bounceCount < 2)
        {
            foreach (var contact in collision.contacts)
            {
                // 床との衝突時のみ判定
                if (contact.normal.y > 0.7f)
                {
                    _bounceCount++;

                    // 最初の2回は跳ねさせる
                    Vector3 bounce = Vector3.up * (_bounceForce / _bounceCount);
                    _rb.AddForce(bounce, ForceMode.Impulse);
                    break;
                }
            }
        }

        // 衝突した面の法線をチェック
    }

    // ===== メソッド =====
    public void Roll(int targetValue, Vector3 spawnPosition)
    {
        StopAllCoroutines();
        _targetValue = targetValue;
        StartCoroutine(RollRoutine(spawnPosition));
    }

    private IEnumerator RollRoutine(Vector3 spawnPosition)
    {
        _isRolling = true;
        _isGuiding = false;
        _guideWeight = 0f;
        _bounceCount = 0;
        _isKept = false;
        _rb.isKinematic = false;

        // ダイスを上空に配置して物理をセット
        transform.position = spawnPosition;
        transform.rotation = Random.rotation;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // ランダムな力と回転を加えて投げる
        Vector3 throwForce = new Vector3(Random.Range(-1f, 1f), Random.Range(-5f, -3f), Random.Range(-1f, 1f));
        Vector3 throwTorque = new Vector3(Random.Range(-40f, 40f), Random.Range(-40f, 40f), Random.Range(-40f, 40f));

        _rb.AddForce(throwForce, ForceMode.Impulse);
        _rb.AddTorque(throwTorque, ForceMode.Impulse);

        // 自由落下時間
        yield return new WaitForSeconds(0.1f);
        _isGuiding = true;

        // 転がりながら徐々に誘導力を高める
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            _guideWeight = Mathf.Clamp01(elapsed / 0.5f);
            yield return null;
        }
        _guideWeight = 1f;

        // 転がり待ち
        float timer = 0f;
        while (timer < 2.0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 静止完了
        _isGuiding = false;
        transform.rotation = CalculateTargetRotationPreservingYaw(_targetValue);
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _landedPosition = transform.position;   // 着地位置を記録
        _isRolling = false;
    }

    // 現在のダイスの向き(水平方向)を維持したまま、目的の面を上に向ける回転を算出
    private Quaternion CalculateTargetRotationPreservingYaw(int value)
    {
        int index = Mathf.Clamp(value - 1, 0, 5);
        Vector3 localFace = LocalFaceDirections[index];

        // ダイスの現在向いている目的の面のワールド方向
        Vector3 currentWorldFace = transform.TransformDirection(localFace);

        // その面を真上に向ける為の最小回転を計算
        Quaternion alignRotation = Quaternion.FromToRotation(currentWorldFace, Vector3.up);

        // 現在の回転に合成
        return alignRotation * transform.rotation;
    }

    public void SetKeepState(bool isKept)
    {
        if (isKept)
        {
            // キープ時固定してぶつかっても動かないようにする
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;

            StopCoroutine("AnimationLiftRoutine");
            StartCoroutine(AnimationLiftRoutine(_landedPosition + Vector3.up * _keepLiftHight));
        }
        else
        {
            StopCoroutine("AnimationLiftRoutine");
            StartCoroutine(AnimationLiftRoutine(_landedPosition));
        }
    }

    private IEnumerator AnimationLiftRoutine(Vector3 targetPos)
    {
        float t = 0f;

        Vector3 startPos = transform.position;

        while (t < 0.15f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, t / 0.15f);
            yield return null;
        }

        transform.position = targetPos;
    }
}
