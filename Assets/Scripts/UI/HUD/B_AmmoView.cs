using UnityEngine;
using TMPro;

/// <summary>
/// 現在弾数と予備弾数のテキスト表示を担当するビヘイビア
/// </summary>
public class B_AmmoView : MonoBehaviour
{
    #region 定義
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _reserveAmmoText;

    private static readonly Color _normalColor = Color.white;
    private static readonly Color _emptyColor  = new Color(0.9f, 0.2f, 0.2f, 1f);
    #endregion

    #region 公開メソッド
    /// <summary>
    /// 弾数の表示を更新します。現在弾数が0の場合は赤色で強調します
    /// </summary>
    /// <param name="current">現在弾数</param>
    /// <param name="reserve">予備弾数</param>
    public void UpdateAmmo(int current, int reserve)
    {
        if (_currentAmmoText != null)
        {
            _currentAmmoText.text  = current.ToString();
            _currentAmmoText.color = current <= 0 ? _emptyColor : _normalColor;
        }

        if (_reserveAmmoText != null)
            _reserveAmmoText.text = reserve.ToString();
    }
    #endregion
}
