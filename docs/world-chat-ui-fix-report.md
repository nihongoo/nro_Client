# Sửa giao diện World chat — Stage 3

Ngày kiểm tra: 13/10/2026. Client: `feat/world-chat-p3`. Commit sửa runtime: `ca3f845`.

## Kết quả và đối chiếu thanh gốc

Đã đối chiếu `origin/develop` (`e9f1721`), `Assets/Scripts/GameScr.cs:7070`, hàm `paintChatVip`. Thanh gốc dùng đúng các lệnh:

```csharp
g.setClip(0, GameCanvas.h - 13, GameCanvas.w, 15);
g.fillRect(0, GameCanvas.h - 13, GameCanvas.w, 15, 0, 90);
mFont.tahoma_7b_yellow.drawString(g, text, x, GameCanvas.h - 13, 0, mFont.tahoma_7b_dark);
```

Đã dùng lại nguyên vị trí, chiều cao vẽ, nền đen mờ và font vàng có bóng tối. Phần nhìn thấy trong màn hình cao 13 pixel logic; hai pixel cuối của clip 15 nằm ngoài màn hình. Giữ nguyên hành vi alpha của overload `fillRect` hiện có, không sửa mGraphics. Chỉ nội dung và vị trí chạy chữ lấy từ tin world mới nhất.

**Thanh gốc cũng có giao hình học với nút dưới:** hai hàng skill và hình nút hồi máu có thể chạm/giao với dải `h - 13`; hitbox skill còn rộng hơn hình vẽ. Bản Stage 3 làm rõ lỗi hơn do tăng thanh lên 20 pixel, đổi nền thành vàng đặc và vẽ sau toàn bộ HUD. Không đổi bất kỳ công thức đặt skill, joystick, hồi máu hay tấn công nào.

## Luồng vẽ và bấm sau sửa

- `Assets/Scripts/WorldChat/WorldChatHud.cs:56`: dùng nguyên cách vẽ thanh gốc; bỏ nền vàng đặc, viền nâu, font tối và padding cũ.
- `Assets/Scripts/GameScr.cs:5294`: vẽ thanh trước skill, hồi máu và các điều khiển touch. Bỏ lần vẽ muộn ở GameCanvas.
- `Assets/Scripts/GameScr.cs:675`: thu thập hình/vùng bấm hiện có của skill, hồi máu, tấn công, chọn mục tiêu, joystick, R, chuông và nút menu.
- `Assets/Scripts/WorldChat/WorldChatLayout.cs:14`: vùng thanh đúng 13 pixel thực sự nhìn thấy. `CanTapBar` loại các vùng điều khiển; chạm vào phần giao nhau thuộc nút, không mở chat.
- `Assets/Scripts/WorldChat/WorldChatLayout.cs:24`: tìm và cache vùng trống cho lịch sử theo vị trí điều khiển thật. Không di chuyển nút. Chừa mép màn hình và thanh dưới. Khung có thể hẹp hơn ở màn dài/chế độ joystick.
- `Assets/Scripts/WorldChat/WorldChatDialog.cs:26`: input và nút Gửi cùng hàng, chiều rộng thích nghi; lịch sử có clip/cuộn. `:111`: tap ngoài đóng và tiêu thụ input; X/Escape/Back vẫn đóng được.
- `Assets/Scripts/GameScr.cs:6030`, `:6479`: giữ hình skill/hồi máu/tấn công/R khi mở lịch sử. Dialog vẫn nhận input theo cơ chế modal; tap ngoài chỉ đóng, không đồng thời kích hoạt gameplay. Chuông giữ cơ chế ẩn khi có dialog của P1.
- `Assets/Scripts/GameCanvas.cs:2260`: InfoMe vẽ trước khung lịch sử world, tránh popup hướng dẫn che input/lịch sử. Các dialog khác giữ thứ tự cũ.
- `Assets/Scripts/ScaleGUI.cs:18`: Editor/Device Simulator dùng Screen.safeArea và cùng phép biến đổi vẽ/bấm; không gọi Android JNI trong Editor. Nhánh Android trên thiết bị giữ cách xử lý safe area/system bars hiện có.

Không đổi server, protocol, hằng số phí, trừ tiền, cooldown hay validation gửi tin.

## Device Simulator và ảnh thật

