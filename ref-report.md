# รายงานตรวจสอบ Asset และ License ของโปรเจกต์ Unity

วันที่ตรวจสอบ: 12 กรกฎาคม 2569  
โปรเจกต์: `The-Great-Commander`  
ขอบเขต: ตรวจจากไฟล์ใน repository เท่านั้น โดยไม่เปิด Unity Editor, ไม่รัน Build, ไม่แก้ไข Scene/Prefab/Package/Project Settings

## วิธีตรวจสอบ

- อ่านข้อกำหนดจาก `ref.md`
- ตรวจ `Assets/`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/EditorBuildSettings.asset`, `README.MD`, `REPORT.md`
- ตรวจรายการไฟล์ใน `Assets/` และกลุ่มไฟล์หลัก เช่น Scene, Prefab, Material, Texture, Model, Audio, Font, Shader, VFX, ScriptableObject, Terrain, Resources และ Plugins
- สร้าง dependency แบบ read-only จาก GUID ในไฟล์ YAML ของ Unity เช่น `.unity`, `.prefab`, `.asset`, `.mat`, `.controller`, `.overrideController`, `.anim`, `.terrainlayer`, `.lighting`, `.mixer`, `.inputactions`, `.shadergraph`
- ตรวจ runtime-loading pattern ใน script เช่น `Resources.Load`, `SceneManager.LoadScene`, `AssetDatabase.LoadAssetAtPath`, `File.ReadAllBytes`, `File.ReadAllText`
- ไม่สรุป License จากชื่อ package หรือชื่อเว็บไซต์เพียงอย่างเดียว

## ข้อจำกัดของรายงาน

- ไม่ได้รัน `AssetDatabase.GetDependencies()` จริงใน Unity Editor จึงเป็นการประมาณจาก GUID ในไฟล์ YAML
- ไม่ได้ตรวจ metadata ฝังในไฟล์ binary เช่น `.fbx`, `.png`, `.mp3`, `.wav`, `.pdf`
- ไม่ได้ยืนยันประวัติซื้อ Asset Store, seat license, ใบเสร็จ, บัญชีผู้ซื้อ หรือ license ณ วันที่ดาวน์โหลด
- ไม่ได้สร้าง `Assets/AssetAudit/used_assets.csv`, `used_assets.json`, หรือ Unity Editor Script เพราะคำขอรอบนี้ให้เขียน report และข้อกำหนดโปรเจกต์ห้ามแก้ไข asset/project settings โดยไม่อนุมัติแผนเฉพาะ
- Runtime loading จาก dynamic string, reflection, server ภายนอก หรือ asset ที่ไม่ได้อยู่ใน repository อาจตรวจไม่พบ

## 1. บทสรุป

ผลการตรวจแบบ static GUID scan:

| รายการ | จำนวน/สถานะ | หลักฐาน |
|---|---:|---|
| Scene ใน Build Settings | 5 scene | `ProjectSettings/EditorBuildSettings.asset` |
| Scene ทั้งหมดที่ตรวจพบใน `Assets/` | 112 scene | inventory จาก `Assets/` |
| Asset ที่มี GUID ใน `.meta` | 4,944 รายการ | `.meta` files |
| ไฟล์ Unity YAML ที่มี reference | 1,791 ไฟล์ | `.unity`, `.prefab`, `.asset`, `.mat`, controller ฯลฯ |
| Asset ที่พบใน dependency closure ของ Build Settings scenes | 1,004 รายการ | GUID scan จาก 5 build scenes |
| Asset ที่พบใน dependency closure ของ scene ทั้งหมด | 2,547 รายการ | GUID scan จาก scene ทั้งหมด |
| กลุ่ม asset/package ภายนอกหรือไม่ทราบที่ถูกอ้างอิงจาก Build Settings | อย่างน้อย 40 กลุ่ม | ตารางข้อ 2 |
| Package ภายนอก/direct Unity package | 21 package direct ที่ไม่ใช่ built-in module | `Packages/manifest.json` |
| Package transitive สำคัญ | อย่างน้อย 13 package | `Packages/packages-lock.json` |
| Asset ที่มีอยู่แต่ไม่พบ reference จาก Build Settings | หลายกลุ่ม เช่น `DoubleL`, `ParrelSync`, `VegetationSpawner`, `Assets/Models`, `_Recovery` | GUID scan |
| Asset ที่ไม่ทราบแหล่งที่มาหรือ License | พบหลายกลุ่ม โดยเฉพาะ `Assets/Audio`, `Assets/Pictures`, `Assets/Fonts`, package ใน `Assets/Plugins` ที่ไม่มี license file | ตารางข้อ 5 |
| Asset ที่ไม่พบ License local | ส่วนใหญ่ของ Asset Store-style folders และ audio/font/image ภายนอก | README/license search |
| ความเสี่ยงรวม | สูง | มี asset ใช้งานจริงจำนวนมากแต่ไม่มี license/purchase proof ใน repo |

## 2. ตาราง Asset ภายนอกที่ใช้งานจริง

เกณฑ์ "ใช้งานจริง" ในตารางนี้หมายถึงพบใน dependency closure ของ scene ใน Build Settings หรือพบ runtime-loading evidence ใน code. ถ้าเป็น package/demo asset ที่พบเฉพาะใน scene อื่น จะไม่จัดเป็นใช้งานจริงใน build แต่จะระบุในข้อ 4.

| ลำดับ | ชื่อ Asset หรือ Package | ประเภท | ตำแหน่งในโปรเจกต์ | ถูกใช้งานโดย | แหล่งที่มา | ผู้สร้างหรือผู้เผยแพร่ | License | ต้องให้เครดิตหรือไม่ | หลักฐานที่พบ | ระดับความมั่นใจ | หมายเหตุ |
|---:|---|---|---|---|---|---|---|---|---|---|---|
| 1 | BGM/SFX audio set | Audio | `Assets/Audio/Bgm`, `Assets/Audio/Sfx` | `Assets/Prefabs/Manager/AudioManager.prefab`, player selector prefabs | ไม่พบข้อมูลครบถ้วน; บางชื่อไฟล์ระบุ `freesound_*`, `dragon-studio`, `universfield`, `voicebosch`, `crunchpixstudio` | หลายผู้สร้างตามชื่อไฟล์ แต่ยังไม่ยืนยัน | ไม่พบข้อมูล License รายไฟล์ | ต้องตรวจทีละไฟล์ | `Assets/Audio/...`, `REPORT.md` | กลาง | ความเสี่ยงสูงเพราะ audio license มักต่างกันรายไฟล์ |
| 2 | Storm Gust font | Font | `Assets/Fonts/storm_gust` | `Assets/Scenes/CharacterSelectScene.unity`, `Assets/Scenes/SoloCharactor.unity` | ไม่พบใน repo | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Fonts/storm_gust/Storm Gust.otf`, `Storm Gust SDF.asset` | สูงสำหรับการใช้งาน, ต่ำสำหรับ license | ต้องหาแหล่งดาวน์โหลด/ใบอนุญาต |
| 3 | DRAGON HUNTER font | Font | `Assets/Fonts/DRAGON HUNTER` | `Assets/Scenes/MenuSceneTest.unity`, `Assets/Canvas/SettingCanvas.prefab` | ไม่พบใน repo | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Fonts/DRAGON HUNTER/DRAGON HUNTER.otf`, `DRAGON HUNTER SDF 1.asset` | สูงสำหรับการใช้งาน, ต่ำสำหรับ license | Font เป็นกลุ่มเสี่ยง ต้องยืนยันสิทธิ์ |
| 4 | TextMesh Pro imported resources | Font/UI/Shaders | `Assets/TextMesh Pro` | UI prefabs เช่น `Canvas.prefab`, enemy amount prefabs | Unity/TextMesh Pro package resources | Unity Technologies; EmojiOne สำหรับ sprite sample | พบ Apache 2.0 เฉพาะ `Roboto-Bold`; EmojiOne ให้ไปตรวจ terms | ต้องตรวจเฉพาะส่วนที่ใช้ | `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`, `Roboto-Bold - License.txt` | กลาง | ใช้ TextMesh Pro ใน UI; ต้องแยก sample fonts/sprites ที่ถูกใช้งานจริง |
| 5 | Massive Crosshair Bundle / Crosshairs | UI texture | `Assets/Plugins/Crosshairs` | `Assets/Canvas/Canvas.prefab`, `Assets/Scenes/GameScene.unity`, `SoloGameScene.unity` | README ระบุ package crosshair สำหรับ Unity | OccaSoftware จากอีเมล support | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Crosshairs/Readme.txt` | สูงสำหรับการใช้งาน, กลางสำหรับ source | มี README แต่ไม่มี license |
| 6 | CliffStonePackM | Environment/rock model | `Assets/Plugins/Dzeruza/CliffStonePackM` | Prefab/material dependencies ใน build scenes | README PDF อยู่ใน package แต่ไม่ได้อ่านเนื้อหา | Dzeruza ตาม path | ไม่พบข้อมูล License ที่ยืนยันได้ | ต้องตรวจ | `Assets/Plugins/Dzeruza/CliffStonePackM/README_Documentation_CliffStonePackM.pdf` | กลาง | ต้องตรวจ PDF/Asset Store license |
| 7 | Enemy character packs | Character/model/animation | `Assets/Plugins/Enemy` | `Assets/DefaultNetworkPrefabs.asset`, enemy controllers/materials | หลาย package ใน folder `Enemy`; `REPORT.md` ระบุ Skeleton, Orc, Ogre, Demon, Goblin ฯลฯ | หลายผู้สร้าง ยังไม่ยืนยัน | ไม่พบข้อมูล License ครบถ้วน | ต้องตรวจ | `Assets/Plugins/Enemy/...`, `REPORT.md` | สูงสำหรับการใช้งาน, ต่ำสำหรับ license | มี `ReadMe.txt` ของ Demon แต่ไม่ระบุ license |
| 8 | Free Game VFX | VFX/particle | `Assets/Plugins/Eric VFX Studio/Free Game VFX` | VFX prefabs/materials ใน dependency | README ระบุ pack และช่องทางติดต่อ | Eric VFX Studio / Eric Wang ตาม README | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Eric VFX Studio/Free Game VFX/Readme.txt` | สูง | README ไม่มี license |
| 9 | Fantasy Skybox FREE | Skybox/texture | `Assets/Plugins/Fantasy Skybox FREE` | `Assets/Scenes/CharacterSelectScene.unity`, skybox materials | Unity Asset Store URL ใน README | Render Knight ตาม README/email | ไม่พบข้อมูล License local | ต้องตรวจ | `Assets/Plugins/Fantasy Skybox FREE/Readme.txt` | สูง | README ระบุ URL แต่ไม่ใช่ license |
| 10 | Fantasy knight | Character/model | `Assets/Plugins/Fantasy knight` | Materials/models/prefabs in dependency | Folder/package name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Fantasy knight/...` | กลาง | ต้องยืนยันแหล่งที่มา |
| 11 | Thundercloud | VFX | `Assets/Plugins/FlexUnit/Thundercloud` | Animator/material/prefab dependencies | README PDF อยู่ใน package | FlexUnit ตาม path | ไม่พบข้อมูล License ที่อ่านได้ | ต้องตรวจ | `Assets/Plugins/FlexUnit/Thundercloud/Readme.pdf` | กลาง | ใช้ใน build dependency 13 รายการ |
| 12 | GUI_Parts | UI images | `Assets/Plugins/GUI_Parts` | `Canvas.prefab`, `Money.prefab`, `SettingCanvas.prefab` | ไม่พบ source/license | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/GUI_Parts/...` | สูงสำหรับการใช้งาน, ต่ำสำหรับ license | มีภาพชื่อ ChatGPT/Image/date ต้องตรวจสิทธิ์ AI image ด้วย |
| 13 | Free Quick Effects Vol.1 | VFX | `Assets/Plugins/GabrielAguiarProductions/FreeQuickEffectsVol1` | Material/prefab dependencies | Folder name | Gabriel Aguiar Productions ตาม path | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/GabrielAguiarProductions/...` | กลาง | ต้องหา Asset Store/source proof |
| 14 | Game Input Controller Icons Free | UI/icon | `Assets/Plugins/GameInputControllerIconsFree` | `Canvas.prefab`, `SettingCanvas.prefab` | README PDF อยู่ใน package | ไม่พบใน text output | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/GameInputControllerIconsFree/readme.pdf` | สูง | ต้องอ่าน PDF/Asset Store page |
| 15 | HEROES | Character/animation/model | `Assets/Plugins/HEROES` | animation controller/material/prefab dependency | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/HEROES/...` | กลาง | ต้องยืนยันแหล่งที่มา |
| 16 | Mountain / Holotna | Environment/material | `Assets/Plugins/Holotna/Mountain` | Material dependencies | Folder name | Holotna ตาม path | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Holotna/...` | กลาง | ต้องยืนยันแหล่งที่มา |
| 17 | Modular Fantasy Bridges | Environment/model | `Assets/Plugins/Julien Tonsuso/Modular Fantasy Bridges` | bridge prefabs/materials | README มี Asset Store URL | Julien Tonsuso ตาม path | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Julien Tonsuso/Modular Fantasy Bridges/README.txt` | กลาง | README มี URL แต่ไม่ระบุ license |
| 18 | KE Statues - Lite | Environment/statue | `Assets/Plugins/KE Statues - Lite` | Material/prefab dependency | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/KE Statues - Lite/...` | กลาง | ใช้บางส่วนใน build dependency |
| 19 | Kevin Iglesias Human/Giant assets | Animation/model | `Assets/Plugins/Kevin Iglesias` | `Assets/Models/HumanM_Model.prefab` และ dependencies | `REPORT.md` อ้างถึง Human Animations และ Humanoid Giant | Kevin Iglesias ตาม folder และ `REPORT.md` | ไม่พบข้อมูล License local | ต้องตรวจ | `Assets/Plugins/Kevin Iglesias/...`, `REPORT.md` | สูงสำหรับการใช้งานบางส่วน | ต้องยืนยัน Asset Store purchase/license |
| 20 | LowPolyRPGWeapons_Lite | Weapon model | `Assets/Plugins/LowPolyRPGWeapons_Lite` | weapon prefabs/materials | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/LowPolyRPGWeapons_Lite/...` | กลาง | ใช้ 4 รายการใน build dependency |
| 21 | Slash Effects FREE | VFX | `Assets/Plugins/Matthew Guz/Slash Effects FREE` | slash materials/prefabs | README ระบุ contact email | Matthew Guz ตาม path/README | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Matthew Guz/Slash Effects FREE/Documentation/Readme IMPORTANT.txt` | กลาง | README ไม่ใช่ license |
| 22 | Medieval Town Exteriors | Environment/building | `Assets/Plugins/MedievalTownExteriors` | building/barrel/bucket prefabs/materials | README ระบุ package และเว็บไซต์ | Lylek Games | ไม่พบข้อมูล License local | ต้องตรวจ | `Assets/Plugins/MedievalTownExteriors/Readme.txt` | สูง | README ระบุ purchased asset แต่ต้องมี proof |
| 23 | Mini Legion Footman PBR HP Polyart | Character/model/animation | `Assets/Plugins/Mini Legion Footman PBR HP Polyart` | controller/material/model dependencies | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Mini Legion Footman PBR HP Polyart/...` | กลาง | ต้องยืนยัน source/license |
| 24 | Inferno VFX / Mirza Beig particle systems | VFX | `Assets/Plugins/Mirza Beig` | particle materials/textures | Folder name | Mirza Beig ตาม path | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Mirza Beig/...` | กลาง | ใช้บาง materials ใน build dependency |
| 25 | Modern GDR - Free icons pack | UI/icon | `Assets/Plugins/Modern GDR - Free icons pack` | `Canvas.prefab`, `PhaseTxt.prefab`, `SettingCanvas.prefab` | README ระบุ Asset Store review context | Jennifer ตาม README | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Modern GDR - Free icons pack/README.md` | สูง | README ไม่มี license |
| 26 | RPG Monster DUO PBR Polyart | Character/model/animation | `Assets/Plugins/RPG Monster DUO PBR Polyart` | slime/turtle controller/material dependencies | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/RPG Monster DUO PBR Polyart/...` | กลาง | ต้องยืนยัน source/license |
| 27 | RPG Tiny Fantasy Forest PBR | Environment/nature | `Assets/Plugins/RPG Tiny Fantasy Forest PBR` | terrain/nature materials/prefabs | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/RPG Tiny Fantasy Forest PBR/...` | กลาง | ใช้ 9 รายการใน build dependency |
| 28 | Scene Teleportation Kit | VFX/environment | `Assets/Plugins/Scene Teleportation Kit` | portal/ground materials | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Scene Teleportation Kit/...` | กลาง | ต้องยืนยัน source/license |
| 29 | Hand Painted Nature Kit LITE | Environment/nature | `Assets/Plugins/Silver_Cats/Hand_Painted_Nature_Kit_LITE` | material/terrain/demo dependencies | readme exists | Silver_Cats ตาม path | ไม่พบข้อมูล License จาก text ที่อ่าน | ต้องตรวจ | `Assets/Plugins/Silver_Cats/Hand_Painted_Nature_Kit_LITE/.../readme.txt` | กลาง | ใช้ 3 รายการใน build dependency |
| 30 | StoneKeep | Environment/castle | `Assets/Plugins/StoneKeep` | meshes/materials/prefabs | README ระบุ package | Lylek Games | ไม่พบข้อมูล License local | ต้องตรวจ | `Assets/Plugins/StoneKeep/Readme.txt` | สูง | README ระบุ purchased asset แต่ต้องมี proof |
| 31 | Stylized Character Archer | Character/model | `Assets/Plugins/Stylized Character_Archer` | `Archer.prefab`, materials/models | Folder name | ไม่พบสำหรับ character pack; shader component by Cyanilux | MIT เฉพาะ `ToonShader/Shader/CustomLighting/LICENSE` | ต้องตรวจ character pack | `Assets/Plugins/Stylized Character_Archer/...`, `LICENSE` | กลาง | ห้ามนำ MIT shader ไปสรุป license ทั้ง character pack |
| 32 | Stylized Lava Materials | Material/VFX | `Assets/Plugins/Stylized Lava Materials` | lava materials/demo dependency | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Stylized Lava Materials/...` | กลาง | ต้องยืนยัน source/license |
| 33 | Sword Two-Hander | Weapon model | `Assets/Plugins/Sword Two-Hander` | sword materials/model | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Sword Two-Hander/...` | กลาง | ใช้ 7 รายการใน build dependency |
| 34 | Tower Mage | Tower model | `Assets/Plugins/Tower` | `Assets/Prefabs/Base/Tower Mage.prefab` | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Tower/...` | สูง | ใช้ใน gameplay prefab |
| 35 | Trails VFX URP | VFX | `Assets/Plugins/Vefects/Trails VFX URP` | character/VFX materials and demo resources | Folder name | Vefects ตาม path | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/Vefects/...` | กลาง | ใช้ 55 รายการใน build dependency |
| 36 | WaterWorks | Water shader/material/runtime resource | `Assets/Plugins/WaterWorks` | `Resources.Load("Water_Volume")` ใน WaterWorks scripts, water prefab | Folder name | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Plugins/WaterWorks/Scripts/Water_Settings.cs`, `Water_Volume.cs`, `Resources/Water_Volume.mat` | สูง | พบ runtime loading โดยตรง |
| 37 | Magic effects pack | VFX | `Assets/VFX/Hovl Studio/Magic effects pack` | `Tower Mage.prefab`, `Archer.prefab`, `PlayerWarrior.prefab` | Folder name/readme demo | Hovl Studio ตาม path | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/VFX/Hovl Studio/Magic effects pack/...` | สูง | ใช้กับ player/tower prefabs |
| 38 | Cartoon FX legacy | VFX | `Assets/VFX/JMO Assets/Cartoon FX (legacy)` | `Assets/Scenes/MenuSceneTest.unity`, prefab/material dependencies | Folder name | JMO Assets ตาม path | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/VFX/JMO Assets/...` | กลาง | ใช้ใน menu/build dependency |
| 39 | Healing Item | Gameplay pickup/model/material | `Assets/ViapixGames/HealingItem` | `Assets/Prefabs/Pack/Healing Pack.prefab` | Documentation PDF exists | ViapixGames ตาม path | ไม่พบข้อมูล License ที่อ่านได้ | ต้องตรวจ | `Assets/ViapixGames/HealingItem/Documentation/HealingItemReadMe.pdf`, `Prefabs/Healing Item.prefab` | สูง | ใช้ใน build dependency |
| 40 | Pictures/icons including Gemini-generated images | UI/icon/image | `Assets/Pictures` | `Canvas.prefab`, `Money.prefab`, `SettingCanvas.prefab`, win/lose prefabs | บางไฟล์มีชื่อ `Gemini_Generated_Image_*`; อื่น ๆ ไม่พบ source | Google Gemini สำหรับบางไฟล์ตามชื่อ แต่ยังไม่ยืนยัน account/terms | ไม่พบข้อมูล License | ต้องตรวจ | `Assets/Pictures/...`, `REPORT.md` | สูงสำหรับการใช้งาน, ต่ำสำหรับ license | AI-generated image ต้องตรวจ terms ของบัญชีและวันที่สร้าง |

