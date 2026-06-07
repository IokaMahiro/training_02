using UnityEngine;

/// <summary>
/// 複数の脊椎ジョイントをカメラの向きに追従させるルックIKコンポーネント。
///
/// ── 逆算アプローチ ────────────────────────────────────────────────────────────
///   「カメラ前方を向かせたい姿勢」から必要な回転を逆算して求める。
///   カメラ前方ベクトルをキャラクターのローカル座標系に変換し、
///   ヨー（Atan2 x/z）とピッチ（Atan2 y/水平長）に分解することで、
///   キャラクターが傾いた地面にいる場合にも正確に対応する。
///
/// ── 機能 ──────────────────────────────────────────────────────────────────────
///   ピッチ（上下）: カメラ前方の仰俯角成分を _pitchWeight × ratio でジョイントに分配
///   ヨー（左右）  : カメラ前方の水平偏差成分を _yawWeight × ratio でジョイントに分配
///
/// ── 動作タイミング ─────────────────────────────────────────────────────────────
///   LateUpdate → Animator がアニメーションをボーンに書き込んだあとで上乗せ
///
/// ── 基準姿勢 ──────────────────────────────────────────────────────────────────
///   Animator あり → 毎フレーム Animator が書き込んだワールド回転を基準にする
///                   （Animator が次フレームにリセットするため蓄積しない）
///   Animator なし → Start() で記録したワールド回転を基準にする
///
/// ── ratio の設計指針 ──────────────────────────────────────────────────────────
///   各ジョイントの回転は階層的に累積される（親の回転は子に引き継がれる）。
///   そのため ratio の合計が 1 に近いほど、チェーン末端がカメラ方向に揃う。
///   例: 3 ジョイント構成で ratio = [0.35, 0.35, 0.30] → 合計 1.0
/// </summary>
public class B_SpineLookIK : MonoBehaviour
{
    #region 内部型定義
    [System.Serializable]
    public struct JointEntry
    {
        [Tooltip("対象ジョイントの Transform")]
        public Transform joint;

        [Tooltip("ピッチ（上下）全体のうちこのジョイントが受け持つ割合（0〜1）")]
        [Range(0f, 1f)]
        public float pitchRatio;

        [Tooltip("ヨー（左右）全体のうちこのジョイントが受け持つ割合（0〜1）")]
        [Range(0f, 1f)]
        public float yawRatio;
    }
    #endregion

    #region フィールド
    [Header("ジョイント（腰→首の順に登録）")]
    [SerializeField] private JointEntry[] _joints;

    [Header("カメラ")]
    [Tooltip("方向を読み取るカメラ Transform（未指定時は Camera.main）")]
    [SerializeField] private Transform _cameraTransform;

    [Header("ピッチ（上下追従）")]
    [Tooltip("ピッチ追従の全体ウェイト（0=無効 / 1=完全追従）")]
    [SerializeField] [Range(0f, 1f)]  private float _pitchWeight      = 0.8f;
    [Tooltip("各ジョイントに適用するピッチの上限角度（度）")]
    [SerializeField] [Range(0f, 90f)] private float _pitchLimit       = 60f;
    [Tooltip("ピッチ変化をなめらかにする速度（0=即時反映）")]
    [SerializeField] [Range(0f, 20f)] private float _pitchSmoothSpeed = 8f;

    [Header("ヨー（左右追従）")]
    [Tooltip("ヨー追従の全体ウェイト（0=無効 / 1=完全追従）")]
    [SerializeField] [Range(0f, 1f)]  private float _yawWeight        = 0.5f;
    [Tooltip("各ジョイントに適用するヨーの上限角度（度）")]
    [SerializeField] [Range(0f, 90f)] private float _yawLimit         = 45f;
    [Tooltip("ヨー変化をなめらかにする速度（0=即時反映）")]
    [SerializeField] [Range(0f, 20f)] private float _yawSmoothSpeed   = 6f;

    // ランタイム状態
    private float        _currentPitch;   // スムージング済みピッチ角（度）
    private float        _currentYaw;     // スムージング済みヨー角（度）
    private Quaternion[] _restRotations;  // Animator なし時の初期姿勢キャッシュ
    private bool         _hasAnimator;
    #endregion

    #region Unity イベント
    private void Awake()
    {
        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        if (_cameraTransform == null)
            Debug.LogWarning("[B_SpineLookIK] カメラが見つかりません");
        if (_joints == null || _joints.Length == 0)
            Debug.LogWarning("[B_SpineLookIK] _joints が未設定です");
    }

    private void Start()
    {
        // Animator の有無を判定（コンポーネント自身・親・子を探索）
        _hasAnimator = GetComponent<Animator>() != null
                    || GetComponentInParent<Animator>() != null
                    || GetComponentInChildren<Animator>() != null;

        // Animator がない場合のみ初期姿勢をキャッシュ
        if (!_hasAnimator && _joints != null)
        {
            _restRotations = new Quaternion[_joints.Length];
            for (int i = 0; i < _joints.Length; i++)
                if (_joints[i].joint != null)
                    _restRotations[i] = _joints[i].joint.rotation;
        }
    }

