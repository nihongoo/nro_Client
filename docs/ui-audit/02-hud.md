# 2. HUD trong game

Đọc `GameScr.updateKey`, touch handler, `actionPerform`, GamePad và overlay mod. `OK` chỉ là kết luận tĩnh phía client; xem [README](README.md).

## Bản đồ

| Thành phần | Mở/hiện | Đóng/ẩn |
|---|---|---|
| GameScr/HUD | `GameScr.cs:1215`; vòng paint `:4927` | Đổi tài khoản reset toàn game (`GameCanvas.cs:1021`) |
| HP/MP, tên/đối tượng | `GameScr.cs:5919`; chọn kiểu thanh theo map/VS | Tự cập nhật theo trạng thái; đây là chỉ báo, không tính mỗi thanh là nút |
| Thanh kỹ năng + hồi HP | `GameScr.cs:4291`, `:5740`; vị trí `:1985` | Guard `isNotPaintTouchControl` ẩn/khóa khi có dialog/menu/panel (`:6470`) |
| Joystick, đánh, đổi mục tiêu | `GameScr.cs:7084`; `GamePad.cs:88` | Thả tay reset keyHold (`GameScr.cs:4478`); chết thì không xử lý GamePad (`:7086`) |
| Chat HUD | Nút chat (`GameScr.cs:4317`) → ChatTextField | Gửi hoặc Xóa/Đóng; handler chung `ChatTextField.cs:301` |
| Thông báo InfoMe/Info | `InfoMe.cs:87`, `:331`; tự xếp thông báo | Hết delay ẩn (`InfoMe.cs:155`); bấm dẫn sang lịch sử (`:183`) |
| Menu khi chết | `GameScr.cs:2662`, `:6719` | Chọn lệnh hồi sinh/về làng hoặc đóng Menu chung |
| Overlay auto VIP | Icon hộp (`Mod.CuongLe/MainMod.cs:1399`) | Bấm icon hoặc bấm ngoài (`:1421`); không dùng Dialog/Panel chung |
| Bản đồ | Nút ở tab nhiệm vụ → Panel type 4 (`Panel.cs:2509`, `:8352`) | Hide/Back về main panel (`Panel.cs:6927`). Tìm toàn source chưa thấy đường vẽ/nhấn mini-map HUD riêng; `TileMap.imgMiniMap` chỉ là khai báo (`TileMap.cs:67`) |

## Hành động

