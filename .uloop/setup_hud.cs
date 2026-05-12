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

void SetField(object obj, string fieldName, object value)
{
    var f = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
    if (f != null) f.SetValue(obj, value);
    else Debug.LogWarning($"[HUD Setup] フィールド未発見: {fieldName}");
}

RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();

void SetAnchored(GameObject go,
    float axMin, float ayMin, float axMax, float ayMax,
    float px, float py, float sx, float sy,
    float pivotX = 0.5f, float pivotY = 0.5f)
{
    var rt = RT(go);
    rt.anchorMin        = new Vector2(axMin, ayMin);
    rt.anchorMax        = new Vector2(axMax, ayMax);
    rt.pivot            = new Vector2(pivotX, pivotY);
    rt.anchoredPosition = new Vector2(px, py);
    rt.sizeDelta        = new Vector2(sx, sy);
}

Sprite CircleSprite() => Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

// =============================================================
// 1. SO_HealthBarConfig アセット作成
// =============================================================
if (!AssetDatabase.IsValidFolder("Assets/Data"))
    AssetDatabase.CreateFolder("Assets", "Data");

const string configPath = "Assets/Data/HealthBarConfig.asset";
var config = AssetDatabase.LoadAssetAtPath<SO_HealthBarConfig>(configPath);
if (config == null)
{
    config = ScriptableObject.CreateInstance<SO_HealthBarConfig>();
    AssetDatabase.CreateAsset(config, configPath);
    AssetDatabase.SaveAssets();
}

// =============================================================
// 2. Canvas
// =============================================================
var canvasGO = new GameObject("HUD_Canvas");
var canvas   = canvasGO.AddComponent<Canvas>();
canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 10;

var scaler = canvasGO.AddComponent<CanvasScaler>();
scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1920, 1080);
scaler.matchWidthOrHeight  = 0.5f;
canvasGO.AddComponent<GraphicRaycaster>();

// =============================================================
// 3. WeaponWidget（右下アンカー）
// =============================================================
var widgetGO = CreateUI("WeaponWidget", canvasGO.transform);
SetAnchored(widgetGO, 1,0, 1,0, -30,30, 160,230, 1,0);

// =============================================================
// 4. SubWeaponSlot（ウィジェット上端）
// =============================================================
var subSlotGO = CreateUI("SubWeaponSlot", widgetGO.transform);
SetAnchored(subSlotGO, 0.5f,1, 0.5f,1, 0,0, 160,55, 0.5f,1);

// SubWeaponIcon
var subIconGO    = CreateUI("SubWeaponIcon", subSlotGO.transform);
SetAnchored(subIconGO, 0.5f,0.5f, 0.5f,0.5f, 0,12, 60,30);
var subIconImg   = subIconGO.AddComponent<Image>();
subIconImg.sprite         = CircleSprite();
subIconImg.color          = new Color(1,1,1,0.85f);
subIconImg.preserveAspect = true;
subIconGO.SetActive(false);

// Divider（区切り線）
var dividerGO  = CreateUI("Divider", subSlotGO.transform);
SetAnchored(dividerGO, 0.5f,0, 0.5f,0, 0,0, 110,2, 0.5f,0);
var dividerImg = dividerGO.AddComponent<Image>();
dividerImg.color = new Color(0.85f,0.85f,0.85f,0.55f);
dividerGO.SetActive(false);

// =============================================================
// 5. RingContainer（SubWeaponSlotの直下）
// =============================================================
var ringContGO = CreateUI("RingContainer", widgetGO.transform);
SetAnchored(ringContGO, 0.5f,1, 0.5f,1, 0,-55, 130,130, 0.5f,1);

// RingBackground（暗色の円）
var ringBgGO  = CreateUI("RingBackground", ringContGO.transform);
SetAnchored(ringBgGO, 0,0, 1,1, 0,0, 0,0);
var ringBgImg = ringBgGO.AddComponent<Image>();
ringBgImg.sprite = CircleSprite();
ringBgImg.color  = new Color(0.05f,0.05f,0.05f,0.8f);
ringBgImg.type   = Image.Type.Simple;

// HPRing（Radial360 Fill）
var hpRingGO  = CreateUI("HPRing", ringContGO.transform);
SetAnchored(hpRingGO, 0,0, 1,1, 0,0, 0,0);
var hpRingImg = hpRingGO.AddComponent<Image>();
hpRingImg.sprite       = CircleSprite();
hpRingImg.color        = new Color(0.4f,1f,0.4f,1f);
hpRingImg.type         = Image.Type.Filled;
hpRingImg.fillMethod   = Image.FillMethod.Radial360;
hpRingImg.fillOrigin   = (int)Image.Origin360.Top;
hpRingImg.fillClockwise = true;
hpRingImg.fillAmount   = 1f;

