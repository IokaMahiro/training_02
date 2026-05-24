using UnityEngine;

/// <summary>
/// 仮リグの腕を毎フレーム指定ターゲットへ向ける、デバッグ用姿勢コントローラー。
/// Animator の代わりとして機能し、<see cref="B_ArmObstacleIK.SetAnimatedPose"/> でベースポーズを更新します。
///
/// 実行順：
///   Update（本スクリプト） → LateUpdate（B_ArmObstacleIK）
///   ↑ Update でベースポーズを決め、LateUpdate で障害物 IK を上乗せする流れ
/// </summary>
public class B_DebugArmPose : MonoBehaviour
{
    #region 定義
    [Header("仮リグ")]
    [Tooltip("肩の基点（IK の回転軸）")]
    [SerializeField] private Transform _armRoot;

    [Tooltip("上腕ボーン")]
    [SerializeField] private Transform _upperArm;

    [Tooltip("前腕ボーン")]
    [SerializeField] private Transform _foreArm;

    [Tooltip("手首ボーン")]
    [SerializeField] private Transform _hand;

    [Header("リーチターゲット")]
    [Tooltip("手先が目指すワールド座標")]
    [SerializeField] private Transform _reachTarget;

    [Header("B_ArmObstacleIK 連携")]
    [Tooltip("ベースポーズを渡す B_ArmObstacleIK（なければ姿勢のみ適用）")]
    [SerializeField] private B_ArmObstacleIK _obstacleIK;

    [Tooltip("_obstacleIK の _arms 配列における対象インデックス")]
    [SerializeField] private int _armIndex;

    [Header("肘の曲げ方向（ポールベクター）")]
    [Tooltip("肘が曲がる方向のヒント（ワールド空間）")]
    [SerializeField] private Vector3 _poleDir = Vector3.down;

    // 初期姿勢キャッシュ（ArmRoot ローカル空間）
    private Quaternion _initUpperRot;
    private Quaternion _initForeRot;   // 前腕の初期ワールド回転（非蓄積のための基準）
    private Quaternion _initHandRot;
    private Vector3    _elbowRestLocal; // 初期肘位置（ArmRoot ローカル）
    private Vector3    _initForeLocalDir; // 初期前腕の向き（上腕ローカル空間）
    private float      _lab;           // 上腕長
    private float      _lbc;           // 前腕長
    #endregion

    #region Unity イベント
    private void Start()
    {
        if (_armRoot == null || _upperArm == null || _foreArm == null || _hand == null)
        {
            Debug.LogWarning("[B_DebugArmPose] ボーン参照が未設定です");
            return;
        }

        // 初期ポーズを記録（IK 適用前の rest ポーズ）
        _initUpperRot = _upperArm.rotation;
        _initForeRot  = _foreArm.rotation;
        _initHandRot  = _hand.rotation;

        // 前腕の初期方向を上腕ローカル空間で記録（フレーム間蓄積防止用）
        _initForeLocalDir = _upperArm.InverseTransformDirection(
            (_hand.position - _foreArm.position).normalized);

        // ArmRoot ローカルで初期の肘位置を保存（ArmRoot が移動しても追従）
        _elbowRestLocal = _armRoot.InverseTransformPoint(_foreArm.position);

        _lab = Vector3.Distance(_upperArm.position, _foreArm.position);
        _lbc = Vector3.Distance(_foreArm.position,  _hand.position);
    }

    private void Update()
    {
        if (_armRoot == null || _upperArm == null || _foreArm == null || _hand == null) return;
        if (_reachTarget == null) return;

        Vector3 shoulder  = _armRoot.position;
        Vector3 target    = _reachTarget.position;

        // --- 1. 目標を到達可能範囲にクランプ ---
        Vector3 toTarget = target - shoulder;
        float   lAT      = Mathf.Clamp(
            toTarget.magnitude,
            Mathf.Abs(_lab - _lbc) + 0.001f,
            _lab + _lbc            - 0.001f);

        Vector3 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : _armRoot.forward;
        target = shoulder + dir * lAT;

        // --- 2. 肘の曲げ方向を決める（ポール方向の dir への垂直成分）---
        Vector3 pole = _poleDir.normalized;
        Vector3 bendDir = pole - Vector3.Dot(pole, dir) * dir;
        if (bendDir.sqrMagnitude < 0.0001f)
            bendDir = Vector3.Cross(dir, _armRoot.right);
        bendDir.Normalize();

        // --- 3. 余弦定理で肩の角度を算出 ---
        float cosA   = (_lab * _lab + lAT * lAT - _lbc * _lbc) / (2f * _lab * lAT);
        float angleA = Mathf.Acos(Mathf.Clamp(cosA, -1f, 1f));

        // --- 4. 新しい肘位置 ---
        Vector3 newElbow = shoulder + (Mathf.Cos(angleA) * dir + Mathf.Sin(angleA) * bendDir) * _lab;

        // --- 5. 上腕を初期姿勢基準で回転（蓄積しない）---
        Vector3    initElbowWorld = _armRoot.TransformPoint(_elbowRestLocal);
        Vector3    initUpperDir   = (initElbowWorld - shoulder).normalized;
        Vector3    newUpperDir    = (newElbow        - shoulder).normalized;
        Quaternion upperRot       = Quaternion.FromToRotation(initUpperDir, newUpperDir) * _initUpperRot;
        _upperArm.rotation        = upperRot;

        // --- 6. 前腕を回転（蓄積しない方式）---
        // 上腕回転後の肘・手位置を再取得
        Vector3 currentElbow = _foreArm.position;
        // 初期前腕方向を「現在の上腕回転」で再構成（基準として使う）
        Vector3    initForeDirWorld = upperRot * _initForeLocalDir;
        Vector3    newForeDir       = (target - currentElbow).normalized;
        Quaternion foreRot          = Quaternion.FromToRotation(initForeDirWorld, newForeDir) * _initForeRot;
        _foreArm.rotation           = foreRot;

        // --- 7. B_ArmObstacleIK にベースポーズを通知（あれば）---
        _obstacleIK?.SetAnimatedPose(_armIndex, upperRot, foreRot, _hand.rotation);
    }
    #endregion

    #region エディタ補助
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_armRoot == null || _upperArm == null || _foreArm == null || _hand == null) return;

        // ボーン同士を線で結ぶ
        Gizmos.color = new Color(0f, 1f, 0.5f);
        Gizmos.DrawLine(_armRoot.position, _upperArm.position);
        Gizmos.DrawLine(_upperArm.position, _foreArm.position);
        Gizmos.DrawLine(_foreArm.position, _hand.position);

        // 各関節
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_upperArm.position, 0.04f);
        Gizmos.DrawWireSphere(_foreArm.position,  0.035f);
        Gizmos.DrawWireSphere(_hand.position,     0.03f);

        // リーチターゲット
        if (_reachTarget != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_reachTarget.position, 0.06f);
            Gizmos.DrawLine(_hand.position, _reachTarget.position);
        }
    }
#endif
    #endregion
}
