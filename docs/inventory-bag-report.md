# Cụm B — túi đồ dạng lưới

## Sửa kéo cuộn Trang bị + Túi trên mobile

Nguyên nhân xác nhận từ code: `GameCanvas.pointerPressed` (`Assets/Scripts/GameCanvas.cs:2150`) bật `isPointerJustDown`; `pointerDragged:2123` không tắt cờ. `Panel.UpdateInventoryFrameInput` (`Assets/Scripts/Hud/InventoryFrame.cs:250`) trước đây khởi tạo lại `inventoryDragY` / `inventoryDragScroll` mỗi lần thấy cờ đó, nên delta bị về0 trong lúc giữ kéo. `GameCanvas.update` còn có thể gọi `panel.updateKey` hai lần mỗi tick (`GameCanvas.cs:654`, `:666`). Bộ test cũ tự tắt cờ sau lần nhấn nên chưa bắt được lỗi này.

- `UpdateInventoryFrameInput`: tiêu thụ `GameCanvas.isPointerJustDown` sau khi chốt điểm nhấn và điểm bắt đầu cuộn chi tiết, chỉ chốt một lần cho mỗi gesture. Giữ nguyên `HudLayout` và vùng cuộn chung.
- Áp dụng vị trí cuối khi nhận release; tiêu thụ release kể cả sau swipe/khi nhả ở khoảng trống để không xử lý lại. Swipe không gọi chọn/mặc/mở menu.
- `UpdateInventoryBagDetailDrag`, `Assets/Scripts/Hud/InventoryBagPanel.cs:119`: nhận cả vị trí release cuối, cùng cơ chế giữ điểm nhấn. Không đổi layout, giới hạn cuộn, wheel PC, header/tab/footer, server hoặc dữ liệu món.
- `Tests/Inventory/input.py`: thêm phương thức `GameCanvas.pointerPressed` / `pointerDragged` / `pointerReleased` thực vào harness, không tự tắt cờ để giả lập một gesture hợp lệ. Kiểm tra kéo từ ô trang bị, ô túi, chip lọc và khoảng trống; hai update liên tiếp mỗi bước, nhiều bước giữ kéo, vị trí release, không dispatch click/mặc/menu, header/footer cố định, trên bốn viewport mobile/PC zoom2/4. **156 kiểm tra qua** (trước đây60). Bỏ riêng dòng tiêu thụ JustDown trong bản source đưa vào harness thì test thất bại đúng tại `inventory did not consume first press`; không sửa file game để chạy đối chứng.
- Registry/model34.061 và compile C# client/Editor độc lập qua. Chưa chạy Unity/Play Mode/build APK hoặc xác nhận touch thật qua toàn bộ `MobileCombatPointers.Poll`.

Không xoá code game ngoài phạm vi, không phát hiện thêm hàm chết cần xoá trong bản sửa này. Phần giả lập cờ pointer của test cũ vẫn dùng cho các kiểm tra tap trước đó; ca kéo mới dùng code pointer thực để tránh bỏ sót cùng lỗi. Test máy thật: vuốt lên/xuống từ Trang bị sang Túi, từ ô/icon/chip/khoảng trống; kiểm tra cuộn đến cuối rồi quay đầu, không bật menu hoặc mặc đồ khi vuốt, và header/tab/tiền vẫn cố định. Wheel/click/tap đúp PC cần giữ hành vi cũ.

## Porata [454] tự chuyển thật sang ô Tai

Theo lựa chọn mới, không làm icon đại diện cho món còn trong túi: server chuyển **chính món Porata [454]** từ túi sang `itemsBody[7]`, trả ô túi về trống. Không cấp thêm sức chứa hay che món còn chiếm slot. Việc chuyển xảy ra trước gói đăng nhập trong `Service.player` (`src/nro/models/services/Service.java:816`) và trước đồng bộ túi trong `InventoryService.sendItemBags` (`src/nro/models/services/InventoryService.java:603`). Chi tiết server tại `docs/porata-equipment-report.md` trong repo server.

- `InventoryBag.EquippedPorataIndex`, `Assets/Scripts/Hud/InventoryBag.cs:14`: nhận đúng ID454 ở vị trí7, chịu được null/danh sách chưa đủ. Ô Tai vốn đọc vị trí này nên không cần đổi layout hoặc hit-test.
- `Panel.doFireInventory`, `Assets/Scripts/Panel.cs:7519`, nhánh tại7558: Porata đã mặc có **Dùng** thay **Lấy ra**, tái dùng `perform(2001, ...)` gửi `useItem(0, 0, 7, -1)` để hợp thể/tách. Có thể gán ô nhanh như trước.
- `MobileQuickItems.Get:52` / `Use:75`, `Assets/Scripts/Hud/MobileQuickItems.cs`: ô nhanh lấy đúng Porata trên người và gửi nguồn body; nếu món còn trong túi thì giữ đường bag. Thả nút vẫn giải lại nguồn/index hiện tại, không giữ index túi cũ.
- `MainMod.UseItem`, `Assets/Scripts/Mod.CuongLe/MainMod.cs:1159`: ưu tiên Porata trên người; phím F tại887 nhận nguồn này. `Yardat.AutoPotaraFusion`, `Assets/Scripts/Mod.CuongLe/Yardat.cs:69`, dùng lại hàm này để tránh dereference `FindItemBag(454)` khi món đã rời túi.
- Không tự xoá bông tai921/1819. Nếu ô Tai đã có món khác, server không thay thế; nếu nhiều bản454, chỉ một món chuyển, bản dư vẫn còn. Giữ kiểm tra giới tính/sức mạnh template và option21. Porata đang ở rương vẫn ở rương, lấy ra túi sẽ được xét tự mặc khi đồng bộ.

