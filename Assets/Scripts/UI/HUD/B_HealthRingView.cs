using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武器アイコンを囲む円形HPリングゲージの表示を担当するビヘイビア
/// </summary>
public class B_HealthRingView : MonoBehaviour
{
    #region 定義
    [SerializeField] private Image              _ringImage;
    [SerializeField] private SO_HealthBarConfig _config;

    private Coroutine _lerpCoroutine;
    #endregion

    #region 公開メソッド
    /// <summary>
    /// HPの正規化済み割合（0〜1）でリングの充填量と色をアニメーション付きで更新します
    /// </summary>
    /// <param name="hpRatio">HP割合（0＝死亡, 1＝満タン）</param>
    public void UpdateRing(float hpRatio)
    {
        float targetFill = Mathf.Clamp01(hpRatio);
        ApplyColor(hpRatio);

        if (_lerpCoroutine != null)
            StopCoroutine(_lerpCoroutine);
        _lerpCoroutine = StartCoroutine(LerpFill(targetFill));
    }
    #endregion

    #region 非公開メソッド
    private void Awake()
    {
        if (_ringImage == null)
            _ringImage = GetComponent<Image>();

        if (_ringImage == null)
            Debug.LogError("[B_HealthRingView] Image コンポーネントが見つかりません");
    }

    private IEnumerator LerpFill(float targetFill)
    {
        float speed     = _config != null ? _config.fillAnimSpeed : 6f;
        float startFill = _ringImage.fillAmount;
        float elapsed   = 0f;
        float duration  = Mathf.Max(Mathf.Abs(targetFill - startFill) / speed, 0.016f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _ringImage.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / duration);
            yield return null;
        }

        _ringImage.fillAmount = targetFill;
        _lerpCoroutine = null;
    }

    private void ApplyColor(float ratio)
    {
        if (_ringImage == null || _config == null) return;

        if (ratio <= _config.redThreshold)
            _ringImage.color = _config.dangerColor;
        else if (ratio <= _config.yellowThreshold)
            _ringImage.color = _config.warningColor;
        else
            _ringImage.color = _config.healthyColor;
    }
    #endregion
}
