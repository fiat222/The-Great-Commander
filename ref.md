คุณคือผู้ตรวจสอบ Asset และ License สำหรับโปรเจกต์ Unity ระดับมืออาชีพ

โปรดวิเคราะห์โปรเจกต์ Unity นี้อย่างละเอียด เพื่อจัดทำรายการ Asset ภายนอกทั้งหมดที่ถูกนำมาใช้จริงในเกม สำหรับนำไปเขียนอ้างอิงในรายงานโครงการ

## เป้าหมาย

ตรวจสอบและจำแนก Asset ภายในโปรเจกต์ออกเป็นกลุ่มต่อไปนี้

1. Asset ที่ทีมพัฒนาสร้างขึ้นเอง
2. Asset ที่ดาวน์โหลดหรือซื้อมาจาก Unity Asset Store
3. Asset จากเว็บไซต์ภายนอก
4. Unity Package และ Third-party Package
5. Asset ที่มีอยู่ในโปรเจกต์ แต่ไม่พบหลักฐานว่าถูกใช้งาน
6. Asset ที่ถูกใช้งานจริงใน Scene, Prefab, ScriptableObject, Resources, Addressables หรือระบบ Runtime Loading
7. Asset ที่ไม่สามารถระบุแหล่งที่มาหรือ License ได้อย่างแน่ชัด

## ขอบเขตการตรวจสอบ

โปรดตรวจสอบข้อมูลต่อไปนี้ทั้งหมด หากมีอยู่ในโปรเจกต์

* โฟลเดอร์ `Assets/`
* โฟลเดอร์ `Packages/`
* ไฟล์ `Packages/manifest.json`
* ไฟล์ `Packages/packages-lock.json`
* Unity Scene ทุกไฟล์
* Prefab ทุกไฟล์
* Material
* Texture และ Sprite
* 3D Model และ Animation
* Audio Clip และ Music
* Font
* Shader และ Shader Graph
* VFX และ Particle System
* ScriptableObject
* Timeline
* Animator Controller
* Terrain Data
* Lighting Data
* Render Pipeline Asset
* Resources
* StreamingAssets
* Addressables
* Plugins
* DLL
* Native Plugin
* Editor Extension
* Asset Store Package
* ไฟล์ README, LICENSE, NOTICE, Documentation และไฟล์ข้อความที่เกี่ยวข้อง
* `.meta` file ที่ช่วยระบุ GUID และความสัมพันธ์ระหว่าง Asset

## วิธีตรวจสอบการใช้งาน

อย่าตัดสินว่า Asset ถูกใช้งานจากการมีไฟล์อยู่ในโฟลเดอร์เพียงอย่างเดียว

ให้ตรวจสอบการอ้างอิงจาก

* Scene ที่อยู่ใน Build Settings หรือ Build Profiles
* Scene ทุกไฟล์ภายในโปรเจกต์
* Prefab ที่ถูกอ้างอิงจาก Scene
* Prefab ซ้อน Prefab
* Material ที่อ้างอิง Texture หรือ Shader
* Animator Controller ที่อ้างอิง Animation Clip
* ScriptableObject ที่อ้างอิง Asset อื่น
* Timeline
* Addressables
* Resources
* StreamingAssets
* `AssetDatabase.GetDependencies`
* GUID reference ภายในไฟล์ `.unity`, `.prefab`, `.asset`, `.mat` และไฟล์ YAML อื่น
* การโหลดแบบ Runtime เช่น

```csharp
Resources.Load
Resources.LoadAll
Addressables.LoadAssetAsync
Addressables.LoadAssetsAsync
AssetBundle.LoadAsset
AssetBundle.LoadAssetAsync
SceneManager.LoadScene
File.ReadAllBytes
File.ReadAllText
UnityWebRequest
Application.streamingAssetsPath
Application.persistentDataPath
```

