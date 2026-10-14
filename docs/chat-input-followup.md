# Sửa chat modal, tự đóng và lệnh có dấu /

Ngày 14/10/2026, branch feat/world-chat-p3, repo gốc client/server. Yêu cầu mới của người dùng thay quyết định trước đây giữ S13: lệnh phải có dấu /; sửa nhập tiếng Việt và /.

## Commit

- Client `f6ab60b`: chặn input lọt xuống game, đóng form sau RESULT thành công, giữ ticker khi bị form che, nhập Unicode và phân luồng slash command.
- Client `cba49cd`: nhập chuỗi Unicode thẳng vào text thay vì gửi qua alias mã phím cũ (ví dụ ký tự Ì trùng key code 204).
- Client `5918eb4`: test ký tự Unicode trùng alias phím cũ, paste có control/giới hạn và không cắt surrogate pair.
- Server `41cd0ca`: chỉ xử lý command khi có /, giữ kiểm tra quyền admin, bỏ special-case part khỏi broadcast chat, test prefix/quyền.

## Luồng và bằng chứng

- `Assets/Scripts/GameCanvas.cs:672`: chụp trạng thái modal trước xử lý form; xử lý form trước update màn chơi, không gọi GameScr.updateKey trong cả frame đóng/gửi. Nhân vật/mob/server vẫn update.
- `Assets/Scripts/ChatTextField.cs:49`: nhận input/tab/nút OK/Đóng rồi xóa pointer, key hold/pressed và ASCII của frame. `:77`: mở form cũng consume thao tác mở; `GameScr.cs:2678` có guard, `Mod.CuongLe/MainMod.cs:1362` chặn các nút mod khi đang nhập.
- `ChatTextField.cs:132`: RESULT thành công xóa đúng bản nháp đã gửi, đóng form, đóng keyboard và consume input. Không đóng/xóa bản nháp mới đã đổi. Lỗi/từ chối giữ draft và luồng popup hiện có.
- `Assets/Scripts/WorldChat/WorldChatHud.cs:27`: xử lý RESULT trước ticker update; tạm dừng ticker trong khi form/popup đang che. Sau đóng form tiếp tục đúng một lượt, không phát lại tin đã hoàn tất.
- `Assets/Scripts/Main.cs:346`: desktop dùng Event.character thay physical key map khi Chat mở; không phụ thuộc Input.anyKeyDown nên nhận cả ký tự do bộ gõ sinh ra. Hỗ trợ /, Unicode, Ctrl/Cmd+V, Backspace, caret trái/phải, Enter/Đóng; không gửi Enter khi IME còn composition.
- `Assets/Scripts/TField.cs:655`: hiển thị composition chưa chốt, chưa gửi nó lên server. `:483`: ô chat nhận mọi Backspace từ bộ gõ, không debounce theo tick. `:305`: backspace giữ đoạn phía sau caret và xóa trọn surrogate pair. `:749`: ô chat cập nhật bàn phím native cả tin dài hơn 39 ký tự. `:850` và `ipKeyboard.cs:18`: keyboard text dùng Default hỗ trợ ngôn ngữ; password/numeric giữ loại cũ.
- `Assets/Scripts/MyKeyMap.cs:60` và `GameCanvas.cs:1801`: bổ sung slash để dùng cả đường mở chat bằng phím.
- `Assets/Scripts/GameScr.cs:6561`: chỉ chat công khai tab Thường bắt đầu / mới đi vào local command; `/admin` khớp nguyên lệnh, không còn Contains(admin). `Mod.CuongLe/ModProCuongLe.cs:653` trả bool để lệnh local không bị broadcast tiếp. Tab World giữ composer riêng, / chỉ là nội dung. Chat riêng không chạy lệnh local.
- Server `src/nro/models/server/Command.java:246`: chat không / đi thẳng broadcast; tin có / qua dispatcher và bị consume, lệnh không hợp lệ/không đủ quyền có thông báo. `:258`: guard slash cả khi gọi trực tiếp check. `:283`: bỏ đúng một / trước xử lý admin/pet; quyền admin kiểm tra như trước. `/notice` giữ nguyên. `/part` chỉ cho admin; `Service.java` không còn đọc chữ part để reload dữ liệu.

## Thay đổi hành vi lệnh

- Tab Thường: `admin`, `m 12`, `part`, `follow` là nội dung chat. Lệnh tương ứng dùng `/admin`, `/m 12`, `/part`, `/follow`.
- Lệnh pet/đổi tên đệ cũng phải có / để bảo đảm nội dung chat không có / không thực thi command.
- `/notice add ...`, `/notice remove ...`, `/notice list` giữ cú pháp và quyền server.
- Tab Thế giới: cả `admin`, `/admin` và tiếng Việt đều là nội dung, vẫn hỏi phí snapshot.
- Không đổi opcode, payload, phí, cooldown, replay hay logic thu phí world chat trong đợt sửa này.

