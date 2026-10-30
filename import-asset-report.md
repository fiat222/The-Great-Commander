# รายงาน Imported Asset สำหรับอ้างอิงในรายงานโครงการ

วันที่จัดทำ: 12 กรกฎาคม 2569  
โปรเจกต์: `The-Great-Commander`  
ขอบเขต: รายงานนี้คัดเฉพาะ asset/package ที่มีลักษณะเป็น imported หรือ external asset ไม่รวม scene, prefab, script และ data ที่ทีมสร้างเอง ยกเว้นกรณีที่ไฟล์ของทีมอ้างอิง imported asset นั้นจริง

## หลักฐานที่ใช้

- ผล Unity Editor scan: `Assets/AssetAudit/used_assets.csv`
- ผล Unity Editor scan: `Assets/AssetAudit/used_assets.json`
- Package manifest: `Packages/manifest.json`
- Package lock: `Packages/packages-lock.json`
- README/LICENSE/Attribution ที่พบในโฟลเดอร์ asset
- Firecrawl search result files ใน `.firecrawl/import-*.json`

## สรุปจาก Unity Asset Audit

| รายการ | จำนวน |
|---|---:|
| Project assets ทั้งหมด | 4,947 |
| Creditable assets | 3,949 |
| Used/reference-detected assets | 3,662 |
| Unreferenced assets | 287 |
| Imported/external asset rows ที่ตรวจพบ | 3,155 |
| Imported/external asset rows ที่อยู่ใน Build Settings dependency | 651 |
| Package rows จาก `Packages/` | 298 |
| Resources rows | 63 |
| StreamingAssets rows | 0 |

หมายเหตุ: จำนวนข้างต้นเป็นจำนวนไฟล์ asset หลังรวม dependency ไม่ใช่จำนวน package. ในรายงานอ้างอิงควรรวมเป็นชุด asset/package ไม่ควรอ้างอิงทุกไฟล์ย่อย

## Imported Asset ที่ใช้งานจริงใน Build

ตารางนี้เรียงตามจำนวน asset ที่ถูกอ้างอิงจาก Build Settings scenes. ช่อง License ระบุเฉพาะสิ่งที่พบหลักฐานได้ หากไม่พบจะระบุให้ตรวจสอบเพิ่ม

