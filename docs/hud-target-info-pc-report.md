# Cụm 6b — Thẻ mục tiêu PC

**Chưa compile/chạy client, Unity/Device Simulator hoặc build Unity/APK.** Chỉ kiểm tra tĩnh; user tự build/test. Không sửa server; user đã duyệt tạo commit/PR sau khi test giao diện. Không merge/stash, chưa xóa branch/drop stash.

## Git

Fetch origin: PR21 đã squash vào develop `ce1d132`; tree local + remote `feat/hud-target-mobile` và `origin/develop` cùng `f8a643e284e5a9a59e1a78c01c61eb7c2310801c`. Đã tạo `feat/hud-target-info-pc` từ develop mới. Logo/Packages/ProjectSettings/UserSettings dirty sẵn giữ nguyên byte, ngoài phạm vi.

Ứng viên dọn **local + remote**, chỉ liệt kê chờ user duyệt; từng tree đã khớp squash commit nằm trong develop:

| Branch | Squash develop cùng tree |
|---|---|
| feat/hud-top-info | ae61050 / PR15 |
| feat/hud-top-actions | a4d8c3e / PR16 |
| feat/hud-notification-panel | 97468bd / PR17 |
| feat/hud-lists | ba1cc48 / PR18 |
| feat/hud-combat-mobile | 6c0d26f / PR19 |
| feat/hud-combat-pc | 015c672 / PR20 |
| feat/hud-target-mobile | ce1d132 / PR21 |

Giữ nguyên develop/master/feature/custom-server-address/fix/login-batch3a/fix/ui-batch-c và worktree ui-batch-c. Stash list đã in lại; cả4 là “giữ nguyên”, không có mục mới đề nghị drop:

- `d1a9a4a242b8bdce69c9f7645adcd2282d9df34a` — local server address/layout.
- `a9a997d0a600ec9062acb7c7653783aaba17cf20` — change-ip-btn.
- `dcbb14c83bf39b0950f7ec31d93179cc0a642d6b` — local packages.
- `5922203f9a36a4d24d4b2cacf1479ab13726da02` — backup custom-server.

Stash server6121b4b giữ theo quyết định trước, không thao tác repo server. Nếu được duyệt drop sau này sẽ đối chiếu hash→ref ngay trước thao tác và in lại list; lần này không drop.

## Sửa tỉ lệ PC theo mobile — 16/10/2026

Một lớp TargetInfoHud, một provider Center/Top `target-info.bounds` order10/blocksPointer=false, một BuildPanel/rasterizer/cache cho cả hai nền tảng. Không đổi nguồn focus, gameplay/phím/input, font, MotherCanvas/isPC/isTouch. Mobile giữ nguyên công thức Unit và hình học trước lượt sửa này, kể cả tab map-only.

### Đối chiếu tham số

`uM = PixelsToLogical(1) × h × zoom / 390`; `uPC cũ = PixelsToLogical(1)`; `F = max(1, chiều cao font thường, chiều cao font đậm)` logical. PC mới dùng `u = F/12`; mobile vẫn dùng uM. Các số dưới đây nhân u tương ứng, trừ font native. Lấy mobile làm bộ tham số hình học duy nhất; chỉ bộ đổi đơn vị khác để bảo toàn mobile đã duyệt.

| Tham số | Mobile chuẩn, giữ nguyên | PC trước sửa | PC sau sửa |
|---|---|---|---|
| HP rộng ưu tiên | 192uM | 280uPC cũ | 192u |
| HP cao | max(14uM, font đậm + 2uM) | max(14uPC cũ, font đậm + 2uPC cũ) | Cùng công thức mobile theo u |
| Thân trên / rộng cả vai | 212uM / 240uM | 300uPC cũ / 328uPC cũ | 212u / 240u |
| Lề ngang HP/tên | 10uM | 10uPC cũ | 10u |
| Bậc dưới ở rộng ưu tiên | 160uM / 212uM ≈ 75.5% | Theo nội dung, 140..248uPC cũ; tên ngắn chỉ ≈46.7% | 160u / 212u ≈75.5%, không co theo tên |
| Thụt mỗi bên ở rộng ưu tiên | 26uM | Tên ngắn 80uPC cũ | 26u |
| Min bậc dưới khi thu viewport | 140uM, kẹp thân trên−40uM | 140uPC cũ, kẹp theo nội dung | Cùng quy tắc mobile theo u |
| Bo / chân loe | 7uM / 14uM | 7uPC cũ / 14uPC cũ | 7u / 14u |
| Viền | max(1 pixel thật, 1.5uM × scale màn hình), #f2b45a | Cùng công thức nhưng u nhỏ theo zoom nên viền mảnh tương đối với chữ | Cùng công thức; 1.5u theo font logical, không mỏng dần tương đối với chữ khi zoom |
| Nền | #2b1a0a, 88% | Giống mobile | Giữ nguyên |
| Font map/HP/SM, tên | tahoma_7_white, tahoma_7b_white native | Giống mobile, nhưng hình nền tính theo design px/zoom | Giữ font; hình nền theo chiều cao font |
| Lề trên / map→HP / HP→tên / đáy | 1 / 2 / 3.5 / 4 × uM | Cùng hằng số nhưng nhân uPC cũ | Cùng hằng số nhân u; không thêm padding trên |
| Cao tổng | max(58uM, nameY + row + 4uM) | Cùng công thức nhưng uPC cũ | Cùng công thức theo u |
| Ô sức mạnh | 55% hàng nội dung | Tính theo độ rộng số/tên riêng PC | 55%, cùng mobile |