Unity 2022.3.62f1, scene SampleScene, màn GameScr thực tế. Ảnh lấy trực tiếp render texture của Simulator; chỉ chuẩn hóa chiều đọc D3D, không dựng lại hình. Đóng Game view thứ hai để tránh khung bị cũ. Độ phân giải render được đặt như bảng; DPI/hình máy/safe area dùng profile Simulator. Client Editor hiện chỉ khởi tạo layout lúc khởi động, nên helper gọi lại các hàm layout hiện có sau khi đổi profile; không sửa Main hay cách đặt skill.

Ảnh trước là mã Stage 3 trước sửa (`69bc474`), ảnh sau là mã runtime `ca3f845`. Ảnh lịch sử dùng fixture **cục bộ 30 tin** cùng phí 10 gem + 10.000.000 vàng để kiểm tra bố cục; không broadcast hay trừ tiền. Đây là kiểm tra UI, không phải chứng nhận luồng thu phí trên server đang chạy.

| Tỷ lệ / thiết bị Simulator | Render / logic | Kết quả | Trước / sau / lịch sử / joystick |
|---|---|---|---|
| 16:9 — Galaxy S7 | 960×540 / 480×270 | PASS: thanh, vùng bấm, khung, kéo, X, tap ngoài, ticker; joystick PASS | [Trước](world-chat-ui-review/16-9-before.png) · [Sau](world-chat-ui-review/16-9-after.png) · [Lịch sử](world-chat-ui-review/16-9-history.png) · [Joystick](world-chat-ui-review/16-9-analog-history.png) |
| 18:9 — Asus ROG Phone | 960×480 / 480×240 | PASS cùng các kiểm tra trên | [Trước](world-chat-ui-review/18-9-before.png) · [Sau](world-chat-ui-review/18-9-after.png) · [Lịch sử](world-chat-ui-review/18-9-history.png) · [Joystick](world-chat-ui-review/18-9-analog-history.png) |
| 20:9 — Huawei P40 Pro, đặt render 20:9 | 960×432 / 480×216 | PASS; khung thấp hiển thị khoảng một dòng và cuộn được; joystick dùng khung hẹp 132×90 | [Trước](world-chat-ui-review/20-9-before.png) · [Sau](world-chat-ui-review/20-9-after.png) · [Lịch sử](world-chat-ui-review/20-9-history.png) · [Joystick](world-chat-ui-review/20-9-analog-history.png) |
| Tablet 4:3 — iPad Air 2 | 800×600 / 400×300 | PASS cùng các kiểm tra trên | [Trước](world-chat-ui-review/tablet-before.png) · [Sau](world-chat-ui-review/tablet-after.png) · [Lịch sử](world-chat-ui-review/tablet-history.png) · [Joystick](world-chat-ui-review/tablet-analog-history.png) |
| Tai thỏ — iPhone 12 Pro | 960×444 / 480×222 | PASS; safe area (53,24,853,420), thanh/nút/khung nằm trong viewport an toàn | [Trước](world-chat-ui-review/notch-before.png) · [Sau](world-chat-ui-review/notch-after.png) · [Lịch sử](world-chat-ui-review/notch-history.png) · [Joystick](world-chat-ui-review/notch-analog-history.png) |

Safe area của bốn cấu hình đầu phủ render; cấu hình tai thỏ có insets thật từ Simulator. Safe area dùng tọa độ Screen từ dưới lên, viewport GUI được đổi sang từ trên xuống. Kết quả chi tiết: [before-state.json](world-chat-ui-review/before-state.json), [after-state.json](world-chat-ui-review/after-state.json).

Tổng 69.600 điểm vùng bấm được kiểm tra ở 5 profile × 2 chế độ điều khiển, gồm cả hàng ngoài mép thanh. Tất cả khung lịch sử không giao vùng điều khiển đã thu thập và nằm trong màn hình. 5 kiểm tra kéo lịch sử/X/tap ngoài và 5 kiểm tra chạy tin dài đều PASS. Bộ hồi quy client WorldChat: **32 assertions PASS** (packet Java, UTF, policy, retention, badge isolation, replay/gap, pending/cooldown/session reset).

## Bug đã sửa / code đã xóa