| ลำดับ | Asset / Package | ประเภท | ตำแหน่งในโปรเจกต์ | จำนวนไฟล์ที่พบใช้ใน Build | ถูกใช้งานโดย / หลักฐาน | แหล่งอ้างอิงเบื้องต้น | ผู้เผยแพร่ที่พบ | License / เงื่อนไข | หมายเหตุ |
|---:|---|---|---|---:|---|---|---|---|---|
| 1 | Enemy character packs | Character, model, animation, material | `Assets/Plugins/Enemy` | 64 | `Assets/DefaultNetworkPrefabs.asset`, enemy prefab/controller dependencies | ไม่พบ URL เดียวที่ยืนยันทั้งชุด | ต้องตรวจแยกราย pack | ไม่พบข้อมูล License ครบถ้วน | เป็นกลุ่มเสี่ยงสูง ต้องแยก Skeleton/Demon/Goblin/Orc/Ogre ตามแหล่งจริง |
| 2 | Medieval Stone Keep / StoneKeep | Environment, castle props | `Assets/Plugins/StoneKeep` | 64 | `Assets/Scenes/GameScene.unity`, `Assets/Scenes/SoloGameScene.unity`, StoneKeep prefabs | https://assetstore.unity.com/packages/3d/environments/medieval-stone-keep-56596 | Lylek Games จาก README local | ต้องตรวจ Unity Asset Store EULA และหลักฐานการซื้อ | README local: `Assets/Plugins/StoneKeep/Readme.txt` |
| 3 | Hand Painted Nature Kit LITE | Environment, terrain, nature props | `Assets/Plugins/Silver_Cats/Hand_Painted_Nature_Kit_LITE` | 56 | terrain/material dependencies ใน build scenes | https://assetstore.unity.com/packages/3d/environments/hand-painted-nature-kit-lite-69220 | Silver_Cats ตาม path | ต้องตรวจ Unity Asset Store EULA และหลักฐานการซื้อ | มี readme local แต่ยังไม่พบ license text |
| 4 | Trails VFX URP | VFX, trail effects, resources | `Assets/Plugins/Vefects/Trails VFX URP` | 55 | VFX/material/prefab dependencies | https://assetstore.unity.com/packages/vfx/trails-vfx-urp-242574 | Vefects | ต้องตรวจ Unity Asset Store EULA และหลักฐานการซื้อ | มี runtime/resource dependency บางส่วน |
| 5 | Medieval Town Exteriors | Environment, town props | `Assets/Plugins/MedievalTownExteriors` | 27 | `Assets/Scenes/GameScene.unity`, `Assets/Scenes/SoloGameScene.unity` | https://assetstore.unity.com/packages/3d/environments/fantasy/medieval-town-exteriors-27026 | Lylek Games จาก README local | ต้องตรวจ Unity Asset Store EULA และหลักฐานการซื้อ | README local: `Assets/Plugins/MedievalTownExteriors/Readme.txt` |
| 6 | Imported SFX audio set | Audio SFX | `Assets/Audio/Sfx` | 26 | player/enemy/audio manager dependencies | บางไฟล์มีชื่อ Freesound/creator ใน filename | หลายผู้สร้าง ยังไม่ยืนยัน | ต้องตรวจ license รายไฟล์ | ห้ามสรุปเป็น license เดียว ต้องทำ audio sheet แยก |
| 7 | HEROES | Character/model/animation | `Assets/Plugins/HEROES` | 24 | animation controller/material/model dependencies | ไม่พบแหล่งที่ยืนยันชัดเจนใน Firecrawl รอบนี้ | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจที่มาจากทีม/Asset Store เดิม |
| 8 | GUI_Parts | UI graphics | `Assets/Plugins/GUI_Parts` | 24 | `Assets/Canvas/Canvas.prefab`, UI prefabs | ไม่พบแหล่งที่ยืนยันชัดเจน | ไม่พบ | ไม่พบข้อมูล License | มีภาพบางไฟล์ชื่อคล้าย AI-generated ต้องตรวจสิทธิ์เพิ่ม |
| 9 | Holotna Mountain | Environment, mountain/bridge/rock | `Assets/Plugins/Holotna/Mountain` | 22 | build scene material/terrain dependencies | ไม่พบแหล่งที่ยืนยันชัดเจน | Holotna ตาม path | ไม่พบข้อมูล License | ต้องตรวจแหล่งเดิม |
| 10 | Game Input Controller Icons Free | UI icons | `Assets/Plugins/GameInputControllerIconsFree` | 18 | `Assets/Canvas/Canvas.prefab`, `Assets/Canvas/SettingCanvas.prefab` | https://assetstore.unity.com/packages/2d/gui/icons/game-input-controller-icons-free-285953 | ต้องตรวจจาก Asset Store page | ต้องตรวจ Unity Asset Store EULA | มี readme PDF local: `Assets/Plugins/GameInputControllerIconsFree/readme.pdf` |
| 11 | Adobe Mixamo warrior animation/model set | Character model, animation, material, Animator Controller | `Assets/Prefabs/Player/Warrior/Mixamo` | 18 | warrior prefab/controller dependencies; `PYWarriorController.controller` และ animation clips ถูกอ้างอิงใน Build Settings scenes | https://www.mixamo.com/ | Adobe Mixamo | ต้องตรวจ Adobe/Mixamo terms และบัญชีที่ดาวน์โหลด | พบ `HandsomePlayer (1).fbx`, animation clips, materials และ controller ใช้งานจริงใน build |
| 12 | Free Game VFX | VFX, particle effects | `Assets/Plugins/Eric VFX Studio/Free Game VFX` | 15 | VFX prefab/material dependencies | https://unityvfx.weebly.com/ | Eric Wang / Eric VFX Studio จาก README local | ไม่พบข้อมูล License local | README local: `Assets/Plugins/Eric VFX Studio/Free Game VFX/Readme.txt` |
| 13 | Inferno VFX | VFX, fire/explosion materials | `Assets/Plugins/Mirza Beig/Particle Systems/Inferno VFX` | 14 | VFX material dependencies | https://assetstore.unity.com/packages/vfx/particles/fire-explosions/inferno-vfx-50735 | Mirza Beig ตาม path | ต้องตรวจ Unity Asset Store EULA และหลักฐานการซื้อ | Firecrawl พบหน้า Asset Store/reviews |
| 14 | CliffStonePackM | Environment, cliff/stone models | `Assets/Plugins/Dzeruza/CliffStonePackM` | 14 | `Assets/Scenes/GameScene.unity`, build scene dependencies | ไม่พบ URL ที่ชัดเจนใน Firecrawl รอบนี้ | Dzeruza ตาม path | ไม่พบข้อมูล License | มี PDF documentation local |
| 15 | Scene Teleportation | Portal/teleportation props | `Assets/Plugins/Scene Teleportation Kit` | 13 | portal/ground material/prefab dependencies | https://assetstore.unity.com/packages/3d/props/tools/scene-teleportation-202903 | ต้องตรวจจาก Asset Store page | ต้องตรวจ Unity Asset Store EULA | ชื่อใน project คือ `Scene Teleportation Kit` |
| 16 | Thundercloud | VFX, lightning/cloud | `Assets/Plugins/FlexUnit/Thundercloud` | 13 | animator/material/prefab dependencies | ไม่พบ URL ที่ชัดเจนใน Firecrawl รอบนี้ | FlexUnit ตาม path | ไม่พบข้อมูล License | มี `Assets/Plugins/FlexUnit/Thundercloud/Readme.pdf` |
| 17 | Vegetation Spawner demo assets | Vegetation, terrain/nature | `Assets/VegetationSpawner` | 12 | build scene dependencies | ไม่ค้นเว็บเพิ่ม | Staggart Creations/SC อาจเกี่ยวข้องจาก asmdef `sc.vegetationspawner` แต่ยังไม่ยืนยัน | ไม่พบข้อมูล License | ต้องตรวจว่าตั้งใจใช้ demo asset ใน build หรือไม่ |
| 18 | Magic Effects FREE | VFX, magic spell effects | `Assets/VFX/Hovl Studio/Magic effects pack` | 11 | player/tower VFX dependencies | https://assetstore.unity.com/packages/vfx/particles/spells/magic-effects-free-247933 | Hovl Studio | ต้องตรวจ Unity Asset Store EULA | Firecrawl พบ publisher page ด้วย |
| 19 | Free Quick Effects Vol. 1 | VFX, particle effects | `Assets/Plugins/GabrielAguiarProductions/FreeQuickEffectsVol1` | 11 | VFX material/prefab dependencies | https://assetstore.unity.com/packages/vfx/particles/free-quick-effects-vol-1-304424 | Gabriel Aguiar Productions | ต้องตรวจ Unity Asset Store EULA | ใช้ใน build dependency 11 files |
| 20 | RPG Tiny Fantasy Forest PBR | Environment, fantasy forest props | `Assets/Plugins/RPG Tiny Fantasy Forest PBR` | 9 | terrain/nature prefab/material dependencies | https://assetstore.unity.com/packages/3d/environments/fantasy/rpg-tiny-fantasy-forest-pbr-309458 | ต้องตรวจจาก Asset Store page | ต้องตรวจ Unity Asset Store EULA | Firecrawl พบหน้า Asset Store |
| 21 | Universal Render Pipeline assets | Unity package | `Packages/com.unity.render-pipelines.universal` | 8 | render pipeline asset dependencies | `Packages/manifest.json`, `Packages/packages-lock.json` | Unity Technologies | Unity package terms | Package ID: `com.unity.render-pipelines.universal` v17.3.0 |
| 22 | WaterWorks | Water shader/material/resource | `Assets/Plugins/WaterWorks` | 8 | `Resources.Load("Water_Volume")`, water materials | ไม่พบ URL ชัดเจน; Firecrawl เจอผลทั่วไปเกี่ยวกับ water assets | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจแหล่งที่นำเข้าเดิม |
| 23 | Cartoon FX legacy | VFX | `Assets/VFX/JMO Assets/Cartoon FX (legacy)` | 7 | `Assets/Scenes/MenuSceneTest.unity`, VFX prefab/material dependencies | ไม่ค้นเว็บเพิ่ม | JMO Assets ตาม path | ไม่พบข้อมูล License | ต้องตรวจ package/source เดิม |
| 24 | Stylized Character Archer | Character/model/material | `Assets/Plugins/Stylized Character_Archer` | 7 | `Assets/Scenes/GameScene.unity`, archer prefab dependencies | https://assetstore.unity.com/packages/3d/characters/stylized-modular-archer-female-225768 | ต้องตรวจว่าเป็น asset เดียวกับใน project หรือไม่ | Character pack license ต้องตรวจเพิ่ม; shader subcomponent มี MIT local | ไฟล์ shader license: `Assets/Plugins/Stylized Character_Archer/ToonShader/Shader/CustomLighting/LICENSE` |
| 25 | Sword Two-Hander | Weapon model/material | `Assets/Plugins/Sword Two-Hander` | 7 | sword material/model dependencies | ไม่พบ URL ที่ยืนยันชัดเจนใน Firecrawl รอบนี้ | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจแหล่งเดิม |
| 26 | Enemy icons | UI icons/images | `Assets/Pictures/Icon/Enemy`, `Assets/Pictures/*.png` | 18+ | shop/wave/UI dependencies | ไม่พบแหล่งที่ยืนยันชัดเจน | ไม่พบ | ไม่พบข้อมูล License | ภาพอาจเป็น AI-generated/imported ต้องตรวจทีละไฟล์ |
| 27 | KE Statues - Lite | Environment/statue props | `Assets/Plugins/KE Statues - Lite` | 6 | statue material/prefab dependencies | ไม่ค้นเว็บเพิ่ม | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจแหล่งเดิม |
| 28 | Stylized Lava Materials | Material/texture | `Assets/Plugins/Stylized Lava Materials` | 6 | lava material dependencies | ไม่ค้นเว็บเพิ่ม | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจแหล่งเดิม |
| 29 | Mini Legion Footman PBR HP Polyart | Character/model/animation | `Assets/Plugins/Mini Legion Footman PBR HP Polyart` | 6 | footman prefab/controller dependencies | https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/mini-legion-footman-pbr-hp-polyart-86576 | ต้องตรวจจาก Asset Store page | ต้องตรวจ Unity Asset Store EULA | Firecrawl พบหน้า Asset Store |
| 30 | Modern RPG - Free icons pack | UI icons | `Assets/Plugins/Modern GDR - Free icons pack` | 6 | `Assets/Canvas/Canvas.prefab`, `PhaseTxt.prefab`, `SettingCanvas.prefab` | https://assetstore.unity.com/packages/2d/gui/icons/modern-rpg-free-icons-pack-264706 | Jennifer จาก README local | ต้องตรวจ Unity Asset Store EULA | README local: `Assets/Plugins/Modern GDR - Free icons pack/README.md` |
| 31 | Kevin Iglesias Human/Giant assets | Animation/model | `Assets/Plugins/Kevin Iglesias` | 5 | humanoid model/animation dependencies | https://assetstore-fallback.unity.com/publishers/36307 | Kevin Iglesias | ต้องตรวจ asset page และ purchase proof | `REPORT.md` เดิมระบุ Human Animations/Humanoid Giant |
| 32 | Fantasy Skybox FREE | Skybox/material/texture | `Assets/Plugins/Fantasy Skybox FREE` | 5 | skybox material dependencies | https://assetstore.unity.com/packages/2d/textures-materials/sky/fantasy-skybox-free-18353 | Render Knight จาก README local | ต้องตรวจ Unity Asset Store EULA | README local: `Assets/Plugins/Fantasy Skybox FREE/Readme.txt` |
| 33 | Fantasy knight | Character/model/material | `Assets/Plugins/Fantasy knight` | 5 | knight model/material dependencies | ไม่พบ URL ที่ยืนยันชัดเจนใน Firecrawl รอบนี้ | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจแหล่งเดิม |
| 34 | TextMesh Pro resources | Font, shader, sprite resources | `Assets/TextMesh Pro` | 4 | UI dependencies | Unity package/local imported TMP resources | Unity Technologies; EmojiOne for sample sprite | Roboto sample: Apache 2.0; EmojiOne terms ต้องตรวจ | หลักฐาน local: `EmojiOne Attribution.txt`, `Roboto-Bold - License.txt` |
| 35 | RPG Monster DUO PBR Polyart | Character/model/animation | `Assets/Plugins/RPG Monster DUO PBR Polyart` | 4 | monster prefab/controller dependencies | ไม่ค้นเว็บเพิ่ม | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจแหล่งเดิม |
| 36 | Massive Crosshair Bundle / Crosshairs | UI/crosshair textures | `Assets/Plugins/Crosshairs` | 4 | `Assets/Canvas/Canvas.prefab`, game UI dependencies | https://www.occasoftware.com/ | OccaSoftware จาก README local | ไม่พบข้อมูล License local | Firecrawl ไม่พบหน้า exact free bundle; พบ publisher/site |
| 37 | LowPolyRPGWeapons_Lite | Weapon model/material | `Assets/Plugins/LowPolyRPGWeapons_Lite` | 4 | weapon prefab/material dependencies | ผล Firecrawl พบ `Low Poly Weapons` แต่ยังไม่ยืนยันว่าเป็น package เดียวกัน | ไม่พบ | ไม่พบข้อมูล License | ต้องตรวจว่า URL ตรงกับ package ใน project หรือไม่ |
| 38 | Storm Gust font | Font/TMP font asset | `Assets/Fonts/storm_gust` | 2 | `CharacterSelectScene`, `SoloCharactor` | ไม่ค้นเว็บเพิ่ม | ไม่พบ | ไม่พบข้อมูล License | Font license ต้องตรวจเป็นพิเศษ |
| 39 | DRAGON HUNTER font | Font/TMP font asset | `Assets/Fonts/DRAGON HUNTER` | 2 | `CharacterSelectScene`, `GameScene` | ไม่ค้นเว็บเพิ่ม | ไม่พบ | ไม่พบข้อมูล License | Font license ต้องตรวจเป็นพิเศษ |
| 40 | Gemini-generated images / UI images | Image/UI | `Assets/Pictures/Icon/Skill`, `Assets/Pictures/Icon/losewin`, `Assets/Pictures/Icon/Player`, `Assets/Pictures/Icon/Menu` | หลายไฟล์ | character select, game UI, win/lose UI | ไม่ค้นเว็บ เพราะเป็น generated/local file | Google Gemini ถูกระบุจาก filename/`REPORT.md` แต่ยังไม่ยืนยัน | ต้องตรวจ terms ของบัญชีและวันที่สร้าง | ห้ามอ้าง license โดยไม่มีหลักฐาน |
| 41 | ViapixGames HealingItem | Pickup/healing item | `Assets/ViapixGames/HealingItem` | 2 | `Assets/Prefabs/Pack/Healing Pack.prefab`, demo dependencies | ไม่ค้นเว็บเพิ่ม | ViapixGames ตาม path | ไม่พบข้อมูล License | มี PDF local: `Assets/ViapixGames/HealingItem/Documentation/HealingItemReadMe.pdf` |

