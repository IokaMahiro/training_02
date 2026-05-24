using UnityEngine;

/// <summary>
/// 腕・手が壁などにめり込まないよう、LateUpdate でボーンを直接操作するコンポーネント。
/// OnAnimatorIK / Humanoid IK を使わず、余弦定理による解析的な 2ボーン IK で解きます。
///
/// 動作順序（LateUpdate）：
///   1. Raycast で壁を検知し、IK ターゲット位置を決定
///   2. ウェイトをスムーズに遷移
///   3. 2ボーン IK（上腕・前腕）をワールド空間クォータニオンで適用
///
/// Animator なし環境でも、Start で記録した初期姿勢を基準にするため蓄積しません。
/// </summary>
public class B_ArmObstacleIK : MonoBehaviour
{
    #region 内部型定義
    [System.Serializable]
    public struct ArmSetting
    {
        [Tooltip("上腕ボーン（肩）Transform")]
        public Transform upperArm;

        [Tooltip("前腕ボーン（肘）Transform")]
        public Transform foreArm;

        [Tooltip("手首ボーン Transform（IK の終端）")]
        public Transform hand;

        [Tooltip("Raycast 起点。upperArm と異なる場合のみ設定")]
        public Transform raycastOrigin;

        [Tooltip("障害物と判定するレイヤー")]
        public LayerMask obstacleMask;

        [Tooltip("壁面から手を離す距離（m）")]
        public float wallOffset;

        [Tooltip("IK 適用の最大ウェイト")]
        [Range(0f, 1f)]
        public float maxWeight;
    }
    #endregion

    #region 定義
    [Header("腕設定（左右を登録）")]
    [SerializeField] private ArmSetting[] _arms;

    [Header("ウェイト遷移速度")]
    [Tooltip("壁検知時のウェイト立ち上がり速度")]
    [SerializeField] [Range(1f, 20f)] private float _weightUpSpeed   = 12f;

    [Tooltip("壁から離れたときのウェイト戻り速度")]
    [SerializeField] [Range(1f, 20f)] private float _weightDownSpeed =  6f;

    [Tooltip("IK ターゲット位置のスムージング速度（m/s）")]
    [SerializeField] [Range(1f, 30f)] private float _positionSpeed   = 10f;

    private float[]   _weights;          // 現在のウェイト（腕ごと）
    private Vector3[] _smoothedTargets;  // スムージング済み IK ターゲット位置

    // Animator なし時の初期姿勢キャッシュ [腕][0=上腕, 1=前腕, 2=手首]
    private bool          _hasAnimator;
    private Quaternion[][] _restRotations;
    #endregion

    #region 公開メソッド
    /// <summary>
    /// Animator の代わりに外部（デバッグ用姿勢コントローラーなど）からベースポーズを注入します。
    /// Animator がない環境で呼び出すことで、毎フレームの姿勢をリセットできます。
    /// </summary>
    /// <param name="armIndex">_arms 配列のインデックス</param>
    public void SetAnimatedPose(int armIndex, Quaternion upperRot, Quaternion foreRot, Quaternion handRot)
    {
        if (_restRotations == null || armIndex >= _restRotations.Length) return;
        _restRotations[armIndex][0] = upperRot;
        _restRotations[armIndex][1] = foreRot;
        _restRotations[armIndex][2] = handRot;
    }
    #endregion

    #region Unity イベント
    private void Awake()
    {
        if (_arms == null || _arms.Length == 0)
        {
            Debug.LogWarning("[B_ArmObstacleIK] _arms が未設定です");
            return;
        }

        _weights         = new float[_arms.Length];
        _smoothedTargets = new Vector3[_arms.Length];
    }

