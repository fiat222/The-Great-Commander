using UnityEngine;

/// <summary>
/// เก็บผลการเลือกตัวละครข้ามซีน (static)
/// ข้อมูลถูก Sync ผ่าน NetworkVariable.OnValueChanged ใน CharacterSelectManager
/// → ทุกเครื่องมีข้อมูลเหมือนกัน
/// </summary>
public static class CharacterSelectData
{
    public static int P1CharacterIndex = -1;
    public static int P2CharacterIndex = -1;
    public static CharacterDataSO[] Characters;

    /// <summary>ดึง CharacterDataSO ของ Player ตาม playerIndex (0=Host, 1=Client)</summary>
    public static CharacterDataSO GetCharacter(int playerIndex)
    {
        if (Characters == null) return null;
        int idx = playerIndex == 0 ? P1CharacterIndex : P2CharacterIndex;
        if (idx < 0 || idx >= Characters.Length) return null;
        return Characters[idx];
    }

    /// <summary>ดึง Prefab ที่ต้อง Spawn ในเครื่องของเรา</summary>
    public static GameObject GetMyPlayerPrefab(int myPlayerIndex)
    {
        var charData = GetCharacter(myPlayerIndex);
        if (charData == null) return null;
        // ใช้ playablePrefab ถ้ามี ไม่งั้น fallback เป็น playerPrefab
        return charData.playablePrefab != null ? charData.playablePrefab : charData.playerPrefab;
    }

    /// <summary>รีเซ็ตข้อมูลทั้งหมด (เรียกตอนกลับ Menu)</summary>
    public static void Reset()
    {
        P1CharacterIndex = -1;
        P2CharacterIndex = -1;
        Characters = null;
    }
}
