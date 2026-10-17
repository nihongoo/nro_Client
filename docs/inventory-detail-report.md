# Cụm C — chi tiết vật phẩm và hành động

## Nhánh và giới hạn

`feat/inventory-detail` tạo từ `feat/inventory-bag` tại **4c648ea**, theo xác nhận của người dùng. Fetch đã chạy; `origin/develop` vẫn ở **f4e58a9** (A/#23), chưa chứa tám commit B. Không tự merge/cherry-pick, không clone/worktree mới. PR của C cần chờ B vào develop hoặc đổi base khi làm PR; hiện chưa push.

Không chạy build/Unity/APK/server, không phát hành runtime, không sửa server/protocol, không push/merge/stash hoặc xoá branch. Các thay đổi sẵn ở logo/Packages/ProjectSettings/UserSettings không đưa vào commit C.

Hai quyết định bổ sung đã được người dùng xác nhận: màn thấp có nút **Thu/Mở Trang bị** do người chơi bấm; **Nâng cấp/Gắn sao phải đến NPC và thao tác như cũ**, không làm trực tiếp trong kho đồ.

## Tình trạng bốn yêu cầu

| Yêu cầu | Đã thực hiện | Giới hạn cần test |
| --- | --- | --- |
| 1. Panel chi tiết | Full-height sát phải, một đường ngang tại nửa chiều cao, không card/khung lồng. Trên: icon, tên/cấp, trạng thái mặc, metadata/option, set, so sánh và số lượng thật. Dưới: description/content. Hai vùng cuộn riêng; nút cố định sát đáy. | Trên S22 thấp, thông tin dài phải cuộn cả nửa trên; không giảm font. Chưa có ảnh runtime. |
| 2. Hành động | Bỏ “Thao tác” và việc mở menu nút vàng ở Hành trang. Tap món túi hoặc trang bị đều mở chi tiết. Nút trực tiếp gọi `Command.performAction` cũ. Consumable có ba ô gán nhỏ + dòng danh sách vứt, dưới có Dùng/Auto/Bỏ ra nếu handler cũ cung cấp. Túi trang bị có Mặc/Bỏ ra; đang mặc có Tháo; Porata454 giữ Dùng và bấm đúp Dùng. | Theo quyết định mới, không có nút xử lý Nâng cấp/Gắn sao trực tiếp; sáu loại nâng cấp có dòng nhắc đến NPC. Vật liệu chỉ dùng lệnh Dùng/Bỏ ra mà menu cũ thực sự có, không tạo nghiệp vụ. Footer ba nút dùng hai hàng để không ép chữ ở panel hẹp. |
| 3. Giao diện/Rương đồ | Thêm hai dòng cuối Chức năng, cùng renderer/style danh sách cũ. Giữ các index cũ, gồm mục xoá acc. Giao diện gọi đường tab4/`setTabMod`; Rương gọi tab5/`setTabBox` và `paintBox`/`doFireBoxInvenTab` cũ. Đóng trang con quay về Chức năng; đường Giao diện từ Cài đặt vẫn quay về Cài đặt. | Rương giữ nguồn `arrItemBox` và handler tab cũ; nếu chưa có dữ liệu thì báo mở tại NPC thay vì dereference null; không tạo packet lấy dữ liệu rương mới. Cần test dữ liệu rương sau khi thao tác NPC. |
| 4. Trang bị cố định | Geometry trang bị không còn trừ `cmy`; riêng cell túi trừ scroll. Header, khối trang bị, chip lọc, tiền đứng yên. Có Thu/Mở, mặc định mở; không tự thu hoặc giảm cỡ ô. Thiếu chỗ thì người chơi bấm Thu để thấy túi. | Không thể đồng thời có Trang bị mở + một hàng túi ở một số viewport thấp; đã có lựa chọn Thu/Mở, không tự co ô. |

## File và hàm thay đổi

Đường dẫn dưới đây tương đối với `D:/project_Unity/client_Nro/Mod-nro-unity`; số dòng theo bản C.

- `Assets/Scripts/Hud/InventoryDetailPanel.cs` (+ `.meta`): partial `Panel`, `BuildInventoryDetail:69`, `PrepareInventoryBagDetail:127`, `AddInventoryDetailComparison:171`, `HandleInventoryDetailButton:193`, `PaintInventoryBagDetail:236`. Rect riêng cho vùng trên/dưới và từng nút; nội dung/cache nhãn dựng lại khi chọn món, đổi kích thước hoặc thay mảng body. Không tạo nội dung/texture mỗi frame; không split chữ nút mỗi paint.
- `Assets/Scripts/Hud/InventoryFrame.cs:30`, `Register`: thêm vùng Trang bị cố định/Thu-Mở, vùng cuộn chi tiết và hit-target từng nút. `EquipmentBounds:101`, `EquipmentBottom:108`, `BagCellBounds:119` tách nguồn scroll. `UpdateInventoryFrameInput:268` nhận toggle và giữ nguyên gate pointer của tab legacy; `PaintInventoryFrame:358` / `PaintInventoryEquipment:413` dùng clip riêng.
- `Assets/Scripts/Hud/InventoryBagPanel.cs`: `SyncInventoryBag:20` tiếp tục kiểm tra selected index/object trước hành động; `HandleInventoryEquipmentClick:73` mở chi tiết cho trang bị, giữ đúp454; `OpenInventoryItemDetail:89` thay popup B bằng detail C; `ScrollInventoryBagMouse` định tuyến wheel theo vùng. Grid vẫn5 cột, ghost không bấm được, giữ số ô thật và999+ trên ô.
- `Assets/Scripts/Panel.cs:3250`, `setTabTool`; `paintTools:4534`; `doFireTool:7672`: thêm hai dòng cuối bằng cùng renderer và giữ nguyên index cũ. `doFireInventory:7520` thêm tham số `detailOnly`; khi true chỉ trả danh sách `Command`, khi Hành trang đang dùng C thì không mở menu cũ.
- `Tests/Inventory/run.py`, `input.py`: cập nhật fixture/geometry và kỳ vọng cho Trang bị cố định, Thu/Mở và confirm mở chi tiết. Bộ test C# này **chưa thực thi** trong C vì yêu cầu không build. Đã phân tích ngữ nghĩa fixture không tạo executable.
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

## Chiều cao thấp và S22

Mô hình theo công thức thực, đơn vị logical; tiền bắt đầu tại `h-20`, viewport túi kết thúc trước đó2:

| Viewport | Trang bị mở: túi bắt đầu / cao khả dụng | Trang bị thu: túi bắt đầu / cao khả dụng |
| --- | --- | --- |
| Mobile422×195 (844×390/zoom2) | y226 /0 | y140 /33, đủ một hàng22 |
| Mobile844×390 | y286 /82, một hàng44 và phần hàng sau | y140 /228 |
| PC640×360 | y262 /76, hai hàng28 | y140 /198 |
| PC1280×390 | y270 /98, hai hàng khoảng30,3 | y140 /228 |

Giữ ô trang bị40/64 và ô túi44/56 theo `InventoryFrame.Unit` hiện có; không thêm hệ số co. Nút Thu/Mở cao22 logical, nút footer24, chip22. Chưa đo thao tác tay trên S22 nên không khẳng định đạt mọi mức dpi/safe-area. Màn thấp hơn195 có thể clip một phần Trang bị khi mở; cần Thu hoặc viewport lớn hơn, không tự giảm ô.

Panel phải bị giới hạn bởi khoảng trống thực sau panel trái (chừa8 logical), không phủ túi/tiền. Các viewport kiểm tra còn chỗ; nếu cửa sổ rất hẹp ngoài các mẫu thì panel phải có thể quá hẹp để đọc. Chưa tự chọn chế độ chuyển màn hoặc chồng panel trong trường hợp đó.

Nửa trên/dưới đều cuộn riêng vì S22 thấp không đủ chỗ hiển thị hết option, số lượng, so sánh và các nút gán cùng lúc. Nút gán/discard nằm cố định trong nửa trên, footer cố định; cần kiểm tra chữ tiếng Việt và nhận kéo ở từng vùng trên máy thật.

## Code thừa đã bỏ / giữ lại

Đã bỏ trong phạm vi: ID/rect `InventoryFrame.DetailActions`, nút “Thao tác”; đường dock `ChatPopup`/`paintDetail` của B; việc nối quantity vào `cp.says` và cuộn chi tiết qua `ChatPopup.cmyText`; phép trừ scroll khỏi khối Trang bị; đường kéo/wheel chung Trang bị+Túi; đoạn đo/vẽ nội dung chi tiết B đã được thay bằng hai vùng C. Không tạo `ChatPopup` rỗng chỉ để giữ cờ mở.

Giữ `Panel.addItemDetail`, `paintDetail`, `doFireInventory` và các Command cũ vì rương/shop/combine/giao dịch vẫn có caller. Không xoá các handler hoặc chức năng Auto chỉ vì không còn hàng menu ở Hành trang.

Chỉ ghi nhận ngoài phạm vi: `Panel.getCompare` thay đổi option của item; `Panel.subArray` trả null; các biến `flag`/khối `if (1 == 0)` trong `setTextColor` đã ghi ở A/B. Không xoá hoặc sửa các đường legacy dùng chung này. Server không bị sửa trong C.

## Kiểm tra đã chạy và test còn lại

- `python Tests/Inventory/detail_layout.py`: **1.107 kiểm tra source/mô hình hình học qua**: panel không đè nhau/tài nguyên, Thu mở được ít nhất một hàng túi, số ô0/30/35/36/50/80/255 và cuộn cuối, bố trí footer/gán/discard không đè vùng cuộn, giữ handler và guard chết/stale. Đây là mô hình + hợp đồng source, không phải render hoặc phiên mạng thật.
- `pwsh -NoProfile -File Tests/Inventory/analyze.ps1`: **255 file C# không có lỗi ngữ nghĩa** theo reference/define sẵn có. Không Emit/build/Unity/DLL output.
- `python Tests/Inventory/check_fixtures.py`: ba nhóm fixture registry/input/Porata không có lỗi ngữ nghĩa; **không thực thi assertion C#** hoặc packet thật. Các con số pass runtime của B không được coi là đã chạy lại trong C.
- `git diff --check` các file C: qua. Chưa chạy Unity, chụp ảnh S22/PC, build hoặc test server.

Người dùng test: chọn từng loại món → chi tiết ngay; cuộn hai nửa độc lập; thử gán/gỡ cả ba ô, thêm/xoá danh sách vứt, cấu hình/tắt Auto và xác nhận Bỏ ra. Mặc/tháo phải đúng index sau lọc và server thay đồ, không Dùng khi chết. Giữ bấm đúp mặc đồ túi/đúp Porata454; kiểm tra Tháo và đúp không bị menu vàng chặn. Mở Giao diện/Rương cuối Chức năng và quay về; kiểm tra Settings Giao diện vẫn quay về Settings. Trên S22 thử Trang bị mở không bị kéo, Thu/Mở thủ công, chỉ túi cuộn và tiền/header đứng yên; kiểm tra tới ô thật cuối khi50/80 ô. Test item có ít/nhiều option, set13 màu, Lv7, sao6 và số lượng lớn, không cắt/mất nội dung khi cuộn.