## Unity Package ที่ควรอ้างอิงในรายงาน

รายการนี้มาจาก `Packages/manifest.json` และ `Packages/packages-lock.json`

| Package ID | Version | ผู้พัฒนา | ใช้งานด้านใด | หลักฐาน |
|---|---:|---|---|---|
| `com.unity.netcode.gameobjects` | 2.9.2 | Unity Technologies | Multiplayer networking | `Packages/manifest.json` |
| `com.unity.cinemachine` | 3.1.5 | Unity Technologies | Camera system | `Packages/manifest.json` |
| `com.unity.inputsystem` | 1.18.0 | Unity Technologies | Input handling | `Packages/manifest.json` |
| `com.unity.render-pipelines.universal` | 17.3.0 | Unity Technologies | Universal Render Pipeline | `Packages/manifest.json` |
| `com.unity.shadergraph` | 17.3.0 | Unity Technologies | Shader Graph | `Packages/manifest.json` |
| `com.unity.ai.navigation` | 2.0.10 | Unity Technologies | NavMesh/AI navigation | `Packages/manifest.json` |
| `com.unity.postprocessing` | 3.5.1 | Unity Technologies | Post-processing | `Packages/manifest.json` |
| `com.unity.timeline` | 1.8.10 | Unity Technologies | Timeline | `Packages/manifest.json` |
| `com.unity.ugui` | 2.0.0 | Unity Technologies | UI | `Packages/manifest.json` |

