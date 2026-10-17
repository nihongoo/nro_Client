# Cụm B — túi đồ dạng lưới

## Sửa góc trắng và giảm viền theo ảnh test

Ảnh test người dùng có các mảng trắng ở góc khối Trang bị, ô đồ, badge Lv và footer. Nguyên nhân: `mGraphics.DrawRegionTexture` (`Assets/Scripts/mGraphics.cs:10`) dùng `Graphics.DrawTexture`; sprite góc trắng `PopUp.goc` không nhận tint `GUI.color` như phần fill. Cách tint của lần sửa trước chưa xử lý đường vẽ sprite này.

- `Assets/Scripts/Hud/InventoryFrameSkin.cs:11`, `Corners`: dùng `HudButtonSkin.CopyNative` tạo bản sao **sprite gốc**, tô phần trắng theo nền và phần đen theo màu viền. `TintCorners:32` giữ nguyên alpha/hình bo. Không sửa asset nguồn hay tự vẽ hình học góc.
- Cache theo màu nền/viền/độ mờ, giới hạn 64 bản; đổi sprite/zoom thì huỷ bản sao cũ. Không copy/read GPU mỗi frame. Kiểm tra 10.000 lần lấy cùng skin không phát sinh bản sao mới.
- `InventoryFrame.Rounded:145` truyền sprite đã tô vào overload `PopUp.paintPopUp:181`. Hàm 7 tham số cũ tại `PopUp.cs:176` vẫn giữ nguyên, dùng góc gốc/viền đen; không đổi giao diện của popup/tab ở màn khác.
- `PaintInventoryEquipment`, `InventoryFrame.cs:391`: bỏ lớp khung ngoài/khung trong lồng nhau, chỉ còn **một khung bo**. Ô chọn dùng viền vàng, ô có set dùng màu set; ô thường viền mảnh đen. Nền, icon, số ô, vị trí, badge và thao tác không đổi.
- Code thừa đã bỏ: cách tint RGB dựa vào `GUI.color` không tác động lên sprite, các lần vẽ khung bo lồng/viền glow thừa ở ô trang bị. Không xoá chức năng chọn hay nhận diện set.
- `python Tests/Inventory/skin.py`: **58.962 kiểm tra qua** trên PNG góc x1–x4 và code recolor/cache thực (trắng→đúng nền, đen→đúng viền, alpha, nguồn không đổi và tái dùng cache). Các bộ 34.061 geometry/model và 60 input vẫn qua; compile C# client/Editor qua. Chưa chạy Unity/Play Mode/build APK; cần người dùng xác nhận ảnh sau sửa.

Ảnh mới cũng xác nhận nhãn client đang hiển thị **50 ô thật** cho acc test. Theo lựa chọn đã chốt, client giữ đủ 50 ô (5×10), cuộn xuống tiếp, không giảm về 35 hoặc sửa server.

Nhánh `feat/inventory-bag` tạo trong repo chính từ `develop`/`origin/develop` **f4e58a9**, sau khi fetch và xác nhận PR #23 đã merge. Tree của cụm A trùng tree develop tại thời điểm tạo nhánh. Không clone/worktree mới, không sửa server/protocol, không build Unity/APK, không push/merge/stash và không xoá nhánh.

Đường dẫn client dưới đây tương đối với `D:/project_Unity/client_Nro/Mod-nro-unity`; đường dẫn server tương đối với `D:/dowload/SRC NRO NEW 03/SRC`. Số dòng theo bản B.

## Đã làm

- Lưới **5 cột, tối thiểu 7 hàng**, ô **44 mobile / 56 PC design px**, dùng quy đổi `InventoryFrame.Unit` như cụm A. Túi không còn dùng danh sách tên/chỉ số theo hàng; trong ô chỉ có icon, Lv khi áp dụng và badge số lượng. Tên/chỉ số chỉ hiện ở phần xem chi tiết bên phải.
- Theo xác nhận người dùng, **Trang bị + Túi cùng cuộn dọc**, header/tab/thanh tiền cố định. Cuộn bằng kéo hoặc wheel hiện có; không thêm phím. Rect vẽ và hit-test lấy từ `HudLayout`, cắt theo cùng viewport. Ô đã cuộn ra ngoài không bắt input lên header/tab/footer.
- Bốn chip Tất cả / Trang bị / Tiêu hao / Nguyên liệu. Bản đồ `inventoryBagMap` giữ **chỉ số arrItemBag gốc**; lọc chỉ đổi thứ tự hiển thị, không sửa dữ liệu hay chỉ số gửi server. Chip hẹp dùng hai dòng với font gốc, không cắt mất chữ.
- Tap đầu chọn và mở phần xem chi tiết bên phải; tap món khác cập nhật nội dung. X của phần chi tiết giữ nguyên `selected`. Mở phần chi tiết chỉ đọc dữ liệu, không gửi packet.
- Tap đúp/click đúp cùng món trang bị trong 350 ms gọi **`perform(2000, item)`** đúng một lần cho cặp tap, tái dùng handler mặc cũ. Material/consumable không gọi handler dùng/mặc. Kéo, đổi bộ lọc, thao tác vào vùng khác hoặc server thay object item sẽ huỷ cặp tap. Khi chết, giữ thông báo/chặn thao tác như màn cũ.
- Khung ô/chip dùng skin bo gốc từ cụm A (`InventoryFrame.Rounded`, `PopUp.paintPopUp`, `myTexture2dbd3.png` / `myTexture2dimgPopup*.png`), không tạo sprite bo mới. Ô trống giữ khung bo như A; không tự vẽ viền đứt khi chưa có tài nguyên phù hợp.