Kiểm tra riêng `python Tests/Inventory/porata.py`: **15 kiểm tra qua** với code routing thực cho phím tắt, ô nhanh, `Panel.perform` action2001, thiếu món, đổi nguồn, gán lại ô, và bông tai921 vẫn dùng đường túi. Registry/model34.061, input60 và compile C# client/Editor độc lập qua. Server có26 kiểm tra chuyển đồ/routing thực và compile Java toàn bộ nguồn trong thư mục kiểm tra riêng qua; chưa chạy server/DB/Unity, không chuyển runtime đang chọn. Test tích hợp vẫn cần người dùng thực hiện.

Test sau khi dùng bản server và client mới: đăng nhập acc có454/ô Tai trống/đủ sức mạnh; Tai phải hiện icon, túi giảm một ô bị chiếm nhưng tổng sức chứa giữ nguyên. Bấm Tai → Dùng để hợp thể/tách; thử F, auto hợp thể và ô nhanh; đăng xuất/đăng nhập lại kiểm tra món ở Tai được lưu và không nhân đôi. Thử ô Tai có món khác, thiếu sức mạnh, nhiều bản454 và921/1819 để kiểm tra không mất đồ.

Code thừa trong phạm vi: bỏ đường gọi packet trực tiếp kèm dereference `FindItemBag(454).indexUI` ở `Yardat.AutoPotaraFusion`, thay bằng đường dùng chung. Không xoá hàm `FindItemBag`, vì vẫn có caller khác. Ghi nhận chưa sửa: `InventoryService.itemBodyToBox` có kiểm tra index ngược ở repo server; không nằm trong thay đổi này. Các biến/khối không dùng đã ghi trước vẫn giữ.

## Điều chỉnh góc Lv và giới hạn số lượng hiển thị

- `InventoryItemVisuals.LevelBounds`, `Assets/Scripts/Hud/InventoryItemVisuals.cs:23`: đưa badge Lv túi ra góc trên trái, nhô 3 đơn vị logic theo cả hai trục, cân với badge trang bị nhô 3 đơn vị ở góc dưới phải. Giữ nguyên khung, icon, aura, hit-test và vị trí badge số lượng.
- `InventoryBag.Quantity`, `Assets/Scripts/Hud/InventoryBag.cs:42`: đến 999 hiện số thật; từ 1.000 trở lên hiện **999+** (ví dụ 1.148 → 999+). Đây là giới hạn hiển thị trên ô; không giới hạn stack thật, số ô hay sửa server. Panel chi tiết vẫn hiện số đầy đủ.
- `InventoryBagBadge.maximumLabels`, `Assets/Scripts/Hud/InventoryBagBadge.cs:12`: đo chiều rộng theo `999` / `999+`, tiếp tục dùng badge đỏ bo gốc và căn giữa chữ.
- Code thừa đã bỏ trong phạm vi: phép chia/làm tròn và hậu tố k/M/B không còn dùng trong `Quantity`, cùng các nhãn đo chiều rộng tương ứng. Không xoá code khác.
- Kiểm tra: model/geometry 34.061, hình học Lv/aura/sao 65.180, input 60, skin 58.962 đều qua; compile C# client và Editor độc lập qua. Không chạy Unity/Play Mode/build APK. Cần xem lại trên S22/PC: Lv ở cột đầu/hàng đầu khi cuộn và badge số lượng với 999 / 1.000 / 1.148; panel chi tiết phải giữ số thật.

## Sao asset gốc, badge Lv và aura set phía trong

Theo ảnh test tiếp theo, thay chỉ báo sáu chấm bằng **asset sao gốc**: `Panel.imgStar` (`/mainImage/star.png`) cho sao đã gắn và `Panel.imgMaxStar` (`/mainImage/starE.png`) cho sao trống, nạp tại `Panel.cs:720`, `:721`. `InventoryItemVisuals.PaintStars` giữ đúng sáu vị trí, đọc option102 như trước. Hàng sao căn giữa dưới ô, không đè badge Lv hoặc ô ở hàng kế tiếp; chỉ sáu ô nâng cấp có hàng sao.

