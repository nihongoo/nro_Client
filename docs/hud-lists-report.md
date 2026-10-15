# Cụm 4 — Người / Vật phẩm

**Chưa compile, chạy game/test runtime hoặc build Unity/APK.** Chỉ rà source và diff; kiểm tra whitespace/UTF-8 đạt, đối chiếu thân focusManualTo/pickItem/paintListCharsInMap/controlTeleChar với HEAD giữ nguyên; không merge/stash/xóa Git, không sửa server. Commit/push/tạo PR theo yêu cầu user; các file local ngoài phạm vi không đưa vào commit.

## Đã làm

- Giữ nguyên neo, chiều rộng và cách giới hạn chiều cao panel đã được user test: Người 130, Vật phẩm 190; header 24, hàng 28/32, thân tối đa 140/160 px thiết kế, có mức tối thiểu theo font. Kích thước qua `PixelsToLogical`; dưới HUD trên, tránh skill/joystick/potion qua registry, không dịch gameplay.
- `Assets/Scripts/Hud/MapListsHud.cs:11/:202`: **một hình chữ nhật world-logic cố định**, chung `InRange(IMapObject)` cho Người/Vật phẩm. Bốn hằng ở một chỗ: `HalfWidthLeft=120`, `HalfWidthRight=120`, `Up=90`, `Down=60`; hai nửa rộng bằng nhau nên đúng `|dx| <= 120`, `-90 <= dy <= 60`, tính cả biên. Không camera/screen/zoom/PixelsToLogical trong bộ lọc; không vẽ khung. Loại invisible/fusion/setPos và item status2 như trước.
- Tâm tham chiếu là chân người chơi **Char.cx/cy**, field thật mà `Char.getX/getY` trả về. Người/đệ tử dùng chân `getX/getY`; item dùng tâm bounds IMapObject: `(getX(), getY()-getH()/2)` = `(ItemMap.x, ItemMap.y-10)` với chiều cao thật 20, không phụ thuộc ảnh/icon hay animation bay. Không dùng giao sprite với vùng lọc: chính điểm đo phải nằm trong hình chữ nhật.
- Quy đổi số đo: world-logic = pixel game / **mGraphics.zoomLevel**, sau khi loại scale hiển thị Game view/safe-area nếu có. Với 1424×670, nhánh PC trong `MotherCanvas.cs:63` chọn zoom2 (nếu đây là kích thước đầu vào thật): vùng 240×150 logic tương ứng 480×300 pixel; mobile nhánh `:42` với cùng đầu vào chọn zoom3, tương ứng 720×450 pixel. S22 đầu vào2340×1080 chọn zoom4, tương ứng960×600 pixel; PC1280×720 zoom2 tương ứng480×300 pixel. **Đây là suy ra từ source, chưa đo runtime/ảnh khung đánh dấu**: ảnh khung đo không có trong lượt này, không chạy game để đọc zoom hiện tại. Giữ đúng mặc định120/90/60 user chỉ định, không tự điều chỉnh theo màn hình. GUI/safe-area có thể đổi số pixel hiển thị, không đổi khoảng cách world.
- `Char.cs:5862`, `MapListsHud.cs:51/:204`: đọc `ExistingLocalPlayer` trả field myChar có sẵn, không gọi getter myCharz để tự tạo nhân vật. Chưa có nhân vật/đang chuyển map/loading: visibility tắt, cả hai panel không paint/nhận input; InRange cũng chặn null/chuyển map. Chết vẫn dùng chân hiện có, không dereference đối tượng thiếu; không đổi gameplay khi chết.
- `MapListsHud.cs:158`: chiếu trực tiếp `GameScr.vCharInMap/vItemMap` còn sống trước input và paint, tránh cache trễ một frame khi nhân vật/đối tượng di chuyển hoặc item biến mất. Chỉ người chơi khác và đệ tử xác định được của họ; không self, đệ tử self, boss, mini-pet không xác định chủ hoặc Trọng tài. N = số mục qua bộ lọc trong phạm vi. Rời phạm vi/map hoặc nhặt xong thì mất hàng, **không ghi/xóa charFocus của game**.
- `MapListsHud.cs:115`: rỗng trả rect rỗng, tắt visibility header/body; row/rail không có bounds hữu dụng, không paint/chặn pointer/chiếm obstacle. Đăng ký một lần, không gỡ/đăng ký lại theo số mục; tự hiện lại khi có đối tượng. Tối đa 6 slot tái dùng để cuộn 5 hàng + phần hàng cắt.
- `MapListsHud.cs:396`: một bộ style mới: nâu `#28180c` alpha .55, header đen alpha .25 phủ lên; chữ kem `#fff3da`; phân cách `#f2b45a` alpha .2; focus/hover `#f28c1b` alpha .35. Padding 8, dot tròn 8, icon 20 px thiết kế. Nền/ô icon xấp xỉ bo bằng cắt bốn góc 1 pixel thật, đúng phương án được cho phép; nét/icon border vẽ màu đặc, không phóng skin. Fill/clip snap qua `HudButtonSkin.ScreenPixels`; separator/border 1 pixel, không đổi zoom/filter toàn game. Icon SmallImage chỉ thu vừa ô khi cần, không phóng ảnh nhỏ.
- `mFont.cs:414`: thêm renderer màu cục bộ dùng nguyên glyph/font/cỡ game; bitmap zoom 1 tint trực tiếp atlas font bằng GUI, zoom lớn dùng renderer chữ sẵn có. Khôi phục màu/GUI sau khi vẽ, không đổi texture/font/palette dùng chung. Font bitmap thiếu “·”/“…” dùng “.”/“...” có sẵn. Tên dài cắt, không scale font.
- Focus Người dùng `Char.focusManualTo(object)` như bấm nhân vật; hàng đang trùng `charFocus` tô cam, kể cả focus từ gameplay. Vật phẩm sáng gọi `GameScr.cs:3984/:3992` → private `pickItem()` cũ: MovePoint/lệnh 8002, item673 vẫn hỏi. Quyền own/free dùng chung `AutoPick.cs:284`, không pathing mới. **Hàng mờ 55% vẫn focus-only theo user**, không tự chạy/nhặt; cả hai loại giữ click, không lọt xuống gameplay.
- Registry order 60, dưới modal; `GameCanvas.cs:598`, `GameScr.cs:2654/:5151` chặn input/paint đúng tầng. Header toggle, kéo/wheel/rail cuộn, giữ reference khi nhấn–giữ–thả để không chọn nhầm hàng thay thế. Trạng thái phiên, không RMS; `Service.cs:858` reset khi login. Title tên map/khu vẫn trang trí ở giữa, không chặn pointer.

