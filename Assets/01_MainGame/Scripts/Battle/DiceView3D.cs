using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

// Colorのα値の変更を行う為の拡張クラス
public static class ColorExtensions
{
    public static Color WhithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
}

[RequireComponent(typeof(Rigidbody))]
public class DiceView3D : MonoBehaviour
{
    // ===== フィールド =====
    [Header("参照")]
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private DiceManager _diceManager;
    [SerializeField] private Renderer _diceRenderer;
    [SerializeField] private BoxCollider _boxCollider;

    [Header("物理誘導パラメータ")]
    [SerializeField] private float _maxTorqueStrength = 30f;   // 目的面への最大誘導力
    [SerializeField] private float _angularDamping = 1f;    // 回転の減速
    [SerializeField] private float _bounceForce = 10f;
    [SerializeField] private float _diceRepelForce = 15f;  // ダイス同士の反発力

    [Header("シール表示用パラメータ")]
    [SerializeField] private Material _sealBaseMaterial;
    [SerializeField] private float _faceDistance = 0.52f;
    [SerializeField] private float _sealSize = 0.85f;
    [SerializeField] private float _sealThickness = 0.02f;

    [Header("ジャラジャラ用パラメータ")]
    [SerializeField] private float _shakeAmount = 0.3f;     // 位置揺れの大きさ
    [SerializeField] private float _rotateSpeed = 400f;     // 回転スピード

    [Header("状態")]
    [SerializeField] private int _diceIndex = 0;    // ダイススロットの何番目か(0~4)

    [Header("キープエリア設定")]
    [SerializeField] private float _keepAreaZ = -8f;
    [SerializeField] private float _keepSlotSpacing = 2f;
    [SerializeField] private float _fixedKeepHight = 1.5f;  // キープ時の高さ
    [SerializeField] private float _fixedFloorHight = 0.7f; // キープ解除時の高さ

    public bool IsRolling = false;
    public bool IsKept = false;

    private int _targetValue = 1;
    private bool _isGuiding = false;
    private float _guideWeight = 0f;    // 誘導の強さ(0.0 ~ 1.0)
    private int _bounceCount = 0;

    private Vector3 _landedPosition;    // 着地した位置

    private MeshRenderer[] _sealFaceRenderers = new MeshRenderer[6];    // 各面のシール表示用Quad

    private GameObject _keepMarkObj;    // 選択中ダイスのリングオブジェクト
    private Coroutine _hoverCoroutine;  // 浮遊アニメーション
    private Coroutine _jiggleCroutine;  // 空中でジャラジャラする動き

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

