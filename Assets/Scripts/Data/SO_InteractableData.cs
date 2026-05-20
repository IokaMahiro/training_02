using UnityEngine;

/// <summary>
/// インタラクト可能オブジェクトの表示データを保持する ScriptableObject。
/// 派生クラスでフィールドを追加することで、アイテム・ドア・NPC など
/// 種類ごとのデータを表現できます。
/// </summary>
[CreateAssetMenu(fileName = "InteractableData", menuName = "RE4HUD/InteractableData")]
public class SO_InteractableData : ScriptableObject
{
    #region 定義
    [Header("アイテム情報")]
    public string itemName  = "Item";
    public Sprite itemIcon;
    public int    count     = 1;

    [Header("ボタン表示")]
    public string buttonLabel = "A";
    public Sprite buttonIcon;

    [Header("表示位置オフセット（ワールド）")]
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    #endregion
}
