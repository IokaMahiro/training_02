using UnityEngine;

/// <summary>
/// マウスでカメラのピッチ（上下）とヨー（左右）を操作するデバッグ用コンポーネント。
/// MainCamera にアタッチして B_SpineLookIK のピッチ・ヨー両方の動作確認に使用します。
///
/// ── セットアップ ──────────────────────────────────────────────────────────────
///   カメラをキャラクタールートの子として配置する場合:
///     ローカル回転として適用されるため、キャラクター正面を基準に
///     どれだけカメラが上下左右へ向いているか = B_SpineLookIK への入力そのもの。
///
///   カメラが独立している場合:
///     ワールド Y 軸まわりのヨーとして回転する。
///     キャラクターの向きとカメラの向きは別々に管理すること。
///
/// ── 操作 ──────────────────────────────────────────────────────────────────────
///   マウス Y（上下）: ピッチ操作
///   マウス X（左右）: ヨー操作
///   Escape           : カーソルロック切替（_lockCursor が true の場合）
/// </summary>
public class B_DebugCameraLook : MonoBehaviour
{
    #region フィールド
    [Header("操作設定")]
    [SerializeField] private float _mouseSensitivity = 3f;

    [Header("ピッチ（上下）制限")]
    [SerializeField] private float _pitchMin = -80f;
    [SerializeField] private float _pitchMax =  80f;

    [Header("ヨー（左右）制限")]
    [Tooltip("ヨー制限を有効にするか。false にすると 360° 自由回転")]
    [SerializeField] private bool  _clampYaw  = false;
    [SerializeField] private float _yawMin    = -90f;
    [SerializeField] private float _yawMax    =  90f;

    [Header("カーソル")]
    [Tooltip("Play 開始時にカーソルをロックするか")]
    [SerializeField] private bool _lockCursor = true;

    private float _currentPitch;
    private float _currentYaw;
    #endregion

    #region Unity イベント
    private void Awake()
    {
        ApplyCursorLock(_lockCursor);
    }

    private void Update()
    {
        ProcessInput();
        ApplyRotation();
    }

    private void OnGUI()
    {
        GUI.Label(
            new Rect(10, 10, 500, 20),
            $"Pitch: {_currentPitch:F1}°  Yaw: {_currentYaw:F1}°  （マウスで操作 / Esc: カーソルロック切替）"
        );
    }
    #endregion

    #region 非公開メソッド
    private void ProcessInput()
    {
        // ピッチ: マウス上→カメラ上を向く（Y 軸入力を反転）
        _currentPitch -= Input.GetAxis("Mouse Y") * _mouseSensitivity;
        _currentPitch  = Mathf.Clamp(_currentPitch, _pitchMin, _pitchMax);

        // ヨー: マウス右→カメラ右を向く
        _currentYaw += Input.GetAxis("Mouse X") * _mouseSensitivity;
        if (_clampYaw)
        {
            _currentYaw = Mathf.Clamp(_currentYaw, _yawMin, _yawMax);
        }

        // Escape でカーソルロック切替
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            ApplyCursorLock(!isLocked);
        }
    }

    private void ApplyRotation()
    {
        transform.localEulerAngles = new Vector3(_currentPitch, _currentYaw, 0f);
    }

    private static void ApplyCursorLock(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }
    #endregion
}