## เครดิตพร้อมนำไปใช้ในรายงาน

ใช้ข้อความต่อไปนี้ได้เป็นร่างเครดิต โดยต้องเติม License หลังตรวจ Asset Store / ใบเสร็จ / เงื่อนไข ณ วันที่ดาวน์โหลดแล้ว

> โครงการนี้ใช้ Medieval Stone Keep และ Medieval Town Exteriors โดย Lylek Games เพื่อประกอบฉากเมืองและปราสาทยุคกลาง โดยพบการใช้งานในฉากหลักของเกม และต้องตรวจสอบสิทธิ์การใช้งานจาก Unity Asset Store EULA และหลักฐานการซื้อของทีม

> โครงการนี้ใช้ Hand Painted Nature Kit LITE, RPG Tiny Fantasy Forest PBR, CliffStonePackM และ Holotna Mountain เพื่อประกอบสภาพแวดล้อม ภูมิประเทศ หิน ต้นไม้ และฉากธรรมชาติ โดยต้องตรวจสอบ License ของแต่ละ asset package ก่อนเผยแพร่เกม

> โครงการนี้ใช้ VFX packages เช่น Trails VFX URP, Free Game VFX, Inferno VFX, Magic Effects FREE, Free Quick Effects Vol. 1, Slash Effects FREE และ Cartoon FX legacy เพื่อสร้างเอฟเฟกต์การโจมตี เวทมนตร์ ไฟ trail และเอฟเฟกต์ประกอบฉาก โดยต้องตรวจสอบ License และเงื่อนไขการให้เครดิตของแต่ละ package

