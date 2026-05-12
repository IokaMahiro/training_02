using UnityEngine;

/// <summary>
/// 武器1種のデータを保持するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "RE4HUD/WeaponData")]
public class SO_WeaponData : ScriptableObject
{
    #region 定義
    [Header("武器情報")]
    [SerializeField] public string weaponName;
    [SerializeField] public Sprite weaponIcon;

    [Header("弾薬")]
    [SerializeField] public int maxAmmoInMag;
    [SerializeField] public int maxReserveAmmo;
    #endregion
}