- Thanh đặc cao 20 pixel vẽ trên skill/hồi máu: khôi phục paint gốc và thứ tự vẽ.
- Click thanh chiếm vùng bấm nút: loại toàn bộ vùng điều khiển trước khi mở chat.
- Khung căn giữa theo màn hình nhưng không xét nút: thay bằng tìm vùng trống; bỏ kích thước/vị trí căn giữa cũ và padding thanh cũ.
- Thiếu tap ngoài để đóng: bổ sung, tiêu thụ thao tác để không bấm xuyên.
- Dòng trống sau tin cuối làm khung thấp nhìn như rỗng: bỏ dòng đệm trong danh sách.
- Cache lịch sử chỉ xét chiều rộng: thêm chiều cao và tính trạng thái bám cuối từ chiều cao trước, tránh scroll sai sau đổi vùng hiển thị.
- Popup InfoMe hướng dẫn vẽ trên lịch sử: đổi thứ tự riêng khi mở WorldChatDialog.
- Safe area của Simulator trước đây bị bỏ qua vì chỉ chạy nhánh Android: cho Editor dùng safe area mà không gọi JNI.

Không còn đường vẽ thanh Stage 3 cũ hay hằng Height=20. Code ngoài phạm vi giữ nguyên, gồm các field GameScr đang có cảnh báo không dùng (`cmdTestLogin`, `maxSizeRow`, `clickMovingTimeOut`), các callback resize rỗng cũ và phần Main tự đặt độ phân giải Editor. Các phần dư cũ của MainMod không liên quan bản sửa này, không xóa.

Helper `Assets/Editor/WorldChatUiReview.cs` chỉ chạy sau khi chủ động bật **Tools → World chat → Start UI review**, không đưa vào player APK. Để tái lập, vào Play/login rồi chạy từ thư mục gốc client: `python docs/world-chat-ui-review/run_review.py`. Helper chỉ dùng fixture cục bộ cho lịch sử/ticker; lệnh login dùng luồng login bình thường của client và không ghi mật khẩu. Bộ lệnh JSON và script kiểm tra được lưu cùng ảnh.

## Test tay còn cần trên APK

| Case | Kỳ vọng |
|---|---|
| Chạm giữa thanh, chạm trên/dưới mép thanh | Chỉ vùng nhìn thấy mở chat |
| Chạm phần skill/hồi máu/tấn công giao thanh | Nút hoạt động, chat không mở |
| Joystick, đổi mục tiêu, R, chuông | Giữ vị trí và hành vi cũ; thanh không chiếm click |
| Mở lịch sử trên màn dài/tablet/tai thỏ | Khung/input/Gửi/X trong màn hình, không che hình nút quan trọng |
| X, tap ngoài, Android Back | Đóng, không kích hoạt nút gameplay nằm sau tap |
| Kéo 30 tin; tin mới khi đang cuộn | Cuộn được; giữ vị trí xem hoặc bám cuối như trước |
| Tin dài, tin ngắn mới thay tin dài | Tin dài chạy; tin mới reset vị trí chạy |
| Bật bàn phím Android, nhập/gửi/hủy phí | Kiểm tra bàn phím/IME thực tế; phí và server giữ logic Stage 3 |
| Đổi gesture/3-button navigation, xoay tai thỏ | Kiểm tra system bars trên máy thật; Simulator không thay thế Android WindowInsets/IME |

Màn quá nhỏ để có vùng trống tối thiểu 128×86 sẽ báo rõ thay vì che điều khiển. Không có trường hợp này trong 5 cấu hình đã kiểm tra. Màn 20:9 có ít dòng lịch sử để giữ nguyên toàn bộ nút; đây là giới hạn bố cục đã ghi trong ảnh.

## Build và workspace

APK **Succeeded, errors=0**, Unity batch mode thoát mã 0. File: `Build/world-chat-p3/NRO_WORLD_CHAT_P3.apk` (53,902,466 byte). SHA256: `7156C85A9D47E54BDC83A4C3AD609D4E750FDCF8D3C1F678C7B829D615DCCC61`. Log: `Logs/world-chat-ui-build.log`; metadata: [build-result.json](world-chat-ui-review/build-result.json). Không push/merge. Server giữ HEAD `a5d7a99`, không sửa source/config. Giữ nguyên toàn bộ 6 stash client, 1 stash server; không áp stash. Ba file bảo vệ được kiểm tra SHA256, không thay đổi so với trước tác vụ. Logo, Packages, ProjectSettings và Unity layout có thay đổi local từ trước, không đưa vào commit này.
