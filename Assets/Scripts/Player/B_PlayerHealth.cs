using UnityEngine;
using System;

/// <summary>
/// プレイヤーのHP値を管理するビヘイビア
/// </summary>
public class B_PlayerHealth : MonoBehaviour, I_Damageable
{
    #region 定義
    [Header("HP設定")]
    [SerializeField] private float _maxHp = 100f;

    private float _currentHp;

    /// <summary>HP変化時に正規化済み割合（0〜1）を通知するイベント</summary>
    public event Action<float> OnHpChanged;

    public float MaxHp     => _maxHp;
    public float CurrentHp => _currentHp;
    #endregion

    #region 公開メソッド
    /// <summary>
    /// ダメージを受けてHPを減少させます
    /// </summary>
    /// <param name="amount">ダメージ量（正の値）</param>
    public void TakeDamage(float amount)
    {
        if (amount < 0) return;
        _currentHp = Mathf.Max(0f, _currentHp - amount);
        OnHpChanged?.Invoke(_currentHp / _maxHp);
    }

    /// <summary>
    /// HPを回復します
    /// </summary>
    /// <param name="amount">回復量（正の値）</param>
    public void Heal(float amount)
    {
        if (amount < 0) return;
        _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
        OnHpChanged?.Invoke(_currentHp / _maxHp);
    }
    #endregion

    #region 非公開メソッド
    private void Awake()
    {
        _currentHp = _maxHp;
    }
    #endregion
}