## Số ô thật và quyết định đã chốt

Server `src/nro/models/database/PlayerDAO.java:131` tạo mặc định **30 ô**; `src/nro/models/player/Inventory.java:21` đặt tối đa **80 ô**; item517 tại `src/nro/models/services/InventoryService.java:789` mở thêm **1 ô**. Client nhận số ô qua `Assets/Scripts/Controller.cs:1862`, `:5536`, rồi cấp `Char.myCharz().arrItemBag`. Không có căn cứ coi số ô của acc đang test luôn là 30 hoặc 35.

Đã thực hiện đúng lựa chọn người dùng:

| Số ô thật server gửi | Client xử lý |
| --- | --- |
| 30 | 30 ô thật + 5 ô mờ không bấm được, tổng 5×7 |
| 35 | 35 ô thật, tổng 5×7 |
| 36 | Giữ đủ 36 ô thật, thêm hàng thứ 8 để cuộn; 4 vị trí cuối chỉ là nền mờ |
| 80 | Giữ đủ 80 ô thật, tổng 5×16; cuộn xuống các hàng tiếp theo |

Ô padding chỉ có hình nền alpha100/255; không có item/index server và không có hit-target ô. Không cấp thêm slot, không giảm slot, không đổi giá. Khi lọc, chỉ xếp những món phù hợp, vẫn giữ tối thiểu 35 vị trí hiển thị; các ô còn lại không bấm được. Nhãn **`Túi đồ (N ô)`** hiển thị đúng `arrItemBag.Length` đang nhận ở runtime để người dùng thấy số thực. Chưa chạy acc/Unity nên báo cáo không khẳng định con số slot của acc hiện tại.

Registry chuẩn bị tên cho tối đa 256 vị trí theo trường byte của packet; đây không phải giới hạn/cấp thêm túi và không đổi số 80 của server. Vị trí chỉ active khi tồn tại trong bản đồ ô thật.

## Badge số lượng

- `InventoryBagBadge.Paint` dùng **`Notifications.NotificationSkin.BadgeIcon`**, skin đỏ HUD hiện có (`/mainImage/notification-badge.png`), ghép lại phần giữa và giữ đầu bo như badge HUD. Không đổi vị trí/icon khi thêm badge, không có offset `quantity > 1 ? 8 : 0` của danh sách cũ.
- Badge là lớp phủ gắn góc dưới phải, tràn 2 logic; chỉ clip theo **viewport cuộn**, không clip theo khung ô. Hit-test ô bao gồm vùng badge tràn; có thể tap lên badge để chọn món. Cuộn ra khỏi viewport vẫn bị clip đúng ở rìa vùng cuộn.
- Chiều rộng cố định theo nhãn lớn nhất trong các định dạng `9999`, `999,9k`, `999,9M`, `2,1B`; giới hạn theo bề rộng ô + khoảng trống giữa ô để không đè badge hàng xóm. Giữ font gốc, chỉ giảm cỡ chữ trong badge khi không đủ chỗ; cache phép đo theo font/zoom/kích thước ô. Bitmap zoom1 dùng font bitmap gốc và scale phần chữ, không dịch icon.
- Dưới 10.000: `758`, `9999`. Từ 10.000: `10k`, `24,3k`; hàng triệu dùng `M`, hàng tỷ dùng `B` để cả giá trị `int.MaxValue` vừa nhãn (`2,1B`). Dùng tối đa một chữ số thập phân và cắt xuống; `999999 → 999,9k`. Số đầy đủ thêm vào phần xem chi tiết, không đưa tên/chỉ số trở lại ô.
- Lv chỉ áp dụng `type` 0–4 và 32, theo sáu loại nâng cấp đã chốt. Skin/type5, bông tai, vật phẩm khác không hiện Lv trong túi. Lv nằm phía trên, badge số lượng phía dưới để không chồng nhau.

