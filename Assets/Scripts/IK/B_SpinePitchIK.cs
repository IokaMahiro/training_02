using UnityEngine;

/// <summary>
/// 複数の腰（Spine）ジョイントをカメラのピッチ（上下回転）に追従させるコンポーネント。
///
/// 動作順序：
///   1. Animator がアニメーションをボーンに書き込む（Update）
///   2. LateUpdate で各ジョイントのワールド回転にピッチ回転を上乗せする
///
/// Animator の有無による基準姿勢の切り替え：
///   あり → LateUpdate 先頭のアニメ後回転を基準（Animator が毎フレームリセットするため蓄積しない）
///   なし → Start で記録した初期姿勢を基準（蓄積を防ぐ）
/// </summary>
public class B_SpinePitchIK : MonoBehaviour
{
    #region 内部型定義
    /// <summary>ジョイント 1 本分の設定</summary>
    [System.Serializable]
    public struct JointEntry
    {
        [Tooltip("対象ジョイントの Transform")]
        public Transform joint;

        [Tooltip("全体ピッチのうちこのジョイントが受け持つ割合（0〜1）")]
        [Range(0f, 1f)]
        public float ratio;
    }
    #endregion

    #region 定義
    [Header("ジョイント（下から上の順に登録）")]
    [SerializeField] private JointEntry[] _joints;

    [Header("カメラ")]
    [Tooltip("ピッチを読み取るカメラ Transform（未指定時は Camera.main）")]
    [SerializeField] private Transform _cameraTransform;

    [Header("回転パラメータ")]
    [Tooltip("ピッチ追従の全体ウェイト（0=追従しない / 1=完全追従）")]
    [SerializeField] [Range(0f, 1f)] private float _weight = 0.5f;

    [Tooltip("各ジョイントに適用するピッチの上限角度（度）")]
    [SerializeField] [Range(0f, 90f)] private float _pitchLimit = 60f;

    [Tooltip("ピッチ変化をなめらかにする速度（0=即時反映）")]
    [SerializeField] [Range(0f, 20f)] private float _smoothSpeed = 8f;

    private float       _currentPitch;
    private Quaternion[] _restRotations; // Animator なし時の初期姿勢
    private bool         _hasAnimator;
    #endregion

    #region Unity イベント
    private void Awake()
    {
        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        if (_cameraTransform == null)
            Debug.LogWarning("[B_SpinePitchIK] カメラが見つかりません");

        if (_joints == null || _joints.Length == 0)
            Debug.LogWarning("[B_SpinePitchIK] _joints が未設定です");
    }

    private void Start()
    {
        // Animator の有無を確認
        _hasAnimator = GetComponent<Animator>() != null
                    || GetComponentInParent<Animator>() != null
                    || GetComponentInChildren<Animator>() != null;

        // Animator がない場合は初期姿勢をキャッシュして基準にする
        if (!_hasAnimator && _joints != null)
        {
            _restRotations = new Quaternion[_joints.Length];
            for (int i = 0; i < _joints.Length; i++)
            {
                if (_joints[i].joint != null)
                    _restRotations[i] = _joints[i].joint.rotation;
            }
        }
    }

    private void LateUpdate()
    {
        if (_joints == null || _cameraTransform == null) return;

        // --- 1. カメラのピッチを取得・クランプ・ウェイト適用 ---
        float targetPitch = Mathf.Clamp(GetCameraPitch(), -_pitchLimit, _pitchLimit) * _weight;

        // --- 2. スムージング ---
        _currentPitch = _smoothSpeed > 0f
            ? Mathf.LerpAngle(_currentPitch, targetPitch, Time.deltaTime * _smoothSpeed)
            : targetPitch;

        // --- 3. 各ジョイントにピッチを配分して適用 ---
        Vector3 rotAxis = transform.right; // キャラクターの向きに追従する回転軸

        for (int i = 0; i < _joints.Length; i++)
        {
            if (_joints[i].joint == null) continue;

            Quaternion pitchRot = Quaternion.AngleAxis(_currentPitch * _joints[i].ratio, rotAxis);

            // 基準姿勢：
            //   Animator あり → LateUpdate 先頭のアニメ後回転（Animator がリセット済み）
            //   Animator なし → Start で記録した初期姿勢（毎フレーム蓄積しない）
            Quaternion baseRot = _hasAnimator ? _joints[i].joint.rotation : _restRotations[i];

            _joints[i].joint.rotation = pitchRot * baseRot;
        }
    }
    #endregion

    #region 非公開メソッド
    /// <summary>カメラの X オイラー角を -180〜180 に正規化して返します。</summary>
    private float GetCameraPitch()
    {
        float pitch = _cameraTransform.eulerAngles.x;
        return pitch > 180f ? pitch - 360f : pitch;
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

            // 回転軸を矢印で可視化
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.ArrowHandleCap(
                0,
                _joints[i].joint.position,
                Quaternion.LookRotation(transform.right),
                0.25f,
                EventType.Repaint
            );

            // ジョイント名・ratio・現在適用ピッチをラベル表示
            UnityEditor.Handles.Label(
                _joints[i].joint.position + Vector3.up * 0.15f,
                $"{_joints[i].joint.name}  ratio:{_joints[i].ratio:F2}  pitch:{_currentPitch * _joints[i].ratio:F1}°"
            );
        }
    }
#endif
    #endregion
}
