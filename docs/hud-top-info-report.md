# HUD cụm 1 — thông tin trên trái

Branch `feat/hud-top-info`, nền `d18198c`. Cập nhật 15/10/2026 theo ảnh S22: **user xác nhận layout đã chuẩn**; lượt này giữ nguyên provider/layout/input/font và thanh HP đã được duyệt; chỉnh nền avatar và độ mượt KI/EXP.

## Sửa viền avatar sau ảnh test mới

- Nguyên nhân nền avatar còn vẽ từng scanline từ texture Point của nút R ở một đường kính khác ảnh gốc, nên cạnh tròn bị lấy mẫu thành bậc. Thay bằng một lần vẽ toàn nền đã bỏ glyph, dùng bản sao Bilinear riêng cho avatar tại `Hud/HudButtonSkin.cs` (`PaintBackgroundPixels`).
- Giữ nguyên rect/đường kính/căn tâm, sprite đầu và cách vẽ đầu scale nguyên; không sửa HP/KI/EXP, nền Point của ba nút bên phải hoặc filter toàn game. Bản sao nền avatar được cache theo texture nguồn và normal/hover, hủy bản cũ khi nguồn thay đổi. Không thêm asset.
- User tự build kiểm tra lại cạnh ngoài avatar trên S22, PC, resize/đổi zoom, cả normal và hover. **Chưa compile/chạy sau chỉnh này; không push/merge/stash.**

## Hiện trạng sau chỉnh đồ họa

- `Assets/Scripts/Hud/TopInfoHud.cs:40`: một provider `ResolveBounds` cho toàn cụm. HP/KI cùng W; EXP + khoảng cách + vùng `FPS: 999` = W, FPS căn phải. Giữ giới hạn trước bánh răng/R, không di chuyển bánh răng/rương.
- `TopInfoHud.cs:161/:193`: dùng lại asset game `myTexture2dHP.png` (đỏ), `myTexture2dMP.png` (xanh dương), `SP.png` (xanh lá). Vẽ 3 phần: giữ tỉ lệ hai đầu bo, kéo phần giữa theo chiều rộng hiện có; không tile toàn sprite làm lặp viền. Nền phần chưa đầy lấy cùng asset làm tối, phần đầy giữ màu/viền/lớp sáng nguyên bản. Không còn thanh màu phẳng viền vuông của lượt S22 trước.
- Khôi phục riêng `Assets/Resources/res/x{1,2,3,4}/mainImage/SP.png` và `.meta` từ HEAD (8 file), dùng cho EXP tại `TopInfoHud.cs:15`; cache load lại nếu zoom đổi. HP/MP vốn còn trong repo vì panel/world dùng chung. Không khôi phục painter hay khung HUD cũ.
- `TopInfoHud.cs:239/:261`: nền avatar dùng chính asset nút R (`GameScr.imgChatPC`, hover dùng `imgChatsPC2`): viền/nền cam, hover xanh. Lấy các dải nền sạch hai bên, bỏ vùng glyph chat ở giữa khi vẽ để không lộ chữ R/bong bóng dưới đầu nhân vật. Bỏ nền kem phẳng; giữ đầu nhân vật scale nguyên, crop tròn, menu và chấm đỏ cũ.
- `TopInfoHud.cs:177`: KI/EXP dùng hai bản sao texture được cache với `FilterMode.Bilinear`; HP vẫn dùng texture gốc và cùng renderer đã được user duyệt. Khi texture nguồn đổi sẽ hủy bản sao cũ. Không sửa filter texture dùng chung, không đổi ảnh asset trên đĩa hoặc font.
- Giữ font game. Số sức mạnh quá dài vẫn được clip trong EXP và cảnh báo, không tự scale/rút gọn. Ảnh user gửi cho thấy chuỗi `47.129.020.356` hiện vừa; cần kiểm tra thêm chuỗi dài hơn khi tự build.

## Nguyên nhân pixel hóa đã rà ở lượt S22 (lịch sử)

1. Painter trước tile texture HP/MP/SP làm lặp chi tiết/viền raster; avatar cắt từng pixel logic, zoom 4 trên S22 khiến nấc cắt tương ứng 4 pixel GUI (`MotherCanvas.cs:76`, `mGraphics.cs:148`). Không có lệnh phóng avatar tới diameter trong painter cũ.
2. `Image.cs:431` đặt `FilterMode.Point`, không phải bilinear. `ScaleGUI.cs:115` có scale viewport khi safe area khác màn hình, có thể tạo tỉ lệ lẻ; chưa có log safe area thực tế để khẳng định nó xảy ra trên lần test.
3. Ảnh test mới cho thấy KI/EXP răng cưa: tại x4, ảnh MP cao 24px, SP cao 32px, HP cao 40px; co về cùng chiều cao hàng với Point lấy mẫu gây nấc ở mép cong, HP được user chấp nhận. Sửa bằng Bilinear trên bản sao riêng KI/EXP; giữ HP, scale/zoom MotherCanvas, font và filter toàn game. Avatar vẫn vẽ texel đầu theo scale nguyên; nền lấy từ nút R.

