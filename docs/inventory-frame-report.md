# Cụm A — khung Hành trang và trang bị

## Lần sửa 2 — nền gốc, góc bo và thanh tài nguyên (hiện tại)

Tiếp tục `feat/inventory-frame`; giữ các rect panel/tab/9 ô/túi/footer, kích thước ô, hit-test và handler. Không thêm chip lọc/lưới túi/panel chi tiết của cụm B/C. Phần này thay thế mô tả màu mockup và footer phẳng của bản A ban đầu bên dưới.

### Nền và tài nguyên được tái dùng

| Thành phần | Màu / sprite gốc dùng trong bản sửa | Nguồn thực tế |
| --- | --- | --- |
| Panel kho đồ | `GameCanvas.paintz.paintFrameSimple`: nền **14338484**, viền **6702080**, đúng panel Hành trang cũ | `Assets/Scripts/Paint.cs:375`; điểm gọi cũ `Assets/Scripts/Panel.cs:3881` |
| Header | **9993045**, chữ dùng `mFont.tahoma_7_yellow` sẵn có | `Panel.paintTopInfo:6180`; màu nền ở `:6183`; Hành trang cũ dùng font vàng trong `paintItemBodyBagInfo` |
| Khối Trang bị | **15196114** (nền body cũ), khung góc sprite `PopUp.goc` | `Panel.paintInventory:5599`; `Assets/Scripts/Hud/InventoryFrame.cs:301` |
| Ô trang bị | **9993045** (nền icon body cũ); chọn dùng **9541120**, vòng nhấn **16383818**. Màu riêng của set đã chốt vẫn giữ, chỉ 5 ô đầu | `Panel.paintInventory:5599`; `InventoryFrame.PaintInventoryEquipment:318` |
| Hàng túi / vùng icon túi | **15723751** / **11837316**; chọn **16383818** / **9541120**; màu option34/35/36 của danh sách cũ giữ nguyên | `Panel.paintInventory:5599`, nhánh `bagOnly`; các màn dùng hàm này với `bagOnly=false` giữ đường vẽ cũ |
| Footer tài nguyên | **11837316**, đúng nền footer cũ; bo bằng `PopUp.goc` | `Panel.paintBottomMoneyInfo:5880`; `InventoryFrame.PaintInventoryMoney:356` |
| Tab | `PopUp.imgPopUp` — `/mainImage/myTexture2dimgPopup.png`; chọn dùng `PopUp.imgPopUp2` — `/mainImage/myTexture2dimgPopup2.png`; màu ruột gốc **16770503** / **12052656**; font `tahoma_7_grey` / `tahoma_7_green2` | `Assets/Scripts/PopUp.cs:176`, `paintPopUp`; `Panel.paintTab:5714` |
| Nút X | `Panel.imgX` — `/mainImage/myTexture2dbtX.png`, đúng sprite nút đóng cũ, trên khung bo gốc | `Panel.loadBg:714`; `InventoryFrame.cs:283` |
| Lv | Nền chọn vàng gốc **16383818**, chữ `mFont.tahoma_7`; bo bằng `PopUp.goc` | `InventoryFrame.cs:346` |
| Vàng / ngọc xanh | `Panel.imgXu` — `/mainImage/myTexture2dimgMoney.png`; `Panel.imgLuong` — `/mainImage/myTexture2dimgDiamond.png`; font `mFont.tahoma_7_yellow` | `Panel.loadBg:715`, `:716`; `InventoryFrame.cs:356` |

Không còn các hằng nền/viền/nút/chữ lấy từ bảng hex mockup trong `InventoryFrame.cs`. Màu viền 13 set là bảng phân biệt set đã được người dùng chốt riêng trước đó, tiếp tục giữ.

### Góc bo

`InventoryFrame.Rounded:100` tái dùng nguyên `PopUp.paintPopUp(..., isButton:false)` và sprite **`PopUp.goc`**, được nạp từ **`/mainImage/myTexture2dbd3.png`** (`PopUp.loadBg:90`). Sprite có góc 7×6 logic và phần trong màu trắng: tint sprite/ruột trắng cùng màu nền gốc để tránh góc bị trắng lệch nền; khôi phục `GUI.color` trong `finally`. Không tạo texture/sprite, không tự viết hình học bo góc. Tab dùng nguyên bộ ghép sprite của `paintPopUp(..., isButton:true)` như màn cũ.