    private void Start()
    {
        if (_arms == null) return;

        _hasAnimator = GetComponent<Animator>() != null
                    || GetComponentInParent<Animator>() != null
                    || GetComponentInChildren<Animator>() != null;

        // Animator なし時は初期姿勢をキャッシュしておく（蓄積防止）
        if (!_hasAnimator)
        {
            _restRotations = new Quaternion[_arms.Length][];
            for (int i = 0; i < _arms.Length; i++)
            {
                _restRotations[i] = new Quaternion[3];
                ref var arm = ref _arms[i];
                if (arm.upperArm != null) _restRotations[i][0] = arm.upperArm.rotation;
                if (arm.foreArm  != null) _restRotations[i][1] = arm.foreArm.rotation;
                if (arm.hand     != null) _restRotations[i][2] = arm.hand.rotation;
            }
        }

        // IK ターゲットの初期位置を手先にセット
        for (int i = 0; i < _arms.Length; i++)
        {
            if (_arms[i].hand != null)
                _smoothedTargets[i] = _arms[i].hand.position;
        }
    }

    private void LateUpdate()
    {
        if (_arms == null) return;

        // フォールバック：Start() で _restRotations が生成されなかった場合に備え、
        // ここで遅延初期化する（Animator なし環境でのボーン参照タイミング問題対策）
        if (!_hasAnimator && _restRotations == null)
        {
            _restRotations = new Quaternion[_arms.Length][];
            for (int i = 0; i < _arms.Length; i++)
            {
                _restRotations[i] = new Quaternion[3];
                ref var a = ref _arms[i];
                if (a.upperArm != null) _restRotations[i][0] = a.upperArm.rotation;
                if (a.foreArm  != null) _restRotations[i][1] = a.foreArm.rotation;
                if (a.hand     != null) _restRotations[i][2] = a.hand.rotation;
            }
        }

        for (int i = 0; i < _arms.Length; i++)
        {
            ref var arm = ref _arms[i];
            if (arm.upperArm == null || arm.foreArm == null || arm.hand == null) continue;

            // Animator なし → 初期姿勢に戻してから計算（蓄積防止）
            if (!_hasAnimator && _restRotations != null)
            {
                arm.upperArm.rotation = _restRotations[i][0];
                arm.foreArm.rotation  = _restRotations[i][1];
                arm.hand.rotation     = _restRotations[i][2];
            }

            // --- 1. Raycast で壁を検知 ---
            Vector3 origin   = arm.raycastOrigin != null ? arm.raycastOrigin.position : arm.upperArm.position;
            Vector3 handPos  = arm.hand.position;
            Vector3 toHand   = handPos - origin;
            float   reach    = toHand.magnitude;

            float   targetWeight = 0f;
            Vector3 targetPos    = handPos;

            if (reach > 0.001f &&
                Physics.Raycast(origin, toHand / reach, out RaycastHit hit, reach, arm.obstacleMask))
            {
                targetPos    = hit.point + hit.normal * arm.wallOffset;
                targetWeight = arm.maxWeight;
            }

            // --- 2. ウェイトとターゲット位置をスムーズに更新 ---
            float weightSpeed    = targetWeight > _weights[i] ? _weightUpSpeed : _weightDownSpeed;
            _weights[i]          = Mathf.MoveTowards(_weights[i], targetWeight, Time.deltaTime * weightSpeed);
            _smoothedTargets[i]  = Vector3.MoveTowards(_smoothedTargets[i], targetPos, Time.deltaTime * _positionSpeed);

            // --- 3. IK 適用 ---
            if (_weights[i] > 0.001f)
            {
                Vector3 blendedTarget = Vector3.Lerp(handPos, _smoothedTargets[i], _weights[i]);
                SolveTwoBoneIK(arm.upperArm, arm.foreArm, arm.hand, blendedTarget);
            }
        }
    }
    #endregion