* Path หรือชื่อ Asset ที่ถูกประกอบจาก String
* Asset ที่อาจถูกโหลดผ่าน Reflection หรือ Custom Asset Loader

หากไม่สามารถพิสูจน์ได้ว่า Asset ถูกใช้หรือไม่ ให้ระบุสถานะว่า “ต้องตรวจสอบเพิ่มเติม” ห้ามเดา

## การค้นหาแหล่งที่มา

สำหรับ Asset แต่ละรายการ ให้พยายามค้นหาหลักฐานแหล่งที่มาจาก

* README
* LICENSE
* NOTICE
* Documentation
* ชื่อโฟลเดอร์
* Namespace
* Assembly Definition
* Package name
* Package ID
* URL ภายในไฟล์
* Comment ภายใน Script
* Metadata
* Publisher name
* Author name
* Asset Store package information
* Git repository information
* ชื่อไฟล์ต้นฉบับ
* เครดิตที่ฝังอยู่ในไฟล์เสียง รูปภาพ หรือโมเดล หากตรวจสอบได้

ห้ามสรุป License จากชื่อเว็บไซต์เพียงอย่างเดียว

ตัวอย่างเช่น Asset จากเว็บไซต์เดียวกันอาจใช้ License แตกต่างกันในแต่ละไฟล์

หากไม่พบหลักฐาน License ให้ระบุว่า “ไม่พบข้อมูล License” และแจ้งความเสี่ยงในการนำไปเผยแพร่

## การจัดกลุ่ม Asset

อย่าแสดงทุกไฟล์ย่อยโดยไม่จำเป็น

ให้รวม Asset ที่มาจาก Package หรือชุดเดียวกันเป็นรายการเดียว เช่น

* ชุด Environment Pack หนึ่งชุด
* Character Pack หนึ่งชุด
* Audio Pack หนึ่งชุด
* Icon Pack หนึ่งชุด
* Unity Package หนึ่ง Package

แต่ให้แยกรายการเมื่อ

* มาจากคนละแหล่ง
* ใช้ License คนละแบบ
* มีผู้สร้างคนละคน
* เป็น Asset คนละชุด
* มีความเสี่ยงด้านสิทธิ์แตกต่างกัน

ไฟล์ที่ทีมสร้างเอง เช่น Scene, Prefab, Material หรือ Script ซึ่งสร้างจาก Asset ภายนอก ไม่จำเป็นต้องนับเป็น Asset ภายนอกใหม่ แต่ควรระบุว่าใช้ Asset ต้นทางใด

## ผลลัพธ์ที่ต้องการ

จัดทำรายงานเป็นภาษาไทย โดยแบ่งเป็นหัวข้อต่อไปนี้

### 1. บทสรุป

สรุปจำนวน

* Asset ภายนอกที่ใช้งานจริง
* Asset ที่ทีมสร้างเอง
* Package ภายนอก
* Asset ที่ไม่พบการใช้งาน
* Asset ที่ไม่ทราบแหล่งที่มา
* Asset ที่ไม่พบ License
* Asset ที่มีความเสี่ยงด้านลิขสิทธิ์

### 2. ตาราง Asset ภายนอกที่ใช้งานจริง

สร้างตารางโดยมีคอลัมน์ดังนี้

| ลำดับ | ชื่อ Asset หรือ Package | ประเภท | ตำแหน่งในโปรเจกต์ | ถูกใช้งานโดย | แหล่งที่มา | ผู้สร้างหรือผู้เผยแพร่ | License | ต้องให้เครดิตหรือไม่ | หลักฐานที่พบ | ระดับความมั่นใจ | หมายเหตุ |

ในคอลัมน์ “ถูกใช้งานโดย” ให้ระบุ Scene, Prefab, Material, Script หรือระบบที่อ้างอิง Asset นั้น

ในคอลัมน์ “หลักฐานที่พบ” ให้ระบุชื่อไฟล์หรือ Path ที่ตรวจพบข้อมูล เช่น

