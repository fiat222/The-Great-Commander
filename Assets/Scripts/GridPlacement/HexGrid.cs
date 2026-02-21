using UnityEngine;
using DT.GridSystem; // 🌟 ต้องเรียก Namespace ให้ตรงกับในสคริปต์ที่คุณส่งมา

// สร้างคลาสใหม่ที่ "ล็อก" ประเภทข้อมูลไว้ (สมมติว่าเก็บข้อมูลเป็น String ไปก่อน)
public class HexGrid : HexGridSystem3D<string>
{
    public HexOrientation Orientation => hexOrientation;
    
}