> โครงการนี้ใช้ imported character/weapon assets เช่น Adobe Mixamo warrior animation/model set, Stylized Character Archer, Fantasy knight, HEROES, Mini Legion Footman PBR HP Polyart, RPG Monster DUO PBR Polyart, Kevin Iglesias Human/Giant assets, Sword Two-Hander และ LowPolyRPGWeapons_Lite เพื่อประกอบโมเดลตัวละคร animation ศัตรู มินเนียน และอาวุธ โดยต้องตรวจสอบแหล่งที่มาและ License ราย package

> โครงการนี้ใช้ UI/icon assets เช่น Game Input Controller Icons Free, Modern RPG - Free icons pack, Crosshairs, GUI_Parts และภาพ/icon ภายใน `Assets/Pictures` เพื่อประกอบ UI, cursor, icon ปุ่มควบคุม, icon ตัวละคร, icon skill และภาพ win/lose โดยต้องตรวจสอบ License ของแต่ละชุด โดยเฉพาะภาพที่สร้างด้วย AI หรือไม่มีแหล่งที่มาชัดเจน

> โครงการนี้ใช้ไฟล์เสียงใน `Assets/Audio/Bgm` และ `Assets/Audio/Sfx` สำหรับเพลงและ sound effects ภายในเกม โดยต้องตรวจสอบ License รายไฟล์ก่อนเผยแพร่ เนื่องจากชื่อไฟล์บางส่วนบ่งชี้ว่ามาจากหลายผู้สร้างหรือหลายแหล่ง

