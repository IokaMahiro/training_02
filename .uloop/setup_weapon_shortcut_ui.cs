using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Reflection;

// =====================================================================
// 武器ショートカットUI セットアップスクリプト（十字形レイアウト版）
// 上下左右 各2スロット、計8スロットを生成します
//
// スロット番号:
//   Up外(0) Up内(1) | Down内(2) Down外(3) | Left上(4) Left下(5) | Right上(6) Right下(7)
// =====================================================================

const float SLOT_W       = 210f;
const float SLOT_H       = 64f;
const float SLOT_GAP     = 6f;      // 同アーム内スロット間隔
const float ARM_GAP      = 12f;     // クロス中心からアーム開始までの空白
const float ICON_SIZE    = 52f;
const float FRAME_BORDER = 2.5f;

// クロスのアーム高さ・全体サイズ
float ARM_H   = 2f * SLOT_H + SLOT_GAP;                   // 134
float CROSS_W = 2f * (ARM_GAP + SLOT_W);                  // 444
float CROSS_H = 2f * (ARM_GAP + ARM_H);                   // 292

// =====================================================================
// ヘルパー
// =====================================================================
RectTransform MakeRect(string name, Transform parent)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    return go.AddComponent<RectTransform>();
}

Image MakeImage(string name, Transform parent, Color color)
{
    var rt  = MakeRect(name, parent);
    var img = rt.gameObject.AddComponent<Image>();
    img.color = color;
    return img;
}

// =====================================================================
// 1. Canvas を準備（既存があれば再利用）
// =====================================================================
var existingCanvas = GameObject.Find("WeaponShortcutCanvas");
if (existingCanvas != null)
    UnityEngine.Object.DestroyImmediate(existingCanvas);

var canvasGO = new GameObject("WeaponShortcutCanvas");
var canvas   = canvasGO.AddComponent<Canvas>();
canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 10;

var scaler = canvasGO.AddComponent<CanvasScaler>();
scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1920, 1080);
scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
scaler.matchWidthOrHeight  = 1f;

canvasGO.AddComponent<GraphicRaycaster>();

// =====================================================================
// 2. CrossPanel（十字全体の親、CanvasGroup でフェード管理）
// =====================================================================
var panelRT = MakeRect("CrossPanel", canvasGO.transform);
// pivot を中心にし、アームの位置計算を簡単にする
panelRT.anchorMin        = new Vector2(1f, 0.5f);
panelRT.anchorMax        = new Vector2(1f, 0.5f);
panelRT.pivot            = new Vector2(0.5f, 0.5f);
// 右端から 40px + パネル半幅 の位置にクロス中心を配置
panelRT.anchoredPosition = new Vector2(-(40f + CROSS_W * 0.5f), 0f);
panelRT.sizeDelta        = new Vector2(CROSS_W, CROSS_H);

var cg = panelRT.gameObject.AddComponent<CanvasGroup>();

// =====================================================================
// 3. アームを生成するローカル関数
//    armName  : GameObject名
//    pivotVec : アームのピボット（中心に向く端が基準）
//    aPos     : CrossPanel中心からのオフセット
//    isHoriz  : true=横向きアーム（Left/Right）はVerticalで積む
// =====================================================================
// ※ローカル関数はクロージャを避けてパラメータで値を受け取る

RectTransform MakeArm(string armName, Vector2 pivotVec, Vector2 aPos)
{
    var armRT = MakeRect(armName, panelRT);
    armRT.anchorMin        = new Vector2(0.5f, 0.5f);
    armRT.anchorMax        = new Vector2(0.5f, 0.5f);
    armRT.pivot            = pivotVec;
    armRT.anchoredPosition = aPos;
    armRT.sizeDelta        = new Vector2(SLOT_W, ARM_H);

    var vlg = armRT.gameObject.AddComponent<VerticalLayoutGroup>();
    vlg.childAlignment         = TextAnchor.UpperCenter;
    vlg.spacing                = SLOT_GAP;
    vlg.childControlWidth      = true;
    vlg.childControlHeight     = true;
    vlg.childForceExpandWidth  = true;
    vlg.childForceExpandHeight = false;

    return armRT;
}

// =====================================================================
// 4. 全スロットを格納する配列（index 0〜7）
//    0,1=Up  2,3=Down  4,5=Left  6,7=Right
// =====================================================================
var slotViews = new B_WeaponShortcutSlotView[8];