## Đã xóa / giữ asset

Dòng dưới là vị trí **trước lượt sửa S22**, dùng định vị phần đã xóa. Một bộ thanh mới hiện được gọi qua `GameScr.cs:5739` → TopInfoHud.

| Đã xóa | File:dòng trước sửa S22 |
|---|---|
| Toàn bộ `paintImageBarRight`, `paintVersusBar` và bốn caller/translate thanh VS | `Assets/Scripts/GameScr.cs:5718/:5740/:5853` |
| `paintHPCT` không có caller | `GameScr.cs:7407` |
| Field/load `imgPanel`, `imgPanel2`, `imgSP`, `imgHP_NEW`, `imgKhung`; các field/gán hpBarX/Y/W, mpBarW, spBarW, expBarW hết consumer | `GameScr.cs:439/:811/:979/:1098/:6252` |
| Khối tên/HP mục tiêu theo imgScrW/2, init chỉ gán hằng số loadInforBar, isSmallScr hết caller | `GameScr.cs:820/:1810/:5745/:6121/:7061` (trước dọn cuối) |
| Tile toàn sprite và công thức FPS nằm sau toàn thanh EXP | `Assets/Scripts/Hud/TopInfoHud.cs:26/:92` (bản trước S22) |

Còn xóa **16 PNG + 16 `.meta`**: `Assets/Resources/res/x{1,2,3,4}/mainImage/{myTexture2dpanel.png,panel2.png,i_hp.png,i_khung.png}`. Đã rà không còn source-load hoặc GUID reference scene/prefab/asset/material/animation. `SP.png` đã khôi phục theo yêu cầu đồ họa mới; ảnh nhị phân không có số dòng.

Giữ `imgHP/imgHPLost` cho thanh Mabu trong world; `imgMP/imgMPLost` cho panel stamina. Chúng không tạo thêm bộ thanh góc trái. Kích thước panel cũ chuyển thành số đo tại `GameScr.cs:654`; các tham chiếu ở `DoHoa.cs:53/:59`, `MainMod.cs:1359/:1367`, `NotificationHudLayout.cs:72` giữ nguyên vị trí cụm 2.

Đợt cụm 1 trước đã xóa tọa độ/map-zone/tên bản thân, icon/text buff HUD, panel màn nhỏ, hitbox 60×50, menu click trong painter, flare cũ, smethod_7/8, getInjure và nhánh G đồ họa không tới được. Giữ gameplay buff/timer, logic focus/NPC, portrait/timer VS; giữ paintListCharsInMap/listCharsInMap/controlTeleChar. Các smethod rỗng khác chỉ ghi nhận, chưa xóa ngoài phạm vi.

Sửa server `ClanService.java:348` (null-check clan message trước playerId) giữ nguyên, không sửa thêm server trong lượt đồ họa này.

## Kiểm tra / giới hạn

Đã xem ảnh user, đối chiếu trực quan các asset HP/MP/SP và rà source/diff. **Chưa build Unity/APK, chưa compile hoặc chạy Unity/game/test runtime sau sửa. Không merge/stash.** Theo yêu cầu mới của user, đóng gói commit/push cụm 1 và tạo PR vào develop để user tự merge; chưa làm code cụm 2. Các branch/stash đã giữ theo duyệt ở lượt trước vẫn nguyên; không sửa file local logo/Packages/ProjectSettings/UserSettings.

## Ma trận test tay — user tự build

| Case | Cần xác nhận sau chỉnh đồ họa |
|---|---|
| S22 2340×1080 | Layout giống ảnh đã duyệt; HP giữ nguyên; KI/EXP bo mượt hơn, không răng cưa ở mép/điểm ghép; HP/KI cùng W, EXP+FPS đúng W; avatar có nền/viền cam giống R, không lộ glyph chat |
| PC 1280×720 | Cạnh bo không kéo méo theo chiều rộng, font giữ nguyên; FPS không dưới bánh răng/rương |
| Mobile 16:9/20:9, tai thỏ trái/phải | Vị trí theo safe viewport; avatar tròn, sprite đầu scale nguyên, không đổi filter chung |
| Resize/đổi zoom có sẵn | Rect cập nhật đồng thời; EXP nạp đúng asset zoom, hai đầu bo ổn định; viewport quá hẹp không vẽ dưới nút |
| HP/KI/EXP 0%, rất thấp, 50%, 100% | Nền tối vẫn bo; fill không vượt thanh, không lặp cạnh/khung; chỉ một bộ thanh |
| Sức mạnh dài; FPS 9/99/999 | Không scale font, FPS căn phải; sức mạnh tràn chỉ clip trong EXP và ghi cảnh báo |
| Avatar/menu, hover, tin bang; các modal | Nền cam/hover xanh rõ, mở menu cũ; không bắt click xuyên dialog/panel/Chat/thông báo/lời mời/VIP |
| Võ đài/phó bản/Mabu/panel stamina | Không có painter HUD cũ vẽ đè; dữ liệu gameplay và các thanh world/panel riêng vẫn hoạt động |