> โครงการนี้ใช้ TextMesh Pro resources และ font ภายนอก เช่น Storm Gust และ DRAGON HUNTER เพื่อแสดงข้อความใน UI โดยต้องตรวจสอบ font license ก่อนเผยแพร่ build

> โครงการนี้ใช้ Unity packages จาก Unity Package Manager เช่น Netcode for GameObjects, Cinemachine, Input System, Universal Render Pipeline, Shader Graph, AI Navigation, Timeline และ UGUI ตามรายการใน `Packages/manifest.json`

## รายการอ้างอิงพร้อมใช้

รูปแบบนี้ไม่ใส่ปีเผยแพร่หากยังไม่พบหลักฐานชัดเจน และไม่สรุป License เอง

- Unity Technologies. (ม.ป.ป.). *Unity Package Manager dependencies*. หลักฐาน: `Packages/manifest.json`, `Packages/packages-lock.json`
- Adobe. (ม.ป.ป.). *Mixamo*. URL: https://www.mixamo.com/
- Lylek Games. (ม.ป.ป.). *Medieval Stone Keep*. Unity Asset Store. URL: https://assetstore.unity.com/packages/3d/environments/medieval-stone-keep-56596
- Lylek Games. (ม.ป.ป.). *Medieval Town Exteriors*. Unity Asset Store. URL: https://assetstore.unity.com/packages/3d/environments/fantasy/medieval-town-exteriors-27026
- Silver_Cats. (ม.ป.ป.). *Hand Painted Nature Kit LITE*. Unity Asset Store. URL: https://assetstore.unity.com/packages/3d/environments/hand-painted-nature-kit-lite-69220
- Vefects. (ม.ป.ป.). *Trails VFX - URP*. Unity Asset Store. URL: https://assetstore.unity.com/packages/vfx/trails-vfx-urp-242574
- Hovl Studio. (ม.ป.ป.). *Magic Effects FREE*. Unity Asset Store. URL: https://assetstore.unity.com/packages/vfx/particles/spells/magic-effects-free-247933
- Gabriel Aguiar Productions. (ม.ป.ป.). *Free Quick Effects Vol. 1*. Unity Asset Store. URL: https://assetstore.unity.com/packages/vfx/particles/free-quick-effects-vol-1-304424
- Mirza Beig. (ม.ป.ป.). *Inferno VFX*. Unity Asset Store. URL: https://assetstore.unity.com/packages/vfx/particles/fire-explosions/inferno-vfx-50735
- Matthew Guz. (ม.ป.ป.). *Slash Effects FREE*. Unity Asset Store. URL: https://assetstore.unity.com/packages/vfx/particles/spells/slash-effects-free-295209
- Render Knight. (ม.ป.ป.). *Fantasy Skybox FREE*. Unity Asset Store. URL: https://assetstore.unity.com/packages/2d/textures-materials/sky/fantasy-skybox-free-18353
- Jennifer. (ม.ป.ป.). *Modern RPG - Free icons pack*. Unity Asset Store. URL: https://assetstore.unity.com/packages/2d/gui/icons/modern-rpg-free-icons-pack-264706
- ไม่พบชื่อผู้เผยแพร่จากไฟล์ local. (ม.ป.ป.). *Game Input Controller Icons Free*. Unity Asset Store. URL: https://assetstore.unity.com/packages/2d/gui/icons/game-input-controller-icons-free-285953
- ไม่พบชื่อผู้เผยแพร่จากไฟล์ local. (ม.ป.ป.). *Scene Teleportation*. Unity Asset Store. URL: https://assetstore.unity.com/packages/3d/props/tools/scene-teleportation-202903
- ไม่พบชื่อผู้เผยแพร่จากไฟล์ local. (ม.ป.ป.). *RPG Tiny Fantasy Forest PBR*. Unity Asset Store. URL: https://assetstore.unity.com/packages/3d/environments/fantasy/rpg-tiny-fantasy-forest-pbr-309458
- Kevin Iglesias. (ม.ป.ป.). *Kevin Iglesias publisher page*. Unity Asset Store. URL: https://assetstore-fallback.unity.com/publishers/36307
- OccaSoftware. (ม.ป.ป.). *OccaSoftware publisher/site*. URL: https://www.occasoftware.com/
- Eric Wang / Eric VFX Studio. (ม.ป.ป.). *ERIC VFX STUDIO*. URL: https://unityvfx.weebly.com/
- Cyanilux. (2020). *Custom Lighting shader component*. หลักฐาน: `Assets/Plugins/Stylized Character_Archer/ToonShader/Shader/CustomLighting/LICENSE`
- EmojiOne. (ม.ป.ป.). *EmojiOne sample sprites*. หลักฐาน: `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`
- Apache Software Foundation. (ม.ป.ป.). *Apache License, Version 2.0 for Roboto-Bold sample font*. หลักฐาน: `Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold - License.txt`

