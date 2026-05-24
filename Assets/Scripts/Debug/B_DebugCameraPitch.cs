using UnityEngine;

/// <summary>
/// マウス上下でカメラのピッチを操作するデバッグ用コンポーネント。
/// MainCamera にアタッチして B_SpinePitchIK の動作確認に使用します。
/// </summary>
public class B_DebugCameraPitch : MonoBehaviour
{
    #region 定義
    [Header("操作設定")]
    [SerializeField] private float _mouseSensitivity = 3f;
    [SerializeField] private float _pitchMin = -80f;
    [SerializeField] private float _pitchMax =  80f;

    private float _currentPitch;
    #endregion

    #region Unity イベント
    private void Update()
    {
        _currentPitch -= Input.GetAxis("Mouse Y") * _mouseSensitivity;
        _currentPitch  = Mathf.Clamp(_currentPitch, _pitchMin, _pitchMax);
        transform.localEulerAngles = new Vector3(_currentPitch, 0f, 0f);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Camera Pitch: {_currentPitch:F1}°  （マウス上下で操作）");
    }
    #endregion
}
