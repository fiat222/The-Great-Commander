using UnityEngine;
using UnityEditor;
using Unity.Netcode;

/// <summary>
/// Editor Script สำหรับเตรียม GameScene ให้ใช้ Local Player Spawner
/// เมนู: Tools → Character Select
/// </summary>
public class PlayerPrefabSetup : Editor
{
    [MenuItem("Tools/Character Select/4 — Revert Player Prefabs (Remove Network Components)", false, 40)]
    static void RevertPlayerPrefabs()
    {
        string[] paths = new string[]
        {
            "Assets/Prefabs/Player/Archer/Archer.prefab",
            "Assets/Prefabs/Player/NewWarrior/PlayerWarrior.prefab",
            "Assets/Prefabs/Player/Warrior/Warrior.prefab",
        };

        int modified = 0;
        foreach (string path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                bool changed = false;



                // ลบ NetworkObject (ถ้ามี — Player ไม่ต้อง Spawn ผ่าน NGO แล้ว)
                var no = root.GetComponent<NetworkObject>();
                if (no != null) { DestroyImmediate(no); changed = true; }

                if (changed)
                {
                    modified++;
                    Debug.Log($"<color=lime>[PrefabSetup]</color> ✅ ลบ Network Components จาก '{prefab.name}'");
                }
                else
                {
                    Debug.Log($"<color=cyan>[PrefabSetup]</color> '{prefab.name}' สะอาดอยู่แล้ว → ข้าม");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=lime>[PrefabSetup]</color> เสร็จ! แก้ไข {modified} Prefabs");
    }

    [MenuItem("Tools/Character Select/5 — Create LocalPlayerSpawner in GameScene", false, 50)]
    static void CreateLocalPlayerSpawner()
    {
        // ลบตัวเก่า
        var old = GameObject.Find("LocalPlayerSpawner");
        if (old != null) DestroyImmediate(old);
        var oldMgr = GameObject.Find("PlayerSpawnManager");
        if (oldMgr != null) DestroyImmediate(oldMgr);

        // สร้าง LocalPlayerSpawner (ไม่ต้อง NetworkObject!)
        var go = new GameObject("LocalPlayerSpawner");
        var spawner = go.AddComponent<LocalPlayerSpawner>();

        // Fallback Prefab
        var archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Archer/Archer.prefab");
        if (archerPrefab != null)
        {
            spawner.fallbackPrefab = archerPrefab;
            Debug.Log("<color=lime>[PrefabSetup]</color> ✅ Fallback = Archer");
        }

        // Characters array (สำหรับ debug mode)
        var warrior = AssetDatabase.LoadAssetAtPath<CharacterDataSO>("Assets/SO/Character/WarriorCharacter.asset");
        var archer = AssetDatabase.LoadAssetAtPath<CharacterDataSO>("Assets/SO/Character/ArcherCharacter.asset");
        if (warrior != null && archer != null)
        {
            spawner.characters = new CharacterDataSO[] { warrior, archer };
            Debug.Log("<color=lime>[PrefabSetup]</color> ✅ ใส่ Characters array สำหรับ debug mode");
        }

        // SpawnPoints
        var oldP1 = GameObject.Find("P1SpawnPoint");
        if (oldP1 != null) DestroyImmediate(oldP1);
        var oldP2 = GameObject.Find("P2SpawnPoint");
        if (oldP2 != null) DestroyImmediate(oldP2);

        var p1 = new GameObject("P1SpawnPoint");
        p1.transform.position = new Vector3(-15f, 1f, 0f);

        var p2 = new GameObject("P2SpawnPoint");
        p2.transform.position = new Vector3(15f, 1f, 0f);
        p2.transform.rotation = Quaternion.Euler(0, 180, 0);

        spawner.p1SpawnPoint = p1.transform;
        spawner.p2SpawnPoint = p2.transform;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("<color=lime>[PrefabSetup]</color> ✅ สร้าง LocalPlayerSpawner + SpawnPoints เรียบร้อย!");
        Debug.Log("<color=yellow>[PrefabSetup]</color> 📝 ย้าย P1SpawnPoint / P2SpawnPoint ไปตำแหน่งที่เหมาะสม!");

        EditorGUIUtility.PingObject(go);
        Selection.activeGameObject = go;
    }
}
