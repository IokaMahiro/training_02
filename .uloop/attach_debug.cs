using UnityEngine;
using System.Reflection;

void SetField(object obj, string name, object val)
{
    var f = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
    if (f != null) f.SetValue(obj, val);
    else Debug.LogWarning("[DebugSetup] field not found: " + name);
}

var playerGO = GameObject.Find("Player");
if (playerGO == null)
{
    Debug.LogError("[DebugSetup] Player not found");
    return null;
}

var health = playerGO.GetComponent<B_PlayerHealth>();
var weapon = playerGO.GetComponent<B_PlayerWeapon>();
var dbg    = playerGO.GetComponent<B_DebugWeaponHUD>() ?? playerGO.AddComponent<B_DebugWeaponHUD>();

SetField(dbg, "_playerHealth", health);
SetField(dbg, "_playerWeapon", weapon);

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

Debug.Log("[DebugSetup] B_DebugWeaponHUD をアタッチしました");
