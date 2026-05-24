using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;

// =============================================================
// ユーティリティ
// =============================================================
void SetField(object obj, string name, object val)
{
    var f = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
    if (f != null) f.SetValue(obj, val);
    else Debug.LogWarning("[ArmIKSetup] field not found: " + name);
}

GameObject MakeCube(string name, Transform parent, Vector3 localPos, Vector3 localScale, Color color)
{
    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.name = name;
    go.transform.SetParent(parent, false);
    go.transform.localPosition = localPos;
    go.transform.localScale    = localScale;
    var mat = new Material(Shader.Find("Standard"));
    mat.color = color;
    go.GetComponent<Renderer>().sharedMaterial = mat;
    // Collider は視覚のみなので削除
    UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());
    return go;
}

GameObject MakeEmpty(string name, Transform parent, Vector3 localPos)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.localPosition = localPos;
    return go;
}

// =============================================================
// 1. Player を準備
// =============================================================
var playerGO = GameObject.Find("Player");
if (playerGO == null)
{
    playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
    playerGO.name = "Player";
}
playerGO.transform.position = Vector3.zero;

// =============================================================
// 2. 仮腕リグ構築
//    ArmRoot（肩基点）→ UpperArm（上腕ピボット）→ ForeArm（肘ピボット）→ Hand（手首ピボット）
//    腕は Player の正面（+Z）方向に伸びる
// =============================================================
// 既存の ArmDebugRoot があれば削除して再作成
var oldRoot = playerGO.transform.Find("ArmDebugRoot");
if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);

// ArmRoot（右肩付近）
var armRootGO = MakeEmpty("ArmDebugRoot", playerGO.transform, new Vector3(0.3f, 0.5f, 0f));

// UpperArm（pivot = 肩、ビジュアルは前方 +Z 方向へ延びる）
var upperArmGO = MakeEmpty("UpperArm", armRootGO.transform, Vector3.zero);
MakeCube("UpperArm_Visual", upperArmGO.transform,
    new Vector3(0f, 0f, 0.15f),      // 中心を前方へ
    new Vector3(0.08f, 0.08f, 0.3f), // 長さ 0.3m
    new Color(0.2f, 0.5f, 1f));       // 青系

// ForeArm（pivot = 肘、UpperArm の先端）
var foreArmGO = MakeEmpty("ForeArm", upperArmGO.transform, new Vector3(0f, 0f, 0.3f));
MakeCube("ForeArm_Visual", foreArmGO.transform,
    new Vector3(0f, 0f, 0.14f),
    new Vector3(0.07f, 0.07f, 0.28f),
    new Color(0.3f, 0.8f, 0.4f));     // 緑系

// Hand（pivot = 手首、ForeArm の先端）
var handGO = MakeEmpty("Hand", foreArmGO.transform, new Vector3(0f, 0f, 0.28f));
MakeCube("Hand_Visual", handGO.transform,
    new Vector3(0f, 0f, 0.05f),
    new Vector3(0.1f, 0.07f, 0.1f),
    new Color(1f, 0.7f, 0.2f));       // オレンジ系

// =============================================================
// 3. 壁（障害物）を配置
//    肩基点（0.3, 0.5, 0）から前方 0.35m、腕リーチ（0.579m）以内に配置
//    壁中心 Z=0.35 、厚さ 0.3m → 前面 Z=0.20、後面 Z=0.50
//    Raycast は肩→手先を飛ばすので壁前面（Z≈0.20）でヒット
// =============================================================
var oldWall = GameObject.Find("DebugWall");
if (oldWall != null) UnityEngine.Object.DestroyImmediate(oldWall);

var wallGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
wallGO.name = "DebugWall";
wallGO.transform.position   = new Vector3(0.3f, 0.5f, 0.35f); // 肩から 0.35m 先（腕リーチ 0.579m 以内）
wallGO.transform.localScale = new Vector3(1.5f, 2f, 0.3f);    // 厚め（0.3m）で確実に Raycast がヒット
var wallMat = new Material(Shader.Find("Standard"));
wallMat.color = new Color(0.6f, 0.4f, 0.3f, 0.8f); // 茶色
wallGO.GetComponent<Renderer>().sharedMaterial = wallMat;
// 壁は Default レイヤー（Raycast で検知）