Khối Trang bị, ô đang mặc/ô trống, vùng highlight, hàng túi và vùng icon của hàng túi, nút X, badge Lv, footer đều dùng khung bo này. Khung panel ngoài giữ `paintFrameSimple` nguyên bản vì yêu cầu tái dùng đúng nền panel cũ. Nền badge Lv cao 12 logic để đủ hai nửa góc 6 của sprite gốc; điểm neo, vị trí ô và các rect tương tác không đổi.

**Chưa có sprite phù hợp:** chưa tìm thấy tài nguyên gốc cho **viền đứt có góc bo**. Đã bỏ vòng lặp tự vẽ viền đứt vuông; ô trống hiện dùng khung bo liền và nhãn ô có sẵn. Cần người dùng quyết định giữ khung bo liền hay cung cấp/chọn tài nguyên viền đứt phù hợp. Không tự vẽ thêm. Chip lọc chưa tồn tại trong cụm A; chưa triển khai trước cụm B.

### Căn thanh tài nguyên

`InventoryFrame.MoneySpriteUv:115` dùng vùng alpha thực của sprite x1/x2/x3/x4, không sửa file ảnh. Ví dụ x2: vàng 26×20 nhưng phần nhìn thấy 24×18; ngọc 24×20 và phần nhìn thấy 24×20. Vì vậy chỉ đặt hai ảnh cùng kích thước texture chưa đủ. `MoneyIconBounds:123` chuẩn hóa **phần hình nhìn thấy cao 10 logic** cho cả hai, giữ đúng tỉ lệ ảnh và đặt tâm tại `row.center.y`. Vẽ vào rect tường minh, không còn phụ thuộc pivot/anchor cũ.

`MoneyTextBounds:126` và `Panel.PaintInventoryMoneyText:389`: số ở ô rộng cố định theo chuỗi tối đa **99.999.999.999** / **1.000.000**, cộng 1 logic để bù việc `mFont.getWidth` làm tròn xuống. Font GUI giữ font game, `MiddleLeft`, padding/margin=0, không wrap; không đi qua offset `mFont.yAddFont`/`yAdd` của đường vẽ chữ cũ. Bitmap zoom1 dùng `font.getHeight()` để căn giữa. Cả hai số canh trái bên trong ô cố định, giữ khoảng cách **3 logic từ mép icon tới điểm bắt đầu số**, kể cả số ngọc ngắn; cả cụm ngọc vẫn neo phải. Không có ngọc đỏ.

Trạng thái kiểm tra: đã xác nhận alpha crop từ **8 PNG gốc**, tâm dọc icon/ô chữ trùng nhau, chiều cao hai icon bằng nhau, gap bằng nhau, số tối đa không đè nhau và không vượt footer trên các viewport mobile 422×195 / 585×270 / 844×390 và PC 640×360 / 960×540 / 1280×720, zoom2/3/4. **1139 kiểm tra qua**, compile C# client/Editor qua; chưa mở Unity/Play Mode, chưa có ảnh S22/PC thực nên chưa xác nhận cảm nhận baseline bằng mắt trên thiết bị. Cần người dùng kiểm tra footer ở số ngắn và tối đa, zoom đang dùng trên S22, góc bo và màu set khi chọn món.

### Dọn code trong lần sửa 2

- Xoá `InventoryFrame.Fill` và `InventoryFrame.Text` sau khi đối chiếu/chuyển hết chỗ gọi sang skin/font gốc; xoá các hằng mockup `Background`, `Dark`, `Red` không còn dùng và đường đổi màu font tạm đi kèm.
- Bỏ các vòng `drawLine` tự dựng viền đứt vuông và lệnh `drawRect` ô trang bị; thay bằng sprite bo. Bỏ `fillRoundRect` ở badge Lv vì `Assets/Scripts/mGraphics.cs:969` chỉ gọi `fillRect`, không thực sự bo. Sáu dấu sao nhỏ vẫn dùng chỉ báo 3×3, không thêm hình học/sprite mới cho chúng.
- Bỏ nhánh `case 5` trong hai hàm riêng của frame (`SelectInventoryFrameTab`, `PaintInventoryFrame`): đã đối chiếu toàn bộ chỗ gọi, chỉ chọn từ bộ tab 0/1/2/3/6 hoặc mở Giao diện với ID4; không có chỗ gán tab5. Các hàm rương dùng chung của `Panel` và NPC vẫn giữ.
- Trong `Panel.paintInventory`, màu icon tính một lần vào `iconColor`, tránh tính màu rồi bị bước vẽ skin ghi đè. Không xoá code ở màn khác; danh sách code thừa ngoài phạm vi ở phần dưới vẫn chỉ ghi nhận.

