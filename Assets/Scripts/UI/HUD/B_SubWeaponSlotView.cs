using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リング上部のサブ武器スロットと区切り線の表示を担当するビヘイビア
/// </summary>
public class B_SubWeaponSlotView : MonoBehaviour
{
    #region 定義
    [SerializeField] private Image      _subWeaponIcon;
    [SerializeField] private GameObject _divider;
    #endregion

    #region 公開メソッド
    /// <summary>
    /// サブ武器スロットの表示を更新します。weaponDataがnullの場合は非表示にします
    /// </summary>
    /// <param name="weaponData">表示する武器データ（nullで非表示）</param>
    public void UpdateSlot(SO_WeaponData weaponData)
    {
        bool hasWeapon = weaponData != null && weaponData.weaponIcon != null;

        if (_subWeaponIcon != null)
        {
            _subWeaponIcon.sprite = weaponData?.weaponIcon;
            _subWeaponIcon.gameObject.SetActive(hasWeapon);
        }

        if (_divider != null)
            _divider.SetActive(hasWeapon);
    }
    #endregion
}
