using UnityEngine;
using System;

/// <summary>
/// 武器ショートカットメニュー全体を制御するビヘイビア。
/// 十字形レイアウト（上下左右 各2スロット、計8スロット）を管理します。
///
/// スロット割り当て:
///   Up    : index 0（外側）, 1（内側）
///   Down  : index 2（内側）, 3（外側）
///   Left  : index 4（上）,   5（下）
///   Right : index 6（上）,   7（下）
///
/// 入力フロー:
///   Tab       → メニュー開閉
///   W / ↑    → Upグループ選択（同方向を再度押すと0⇔1切替）
///   S / ↓    → Downグループ
///   A / ←    → Leftグループ
///   D / →    → Rightグループ
///   Enter     → 確定（OnWeaponSelected 発火）
/// </summary>
public class B_WeaponShortcutUI : MonoBehaviour
{
    #region 定義
    public const int DirectionUp    = 0;
    public const int DirectionDown  = 1;
    public const int DirectionLeft  = 2;
    public const int DirectionRight = 3;

    // [方向][0=最初のスロット, 1=2番目のスロット]
    private static readonly int[,] SlotGroups = new int[4, 2]
    {
        { 0, 1 }, // Up
        { 2, 3 }, // Down
        { 4, 5 }, // Left
        { 6, 7 }, // Right
    };

    [Header("スロットビュー（Up外→Up内→Down内→Down外→Left上→Left下→Right上→Right下）")]
    [SerializeField] private B_WeaponShortcutSlotView[] _slotViews;

    [Header("フェード")]
    [SerializeField] private CanvasGroup _rootCanvasGroup;
    [SerializeField] [Range(1f, 20f)] private float _fadeSpeed = 10f;

    [Header("入力キー")]
    [SerializeField] private KeyCode _toggleKey      = KeyCode.Tab;
    [SerializeField] private KeyCode _navUpKey       = KeyCode.W;
    [SerializeField] private KeyCode _navDownKey     = KeyCode.S;
    [SerializeField] private KeyCode _navLeftKey     = KeyCode.A;
    [SerializeField] private KeyCode _navRightKey    = KeyCode.D;
    [SerializeField] private KeyCode _navUpArrow     = KeyCode.UpArrow;
    [SerializeField] private KeyCode _navDownArrow   = KeyCode.DownArrow;
    [SerializeField] private KeyCode _navLeftArrow   = KeyCode.LeftArrow;
    [SerializeField] private KeyCode _navRightArrow  = KeyCode.RightArrow;
    [SerializeField] private KeyCode _confirmKey     = KeyCode.Return;

    /// <summary>スロット選択確定時に（スロットインデックス, 武器データ）を通知します。</summary>
    public event Action<int, SO_WeaponData> OnWeaponSelected;

    private bool        _isOpen;
    private int         _selectedIndex;
    private int         _currentDirection;
    private float       _targetAlpha;
    private SlotCache[] _caches;

    private struct SlotCache
    {
        public SO_WeaponData WeaponData;
        public int           Current;
        public int           Reserve;
    }
    #endregion

    #region 公開メソッド
    /// <summary>指定スロットに武器データと弾数を設定します（null で空スロット）。</summary>
    public void SetSlot(int slotIndex, SO_WeaponData weaponData, int current, int reserve)
    {
        if (!IsValidIndex(slotIndex)) return;
        _caches[slotIndex] = new SlotCache { WeaponData = weaponData, Current = current, Reserve = reserve };
        _slotViews[slotIndex].Setup(weaponData, current, reserve);
    }

    /// <summary>既にセット済みのスロットの弾数だけを更新します。</summary>
    public void UpdateSlotAmmo(int slotIndex, int current, int reserve)
    {
        if (!IsValidIndex(slotIndex)) return;
        ref var cache = ref _caches[slotIndex];
        cache.Current = current;
        cache.Reserve = reserve;
        _slotViews[slotIndex].UpdateAmmo(current, reserve);
    }