Các số dòng của phần này theo bản sửa lần 2; các mục khảo sát/lịch sử bên dưới giữ số dòng tại lần ghi nhận và có thể đã dịch.

## Sửa lỗi khởi động sau test của người dùng

Người dùng gặp NullReferenceException từ `Panel.InventoryFrameTabIds` qua `HudLayout.HitTest` khi chưa vào game. Nguyên nhân do cụm A đọc `currentTabName.Length` khi dữ liệu chưa khởi tạo; `HitTest` còn resolve rect trước khi kiểm tra visibility. Exception ở đường `Main.OnGUI`/input có thể lặp liên tục và gây lag; chưa có đo FPS để kết luận mọi nguyên nhân lag.

- `Assets/Scripts/Hud/InventoryFrame.cs:13`, `Visible`: chỉ hiện trên `GameScr.instance`, panel đã mở và có dữ liệu tab.
- `InventoryFrame.cs:119`, `InventoryFrameTabIds`: null trả mảng rỗng; các bộ tab dùng mảng static, không cấp phát mảng mới mỗi lần đọc. Provider tab ở `:38` xử lý panel/tab chưa có bằng rect rỗng, không chia cho 0.
- `Assets/Scripts/Hud/HudLayout.cs:131`, `ForEachBackToFront`, và `:141`, `HitTest`: kiểm tra visibility trước khi gọi provider rect. `GetRect` vẫn cho phép lấy geometry trực tiếp của rect trang trí/ẩn khi caller cần.
- Bổ sung test dùng **getter thực** thay cho mảng giả lập cố định: startup không panel/font, panel chưa có tab, màn login, traversal các rect ẩn, resolve tab khi panel null, tái dùng cùng mảng tab. Test mới đã thất bại trên code trước sửa; sau sửa **963 kiểm tra qua**, compile client/Editor qua. Log `Build/inventory-compile/ecabf9825dc84d06b40514e92502683f`.
- Không chạy Unity/Play Mode/build APK. Người dùng dừng Play, chờ Unity compile xong, Clear Console rồi vào lại game; kiểm tra exception này không lặp, đăng nhập và mở/đóng Hành trang. Nếu còn lag, cần đo tiếp sau khi hết exception spam.

Các số dòng trong phần báo cáo cụm A bên dưới là tại commit ban đầu; phần sửa startup này làm dịch số dòng trong `InventoryFrame.cs`.

Nhánh `feat/inventory-frame`, tạo tại repo chính từ `origin/develop` đã fetch (`bc44620`). Không tạo clone/worktree, không sửa server, không đổi protocol, không build Unity/APK, không push/merge/stash và chưa xoá nhánh/stash nào. Dừng sau cụm A để người dùng test và merge. Link mockup không đọc được qua công cụ; triển khai theo thông số văn bản và hai lựa chọn người dùng đã xác nhận.

## Khảo sát và dữ liệu hiện có

Các đường dẫn dưới đây tương đối với repo client `D:/project_Unity/client_Nro/Mod-nro-unity`; đường dẫn server tương đối với `D:/dowload/SRC NRO NEW 03/SRC`. Số dòng theo bản hoàn tất cụm A.