## 3. ตาราง Asset ที่ทีมพัฒนาสร้างเอง

รายการนี้เป็นการจัดกลุ่มจากชื่อโฟลเดอร์และลักษณะเฉพาะของโปรเจกต์ ไม่ใช่การรับรองลิขสิทธิ์ทั้งหมดของไฟล์ย่อย เพราะบาง prefab/material อาจอ้างอิง asset ภายนอก

| ลำดับ | รายการ | ประเภท | ตำแหน่ง | หลักฐานว่าเป็นงานของทีม | หมายเหตุ |
|---:|---|---|---|---|---|
| 1 | Game scenes หลัก | Scene | `Assets/Scenes/MenuSceneTest.unity`, `CharacterSelectScene.unity`, `GameScene.unity`, `SoloCharactor.unity`, `SoloGameScene.unity` | อยู่ใน Build Settings และสอดคล้องกับ gameplay ที่อธิบายใน `README.MD` | Scene อาจใช้ asset ภายนอกจำนวนมาก |
| 2 | Gameplay scripts | C# scripts | `Assets/Scripts` | Namespace/ชื่อ class เกี่ยวกับระบบเกม เช่น Network, Manager, Enemy, Player, UI, Singleplayer | ควรตรวจ snippet ภายนอกแยกถ้ามี |
| 3 | Gameplay prefabs | Prefab | `Assets/Prefabs`, `Assets/Canvas`, `Assets/Minimap` | ชื่อ prefab เฉพาะเกม เช่น Player, Minion, Enemy, Tower, Canvas, Manager | Prefab หลายตัวอ้างอิง model/VFX/audio ภายนอก |
| 4 | ScriptableObjects | ScriptableObject | `Assets/SO` | ชื่อข้อมูล Player/Minion/EnemyCombatPattern เฉพาะระบบเกม | Data สร้างเองแต่ field อาจอ้างอิง asset ภายนอก |
| 5 | Render/URP settings | Render pipeline assets | `Assets/Settings` | Project-specific URP/renderer/global settings | เป็น configuration ของโปรเจกต์ |
| 6 | Input actions และ Audio mixer | Unity config | `Assets/InputSystem_Actions.inputactions`, `Assets/MainMixer.mixer` | สอดคล้องกับ control/gameplay ใน `README.MD` | Mixer อ้างอิง audio clip ภายนอกทาง prefab/data |
| 7 | Terrain layer / terrain asset | Terrain data | `Assets/New Terrain.asset`, `Assets/NewLayer.terrainlayer` | อยู่ root project และใช้ใน scene dependency | Texture/material ที่ terrain ใช้อาจมาจาก package ภายนอก |
| 8 | Project documentation | Documentation | `README.MD`, `REPORT.md`, `ref.md` | ระบุทีมพัฒนาและรายละเอียดโครงการ | ไม่ใช่ game asset |

