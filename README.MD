# The Great Commander (เดอะ เกรท คอมมานเดอร์)

**The Great Commander** คือเกมแนว **Action PvP Tower Defense** ที่ผสมผสานระหว่างการบริหารจัดการทรัพยากร (Strategy Economy) และการควบคุมตัวละครต่อสู้แบบ Real-time (Active Combat) เพื่อสร้างประสบการณ์ใหม่ในแนวเกมป้องกันฐาน

---

## จุดเด่นของเกม (Key Features)

### Active Combat System
เปลี่ยนจากผู้เฝ้าดู (Passive Observer) สู่ผู้ควบคุมสมรภูมิ! ผู้เล่นสามารถเลือกฮีโร่ได้ 2 สายอาชีพ:
*   **Warrior (Lancelot):** สายโจมตีประชิด (Melee) พร้อมระบบ Combo, Parry, และ Skill สุดอลังการ
*   **Archer (Mockingjay):** สายโจมตีไกล (Ranged) เน้นความเร็วและการยิงธนูแบบฝนถล่มทลาย

### The Split Battlefield (Multiplayer PvP 1v1)
นวัตกรรมการออกแบบสนามรบที่แยกการคำนวณยูนิตออกจากกันเพื่อลดปัญหา Network Latency แต่ยังคงการเชื่อมต่อข้อมูลที่ลื่นไหลผ่าน **Unity Netcode for GameObjects** ผู้เล่นทั้งสองสถาบันต้องแข่งขันกันป้องกันฐาน (The Fortress) ของตนเอง

### Strategic Economy & Disruption
ตัดสินใจอย่างชาญฉลาดในทุกวินาที:
*   **Defense:** ซื้อ Minions (ยูนิตสนับสนุน 6 ชนิด) เพื่อป้องกันฐาน
*   **Upgrade:** ใช้ Orb เพื่อพัฒนาความสามารถของฮีโร่และยูนิตให้แข็งแกร่งขึ้น (Max Level 5)
*   **Disruption:** ส่งกองทัพศัตรูไปขัดขวางสมดุลฝั่งตรงข้ามแบบ Real-time!

---

## 🔄 วงจรการเล่น (Gameplay Loop)

1.  **Planning Phase (30s):** บริหารจัดการทอง (Gold) ซื้อยูนิต วางตำแหน่งบนระบบ Grid และอัปเกรดความสามารถ
2.  **Combat Phase:** ลงสนามรบเคียงคู่กับกองกำลัง Minion ป้องกันการบุกรุกของศัตรู (Goblin, Skeleton, Golem, Troll ฯลฯ)
3.  **Wave Scaling:** ความยากของศัตรูจะเพิ่มขึ้นแบบทวีคูณ (Dynamic Difficulty) ทั้งด้านค่าสถานะและจำนวน

---

## 🛠️ เทคนิคและเทคโนโลยีที่ใช้ (Technical Excellence)

*   **Game Engine:** Unity Engine (Universal Render Pipeline - URP)
*   **Networking:** Unity Netcode for GameObjects (NGO) v2.9.2
*   **Camera System:** Unity Cinemachine (Dynamic Follow & Target Lock)
*   **AI System:** พัฒนา AI ศัตรูที่มีรูปแบบการต่อสู้หลากหลาย (State Machine, Strafe, Hyper Armor)
*   **Architecture:** ระบบ **Unit Cost Base Stat Balance** เพื่อความสมดุลที่คำนวณจากสูตรคณิตศาสตร์
*   **AI-Assisted Development:** ใช้ Claude (Code), ChatGPT (Design), และ Gemini (Assets) เพื่อเพิ่มประสิทธิภาพการพัฒนา

---

## 🎮 การควบคุม (Controls)

| การกระทำ | ปุ่มกด |
| :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` |
| **Attack (Combo)** | `Left Click` |
| **Parry / Shield** | `Right Click` |
| **Roll / Dodge** | `Left Shift` |
| **Special Skill** | `R` |
| **Target Lock** | `Tab` |
| **UI Interaction** | `F` (Toggle Cursor) |

---

## 👥 ทีมพัฒนา (The Team)
*   thanapon 
*   rusnanee 
*   sunpipop 

---
*โปรเจคนี้เป็นส่วนหนึ่งของวิชา 240-332 Game Development และโครงงาน NSC 2026*