Nguyên nhân: PC trước dùng design px chia zoom trong khi font có giới hạn chiều cao native; các cạnh/viền/HP nhỏ tương đối với chữ. Bậc dưới còn bị co theo tên ngắn; pill có nhánh tính riêng. Đã bỏ cả hai nhánh riêng đó, bỏ HP280 và bỏ phân biệt nền tảng trong key cache hình nền.

**Quy tắc bậc dưới thực tế của mobile:** `max(upper−52u, min(140u, upper−40u))`, cố định theo viewport, không theo tên. Để giữ mobile nguyên vẹn, PC dùng đúng quy tắc này; tên/số dài vẫn chạy một chiều trong clip hiện có. Chưa thêm việc nới bậc theo nội dung vì việc đó sẽ đổi hình mobile, trái yêu cầu “Mobile KHÔNG đổi”. Gờ giữ ngắn, chuyển tên ngắn/dài không đổi texture.

Ví dụ tĩnh F=11 logical, zoom2: u=11/12, HP352px thật, thân trên388.67px, bậc dưới293.33px (75.5%), bo12.83px, chân loe25.67px, viền2.75px; cao tổng106.33px khi không bị font cao hơn chi phối. Đây là ví dụ mô hình, không phải số đo từ game. Rộng viewport chỉ kẹp bề ngang; không scale font/chiều cao. Dưới upper180u (HP160u) chuyển tab map-only như trước. Chưa đo ngưỡng viewport thực vì phụ thuộc rect avatar/hub, font và zoom đang chạy.

### Đã làm / đã xóa / còn lại

- TargetMobileHud được đổi tên thành TargetInfoHud, giữ GUID meta; shared Read/Paint/Text/BuildPanel. Nền hai bậc, AA scanline, HP/pill, marquee một chiều, map-only, trạng thái mục tiêu dùng chung. Mobile formula được giữ nguyên trong lượt sửa tỉ lệ.
- Xóa HP width280 riêng PC, lower tier tính theo nội dung riêng PC và pill đo chữ riêng PC. Xóa nhánh vẽ head VS nhập tay PC; giữ fallback mobile. MapListsHud đã bỏ map title renderer cũ; tab chung sở hữu tên map và BG.
- Cache panel LRU8 theo geometry pixel thực; key không phân biệt PC/mobile khi hình giống nhau. Mask HP/pill cache riêng24. Giữ builder tối ưu edge ribbon + scanline, Destroy khi evict/dọn; không dựng texture mỗi frame. Chưa đo FPS runtime.
- Shared request−79 giữ kiểm ID/object/owner/map/zone, giới hạn request750ms, 2lần/đối tượng, retry5s, 32entry; không mở panel/đổi focus. Controller chỉ cập nhật panel.charMenu đúngID. PC thêm hook dọn cache, mobile dùng hook cũ.
- Ngoài phạm vi chỉ liệt kê: tên/HP/marker trên map, fallback VS mobile, helper findCharVS/paintHeadWithXY còn caller, toast map, bộ lọc/chọn mục tiêu, HUD chiến đấu và các file dirty có sẵn.

### Kiểm tra và test tay