    private void LateUpdate()
    {
        if (_joints == null || _cameraTransform == null) return;

        // ── ① カメラ前方を逆算してターゲット角度を取得・スムージング ────────────

        GetTargetAngles(out float rawPitch, out float rawYaw);

        float targetPitch = Mathf.Clamp(rawPitch, -_pitchLimit, _pitchLimit) * _pitchWeight;
        if (_pitchSmoothSpeed > 0f)
        {
            _currentPitch = Mathf.LerpAngle(_currentPitch, targetPitch, Time.deltaTime * _pitchSmoothSpeed);
        }
        else
        {
            _currentPitch = targetPitch;
        }

        float targetYaw = Mathf.Clamp(rawYaw, -_yawLimit, _yawLimit) * _yawWeight;
        if (_yawSmoothSpeed > 0f)
        {
            _currentYaw = Mathf.LerpAngle(_currentYaw, targetYaw, Time.deltaTime * _yawSmoothSpeed);
        }
        else
        {
            _currentYaw = targetYaw;
        }

        // ── ② 各ジョイントへ回転を適用 ──────────────────────────────────────────

        for (int i = 0; i < _joints.Length; i++)
        {
            if (_joints[i].joint == null) continue;

            float jointPitch = _currentPitch * _joints[i].pitchRatio;
            float jointYaw   = _currentYaw   * _joints[i].yawRatio;

            // ヨー回転: キャラクター上軸まわりに水平旋回
            Quaternion yawRot = Quaternion.AngleAxis(jointYaw, transform.up);

            // ピッチ回転: ヨー適用後の右軸を使うことで斜め方向を正確に表現
            //   例: 右45°向きながら上30°仰角 → 軸が斜め右になるため精度が上がる
            Vector3    rightAfterYaw = yawRot * transform.right;
            Quaternion pitchRot      = Quaternion.AngleAxis(jointPitch, rightAfterYaw);

            // 基準姿勢: Animator あり → 現フレームのアニメ出力（Animator が毎フレームリセット済み）
            //           Animator なし → Start() で記録した初期姿勢
            Quaternion baseRot;
            if (_hasAnimator)
            {
                baseRot = _joints[i].joint.rotation;
            }
            else
            {
                baseRot = _restRotations[i];
            }

            // ヨー → ピッチ の順で基準姿勢に上乗せ
            _joints[i].joint.rotation = pitchRot * yawRot * baseRot;
        }
    }
    #endregion

    #region 非公開メソッド
    /// <summary>
    /// カメラ前方ベクトルをキャラクターのローカル座標系に変換し、
    /// ヨーとピッチを逆算して返します。
    ///
    /// 逆算の流れ:
    ///   1. カメラ前方（ワールド）をキャラクターローカルに変換
    ///   2. ヨー = atan2(local.x, local.z)  ← XZ平面での水平偏差
    ///   3. ピッチ = -atan2(local.y, 水平長) ← 上方向が負になるよう反転
    ///
    /// キャラクターが傾斜地にいても、ローカル変換によって正確に算出されます。
    /// </summary>
    /// <param name="pitch">上方向が負・下方向が正（度）</param>
    /// <param name="yaw">右方向が正・左方向が負（度）</param>
    private void GetTargetAngles(out float pitch, out float yaw)
    {
        // カメラ前方をキャラクターのローカル座標系へ変換
        Vector3 camFwdLocal = Quaternion.Inverse(transform.rotation) * _cameraTransform.forward;

        // ヨー: ローカル XZ 平面での +Z（キャラクター正面）からの偏差
        yaw = Mathf.Atan2(camFwdLocal.x, camFwdLocal.z) * Mathf.Rad2Deg;

        // ピッチ: 水平面からの仰俯角（上=負・下=正 に合わせて反転）
        float horizontalLen = Mathf.Sqrt(camFwdLocal.x * camFwdLocal.x + camFwdLocal.z * camFwdLocal.z);
        pitch = -Mathf.Atan2(camFwdLocal.y, horizontalLen) * Mathf.Rad2Deg;
    }
    #endregion

    #region エディタ補助
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_joints == null) return;

        for (int i = 0; i < _joints.Length; i++)
        {
            if (_joints[i].joint == null) continue;

            Vector3 pos = _joints[i].joint.position;

            // ピッチ軸（シアン）
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.ArrowHandleCap(0, pos,
                Quaternion.LookRotation(transform.right), 0.20f, EventType.Repaint);

            // ヨー軸（黄）
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.ArrowHandleCap(0, pos,
                Quaternion.LookRotation(transform.up), 0.20f, EventType.Repaint);

            // ラベル: ジョイント名 / 現在の各軸回転量
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                pos + Vector3.up * 0.18f,
                $"{_joints[i].joint.name}\n" +
                $"  pitch {_currentPitch * _joints[i].pitchRatio:+0.0;-0.0;0.0}° (r={_joints[i].pitchRatio:F2})\n" +
                $"  yaw   {_currentYaw   * _joints[i].yawRatio  :+0.0;-0.0;0.0}° (r={_joints[i].yawRatio  :F2})");
        }
    }
#endif
    #endregion
}