## File và hàm chính / tái dùng

| File | Thay đổi / hàm thực tế |
| --- | --- |
| `Assets/Scripts/Hud/InventoryBag.cs:13` | `InventoryBag`: hằng cột/hàng, `Map`, `Rows`, `Quantity`, `HasLevel`, `Category`; `InventoryBagDoubleTap.Tap:58` xác nhận cặp tap theo index và object hiện tại |
| `Assets/Scripts/Hud/InventoryBagPanel.cs:19` | Partial `Panel`: `SyncInventoryBag`, `HandleInventoryBagClick:38`, `PaintInventoryBag:142`, cuộn/chọn/chi tiết |
| `Assets/Scripts/Hud/InventoryBagBadge.cs:14` | `InventoryBagBadge.Paint`: đo/cache chữ và vẽ badge skin HUD |
| `Assets/Scripts/Hud/InventoryFrame.cs:30` | `Register`: rect ô/bộ lọc/preview, `BagCellBounds:108`, viewport cuộn chung; `SyncInventoryFrame:209` tính giới hạn cuộn và giữ header/footer cố định |
| `Assets/Scripts/Panel.cs:2632` | `updateScroolMouse`: chuyển wheel vào đường cuộn túi/preview khi màn này đang mở |
| `Assets/Scripts/Panel.cs:5600` | `paintInventory`: bỏ chế độ danh sách túi tạm `bagOnly`; giữ hàm legacy cho shop/rương/giao dịch và các màn dùng chung |
| `Tests/Inventory/run.py`, `input.py` | Registry/geometry/model và harness chạy các phương thức input thực của `Panel`, không chạy game/server |

`InventoryBag.CanEquip:14` tái dùng **`Item.isTypeBody` (`Assets/Scripts/Item.cs:382`)**, bổ sung thú cưỡi type24 và đúng ba bông tai type27 ID **454/921/1819** đã đối chiếu trong SQL (`sql/nro1.sql:5161`, `:5629`, `:6529`). Server `InventoryService.putItemBody:289` chấp nhận type24/27; `itemBagToBody:397` dùng cùng đường mặc và thay món cũ về túi. Không coi mọi item type27 là trang bị vì nó còn chứa đồ ăn/capsule/mảnh. Tap đúp bông tai chỉ gọi hành động **Mặc 2000**, không gọi Dùng 2001: tránh kích hoạt hợp thể ở `src/nro/models/services_func/UseItem.java:807`, `:810`, `:813`. Dùng qua menu cũ vẫn giữ hành vi hợp thể hiện có.

`Category` là nhóm hiển thị ở client: nhóm Trang bị theo `CanEquip`; nhóm Tiêu hao gồm type6/7/13/16/22/25/27/28/29/31/37/75 từ template/handler hiện có, sau khi loại trang bị. Các mảnh bông tai **933/934/1820** là Nguyên liệu dù cùng type27 (SQL `:5641`, `:5642`, `:6530`); phần còn lại nằm Nguyên liệu (gồm vật liệu ghép/nhiệm vụ/card…). Đây không phải luật được server gửi; có thể còn item ngoại lệ nằm trong type chung. Tất cả luôn cho thấy đầy đủ, bộ lọc không thay đổi quyền sử dụng hay packet. Cần test thêm món đặc biệt trên acc thực.

## Phần xem chi tiết trong B và ranh giới C

`PrepareInventoryBagDetail:73` và `PaintInventoryBagDetail:81` **dock popup item hiện có** sang phải full-height, rộng 250/320 design px; tái dùng `Panel.addItemDetail`, `Panel.paintDetail`, `ChatPopup` để giữ nội dung/option/màu font hiện có. Nút **Thao tác** mở `doFireInventory` cũ để vẫn Mặc/Tháo/Dùng/Vứt/gán ô nhanh được; phím xác nhận cũ vẫn mở menu cũ. Mặc gửi qua `Panel.perform(2000, ...)` → `Service.getItem`, opcode -40 type4 với index túi gốc. Không viết lại nghiệp vụ chuyển đồ, thay đồ cũ về túi do server thực hiện.

Chưa làm bố cục C chia nửa, bảy vạch level, bonus set/so sánh mới, hàng nút hành động riêng, gán ô nhanh trong panel, nâng cấp/gắn sao trực tiếp. Nội dung B vẫn là popup formatter cũ được dock; chưa thiết kế lại nó theo C. Server thay object món đang xem thì đóng preview để không dùng thông tin cũ; người dùng tap lại món mới.

## Code thừa đã xoá / chỉ ghi nhận

