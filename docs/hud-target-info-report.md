# Cụm 6 — Thẻ mục tiêu và danh sách Người/Vật phẩm

Ngày 16/10/2026. Branch giữ nguyên `feat/hud-target-mobile`, base `origin/develop` 015c672. Hai phần commit riêng; không sửa server, không push/merge/stash. Các thay đổi logo, Packages, ProjectSettings, UserSettings có sẵn được giữ ngoài commit.

## Phần 1 — Thẻ mục tiêu mobile

- `Assets/Scripts/Hud/TargetMobileHud.cs` Register/Bounds/PaintPanel/BuildPanel: một provider `target-mobile.bounds`, Center/Top, order10, **blocksPointer=false**. Rect con chỉ là các lát nội bộ của provider, không đăng ký thêm. Kẹp đối xứng giữa top-info và hàng nút phải, không đẩy thẻ xuống che list. HP ưu tiên192px, tối thiểu160px; không đủ khoảng trống thì chỉ còn map title.
- Nền tab hai bậc: thân trên212px + vai cong14px mỗi bên, mép HP cách thân10px; bậc dưới theo chiều rộng tên + pill + khoảng cách + lề20px, tối thiểu140px và tối đa thân trên−52px. Tên quá dài dùng vùng hiển thị có cuộn; màn hẹp giảm bậc thụt từ26 xuống20px mỗi bên để vẫn giữ bậc dưới140px. Hàng dưới cách HP3.5px. Cao58px thiết kế khi font native cho phép; viewport rất thấp phải tăng đến chiều cao chứa đủ ba hàng font nguyên cỡ, không cắt/scale font.
- Đường bao polygon có vai Bezier lõm, gờ nối ngắn, tất cả góc chuyển bậc/đáy dùng đường cong quadratic bo7px; sai số lấy mẫu đường cong <=0.75 pixel thật, coverage theo signed distance khử răng cưa1px. Nền #2b1a0a alpha88%, viền #f2b45a1.5px, không kẻ ngang mép trên. Texture RGBA32 đúng pixel đích qua ScreenPixels, không scale sprite nhỏ; cache một texture theo kích thước trên/dưới, height/step/bo/zoom và kích thước pixel safe-area. Hủy texture trước khi dựng lại, dọn khi đổi map/viewport/session qua MobileCombatPointers.Cancel; không tạo texture/array/path/key-string mỗi frame.
- Giữ nguyên painter HP đỏ/track, fraction/căn giữa `HP + số`, font game và marquee từng vùng. Tên khu dùng font thường kem mờ. Chưa có mục tiêu/NPC/item: chỉ map title nhỏ có bóng tối, không nền và không chặn pointer. Char còn sống: tên/HP/sức mạnh thật (thiếu dữ liệu tạm `SM --` cho người chơi); Mob không có field sức mạnh nên ẩn pill. Boss dạng Char ID âm cũng ẩn pill nếu chưa có số. Mục tiêu chết/status chết/ẩn/ra khỏi vector map hoặc game bỏ focus thì thẻ ẩn; không tự xóa focus, không đặt phạm vi hoặc timeout mới.
- `Assets/Scripts/GameScr.cs`: đăng ký/paint thẻ; VS head cũ chỉ nhường khi thẻ mobile đang hiện, countdown đặt dưới thẻ. PC giữ cũ. `Assets/Scripts/Hud/MapListsHud.cs`: map title nhường cho thẻ đang hiện, fallback mobile có bóng tối.
- `Assets/Scripts/GameCanvas.cs`, `Controller.cs` case−79: giữ phần đang làm dở lấy sức mạnh qua API sẵn `Service.getPlayerMenu(id)` cho Char chưa biết; kiểm ID/object/owner/map/zone, giới hạn750ms/yêu cầu,2lần/đối tượng (retry5s),32entry. Không mở menu; không đổi handler PC/server.

Đã xóa: bộ nền Seam/tai thỏ/vát cũ và nhánh fallback hộp bo, các rect map/HP/name/power đăng ký riêng, capture chặn pointer thẻ, nhánh viền hộp cũ không còn dùng. Giữ mask HP/pill để không thay style thanh. Ngoài phạm vi không xóa: head/VS PC, tên/HP trên đầu nhân vật, logic focus và dữ liệu sức mạnh server không gửi.

## Phần 2 — Danh sách (sẽ cập nhật ở commit riêng)

Đã xác định cần tách vùng bắt joystick rộng khỏi vùng hình vẽ khi tính chỗ trống, và bỏ trần chiều cao cố định160/140px khi font native lớn hơn thiết kế. Số liệu, sửa cuộn và nguyên nhân chi tiết sẽ bổ sung ở commit phần2.

## Kiểm tra

Đã kiểm tra tĩnh delimiter/encoding và diff. **Chưa compile/chạy client, Device Simulator, build Unity/APK.**

| Test tay — tất cả chưa chạy | Cần xác nhận |
|---|---|
| Mobile844×390; Android16:9/20:9, tai thỏ trái/phải; máy thật | Tab hai bậc cong mượt, đủ thông tin, không che avatar/nút phải; màn hẹp giảm HP tới160px hoặc chỉ còn tên khu |
| Quái/boss/người chơi, tên dài, sức mạnh ẩn/hiện | Đúng dữ liệu, bậc dưới ôm nội dung, tên/số dài cuộn đủ, HP giữ style/căn giữa |
| NPC/chưa có mục tiêu/mục tiêu chết/mất/bỏ focus | Chỉ tên khu, tab biến mất ngay theo trạng thái; không xóa focus hoặc thêm timer |
| Chuyển mục tiêu liên tục, đổi map/relog/resize | Không nhầm phản hồi sức mạnh, không đăng ký trùng, texture cũ được hủy |
| Bấm xuyên thẻ, joystick+chiêu, modal | Thẻ không chặn pointer; controls/modal vẫn quyết định input theo registry |
| PC1280×720 | HUD mục tiêu và gameplay giữ nguyên |
| Danh sách0/1/3/5/10, kéo/lăn chuột | Bổ sung kết quả tĩnh và ca cuộn ở phần2; không đẩy nhân vật chạy |