* `Assets/ThirdParty/Example/README.md`
* `Packages/manifest.json`
* `Assets/Scenes/MainMenu.unity`
* `Assets/Prefabs/Player.prefab`

### 3. ตาราง Asset ที่ทีมพัฒนาสร้างเอง

| ลำดับ | รายการ | ประเภท | ตำแหน่ง | หลักฐานว่าเป็นงานของทีม | หมายเหตุ |

### 4. ตาราง Asset ที่มีอยู่แต่ไม่พบการใช้งาน

| ลำดับ | Asset | ประเภท | ตำแหน่ง | เหตุผลที่คาดว่าไม่ได้ใช้ | ระดับความมั่นใจ | คำแนะนำ |

อย่าระบุว่า “ไม่ได้ใช้แน่นอน” หากยังมีความเป็นไปได้ที่จะถูกโหลดแบบ Runtime

### 5. ตาราง Asset ที่ไม่ทราบแหล่งที่มาหรือ License

| ลำดับ | Asset | ตำแหน่ง | ข้อมูลที่พบ | ข้อมูลที่ขาด | ความเสี่ยง | สิ่งที่ควรตรวจสอบต่อ |

### 6. ตาราง Unity Package และ Plugin

| ลำดับ | Package ID | ชื่อ Package | Version | ผู้พัฒนา | ที่มา | License | ใช้งานด้านใด | เป็น Direct หรือ Transitive Dependency |

### 7. รายการเครดิตสำหรับใส่ในรายงาน

เขียนข้อความเครดิตที่พร้อมนำไปใช้ในรายงาน โดยใช้รูปแบบประมาณนี้

> โครงการนี้ใช้ [ชื่อ Asset] จัดทำโดย [ชื่อผู้สร้าง] จาก [แหล่งที่มา] ภายใต้สัญญาอนุญาต [ชื่อ License]

หาก License ไม่ได้บังคับให้ให้เครดิต แต่ควรให้เพื่อความเหมาะสม ให้ระบุว่าเป็น “เครดิตเพื่อแสดงการขอบคุณ”

### 8. รายการอ้างอิงแบบพร้อมใช้งาน

สร้างรายการอ้างอิงสำหรับแต่ละ Asset โดยใช้รูปแบบเดียวกันทั้งหมด เช่น

ผู้สร้างหรือองค์กร. (ปีหรือ ม.ป.ป.). ชื่อ Asset หรือ Package. แหล่งเผยแพร่. วันที่เข้าถึงหรือ URL หากมีข้อมูลอยู่ในโปรเจกต์

ห้ามสร้าง URL, ผู้สร้าง, ปีเผยแพร่ หรือ License ขึ้นมาเอง

### 9. ความเสี่ยงและคำแนะนำ

จัดระดับความเสี่ยงเป็น

* ต่ำ
* ปานกลาง
* สูง
* วิกฤต

พิจารณาจาก

* ไม่พบแหล่งที่มา
* ไม่พบ License
* License ห้ามใช้เชิงพาณิชย์
* License ต้องเปิดเผย Source Code
* License ต้องให้เครดิต
* Asset ถูกแก้ไขต่อ
* Asset Store Seat License
* Font License
* Music License
* Trademark
* Model ที่อาจมีทรัพย์สินทางปัญญาของบุคคลอื่น
* Package ที่มี License หลายชั้น

### 10. สิ่งที่ AI ยังยืนยันไม่ได้

ระบุรายการที่ต้องให้มนุษย์ตรวจสอบต่ออย่างชัดเจน เช่น

* ประวัติการซื้อจาก Unity Asset Store
* ใบเสร็จ
* License ณ วันที่ดาวน์โหลด
* สิทธิ์ของบัญชีผู้ซื้อ
* Asset ที่ถูกนำเข้ามาแล้วเปลี่ยนชื่อ
* Asset ที่ถูกแก้ไขจนระบุแหล่งที่มายาก
* Runtime Asset ที่ดาวน์โหลดจาก Server ภายนอก
* Asset ที่ไม่ได้อยู่ใน Repository