## 4. ตาราง Asset ที่มีอยู่แต่ไม่พบการใช้งานใน Build Settings

คำว่า "ไม่พบการใช้งาน" หมายถึงไม่พบใน static dependency closure ของ 5 scene ใน Build Settings เท่านั้น ไม่ได้ยืนยันว่าไม่ได้ใช้แน่นอน

| ลำดับ | Asset | ประเภท | ตำแหน่ง | เหตุผลที่คาดว่าไม่ได้ใช้ | ระดับความมั่นใจ | คำแนะนำ |
|---:|---|---|---|---|---|---|
| 1 | DoubleL demo assets | Animation/model/demo | `Assets/Plugins/DoubleL` | พบ reference จาก scene/demo ภายใน package แต่ build dependency = 0 | กลาง | ตรวจว่ามีการโหลด runtime หรือ copy ไป prefab อื่นหรือไม่ |
| 2 | ParrelSync | Editor tool | `Assets/Plugins/ParrelSync` | build dependency = 0 และ script อยู่ใน `Editor` | สูง | รายงานเป็น Editor-only plugin ไม่ใช่ game credit asset |
| 3 | VegetationSpawner demo | Editor/runtime/demo | `Assets/VegetationSpawner` | build dependency = 0 แต่ scene demo อ้างอิงภายใน package | กลาง | ถ้าไม่ได้ใช้ terrain runtime ให้แยกเป็น unused/demo-only |
| 4 | Viapix Common Scripts | Script/demo support | `Assets/ViapixGames/Common Scripts` | build dependency = 0; อ้างอิงจาก HealingItem demo scene | กลาง | แยกจาก HealingItem prefab ที่พบใช้จริง |
| 5 | Standalone Models | Model | `Assets/Models` | GUID scan ไม่พบใน scene dependency | กลาง | ตรวจว่าถูกใช้ผ่าน animator/avatar หรือ runtime string หรือไม่ |
| 6 | Recovery scenes and Mixamo-like files | Scene/model/animation backup | `Assets/_Recovery` | build dependency = 0; เป็น recovery scenes | กลาง | ไม่ควรใส่เป็น asset credit หลักจนกว่าจะยืนยันว่าใช้จริง |
| 7 | SelectCharacter BGM files | Audio | `Assets/Audio/Bgm/SelectCharacter` | กลุ่ม BGM มี 10 ไฟล์ แต่ build dependency พบ 3 ไฟล์หลัก | ต่ำ-กลาง | ตรวจ `AudioManager`/runtime assignment ว่าเลือกจาก string หรือ inspector อื่นหรือไม่ |
| 8 | Demo resources inside used packages | Demo scenes/materials/prefabs | หลาย folder ใน `Assets/Plugins/*/Demo*` | พบใน scene ทั้งหมดแต่ไม่อยู่ใน build scenes | กลาง | อย่าเครดิตทั้งหมดแบบเหมารวม ให้เครดิตเฉพาะ asset ที่ build ใช้จริง |