`Assets/Scripts/Hud/InventoryItemVisuals.cs`, `PaintLevel`: badge Lv vàng bo gốc, độ rộng cố định theo `Lv7`, chữ đen `MiddleCenter`, padding/margin=0. Trong túi badge ở góc trên, số lượng vẫn ở góc dưới; icon không bị dịch. Cùng hàm căn chữ dùng cho Lv trang bị; vẫn giới hạn 0–7 và chỉ sáu loại được nâng cấp. Bỏ dòng Lv vàng thả trực tiếp lên icon và đoạn vẽ badge cũ bị lệch baseline.

`InventoryItemVisuals.PaintAura` / `AuraPixels`: thêm **aura màu set mờ dần vào phía trong khung**, vẽ dưới icon. Mép ngoài giữ viền bo, vùng giữa trong suốt để icon rõ; không có lớp khung lồng mới. Mask góc lấy alpha của `PopUp.goc` gốc. Chỉ áo/quần/găng/giày/rada có option set đã xác minh nhận aura, cả đang mặc và trong túi; GLT/skin/bông tai/pet không nhận aura set. Viền vàng chọn món vẫn phân biệt với màu aura set.

Aura dùng texture64×64 cache theo màu (bảng set có13 màu); đọc mask một lần mỗi sprite/zoom, đổi nguồn thì huỷ cache cũ. Không tạo texture hoặc tính gradient mỗi frame. Style Lv cache theo font/zoom/độ rộng, giới hạn8 style. Không sửa sprite nguồn, server, packet, vị trí khung hoặc hit-test.

Code thay thế đã bỏ: vòng lặp `fillRect` vẽ chấm sao; các đoạn đo/vẽ Lv riêng ở `PaintInventoryEquipment` và chữ Lv túi chưa có badge. Không xoá code ngoài phạm vi.

Kiểm tra `python Tests/Inventory/visuals.py`: **65.172 kiểm tra qua** cho màu/alpha aura, gradient giảm vào trong, giữa trong suốt, asset sao x1–x4 tồn tại, hàng sao và badge Lv không chồng nhau. Các bộ skin58.962, registry/model34.061, input60 và compile C# client/Editor vẫn qua. Chưa chạy Unity/Play Mode/build APK; cần xem lại trên S22/PC để đánh giá độ sáng aura và độ dễ đọc của badge ở zoom hiện dùng.

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
- Chiều rộng cố định theo nhãn lớn nhất `999` / `999+`; giới hạn theo bề rộng ô + khoảng trống giữa ô để không đè badge hàng xóm. Giữ font gốc, chỉ giảm cỡ chữ trong badge khi không đủ chỗ; cache phép đo theo font/zoom/kích thước ô. Bitmap zoom1 dùng font bitmap gốc và scale phần chữ, không dịch icon.
- Đến 999 hiện số thật; trên 999 hiện `999+`, thay cho cách viết k/M/B trước đây theo yêu cầu mới. Số đầy đủ hiện trong phần xem chi tiết, không đưa tên/chỉ số trở lại ô và không thay dữ liệu item.
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
2. Chọn **Sao pha lê lam ×758**: icon luôn ở giữa khung, badge không kéo icon sang trái và không bị cắt mép ô. Test 999 phải hiện `999`, 1.000 / 1.148 / 24.300 / số lớn phải hiện `999+`, các cột biên và hàng cuối; xem số đầy đủ trong preview. Lv túi nhô góc trên trái cân với Lv trang bị ở góc dưới phải.
3. Lọc từng nhóm, tap một món mở preview; đổi món cập nhật; X giữ chọn. Xong lọc tap đúp trang bị → mặc đúng món, món cũ về túi; tap đúp đá/đậu không được Dùng.
4. Thử kéo bắt đầu trên icon/chip/badge, wheel PC, phím điều hướng/xác nhận cũ; không gửi nhầm Mặc khi kéo. Đổi tab/đóng mở, đăng nhập lại không có exception lặp.
5. Nút Thao tác/menu cũ: Dùng/Vứt/gán ô nhanh còn hoạt động, rương/shop/giao dịch không đổi. Test khi chết và khi server cập nhật túi trong lúc preview mở.

Rủi ro riêng S22: safe-area/zoom4 chưa xem trực tiếp; badge có thể phải giảm cỡ font để vừa nhãn tối đa, cần đánh giá dễ đọc. Dấu level/nhãn chip và nội dung `ChatPopup` cũ cần kiểm tra bằng mắt ở panel phải hẹp. Tổng lưới dài hơn màn hình theo thiết kế đã chốt, không hiển thị đủ Trang bị + 7 hàng đồng thời. Viewport hẹp vẫn giữ mức rộng tối thiểu footer của A.

## Git và việc tiếp theo

`feat/inventory-frame` local + `origin/feat/inventory-frame` đã có squash merge #23 trên develop, là ứng viên dọn sau khi người dùng duyệt; **chưa xoá**. Các ứng viên/stash/worktree còn lại giữ như báo cáo A, chưa có quyết định xoá.

Các thay đổi sẵn có của người dùng ở logo, Packages, ProjectSettings và UserSettings không đưa vào commit B. Dừng sau B cho người dùng test/merge; không bắt đầu C. Không tự quyết giá mở thêm ô, số ô server khởi đầu mới, xử lý mảnh đá hoặc khóa món.