## Nhận biết đệ tử

- Client thật: `Char.cs:5941` nhận tiền tố `$`, bỏ tiền tố rồi đặt `isPet`. Bộ lọc kiểm tra **cả isPet và tiền tố** vì render có thể chưa chạy. Chủ không được suy từ tên.
- Đã **chỉ đọc** server để xác minh hợp đồng dữ liệu: `src/nro/models/services/PetService.java:296/:300`, `createNewPet` đặt tên `$…`, ID đệ tử người chơi = **-player.id**. `MapListsHud.cs:191` chỉ nhận khi có người chủ ID dương tương ứng còn trong vCharInMap, không phải mình. Không sửa server/protocol. Chủ không cần nằm trong hình chữ nhật nếu vẫn được client biết; chính hàng đệ tử phải qua InRange.
- Hàng lưu tên đệ tử **và tên chủ**, ví dụ `Mabư · TenChu`. `MapListsHud.cs:354` nếu thiếu chỗ chia phần tên để cả hai còn được biểu diễn bằng dấu cắt, không bỏ hẳn tên chủ.
- `Assets/Scripts/Assets.src.g/PetFollow.cs:3` là pet trang trí gắn với chủ, chỉ có smallID/animation, không phải Char có tên/đối tượng focus độc lập. Mini-pet `#` chưa có hợp đồng owner đáng tin được xác minh nên không tự đoán đưa vào list.

