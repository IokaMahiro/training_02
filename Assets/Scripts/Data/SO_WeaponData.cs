using UnityEngine;

/// <summary>
/// 武器1種のデータを保持するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "RE4HUD/WeaponData")]
public class SO_WeaponData : ScriptableObject
{
    // ショートカットUIでの弾薬表示形式
    public enum AmmoDisplayMode
    {
        /// <summary>マガジン形式（現在弾数 / 予備弾数）例: 30/62</summary>
        MagazineSlash,
        /// <summary>個数のみ（手榴弾・ナイフ等）例: 2</summary>
        CountOnly,
    }

    #region 定義
    [Header("武器情報")]
    [SerializeField] public string          weaponName;
    [SerializeField] public Sprite          weaponIcon;

    [Header("弾薬")]
    [SerializeField] public int             maxAmmoInMag;
    [SerializeField] public int             maxReserveAmmo;
    [SerializeField] public AmmoDisplayMode ammoDisplayMode = AmmoDisplayMode.MagazineSlash;
    #endregion
}