## รายการที่ยังต้องตรวจสอบก่อนส่งรายงานฉบับสมบูรณ์

- Unity Asset Store purchase history และ invoice ของ asset ทุกชุด
- Seat/license ของบัญชี Unity ที่ซื้อหรือนำเข้า asset
- License ณ วันที่ดาวน์โหลด ไม่ใช่ License ณ วันที่ค้นเว็บ
- License รายไฟล์ของ audio ใน `Assets/Audio`
- Font license ของ Storm Gust และ DRAGON HUNTER
- Terms ของ Gemini-generated images และสิทธิ์ของบัญชีที่สร้างภาพ
- Adobe/Mixamo terms และบัญชีที่ใช้ดาวน์โหลดไฟล์ใน `Assets/Prefabs/Player/Warrior/Mixamo`
- ความตรงกันระหว่างชื่อ folder ใน project กับหน้า Asset Store ที่ Firecrawl พบ โดยเฉพาะ `Stylized Character_Archer`, `LowPolyRPGWeapons_Lite`, `WaterWorks`, `CliffStonePackM`, `HEROES`, `GUI_Parts`, `Holotna`, `Fantasy knight`, `Sword Two-Hander`
- Asset ที่อาจถูก import แล้วเปลี่ยนชื่อหรือย้ายออกจาก folder เดิม
- Runtime-loaded asset ที่อาจไม่พบจาก static dependency หาก path ถูกประกอบจาก string

## คำแนะนำการใช้ในรายงานโครงงาน

- ในรายงานหลักให้ใช้ตาราง grouped package ไม่ต้องลงทุกไฟล์จาก CSV
- ใส่เครดิตเฉพาะ asset ที่ใช้จริงใน build หรือ asset ที่แสดงผลในเกม
- สำหรับ asset ที่ไม่มี License ชัดเจน ให้ใส่หมายเหตุว่า "อยู่ระหว่างตรวจสอบ License" แทนการเดา
- ก่อนเผยแพร่เกมหรือส่ง repository สาธารณะ ควรแนบหลักฐาน purchase/license แยกต่างหาก
