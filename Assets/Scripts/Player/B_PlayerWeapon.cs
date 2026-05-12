using UnityEngine;
using System;

/// <summary>
/// プレイヤーの武器・弾薬状態を管理するビヘイビア
/// </summary>
public class B_PlayerWeapon : MonoBehaviour
{
    #region 定義
    [SerializeField] private SO_WeaponData _mainWeaponData;
    [SerializeField] private SO_WeaponData _subWeaponData;

    private int _currentAmmo;
    private int _reserveAmmo;

    /// <summary>弾数変化時に（現在弾数, 予備弾数）を通知するイベント</summary>
    public event Action<int, int>        OnAmmoChanged;
    /// <summary>メイン武器切り替え時に通知するイベント</summary>
    public event Action<SO_WeaponData>   OnMainWeaponChanged;
    /// <summary>サブ武器切り替え時に通知するイベント</summary>
    public event Action<SO_WeaponData>   OnSubWeaponChanged;

    public SO_WeaponData MainWeaponData => _mainWeaponData;
    public SO_WeaponData SubWeaponData  => _subWeaponData;
    public int CurrentAmmo              => _currentAmmo;
    public int ReserveAmmo              => _reserveAmmo;
    #endregion

    #region 公開メソッド
    /// <summary>
    /// 弾を消費します（射撃時に呼び出す）
    /// </summary>
    /// <param name="amount">消費弾数</param>
    public void ConsumeAmmo(int amount = 1)
    {
        if (_currentAmmo <= 0) return;
        _currentAmmo = Mathf.Max(0, _currentAmmo - amount);
        OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
    }

    /// <summary>
    /// リロードを実行します
    /// </summary>
    public void Reload()
    {
        if (_mainWeaponData == null) return;
        int needed   = _mainWeaponData.maxAmmoInMag - _currentAmmo;
        int reloaded = Mathf.Min(needed, _reserveAmmo);
        _currentAmmo  += reloaded;
        _reserveAmmo  -= reloaded;
        OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
    }

    /// <summary>
    /// メイン武器を切り替えます
    /// </summary>
    /// <param name="weaponData">切り替え先の武器データ</param>
    public void SetMainWeapon(SO_WeaponData weaponData)
    {
        if (weaponData == null) return;
        _mainWeaponData = weaponData;
        _currentAmmo    = weaponData.maxAmmoInMag;
        _reserveAmmo    = weaponData.maxReserveAmmo;
        OnMainWeaponChanged?.Invoke(_mainWeaponData);
        OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
    }

    /// <summary>
    /// サブ武器を設定します
    /// </summary>
    /// <param name="weaponData">サブ武器データ（nullで非表示）</param>
    public void SetSubWeapon(SO_WeaponData weaponData)
    {
        _subWeaponData = weaponData;
        OnSubWeaponChanged?.Invoke(_subWeaponData);
    }
    #endregion

    #region 非公開メソッド
    private void Awake()
    {
        if (_mainWeaponData == null) return;
        _currentAmmo = _mainWeaponData.maxAmmoInMag;
        _reserveAmmo = _mainWeaponData.maxReserveAmmo;
    }
    #endregion
}