## 5. ตาราง Asset ที่ไม่ทราบแหล่งที่มาหรือ License

| ลำดับ | Asset | ตำแหน่ง | ข้อมูลที่พบ | ข้อมูลที่ขาด | ความเสี่ยง | สิ่งที่ควรตรวจสอบต่อ |
|---:|---|---|---|---|---|---|
| 1 | Audio ทั้งหมด | `Assets/Audio` | มีชื่อไฟล์ที่บ่งชี้ผู้สร้าง/เว็บบางส่วน เช่น Freesound/community/CrunchpixStudio | License รายไฟล์, URL, วันที่ดาวน์โหลด, requirement การให้เครดิต | สูง | ทำ sheet รายไฟล์พร้อม URL และ license ณ วันที่ดาวน์โหลด |
| 2 | Storm Gust font | `Assets/Fonts/storm_gust` | มี `.ttf`, `.otf`, TMP SDF asset | แหล่งดาวน์โหลดและ font license | สูง | หา license จากแหล่งเดิมหรือเปลี่ยนเป็น font ที่ license ชัดเจน |
| 3 | DRAGON HUNTER font | `Assets/Fonts/DRAGON HUNTER` | มี `.otf` และ TMP SDF asset | แหล่งดาวน์โหลดและ font license | สูง | ตรวจว่าใช้เชิงพาณิชย์/แจกจ่ายในเกมได้หรือไม่ |
| 4 | Gemini-generated images | `Assets/Pictures/Icon/...Gemini_Generated_Image...`, `REPORT.md` | ชื่อไฟล์และ `REPORT.md` ระบุ Gemini | Terms/account/date/prompt/source confirmation | ปานกลาง-สูง | ตรวจ Google Gemini terms ของบัญชีที่ใช้และวันที่สร้าง |
| 5 | Enemy/character/model packs | `Assets/Plugins/Enemy`, `Fantasy knight`, `HEROES`, `Stylized Character_Archer` | มี folder/package names และบาง README | Asset Store proof, publisher, license, seat right | สูง | ยืนยันหน้า Asset Store/receipt/Unity organization |
| 6 | Environment packs | `Assets/Plugins/MedievalTownExteriors`, `StoneKeep`, `RPG Tiny Fantasy Forest PBR`, `Dzeruza`, `Holotna` | บาง README ระบุผู้สร้าง/website | License/purchase proof | สูง | ตรวจใบเสร็จและ EULA ของแต่ละ package |
| 7 | VFX packs | `Assets/Plugins/Eric VFX Studio`, `Matthew Guz`, `Mirza Beig`, `Vefects`, `Assets/VFX` | บาง README/contact | License เฉพาะ package | สูง | ตรวจ requirement attribution และ redistribution |
| 8 | UI/icon packs | `Crosshairs`, `GameInputControllerIconsFree`, `Modern GDR`, `GUI_Parts` | บาง README | License/purchase proof | สูง | ตรวจว่าสามารถใช้ใน build/repository ได้หรือไม่ |
| 9 | `_Recovery` HumanM animation/model files | `Assets/_Recovery` | ชื่อไฟล์ลักษณะ Mixamo; `REPORT.md` กล่าวถึง Adobe Mixamo | แหล่งดาวน์โหลดและ license | ปานกลาง | ถ้าไม่ใช้ใน build อาจไม่นำไปเครดิต แต่ควรเก็บ proof หากเผยแพร่ repo |