// RingCenter（内側を隠してドーナツ形状を演出）
var ringCenterGO  = CreateUI("RingCenter", ringContGO.transform);
SetAnchored(ringCenterGO, 0.5f,0.5f, 0.5f,0.5f, 0,0, 95,95);
var ringCenterImg = ringCenterGO.AddComponent<Image>();
ringCenterImg.sprite = CircleSprite();
ringCenterImg.color  = new Color(0.06f,0.06f,0.06f,0.85f);

// WeaponIcon（リング中央）
var weaponIconGO  = CreateUI("WeaponIcon", ringContGO.transform);
SetAnchored(weaponIconGO, 0.5f,0.5f, 0.5f,0.5f, 0,0, 75,45);
var weaponIconImg = weaponIconGO.AddComponent<Image>();
weaponIconImg.color          = Color.white;
weaponIconImg.preserveAspect = true;

// =============================================================
// 6. AmmoContainer（ウィジェット下端）
// =============================================================
var ammoContGO = CreateUI("AmmoContainer", widgetGO.transform);
SetAnchored(ammoContGO, 0.5f,0, 0.5f,0, 0,0, 160,42, 0.5f,0);

var hLayout = ammoContGO.AddComponent<HorizontalLayoutGroup>();
hLayout.childAlignment       = TextAnchor.MiddleCenter;
hLayout.spacing              = 4;
hLayout.childForceExpandWidth  = false;
hLayout.childForceExpandHeight = false;
hLayout.padding = new RectOffset(0,0,0,0);

// CurrentAmmo
var curAmmoGO  = CreateUI("CurrentAmmo", ammoContGO.transform);
var curAmmoTMP = curAmmoGO.AddComponent<TextMeshProUGUI>();
curAmmoTMP.text      = "16";
curAmmoTMP.fontSize  = 30;
curAmmoTMP.fontStyle = FontStyles.Bold;
curAmmoTMP.color     = Color.white;
curAmmoTMP.alignment = TextAlignmentOptions.Right;
var curLE = curAmmoGO.AddComponent<LayoutElement>();
curLE.preferredWidth = 56; curLE.preferredHeight = 40;

// Slash
var slashGO  = CreateUI("Slash", ammoContGO.transform);
var slashTMP = slashGO.AddComponent<TextMeshProUGUI>();
slashTMP.text      = "/";
slashTMP.fontSize  = 20;
slashTMP.color     = new Color(0.65f,0.65f,0.65f,1f);
slashTMP.alignment = TextAlignmentOptions.Center;
var slashLE = slashGO.AddComponent<LayoutElement>();
slashLE.preferredWidth = 16; slashLE.preferredHeight = 40;

// ReserveAmmo
var resAmmoGO  = CreateUI("ReserveAmmo", ammoContGO.transform);
var resAmmoTMP = resAmmoGO.AddComponent<TextMeshProUGUI>();
resAmmoTMP.text      = "0";
resAmmoTMP.fontSize  = 20;
resAmmoTMP.color     = new Color(0.65f,0.65f,0.65f,1f);
resAmmoTMP.alignment = TextAlignmentOptions.Left;
var resLE = resAmmoGO.AddComponent<LayoutElement>();
resLE.preferredWidth = 50; resLE.preferredHeight = 40;

// =============================================================
// 7. Player GameObject
// =============================================================
var playerGO     = new GameObject("Player");
var playerHealth = playerGO.AddComponent<B_PlayerHealth>();
var playerWeapon = playerGO.AddComponent<B_PlayerWeapon>();

// =============================================================
// 8. HUD コンポーネントをアタッチ
// =============================================================
var healthRingView = hpRingGO.AddComponent<B_HealthRingView>();
var ammoView       = ammoContGO.AddComponent<B_AmmoView>();
var subSlotView    = subSlotGO.AddComponent<B_SubWeaponSlotView>();
var hudView        = widgetGO.AddComponent<B_WeaponHUDView>();

// =============================================================
// 9. Reflection でシリアライズフィールドを配線
// =============================================================
SetField(healthRingView, "_ringImage", hpRingImg);
SetField(healthRingView, "_config",    config);

SetField(ammoView, "_currentAmmoText", curAmmoTMP);
SetField(ammoView, "_reserveAmmoText", resAmmoTMP);

SetField(subSlotView, "_subWeaponIcon", subIconImg);
SetField(subSlotView, "_divider",       dividerGO);

SetField(hudView, "_healthRing",    healthRingView);
SetField(hudView, "_ammoView",      ammoView);
SetField(hudView, "_subWeaponSlot", subSlotView);
SetField(hudView, "_mainWeaponIcon", weaponIconImg);
SetField(hudView, "_playerHealth",  playerHealth);
SetField(hudView, "_playerWeapon",  playerWeapon);

// =============================================================
// 10. シーンをダーティにして変更を保持
// =============================================================
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
Debug.Log("[HUD Setup] RE4 WeaponHUD の配置が完了しました");