    /// <summary>メニューを開きます。</summary>
    public void Open()
    {
        if (_isOpen) return;
        _isOpen      = true;
        _targetAlpha = 1f;
    }

    /// <summary>メニューを閉じます。現在の選択を確定し OnWeaponSelected を発火します。</summary>
    public void Close()
    {
        if (!_isOpen) return;
        _isOpen      = false;
        _targetAlpha = 0f;
        FireSelectionEvent();
    }

    /// <summary>
    /// 方向キーでスロットを選択します。
    /// 同じ方向を再度押すとグループ内の2スロットを切り替えます。
    /// </summary>
    /// <param name="direction">0=Up / 1=Down / 2=Left / 3=Right</param>
    public void Navigate(int direction)
    {
        if (!_isOpen || _slotViews == null || _slotViews.Length < 8) return;

        _slotViews[_selectedIndex].SetSelected(false);

        if (_currentDirection == direction)
        {
            // 同じ方向を再度押したらグループ内で交互切替
            int s0 = SlotGroups[direction, 0];
            int s1 = SlotGroups[direction, 1];
            _selectedIndex = (_selectedIndex == s0) ? s1 : s0;
        }
        else
        {
            // 別の方向グループへ移動
            _currentDirection = direction;
            _selectedIndex    = SlotGroups[direction, 0];
        }

        _slotViews[_selectedIndex].SetSelected(true);
    }

    public bool IsOpen        => _isOpen;
    public int  SelectedIndex => _selectedIndex;
    #endregion

    #region 非公開メソッド
    private void Awake()
    {
        int count = _slotViews != null ? _slotViews.Length : 0;
        _caches = new SlotCache[count];

        _targetAlpha      = 0f;
        _currentDirection = DirectionUp;
        ApplyCanvasGroup(0f, false);

        if (count >= 8)
        {
            _selectedIndex = SlotGroups[DirectionUp, 0]; // = 0
            _slotViews[_selectedIndex].SetSelected(true);
        }
    }

    private void Update()
    {
        ProcessInput();
        UpdateFade();
    }

    private void ProcessInput()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            if (_isOpen) Close();
            else         Open();
        }
        if (!_isOpen) return;

        if (Input.GetKeyDown(_navUpKey)    || Input.GetKeyDown(_navUpArrow))    Navigate(DirectionUp);
        if (Input.GetKeyDown(_navDownKey)  || Input.GetKeyDown(_navDownArrow))  Navigate(DirectionDown);
        if (Input.GetKeyDown(_navLeftKey)  || Input.GetKeyDown(_navLeftArrow))  Navigate(DirectionLeft);
        if (Input.GetKeyDown(_navRightKey) || Input.GetKeyDown(_navRightArrow)) Navigate(DirectionRight);
        if (Input.GetKeyDown(_confirmKey))                                        Close();
    }

    private void UpdateFade()
    {
        if (_rootCanvasGroup == null) return;
        float newAlpha = Mathf.MoveTowards(
            _rootCanvasGroup.alpha, _targetAlpha, Time.unscaledDeltaTime * _fadeSpeed);
        ApplyCanvasGroup(newAlpha, newAlpha > 0.01f);
    }

    private void ApplyCanvasGroup(float alpha, bool interactive)
    {
        if (_rootCanvasGroup == null) return;
        _rootCanvasGroup.alpha          = alpha;
        _rootCanvasGroup.interactable   = interactive;
        _rootCanvasGroup.blocksRaycasts = interactive;
    }

    private void FireSelectionEvent()
    {
        if (!IsValidIndex(_selectedIndex)) return;
        var cache = _caches[_selectedIndex];
        if (cache.WeaponData != null)
            OnWeaponSelected?.Invoke(_selectedIndex, cache.WeaponData);
    }

    private bool IsValidIndex(int index)
        => _slotViews != null && (uint)index < (uint)_slotViews.Length;
    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        int count = _slotViews != null ? _slotViews.Length : 0;
        if (_caches == null || _caches.Length != count)
            _caches = new SlotCache[count];
    }
#endif
}