## 6. ตาราง Unity Package และ Plugin

### 6.1 Direct Unity Package จาก `Packages/manifest.json`

| ลำดับ | Package ID | ชื่อ Package | Version | ผู้พัฒนา | ที่มา | License | ใช้งานด้านใด | Direct หรือ Transitive |
|---:|---|---|---|---|---|---|---|---|
| 1 | `com.unity.2d.sprite` | 2D Sprite | 1.0.0 | Unity Technologies | builtin | Unity package terms; ไม่พบ license file local | Sprite editor/import | Direct |
| 2 | `com.unity.2d.tilemap` | 2D Tilemap | 1.0.0 | Unity Technologies | builtin | Unity package terms | Tilemap module | Direct |
| 3 | `com.unity.ai.navigation` | AI Navigation | 2.0.10 | Unity Technologies | registry `https://packages.unity.com` | Unity package terms | NavMesh/AI navigation | Direct |
| 4 | `com.unity.cinemachine` | Cinemachine | 3.1.5 | Unity Technologies | registry | Unity package terms | Camera system | Direct |
| 5 | `com.unity.collab-proxy` | Version Control/Collab Proxy | 2.11.3 | Unity Technologies | registry | Unity package terms | Editor/version control integration | Direct |
| 6 | `com.unity.formats.fbx` | FBX Exporter | 5.1.5 | Unity Technologies | registry | Unity package terms | FBX import/export workflow | Direct |
| 7 | `com.unity.ide.rider` | Rider Editor | 3.0.39 | Unity Technologies | registry | Unity package terms | IDE integration | Direct |
| 8 | `com.unity.ide.visualstudio` | Visual Studio Editor | 2.0.26 | Unity Technologies | registry | Unity package terms | IDE integration | Direct |
| 9 | `com.unity.inputsystem` | Input System | 1.18.0 | Unity Technologies | registry | Unity package terms | Player input | Direct |
| 10 | `com.unity.multiplayer.center` | Multiplayer Center | 1.0.1 | Unity Technologies | builtin | Unity package terms | Multiplayer setup/help | Direct |
| 11 | `com.unity.netcode.gameobjects` | Netcode for GameObjects | 2.9.2 | Unity Technologies | registry | Unity package terms | Multiplayer networking | Direct |
| 12 | `com.unity.postprocessing` | Post Processing | 3.5.1 | Unity Technologies | registry | Unity package terms | Visual effects/post processing | Direct |
| 13 | `com.unity.recorder` | Recorder | 5.1.5 | Unity Technologies | registry | Unity package terms | Recording/capture | Direct |
| 14 | `com.unity.render-pipelines.universal` | Universal Render Pipeline | 17.3.0 | Unity Technologies | builtin | Unity package terms | Rendering pipeline | Direct |
| 15 | `com.unity.shadergraph` | Shader Graph | 17.3.0 | Unity Technologies | builtin | Unity package terms | Shader authoring | Direct |
| 16 | `com.unity.terrain-tools` | Terrain Tools | 5.3.2 | Unity Technologies | registry | Unity package terms | Terrain editing | Direct |
| 17 | `com.unity.test-framework` | Unity Test Framework | 1.6.0 | Unity Technologies | builtin | Unity package terms | Tests | Direct |
| 18 | `com.unity.timeline` | Timeline | 1.8.10 | Unity Technologies | registry | Unity package terms | Timeline/sequencing | Direct |
| 19 | `com.unity.ugui` | Unity UI | 2.0.0 | Unity Technologies | builtin | Unity package terms | UI | Direct |
| 20 | `com.unity.visualscripting` | Visual Scripting | 1.9.9 | Unity Technologies | registry | Unity package terms | Visual scripting | Direct |
| 21 | `com.unity.modules.*` | Unity built-in modules | 1.0.0 | Unity Technologies | builtin | Unity Editor/module terms | Engine modules เช่น audio, physics, terrain, webrequest | Direct builtin modules |