    private static readonly Quaternion[] FaceRotations = new Quaternion[6]
    {
        Quaternion.Euler(-90, 0, 0),
        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 0, -90),
        Quaternion.Euler(0, 0, 90),
        Quaternion.Euler(0, 0, 180),
        Quaternion.Euler(90, 0, 0),
    };

    // ===== プロパティ =====

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        _rb.maxAngularVelocity = 40f;   // 高速回転を許可

        // Rendererコンポーネントを取得
        if (_diceRenderer == null) _diceRenderer = GetComponentInChildren<Renderer>();

        // BoxColliderコンポーネントを取得
        if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider>();

        // シール表示用パネル(Quad)を作成
        CreateSealFacePanels();

        // 選択中マーカーを生成
        CreateKeepMarker();
    }

    private void OnMouseDown()
    {
        // 転がっている最中はクリック無効
        if (IsRolling) return;

        // DiceManaagerにキープ切り替えを通知
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.Handle3DDiceClicked(_diceIndex);
        }
        else if (_diceManager != null)
        {
            _diceManager.ToggleKeepDice(_diceIndex);
        }

        // ダイス詳細パネルを非表示にする
        if (DiceTooltipUI.Instance != null) DiceTooltipUI.Instance.Hide();
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
        if (!IsRolling) return;

        // ダイス同士が接触した時の処理
        DiceView3D otherDice = collision.gameObject.GetComponent<DiceView3D>();
        if (otherDice != null)
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
            return;
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
    }

    private void OnDestroy()
    {
        // 自身が破棄されたらマーカーも削除
        if (_keepMarkObj != null)
        {
            Destroy(_keepMarkObj);
        }
    }

    private void OnMouseEnter()
    {
        // 転がっている最中は非表示
        if (IsRolling) return;

        // 自身のダイスデータを取得してダイス詳細パネルを表示
        if (DiceTooltipUI.Instance != null && _diceManager != null)
        {
            if (_diceIndex >= 0 && _diceIndex < _diceManager.DiceList.Count)
            {
                var diceData = _diceManager.DiceList[_diceIndex];
                DiceTooltipUI.Instance.Show(diceData, _diceIndex);
            }
        }
    }

    private void OnMouseExit()
    {
        if (DiceTooltipUI.Instance != null)
        {
            DiceTooltipUI.Instance.Hide();
        }
    }

    // ===== メソッド =====
    public void Roll(int targetValue, Vector3 spawnPosition)
    {
        StopAllCoroutines();
        _targetValue = targetValue;
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        IsRolling = true;
        _guideWeight = 0f;
        _bounceCount = 0;
        _rb.isKinematic = false;

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
        IsRolling = false;
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

    public void SetKeepState(bool Kept)
    {
        IsKept = Kept;

        if (_hoverCoroutine != null)
        {
            StopCoroutine(_hoverCoroutine);
            _hoverCoroutine = null;
        }

        if (IsKept)
        {
            // キープ時固定してぶつかっても動かないようにする
            // isKineticが無効の時だけ速度を0にする。
            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;

            // 各ダイスごとのキープ定位置を計算
            Vector3 keepTargetPos = new Vector3((_diceIndex - 2) * _keepSlotSpacing, _fixedKeepHight, _keepAreaZ);

            // カメラ正面を向く回転を計算
            Quaternion frontTargetRot = FaceRotations[Mathf.Clamp(_targetValue - 1, 0, 5)];

            // ダイスの下に選択中マーカーを表示
            if (_keepMarkObj != null)
            {
                // 位置：床から少し浮かす
                _keepMarkObj.transform.position = new Vector3(keepTargetPos.x, 0.02f, keepTargetPos.z);

                // 向き：床と水平かつダイスと同じ向き
                _keepMarkObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                _keepMarkObj.SetActive(true);
            }

            // 浮遊アニメーション
            _hoverCoroutine = StartCoroutine(KeepMoveAlignAndHoverRoutine(keepTargetPos, frontTargetRot));
        }
        else
        {
            // 選択中マーカーを非表示
            if (_keepMarkObj != null) _keepMarkObj.SetActive(false);

            // ダイスを床に着地させる
            StartCoroutine(DropToFloorInPlaceRoutine());
        }
    }

    // 浮遊アニメーション
    private IEnumerator KeepMoveAlignAndHoverRoutine(Vector3 targetPos, Quaternion targetRot)
    {
        // 一時的に当たり判定を消す
        if(_boxCollider != null) _boxCollider.enabled = false;

        // 浮き上がりキープ定位置へ移動
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        // 当たり判定を戻す
        if(_boxCollider != null) _boxCollider.enabled = true;

        // その後ゆっくり上下に揺れる
        while (IsKept)
        {
            float hoverOffset = Mathf.Sin(Time.time * 4f) * 0.1f;
            transform.position = targetPos + Vector3.up * hoverOffset;
            yield return null;
        }
    }

    // ダイスを床に着地させるアニメーション
    private IEnumerator DropToFloorInPlaceRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 floorPos = new Vector3(startPos.x, _fixedFloorHight, startPos.z);

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, floorPos, t);
            yield return null;
        }

        transform.position = floorPos;
        _landedPosition = floorPos;
        _rb.isKinematic = false;
    }

    public void AnimateCheatFaceChange(int newValue)
    {
        StartCoroutine(CheatAnimateRoutine(newValue));
    }

    private IEnumerator CheatAnimateRoutine(int newValue)
    {
        IsRolling = true;
        _rb.isKinematic = true;

        Vector3 startPos = transform.position;
        Vector3 liftPos = startPos + Vector3.up * 2.0f;

        // 浮かび上がる演出
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, liftPos, elapsed / 0.15f);
            yield return null;
        }

        // 空中で回転
        _targetValue = newValue;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = CalculateTargetRotationPreservingYaw(_targetValue);

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, Mathf.SmoothStep(0f, 1f, elapsed / 0.3f));
            yield return null;
        }
        transform.rotation = targetRot;

        // 床に着地する
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(liftPos, startPos, elapsed / 0.15f);
            yield return null;
        }
        transform.position = startPos;

        _landedPosition = startPos;
        IsRolling = false;
    }

    private void CreateSealFacePanels()
    {
        // BoxColliderがついていれば、そのサイズから自動で距離を算出
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            // コライダの半径の少し外側を基準にする
            float autoDist = (boxCol.size.z * 0.5f) + 0.02f;
            if (autoDist > 0.1f)
            {
                _faceDistance = autoDist;
                _sealSize = boxCol.size.x * 0.75f;
            }
        }

        // 各面のローカル位置と回転(表面の0.505の厚みに貼る)
        Vector3[] facePotisions = new Vector3[6]
        {
            new Vector3(0,0,_faceDistance),
            new Vector3(0,_faceDistance,0),
            new Vector3(-_faceDistance,0,0),
            new Vector3(_faceDistance,0,0),
            new Vector3(0,-_faceDistance,0),
            new Vector3(0,0,-_faceDistance)
        };

        Vector3[] faceScales = new Vector3[6]
        {
            new Vector3(_sealSize,_sealSize,_sealThickness),
            new Vector3(_sealSize,_sealThickness,_sealSize),
            new Vector3(_sealThickness,_sealSize,_sealSize),
            new Vector3(_sealThickness,_sealSize,_sealSize),
            new Vector3(_sealSize,_sealThickness,_sealSize),
            new Vector3(_sealSize,_sealSize,_sealThickness)
        };

        for (int i = 0; i < 6; i++)
        {
            GameObject sealObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sealObj.name = $"SealPanel_Face_{i + 1}";
            sealObj.transform.SetParent(transform);
            sealObj.transform.localPosition = facePotisions[i];
            sealObj.transform.localRotation = Quaternion.identity;
            sealObj.transform.localScale = faceScales[i];

            DestroyImmediate(sealObj.GetComponent<Collider>());

            var meshRenderer = sealObj.GetComponent<MeshRenderer>();
            if (_sealBaseMaterial != null)
            {
                meshRenderer.material = new Material(_sealBaseMaterial);
            }

            _sealFaceRenderers[i] = sealObj.GetComponent<MeshRenderer>();
            sealObj.SetActive(false);
        }
    }

    public void UpdateVisuals(Dice diceData)
    {
        if (diceData == null) return;

        // ダイス本体の色を変更
        if (_diceRenderer != null)
        {
            Color targetColor = GetDiceColor(diceData.Type);
            _diceRenderer.material.SetColor("_BaseColor", targetColor);
        }

        // 各面のシールの色と表示を更新
        for (int i = 0; i < 6; i++)
        {
            int faceVal = i + 1;
            SealType seal = diceData.GetSealOnFace(faceVal);

            if (_sealFaceRenderers[i] != null)
            {
                if (seal == SealType.None)
                {
                    _sealFaceRenderers[i].gameObject.SetActive(false);
                }
                else
                {
                    _sealFaceRenderers[i].gameObject.SetActive(true);

                    Color sealColor = GetSealColor(seal);
                    _sealFaceRenderers[i].material.color = sealColor;
                    if (_sealFaceRenderers[i].material.HasProperty("_BaseColor"))
                    {
                        _sealFaceRenderers[i].material.SetColor("_BaseColor", sealColor);
                    }
                }
            }
        }
    }

    private Color GetDiceColor(DiceType type)
    {
        switch (type)
        {
            case DiceType.Odd: return Color.blue;           // 青
            case DiceType.Even: return Color.red;           // 赤
            case DiceType.Straight: return Color.green;     // 緑
            case DiceType.HighRoller: return Color.orange;  // オレンジ
            case DiceType.LowRoller: return Color.violet;     // 紫
            default: return Color.white;
        }
    }

    private Color GetSealColor(SealType seal)
    {
        switch (seal)
        {
            case SealType.Fire: return WithAlpha(Color.red, 0.6f);
            case SealType.Ice: return WithAlpha(Color.cyan, 0.6f);
            case SealType.Gold: return WithAlpha(Color.yellow, 0.6f);
            case SealType.Iron: return WithAlpha(Color.gray, 0.6f);
            case SealType.Lightning: return WithAlpha(Color.white, 0.6f);
            case SealType.Solo: return WithAlpha(Color.green, 0.6f);
            case SealType.Odd: return WithAlpha(Color.blue, 0.6f);
            case SealType.Even: return WithAlpha(Color.magenta, 0.6f);
            case SealType.Gamble: return WithAlpha(Color.black, 0.6f);
            default: return Color.clear;
        }
    }

    private void CreateKeepMarker()
    {
        _keepMarkObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _keepMarkObj.name = "Keep_Selection_Marker";

        // ダイスと連動しないように独立させる
        _keepMarkObj.transform.SetParent(transform.parent != null ? transform.parent.transform : null);

        // 水平方向に寝かせる
        _keepMarkObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // ダイスより一回り大きくする
        _keepMarkObj.transform.localScale = Vector3.one * 2f;

        DestroyImmediate(_keepMarkObj.GetComponent<Collider>());

        var mr = _keepMarkObj.GetComponent<MeshRenderer>();
        if (_sealBaseMaterial != null)
        {
            mr.material = new Material(_sealBaseMaterial);

            // シアン色に変更
            mr.material.SetColor("_BaseColor", WithAlpha(Color.cyan, 0.7f));
        }

        // 最初は非表示
        _keepMarkObj.SetActive(false);
    }

    private Color WithAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }

    // 空中でジャラジャラする
    public void PrepareRoll(Vector3 spawnPosition)
    {
        // ダイス詳細パネルを非表示にする
        if (DiceTooltipUI.Instance != null) DiceTooltipUI.Instance.Hide();

        StopAllCoroutines();
        IsRolling = true;
        _isGuiding = false;
        IsKept = false;
        _rb.isKinematic = true;

        if (_keepMarkObj != null)
        {
            _keepMarkObj.SetActive(false);
        }

        transform.position = spawnPosition;
        transform.rotation = Random.rotation;

        // 空中でジャラジャラ開始
        _jiggleCroutine = StartCoroutine(JiggleRoutine(spawnPosition));
    }

    // ジャラジャラアニメーション
    private IEnumerator JiggleRoutine(Vector3 basePos)
    {
        float randomSeed = _diceIndex * 1.5f;

        while (true)
        {
            // 位置の小刻みな揺れ
            Vector3 shakeOffset = new Vector3(
                Mathf.Sin(Time.time * 18f + randomSeed) * _shakeAmount,
                Mathf.Sin(Time.time * 22f + randomSeed) * (_shakeAmount * 1.2f),
                Mathf.Sin(Time.time * 15f + randomSeed * 2f) * _shakeAmount);

            transform.position = basePos + shakeOffset;

            transform.Rotate(new Vector3(
                Mathf.Sin(Time.time * 10f) * _rotateSpeed,
                _rotateSpeed,
                Mathf.Cos(Time.time * 12f) * _rotateSpeed
                ) * Time.deltaTime);

            yield return null;
        }
    }

    // 投げるボタンが押されたらダイスロール
    public void ThrowDice(int targetValue)
    {
        if (_jiggleCroutine != null)
        {
            StopCoroutine(_jiggleCroutine);
            _jiggleCroutine = null;
        }

        _targetValue = targetValue;
        StartCoroutine(RollRoutine());
    }
}

