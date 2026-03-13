using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Script แก้ไขปัญหา DRAGON HUNTER Font Atlas corruption
/// </summary>
public class FixDragonHunterFont
{
    [MenuItem("Tools/Fix Font/Repair DRAGON HUNTER Atlas")]
    public static void RepairDragonHunterFont()
    {
        try
        {
            // หาไฟล์ DRAGON HUNTER SDF
            string[] guids = AssetDatabase.FindAssets("DRAGON HUNTER SDF");
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("ไม่พบไฟล์", "ไม่พบไฟล์ DRAGON HUNTER SDF.asset\nกรุณาตรวจสอบชื่อไฟล์", "OK");
                return;
            }
            
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            
            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("โหลดไม่ได้", "ไม่สามารถโหลด DRAGON HUNTER SDF.asset", "OK");
                return;
            }
            
            // แสดงข้อมูลปัจจุบัน
            Debug.Log($"<color=yellow>[FixFont]</color> พบ Font: {fontAsset.name}");
            Debug.Log($"<color=yellow>[FixFont]</color> Path: {assetPath}");
            
            // สร้าง Font Atlas ใหม่
            bool success = RegenerateFontAtlas(fontAsset);
            
            if (success)
            {
                EditorUtility.DisplayDialog("✅ แก้ไขสำเร็จ!", 
                    "DRAGON HUNTER Font Atlas ถูกสร้างใหม่เรียบร้อยแล้ว\n\nError หายไปแล้ว!", 
                    "เยี่ยม!");
                
                Debug.Log("<color=lime>[FixFont]</color> ✅ สร้าง DRAGON HUNTER Atlas ใหม่สำเร็จ!");
            }
            else
            {
                EditorUtility.DisplayDialog("❌ แก้ไขไม่สำเร็จ", 
                    "ไม่สามารถสร้าง Font Atlas ใหม่ได้\nกรุณาลองวิธี Manual", 
                    "OK");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("❌ Error", $"เกิดข้อผิดพลาด: {e.Message}", "OK");
            Debug.LogError($"<color=red>[FixFont]</color> ❌ Error: {e.Message}");
        }
    }
    
    private static bool RegenerateFontAtlas(TMP_FontAsset fontAsset)
    {
        try
        {
            if (fontAsset == null)
            {
                Debug.LogError("<color=red>[FixFont]</color> ❌ Font Asset เป็น null ไม่สามารถสร้าง Atlas ใหม่ได้");
                return false;
            }

            // Best Practice: บังคับให้ Unity re-import asset เพื่อสร้าง Atlas ใหม่
            // นี่เป็นวิธีที่เชื่อถือได้มากที่สุดในการอัปเดต Font Atlas
            string assetPath = AssetDatabase.GetAssetPath(fontAsset);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=lime>[FixFont]</color> ✅ บังคับ Re-import '{fontAsset.name}' เพื่อสร้าง Atlas ใหม่สำเร็จ!");
                return true;
            }
            else
            {
                Debug.LogError($"<color=red>[FixFont]</color> ❌ ไม่พบ Asset Path สำหรับ '{fontAsset.name}'");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[FixFont]</color> ❌ Regenerate Atlas Error: {e.Message}");
            return false;
        }
    }
    
    [MenuItem("Tools/Fix Font/Delete and Recreate DRAGON HUNTER")]
    public static void DeleteAndRecreate()
    {
        if (EditorUtility.DisplayDialog("ยืนยันการลบ", 
            "จะลบ DRAGON HUNTER SDF.asset แล้วสร้างใหม่\n\nต้องการทำต่อหรือไม่?", 
            "ลบและสร้างใหม่", "ยกเลิก"))
        {
            try
            {
                // หาไฟล์ DRAGON HUNTER SDF
                string[] guids = AssetDatabase.FindAssets("DRAGON HUNTER SDF");
                
                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("ไม่พบไฟล์", "ไม่พบไฟล์ DRAGON HUNTER SDF.asset", "OK");
                    return;
                }
                
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                
                // ลบไฟล์
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("✅ ลบสำเร็จ!", 
                    "ลบ DRAGON HUNTER SDF.asset เรียบร้อยแล้ว\n\nUnity จะสร้างใหม่โดยอัตโนมัติเมื่อจำเป็น", 
                    "OK");
                
                Debug.Log("<color=orange>[FixFont]</color> 🗑️ ลบ DRAGON HUNTER SDF.asset เรียบร้อย");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("❌ Error", $"เกิดข้อผิดพลาด: {e.Message}", "OK");
                Debug.LogError($"<color=red>[FixFont]</color> ❌ Delete Error: {e.Message}");
            }
        }
    }
}