Tĩnh: đối chiếu Unit mobile giữ đúng biểu thức trước sửa; chung bộ hằng số/renderer; không còn nhánh width/pill theo nội dung PC; 80 tổ hợp mô hình font/zoom/bề ngang đạt tỷ lệ bậc dưới70–80%, HP/font và viền/font ổn định trên PC; kiểm delimiter/diff. So với snapshot dirty từ lượt trước, CurrentMaximizeLayout.dwlt đã thay đổi bên ngoài lượt sửa này; không ghi/sửa file đó. Các file dirty còn lại khớp snapshot. Chưa compile/chạy/build.

| Test tay — tất cả chưa chạy | Cần xác nhận |
|---|---|
| PC1280×720,1920×1080; zoom đang dùng; resize rộng/hẹp | Viền cam rõ cả hai bậc/cạnh vát, bo mượt, chân loe/gờ giống mobile; chữ/HP cân đối; không tăng lề trên |
| Quái/boss/người chơi/NPC/bỏ chọn/chết/mất | Đúng focus gốc; không có mục tiêu có BG map-only; thiếu SM tên giữa |
| Tên ngắn/dài, SM ẩn/hiện, số lớn | Bậc dưới ≈75% ở rộng chuẩn, không co theo tên; clip/marquee một chiều, không che thông tin |
| Chuột/phím/danh sách Người đổi mục tiêu liên tục | Không nhầm A/B, không mở panel; cache ấm không tạo texture lại khi hình không đổi; đo CPU/FPS thực |
| Cache lạnh/ấm,resize/đổi map/relog/pause | Không rò/đăng ký trùng; không răng cưa/giật, modal/form đúng thứ tự |
| Cụm avatar/nút phải/list; click vùng thẻ | Không chồng hai bên; thẻ không chặn pointer, thiếu chỗ chỉ thu bề ngang/map-only |
| Android Simulator16:9/20:9/tai thỏ,S22 | Mobile giữ hình học, padding, viền, HP, map-only và hành vi đã duyệt; joystick+chiêu/chọn quái/list không hồi quy |

Nguồn (file:line): `Assets/Scripts/Hud/TargetInfoHud.cs:56` (Unit), `:121` (provider), `:127` (Bounds), `:216` (Text), `:324` (PaintPanel/cache), `:366` (BuildPanel), `:85` (ReceivePower). **Chưa compile/chạy/build; chưa commit/push/merge/stash.**

### Chỉnh cuối: bỏ đệm dọc hàng tên PC

Theo yêu cầu tiếp theo, riêng PC đặt hàng tên/SM ngay sau HP (gap0), gờ tại đáy HP, bỏ lề dưới4u và bỏ min-height58u vốn để thừa khoảng trống. Cao nền PC = nameY + row; vẫn giữ chiều cao hàng đủ font/pill, padding ngang10u, viền/bo và cache chung. Mobile giữ nguyên gap3.5u, gờ1.75u, đáy4u/min58u. Bảng phía trên mô tả lượt hợp nhất trước thay đổi spacing này. Ví dụ PC F11/zoom2, cao mới78.83px thay vì106.33px.

Đã kiểm tĩnh hàng tên/pill nằm ngay dưới HP, không giao HP, chiều cao đủ dòng và mobile giữ công thức cũ. Test tay bổ sung chưa chạy: quái/người có hoặc không SM, tên dài, cạnh bo đáy không cắt chữ; Android giữ nguyên. Chưa build/compile/chạy; chưa commit/push/merge/stash.

### Thanh player và đệm1px — chỉnh PC tiếp theo

PC dùng asset HP `GameScr.imgHP` và sức mạnh xanh `/mainImage/SP.png` của player, chung `TopInfoHud.PaintBarSurface/Capsule` ba phần để giữ đầu bo/highlight, chỉ kéo dài phần giữa. HP vẫn đúng tỷ lệ HP/maxHP; ô sức mạnh đầy nền để hiển thị số tuyệt đối, không bịa tỷ lệ sức mạnh. SM dùng chữ kem giống player trên nền xanh. Các hình pill vàng/mask nhị phân cũ chỉ còn đường mobile hoặc fallback khi asset chưa sẵn sàng; mobile không đổi.

Inset artwork HP/SM1design px trên và dưới qua PixelsToLogical, không tăng chiều cao thẻ hay thêm khoảng trống quanh hàng tên; nếu font sát chiều cao hàng thì giảm inset đủ giữ chữ native trong thanh. Giữ nguyên rect chữ và tâm chữ.

HP dùng bản sao RGBA32 Bilinear riêng qua CopyNative/SmoothTexture; không đổi filter texture HP gốc của player. Cache1 bản HP, hủy khi nguồn đổi hoặc ClearShapes hiện có. SP dùng cache Bilinear sẵn của player, không tạo thêm copy/cache cho target. Không tạo texture mỗi frame. PaintBarSurface tách nguyên phần vẽ từ PaintBar player; đường player vẫn gọi cùng phần vẽ và cùng phần text, không đổi hình mobile/player.