| Nội dung | Tham chiếu thực tế / kết quả |
| --- | --- |
| Màn cũ | `Assets/Scripts/Panel.cs:3538` — `setTabInventory`; `:5599` — `paintInventory`; `:7517` — `doFireInventory`. Danh sách cũ gộp `arrItemBody` và `arrItemBag`; chỉ số chọn vẫn giữ cách gộp này. |
| Mặc / tháo | `Panel.cs:8979`, `:9010` — `Service.gI().getItem`; `BAG_BODY = 4`, `BODY_BAG = 5` ở `:10859`. `Assets/Scripts/Service.cs:673` gửi opcode -40, type và id. |
| Dùng / vứt / gán ô nhanh | Menu hiện có trong `doFireInventory` gọi các action cũ, không thêm nghiệp vụ; `Panel.cs:7573` tái dùng `Hud.MobileQuickItems.AddAssignmentCommands`. `Service.cs:466`, `useItem`, gửi -43 cùng type/where/index và template khi index=-1. |
| Nâng cấp / gắn sao | Luồng NPC kết hợp hiện có; `Panel.cs:7321` gọi `Service.gI().combine(1, vItemCombine)`, hàm gửi ở `Service.cs:151`. Chưa thêm nút trực tiếp trong cụm A. |
| Level / sao | `Assets/Scripts/Item.cs:11` — `OPT_LVITEM=72`; `:13` — `OPT_STARSLOT=102` là sao đã gắn; `:15` — `OPT_MAXSTARSLOT=107` là sức chứa. UI A chặn level 0–7, sao sáng 0–6; không đổi dữ liệu gốc. |
| Icon / mô tả | Dùng `Item.template.iconID` và dữ liệu `Item.template.description`, `itemOption` hiện có; không tạo icon mới. |
| Ô trang bị | `InventoryFrame.cs:118` ánh xạ body `{0,1,2,3,4,6,5,7,-1}`. Server `src/nro/models/services/InventoryService.java:289`, `putItemBody`, ánh xạ GLT type32 vào body6, skin type5 vào body5, bông tai type27 vào body7. Body8 là loại11, không phải pet; ô pet chỉ giữ chỗ. |
| Số ô túi | `Assets/Scripts/Controller.cs:1862`, `:5536`: server gửi số ô, client cấp `arrItemBag`. Server `src/nro/models/database/PlayerDAO.java:131` tạo 30 ô; `src/nro/models/player/Inventory.java:21`, `MAX_ITEMS_BAG=80`. `InventoryService.java:789` item517 mở **1 ô**, item518 mở 1 ô rương; `src/nro/models/npc_list/Santa.java:68` mở shop `SANTA_MO_RONG_HANH_TRANG`. Không đổi khởi đầu/giới hạn/giá. |
| Giao diện | `Panel.cs:8336`, `doFireModTabs`, giữ nguyên các toggle và lưu RMS hiện có. Lối vào mới: Chức năng → Cài đặt → Giao diện; X/phím đóng trong trang này quay về Cài đặt. |
| Rương đồ | Bỏ shortcut tab theo người dùng; giữ các hàm `setTabBox`, `paintBox`, `doFireBoxInvenTab` và luồng NPC tại nhà. Các hàm rương dùng chung không phải dead code. |
| Phí chat thế giới | Server `src/nro/models/services/WorldChatPolicy.java:5`, `:6` khai báo `GEM_FEE=10`, `GOLD_FEE=10000000`; `WorldChatService.java:112`, `:127` trừ `inventory.gem` và `inventory.gold`: **ngọc xanh**, không phải ruby. Client `Controller.cs:3338` nhận `luong`, `:3339` nhận `luongKhoa`. Không sửa phí/server. |
| Ngọc đỏ | Chỉ bỏ khỏi footer mới. Vẫn có UI dùng ngoài phạm vi: `Panel.paintBottomMoneyInfo`, shop và kí gửi; không xoá các tài nguyên/hàm dùng chung. |
| Panel thông báo | `Assets/Scripts/Notifications/NotificationCenter.cs:56`, `RegisterHudRects`; `:81`, `PanelBounds`; `:405`, `Paint`: tham khảo anchor/registry/dim. Dùng trực tiếp `HudLayout` chung, giữ bảng màu nâu yêu cầu thay vì skin thông báo. |

## Thay đổi cụm A

- `Assets/Scripts/Hud/InventoryFrame.cs:22`, `Register`: panel trái full chiều cao, rect đóng/tab/9 ô/footer/túi cùng registry và hit-test của `HudLayout`. `GameScr.cs:673` đăng ký một lần. Màn chính được vẽ và nhận input qua các điểm nối `Panel.cs:1843`, `:2926`, `:3869`.
- `InventoryFrame.cs:116`, `InventoryFrameTabIds`: 5 tab tiếng Việt, giữ ID nội bộ 0/1/2/3/6 để tái dùng handler. `Panel.setTypeMain:1364` chuyển tab 4/5 cũ đã lưu sang Chức năng khi mở lại. Các mảng tab nội bộ và hàm rương vẫn giữ cho các đường dùng chung.
- `SoundMn.cs:259`, `AddPcHudLabelOption`: thêm mục Giao diện trên mobile/PC, ngay trước tùy chọn nhãn phím HUD PC khi tùy chọn đó có mặt; chỉ số các tùy chọn cũ giữ nguyên. `Panel.cs:10066`, `doFireOption`, gọi `InventoryFrame.cs:124`, `OpenInventoryAppearanceSettings`, tái dùng `setTabMod` / `paintModTabs` / `doFireModTabs`.
- `InventoryFrame.cs:294`, `PaintInventoryEquipment`: đúng 3×3 và thứ tự đã yêu cầu, icon cũ, khung nét đứt khi trống, glow khi chọn, Lv/sáu chấm cho sáu ô đầu, pet giữ chỗ. Chọn món gọi `doFireInventory` cũ; chưa thay menu chi tiết.
- `InventoryFrame.cs:344`, `PaintInventoryMoney`: dải phẳng vàng/ngọc xanh (`imgXu`, `imgLuong`), trường số đặt cố định theo chiều rộng chuỗi tối đa, không có `luongKhoa`. Dùng font hiện có; màu chữ tạm được khôi phục sau vẽ để không ảnh hưởng màn khác.
- Túi bên dưới tạm dùng danh sách cũ qua `Panel.paintInventory(g, true)`, chỉ bỏ phần body đã vẽ thành lưới. Chưa làm lưới túi, lọc, double-click hoặc panel chi tiết.