## Màu chấm — palette game, không tự gán màu PK

`Char.cs:5865` dùng chung nguyên nhánh font tên overhead. HUD đọc color1; zoom 1 đọc palette colorJava của chính font (`StatusColor`). Tiền tố đệ tử chưa được renderer xử lý vẫn nhận màu pet; override map170 giữ ưu tiên.

| Cờ / ưu tiên | Màu/font nguồn |
|---|---|
| Map170 flagImage2325 / 2323 (ưu tiên cao nhất) | tahoma_7_blue / tahoma_7_red |
| Đệ tử isPet hoặc tiền tố $ + owner đã xác minh | tahoma_7_blue1Small — xanh cyan |
| Người PK3 hoặc PK5 | nameFontRed — đỏ |
| Người PK4 | nameFontYellow — vàng |
| Người còn lại cùng clan.ID | nameFontGreen — xanh lá |
| Người còn lại | tahoma_7b_white — trắng |
| Item nhặt được / chưa nhặt được | nameFontGreen / nameFontRed theo CanPickHudItem |

Màu chấm giữ palette nguồn, không thay bằng RGB tham chiếu thiết kế; chữ/viền mới dùng màu kem được yêu cầu.

## Đã xóa / còn lại

- `MapListsHud.cs:202`: xóa toàn bộ tạo Rect viewport từ cmx/cmy/gW/gH/transY và phép Overlaps(bounds) cũ; không giữ hai bộ lọc. Thay bằng so sánh điểm world với bốn biên quanh chân người chơi. Rà quanh chỗ sửa không có helper camera riêng khác cần xóa; phần ngoài phạm vi chỉ liệt kê bên dưới.

- `MapListsHud.cs:158`: xóa chèn self vào danh sách/N và bộ lọc cũ chỉ charID dương từ cache cả map; thay bằng bộ lọc live + phạm vi + ownership. Xóa lấy tất cả item toàn map không xét phạm vi/status.
- `MapListsHud.cs:316/:332/:396`: xóa màu cũ 2721889/13524492, nền xanh nhiều lớp, dot chữ thập 6px, chữ trắng/grey cho hàng disabled, scrollbar trắng và nhánh fit phóng icon. Thay bằng đúng **một bộ style** trên; không giữ painter cũ chạy song song. Không có ảnh panel cũ chỉ dùng tại đây cần xóa.
- Đợt đầu cụm4 đã xóa caller HUD `MainMod.updateTouch → controlTeleChar` (nền develop97468bd, MainMod.cs:1359); không còn hitbox teleport riêng trên list. Getter palette overhead chỉ tách nguyên logic cũ.
- **Chỉ liệt kê, không xóa ngoài phạm vi:** `MainMod.cs:500` paintListCharsInMap, caller comment `:362`, `:1978` controlTeleChar, widthRect/heightRect `:133/:135`, helper paintPKFlag/paintCharInfo và cờ/menu/RMS DoHoa.isShowCharsInMap. Giữ nguyên listCharsInMap và cập nhật nguồn `:303` cho các caller cũ; panel mới chiếu trực tiếp live-map để tránh nguồn này lọc mất pet/trễ thay đổi. Không đổi logic gameplay AutoPick/AutoMap/AutoTrain.
- **Tab/F:** user xác nhận giữ gameplay, chỉ bấm header, **không badge**. Tab vẫn đổi target; F vẫn dùng item921/454. Không thêm shortcut gây xung đột.

## Git

Branch `feat/hud-lists` từ origin/develop **97468bd** (PR17); tree nền khớp feat/hud-notification-panel **b0d376d9f2baaf8455cac8bda10f14b7c48725df**. Chờ user duyệt xóa local/remote `feat/hud-top-info`, `feat/hud-top-actions`, `feat/hud-notification-panel`; chưa xóa/drop gì. Giữ develop/master/custom-server-address/login-batch3a/ui-batch-c + worktree; stash client d1a9a4a/a9a997d/dcbb14c/5922203, server6121b4b. Giữ nguyên file local logo/Packages/ProjectSettings/UserSettings.