### 6.2 Transitive Package สำคัญจาก `Packages/packages-lock.json`

| ลำดับ | Package ID | Version | ที่มา | Direct หรือ Transitive | หมายเหตุ |
|---:|---|---|---|---|---|
| 1 | `com.autodesk.fbx` | 5.1.3 | registry | Transitive | Dependency ของ FBX exporter |
| 2 | `com.unity.bindings.openimageio` | 1.0.2 | registry | Transitive | Dependency ของ Recorder |
| 3 | `com.unity.burst` | 1.8.27 | registry | Transitive | Dependency chain ของ Collections/Transport |
| 4 | `com.unity.collections` | 2.6.2 | registry | Transitive | Used by Netcode/Recorder/URP dependencies |
| 5 | `com.unity.ext.nunit` | 2.0.5 | builtin | Transitive | Test Framework |
| 6 | `com.unity.mathematics` | 1.3.3 | registry | Transitive | Burst/Transport/Splines |
| 7 | `com.unity.nuget.mono-cecil` | 1.11.6 | registry | Transitive | Netcode/analysis dependency |
| 8 | `com.unity.render-pipelines.core` | 17.3.0 | builtin | Transitive | URP core |
| 9 | `com.unity.render-pipelines.universal-config` | 17.0.3 | builtin | Transitive | URP config |
| 10 | `com.unity.searcher` | 4.9.4 | registry | Transitive | ShaderGraph dependency |
| 11 | `com.unity.settings-manager` | 2.1.1 | registry | Transitive | Splines dependency |
| 12 | `com.unity.splines` | 2.8.2 | registry | Transitive | Cinemachine dependency |
| 13 | `com.unity.transport` | 2.6.0 | registry | Transitive | Netcode dependency |

