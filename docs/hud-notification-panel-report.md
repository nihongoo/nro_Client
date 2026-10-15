# Cụm 3 — Panel thông báo

**Codex chưa compile, chạy game/test runtime hoặc build Unity/APK.** User đã tự build/test và xác nhận chấp nhận bản sửa cuối trước khi yêu cầu tạo PR. Codex chỉ review source/diff và `git diff --check`; không xác nhận đã chạy toàn bộ ma trận bên dưới. Push/tạo PR theo yêu cầu user; không merge/stash, không sửa server/protocol -109 v2; file local ngoài phạm vi giữ nguyên.

## Git

- Branch `feat/hud-notification-panel` tạo từ `origin/develop` **a4d8c3e** (PR #16). Tree develop và `feat/hud-top-actions` cùng **6ff69d4b0c3400e981e24545aee34442b6a351bd**.
- **Chỉ liệt kê, chưa xóa:** `feat/hud-top-info` và `feat/hud-top-actions`, cả local/remote, đã squash merge qua PR #15/#16. Không có stash thừa ngoài danh sách giữ.
- Giữ `develop`, `master`, `feature/custom-server-address`, `fix/login-batch3a`, `fix/ui-batch-c` + worktree `Mod-nro-unity-ui-batch-c`. Giữ stash client **d1a9a4a, a9a997d, dcbb14c, 5922203**, server **6121b4b**. Không drop stash trong đợt này.

## Đã làm

- `Assets/Scripts/Notifications/NotificationCenter.cs:56/:81`: panel Right/Top, cao `GameCanvas.h`, mobile cố định 40% chiều rộng logic; PC tối thiểu `PixelsToLogical(420)`, đo theo nội dung lúc mở đến tối đa 90% viewport; giữ nguyên chiều rộng suốt lần mở và khi đổi tab/nhận tin/đổi phút, chỉ đo lại khi mở mới hoặc resize/zoom/nền tảng đổi; trượt từ phải 180 ms. Dim `ScreenBounds`, đóng bằng dim/X, bấm trong panel không đóng. Font/nền popup/X/✓/✗ dùng lại của game, không đổi zoom/filter/skin.
- `NotificationCenter.cs:174/:208/:246`: HitTest chọn owner nhấn/thả; capture riêng qua nhiều frame, consume pointer/phím/frame đóng và đường di chuyển do click. Latch ID lời mời tránh trả lời nhầm khi mục hết hạn/đổi slot. Kéo ≥6 logic hủy bấm và cuộn; hỗ trợ wheel PC. Nhường dialog/panel/chat khác theo guard cũ.
- Order **dim 110 → panel 111 → X/tab/content 112 → dòng/text/tuổi tin 113 (trang trí), server-toggle 113 (bắt bấm) → ✓/✗ 114**; legacy overlay 100, popup 101, bell 10/R 40/lưới 40 giữ nguyên. Entry gốc đăng ký một lần (`Assets/Scripts/GameScr.cs:663`); slot đăng ký một lần rồi tái dùng (`NotificationCenter.cs:353`). `Assets/Scripts/Hud/HudLayout.cs:90` thêm Invalidate cache sau thay đổi UI trong cùng frame.
- `NotificationCenter.cs:104/:111`: boss hiển thị **“<boss> đã xuất hiện tại <map>”**, hoặc **“<boss> bị <người giết> tiêu diệt tại <map>”**. User yêu cầu khôi phục map/tuổi tin sau test, thay phần ẩn map của prompt ban đầu; vẫn không có khu/toạ độ/nút đi tới. Server chỉ đọc nguồn broadcast `NotificationStore.Server` hiện có.
- `NotificationCenter.cs:305/:312/:377`: tuổi tin có rect riêng, căn phải cách lề 4 logic, chừa cột cố định đủ 5 chữ số theo digit rộng nhất để đổi phút không đẩy text viewport. Text có provider riêng, chừa cột tuổi tin và clip trong content; vẽ dùng chính rect registry, không hitbox thứ hai.
- **Sửa sau ảnh test mới nhất:** trước đây splitFontArray phụ thuộc bề rộng còn lại sau cột tuổi tin, gây 1/2 dòng khi resize/số phút đổi. Nay mặc định mỗi thông báo Boss/Server/Lời mời có **một dòng text cố định**, riêng Server cho bấm để mở rộng/thu gọn, bỏ wrap ở các mục dữ liệu (`NotificationCenter.cs:322`); tin vừa khung đứng yên, quá dài chạy **phải → trái**, 24 logic/giây, lặp liên tục bằng hai bản chữ nối tiếp có khoảng cách, không nghỉ đầu chu kỳ hoặc đợi trống cả khung. Không thu nhỏ font; số phút cố định. Clock Stopwatch monotonic riêng không reset khi rebuild mỗi giây và không phụ thuộc số lần paint/điều chỉnh giờ hệ thống; mở panel/đổi tab bắt đầu lại. Không dùng TextInfo static chung vì nhiều mục sẽ ghi đè trạng thái nhau. Nút ✓/✗ vẫn nằm dưới một dòng lời mời, xếp dọc nếu quá hẹp.
- `NotificationCenter.cs:423`: clip tiêu đề có gutter, không bắt đầu tại gốc glyph (font có nét x−1 tại `Assets/Scripts/mFont.cs:390`), sửa chữ T bị cụt; giữ clip trước X.
- `Assets/Scripts/Notifications/NotificationStore.cs:10/:55/:76/:97/:109`: lịch sử client riêng tối đa **20**, gộp key bossName/mapId, spawn/killed cập nhật một mục; giữ qua REMOVE retention/resync và đóng/mở panel, reset khi đổi phiên (`:33`). Mục thứ 21 loại mục cũ nhất. User đã chọn **giữ server nguyên, chỉ tích lũy 20 client**: mới đăng nhập chỉ có tối đa 6 snapshot, nhận thêm key boss thì tích lũy đến 20. Registry hiện tại cap 6/REMOVE vẫn nguyên; UI đọc `BossHistory`, không đọc `Bosses` (`NotificationCenter.cs:101`). Không thêm mục giả/đổi protocol -109 v2.
- `NotificationCenter.cs:256`: nguyên source PaintBell gồm badge/count/offset/font. Nguyên RespondToInvitation/vòng expiry/retry. ✓/✗ dùng hàm cũ, giữ đánh dấu đọc/badge; 30s và phản hồi người gửi khi từ chối/hết hạn vẫn do server cũ.
- `Assets/Scripts/PopUpYesNo.cs:21/:79/:91`: popup không vẽ/nhận bấm/hiện registry khi panel mở, vẫn cập nhật hạn. Khi đóng, hiện lại mới nhất còn hạn/chưa trả lời (`NotificationStore.cs:137`), ưu tiên popup vẫn 101.

## Đã xóa

Số dòng dưới đây thuộc nền **a4d8c3e trước sửa**, để tìm phần đã bị thay thế:

| Code cũ đã xóa | File:dòng trước sửa |
|---|---|
| Window/tabs rect tự quản, chiều rộng 45%/180–320 logic, chiều cao tối đa 300, neo theo chuông | `Notifications/NotificationCenter.cs:10/:12/:132` |
| Paint form/tab/dòng cũ, nhãn Boss có map/vị trí và tuổi tin | `Notifications/NotificationCenter.cs:161/:231` |
| Cột nút phải, công thức RowBodyHeight/ActionBounds và vòng Contains để xử lý lời mời riêng | `Notifications/NotificationCenter.cs:207/:212/:218` |
| TouchSize theo DPI/Application.platform, hết caller sau thay form | `Notifications/NotificationHudLayout.cs:8` |

Không có ảnh riêng hết consumer cần xóa: nền popup dùng chung ChatTextField; X/bell/badge và nút Yes vẫn dùng. Không đụng panel Vật phẩm/Người hay các panel khác.

## Còn lại ngoài phạm vi (chỉ liệt kê)

- `NotificationStore.cs:17/:22/:23`: public getter Bosses/ServerUnreadCount/InvitationUnreadCount không còn consumer C# sau thay UI; giữ API store, không xóa logic dữ liệu.
- `NotificationHudLayout.cs:8`: giữ guard chuông bị InfoMe/popup che; các chức năng mod/gameplay và panel Vật phẩm/Người không sửa.
- File local sẵn có giữ nguyên: logo, Packages, ProjectSettings, hai layout UserSettings; server có Config/ClanService/.idea từ trước, đợt này chỉ đọc server.

## Ma trận test tay — chờ user tự build/chạy

| Màn hình/tình huống | Kiểm tra |
|---|---|
| PC 1280×720 | Rộng tối thiểu 420 pixel thiết kế theo zoom, cao hết màn; wheel/kéo, đủ 3 tab/font cũ |
| S22 2340×1080, mobile 20:9 | Panel mobile cố định 40% logic, dim kín viewport; hàng nút/badge không đổi |
| Tai thỏ/safe area | Không trừ inset hai lần hoặc vẽ text/dim ngoài viewport |
| Resize/đổi tab/zoom | Rect cập nhật; mỗi tin vẫn một dòng, tin vừa đứng yên/tin dài chạy trái, text không đè tuổi tin |
| Chữ chạy/tuổi tin | Boss tên/map dài, broadcast và lời mời dài; đọc hết chu kỳ, lặp đúng; 0/1/10/100 phút cùng mép phải; đổi phút không nhảy 2 dòng/reset animation; X/tiêu đề không cụt |
| Đóng/mở | Dim/X đóng; bấm trong panel không đóng; không click xuyên frame đóng |
| Boss 7/20/21 key trong cùng phiên | Hiển thị 7/20/20; mới nhất trước; spawn/killed gộp; cuộn đủ, đóng/mở/resync giữ lịch sử; đăng nhập mới chỉ có snapshot tối đa 6 |
| Server | Chỉ broadcast cũ; bấm dòng mở toàn văn nhiều dòng đứng yên, bấm lại thu gọn về marquee; cuộn hai chiều, kéo không toggle; giữ nhiều frame/thả đúng dòng vẫn toggle; resize wrap lại nhưng không đổi chiều rộng panel |
| Lời mời | ✓/✗ từng loại, sender nhận phản hồi cũ; 30s tự xóa, badge đúng, không gửi lặp; text chạy không che nút |
| Popup | Mời mới/expiry khi panel mở: popup ẩn; đóng hiện mới nhất còn hạn; xử lý hết không còn popup |
| Modal/capture | Giữ/bấm/kéo ở vị trí skill/R/lưới/joystick bên dưới không kích hoạt gameplay; phím chat/gameplay bị chặn; giữ tab/X/✓/✗/dim nhiều frame rồi thả dùng được, kéo ra hủy; hết hạn/đổi slot không trả lời nhầm |

**Chưa compile/chạy/build sau các sửa đổi.** Ma trận trên là kiểm tra user cần thực hiện; ảnh test user đã gửi không thay thế kiểm chứng bản sửa mới nhất. Static diff check đạt; không push/merge/stash.

## Sửa sau test — panel xê dịch và chữ dừng

- Nguyên nhân tại code cũ: PanelBounds đo lại nội dung/tuổi tin mỗi giây, nên độ rộng ký tự 1→2 hoặc 9→10 có thể đẩy mép panel, thay bề rộng text và chuyển điều kiện chạy/đứng yên. Cột tuổi tin cũng thay độ rộng theo số hiện tại. Chu kỳ marquee còn có pause 1000 ms và một đoạn chữ đi hết khung trước khi vòng lại.
- `NotificationCenter.cs:81/:124/:377`: khóa chiều rộng panel theo lần mở/viewport; cố định cột tuổi tin; bỏ cache đo theo revision/thời gian đã hết nhu cầu. Chữ dài chạy nối tiếp liên tục bằng Stopwatch, không dừng giữa chu kỳ và không chỉnh zoom/font. Server/protocol/data lời mời và cap lịch sử 20 giữ nguyên.
- Test bổ sung: để panel mở qua các mốc phút 1→2, 9→10, nhận boss/tin mới, đổi tab và hết hạn lời mời: mép panel đứng yên sau slide 180 ms. Chạy chữ qua ít nhất 3 vòng không nghỉ/nhảy, tuổi tin vẫn căn phải; resize/zoom mới được relayout. **Chưa compile/chạy/build; chỉ kiểm tra source/diff.**


## Server — bấm mở rộng/thu gọn

- `NotificationCenter.cs:208/:225/:322/:353/:377`: từng thông báo Server có trạng thái mở rộng theo ID, bấm đúng owner nhấn/thả để toggle. Khi mở, dùng font game xuống dòng từ nguyên văn (giữ newline), tăng chiều cao mục và scroll; bấm lại về một dòng marquee. Có thể mở nhiều mục độc lập; đổi tab giữ trạng thái trong lần mở, mở panel mới reset, mục bị nguồn xóa được dọn trạng thái.
- Rect `notifications.server-toggle.<slot>` đăng ký một lần ở order 113, chỉ bắt phần dòng nằm trong content; không lấy phần bị cắt để che tab/header. Giữ capture kéo/cuộn và latch ID, nên kéo không toggle hoặc click xuyên; dữ liệu đổi slot giữa lúc giữ không mở nhầm mục khác. Panel giữ nguyên chiều rộng; font, boss/lời mời/badge/server/protocol không đổi.
- Test tay thêm: mở/thu từng mục ngắn/dài/có newline; mở nhiều mục, scroll mục cao hơn màn, resize; nhận thêm/xóa tin khi giữ; thu gọn gần cuối danh sách scroll clamp đúng. **Chưa compile/chạy/build; diff check source đạt.**

## Sửa bổ sung theo ảnh Android thật — HUD chỉ số

- Theo user báo mobile thật lỗi nền avatar/KI/EXP và số sức mạnh bị cắt: sửa trong `Assets/Scripts/Hud/HudButtonSkin.cs:12/:86`, `Assets/Scripts/Hud/TopInfoHud.cs:53/:189`. Đổi clone texture sang copy RGBA32 rõ ràng, nới chiều rộng chỉ mobile theo font hiện có; PC/font/zoom/asset importer giữ nguyên. API CopyNative giữ default Point cho các caller cũ. Chi tiết, giả thuyết nguyên nhân và test thiết bị ở `docs/hud-top-info-report.md`, mục Android thật.
- Panel thông báo, nguồn dữ liệu và server/protocol không thay đổi trong lượt này. **Chưa compile/chạy/build; cần user kiểm chứng Android thật.**

## Sửa theo user — FPS mobile và panel 40%

- `Assets/Scripts/Main.cs:88/:115`: đặt mục tiêu render **60 FPS** khi khởi động Android/iOS, thay đường mặc định chưa đặt targetFrameRate (Unity thường dùng 30 FPS mobile). PC startup, FixedUpdate, Time.timeScale và MotherCanvas không đổi; không sửa số đếm FPS để giả tăng. [Unity targetFrameRate](https://docs.unity3d.com/ja/2022.2/ScriptReference/Application-targetFrameRate.html) nêu mobile dùng targetFrameRate, FPS còn phụ thuộc refresh rate/hiệu năng thiết bị; đây là mục tiêu, chưa đo FPS thực tế.
- `Assets/Scripts/Mod.CuongLe/MainMod.cs:996`: tắt chế độ tối ưu CPU trở về NormalTargetFrameRate (mobile 60, PC giữ 90 cũ); bật tối ưu CPU vẫn giữ 10 FPS. Không có công tắc nền tảng hay preset UI mới.
- `NotificationCenter.cs:89`: mobile dùng đúng **floor(GameCanvas.w × 0.4)**; bỏ việc nới theo độ dài nội dung trên mobile, không xê dịch theo số phút/đổi tab/tin mới. PC giữ cách tính hiện tại. Panel vẫn cao hết viewport; text dài marquee, tab Server vẫn bấm mở nhiều dòng/bấm lại thu gọn, cuộn/clip và modal capture giữ nguyên.
- User đã xác nhận sửa texture/chỉ số mobile lượt trước “ok”. Test bản mới cần tự build trên S22: so FPS khi tắt/bật tối ưu CPU, qua login/map/background→foreground; kiểm tra tốc độ gameplay không đổi. Thử PC 1280×720, mobile 16:9/20:9/tai thỏ/resize: panel mobile ~40%, text không đè cột tuổi tin, Server expand/collapse, X/dim/✓/✗ hoạt động và không click xuyên.
- **Chưa compile/chạy/build; chưa xác nhận đạt 60 FPS trên thiết bị. Không push/merge/stash, không sửa server/protocol.**