## Ma trận test tay — chưa chạy

| Tình huống | Kỳ vọng |
|---|---|
| Đứng một mình, không item trong hình chữ nhật | Cả hai panel ẩn, không chiếm pointer/obstacle |
| Có 1 người khác | Người · 1, không tên mình/boss/đệ tử mình |
| Người khác có đệ tử | Người và đệ tử qua phạm vi đều được đếm; hàng pet giữ tên pet + chủ, màu pet; không đoán mini-pet |
| Người/pet vào-ra hình chữ nhật, rời map | Header N và hàng cập nhật frame sau; game focus cũ không tự bị xóa |
| Item rơi/ra hình chữ nhật/nhặt/biến mất | N đúng, không hàng ma; rỗng ẩn, xuất hiện lại tự hiện |
| Bấm hàng Người / focus từ gameplay | Focus đúng đối tượng theo tùy chọn deselectChar; hàng hiện tại tô cam; không click xuống đất |
| Item sáng / mờ | Sáng chạy tới/nhặt theo luồng cũ; mờ focus-only, không chạy/nhặt; alpha55%, chấm đúng quyền |
| PC1280×720 / S22 2340×1080 / 20:9 + tai thỏ | Giữ vị trí/kích thước đã test, chữ kem cùng font; nét1px, dot tròn, icon không phóng nhỏ; không che skill/joystick/potion |
| Map nhiều tầng | Item/người có điểm đo cao hơn chân >90 logic không hiện; lên tầng đó thì hiện nếu điểm nằm trong cả bốn biên. Tầng khác vẫn nằm trong hình chữ nhật thì được tính: không thêm dò nền/line-of-sight |
| Đi ngang và kiểm tra biên | dx=±120 còn hiện, ±121 mất; dy=-90/60 còn hiện, -91/61 mất; N/ẩn-rỗng cập nhật frame sau |
| S22 2340×1080 và PC1280×720 | Cùng vị trí world cho cùng danh sách, bất kể camera/zoom; phạm vi thật không đổi khi resize |
| Chết / chưa có nhân vật / đang đổi map | Không null; chưa có nhân vật/đổi map ẩn cả hai panel; không tự tạo player hay hủy focus |
| Resize / header thu-mở | Provider cập nhật; mở/đóng độc lập, không gán Tab/F; panel rỗng rồi hiện lại giữ trạng thái phiên |
| List dài, kéo/wheel/rail | Cuộn đủ, tên cắt; pet/chủ đều còn phần tên; nhấn–giữ–thả không chọn nhầm hàng đổi slot |
| Panel thông báo/form/dialog/VIP/chat đang mở | List không nhận click xuyên, nhân vật không di chuyển do bấm list; frame đóng modal an toàn |
| Đổi map / relog | Không đăng ký trùng; đổi map reset scroll/capture; relog mặc định mở, nhưng rỗng vẫn ẩn |

**Chưa compile/chạy/build; tất cả test tay đang chờ user tự build kiểm tra.**

## Bổ sung trên feat/hud-combat-mobile — 15/10/2026

Theo ảnh user, chỉ mobile: `MapListsHud.HeaderBounds` lấy rộng item từ avatar.xMin đến top-info.exp.xMax (bỏ phần FPS), rộng người từ TopActionsHud.Hub.xMin đến ChatButton.xMax. Thay mốc 190/130 px cho mobile; PC giữ nguyên. Mọi body/row/hitbox đi theo header registry, giữ chiều cao/range/cuộn/callback.

`MapListsHud.Refresh` lọc item.template.type==22 khỏi danh sách/count mobile: đây là nhóm vệ tinh (client ItemMap.cs:204; đối chiếu read-only server Trade.java:244), bao gồm mọi loại, không hardcode từng template ID. Không sửa AutoPick/vItemMap/buff/logic nhặt/server. Chưa compile/chạy/build. Chi tiết và test tay trong hud-combat-mobile-report.md, mục Badge/menu/danh sách.