### Màu set đã thiết kế theo xác nhận người dùng

`InventoryFrame.SetColor:72` đọc ID option thực, không dùng độ hiếm option34/35/36. Chỉ 5 ô đầu áp dụng viền set. Không tìm thấy set thì viền nâu trung tính. Khi chọn, viền vàng/glow ưu tiên báo trạng thái chọn.

| Option | Set | Màu |
| --- | --- | --- |
| 127 | Thên Xin Hăng | #ffd166 |
| 128 | Kirin | #62d98b |
| 129 | Sôngôku | #ff914d |
| 130 | Picolo | #4dd9cb |
| 131 | Ốc tiêu | #b8e866 |
| 132 | Pikkoro Daimao | #c58aff |
| 133 | Kakarot | #ff667d |
| 134 | Ca Đíc | #66aaff |
| 135 | Nappa | #d8bc96 |
| 233 | Gohan | #f48bd4 |
| 237 | Nail chiến binh Namếc | #82e6f7 |
| 241 | Cađic M | #a5a0ff |
| 245 | Thần Vũ Trụ Kaio | #f5f0c5 |

Nguồn định danh: server `sql/nro1.sql:2264`–`:2272`, `:2370`, `:2374`, `:2378`, `:2382`. Màu là thiết kế client mới, không phải giá trị màu được server gửi. Các màu gần nhau vẫn cần người dùng đánh giá trên màn thật; chưa có ký hiệu thay thế màu cho người khó phân biệt màu.

## Code thừa

Đã xác nhận và xoá lệnh `g.setColor(16711680)` đầu `Panel.paintInventory`: không có lệnh vẽ sử dụng màu đó trước khi màu được ghi đè trong vòng lặp. Không xoá chức năng dùng chung.

Ngoài phạm vi, chỉ ghi nhận: `Panel.setTextColor:10611` có biến `flag`, `flag2`… không dùng và các khối `if (1 == 0)` tại `:10620`, `:10633`, `:10644`, `:10658`. Không sửa chúng. Các nhánh tab cũ còn phục vụ handler/trang khác nên không kết luận là dead code.

## Kiểm chứng và test cần làm

- `python Tests/Inventory/run.py`: **955 kiểm tra qua**. Biên dịch helper/registry thực với fixture, kiểm tra mobile/PC ở zoom2/3/4, full-height, kích thước ô, giới hạn footer, hit-test, modal ưu tiên, ô ẩn không bắt click, option null, sao gắn khác sức chứa, 13 màu set khác nhau và phục hồi màu font.
- `python Tests/Inventory/compile.py`: **Assembly-CSharp và Assembly-CSharp-Editor qua**, biên dịch C# độc lập với các reference có sẵn; không chạy Unity/build game. Log ở `Build/inventory-compile/1ff51e3f80b24cbfb16c588160089967`. Kiểm tra này không chứng minh hình ảnh hoặc hành vi runtime trong Play Mode.
- `git diff --check` các file code thuộc cụm A: qua.
- Người dùng test mobile/S22 và PC: mở/đóng Hành trang; kiểm tra 9 ô và sao/level (đặc biệt option102 khác107); chọn đồ đang mặc/túi, Mặc/Tháo/Dùng/Vứt/gán ô nhanh vẫn chạy đường cũ; chuyển cả 5 tab bằng touch/phím; mở Cài đặt → Giao diện, bật/tắt các mục và quay lại; nhãn phím HUD PC còn hoạt động; NPC rương ở nhà còn mở được; kéo túi và mở popup không đánh/chạy xuyên xuống map; kiểm tra footer với số tối đa.

