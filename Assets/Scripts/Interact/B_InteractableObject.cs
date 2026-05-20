using UnityEngine;

/// <summary>
/// インタラクト可能なオブジェクトへのアタッチ例。
/// <see cref="B_InteractPromptView.Data"/> に SO をセットしてから
/// <see cref="B_InteractPromptView.Show"/> を呼びます。
/// </summary>
public class B_InteractableObject : MonoBehaviour
{
    #region 定義
    [Header("表示データ")]
    [SerializeField] private SO_InteractableData _data;

    [Header("検知範囲")]
    [SerializeField] private float _interactRange = 2.5f;

    private B_InteractPromptView _promptView;
    private Transform            _playerTransform;
    private bool                 _isShowing;
    #endregion

    #region 公開メソッド
    /// <summary>インタラクトを実行します（ボタン押下時などに外部から呼び出す）。</summary>
    public virtual void Interact()
    {
        Debug.Log($"[B_InteractableObject] インタラクト: {_data?.itemName}");
    }
    #endregion

    #region 非公開メソッド
    private void Start()
    {
        _promptView = FindFirstObjectByType<B_InteractPromptView>();
        if (_promptView == null)
            Debug.LogWarning("[B_InteractableObject] B_InteractPromptView がシーンに見つかりません");
    }

    private void Update()
    {
        if (_promptView == null || !TryGetPlayer()) return;

        bool inRange = Vector3.Distance(transform.position, _playerTransform.position) <= _interactRange;

        if (inRange && !_isShowing)
        {
            _isShowing       = true;
            _promptView.Data = _data;         // 表示内容をセット
            _promptView.Show(transform);      // 位置を渡して表示
        }
        else if (!inRange && _isShowing)
        {
            _isShowing = false;
            _promptView.Hide();
        }
    }

    private void OnDestroy()
    {
        if (_isShowing) _promptView?.Hide();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRange);
    }

    private bool TryGetPlayer()
    {
        if (_playerTransform != null) return true;
        var go = GameObject.Find("Player");
        if (go == null) return false;
        _playerTransform = go.transform;
        return true;
    }
    #endregion
}
