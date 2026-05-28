using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// インタラクトプロンプト UI を制御するビヘイビア。
/// <see cref="Data"/> で表示内容を差し替え、<see cref="Show"/> / <see cref="Hide"/> で表示を切り替えます。
/// </summary>
public class B_InteractPromptView : MonoBehaviour
{
    #region 定義
    [SerializeField] private RectTransform   _Panel;
    [SerializeField] private Image           _buttonIcon;
    [SerializeField] private TextMeshProUGUI _buttonLabel;
    [SerializeField] private Image           _itemIcon;
    [SerializeField] private TextMeshProUGUI _itemLabel;

    private Camera    _camera;
    private Transform _currentTarget;
    private Vector3   _worldOffset;
    #endregion

    #region 公開プロパティ
    /// <summary>
    /// 表示するデータをセットします。セットと同時に UI テキスト・アイコンを更新します。
    /// </summary>
    public SO_InteractableData Data
    {
        set => ApplyData(value);
    }
    #endregion

    #region 公開メソッド
    /// <summary>
    /// プロンプトを表示します。事前に <see cref="Data"/> をセットしてください。
    /// </summary>
    /// <param name="target">追従するワールド Transform</param>
    public void Show(Transform target)
    {
        if (target == null || _Panel == null) return;
        _currentTarget = target;
        _Panel.gameObject.SetActive(true);
    }

    /// <summary>プロンプトを非表示にします。</summary>
    public void Hide()
    {
        _currentTarget = null;
        //_Panel.gameObject.SetActive(false);
    }
    #endregion

    #region 非公開メソッド
    private void Awake()
    {
        _camera = Camera.main;
        if (_camera == null)
            Debug.LogError("[B_InteractPromptView] MainCamera が見つかりません");
    }

    private void Start()
    {
        if (_Panel != null)
            _Panel.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_currentTarget == null || _camera == null) return;

        Vector3 screenPos = _camera.WorldToScreenPoint(_currentTarget.position + _worldOffset);

        if (screenPos.z < 0f)
        {
            _Panel.gameObject.SetActive(false);
            return;
        }

        _Panel.gameObject.SetActive(true);
        _Panel.position = screenPos;
    }

    /// <summary>SO の内容を UI に反映します。</summary>
    private void ApplyData(SO_InteractableData data)
    {
        if (data == null) return;

        _worldOffset = data.worldOffset;

        if (_buttonIcon  != null) 
            _buttonIcon.sprite  = data.buttonIcon;

        if (_buttonLabel != null) 
            _buttonLabel.text   = data.buttonLabel;

        if (_itemIcon != null)
        {
            _itemIcon.sprite = data.itemIcon;
            _itemIcon.gameObject.SetActive(data.itemIcon != null);
        }

        if (_itemLabel != null)
            _itemLabel.text = data.count > 1 ? $"{data.itemName} ×{data.count}" : data.itemName;
    }
    #endregion
}