// =============================================================
// 4. リーチターゲット（腕が目指す点）
//    壁の向こう側、肩から前方 2.0m（肩と同じ高さ）
//    これにより B_DebugArmPose が腕を前方へ真っすぐ伸ばし、
//    壁（Z=0.20～0.50）に確実に Raycast がヒットする
// =============================================================
var oldTarget = GameObject.Find("ArmReachTarget");
if (oldTarget != null) UnityEngine.Object.DestroyImmediate(oldTarget);

var targetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
targetGO.name = "ArmReachTarget";
targetGO.transform.position   = new Vector3(0.3f, 0.5f, 2.0f); // 壁の向こう側（肩と同じ高さ、前方 2m）
targetGO.transform.localScale = Vector3.one * 0.08f;
UnityEngine.Object.DestroyImmediate(targetGO.GetComponent<SphereCollider>());
var targetMat = new Material(Shader.Find("Standard"));
targetMat.color = Color.red;
targetMat.EnableKeyword("_EMISSION");
targetMat.SetColor("_EmissionColor", Color.red * 0.5f);
targetGO.GetComponent<Renderer>().sharedMaterial = targetMat;

// =============================================================
// 5. B_ArmObstacleIK をアタッチ・設定
// =============================================================
var ikComp = playerGO.GetComponent<B_ArmObstacleIK>()
          ?? playerGO.AddComponent<B_ArmObstacleIK>();

var armEntryType = typeof(B_ArmObstacleIK).GetNestedType("ArmSetting");
var armArr = System.Array.CreateInstance(armEntryType, 1);
var entry = System.Activator.CreateInstance(armEntryType);
armEntryType.GetField("upperArm").SetValue(entry,       upperArmGO.transform);
armEntryType.GetField("foreArm").SetValue(entry,        foreArmGO.transform);
armEntryType.GetField("hand").SetValue(entry,           handGO.transform);
armEntryType.GetField("raycastOrigin").SetValue(entry,  armRootGO.transform);
armEntryType.GetField("obstacleMask").SetValue(entry,   (LayerMask)(1 << 0)); // Default レイヤー
armEntryType.GetField("wallOffset").SetValue(entry,     0.08f);
armEntryType.GetField("maxWeight").SetValue(entry,      1f);
armArr.SetValue(entry, 0);
SetField(ikComp, "_arms",           armArr);
SetField(ikComp, "_weightUpSpeed",   12f);
SetField(ikComp, "_weightDownSpeed",  6f);
SetField(ikComp, "_positionSpeed",   10f);

// =============================================================
// 6. B_DebugArmPose をアタッチ・設定
// =============================================================
var poseComp = playerGO.GetComponent<B_DebugArmPose>()
            ?? playerGO.AddComponent<B_DebugArmPose>();
SetField(poseComp, "_armRoot",     armRootGO.transform);
SetField(poseComp, "_upperArm",    upperArmGO.transform);
SetField(poseComp, "_foreArm",     foreArmGO.transform);
SetField(poseComp, "_hand",        handGO.transform);
SetField(poseComp, "_reachTarget", targetGO.transform);
SetField(poseComp, "_obstacleIK",  ikComp);
SetField(poseComp, "_armIndex",    0);
SetField(poseComp, "_poleDir",     Vector3.down); // 肘が下方向に曲がる

// =============================================================
// 7. カメラ位置を調整（腕が見やすい角度）
// =============================================================
var cam = Camera.main;
if (cam != null)
{
    cam.transform.position = new Vector3(1.2f, 1.8f, -1f);
    cam.transform.LookAt(armRootGO.transform.position + Vector3.forward * 0.5f);
}

// =============================================================
// 8. 完了
// =============================================================
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
Debug.Log("[ArmIKSetup] 完了 — ArmRoot/UpperArm/ForeArm/Hand + 壁 + ターゲットを配置しました");
