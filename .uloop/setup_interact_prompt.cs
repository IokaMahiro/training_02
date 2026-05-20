using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Reflection;

// --- ヘルパー ---
GameObject CreateUI(string name, Transform parent)
{
    var go = new GameObject(name, typeof(RectTransform));
    go.transform.SetParent(parent, false);
    return go;
}
RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();
void SetAnchored(GameObject go, float axMin, float ayMin, float axMax, float ayMax,
    float px, float py, float sx, float sy, float pivotX = 0.5f, float pivotY = 0.5f)
{
    var rt = RT(go);
    rt.anchorMin = new Vector2(axMin, ayMin); rt.anchorMax = new Vector2(axMax, ayMax);
    rt.pivot = new Vector2(pivotX, pivotY);
    rt.anchoredPosition = new Vector2(px, py); rt.sizeDelta = new Vector2(sx, sy);
}
void SetField(object obj, string name, object val)
{
    var f = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
    if (f != null) f.SetValue(obj, val);
    else Debug.LogWarning("[InteractSetup] field not found: " + name);
}

// =============================================================
// 1. SO_InteractableData アセット作成（サンプル）
// =============================================================
if (!AssetDatabase.IsValidFolder("Assets/Data"))
    AssetDatabase.CreateFolder("Assets", "Data");

const string dataPath = "Assets/Data/WoodenPlanks.asset";
var itemData = AssetDatabase.LoadAssetAtPath<SO_InteractableData>(dataPath);
if (itemData == null)
{
    itemData = ScriptableObject.CreateInstance<SO_InteractableData>();
    AssetDatabase.CreateAsset(itemData, dataPath);
    AssetDatabase.SaveAssets();
}

// =============================================================
// 2. HUD_Canvas に InteractPrompt ウィジェットを追加
// =============================================================
var canvasGO = GameObject.Find("HUD_Canvas");
if (canvasGO == null)
{
    Debug.LogError("[InteractSetup] HUD_Canvas が見つかりません");
    return null;
}

// すでに存在する場合は削除して再作成
var existing = canvasGO.transform.Find("InteractPrompt");
if (existing != null) GameObject.DestroyImmediate(existing.gameObject);

// PromptRoot（画面中央基準、LateUpdateで位置を上書き）
var promptRoot = CreateUI("InteractPrompt", canvasGO.transform);
SetAnchored(promptRoot, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 300f, 50f);
// HorizontalLayoutGroup（初期非表示は B_InteractPromptView.Start() で行う）
var hLayout = promptRoot.AddComponent<HorizontalLayoutGroup>();
hLayout.childAlignment = TextAnchor.MiddleLeft;
hLayout.spacing = 8f;
hLayout.childForceExpandWidth = false;
hLayout.childForceExpandHeight = false;
hLayout.padding = new RectOffset(8, 8, 4, 4);

// ContentSizeFitter（内容に応じて幅を自動調整）
var csf = promptRoot.AddComponent<ContentSizeFitter>();
csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

// 背景（半透明黒）
var bgImg = promptRoot.AddComponent<Image>();
bgImg.color = new Color(0f, 0f, 0f, 0.55f);

// ---------------------------------------------------------------
// ボタンアイコン（白丸 + テキスト）
// ---------------------------------------------------------------
var btnContainer = CreateUI("ButtonContainer", promptRoot.transform);
var btnContRT = RT(btnContainer);
btnContRT.sizeDelta = new Vector2(36f, 36f);
var btnContLE = btnContainer.AddComponent<LayoutElement>();
btnContLE.preferredWidth = 36f; btnContLE.preferredHeight = 36f;

// 丸背景
var btnBgImg = btnContainer.AddComponent<Image>();
btnBgImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
btnBgImg.color  = Color.white;

// ボタンラベル（"A"）
var btnLabelGO  = CreateUI("ButtonLabel", btnContainer.transform);
SetAnchored(btnLabelGO, 0f,0f, 1f,1f, 0f,0f, 0f,0f);
var btnLabelTMP = btnLabelGO.AddComponent<TextMeshProUGUI>();
btnLabelTMP.text      = "A";
btnLabelTMP.fontSize  = 20f;
btnLabelTMP.fontStyle = FontStyles.Bold;
btnLabelTMP.color     = Color.black;
btnLabelTMP.alignment = TextAlignmentOptions.Center;

// ---------------------------------------------------------------
// アイテムアイコン
// ---------------------------------------------------------------
var itemIconGO = CreateUI("ItemIcon", promptRoot.transform);
var itemIconLE = itemIconGO.AddComponent<LayoutElement>();
itemIconLE.preferredWidth = 32f; itemIconLE.preferredHeight = 32f;
var itemIconImg = itemIconGO.AddComponent<Image>();
itemIconImg.color = Color.white;
itemIconImg.preserveAspect = true;
itemIconGO.SetActive(false); // アイコン未設定時は非表示

// ---------------------------------------------------------------
// アイテム名テキスト
// ---------------------------------------------------------------
var itemLabelGO  = CreateUI("ItemLabel", promptRoot.transform);
var itemLabelTMP = itemLabelGO.AddComponent<TextMeshProUGUI>();
itemLabelTMP.text      = "Wooden Planks ×1";
itemLabelTMP.fontSize  = 18f;
itemLabelTMP.color     = Color.white;
itemLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;
var itemLabelLE = itemLabelGO.AddComponent<LayoutElement>();
itemLabelLE.preferredHeight = 36f;
// 幅は ContentSizeFitter に任せるので minWidth だけ設定
itemLabelLE.minWidth = 80f;

// =============================================================
// 3. B_InteractPromptView をアタッチして配線
// =============================================================
var promptView = promptRoot.AddComponent<B_InteractPromptView>();
SetField(promptView, "_promptRoot",      RT(promptRoot));
SetField(promptView, "_buttonIconImage", btnBgImg);
SetField(promptView, "_buttonLabel",     btnLabelTMP);
SetField(promptView, "_itemIconImage",   itemIconImg);
SetField(promptView, "_itemLabel",       itemLabelTMP);

// =============================================================
// 4. サンプルのインタラクタブルオブジェクトを配置
// =============================================================
var interactGO = GameObject.Find("SampleInteractable");
if (interactGO == null)
{
    interactGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
    interactGO.name = "SampleInteractable";
    interactGO.transform.position = new Vector3(2f, 0f, 0f);
    interactGO.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
}
var interactComp = interactGO.GetComponent<B_InteractableObject>()
                ?? interactGO.AddComponent<B_InteractableObject>();
SetField(interactComp, "_data",          itemData);
SetField(interactComp, "_interactRange", 3f);

// =============================================================
// 5. シーン保存
// =============================================================
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
Debug.Log("[InteractSetup] InteractPrompt セットアップ完了");
