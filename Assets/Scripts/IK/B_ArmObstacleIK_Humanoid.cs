using UnityEngine;

/// <summary>
/// 腕・手が壁などにめり込まないよう、Unity Humanoid IK（OnAnimatorIK）で手先を引き戻すコンポーネント。
///
/// 動作順序：
///   OnAnimatorIK（Animator が IK を解く直前に呼ばれる）
///     1. GetBoneTransform でアニメ後の手先位置を取得
///     2. 肩 → 手先 へ Raycast
///     3. 壁ヒット → SetIKPosition でターゲットを壁手前に指定
///        ヒットなし → ウェイトをゼロへ戻す（アニメに従う）
///     Unity が肩・肘・手首の回転を自動解決する
///
/// 前提：
///   - Animator コンポーネントが同じ GameObject にあること
///   - Rig が Humanoid に設定されていること
///   - Animator Controller の対象レイヤーで IK Pass が ON であること
/// </summary>
public class B_ArmObstacleIK_Humanoid : MonoBehaviour
{
    #region 内部型定義
    [System.Serializable]
    public struct ArmSetting
    {
        [Tooltip("左手 / 右手の IK ゴール")]
        public AvatarIKGoal ikGoal;

        [Tooltip("Raycast の起点となる肩の Transform（未指定時は肩ボーンを自動取得）")]
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

    private Animator  _animator;
    private float[]   _weights;
    private Vector3[] _smoothedTargets;
    #endregion

    #region Unity イベント
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
            Debug.LogError("[B_ArmObstacleIK_Humanoid] Animator が見つかりません");

        if (_arms == null || _arms.Length == 0)
        {
            Debug.LogWarning("[B_ArmObstacleIK_Humanoid] _arms が未設定です");
            return;
        }

        _weights         = new float[_arms.Length];
        _smoothedTargets = new Vector3[_arms.Length];
    }

    private void Start()
    {
        if (_animator == null || _arms == null) return;

        // IK ターゲットの初期値を手先位置に合わせる
        for (int i = 0; i < _arms.Length; i++)
        {
            Transform hand = _animator.GetBoneTransform(GoalToBone(_arms[i].ikGoal));
            if (hand != null)
                _smoothedTargets[i] = hand.position;
        }
    }

    /// <summary>
    /// Animator が IK を解く直前に呼ばれます。
    /// Update より後、LateUpdate より前のタイミングです。
    /// </summary>
    private void OnAnimatorIK(int layerIndex)
    {
        if (_animator == null || _arms == null) return;

        for (int i = 0; i < _arms.Length; i++)
        {
            ref var arm = ref _arms[i];

            // アニメ後の手先ボーンを取得
            Transform handBone = _animator.GetBoneTransform(GoalToBone(arm.ikGoal));
            if (handBone == null) continue;

            // Raycast 起点：指定があればそれを、なければ肩ボーンを使う
            Vector3 origin = arm.raycastOrigin != null
                ? arm.raycastOrigin.position
                : GetShoulderPosition(arm.ikGoal);

            Vector3 handPos = handBone.position;
            Vector3 toHand  = handPos - origin;
            float   reach   = toHand.magnitude;

            // --- 1. Raycast で壁を検知 ---
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

            // --- 3. Unity 組み込み IK に渡す ---
            //   ウェイトをゼロにすれば Animator のアニメーションに完全に従う
            //   ウェイトを上げると SetIKPosition のターゲットへ手先が引き寄せられる
            //   肩・肘の回転は Unity が自動解決する
            _animator.SetIKPositionWeight(arm.ikGoal, _weights[i]);
            _animator.SetIKPosition(arm.ikGoal, _smoothedTargets[i]);
        }
    }
    #endregion

    #region 非公開メソッド
    /// <summary>AvatarIKGoal を対応する HumanBodyBones（手先）に変換します。</summary>
    private static HumanBodyBones GoalToBone(AvatarIKGoal goal) => goal switch
    {
        AvatarIKGoal.LeftHand  => HumanBodyBones.LeftHand,
        AvatarIKGoal.RightHand => HumanBodyBones.RightHand,
        AvatarIKGoal.LeftFoot  => HumanBodyBones.LeftFoot,
        AvatarIKGoal.RightFoot => HumanBodyBones.RightFoot,
        _                      => HumanBodyBones.LastBone,
    };

    /// <summary>AvatarIKGoal に対応する肩ボーン位置を返します。</summary>
    private Vector3 GetShoulderPosition(AvatarIKGoal goal)
    {
        HumanBodyBones shoulder = goal == AvatarIKGoal.LeftHand
            ? HumanBodyBones.LeftUpperArm
            : HumanBodyBones.RightUpperArm;

        Transform bone = _animator.GetBoneTransform(shoulder);
        return bone != null ? bone.position : transform.position;
    }
    #endregion

    #region エディタ補助
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_animator == null || _arms == null) return;

        for (int i = 0; i < _arms.Length; i++)
        {
            ref var arm = ref _arms[i];

            Transform handBone = _animator.GetBoneTransform(GoalToBone(arm.ikGoal));
            if (handBone == null) continue;

            Vector3 origin = arm.raycastOrigin != null
                ? arm.raycastOrigin.position
                : GetShoulderPosition(arm.ikGoal);

            // 肩 → 手先 のレイ
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, handBone.position);
            Gizmos.DrawWireSphere(handBone.position, 0.04f);

            // IK ターゲット（ウェイトがある場合）
            if (_weights != null && _weights.Length > i && _weights[i] > 0.01f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_smoothedTargets[i], 0.05f);
                Gizmos.DrawLine(handBone.position, _smoothedTargets[i]);

                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.Label(
                    _smoothedTargets[i] + Vector3.up * 0.08f,
                    $"{arm.ikGoal}  w:{_weights[i]:F2}"
                );
            }
        }
    }
#endif
    #endregion
}
