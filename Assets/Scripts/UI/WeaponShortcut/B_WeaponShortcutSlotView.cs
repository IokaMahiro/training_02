using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 武器ショートカットメニューの1スロットUIを担当するビヘイビア。
/// 武器アイコン・弾薬テキスト・選択ハイライト枠を管理します。
/// </summary>
public class B_WeaponShortcutSlotView : MonoBehaviour
{
    #region 定義
    [SerializeField] private Image            _weaponIcon;
    [SerializeField] private TextMeshProUGUI  _ammoText;
    /// <summary>
    /// 4本の枠線 Image（Border_T/B/L/R）を子に持つ親 RectTransform。
    /// SetSelected() でその子 Image の color を切り替えることで
    /// 選択枠を表示します（親自身は透明のままにします）。
    /// </summary>
    [SerializeField] private RectTransform    _selectionFrameRoot;
    [SerializeField] private GameObject       _emptyOverlay;   // 空スロット時に表示するオブジェクト

    private static readonly Color _ammoWhite  = Color.white;
    private static readonly Color _ammoRed    = new Color(0.90f, 0.20f, 0.20f, 1f);
    private static readonly Color _ammoGreen  = new Color(0.35f, 1.00f, 0.35f, 1f);
    private static readonly Color _frameOn    = new Color(1f, 1f, 1f, 1.00f);
    private static readonly Color _frameOff   = new Color(1f, 1f, 1f, 0.12f);

    private SO_WeaponData _cachedData;
    #endregion

    #region 公開メソッド
    /// <summary>
    /// スロットの武器データと弾数を設定します。
    /// weaponDataがnullの場合は空スロット表示になります。
    /// </summary>
    /// <param name="weaponData">表示する武器データ（nullで空スロット）</param>
    /// <param name="current">現在弾数または個数</param>
    /// <param name="reserve">予備弾数（CountOnlyモードでは無視）</param>
    public void Setup(SO_WeaponData weaponData, int current, int reserve)
    {
        _cachedData = weaponData;
        bool hasWeapon = weaponData != null;

        if (_weaponIcon != null)
        {
            _weaponIcon.gameObject.SetActive(hasWeapon && weaponData.weaponIcon != null);
            if (hasWeapon && weaponData.weaponIcon != null)
                _weaponIcon.sprite = weaponData.weaponIcon;
        }

        if (_emptyOverlay != null)
            _emptyOverlay.SetActive(!hasWeapon);

        if (_ammoText != null)
        {
            if (hasWeapon) RefreshAmmoText(current, reserve, weaponData);
            else           _ammoText.text = string.Empty;
        }
    }

    /// <summary>
    /// 弾数表示だけを更新します（アイコンは変更しません）。
    /// </summary>
    /// <param name="current">現在弾数または個数</param>
    /// <param name="reserve">予備弾数</param>
    public void UpdateAmmo(int current, int reserve)
    {
        if (_cachedData == null || _ammoText == null) return;
        RefreshAmmoText(current, reserve, _cachedData);
    }

    /// <summary>
    /// 選択状態のハイライト枠を切り替えます。
    /// _selectionFrameRoot の直下にある Image コンポーネント（Border_T/B/L/R）の
    /// color.a を変更することで「枠線のみ」の選択表現を実現します。
    /// </summary>
    /// <param name="isSelected">trueで選択状態（白枠表示）</param>
    public void SetSelected(bool isSelected)
    {
        if (_selectionFrameRoot == null) return;
        Color target = isSelected ? _frameOn : _frameOff;
        foreach (Transform child in _selectionFrameRoot)
        {
            var img = child.GetComponent<Image>();
            if (img != null) img.color = target;
        }
    }
    #endregion

    #region 非公開メソッド
    private void RefreshAmmoText(int current, int reserve, SO_WeaponData data)
    {
        string greenHex = ColorUtility.ToHtmlStringRGB(_ammoGreen);
        string redHex   = ColorUtility.ToHtmlStringRGB(_ammoRed);

        switch (data.ammoDisplayMode)
        {
            case SO_WeaponData.AmmoDisplayMode.CountOnly:
                // 例: "2"（手榴弾・ナイフ等）
                _ammoText.text = current <= 0
                    ? $"<color=#{redHex}>{current}</color>"
                    : current.ToString();
                break;

            case SO_WeaponData.AmmoDisplayMode.MagazineSlash:
            default:
                // 例: "30/62"（弾数が0なら現在弾数を赤色）
                string currentStr = current <= 0
                    ? $"<color=#{redHex}>{current}</color>"
                    : current.ToString();
                _ammoText.text = $"{currentStr}/<color=#{greenHex}>{reserve}</color>";
                break;
        }
    }
    #endregion
}
