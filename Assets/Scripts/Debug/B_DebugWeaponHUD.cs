using UnityEngine;

/// <summary>
/// PlayMode中にキー入力でHP・弾薬をデバッグ操作するビヘイビア
/// </summary>
public class B_DebugWeaponHUD : MonoBehaviour
{
    #region 定義
    [Header("デバッグ対象")]
    [SerializeField] private B_PlayerHealth _playerHealth;
    [SerializeField] private B_PlayerWeapon _playerWeapon;

    [Header("操作量")]
    [SerializeField] private float _damageAmount = 10f;
    [SerializeField] private float _healAmount   = 10f;

    private static readonly GUIStyle _boxStyle   = new GUIStyle();
    private static readonly GUIStyle _labelStyle = new GUIStyle();
    private bool _guiInitialized;
    #endregion

    #region 非公開メソッド
    private void Awake()
    {
        if (_playerHealth == null)
            _playerHealth = GetComponent<B_PlayerHealth>();

        if (_playerWeapon == null)
            _playerWeapon = GetComponent<B_PlayerWeapon>();
    }

    private void Update()
    {
        if (_playerHealth == null || _playerWeapon == null) return;

        if (Input.GetKeyDown(KeyCode.Z))
            _playerHealth.TakeDamage(_damageAmount);

        if (Input.GetKeyDown(KeyCode.X))
            _playerHealth.Heal(_healAmount);

        if (Input.GetKeyDown(KeyCode.C))
            _playerWeapon.ConsumeAmmo(1);

        if (Input.GetKeyDown(KeyCode.R))
            _playerWeapon.Reload();
    }

    private void OnGUI()
    {
        InitGuiStyles();

        const float w = 220f;
        const float h = 110f;
        float x = 10f;
        float y = Screen.height - h - 10f;

        GUI.Box(new Rect(x, y, w, h), GUIContent.none, _boxStyle);

        float lx = x + 10f;
        float ly = y + 8f;
        const float lh = 22f;

        GUI.Label(new Rect(lx, ly,       w - 20, lh), "[ Z ]  ダメージ (-" + _damageAmount + ")",  _labelStyle);
        GUI.Label(new Rect(lx, ly + lh,  w - 20, lh), "[ X ]  回復   (+" + _healAmount + ")",      _labelStyle);
        GUI.Label(new Rect(lx, ly + lh * 2, w - 20, lh), "[ C ]  弾消費 (-1)",                     _labelStyle);
        GUI.Label(new Rect(lx, ly + lh * 3, w - 20, lh), "[ R ]  リロード",                        _labelStyle);
    }

    private void InitGuiStyles()
    {
        if (_guiInitialized) return;
        _guiInitialized = true;

        var bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
        bg.Apply();

        _boxStyle.normal.background = bg;

        _labelStyle.normal.textColor = Color.white;
        _labelStyle.fontSize         = 13;
        _labelStyle.padding          = new RectOffset(4, 0, 2, 0);
    }
    #endregion
}