| ID | Nút/hành động | Loại | Bằng chứng handler |
|---|---|---|---|
| H01 | Menu/ảnh nhân vật góc trái | OK | Dispatch cmdMenu (`GameScr.cs:4301`) → actMenu/setTypeMain/show (`:6877`, `:6670`) |
| H02 | Chat tại map | OK | Mở ChatTextField khi map online (`GameScr.cs:4317`, `:4324`); gửi text qua `:6594`, `:6628` |
| H03 | Joystick di chuyển/bay | OK | Tính góc, vùng kéo và set keyHold/keyPressed (`GamePad.cs:88`, `:149`); trải nghiệm đa chạm thật chưa xác minh |
| H04 | Đánh/tương tác | OK | Touch đặt phím hành động (`GameScr.cs:7090`), doFire quyết định waypoint/skill/target (`:3959`); không kết luận combat server đã chạy đúng |
| H05 | Đổi mục tiêu | OK | findNextFocusByKey (`GameScr.cs:7100`) |
| H06 | Ô kỹ năng | OK | Tính slot, chặn index vượt cuối, doSelectSkill (`GameScr.cs:4437`, `:4470`); doUseSkill `:4138` |
| H07 | Dùng đậu/hồi HP | OK | Nút set keyPressed[10] (`GameScr.cs:4344`); doUseHP có cooldown 10 giây và báo hết đậu (`:3879`) |
| H08 | Bỏ/nhận ngọc rồng theo trạng thái | OK | useItem(-1…) và reset cờ (`GameScr.cs:4357`) |
| H09 | Capsule cấp 4 | OK | Tìm item 194 trong túi rồi useItem (`GameScr.cs:4370`) |
| H10 | Capsule cấp 3 | OK | Tìm item 193 rồi useItem (`GameScr.cs:4399`) |
| H11 | Chạm đối tượng / đi tới | OK | Tách click/drag/double click (`GameScr.cs:3421`, `:3687`, `:3758`); cảm giác và độ ưu tiên khi nhiều đối tượng chồng nhau chưa xác minh |
| H12 | Xem lịch sử thông báo | OK | Đặt timeDelay rồi mở Panel message (`InfoMe.cs:150`, `:183`) |
| H13 | Trả lời người nhắn trong Info | OK | `Info.cs:325` mở chat_player; gửi theo playerID `GameScr.cs:6619` |
| H14 | Menu khi chết | OK | Tạo ba lựa chọn (`GameScr.cs:6719`) |
| H15 | Lựa chọn đầu menu chết, action 110381 | DEAD | Tạo ở `GameScr.cs:6722`; toàn `Assets/Scripts` chỉ có chỗ tạo, switch actionPerform (`:6815`) không xử lý ID này |
| H16 | Về làng khi chết | OK | returnTownFromDead (`GameScr.cs:7048`) |
| H17 | Hồi sinh tại chỗ | OK | wakeUpFromDead (`GameScr.cs:7058`); handler gửi ngay, không thấy hộp xác nhận ở nhánh này; chi phí thực tế chưa xác minh |
| H18 | Dừng Auto Map | OK | FinishXmap, tắt isGoBack, tiêu thụ click (`Mod.CuongLe/MainMod.cs:1374`) |
| H19 | Icon cài đặt mod | OK | ShowMenu, clear pointer (`Mod.CuongLe/MainMod.cs:1391`); menu con xem nhóm 6 |
| H20 | Icon mở/đóng VIP | OK | Toggle isShowMenuVIP và clear pointer (`Mod.CuongLe/MainMod.cs:1399`) |
| H21 | Tab Auto Item VIP | OK | Toggle isShowingAutoItem (`Mod.CuongLe/UnifiedAutoMenucs.cs:246`) |
| H22 | Tab Skill Train VIP | OK | Toggle isShowingAutoItem (`Mod.CuongLe/UnifiedAutoMenucs.cs:254`) |
| H23 | Checkbox Auto Item VIP | OK | Bật/tắt thread có kiểm tra tồn tại item/thông báo hết (`Mod.CuongLe/UnifiedAutoMenucs.cs:275`, `:124`); tắt thread khi đổi tài khoản chưa xác minh |
| H24 | Checkbox Skill Train VIP | OK | Toggle AutoFlag; riêng skill 19 Namek bật HealingPower thread (`Mod.CuongLe/UnifiedAutoMenucs.cs:291`); tác dụng từng skill cần test thật |
| H25 | Save cấu hình VIP | BROKEN | Handler lưu RMS/thông báo (`Mod.CuongLe/UnifiedAutoMenucs.cs:322`, `:350`) nhưng vùng cha tính header 15 (`:59`), paint/hit con dùng 22 (`:168`, `:243`). `MainMod.cs:1416` lọc vùng cha trước: phần dưới nút bị coi là ngoài và đóng overlay |
| H26 | Reset cấu hình VIP | BROKEN | Cùng lệch vùng chạm như H25 (`Mod.CuongLe/UnifiedAutoMenucs.cs:331`); Reset chỉ đổi flags RAM (`:428`), cần Save nếu muốn lưu |
| H27 | Bấm ngoài VIP | OK | Đóng và clear pointer (`Mod.CuongLe/MainMod.cs:1421`); khác panel thường quay lại cấp cha, xem nhóm 6 |
| H28 | Đổi cờ HUD legacy | UNREACHABLE | Chỉ khai báo/khởi tạo/đặt vị trí cmdDoiCo (`GameScr.cs:483`, `:1037`); tìm toàn source không thấy paint/hit/perform của command này |
| H29 | Logout HUD legacy | UNREACHABLE | Chỉ dựng cmdLogOut (`GameScr.cs:485`, `:1038`), không thấy dùng ngoài khởi tạo |
| H30 | Chat world HUD legacy | UNREACHABLE | cmdChatTheGioi (`GameScr.cs:487`, `:1039`), không thấy dùng ngoài khởi tạo; chat world thật ở Panel tool |
| H31 | InfoLog HUD legacy | UNREACHABLE | cmdshowInfo (`GameScr.cs:489`, `:1040`), không thấy dùng ngoài khởi tạo |

## Vấn đề chung

| Chủ đề | Kết quả có bằng chứng |
|---|---|
| Touch nhỏ | Chat/đổi mục tiêu/đậu dùng 34×34, đánh 40×40 đơn vị game (`GameScr.cs:4317`, `:4344`, `:7090`, `:7100`). VIP checkbox 18×18, Save/Reset 40×14 (`UnifiedAutoMenucs.cs:25`, `:318`). Kích thước mm/dp trên thiết bị **chưa xác minh**, không quy đổi trực tiếp pixel game thành dp |
| Co giãn VIP | Cố định 3 cột ×90, không clamp chiều rộng (`UnifiedAutoMenucs.cs:27`, `:57`); nếu GameCanvas.w <270 sẽ vượt màn. Ảnh hưởng thiết bị cụ thể chưa xác minh |
| Modal | HUD gốc có guard dialog/menu/panel (`GameScr.cs:6470`); mod updateTouch được gọi trước guard (`:4293`), nhưng các nút mod đọc đã clear pointer. Không kết luận bấm xuyên chỉ từ thứ tự gọi |
| Ngôn ngữ | “Auto Item”, “Skill Train”, “Save”, “Reset” hardcode (`UnifiedAutoMenucs.cs:184`, `:188`, `:231`, `:235`); thuật ngữ thiếu giải thích cho người mới |
| Hiệu năng | HUD dùng vòng paint game (`GameScr.cs:4927`); chưa profile drawcall/GC/FPS nên không gọi mọi redraw là lỗi |