### Rủi ro Samsung S22 / bố cục

Design px được quy đổi qua `HudLayout.PixelsToLogical` và chuẩn chiều cao `HudLayout.DesignReference`, cùng cách fit theo chiều cao của HUD hiện có. Không đổi zoom/font/safe-area. Ô 40/64 giữ tỉ lệ design, không tự thu gọn số cột. Trên viewport logic hẹp, chiều rộng panel có **mức tối thiểu theo hai chuỗi tiền + icon** để footer không chồng nhau; vì vậy có thể rộng hơn tỉ lệ 300/380 thiết kế. Cần đánh giá phần map còn thấy trên S22 và chữ tab/header ở zoom4; số HP/KI rất dài có thể bị clip trong vùng header cố định.

Chưa chạy Device Simulator/Play Mode hoặc có ảnh chụp S22. Cần kiểm tra safe-area/camera cutout, menu popup cũ, thao tác vuốt/chạm, font và Lv pill không bị cắt. Ở viewport logic rất thấp, vùng danh sách túi có thể còn ít chiều cao; lưới túi cuộn là cụm B. Cụm C mới làm panel phải 250/320 full-height. Nhánh này xuất phát develop nên không tự chứa các fix chưa merge từ `fix/dead-state-hud` / `fix/ulti-skills`.

## Chưa làm / chưa chốt

Cụm B/C chưa bắt đầu. Không quyết định số ô khởi đầu mới, giá mở thêm 5 ô, xử lý mảnh đá hoặc khóa món; đây vẫn là các hạng mục chờ chốt và/hoặc server. Ngọc đỏ ngoài màn Hành trang không đổi. Không thay đổi hành động nâng cấp/gắn sao từ NPC.

## Danh sách git chờ duyệt dọn — chưa xoá

Client: `develop`/`origin/develop` ở `bc44620`. Các nhánh HUD dưới đây có local và remote tương ứng, đã có squash trên develop; **chỉ là ứng viên xoá sau duyệt**, không dựa riêng vào `--merged` vì squash khác hash:

| Nhánh local + origin | Commit squash trên develop |
| --- | --- |
| feat/hud-combat-mobile | 6c0d26f |
| feat/hud-combat-pc | 015c672 |
| feat/hud-lists | ba1cc48 |
| feat/hud-notification-panel | 97468bd |
| feat/hud-target-info-pc | bc44620 |
| feat/hud-target-mobile | ce1d132 |
| feat/hud-top-actions | a4d8c3e |
| feat/hud-top-info | ae61050 |

Giữ: `feature/custom-server-address`, `fix/login-batch3a`, `fix/ui-batch-c` còn commit chưa đối chiếu được hoàn toàn; `fix/dead-state-hud`, `fix/ulti-skills` chưa merge; `feat/inventory-frame` đang làm; `develop`, `master` và remote nền.

Client có hai worktree: repo chính và worktree **đã tồn tại trước nhiệm vụ** `D:/project_Unity/client_Nro/Mod-nro-unity-ui-batch-c` (`fix/ui-batch-c`). Chưa xoá/chưa tạo thêm. Bốn stash chưa có đủ cơ sở xoá: `stash@{0}` d1a9a4a (server address/layout), `{1}` a9a997d (change-ip-btn), `{2}` dcbb14c (Unity MCP packages), `{3}` 5922203 (backup workspace cũ).

Server: `develop`/origin `59daa33`. `feat/world-chat-p3` local/remote là ancestor develop, ứng viên xoá sau duyệt. Giữ `feat/new-event` local/remote (local có commit riêng), `fix/boss-ground-movement`, `fix/sukaku-placeholder-assets` có phần chưa khớp hoàn toàn, `fix/ulti-skills` đang chứa fix chưa merge, `main`, `develop`. Chỉ một worktree repo chính. Stash `{0}` 6121b4b (`server-dirty-before-notification-sync`) chưa xác nhận thừa, giữ.

Các thay đổi người dùng có sẵn được giữ nguyên và loại khỏi commit cụm A: client logo, Packages/manifest.json, Packages/packages-lock.json, ProjectSettings/ProjectSettings.asset, hai file layout UserSettings; server Config.properties, ClanService.java, .idea/. Sau khi người dùng merge cụm A mới đối chiếu lại và đề xuất dọn nhánh này.