// アーム定義: (名前, pivot, anchoredPosition)
// Up   : pivot底辺=(0.5,0), 中心からARM_GAP上
// Down : pivot上辺=(0.5,1), 中心からARM_GAP下
// Left : pivot右辺=(1.0,0.5), 中心からARM_GAP左
// Right: pivot左辺=(0.0,0.5), 中心からARM_GAP右
string[] armNames  = { "Arm_Up", "Arm_Down", "Arm_Left", "Arm_Right" };
Vector2[] armPivots = {
    new Vector2(0.5f, 0f),   // Up
    new Vector2(0.5f, 1f),   // Down
    new Vector2(1f,   0.5f), // Left
    new Vector2(0f,   0.5f), // Right
};
Vector2[] armOffsets = {
    new Vector2(0f,       ARM_GAP),  // Up
    new Vector2(0f,      -ARM_GAP),  // Down
    new Vector2(-ARM_GAP, 0f),       // Left
    new Vector2(ARM_GAP,  0f),       // Right
};

int slotIndex = 0;

for (int armIdx = 0; armIdx < 4; armIdx++)
{
    var armRT = MakeArm(armNames[armIdx], armPivots[armIdx], armOffsets[armIdx]);

    // このアームに2スロットを生成
    for (int s = 0; s < 2; s++)
    {
        string slotName = $"Slot_{slotIndex}";
        var slotRT = MakeRect(slotName, armRT);
        slotRT.sizeDelta = new Vector2(SLOT_W, SLOT_H);

        var le = slotRT.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = SLOT_H;
        le.minHeight       = SLOT_H;

        // --- 背景 ---
        var bg = MakeImage("Background", slotRT, new Color(0.04f, 0.04f, 0.04f, 0.82f));
        bg.rectTransform.anchorMin        = Vector2.zero;
        bg.rectTransform.anchorMax        = Vector2.one;
        bg.rectTransform.sizeDelta        = Vector2.zero;
        bg.rectTransform.anchoredPosition = Vector2.zero;

        // --- 選択枠（4本のボーダーライン）---
        var sfGO = new GameObject("SelectionFrame");
        sfGO.transform.SetParent(slotRT, false);
        var sfRT = sfGO.AddComponent<RectTransform>();
        sfRT.anchorMin        = Vector2.zero;
        sfRT.anchorMax        = Vector2.one;
        sfRT.sizeDelta        = Vector2.zero;
        sfRT.anchoredPosition = Vector2.zero;
        var sfImg = sfGO.AddComponent<Image>();
        sfImg.color = new Color(1f, 1f, 1f, 0f); // 透明ベース

        // Border_T
        var bT = MakeImage("Border_T", sfGO.transform, Color.white);
        bT.rectTransform.anchorMin = new Vector2(0, 1); bT.rectTransform.anchorMax = new Vector2(1, 1);
        bT.rectTransform.pivot = new Vector2(0.5f, 1f); bT.rectTransform.anchoredPosition = Vector2.zero;
        bT.rectTransform.sizeDelta = new Vector2(0, FRAME_BORDER);

        // Border_B
        var bB = MakeImage("Border_B", sfGO.transform, Color.white);
        bB.rectTransform.anchorMin = new Vector2(0, 0); bB.rectTransform.anchorMax = new Vector2(1, 0);
        bB.rectTransform.pivot = new Vector2(0.5f, 0f); bB.rectTransform.anchoredPosition = Vector2.zero;
        bB.rectTransform.sizeDelta = new Vector2(0, FRAME_BORDER);

        // Border_L
        var bL = MakeImage("Border_L", sfGO.transform, Color.white);
        bL.rectTransform.anchorMin = new Vector2(0, 0); bL.rectTransform.anchorMax = new Vector2(0, 1);
        bL.rectTransform.pivot = new Vector2(0f, 0.5f); bL.rectTransform.anchoredPosition = Vector2.zero;
        bL.rectTransform.sizeDelta = new Vector2(FRAME_BORDER, 0);

        // Border_R
        var bR = MakeImage("Border_R", sfGO.transform, Color.white);
        bR.rectTransform.anchorMin = new Vector2(1, 0); bR.rectTransform.anchorMax = new Vector2(1, 1);
        bR.rectTransform.pivot = new Vector2(1f, 0.5f); bR.rectTransform.anchoredPosition = Vector2.zero;
        bR.rectTransform.sizeDelta = new Vector2(FRAME_BORDER, 0);

        // 初期枠アルファ（Slot_0 のみ選択済み白枠）
        float borderAlpha = (slotIndex == 0) ? 1f : 0.12f;
        bT.color = new Color(1, 1, 1, borderAlpha);
        bB.color = new Color(1, 1, 1, borderAlpha);
        bL.color = new Color(1, 1, 1, borderAlpha);
        bR.color = new Color(1, 1, 1, borderAlpha);

        // --- 武器アイコン ---
        var iconImg = MakeImage("WeaponIcon", slotRT, Color.white);
        iconImg.rectTransform.anchorMin        = new Vector2(0, 0.5f);
        iconImg.rectTransform.anchorMax        = new Vector2(0, 0.5f);
        iconImg.rectTransform.pivot            = new Vector2(0, 0.5f);
        iconImg.rectTransform.anchoredPosition = new Vector2(8f, 0);
        iconImg.rectTransform.sizeDelta        = new Vector2(ICON_SIZE, ICON_SIZE);
        iconImg.preserveAspect                 = true;

        // --- 弾薬テキスト ---
        var ammoRT = MakeRect("AmmoText", slotRT);
        ammoRT.anchorMin        = new Vector2(0, 0.5f);
        ammoRT.anchorMax        = new Vector2(1, 0.5f);
        ammoRT.pivot            = new Vector2(1, 0.5f);
        ammoRT.anchoredPosition = new Vector2(-10f, 0);
        ammoRT.sizeDelta        = new Vector2(0, SLOT_H);
        var ammoTMP = ammoRT.gameObject.AddComponent<TextMeshProUGUI>();
        ammoTMP.alignment = TextAlignmentOptions.MidlineRight;
        ammoTMP.fontSize  = 20f;
        ammoTMP.color     = Color.white;
        ammoTMP.richText  = true;
        ammoTMP.text      = "--";

        // --- EmptyOverlay ---
        var emptyGO = new GameObject("EmptyOverlay");
        emptyGO.transform.SetParent(slotRT, false);
        var emptyRT = emptyGO.AddComponent<RectTransform>();
        emptyRT.anchorMin        = Vector2.zero;
        emptyRT.anchorMax        = Vector2.one;
        emptyRT.sizeDelta        = Vector2.zero;
        emptyRT.anchoredPosition = Vector2.zero;
        emptyGO.SetActive(true);

        // --- B_WeaponShortcutSlotView をアタッチ ---
        var slotView = slotRT.gameObject.AddComponent<B_WeaponShortcutSlotView>();
        var slotType = typeof(B_WeaponShortcutSlotView);

        var fi_icon  = slotType.GetField("_weaponIcon",         BindingFlags.NonPublic | BindingFlags.Instance);
        var fi_ammo  = slotType.GetField("_ammoText",           BindingFlags.NonPublic | BindingFlags.Instance);
        var fi_frame = slotType.GetField("_selectionFrameRoot", BindingFlags.NonPublic | BindingFlags.Instance);
        var fi_empty = slotType.GetField("_emptyOverlay",       BindingFlags.NonPublic | BindingFlags.Instance);

        if (fi_icon  != null) fi_icon .SetValue(slotView, iconImg);
        if (fi_ammo  != null) fi_ammo .SetValue(slotView, ammoTMP);
        if (fi_frame != null) fi_frame.SetValue(slotView, sfRT);
        if (fi_empty != null) fi_empty.SetValue(slotView, emptyGO);

        slotViews[slotIndex] = slotView;
        slotIndex++;
    }
}

// =====================================================================
// 5. B_WeaponShortcutUI をアタッチして参照をセット
// =====================================================================
var uiComp = canvasGO.AddComponent<B_WeaponShortcutUI>();
var uiType = typeof(B_WeaponShortcutUI);

var sf = uiType.GetField("_slotViews",      BindingFlags.NonPublic | BindingFlags.Instance);
var cf = uiType.GetField("_rootCanvasGroup", BindingFlags.NonPublic | BindingFlags.Instance);
if (sf != null) sf.SetValue(uiComp, slotViews);
if (cf != null) cf.SetValue(uiComp, cg);

// =====================================================================
// 6. 初期非表示に設定
// =====================================================================
cg.alpha          = 0f;
cg.interactable   = false;
cg.blocksRaycasts = false;

// =====================================================================
// 7. 完了
// =====================================================================
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
Debug.Log("[WeaponShortcutSetup] 完了 — 8スロット十字形レイアウト生成 (Tab:開閉 / WASD:方向選択 / 同方向再押し:内外切替 / Enter:確定)");