    #region 非公開メソッド（IK ソルバー）
    /// <summary>
    /// 余弦定理による解析的 2ボーン IK。
    /// 上腕・前腕のワールド回転を target に手先が届くよう直接書き換えます。
    ///
    /// 曲げ方向はアニメ後の肘位置から自動取得するため、
    /// ポールターゲットなしで自然な肘の向きを維持します。
    /// </summary>
    private static void SolveTwoBoneIK(
        Transform upperArm, Transform foreArm, Transform hand, Vector3 target)
    {
        Vector3 A = upperArm.position; // 肩
        Vector3 B = foreArm.position;  // 肘（アニメ後）
        Vector3 C = hand.position;     // 手首（アニメ後）

        float lab = Vector3.Distance(A, B); // 上腕長
        float lbc = Vector3.Distance(B, C); // 前腕長
        if (lab < 0.0001f || lbc < 0.0001f) return;

        // ターゲットを到達可能距離内にクランプ
        Vector3 AT   = target - A;
        float   lAT  = Mathf.Clamp(AT.magnitude,
                           Mathf.Abs(lab - lbc) + 0.001f,
                           lab + lbc            - 0.001f);
        Vector3 dir  = AT.sqrMagnitude > 0.0001f ? AT.normalized : upperArm.forward;
        target       = A + dir * lAT;

        // 曲げ方向：アニメ後の肘位置から A→target 軸への垂直成分を取る
        Vector3 AB      = B - A;
        Vector3 bendDir = AB - Vector3.Dot(AB, dir) * dir;

        if (bendDir.sqrMagnitude < 0.0001f)
        {
            // フォールバック：上腕の up 方向を使う
            bendDir = Vector3.Cross(dir, upperArm.up);
            if (bendDir.sqrMagnitude < 0.0001f)
                bendDir = Vector3.Cross(dir, Vector3.right);
        }
        bendDir.Normalize();

        // 余弦定理で肩側の角度を求める
        float cosA = (lab * lab + lAT * lAT - lbc * lbc) / (2f * lab * lAT);
        float rad  = Mathf.Acos(Mathf.Clamp(cosA, -1f, 1f));

        // 新しい肘位置
        //   A→target 方向に cos(rad)・上腕長、
        //   曲げ方向に sin(rad)・上腕長 だけ進んだ点
        Vector3 newElbow = A + (Mathf.Cos(rad) * dir + Mathf.Sin(rad) * bendDir) * lab;

        // 上腕を回転（旧肘方向 → 新肘方向）
        Vector3 oldUpperDir = (B - A).normalized;
        Vector3 newUpperDir = (newElbow - A).normalized;
        if (Vector3.Dot(oldUpperDir, newUpperDir) < 0.99999f)
            upperArm.rotation = Quaternion.FromToRotation(oldUpperDir, newUpperDir) * upperArm.rotation;

        // 上腕回転後の肘・手首位置を再取得（子ボーンが追従した状態）
        Vector3 updatedElbow = foreArm.position;
        Vector3 updatedHand  = hand.position;

        // 前腕を回転（現在の手方向 → ターゲット方向）
        Vector3 oldForeDir = (updatedHand - updatedElbow).normalized;
        Vector3 newForeDir = (target      - updatedElbow).normalized;
        if (Vector3.Dot(oldForeDir, newForeDir) < 0.99999f)
            foreArm.rotation = Quaternion.FromToRotation(oldForeDir, newForeDir) * foreArm.rotation;
    }
    #endregion

    #region エディタ補助
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_arms == null) return;

        for (int i = 0; i < _arms.Length; i++)
        {
            ref var arm = ref _arms[i];
            if (arm.upperArm == null || arm.hand == null) continue;

            Vector3 origin  = arm.raycastOrigin != null ? arm.raycastOrigin.position : arm.upperArm.position;
            Vector3 handPos = arm.hand.position;

            // 肩 → 手先 のレイ
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, handPos);
            Gizmos.DrawWireSphere(handPos, 0.04f);

            // IK ターゲット（ウェイトがある場合）
            if (_weights != null && _weights.Length > i && _weights[i] > 0.01f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_smoothedTargets[i], 0.05f);
                Gizmos.DrawLine(handPos, _smoothedTargets[i]);

                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.Label(
                    _smoothedTargets[i] + Vector3.up * 0.08f,
                    $"w:{_weights[i]:F2}"
                );
            }
        }
    }
#endif
    #endregion
}