Tĩnh: đối chiếu phần vẽ player trước/sau refactor; kiểm cache/Destroy, nhánh PC/mobile, inset/font/zoom và delimiter/diff. Đã xem SP.png x2 là asset xanh. Test tay **chưa chạy**: PC HP đầy/thiếu/gần0, hai đầu thanh không răng cưa khi resize; SM xanh, chữ giữa, inset1px trên/dưới; tên dài/SM ẩn-hiện; Android player/target không hồi quy; đổi map/relog/pause/zoom không rò. Chưa build/compile/chạy; chưa commit/push/merge/stash.

Source bổ sung: `Assets/Scripts/Hud/TopInfoHud.cs:201` (asset/cache), `:209` (dọn HP copy), `:214` (surface chung); `Assets/Scripts/Hud/TargetInfoHud.cs:205`/`:214` (chỉ PC), `:222` (inset), `:501` (cleanup).

### Viền nền HP và chiều rộng danh sách PC

Đưa gờ nền xuống đáy row HP+1.75u (cùng mốc mobile) để artwork HP inset1px không đè lên stroke1.5u. Hàng tên/SM PC bắt đầu ngay tại gờ, không thêm footer/min-height. Mobile giữ đúng các mốc cũ. Ví dụ F11/zoom2 thẻ PC cao82.04px sau chỉnh, tăng3.21px để nhường viền.

HeaderBounds của cả hai nền tảng dùng cùng rect registry: Vật phẩm = exp.xMax−avatar.xMin (không gồm FPS), Người = chat.xMax−hub.xMin (toàn hàng3nút và khoảng cách). Body/hit/row/rail tự theo header; bỏ width190/130 và cap nửa viewport riêng PC. Không đổi filter/nội dung/cuộn/chọn item/người, font hay vị trí dọc. Mobile vốn dùng chính công thức này, không đổi.

Tĩnh:20 tổ hợp font/zoom kiểm HP cách stroke đủ nửa pixel AA; kiểm HeaderBounds chung, delimiter/diff. Test tay bổ sung **chưa chạy**: PC1280/1920/resize, HP đầy/thiếu không che viền/gờ; danh sách Vật phẩm thẳng avatar→cuối SM, Người thẳng hub→cuối chat; header/body/hover/bấm/scroll đúng vùng rộng mới; Android không hồi quy. Chưa build/compile/chạy; chưa commit/push/merge/stash.

Source: `Assets/Scripts/Hud/TargetInfoHud.cs:160` (gờ nhường viền HP); `Assets/Scripts/Hud/MapListsHud.cs:120` (độ rộng chung), `:137` (body theo header).

### Nhường viền đáy cho thanh sức mạnh PC

Nguyên nhân: nền PC kết thúc ngay tại đáy row tên/SM; inset artwork1px chưa đủ tránh stroke nền1.5u. Đã kéo riêng đáy nền xuống1.75u, không di chuyển hàng tên/SM hay HP và không thêm min-height. Cộng inset hiện có, artwork không đè lên viền đáy. Mobile giữ công thức cũ. Source: `Assets/Scripts/Hud/TargetInfoHud.cs:163`.

Tĩnh:20 tổ hợp font/zoom có khoảng hở giữa artwork SM và mép trong stroke ít nhất0.5pixel cho AA; giữ nguyên vị trí các hàng. Test tay chưa chạy: SM xanh không che viền đáy/cạnh bo, HP giữ đúng, tên dài/SM ẩn-hiện, resize, Android không hồi quy. Chưa build/compile/chạy; chưa commit/push/merge/stash.

## Chốt PR theo duyệt của user

User đã test bằng ảnh và xác nhận bản cuối “đã ok”, cho phép commit/push/tạo PR vào develop. Agent chỉ chạy kiểm tra tĩnh, không build/compile/chạy game; ma trận đầy đủ vẫn cần user kiểm tra. Đã bỏ hai TextMeasure name/power không còn caller sau khi dùng lại bậc dưới cố định. Các ghi chú “chưa commit/push” phía trên là trạng thái ở thời điểm từng lượt sửa. PR bao gồm bản PC + nền map-only trên cả hai nền tảng đã được yêu cầu bổ sung; không có thay đổi server, logo, package, project settings hay Unity layout.