## ข้อกำหนดสำคัญ

* ห้ามเดาข้อมูล
* ห้ามสร้างชื่อผู้สร้าง แหล่งที่มา URL หรือ License ขึ้นมาเอง
* ทุกข้อสรุปต้องมีหลักฐานเป็น Path หรือไฟล์ที่ตรวจพบ
* แยกระหว่าง “พบไฟล์ในโปรเจกต์” กับ “ยืนยันว่าใช้งานจริง”
* แยกระหว่าง “ใช้งานใน Editor” กับ “รวมอยู่ในตัวเกมที่ Build”
* ระบุระดับความมั่นใจของทุกข้อสรุปเป็น สูง กลาง หรือต่ำ
* หากโปรเจกต์มีขนาดใหญ่ ให้บันทึกผลลัพธ์เป็นไฟล์ `ASSET_AUDIT_REPORT.md`
* สร้างไฟล์ข้อมูลแบบ CSV เพิ่มเติมชื่อ `asset_audit.csv`
* ห้ามแก้ไขหรือลบ Asset ใดในโปรเจกต์
* ห้ามเปลี่ยน Scene, Prefab, Package หรือ Project Settings
* ให้ทำงานแบบ Read-only เท่านั้น

ก่อนสรุปผล ให้ตรวจสอบซ้ำว่ามี Asset ที่ถูกโหลดผ่าน `Resources`, `Addressables`, `StreamingAssets`, AssetBundle หรือ String path หรือไม่

�โดยทำงานร่วมกับ

สร้าง Unity Editor Script สำหรับตรวจสอบ Asset ที่ถูกใช้งานในโปรเจกต์ โดยมีข้อกำหนดดังนี้

1. ใช้ `AssetDatabase.GetDependencies()` ตรวจสอบ Dependency ของทุก Scene
2. ตรวจสอบทั้ง Scene ใน Build Settings และ Scene อื่นภายใน `Assets/`
3. ตรวจสอบ Prefab, ScriptableObject, Material, Animator Controller, Timeline และ Render Pipeline Asset
4. ตรวจสอบโฟลเดอร์ `Resources`, `StreamingAssets` และ Addressables
5. แยก Asset ที่อยู่ใน `Assets/` และ `Packages/`
6. ตัดไฟล์ `.cs`, `.meta` และไฟล์ Editor-only ออกจากรายการ Asset สำหรับเครดิต แต่เก็บ Package และ Plugin ไว้ในรายงานแยก
7. แสดงว่าแต่ละ Asset ถูกอ้างอิงจากไฟล์ใด
8. รวมรายการซ้ำโดยใช้ Asset Path
9. Export ผลลัพธ์ออกเป็น

   * `Assets/AssetAudit/used_assets.csv`
   * `Assets/AssetAudit/used_assets.json`
10. CSV ต้องมีคอลัมน์

```text
AssetPath,AssetType,ReferencedBy,ReferenceCount,SourceGroup,IsInBuildScene,IsResources,IsStreamingAssets,IsPackage,Confidence
```

11. เพิ่มเมนูใน Unity

```text
Tools > Asset Audit > Scan Used Assets
```

12. Script ต้องทำงานใน Unity 6
13. ห้ามแก้ไข Asset ใดในโปรเจกต์
14. ใส่ Error Handling และ Progress Bar
15. เมื่อทำงานเสร็จ ให้แสดงจำนวน Asset ทั้งหมด จำนวน Asset ที่ตรวจพบว่าใช้ และจำนวน Asset ที่ไม่พบ Reference
16. อธิบายข้อจำกัดว่า Runtime Loading แบบ Dynamic String อาจตรวจไม่พบ
17. หลังสร้าง Script ให้ตรวจสอบ Compile Error และแก้จนสามารถ Compile ได้