- Xác nhận toàn bộ chỗ gọi không còn `paintInventory(..., true)`, sau đó xoá tham số `bagOnly`, vòng lặp/y-offset dành riêng danh sách túi tạm và nhánh vẽ bo tương ứng ở `Panel.paintInventory`. Không xoá hàm dùng chung.
- Bỏ đoạn click túi theo `(py - yScroll + cmy) / ITEM_HEIGHT` và giới hạn cuộn theo số hàng danh sách 24px trong frame; thay bằng bản đồ ô/grid rect.
- Bỏ ID registry `inventory.legacy-bag`, thay bằng viewport túi; bỏ đường vẽ danh sách cũ ở frame chính.
- Code ngoài phạm vi vẫn giữ: các biến `flag` không dùng / khối `if (1 == 0)` trong `Panel.setTextColor` đã ghi ở báo cáo A. Không xoá chúng, không sửa các handler/bộ lọc auto khác.

## Kiểm chứng và giới hạn

- `python Tests/Inventory/run.py`: **34.061 kiểm tra qua**, biên dịch registry/geometry/model thực; gồm 0/30/35/36/80/255 ô, 5 cột, cuộn đến ô thật cuối, ghost không bắt input, badge không vượt hai cột biên, header/tab không bị ô cuộn che hit-test; null startup, màu set, nguồn alpha tiền và footer A vẫn qua. Có kiểm tra riêng bông tai/mảnh và gate Lv.
- `python Tests/Inventory/input.py`: **60 kiểm tra qua** trên các phương thức input thực của `Panel`, PC 640×360 zoom2 / 1280×720 zoom4 và mobile 422×195 zoom2 / 844×390 zoom4. Kiểm tra index gốc37 sau lọc, tap đầu chỉ preview, tap đúp một dispatch2000, nguyên liệu/tiêu hao không dispatch, drag/wheel, server thay object, khi chết, X giữ chọn, ghost và tap vùng badge tràn, phím cũ. Các handler mạng/render là fixture; không khẳng định đã gửi/giao dịch thật với server.
- `python Tests/Inventory/compile.py`: **Assembly-CSharp và Assembly-CSharp-Editor qua**, compiler C# độc lập dùng reference hiện có; không build Unity/APK.
- `git diff --check` các file của cụm B: qua. Chưa chạy Unity/Play Mode, chưa có ảnh S22/PC thực.

### Checklist người dùng test

1. S22/PC: cuộn từ Trang bị xuống đủ 7 hàng; header/tab/tiền đứng yên; scroll qua ô cuối của túi thật. Đọc nhãn N ô; với acc30 phải có 5 ô mờ không chọn được; acc lớn hơn35 không mất đồ.
2. Chọn **Sao pha lê lam ×758**: icon luôn ở giữa khung, badge không kéo icon sang trái và không bị cắt mép ô. Test 9.999 / 10.000 / 24.300 / số lớn, các cột biên và hàng cuối; xem số đầy đủ trong preview.
3. Lọc từng nhóm, tap một món mở preview; đổi món cập nhật; X giữ chọn. Xong lọc tap đúp trang bị → mặc đúng món, món cũ về túi; tap đúp đá/đậu không được Dùng.
4. Thử kéo bắt đầu trên icon/chip/badge, wheel PC, phím điều hướng/xác nhận cũ; không gửi nhầm Mặc khi kéo. Đổi tab/đóng mở, đăng nhập lại không có exception lặp.
5. Nút Thao tác/menu cũ: Dùng/Vứt/gán ô nhanh còn hoạt động, rương/shop/giao dịch không đổi. Test khi chết và khi server cập nhật túi trong lúc preview mở.

Rủi ro riêng S22: safe-area/zoom4 chưa xem trực tiếp; badge có thể phải giảm cỡ font để vừa nhãn tối đa, cần đánh giá dễ đọc. Dấu level/nhãn chip và nội dung `ChatPopup` cũ cần kiểm tra bằng mắt ở panel phải hẹp. Tổng lưới dài hơn màn hình theo thiết kế đã chốt, không hiển thị đủ Trang bị + 7 hàng đồng thời. Viewport hẹp vẫn giữ mức rộng tối thiểu footer của A.

## Git và việc tiếp theo

`feat/inventory-frame` local + `origin/feat/inventory-frame` đã có squash merge #23 trên develop, là ứng viên dọn sau khi người dùng duyệt; **chưa xoá**. Các ứng viên/stash/worktree còn lại giữ như báo cáo A, chưa có quyết định xoá.

Các thay đổi sẵn có của người dùng ở logo, Packages, ProjectSettings và UserSettings không đưa vào commit B. Dừng sau B cho người dùng test/merge; không bắt đầu C. Không tự quyết giá mở thêm ô, số ô server khởi đầu mới, xử lý mảnh đá hoặc khóa món.
