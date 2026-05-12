using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 右下の武器HUDウィジェット全体を制御するビヘイビア。
/// 各子ビューへの購読登録・解除を一元管理します
/// </summary>
public class B_WeaponHUDView : MonoBehaviour
{
    #region 定義
    [SerializeField] private B_HealthRingView    _healthRing;
    [SerializeField] private B_AmmoView          _ammoView;
    [SerializeField] private B_SubWeaponSlotView _subWeaponSlot;
    [SerializeField] private Image               _mainWeaponIcon;

    [SerializeField] private B_PlayerHealth  _playerHealth;
    [SerializeField] private B_PlayerWeapon  _playerWeapon;
    #endregion

    #region 非公開メソッド
    private void Start()
    {
        if (_playerHealth == null)
        {
            Debug.LogError("[B_WeaponHUDView] PlayerHealth が未設定です");
            return;
        }
        if (_playerWeapon == null)
        {
            Debug.LogError("[B_WeaponHUDView] PlayerWeapon が未設定です");
            return;
        }

        _playerHealth.OnHpChanged         += _healthRing.UpdateRing;
        _playerWeapon.OnAmmoChanged       += _ammoView.UpdateAmmo;
        _playerWeapon.OnMainWeaponChanged += OnMainWeaponChanged;
        _playerWeapon.OnSubWeaponChanged  += _subWeaponSlot.UpdateSlot;

        _healthRing.UpdateRing(1f);

        if (_playerWeapon.MainWeaponData != null)
            OnMainWeaponChanged(_playerWeapon.MainWeaponData);

        _ammoView.UpdateAmmo(_playerWeapon.CurrentAmmo, _playerWeapon.ReserveAmmo);
        _subWeaponSlot.UpdateSlot(_playerWeapon.SubWeaponData);
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
            _playerHealth.OnHpChanged -= _healthRing.UpdateRing;

        if (_playerWeapon != null)
        {
            _playerWeapon.OnAmmoChanged       -= _ammoView.UpdateAmmo;
            _playerWeapon.OnMainWeaponChanged -= OnMainWeaponChanged;
            _playerWeapon.OnSubWeaponChanged  -= _subWeaponSlot.UpdateSlot;
        }
    }

    private void OnMainWeaponChanged(SO_WeaponData weaponData)
    {
        if (_mainWeaponIcon == null || weaponData == null) return;
        _mainWeaponIcon.sprite = weaponData.weaponIcon;
    }
    #endregion
}
