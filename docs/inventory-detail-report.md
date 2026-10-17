# Cụm C — chi tiết vật phẩm và hành động

## Nhánh và giới hạn

`feat/inventory-detail` tạo từ `feat/inventory-bag` tại **4c648ea**, theo xác nhận của người dùng. Fetch đã chạy; `origin/develop` vẫn ở **f4e58a9** (A/#23), chưa chứa tám commit B. Không tự merge/cherry-pick, không clone/worktree mới. PR của C cần chờ B vào develop hoặc đổi base khi làm PR; hiện chưa push.

Không chạy build/Unity/APK/server, không phát hành runtime, không sửa server/protocol, không push/merge/stash hoặc xoá branch. Các thay đổi sẵn ở logo/Packages/ProjectSettings/UserSettings không đưa vào commit C.

Quyết định cuối của người dùng: **bỏ Thu/Mở, Trang bị + Túi cuộn chung như B**; **Nâng cấp/Gắn sao phải đến NPC và thao tác như cũ**, không làm trực tiếp trong kho đồ. Nút hành động dùng asset vàng của confirm box chat thế giới.

## Tình trạng bốn yêu cầu

| Yêu cầu | Đã thực hiện | Giới hạn cần test |
| --- | --- | --- |
| 1. Panel chi tiết | Full-height sát phải, một đường ngang tại nửa chiều cao, không card/khung lồng. Trên: icon, tên/cấp, trạng thái mặc, metadata/option, set, so sánh và số lượng thật. Dưới: description/content. Hai vùng cuộn riêng; nút cố định sát đáy. | Trên S22 thấp, thông tin dài phải cuộn cả nửa trên; không giảm font. Chưa có ảnh runtime. |
| 2. Hành động | Bỏ “Thao tác” và việc mở menu nút vàng ở Hành trang. Tap món túi hoặc trang bị đều mở chi tiết. Nút trực tiếp gọi `Command.performAction` cũ. Consumable có ba ô gán nhỏ + dòng danh sách vứt, dưới có Dùng/Auto/Bỏ ra nếu handler cũ cung cấp. Túi trang bị có Mặc/Bỏ ra; đang mặc có Tháo; Porata454 giữ Dùng và bấm đúp Dùng. | Theo quyết định mới, không có nút xử lý Nâng cấp/Gắn sao trực tiếp; sáu loại nâng cấp có dòng nhắc đến NPC. Vật liệu chỉ dùng lệnh Dùng/Bỏ ra mà menu cũ thực sự có, không tạo nghiệp vụ. Footer ba nút dùng hai hàng để không ép chữ ở panel hẹp. |
| 3. Giao diện/Rương đồ | Thêm hai dòng cuối Chức năng, cùng renderer/style danh sách cũ. Giữ các index cũ, gồm mục xoá acc. Giao diện gọi đường tab4/`setTabMod`; Rương gọi tab5/`setTabBox` và `paintBox`/`doFireBoxInvenTab` cũ. Đóng trang con quay về Chức năng; đường Giao diện từ Cài đặt vẫn quay về Cài đặt. | Rương giữ nguồn `arrItemBox` và handler tab cũ; nếu chưa có dữ liệu thì báo mở tại NPC thay vì dereference null; không tạo packet lấy dữ liệu rương mới. Cần test dữ liệu rương sau khi thao tác NPC. |
| 4. Cuộn chung theo yêu cầu mới | Trang bị, chip lọc và lưới túi cùng cuộn trong viewport dưới tab. Header/tab/tiền cố định. Bỏ hoàn toàn nút và state Thu/Mở; giữ nguyên cỡ ô. | S22 thấp phải vuốt từ Trang bị xuống Túi, không cố nhét cả hai vào cùng chiều cao. |

## File và hàm thay đổi

Đường dẫn dưới đây tương đối với `D:/project_Unity/client_Nro/Mod-nro-unity`; số dòng theo bản C.

- `Assets/Scripts/Hud/InventoryDetailPanel.cs` (+ `.meta`): partial `Panel`, `BuildInventoryDetail:73`, `PrepareInventoryBagDetail:131`, `AddInventoryDetailComparison:175`, `HandleInventoryDetailButton:197`, `PaintInventoryBagDetail:240`. Rect riêng cho vùng trên/dưới và từng nút; nội dung/cache nhãn dựng lại khi chọn món, đổi kích thước hoặc thay mảng body. Không tạo nội dung/texture mỗi frame; không split chữ nút mỗi paint.
- `Assets/Scripts/Hud/InventoryFrame.cs:30`, `Register`: đăng ký viewport cuộn chung, vùng cuộn chi tiết và hit-target từng nút. `EquipmentBounds:97`, `EquipmentBottom:104`, `BagCellBounds:115` dùng cùng nguồn scroll, chỉ trừ một lần. `UpdateInventoryFrameInput:263` giữ gate pointer của tab legacy; `PaintInventoryFrame:352` / `PaintInventoryEquipment:404` dùng cùng clip.
- `Assets/Scripts/Hud/InventoryBagPanel.cs`: `SyncInventoryBag:20` tiếp tục kiểm tra selected index/object trước hành động; `HandleInventoryEquipmentClick:73` mở chi tiết cho trang bị, giữ đúp454; `OpenInventoryItemDetail:89` thay popup B bằng detail C; `ScrollInventoryBagMouse` định tuyến wheel theo vùng. Grid vẫn5 cột, ghost không bấm được, giữ số ô thật và999+ trên ô.
- `Assets/Scripts/Panel.cs:3250`, `setTabTool`; `paintTools:4534`; `doFireTool:7672`: thêm hai dòng cuối bằng cùng renderer và giữ nguyên index cũ. `doFireInventory:7520` thêm tham số `detailOnly`; khi true chỉ trả danh sách `Command`, khi Hành trang đang dùng C thì không mở menu cũ.
- `Tests/Inventory/run.py`, `input.py`: cập nhật fixture/geometry và kỳ vọng cho cuộn chung Trang bị/Túi và confirm mở chi tiết. Bộ test C# này **chưa thực thi** trong C vì yêu cầu không build. Đã phân tích ngữ nghĩa fixture không tạo executable.
- `Tests/Inventory/detail_layout.py`: hợp đồng source và mô hình hình học. `Tests/Inventory/analyze.ps1`: Roslyn `GetDiagnostics` trên source/reference có sẵn, **không Emit, không tạo DLL/exe, không chạy compiler process hay Unity**. `check_fixtures.py` phân tích source fixture, chủ động bỏ qua bước tạo/chạy executable.

## Hàm/hành động cũ được tái dùng

| UI mới | Hàm/hành động giữ nguyên |
| --- | --- |
| Mặc / Tháo | `Panel.perform(2000, item)` / `perform(2002, item)` → `Service.getItem`, nguồn/index theo handler cũ. Các loại như bông tai/thú cưỡi được `InventoryBag.CanEquip` nhận đúng theo B, không viết lại chuyển đồ. |
| Sử dụng / Bỏ ra | Các `Command` id2001/id2003 của `Panel.doFireInventory` → `Panel.perform` → `Service.useItem`; giữ xác nhận vứt từ server, không gửi packet mới. |
| Auto Sử Dụng / Ngưng Auto | `AutoItem.perform` id1/id2 cùng parameter gốc. Bật Auto vẫn mở form nhập thời gian và đóng panel theo `OpenTFAutoUseItem` cũ; không thay luồng cấu hình đó. |
| Ba nút gán | `MobileQuickItems.AddAssignmentCommands` tạo Command id1/2/3; đã gán thì Command id4 gỡ tại ô tương ứng. Giữ quy tắc phải gỡ trước khi gán lại. Không gọi trực tiếp RMS ở panel mới. |
| Danh sách vứt | `AutoVutDo.perform` id7/id8 cùng parameter/template cũ; chỉ hiện nếu `CanAutoDiscard` đã cho menu cũ tạo lệnh. |
| Tên thuộc tính / sao / nền | `ItemOption.getOptionString`, `NinjaUtil.replace`; `InventoryItemVisuals.PaintStars` với asset sao gốc; `InventoryFrame.Rounded` / `InventoryFrameSkin` với `PopUp.goc` gốc. Màu nền game15196114/9993045/11837316, không dùng hex mockup. Font `mFont` gốc, chỉ số dùng font xanh hiện có. |

Khi chết, nút hành động vẫn bị chặn; xem chi tiết không gửi packet. Trước khi dispatch phải còn đúng object tại index selected, nếu server thay món thì đóng chi tiết. `Command.performAction` tiêu thụ pointer như cũ, tránh dispatch lại trong lần `panel.updateKey` thứ hai.

## Thông tin, set và so sánh

Lv clamp0–7 và sao clamp0–6, chỉ `InventoryBag.HasLevel` (type0–4/32). Set chỉ type0–4, cùng các option set đã dùng cho aura. Hiển thị nguyên chuỗi option server gửi và số món đang mặc cùng option /5; **không bịa công thức hoặc tỷ lệ bonus chưa có trong packet**.

Packet item hiện chỉ đưa danh sách option tổng hợp, không đánh dấu chỉ số nào sinh từ sao. Các dòng option có sẵn vẫn hiện đầy đủ, không tự gắn nhãn “thuộc tính sao” cho dòng không có nguồn xác minh. Bonus chỉ hiện theo chuỗi option có thật; không giả lập bonus server.

So sánh dùng chỉ số số học chung có cùng option ID giữa món trong túi và ô đang mặc tương ứng, theo một chỉ số chính đầu tiên phù hợp; không cộng HP, KI và damage thành một điểm. Dương xanh, âm đỏ, bằng nhau xám; ô trống ghi “đang trống”. Không có chỉ số chung thì ghi rõ. Không gọi `getCompare` legacy vì hàm đó ghi lại option22/23/param của item khi xem; C chỉ đọc dữ liệu. Skin/bông tai/pet không bị gán so sánh số học chưa có căn cứ.

## Cuộn chung, nút asset và S22

Theo yêu cầu mới nhất, bỏ hoàn toàn nút Thu/Mở. Trang bị, tiêu đề, chip lọc và túi cùng dùng `InventoryFrameScroll`; viewport cố định y73 đến trước thanh tiền2 logical. Giữ cơ chế tiêu thụ `isPointerJustDown` đã sửa ở B, chống reset điểm kéo; kéo từ ô trang bị, túi, chip hoặc khoảng trống đều cuộn được, không mở menu khi vuốt.

| Viewport | Viewport cuộn chung | Hàng túi đầu trước khi cuộn |
| --- | --- | --- |
| Mobile422×195 | y73, cao100 | y218: vuốt Trang bị lên để thấy Túi |
| Mobile844×390 | y73, cao295 | y278 |
| PC640×360 | y73, cao265 | y254 |
| PC1280×390 | y73, cao295 | y262 |

Không giảm ô40/64 hoặc44/56, không đổi zoom/font. S22 thấp vẫn xem đủ đồ bằng cuộn; không còn thiếu vùng túi do cố định Trang bị. Panel chi tiết vẫn ở bên phải, giới hạn bởi khoảng trống sau panel trái8 logical, không phủ túi/tiền. Cửa sổ rất hẹp ngoài các mẫu có thể làm chi tiết khó đọc, chưa tự chọn giao diện chồng panel.

`PaintInventoryBagDetail` dùng đúng `Command.paintOngMau` (`Assets/Scripts/Command.cs:202`), cùng đường vẽ của `MsgDlg` → `Dialog.paint` → `Paint.paintCmdBar` → `Command.paint`. Asset `/mainImage/btn0left.png`, `btn0mid.png`, `btn0right.png`; nhấn dùng bộ `btn1left/mid/right`, nạp bởi `GameCanvas.cs:485`. Chỉ lặp phần giữa như renderer gốc, không vẽ nút thay thế hoặc tạo sprite mới. Áp dụng cho nút hành động, gán ô nhanh và dòng danh sách vứt; nút đóng vẫn giữ icon X riêng.

Các rect nút cao24 logical đúng native asset, căn tọa độ nguyên qua `InventoryDetailNativeButtonRect:59`; phần gán/danh sách vứt mỗi hàng26 để không cắt đỉnh/nền. Rect vẽ và hit-test là một. Chữ đen đậm `mFont.tahoma_7b_dark`, khi nhấn `tahoma_7b_green2` như `Command.paint`; nút không áp dụng dùng chữ xám và không dispatch. Cache nhãn theo bề rộng; không split chữ mỗi frame.

Theo chỉnh sửa mới nhất, hàng gán ô nhanh và danh sách vứt cùng nằm trong cụm nút đáy, theo cập nhật bên dưới. Hai vùng nội dung vẫn cuộn riêng và footer không di chuyển. Cần xác nhận bằng mắt chữ/độ dễ bấm và safe-area trên máy thật; chưa chạy Unity hoặc chụp lại UI.

## Cập nhật lề chữ, danh sách vứt và tự đóng khi treo

- `Assets/Scripts/Hud/InventoryDetailPanel.cs:226` (`PaintInventoryDetailLines`): chừa thêm 4 logical ngang và 6 logical trên trong vùng clip; `AddInventoryDetailText:121` giảm bề rộng wrap tương ứng 8 logical. Font gốc `mFont.setTypePaint` tại `Assets/Scripts/mFont.cs:368` có `x--` tại dòng375 và dịch y theo font; vẽ ngay mép clip trước đây có thể cắt glyph. Giới hạn cuộn cộng 8 logical để vẫn xem hết dòng cuối sau khi thêm đệm. Không đổi renderer font chung.
- Nút thêm/xoá danh sách vứt chuyển xuống nửa dưới, ngay trên nhóm Sử dụng/Auto/Bỏ ra; vẫn dùng Command `AutoVutDo` cũ và cùng asset vàng. Hàng gán ô nhanh cũng đã chuyển xuống cùng cụm theo sửa tiếp dưới đây.
- Không có timer đóng panel chi tiết C. `Panel.timeShow` trong `Assets/Scripts/Panel.cs:3623` đếm ngược để khoá đóng tạm thời (`hideNow:6815`, `hide:6859`), không tự gọi đóng khi hết giờ. Đường gây đóng khi đồng bộ: `SyncInventoryBag` trước đây dùng khác reference → đóng. Trong khi đó `Controller` packet -36 tại `Assets/Scripts/Controller.cs:1855` tạo lại `arrItemBag`/`Item` (dòng1862/1873), dù nội dung món không đổi; packet -37 cũng tạo lại trang bị. Đây là nguyên nhân xác định từ code, chưa ghi packet của phiên treo thực tế để xác nhận trigger cụ thể.
- `Assets/Scripts/Hud/InventoryBagPanel.cs:21` (`SyncInventoryBag`) và `Assets/Scripts/Hud/InventoryBag.cs:16` (`SameDetailItem`): chỉ giữ panel nếu món tại **đúng ô đang chọn** có cùng template, info/content và danh sách option/param. Quantity được phép đổi. Khi reference mới: thay `currItem`/item đang xem, dựng lại Command bằng hàm cũ, giữ vị trí cuộn và reset double-tap. Quantity đổi trên cùng object cũng cập nhật chữ. Khi ô trống hoặc nội dung món đổi vẫn đóng để tránh thao tác nhầm.

Giới hạn: packet túi/trang bị không truyền ID riêng cho từng bản sao. Hai món cùng template/metadata/option đổi chỗ trong cùng ô không phân biệt được; hành động luôn được dựng lại theo object hiện tại ở ô đó, không tìm theo template sang ô khác. Khi server sắp xếp làm món chuyển ô hoặc thay option, panel vẫn có thể đóng. Không sửa packet/server trong lần này.

Kiểm tra lần này: **1.370** kiểm tra source/mô hình bố cục qua; phân tích ngữ nghĩa **255** file C# qua; ba nhóm fixture registry/input/Porata qua phân tích ngữ nghĩa (chưa chạy assertion runtime). Không build/Emit/DLL/Unity. Kiểm tra whitespace chỉ trên file đã sửa; lỗi whitespace có sẵn ở UserSettings không thuộc phạm vi.

Test cần xác nhận: PC/S22 mở Taiyaki và món có tên/mô tả dài, xem lề trái/phải và dấu dòng đầu/cuối khi cuộn; nút danh sách vứt ở dưới không che mô tả/nhóm đáy. Treo khi đang Auto dùng vật phẩm để phát sinh cập nhật túi: panel còn mở và số lượng cập nhật; thử dùng/hết món, đổi món cùng ô, mặc/tháo sau refresh và double-tap để bảo đảm chỉ gửi một thao tác. Trên S22 thấp nửa mô tả có thể rất ít chỗ, vẫn cuộn được; chưa xác nhận render thực tế.

Code thừa lần này: bỏ đường đóng vô điều kiện chỉ vì khác reference và vị trí/nới chỗ danh sách vứt cũ ở nửa trên. Không xoá thêm hàm/handler ngoài phạm vi; các mục legacy ghi bên dưới vẫn giữ nguyên.

### Chỉnh tiếp: gom toàn bộ nút xuống đáy

`Assets/Scripts/Hud/InventoryDetailPanel.cs:36` (`InventoryDetailControlsHeight`, `InventoryDetailActionTop`, `InventoryDetailDividerY`) và `InventoryDetailButtonBounds:48`: gom nút thành một cụm liên tục từ dưới lên: Sử dụng/Auto/Bỏ ra → danh sách vứt → hàng 1 trái/2 giữa/3 phải. Khoảng cách các hàng2 logical, giữ nguyên asset/nội dung/hành động và kích thước nút. Không còn hàng gán ô nhanh nằm lẻ phía trên đường chia.

Vùng mô tả dừng trước cụm nút. Trên viewport thấp, đường chia được nâng vừa đủ để còn vùng mô tả cuộn tối thiểu12 logical; màn đủ cao vẫn chia ở giữa. Không giảm kích thước ô/nút. Đã bỏ phép đặt hàng gán theo giữa màn và phép trừ chỗ của hàng này ở vùng thông tin phía trên. Không xoá thêm code legacy.

Kiểm tra cập nhật: **1.658** kiểm tra source/mô hình hình học qua, gồm hàng gán không chồng mô tả/nút đáy tại cả8 viewport; **255** file C# qua phân tích ngữ nghĩa, không Emit/build/Unity. Cần xem lại bằng mắt PC/S22: cả cụm nút nằm sát nhau ở đáy và mô tả cuộn phía trên. Các bước test refresh và thao tác món của lần trước vẫn áp dụng.

## Code thừa đã bỏ / giữ lại

Đã bỏ trong phạm vi: ID/rect `InventoryFrame.DetailActions`, nút “Thao tác”; đường dock `ChatPopup`/`paintDetail` của B; việc nối quantity vào `cp.says` và cuộn chi tiết qua `ChatPopup.cmyText`; state/property/ID/registry/hit-test/renderer Thu-Mở đã bị bỏ theo yêu cầu mới; đường hạn chế kéo chỉ riêng lưới túi; đoạn đo/vẽ nội dung chi tiết B đã được thay bằng hai vùng C. Không tạo `ChatPopup` rỗng chỉ để giữ cờ mở.

Giữ `Panel.addItemDetail`, `paintDetail`, `doFireInventory` và các Command cũ vì rương/shop/combine/giao dịch vẫn có caller. Không xoá các handler hoặc chức năng Auto chỉ vì không còn hàng menu ở Hành trang.

Chỉ ghi nhận ngoài phạm vi: `Panel.getCompare` thay đổi option của item; `Panel.subArray` trả null; các biến `flag`/khối `if (1 == 0)` trong `setTextColor` đã ghi ở A/B. Không xoá hoặc sửa các đường legacy dùng chung này. Server không bị sửa trong C.

## Kiểm tra đã chạy và test còn lại

- `python Tests/Inventory/detail_layout.py`: **1.151 kiểm tra source/mô hình hình học qua**: panel không đè nhau/tài nguyên, cuộn chung và asset nút confirm, số ô0/30/35/36/50/80/255 và cuộn cuối, bố trí footer/gán/discard không đè vùng cuộn, giữ handler và guard chết/stale. Đây là mô hình + hợp đồng source, không phải render hoặc phiên mạng thật.
- `pwsh -NoProfile -File Tests/Inventory/analyze.ps1`: **255 file C# không có lỗi ngữ nghĩa** theo reference/define sẵn có. Không Emit/build/Unity/DLL output.
- `python Tests/Inventory/check_fixtures.py`: ba nhóm fixture registry/input/Porata không có lỗi ngữ nghĩa; **không thực thi assertion C#** hoặc packet thật. Các con số pass runtime của B không được coi là đã chạy lại trong C.
- `git diff --check` các file C: qua. Chưa chạy Unity, chụp ảnh S22/PC, build hoặc test server.

Người dùng test: chọn từng loại món → chi tiết ngay; cuộn hai nửa độc lập; thử gán/gỡ cả ba ô, thêm/xoá danh sách vứt, cấu hình/tắt Auto và xác nhận Bỏ ra. Mặc/tháo phải đúng index sau lọc và server thay đồ, không Dùng khi chết. Giữ bấm đúp mặc đồ túi/đúp Porata454; kiểm tra Tháo và đúp không bị menu vàng chặn. Mở Giao diện/Rương cuối Chức năng và quay về; kiểm tra Settings Giao diện vẫn quay về Settings. Trên S22 thử kéo Trang bị/Túi/chip/khoảng trống, cả khối cùng cuộn và tiền/header đứng yên; kiểm tra tới ô thật cuối khi50/80 ô. Test item có ít/nhiều option, set13 màu, Lv7, sao6 và số lượng lớn, không cắt/mất nội dung khi cuộn.