## Build và test

- Client C#: 236 source, PASS, 0 lỗi. Server Java: 551 source, PASS, 0 lỗi.
- 339 assertions PASS: world server 181; store/protocol/composer/ticker 49; form Chat thật với fixture render/transport 28; registry 42; escrow 6; invitation client 18; server Command thật 15.
- CommandPrefixTests dùng player constructor-free và callback test, không mở database/server. Cảnh báo Unsafe chỉ nằm trong fixture test, không có trong source sản phẩm. Còn warning reader CS0675 và unchecked Java cũ.
- APK: **Succeeded, errors=0**, bản cuối chứa cba49cd, build sau Refresh DLL lúc 12:56:47 và hoàn tất 12:59:26 ngày 14/10/2026. Artifact Build/world-chat-p3/NRO_WORLD_CHAT_P3.apk (53876913 byte), SHA-256 `1ad262cbb79210689cd78e4350e80b5d5a1616d672622a53bf64db23996bdafa`. Log Build/world-chat-rework/unity-apk-followup.log; result.txt ghi Succeeded.
- Unity IME composition trong GUI tự vẽ cần tự hiển thị chuỗi đang ghép: [Unity Input.compositionString](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Input-compositionString.html).

## Test tay

| Case | Kỳ vọng |
|---|---|
| Mở Chat rồi click/giữ/drag ở map, skill, tim, nút mod | Không tạo lệnh đi/đánh/skill/menu ngoài form |
| OK/Đóng/Enter/Escape | Thao tác đóng/gửi không lọt xuống map trong cùng frame |
| Gửi World đồng ý, nhận RESULT thành công | Form tự đóng, thấy chữ chạy; lỗi/từ chối giữ draft |
| Chờ hơn thời gian chạy của một tin khi form/popup mở | Tin không biến mất trong lúc bị form che; sau đóng tiếp tục |
| Gửi rồi lập tức sửa draft/chuyển tab trước RESULT | Receipt không xóa hoặc đóng bản nháp mới |
| PC gõ Tiếng Việt có dấu, /, Shift và Ctrl+V | Đúng ký tự, không biến thành phím điều khiển |
| Bộ gõ Telex/VNI: thay chữ nhanh, nhiều Backspace, caret giữa câu | Dấu đúng, đoạn sau caret được giữ |
| IME: composition đang ghép rồi Enter | Chốt composition trước, không gửi khi còn ghép |
| Android chọn bàn phím tiếng Việt, gõ >39 và tới 100 ký tự | Draft cập nhật đủ, chuyển tab/từ chối không mất text |
| Admin gửi admin, m 12, part ở tab Thường | Chỉ phát nội dung, không mở VIP/chuyển map/reload |
| Admin gửi /admin, /m 12; người thường gửi /m 12 hoặc /part | Admin đúng lệnh; người thường không bypass quyền server |
| Tab World gửi admin và /admin | Chỉ world chat có phí, không thực thi command |
| Pet: /follow, /protect, /ten con la TenMoi | Điều khiển theo cú pháp mới; không / chỉ là chat |

Chưa xác nhận tích hợp bộ gõ Windows/IME và bàn phím Android trên thiết bị thật; test form nhận Unicode qua fixture, không giả lập đầy đủ bộ gõ OS. Cần test APK/client cùng runtime server mới; Đã chạy compile.bat --no-pause thành công và publish runtime 20261014-125355-27b50d81f47b464b8d4f738f6da51824. Lần run.bat tiếp theo dùng đúng server mới; tiến trình server đang chạy vẫn cần dừng/mở lại. Không tự restart server.

## Đã xóa / còn lại

Đã xóa nhánh xử lý chat trùng/không tới được trong GameScr.updateKey và lời gọi Chat.update cũ (chỉ cập nhật modal một lần), nhánh cheat_ trùng ở GameScr, Contains(admin), callback local command void gây broadcast tiếp, special-case part trong Service.chat.

Giữ các luồng riêng/hệ thống, opcode 92, lời mời, thông báo và phí world chat. `Service.isMessageContainsThang` vẫn giữ cơ chế cũ ngoài phạm vi. UI batch C còn branch riêng; nếu merge sau cần giữ quyết định slash mới thay logic S13 cũ.

Không push/merge/stash; không sửa Config.properties, Packages, ProjectSettings, UserSettings, .idea hay logo local.