### 6.3 Plugin/Asset package ใน `Assets/Plugins` และ asset folders

| ลำดับ | Package/Plugin | ตำแหน่ง | ใช้งานใน build | License local | หมายเหตุ |
|---:|---|---|---|---|---|
| 1 | ParrelSync | `Assets/Plugins/ParrelSync` | ไม่พบ | ไม่พบ | Editor-only plugin |
| 2 | Crosshairs | `Assets/Plugins/Crosshairs` | พบ | ไม่พบ | มี README/contact |
| 3 | WaterWorks | `Assets/Plugins/WaterWorks` | พบและ runtime-load | ไม่พบ | มี `Resources.Load("Water_Volume")` |
| 4 | TextMesh Pro resources | `Assets/TextMesh Pro` | พบ | บางไฟล์มี attribution/license | แยกจาก UPM package |
| 5 | VegetationSpawner | `Assets/VegetationSpawner` | ไม่พบใน build | ไม่พบ | มี editor/runtime/demo |
| 6 | Viapix HealingItem | `Assets/ViapixGames/HealingItem` | พบ | ไม่พบจาก PDF ยังไม่ได้อ่าน | ใช้เป็น healing pack |

## 7. รายการเครดิตสำหรับใส่ในรายงาน

รายการต่อไปนี้เป็นแบบร่างที่ใช้ได้เฉพาะรายการที่มีข้อมูลพอจาก repo. รายการที่ยังขาด license ต้องเติมก่อนส่งรายงานจริง

> โครงการนี้ใช้ Fantasy Skybox FREE จาก Render Knight โดยพบข้อมูลแหล่งเผยแพร่ใน `Assets/Plugins/Fantasy Skybox FREE/Readme.txt`; ต้องยืนยัน license และเงื่อนไขการใช้งานจาก Unity Asset Store ก่อนเผยแพร่

> โครงการนี้ใช้ Medieval Town Exteriors และ StoneKeep จัดทำโดย Lylek Games โดยพบข้อมูลใน `Assets/Plugins/MedievalTownExteriors/Readme.txt` และ `Assets/Plugins/StoneKeep/Readme.txt`; ต้องยืนยัน purchase proof และ license ก่อนเผยแพร่

> โครงการนี้ใช้ Massive Crosshair Bundle / Crosshairs โดยพบข้อมูลติดต่อ OccaSoftware ใน `Assets/Plugins/Crosshairs/Readme.txt`; ต้องยืนยัน license ก่อนเผยแพร่

> โครงการนี้ใช้ Modern GDR - Free icons pack โดยพบชื่อ Jennifer ใน `Assets/Plugins/Modern GDR - Free icons pack/README.md`; ต้องยืนยัน license ก่อนเผยแพร่

> โครงการนี้ใช้ Free Game VFX โดยพบข้อมูลติดต่อ Eric Wang / Eric VFX Studio ใน `Assets/Plugins/Eric VFX Studio/Free Game VFX/Readme.txt`; ต้องยืนยัน license ก่อนเผยแพร่

> โครงการนี้ใช้ Slash Effects FREE โดยพบข้อมูลติดต่อ Matthew Guz ใน `Assets/Plugins/Matthew Guz/Slash Effects FREE/Documentation/Readme IMPORTANT.txt`; ต้องยืนยัน license ก่อนเผยแพร่

> โครงการนี้ใช้ TextMesh Pro และ sample resources บางส่วน โดย EmojiOne sprite sample มี attribution file ที่ `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt` และ Roboto-Bold sample font ระบุ Apache License 2.0 ใน `Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold - License.txt`

> โครงการนี้ใช้ shader component ของ Cyanilux ภายใต้ MIT License เฉพาะไฟล์ใน `Assets/Plugins/Stylized Character_Archer/ToonShader/Shader/CustomLighting/LICENSE`

> โครงการนี้ใช้ Unity packages จาก Unity Package Manager เช่น Netcode for GameObjects, Cinemachine, Input System, URP, Shader Graph, Timeline และ UGUI ตามรายการใน `Packages/manifest.json`

## 8. รายการอ้างอิงแบบพร้อมใช้งาน

รายการนี้ไม่สร้างปี/URL/license เพิ่มเอง ถ้าไม่มีใน repo จะใช้ "ม.ป.ป." และ path หลักฐาน

