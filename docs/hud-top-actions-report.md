# HUD cụm 2 — hàng nút trên phải

Ngày 15/10/2026. Branch `feat/hud-top-actions`, tạo từ `origin/develop` **ae61050** (PR #15). Đã fetch, xác nhận `git diff origin/develop feat/hud-top-info` rỗng: tree khớp cụm 1 (`42ac840`).

## Popup lời mời — cùng hàng chức năng (yêu cầu mới nhất)

- `PopUpYesNo.cs:37` (`QuickBounds`): trái → phải là **nội dung → ✓ → × → lưới → chuông → R**, cùng tâm hàng. Không dùng phương án popup bên dưới nữa. Cả khung chữ và hai nút nằm trong **nửa phải viewport**, mép phải nút × cách lưới đúng `TopActionsHud.ButtonGap`.
- Chiều rộng phần lời mời tối đa 190 logic, đồng thời giới hạn theo khoảng từ giữa viewport đến mép lưới; phần chữ nhận độ rộng còn lại sau hai nút. Font giữ nguyên, chữ dài cuộn theo TextInfo, countdown dưới khung chữ. Yes/No vẫn 32×32; hàng chức năng vẫn 34×34, gap 4; không đổi provider các nút chức năng.
- Frame/body/yes/no đăng ký một lần trong HudLayout, dùng chung rect cho paint/VisualBounds/HitTest và tọa độ Command. Popup nhanh là vùng riêng order 101, không capture toàn màn; lưới/chuông/R và avatar vẫn dùng được khi chỉ có lời mời. Các modal thực (menu/dialog/panel/Chat/notification/VIP/popup legacy) vẫn chặn HUD như trước, nút trả lời giữ ưu tiên handler cũ.
- Khi nửa phải không đủ khung chữ tối thiểu 20 logic + hai nút, popup nhanh trả rect rỗng; lời mời vẫn trong tab Lời mời của chuông, không thu nhỏ font/nút hoặc cho popup tràn sang nửa trái.
- Test tay: S22 2340×1080, PC 1280×720, 20:9, tai thỏ, resize quanh ngưỡng đủ chỗ; kiểm tra thứ tự chữ/✓/×, bấm đúng hình, chữ dài cuộn, popup không qua giữa màn; màn hẹp mở chuông trả lời lời mời. Giữ ID/timeout/callback giao dịch/bang/nhóm. **Chưa compile/chạy/build; không push/merge/stash.**

## Sửa riêng viền badge sau ảnh test mới

- Theo yêu cầu tiếp theo, tăng bán kính bo lên nửa chiều cao: badge dạng viên thuốc, hai đầu tròn hoàn toàn. Giữ kích thước 24×18 logic, vị trí, màu và font; không ép thành hình tròn hẹp làm mất chỗ cho số nhiều chữ số. Chưa kiểm tra runtime.

- `Notifications/NotificationSkin.cs` (`BadgeIcon`, `SmoothBadgeEdges`): asset badge có viền bậc thang trong chính ảnh nguồn; làm lại contour bo tròn ở độ phân giải gốc bằng khoảng cách tới cạnh và alpha chuyển tiếp 1 pixel, lấy màu viền/nền và gradient từng hàng từ ảnh cũ. Cache bản riêng với Bilinear để giữ mép mượt khi GUI/safe area scale; không sửa asset/filter chung. Hủy cache khi zoom đổi.
- Không đổi vị trí/kích thước badge, số đếm/font, hitbox hoặc nền ba nút. Thử S22/PC ở zoom hiện có, số 1/2/9/99 và trạng thái không có tin; kiểm tra mép bo không thành bậc hoặc xuất hiện quầng. **Chưa compile/chạy/build sau sửa; không push/merge/stash.**

## Sửa sau ảnh test — nền/kích thước và nút biến mất

- Nguyên nhân: lưới vẽ nền R vào cả rect 34×34 trong khi R vẽ ảnh gốc 31×31 logic; chuông dùng đĩa màu riêng. Nay hub/chuông dùng bản sao **đúng pixel nguồn** của nền R, không phóng lên 34×34; cả ba căn tâm và vẽ bằng `mGraphics.drawImage` như R. Hitbox vẫn 34×34/gap 4, không thay provider/layout. Bản sao cache chỉ bỏ glyph chat ở giữa; giữ viền/kích thước/filter Point nguồn, không sửa asset hoặc filter chung (`Hud/HudButtonSkin.cs:93`, `PaintNativeBackground`).
- Chuông dùng biểu tượng cream lấy từ asset bell hiện có, bỏ nền đỏ/cam riêng; badge/count/font/offset giữ nguyên (`Notifications/NotificationSkin.cs:9`, `BellGlyph`; `NotificationCenter.cs:110`). Texture nhập không readable được copy qua RenderTexture một lần khi cần, trả RenderTexture và khôi phục active target; cache cũ được hủy khi source/zoom đổi.
- Hai nút biến mất do `IsChatButtonVisible` gọi guard touch-control và `IsBellVisible` dùng CanUse (false khi menu mở). Nay tách visibility khỏi input: hàng vẫn vẽ sau khi mở menu lưới; điều kiện CanInput/CanUse và overlay order 100 vẫn chặn click xuyên modal. Giữ điều kiện map offline/VS riêng của R và popup che chuông.
- Test thêm sau user tự build: so đường kính/viền/độ nét cả ba nút ở S22 2340×1080 và PC; bấm lưới thấy R/chuông còn ở nguyên vị trí, có badge; thử bấm chúng khi menu mở phải nhường menu, đóng menu không kích hoạt nút phía sau. **Chưa compile/chạy sau sửa này.**

## Đã làm

- `Assets/Scripts/Hud/TopActionsHud.cs:25`: đăng ký một lần ba entry. Mỗi nút một provider, size **34×34 logic**, hằng `ButtonGap=4` (`:8`). Trái → phải: lưới → chuông → R; chuông suy từ rect R, lưới suy từ rect chuông. Vẽ/hover/input đọc rect registry, không có hitbox/công thức vị trí thứ hai.
- `TopActionsHud.cs:34`: R giữ lề phải/trên **6/2**, màn `w<=240` giữ **1/5**, kể cả offset keyboard cũ. PC/mobile cùng hình học 34×34 theo yêu cầu; không suy nền tảng bằng isPC/isTouch. `IsMobileLayout` chọn hover chuột PC hoặc trạng thái nhấn mobile (`:21/:89`). Không công tắc nền tảng, đổi font/zoom/gameplay.
- Order không đổi: **chuông 10, R 40**, lưới mới **40**. Renderer hub/R duyệt registry tăng dần (`TopActionsHud.cs:87`); bell vẫn vẽ sớm tại `NotificationCenter.PaintBell`. Modal order 100; popup legacy nằm trong predicate capture; popup lời mời dùng vùng riêng order 101 (`GameScr.cs:683`).
- `TopActionsHud.cs:49`: HitTest chọn owner khi nhấn, cùng owner khi thả mới kích hoạt; kéo khỏi nút hủy mở và tiêu thụ thả. Giữ isPointerClick đến release vì `pointerReleased` không tự bật lại cờ này. Dispatch sau popup lời mời, trước form/notification/gameplay (`GameCanvas.cs:596`); `GameScr.cs:2651` nhường frame đã tiêu thụ. Không phát lệnh di chuyển từ click nút.
- Lưới gọi đúng `MainMod.ShowMenu()` cũ. Chỉ thêm một mục **Auto item** (`MainMod.cs:1115`), action **112** (`:832`) giữ nguyên thao tác rương cũ: toggle `DoHoa.isShowMenuVIP`, clear pointer. Không sửa UnifiedAutoMenu hoặc các mục menu cũ.
- R vẫn gọi `ChatTextField.startChat(GameScr.instance, string.Empty)`; giữ asset nhãn R hiện có cho cả hai nền tảng. Chuông gọi `NotificationCenter.TryOpenBell` (`:52`) dùng cùng kiểm tra HitTest, giữ reset tab/scroll và nội dung thông báo. Badge/count/font/offset không đổi; tâm bell nay là tâm rect của chính nó (`:116`). Hover/nhấn dùng nền R xanh có sẵn trước khi vẽ badge.
- Lưới dùng nền nút R đã có + glyph lưới 3×3 ô, không thêm skin/ảnh/font. Tách nguyên renderer nền R và chuyển pixel từ cụm 1 vào `Hud/HudButtonSkin.cs:63/:71/:93` để dùng chung, giữ hình avatar/thanh. `TopInfoHud.cs:49` đổi vùng tránh từ bánh răng đã bỏ sang rect lưới; chỉ giới hạn HUD trái khi viewport hẹp.

## Đã xóa

Vị trí **trước sửa cụm 2** dùng để định vị code đã xóa:

| Phần xóa | File:dòng trước sửa |
|---|---|
| Paint bánh răng/rương, field và load imgSetting/imgBoxItem/imgBoxOpenItem | `Mod.CuongLe/DoHoa.cs:24/:30/:51` |
| Hai hitbox/công thức vị trí HUD bánh răng/rương | `Mod.CuongLe/MainMod.cs:1359/:1367` |
| LegacyInfoPanelWidth/Height và bridge ChatButtonBounds/CenterX/Bottom hết consumer | `GameScr.cs:653` |
| Painter/input/hover R cũ: paintTouchControl, nhánh keyTouch=15; giữ các hover combat còn dùng dưới tên checkMouseControls | `GameScr.cs:4214/:4263/:5671` |
| Cache/search BellBounds/IsFree, BellCenterX lấy tâm từ R, obstacle panel/buff/gear cũ | `Notifications/NotificationHudLayout.cs:8/:33/:70` |
| Caller click chuông riêng trong NotificationCenter.UpdateInput | `Notifications/NotificationCenter.cs:61` |

Xóa **12 PNG + 12 .meta** đã hết source-load, không có GUID reference scene/prefab/asset/material/animation: `Assets/Resources/res/x{1,2,3,4}/mainImage/BoxMenu.png`, `BoxMenuOpen.png`, `Assets/Resources/res/x{1,2,3,4}/pc/myTexture2dSettings.png`. Asset nhị phân không có số dòng. Không còn hitbox riêng mồ côi cho bánh răng/rương.

## Còn lại / chỉ ghi nhận

- `MainMod.cs:1015`: phím X vẫn mở ShowMenu, có thêm Auto item qua cùng menu; các mục More/đồ họa/auto cũ giữ nguyên. `GameScr.cs:6282` (tìm nhánh command==admin): đường /admin vẫn toggle VIP; không xóa chức năng rương/VIP hoặc handler/painter bên trong.
- `NotificationHudLayout.TouchSize` vẫn dùng cho nút hành động trong panel thông báo, không còn quyết định size nút bell HUD. Giữ cơ chế bell bị popup/InfoMe che và điều kiện hiển thị R trên map/VS cũ; slot ba nút không tự xô lệch khi một nút tạm ẩn.
- Chỉ ghi nhận ngoài phạm vi: DoHoa.Update rỗng (`DoHoa.cs:36`); MainMod.smethod_0..3 rỗng còn caller, smethod_4/5/14/16 chưa thấy caller. Giữ paintListCharsInMap/listCharsInMap/controlTeleChar, isUseTouch combat và toàn bộ logic gameplay.

## Git / kiểm tra

**Đề nghị xóa sau khi user duyệt:** branch `feat/hud-top-info` local + remote, đã squash qua PR #15. Chưa xóa branch/worktree/stash nào trong cụm 2. Không có stash thừa ngoài danh sách cần giữ.

Giữ `feature/custom-server-address`, develop/master, fix/login-batch3a, fix/ui-batch-c + worktree; client stash `d1a9a4a`, `a9a997d`, `dcbb14c`, `5922203`; server `6121b4b`. Nếu user duyệt drop về sau: định danh bằng hash, in lại stash/branch/worktree list. Không áp/drop stash trong đợt này.

Chỉ rà source/caller, GUID asset, dependency registry và diff whitespace; file local có sẵn không sửa. **Chưa compile, chưa chạy Unity/game/test runtime, không build Unity/APK. Không merge/stash.** Theo yêu cầu mới của user, commit/push cụm 2 và tạo PR vào develop để user tự merge; không đưa file local ngoài phạm vi vào commit. User đã gửi ảnh test S22 trong các lượt sửa đồ họa; bố trí popup cùng hàng mới nhất chưa được xác nhận runtime.

## Ma trận test tay — chưa xác nhận đủ sau sửa cuối

| Case | Kỳ vọng |
|---|---|
| PC 1280×720 | Lưới–chuông–R cùng hàng, 34×34, gap 4, R lề 6/2; hover khớp hình; font cũ |
| S22 2340×1080 | IsMobileLayout; nhấn/thả đúng nút, không di chuyển nhân vật; avatar/thanh cụm 1 giữ hình |
| Mobile 16:9/20:9, tai thỏ trái/phải | Neo theo safe viewport hiện có, không trừ inset lần hai; row không đè cụm trái |
| Resize, w=240/241, đổi zoom sẵn có | Ba provider cập nhật đồng thời; <=240 dùng lề 1/5, >240 dùng 6/2; size/gap logic giữ nguyên |
| Bấm lưới → Auto item | Menu bánh răng đủ mục cũ + đúng một Auto item; chọn mở đúng UnifiedAutoMenu/VIP của rương; đóng/bấm ngoài vẫn như cũ |
| Bấm/hover R, chuông; kéo ra ngoài | R mở Chat thường, chuông mở thông báo/badge đúng; mỗi click một owner, không walk; kéo ra ngoài không kích hoạt nút khác |
| Dialog/menu/panel/Chat/notification/VIP | Không click xuyên modal; đóng modal không tự mở nút phía dưới; popup lời mời nhanh là vùng riêng, các nút không bị khóa toàn màn |
| Phím X, /admin, đổi map/relog/VS/offline | Các đường mở cũ hoạt động; điều kiện ẩn R/chuông giữ hành vi; không đăng ký trùng; vị trí slot còn lại ổn định |
