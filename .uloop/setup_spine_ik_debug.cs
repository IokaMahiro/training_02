using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;

void SetField(object obj, string name, object val)
{
    var f = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
    if (f != null) f.SetValue(obj, val);
    else Debug.LogWarning("[SpineIKSetup] field not found: " + name);
}

// =============================================================
// 1. Player（Capsule）を準備
// =============================================================
var playerGO = GameObject.Find("Player");
if (playerGO == null)
{
    playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
    playerGO.name = "Player";
}
playerGO.transform.position = new Vector3(0f, 1f, 0f);

// =============================================================
// 2. 仮リグ：Spine1 / Spine2 / Spine3 を親子で積み上げ
// =============================================================
string[] jointNames = { "Spine1", "Spine2", "Spine3" };
float[]  jointRatios = { 0.4f, 0.35f, 0.25f }; // 下から上。合計 1.0
float[]  yOffsets    = { 0.1f, 0.3f, 0.5f };    // Player ローカル Y

var jointTransforms = new Transform[jointNames.Length];
Transform parent = playerGO.transform;

for (int i = 0; i < jointNames.Length; i++)
{
    var existing = playerGO.transform.Find(jointNames[i]);
    var go = existing != null ? existing.gameObject : null;

    if (go == null)
    {
        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = jointNames[i];
        go.transform.SetParent(parent, false);
    }

    go.transform.localPosition = new Vector3(0f, yOffsets[i], 0f);
    go.transform.localScale    = new Vector3(0.55f - i * 0.05f, 0.15f, 0.28f);

    // ジョイントを色分けして可視化
    var renderer = go.GetComponent<Renderer>();
    if (renderer != null)
    {
        var mat = new Material(Shader.Find("Standard"));
        float t = (float)i / (jointNames.Length - 1);
        mat.color = Color.Lerp(new Color(0.2f, 0.6f, 1f), new Color(1f, 0.4f, 0.2f), t);
        renderer.material = mat;
    }

    jointTransforms[i] = go.transform;
    parent = go.transform; // 次のジョイントはこの子に
}

// =============================================================
// 3. B_SpinePitchIK を Player にアタッチ・設定
// =============================================================
var ikComp = playerGO.GetComponent<B_SpinePitchIK>()
          ?? playerGO.AddComponent<B_SpinePitchIK>();

// JointEntry 配列をリフレクションで設定
var entryType = typeof(B_SpinePitchIK).GetNestedType("JointEntry");
var arr = System.Array.CreateInstance(entryType, jointNames.Length);
for (int i = 0; i < jointNames.Length; i++)
{
    var entry = System.Activator.CreateInstance(entryType);
    entryType.GetField("joint").SetValue(entry, jointTransforms[i]);
    entryType.GetField("ratio").SetValue(entry, jointRatios[i]);
    arr.SetValue(entry, i);
}
SetField(ikComp, "_joints", arr);
SetField(ikComp, "_cameraTransform", Camera.main?.transform);
SetField(ikComp, "_weight",          0.5f);
SetField(ikComp, "_pitchLimit",      60f);
SetField(ikComp, "_smoothSpeed",     8f);

// =============================================================
// 4. カメラのセットアップ
// =============================================================
var camGO = Camera.main?.gameObject;
if (camGO == null)
{
    camGO = new GameObject("Main Camera");
    camGO.AddComponent<Camera>();
    camGO.tag = "MainCamera";
}
camGO.transform.position = new Vector3(0f, 1.5f, -4f);
camGO.transform.LookAt(playerGO.transform.position + Vector3.up);

var pitchDebug = camGO.GetComponent<B_DebugCameraPitch>()
              ?? camGO.AddComponent<B_DebugCameraPitch>();

// =============================================================
// 5. 完了
// =============================================================
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
Debug.Log("[SpineIKSetup] 完了 — Spine1/2/3 を配置、IK をアタッチしました");
