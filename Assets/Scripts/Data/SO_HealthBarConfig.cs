using UnityEngine;

/// <summary>
/// HPリングゲージの表示パラメータを保持するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "HealthBarConfig", menuName = "RE4HUD/HealthBarConfig")]
public class SO_HealthBarConfig : ScriptableObject
{
    #region 定義
    [Header("HP閾値（割合 0〜1）")]
    [SerializeField] public float yellowThreshold = 0.5f;
    [SerializeField] public float redThreshold    = 0.25f;

    [Header("リングカラー")]
    [SerializeField] public Color healthyColor = new Color(0.4f, 1f, 0.4f, 1f);
    [SerializeField] public Color warningColor = new Color(1f, 0.85f, 0.1f, 1f);
    [SerializeField] public Color dangerColor  = new Color(0.9f, 0.15f, 0.15f, 1f);

    [Header("アニメーション")]
    [SerializeField] public float fillAnimSpeed = 6f;
    #endregion
}
