using DT.GridSystem;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexGridVisualizer : MonoBehaviour
{
    public HexGrid hexGrid; 
    public Color lineColor = Color.white;

    private Mesh mesh;

    public void CreateGridMesh()
    {
        if (hexGrid == null) return;

        mesh = new Mesh();
        mesh.name = "HexGridMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        // ดึงขนาด Grid จากสคริปต์หลัก
        for (int x = 0; x < hexGrid.GridSize.x; x++)
        {
            for (int y = 0; y < hexGrid.GridSize.y; y++)
            {
                // หาตำแหน่งกึ่งกลางของช่องนั้นๆ
                Vector3 center = hexGrid.GetWorldPosition(x, y, true);

                // สร้างจุด 6 มุมของหกเหลี่ยม
                int startIndex = vertices.Count;
                for (int i = 0; i < 6; i++)
                {
                    float angleDeg = (hexGrid.Orientation == HexGridSystem3D<string>.HexOrientation.PointyTop)
                                     ? 60f * i - 30f : 60f * i;
                    float angleRad = Mathf.Deg2Rad * angleDeg;

                    // คำนวณจุดมุมโดยอิงจาก CellSize
                    Vector3 corner = center + new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * (hexGrid.CellSize * 0.5f);
                    vertices.Add(corner + new Vector3(0, 0.05f, 0)); // ยกขึ้นจากพื้นนิดหน่อยกันภาพซ้อน

                    // เชื่อมเส้น
                    indices.Add(startIndex + i);
                    indices.Add(startIndex + (i + 1) % 6);
                }
            }
        }

        mesh.SetVertices(vertices);
        // ตั้งค่าให้วาดเป็นเส้น (Lines) แทนที่จะเป็นรูปสามเหลี่ยม
        mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);

        GetComponent<MeshFilter>().mesh = mesh;
    }
}