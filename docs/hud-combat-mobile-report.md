# HUD chiến đấu mobile — cụm 5a

**Chưa compile, chạy Unity/game, Device Simulator, máy thật hoặc build Unity/APK.** Chỉ kiểm tra source, đối chiếu diff và tính tọa độ. User tự build/test. Không sửa server, push/merge/stash, xóa branch hay drop stash; chưa commit.

## Git

Đã fetch origin. Branch `feat/hud-combat-mobile` tạo từ `origin/develop` **ba1cc483046eeb38714a694a82e12c5c361cd979** (PR #18). Tree nền khớp hoàn toàn `feat/hud-lists`: **59e7327dd36d4720e3fa0b497532da4e1c1595b0**, diff rỗng.

Chờ user duyệt xóa **local + remote**, chưa xóa:

| Branch | Đã squash merge | Tree branch = tree commit merge |
| --- | --- | --- |
| feat/hud-top-info | ae61050 / #15 | 6cf9c9630b342980407a77ed57c44e8951d77fde |
| feat/hud-top-actions | a4d8c3e / #16 | 6ff69d4b0c3400e981e24545aee34442b6a351bd |
| feat/hud-notification-panel | 97468bd / #17 | b0d376d9f2baaf8455cac8bda10f14b7c48725df |
| feat/hud-lists | ba1cc48 / #18 | 59e7327dd36d4720e3fa0b497532da4e1c1595b0 |

Giữ develop/master/feature/custom-server-address/fix/login-batch3a/fix/ui-batch-c và worktree ui-batch-c như quyết định trước. Giữ stash server `6121b4b` theo báo cáo trước, không thao tác repo server. Đã in lại stash client, cả bốn thuộc danh sách **giữ nguyên**, không có stash thừa đề nghị drop:

| Ref hiện tại | Hash giữ nguyên |
| --- | --- |
| stash@{0} | d1a9a4a242b8bdce69c9f7645adcd2282d9df34a |
| stash@{1} | a9a997d0a600ec9062acb7c7653783aaba17cf20 |
| stash@{2} | dcbb14c83bf39b0950f7ec31d93179cc0a642d6b |
| stash@{3} | 5922203f9a36a4d24d4b2cacf1479ab13726da02 |

Nếu user duyệt drop sau này phải xác minh hash → ref tại thời điểm drop, không dùng số stash đã lưu; in lại list sau thao tác. Logo, Packages, ProjectSettings, hai layout UserSettings dirty sẵn được giữ nguyên; whitespace lỗi sẵn trong layout không sửa.

## HUD mobile và ánh xạ chiêu

`Hud/CombatMobileHud.cs:16/:30/:96`: rect đăng ký một lần qua `GameScr.RegisterHudRects` (`GameScr.cs:658`), order 30, dưới modal 100. Reference 844×390: chiêu 46, ĐÁNH 80, joystick 112/núm 44, item 40. Sau ảnh test đầu, kích thước/lề/khoảng cách/viền cùng scale theo chiều cao logical/390; không đổi MotherCanvas, font, isPC/isTouch. Ví dụ viewport 2340×1080 trước safe-area: chiêu ~127 px, ĐÁNH ~222 px. Item neo Center/Bottom; khi 16:9 gây chồng chiêu 5, dịch cả nhóm trái để giữ gap 6 design px.

Mobile dùng **onScreenSkill**, không keySkill dù Editor Main.isPC=true. Hai mảng/RMS vẫn dài 10 (`GameScr.cs:1263/:1286/:1357/:1396`); tên/ID chiêu phụ thuộc nhân vật và gán thật.

| Nút | Nguồn | Left,top reference |
| --- | --- | --- |
| 1 | onScreenSkill[0] | 685,306 |
| 2 | onScreenSkill[1] | 697,255 |
| 3 | onScreenSkill[2] | 738,222 |
| 4 | onScreenSkill[3] | 790,220 |
| 5 | onScreenSkill[4] | 632,318 |
| 6 | onScreenSkill[5] | 645,239 |
| 7 | onScreenSkill[6] | 699,181 |
| 8 | onScreenSkill[7] | 777,163 |

Ô 9–10 không xóa. Muốn dùng trên mobile, Kỹ năng → phím tắt → gán lại vào ô 1–8 bằng menu gốc/action 11120. Chưa thêm trang/nút chiêu. Nhấn–thả chỉ chọn skill nếu assignment của slot chưa đổi.

`Main.cs:333`, `GameCanvas.cs:594`, `Hud/MobileCombatPointers.cs:46/:79/:128/:149`, `GamePad.cs:50/:300`: native fingerId capture xuyên nhấn–giữ–thả; joystick và chiêu/ĐÁNH độc lập. GamePad dùng thuật toán góc/deadzone/key cũ, rect/radius mới; giữ vùng chạm rộng cũ, hợp thêm vòng hiển thị. Combat không đưa pointer sang chat/map; modal, resize, đổi screen/map/nhân vật, pause hủy capture. Drawing/hover/input/chat avoidance dùng registry.

## Sửa ba lỗi theo ảnh test — 15/10/2026

### 1. Gỡ nút đổi mục tiêu

`CombatMobileHud.cs:63/:82/:130`: xóa constant/registration/rect/action/paint của **combat.focus**, không còn hitbox của nút này. Ngọc rồng/capsule giữ entry ngữ cảnh riêng, chỉ hiện/chặn bấm khi có ngữ cảnh thật; bounds trang trí không chặn pointer. Không tự gán chúng vào ba ô item. Adapter legacy.focus (`GameScr.cs:683`) chỉ phục vụ PC/5b, trả rect rỗng trên mobile. Constructor touch đã đặt cmdFocus=null (`GameScr.cs:1033`).

`Char.findNextFocusByKey` (`Char.cs:6999`) giữ nguyên, còn đúng bốn caller trực tiếp trong Scripts:

| File:line | Caller giữ nguyên |
| --- | --- |
| GameScr.cs:4564 | update(), keyPressed[16] |
| GameScr.cs:4569 | update(), keyPressed[13] khi panel đóng |
| GameScr.cs:6515 | command 11001, đường cmdFocus/phím/PC |
| GameScr.cs:6770 | updateGamePad() cũ nhánh PC, nút xTG/yTG |

Không tìm thấy caller trực tiếp từ mod auto; không xóa các thuật toán nhắm mục tiêu/auto khác.

### 2. Icon và cooldown cover tròn

`Hud/CircularCombatIcon.cs:36/:66/:96`, `CombatMobileHud.cs:109/:122`: nhận texture/UV region thực từ SmallImage (atlas hoặc ảnh tải). **Theo ảnh test mới, đã tách cách vẽ:** chiêu cover tròn; item vẽ nguyên sprite nổi trên nền/viền tròn, không mask. Ảnh nguồn/avatar không sửa.

**Sửa mất chi tiết chiêu:** cách cũ làm tròn hệ số cover lên 2×/3× rồi crop dư, khiến nội dung bị zoom/cắt quá mức. Nay dùng đúng cover tối thiểu `diameter / min(sourceWidth,sourceHeight)`, lấy mẫu bilinear **một lần vào texture đúng đường kính pixel thực**, sau đó vẽ 1:1 với FilterMode.Point. Không làm tròn hệ số lên, không stretch lại mỗi frame. Icon vuông giữ toàn chiều rộng/cao nguồn trước mask tròn; các góc ngoài hình tròn vẫn được che theo yêu cầu. Icon chữ nhật chỉ crop phần dài dư cần thiết của cover, không zoom thêm. Alpha=0 ngoài bán kính, ảnh sát mép trong viền.

**Item nổi:** `PaintFloatingItem` dùng UV đầy đủ, giữ tỉ lệ và pixel native nếu vừa; chỉ thu nhỏ nếu vượt rect ô. Vẽ **nền/viền → sprite item không mask → badge (đã bỏ nhãn tên theo yêu cầu mới)**, không phóng/crop item thành hình tròn. Sprite nằm trong rect gốc nên vùng bấm không đổi; không tạo texture item phụ mỗi frame, dùng texture SmallImage sẵn có. Áp dụng cho cả ba ô item và icon capsule ngữ cảnh.

`mGraphics.cs:9/:308`: chỉ bật sink trong scope icon combat mobile. **Skill.paint không sửa**: vẫn quyết định timer/màu/paintCanNotUseSkill/vùng tô 20×20. Sink ánh xạ rect tô thực đó vào lòng icon, crop bằng cùng mask alpha tròn; không viết lại cooldown. Badge/số giây nằm ngoài scope, font không scale. Caller PC/khác vẫn dùng Graphics.DrawTexture/fillRect gốc; finally khôi phục sink và GUI state.

Cache key = source texture ID + UV pixel region + đường kính. Giới hạn **32 icon LRU + 8 mask**; Destroy khi loại cache. RenderTexture tạm luôn ReleaseTemporary, CPU texture đọc tạm luôn Destroy kể cả lỗi. `MobileCombatPointers.Cancel` (`:121`) Clear khi map/screen/nhân vật/viewport/zoom đổi hoặc pause; không tạo texture mỗi frame sau cache. Chọn alpha mask CPU có cache để dùng Skill.paint/mGraphics sẵn có, không thêm shader/skin. Đã xóa PaintIcon/IconSize/HudSpriteGui và kiểu fit icon vuông cũ.

### 3. Nguyên nhân lỗi gán item và sửa

Source nền không có ba ô item; cụm 5a trước đã thêm menu gán, **luồng đó hiện có trên mobile**, không cần dựng UI mới:

- `Panel.cs:8282/:8292` menu chính tab Hành trang (currentTabIndex=1), hoặc type 7 (`:7057`) → doFireInventory (`:7510`); item túi lấy bằng selected − arrItemBody.Length (`:7531`). Nhánh USE của item không phải trang bị gọi AddAssignmentCommands (`:7566`). Túi bên rương có hook `:8753`. Menu chọn item gọi Command.performAction (`Menu.cs:519/:530`). isnewInventory mặc định false, không có phép gán true trong source hiện tại.
- Command mang chính Item và listener MobileQuickItems; perform ghi template.id vào **templates[0..2]**, RMS theo host/port/charID (`Hud/MobileQuickItems.cs:44/:50`). HUD đọc Get(i) → Template(i) → cùng templates[i], tìm lại bag index trong arrItemBag (`:21/:22/:30`); icon/quantity đọc từ item thật (`CombatMobileHud.cs:115`). Không đọc nhầm mảng, không cache Item/index túi cũ.
- “Gán ô phím tắt” gốc/action 9003 (`Panel.cs:9169`) chỉ gán **SkillTemplate** vào onScreenSkill/keySkill, không phải item.

**Lỗi xác định được: ô 1 dùng action ID 0**, trong khi `Command.performAction` (`Command.cs:112`) chỉ dispatch **idAction > 0**; handler không chạy, templates[0]/RMS không được ghi. Đã sửa action gán **1/2/3 → templates[action−1]**, bỏ gán **4**; giữ menu/listener hiện có và Invalidate registry sau gán/bỏ gán. **Không đổi MobileQuickItems.Use** (`:35`): vẫn Service.useItem(0,1,bagIndex,−1), không tự trừ quantity hoặc đổi logic dùng đồ.

Ô 2/3 trước đã có ID dương và đúng index. **Chưa có bằng chứng runtime để quy lỗi hai ô này cho ID 0**; không khẳng định đã tái hiện mọi trường hợp user gặp. Không tìm thấy lỗi refresh trong nguồn đọc trực tiếp này; cần test cả ba qua đúng menu item sau sửa. Nếu ô 2/3 còn lỗi, cần ghi nhận item/menu/thao tác cụ thể để truy tiếp, không dựng UI suy đoán.

Cả ba mặc định trống; bấm ô trống không mở UI gán như quyết định trước. Gán giữ qua relog; hết/thiếu item trả null, không icon/badge. Quantity cập nhật theo túi từ server. Không tự map đậu; doUseHP/AutoPean nguyên vẹn.

## Đã dọn, còn lại, kiểm tra

Mobile bỏ đường đặt tọa độ/vẽ/hitbox chiến đấu cũ (`GameScr.cs:1938/:1966/:4251/:5748`); các thân legacy giữ cho PC/5b, adapter mobile trả rect rỗng. Đã gỡ focus mobile và helper vẽ icon vuông cũ. Không sửa gameplay/server, avatar/font/zoom hoặc các file local dirty ngoài phạm vi.

Ngoài phạm vi, chỉ liệt kê: tutorial `Hint.cs:471/:534` đọc xHP/yHP cũ; nhánh PC target/legacy; `mGraphics.drawRegion(Small,...)` cuối file còn NotImplementedException ở đường SmallImage không atlas; menu gán skill trong Editor còn chọn keySkill theo Main.isPC. Không sửa/xóa các phần này.

Đã kiểm tra tĩnh UTF-8/delimiter, diff whitespace, action ID/index, loại bỏ focus, scope/cache cleanup. Lượt ảnh mới bổ sung đối chiếu toán cover tối thiểu, miền lấy mẫu icon và fit item không crop. Đối chiếu đường mGraphics mặc định/PC khớp nền sau bỏ hook scoped; Skill.cs/MotherCanvas.cs nguyên vẹn; Char.cs chỉ đổi gán softkey menu mobile trong bổ sung bên dưới. **Đây không phải test Unity/C#: chưa compile/chạy/build.**

## Ma trận test tay — tất cả CHƯA CHẠY

Chạy toàn bảng trên **Device Simulator Android 16:9, 20:9, tai thỏ trái/phải**, và **S22/máy thật**; multi-touch phải kiểm tra máy thật.

| Test | Kết quả cần đạt |
| --- | --- |
| Vào map/đổi map/relog | Không nút xoay đổi mục tiêu, không hitbox mồ côi ở vị trí cũ |
| Từng chiêu 1–8 | Đúng skill; cover tròn sát viền, không góc vuông; chi tiết nhân vật/mạng nhện/nắm đấm không bị zoom/crop thêm như ảnh cũ |
| Cooldown bắt đầu/giữa/kết thúc | Tô/nháy cắt tròn không tràn góc; timer/số giây đúng |
| Ô 9–10 | Dữ liệu còn; gán lại vào 1–8 rồi dùng được |
| ĐÁNH / ngọc rồng / capsule | Đúng action cũ, không phát lại thao tác khi dữ liệu đổi |
| Joystick | Vùng rộng và vòng hình đều nhận, kéo mượt, thả dừng/núm về tâm |
| Joystick + từng chiêu/ĐÁNH | Hai ngón hoạt động độc lập; thử đảo thứ tự chạm/thả và ngón thứ ba |
| Ba ô trống lần đầu | Chỉ khung, bấm không gửi lệnh/chat/map |
| Menu item → gán lần lượt ô 1/2/3 → về map | Đúng item nguyên hình nổi trên nền/viền tròn, không mask/crop; không hiện tên, quantity đúng, ô 1 không còn lỗi ID 0 |
| Dùng từng item | Đúng item; quantity giảm theo server, hết thì ô trống |
| Bỏ gán / đổi gán / túi đảo index / relog | Ô trống ngay hoặc đổi đúng item; không dùng nhầm index/RMS nhân vật-server |
| Không bấm xuyên | Combat không chat/chọn người/click-move; kéo map qua nút không kích hoạt nút |
| Modal/panel/thông báo mở sẵn hoặc khi đang giữ | Nút bị che không nhận bấm; joystick dừng, đóng modal không phát lại gesture |
| Resize / đổi hướng / tai thỏ trái-phải | Neo safe viewport, item không đè chiêu, mask đổi đúng pixel diameter; font không scale |
| Đổi chiêu/map/zoom/pause-relog lặp lại | Không đăng ký trùng/kẹt key; không tăng texture không giới hạn |

PC dành cho 5b; chỉ đối chiếu source, chưa chạy/build/compile PC.

## Bỏ tên item, căn hàng — 15/10/2026

Theo ảnh user mới: đã xóa phần vẽ tên item và entry/obstacle `combat.item-label.*`, không để vùng chặn bấm của nhãn cũ. Ba ô item dùng chung tâm Y với **chiêu 5 / onScreenSkill[4]** nằm ngay bên phải, lấy trực tiếp center.y từ rect registry của chiêu; kích thước item 40, chiêu 46 nên căn tâm thay vì căn mép trên. Reference item top chuyển từ 312 → 321, tâm Y đều 341. Giữ neo ngang Center, gap tránh chồng chiêu, icon nổi và badge số lượng; vẽ/bấm cùng rect mới. Không đổi logic gán/dùng item, PC hoặc gameplay.

Test tay bổ sung trên Android 16:9/20:9, tai thỏ, S22: không còn tên bên dưới cả ba ô; tâm ba vòng item thẳng hàng với chiêu 5 sau resize/đổi hướng; badge đúng và bấm dùng đúng ô. **Chưa compile/chạy/build**, chỉ rà source và diff.

## Badge, menu gốc và danh sách — 15/10/2026

- `Hud/CombatMobileHud.cs:133`: badge quantity dùng lại **NotificationSkin.BadgeIcon** bo tròn/viền của game, kéo nền theo bề rộng số để 1 chữ số tròn, nhiều chữ số thành oval; font trắng không scale, vẫn neo dưới phải. Bỏ fillRect đỏ vuông cũ; dùng texture cache sẵn, không tạo skin/texture mỗi frame.
- `Char.cs:1058`: trên mobile không gán cmdMenu vào softkey trái nữa. `GameScr.cs:670/:5226/:5268`: adapter menu cũ trả rect rỗng và bỏ flare tại nút cũ trên mobile. Giữ nguyên cmdMenu/command 11000/actMenu vì avatar vẫn gọi (`TopInfoHud.cs:112`); không gỡ menu ngữ cảnh của mục tiêu. Nhánh PC giữ như cũ.
- `Hud/MapListsHud.cs:190`: danh sách item mobile loại mọi `template.type == 22`, không so tên/ID rời rạc. Xác nhận client ItemMap.cs:204 và chỉ đọc server Trade.java:244 (case 22: vệ tinh), UseItem.java:751–760. Không sửa server, vItemMap, buff vệ tinh, AutoPick hoặc thuật toán nhặt đồ. Count/scroll của danh sách dựa trên tập đã lọc; nếu chỉ còn vệ tinh thì danh sách ẩn.
- `Hud/MapListsHud.cs:115`: **chiều rộng** item từ mép trái avatar đến mép phải thanh sức mạnh `top-info.exp` (không tính FPS); chiều rộng người chơi bằng cả cụm ba nút **Hub → chuông → R**, gồm gap giữa nút. Đọc rect registry, không hardcode chiều rộng mới; header/body/row/hitbox cùng thay theo viewport. Giữ giới hạn chiều cao, vùng world-range, cuộn/thu gọn và logic focus/pick. Chỉ thay layout/lọc mobile; PC giữ cách rộng 190/130 px cũ.

Test tay bổ sung trên Android 16:9/20:9/tai thỏ/S22: badge 1/9/99/999 bo tròn, số đúng và không scale font; không còn nút/flare/vùng bấm menu gốc, avatar vẫn mở menu; vệ tinh mọi loại không xuất hiện/count, item thường còn đúng; mép danh sách item khớp avatar/thanh sức mạnh, danh sách người khớp ba nút chức năng khi resize/đổi hướng; cuộn/thu gọn/bấm dòng/modal vẫn đúng. **Chưa compile/chạy/build. Không push/merge/stash.**

## Badge tối đa 99+ và căn giữa — 15/10/2026

Chỉ đổi hiển thị `CombatMobileHud.PaintItemBadge`: quantity 1–99 hiện số thật, >99 hiện **99+**; không sửa quantity trong túi hay logic dùng item. Đo chuỗi thực bằng chính font game, thêm đệm đều hai bên/trên dưới, neo dưới phải như cũ. Badge dùng skin thông báo: giữ hai đầu tròn, chỉ giãn lát giữa cho chuỗi dài. Số GUI được vẽ MiddleCenter trong **cùng rect nền**, bỏ ảnh hưởng x-- của mFont.setTypePaint đối với badge; không đổi mFont/fontSize/zoom toàn game. Zoom 1 giữ bitmap font và căn hộp glyph theo kích thước đo. Style cache theo font, không tạo texture mới.

Test tay bổ sung 1/9/10/99/100/168/9999: hiển thị tương ứng 1/9/10/99/99+/99+/99+, không chạm viền, không lệch trái/phải, giữ cân đối khi lượng thay đổi hoặc resize/safe-area. Dùng item qua ngưỡng 100 → 99 phải đổi 99+ → 99 theo dữ liệu server, không giảm/gán lại quantity ở painter. **Chưa compile/chạy/build**, không push/merge/stash.

## Nút đánh thường dùng asset nắm đấm — 15/10/2026

Theo reference user, `CombatMobileHud.PaintAttack` thay toàn bộ nền cam/chữ ĐÁNH vẽ tay bằng cặp asset game **GameScr.imgFire0/imgFire1** (`/mainImage/myTexture2dfirebtn0.png`, `myTexture2dfirebtn1.png`), hình nắm đấm thường/đang nhấn. Không thêm asset/skin ngoài game. Rect attack 80 design px responsive, hitbox/capture và action keyPressed 5/25 giữ nguyên. Bản copy riêng Bilinear dùng cho nền/glyph UI khi fit rect, cache tối đa hai texture; Destroy bản cũ khi source thay đổi, không sửa texture/importer/sampling PC. Xóa phần vẽ chữ ĐÁNH và nền/viền cam riêng của nút này.

Test tay bổ sung Android 16:9/20:9/S22: nút hiện nắm đấm, giữ ngón đổi sang asset nhấn, thả trở lại asset thường; bấm đánh và joystick+đánh vẫn đúng, modal không bấm xuyên. **Chưa compile/chạy/build**, không push/merge/stash.

## Dịch badge ra ngoài ô item — 15/10/2026

PaintItemBadge dịch nền và số cùng nhau ra ngoài góc dưới phải: **+6 design px ngang, +4 design px dọc**, qua DesignUnit để đồng nhất sau resize/zoom. Giữ kích thước/padding/căn giữa và giới hạn 99+; giảm phần badge che icon. Không đổi rect ô item, logic gán/dùng hoặc PC. Chưa compile/chạy/build; user kiểm tra lại badge 1/99+ ở cả ba ô trên mobile.

## Trạng thái chiêu đang chọn — 15/10/2026

`CombatMobileHud.PaintSelectedSkill`: chiêu hiện hành lấy đúng `onScreenSkill[i] == Char.myCharz().myskill`. Bỏ cách chỉ đổi viền kem mảnh khó phân biệt; thêm **viền vàng sáng 4 design px + vòng vàng ngoài** cách 3 design px, luôn hiện sau icon/cooldown để không bị che. Giữ riêng feedback nhấn, không dùng nhấp nháy/cooldown để giả trạng thái chọn. Không đổi kích thước icon, rect/hitbox, logic chọn/dùng hoặc PC. Texture vòng dùng cache hình tròn hiện có.

Test tay: chọn lần lượt 1–8, thả ngón vẫn thấy đúng một chiêu viền vàng; chuyển chiêu thì viền theo myskill; cooldown đang chạy không che viền; ô trống không có dấu chọn; joystick + chọn chiêu/ĐÁNH vẫn hoạt động. Nếu myskill nằm ngoài tám slot hiển thị thì chưa có nút để đánh dấu, cần gán lại vào 1–8 theo menu hiện có. **Chưa compile/chạy/build**, không push/merge/stash.

## Cooldown item nhanh chống spam — 15/10/2026

Theo yêu cầu mới, thêm gate **10 giây** cho mọi item dùng từ ba ô nhanh, theo mốc nút đậu cũ `GameScr.doUseHP` (10000 ms); ItemTemplate không có field cooldown. Đây là cooldown thao tác client, không khẳng định là thời gian server của từng loại item. Giữ nguyên Service.useItem(0,1,bagIndex,-1), không sửa server/protocol/effect/quantity.

- `MobileQuickItems`: cooldown theo **template ID**, không theo số ô; gán cùng item vào ô khác, đổi slot/bỏ gán/gán lại, đổi map không reset thời gian. Riêng mọi đậu `template.type==6` chung một nhóm hồi máu; chia sẻ `lastUsePotion` với doUseHP/phím/AutoPean qua hai helper mobile của GameScr, không luân phiên cấp đậu để bypass timer. Thân doUseHP/Char.doUsePotion/AutoPean/PC giữ nguyên.
- Kiểm tra assignment và item còn trong túi trước, rồi remaining; còn cooldown thì không gửi Service. Bắt đầu cooldown sau khi gửi yêu cầu; không tự giảm item và không chờ ACK mới chặn spam. Request bị server từ chối hoặc popup xác nhận bị hủy vẫn giữ cooldown thao tác 10 giây. Không bọc các đường Panel/auto khác; chỉ đậu chia sẻ bộ đếm gốc.
- `CombatMobileHud` vẽ icon nổi → lớp tối tròn và số giây (làm tròn lên) → badge quantity 99+. `CircularCombatIcon.PaintItemCooldown` dùng chung mask cache với chiêu; icon item vẫn không bị crop, lớp tối không tràn góc. Skill.paint và công thức hồi chiêu chiêu thức không sửa.
- Thời gian giữ trong RAM, tách theo host/port/charID, xóa trạng thái khi owner đổi; không ghi timer vào RMS. Deadline hết được dọn ở lần dùng tiếp. Relog cùng nhân vật trong cùng tiến trình không reset dictionary; restart app không giữ timer client.

Test tay bổ sung, **đều CHƯA CHẠY**: spam một ô chỉ gửi một request mỗi 10 giây; cùng item gán ô 1/2/3 dùng chung timer; hai cấp đậu cũng chung timer; AutoPean/phím dùng đậu trước/sau ô nhanh tôn trọng cùng lastUsePotion; item khác có timer riêng; số đếm 10→1 rồi hết lớp tối; quantity/badge đúng và hết item thì trống; đổi gán/map/modal không reset/bấm xuyên; joystick+item vẫn hoạt động. **Chưa compile/chạy/build**, không push/merge/stash, không sửa server.