- Unity Technologies. (ม.ป.ป.). `Packages/manifest.json` และ Unity Package Manager dependencies. หลักฐาน: `Packages/manifest.json`, `Packages/packages-lock.json`
- Render Knight. (ม.ป.ป.). Fantasy Skybox FREE. หลักฐาน: `Assets/Plugins/Fantasy Skybox FREE/Readme.txt`
- Lylek Games. (ม.ป.ป.). Medieval Town Exteriors. หลักฐาน: `Assets/Plugins/MedievalTownExteriors/Readme.txt`
- Lylek Games. (ม.ป.ป.). Medieval Stone Keep. หลักฐาน: `Assets/Plugins/StoneKeep/Readme.txt`
- Jennifer. (ม.ป.ป.). Modern GDR - Free icons pack. หลักฐาน: `Assets/Plugins/Modern GDR - Free icons pack/README.md`
- OccaSoftware. (ม.ป.ป.). Massive Crosshair Bundle. หลักฐาน: `Assets/Plugins/Crosshairs/Readme.txt`
- Eric Wang / Eric VFX Studio. (ม.ป.ป.). Free Game VFX. หลักฐาน: `Assets/Plugins/Eric VFX Studio/Free Game VFX/Readme.txt`
- Matthew Guz. (ม.ป.ป.). Slash Effects FREE. หลักฐาน: `Assets/Plugins/Matthew Guz/Slash Effects FREE/Documentation/Readme IMPORTANT.txt`
- Julien Tonsuso. (ม.ป.ป.). Modular Fantasy Bridges. หลักฐาน: `Assets/Plugins/Julien Tonsuso/Modular Fantasy Bridges/README.txt`
- Cyanilux. (2020). Custom Lighting shader component. หลักฐาน: `Assets/Plugins/Stylized Character_Archer/ToonShader/Shader/CustomLighting/LICENSE`
- EmojiOne. (ม.ป.ป.). EmojiOne sample sprites. หลักฐาน: `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`
- Apache Software Foundation. (ม.ป.ป.). Apache License, Version 2.0 for Roboto-Bold sample font. หลักฐาน: `Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold - License.txt`
- The Great Commander team. (ม.ป.ป.). Project README and report draft. หลักฐาน: `README.MD`, `REPORT.md`

## 9. ความเสี่ยงและคำแนะนำ

| ระดับ | ประเด็น | Asset/ตำแหน่ง | เหตุผล | คำแนะนำ |
|---|---|---|---|---|
| วิกฤต | ไม่พบ license ของ audio ที่ใช้งานจริง | `Assets/Audio` | เพลง/เสียงถูกใช้งานใน build dependency แต่ไม่มี license รายไฟล์ | ห้ามเผยแพร่ build สาธารณะจนกว่าจะทำ audio license sheet ครบ |
| สูง | Font license ไม่ชัดเจน | `Assets/Fonts/storm_gust`, `Assets/Fonts/DRAGON HUNTER` | Font ถูกใช้ใน scene/UI และอาจมีเงื่อนไข commercial/embed | หา license หรือเปลี่ยนเป็น OFL/Apache/Font ที่ยืนยันได้ |
| สูง | Asset Store-style packages ไม่มี purchase proof ใน repo | `Assets/Plugins/*`, `Assets/VFX/*`, `Assets/ViapixGames/*` | พบใช้งานจริงแต่ไม่มีใบเสร็จ/seat/license proof | ตรวจ Unity Asset Store account, invoice, organization seat |
| สูง | AI-generated image terms ไม่ยืนยัน | `Assets/Pictures/...Gemini_Generated_Image...` | `REPORT.md` และชื่อไฟล์บ่งชี้ Gemini แต่ไม่มี terms/date/account | ตรวจ terms ของ Gemini ณ วันที่สร้างและสิทธิ์ใช้งาน |
| ปานกลาง | Demo/unused assets อยู่ใน repo จำนวนมาก | `DoubleL`, `VegetationSpawner`, `_Recovery`, demo scenes | อาจเพิ่มภาระเครดิตและ license risk แม้ไม่อยู่ใน build | แยก folder demo/unused ออกจาก release หรือจัดทำบัญชีแยก |
| ปานกลาง | Runtime loading อาจตกหล่น | `WaterWorks`, scene loading scripts | พบ `Resources.Load("Water_Volume")` และ scene name loading | ใช้ Unity Editor scan ด้วย `AssetDatabase.GetDependencies()` เพิ่ม |
| ปานกลาง | Package license หลายชั้น | UPM packages/transitive dependencies | `packages-lock.json` มี registry/builtin/transitive dependencies | ตรวจ Unity Package license/third-party notices ของแต่ละ package |
| ต่ำ | Project-specific scripts/scenes | `Assets/Scripts`, `Assets/Scenes` | ดูเป็นงานของทีมจาก README/project structure | เก็บ commit history/author record เป็นหลักฐาน |

## 10. สิ่งที่ AI ยังยืนยันไม่ได้

- ประวัติการซื้อจาก Unity Asset Store
- ใบเสร็จและ Unity organization seat license
- License ณ วันที่ดาวน์โหลดของแต่ละ asset
- สิทธิ์บัญชีผู้ซื้อและสิทธิ์ redistribution ของ source asset ใน repository
- Asset ที่ถูกนำเข้ามาแล้วเปลี่ยนชื่อ
- Asset ที่ถูกแก้ไขจนระบุแหล่งที่มายาก
- License รายไฟล์ของ audio จาก Freesound/แหล่งเสียงอื่น
- Font license ของ Storm Gust และ DRAGON HUNTER
- Terms ของ Gemini-generated images ณ วันที่สร้าง
- Metadata ฝังในไฟล์ `.fbx`, `.png`, `.mp3`, `.wav`, `.pdf`
- Runtime asset ที่โหลดจาก server ภายนอกหรือ path ที่ประกอบจาก dynamic string
- Asset ที่ไม่ได้อยู่ใน repository แต่ถูกใช้ตอน build/deploy
- ความถูกต้องของ license ที่กล่าวใน `REPORT.md` เพราะเอกสารนั้นเองระบุว่าต้องยืนยันก่อนส่งฉบับสมบูรณ์

## ข้อเสนอแนะขั้นต่อไป

1. รัน Unity Editor scan จริงด้วย `AssetDatabase.GetDependencies()` และ export CSV/JSON ตามข้อกำหนดใน `ref.md`
2. ทำ `asset_audit.csv` แยก asset group, usage evidence, source, publisher, license, URL, credit requirement, confidence
3. ตรวจ Unity Asset Store purchase history และแนบ receipt/license proof สำหรับทุก package ใน `Assets/Plugins`, `Assets/VFX`, `Assets/ViapixGames`
4. ทำ audio license sheet รายไฟล์ก่อนเผยแพร่ build
5. ย้าย demo/unused package ออกจาก release build หรือแยกไว้ใน documentation ว่าไม่รวมในเกม
