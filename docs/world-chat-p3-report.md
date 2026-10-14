# Stage 3 chỉnh lại — form Chat thường, không lịch sử

**Cập nhật sau review:** xem [chat-input-followup.md](chat-input-followup.md). Lệnh hiện yêu cầu dấu /, đã sửa S13 theo quyết định mới; form chặn thao tác game, World gửi thành công tự đóng, ticker tạm dừng khi Chat/popup che. Phần dưới ghi trạng thái đợt chỉnh form ban đầu; các nhận xét S13 giữ nguyên và ticker chạy khi form che đã được thay thế bởi cập nhật này.

Ngày: 14/10/2026. Repo gốc client `D:/project_Unity/client_Nro/Mod-nro-unity`, server `D:/dowload/SRC NRO NEW 03/SRC`. Branch cả hai: `feat/world-chat-p3`. Nền trước đợt này: client `0934c02`, server `a5d7a99`.

## 1. Commit và phạm vi

| Repo | Commit | Nội dung |
|---|---|---|
| Client | `9151b31` | Form Chat có tab, xác nhận giá, phân luồng riêng; bỏ WorldChatDialog và lịch sử; ticker một lượt, queue giới hạn; dọn command/field chết |
| Client | `b974e64` | Xóa quyền sở hữu và bản nháp world khi đổi phiên, tránh rơi sang chat thường |
| Client | `0e37438` | Test form thật, consent, keyboard, S13 isolation, giá snapshot, ticker và hồi quy invitation |
| Server | `ba40e09` | Test snapshot policy-only và thứ tự broadcast live |
| Server | `10f35ea` | Snapshot chỉ còn phí/chính sách; bỏ lưu/phát lịch sử; giữ broadcast live và cơ chế thu/hoàn phí |

Commit test/tài liệu được ghi sau khi chạy build; xem `git log` để lấy toàn bộ commit của đợt này. Client/server phải được phát hành cùng nhau. Không hỗ trợ APK dùng payload snapshot world cũ.

Không tạo branch/worktree/clone, không áp/drop stash, không push/merge. Không sửa hoặc commit Config.properties, Packages, ProjectSettings, UserSettings, .idea và logo local có sẵn. Compile server vào output riêng, không thay con trỏ runtime đang chạy.

## 2. Luồng mới và điểm vào

1. **Nút R/chat thường hoặc gõ để mở chat:** các lời gọi `startChat` hiện có giữ nguyên. `Assets/Scripts/ChatTextField.cs:250` gọi `PrepareNormal` (`:57`), mặc định tab **Thường**. Khi OK/Enter, gọi listener cũ `parentScreen.onChatFromMe`; không popup giá. Tiêu đề vẫn `strChat + to` như form cũ, không thêm nhãn phí.
2. **Bấm thanh dưới đang có tin:** `Assets/Scripts/WorldChat/WorldChatHud.cs:15` → `ChatTextField.OpenWorldChat` (`ChatTextField.cs:48`) → cùng form nhưng chọn tab **Thế giới**. Menu Chat thế giới trong Panel cũng mở đường này. `SoundMn.CaseAnalog` ở chế độ không touch vốn có điểm vào WorldChatHud; giữ nguyên và nay mở tab Thế giới.
3. **Đổi tab:** `ChatTextField.cs:313` dùng hai vùng bấm cùng kích thước nút Command. Đổi tab không xóa nội dung. Tab chỉ xuất hiện ở form chat công khai của GameScr; ô nhập của mod và chat riêng giữ luồng/caption/listener hiện có.
4. **OK/Enter/bàn phím native:** cùng đi qua `ChatTextField.SendCurrent` (`:88`) → `WorldChatComposer.Submit` (`Assets/Scripts/WorldChat/WorldChatComposer.cs:15`). Tab Thế giới không gọi listener chat thường và không chạy lệnh mod/S13. Validate lỗi được hiện qua MsgDlg và trả lại form với bản nháp.
5. **Popup giá:** lấy `WorldChatStore.FeeLabel` từ snapshot server. Hai nút Đồng ý/Từ chối. Giá không ghi cứng ở code client; test dùng cả giá 17 gem + 123.456 vàng để kiểm chứng.
6. **Từ chối:** `ChatTextField.cs:403` → `composer.Decline` → quay lại form, giữ nội dung, không tạo request. Đóng bàn phím native bằng việc giải phóng keyboard handle, không xóa bản nháp.
7. **Đồng ý:** `ChatTextField.cs:389` → `WorldChatComposer.Agree` (`:27`) kiểm tra lại policy/cooldown/pending; nếu giá đã đổi thì yêu cầu xác nhận lại. Sau đó `Service.submitWorldChat` (`Assets/Scripts/Service.cs:2159`) gửi -109/v2/action 6, request ID và modified UTF text. Chỉ xóa bản nháp đã gửi sau RESULT thành công; lỗi giữ nội dung.
8. **Server:** `src/nro/models/server/Controller.java:342` → `WorldChatService.submit` (`src/nro/models/services/WorldChatService.java:90`). Vẫn kiểm tra phiên online, replay, text, 100 Unicode code point, ký tự điều khiển, bảo trì, cooldown 5 giây, đủ gem/vàng. Vẫn lock submit + inventory, serialize trước thu phí, áp dụng cả admin.
9. **Thu phí/broadcast:** `src/nro/models/services/WorldChatService.java:115`–`:163`: trừ đúng 10 gem và 10.000.000 vàng theo WorldChatPolicy, enqueue tới các phiên online gồm cả người gửi. Không queue nào nhận thì hoàn cả hai khoản. Có queue nhận thì cập nhật sequence/cooldown/tiền và RESULT. Không còn thêm vào history.
10. **Nhận và chạy chữ:** `WorldChatProtocol.Read` (`Assets/Scripts/WorldChat/WorldChatProtocol.cs:10`) → `WorldChatStore.Receive` → `WorldChatTicker.Enqueue` (`WorldChatTicker.cs:16`). `WorldChatHud.UpdateInput` gọi ticker update; bắt đầu tại mép phải, dịch trái 2 pixel logic/tick, hết chiều rộng text thì biến mất. Tin tiếp theo lấy từ queue; không đứng yên hay lặp lại.

`WorldChatTicker.WaitingLimit = 3` tại `Assets/Scripts/WorldChat/WorldChatTicker.cs:9`: tối đa **3 tin đang chờ**, cộng một tin đang chạy. Khi đầy, bỏ tin chờ cũ nhất; không bỏ tin đang chạy. Không có tin thì thanh/text không vẽ, không còn placeholder. Điểm vào menu vẫn dùng được khi không có tin trên thanh.

## 3. Protocol hai phía

Giữ opcode **-109**, version **2**; action notification/invitation **0/1/2/3** không đổi. Không cần opcode mới. Modified UTF tương thích Java giữ nguyên.

Header server → client của world vẫn: `byte version, byte action, int64 sequence, int64 serverTimeMs`.

| Action | Sau đợt chỉnh lại |
|---|---|
| **4 — snapshot policy** | Client xin chỉ gửi version/action. Server trả `int32 gemFee, int64 goldFee, uint16 maxCharacters, int32 cooldownMs, int64 nextAllowedAtMs`. Tổng packet payload 44 byte gồm header. **Bỏ** historyLimit, count và Entry[]. Login mới chỉ nhận policy, không phát lại tin cũ. |
| **5 — broadcast live** | Đổi tên constant từ APPEND/Append thành BROADCAST/Broadcast. Wire vẫn một Entry: `int64 id, int64 sentAtMs, int32 senderId, UTF senderName, UTF text`. Client đưa vào queue ticker, không lưu lịch sử. |
| **6 — submit** | Giữ `int64 requestId, UTF text`. Không gửi phí/số dư từ client. |
| **7 — result** | Giữ `int64 requestId, bool accepted, UTF message, int64 nextAllowedAtMs`. Client chỉ xử lý khi khớp pending. Không xin replay lịch sử. |

Không bỏ action nào; **đổi payload action 4** và ý nghĩa lưu trữ của action 5. Các tin trùng/cũ bị bỏ; live sequence có gap vẫn nhận tin mới vì không còn cơ chế replay/history. Snapshot không đụng queue chữ đang chờ/chạy.

Mất RESULT: sau 10 giây báo rằng tin có thể đã phát, không tự gửi lại, xin đồng bộ policy/cooldown. Không còn lịch sử để xác minh tin bị mất. Queue/bản nháp phiên trước được reset khi login.

## 4. Đã xóa / thay thế

- `WorldChatDialog.cs` và metadata; toàn bộ layout/cuộn/history/status riêng của dialog.
- HistoryBounds/tìm vùng trống/cache kích thước lịch sử trong WorldChatLayout. Giữ BarBounds và loại vùng bấm điều khiển.
- List 30 Entry, HistoryLimit, Revision lịch sử và logic retention ở store client; ArrayDeque/historyLimit/count/Entries trong snapshot server.
- Ticker chỉ giữ tin cuối, pause, offset lặp, lastWidth/latestText và chuỗi placeholder. Thay bằng queue 3 tin chờ và một lượt chạy theo cơ chế gốc `origin/develop` (`xChatVip = GameCanvas.w`, `xChatVip -= 2`, kết thúc ở `-currChatWidth`).
- Các ngoại lệ vẽ/input dành riêng cho WorldChatDialog ở GameScr/GameCanvas.
- `cmdChatTheGioi`, `cmdshowInfo`, `cmdLogOut`, `cmdDoiCo`: đã tìm mọi tham chiếu; chỉ còn khai báo, khởi tạo, setType và đặt vị trí, không có renderer/input/callback tương ứng. Đã xóa cả nhóm.
- `AutoChat.tempChatInboxContent`: chỉ có khai báo, đã xóa.
- `ChatTextField.isPublic`: chỉ có field và hai phép gán false, không có nơi đọc; đã xóa trong file đang sửa.
- Editor helper WorldChatUiReview, script test UI lịch sử, ảnh/matrix và báo cáo UI cũ chỉ phục vụ dialog đã bỏ. Có thể xem lại bằng Git ở HEAD trước đợt chỉnh này.
- Test history/retention/dialog cũ; thay bằng test policy-only, broadcast live/order, form thật, consent/cancel, queue và giới hạn.

**Còn lại, có dùng:** `Panel.isViewChatServer`, `ServerNotify.sendNotifyTab`, opcode 92 cho tin riêng/hệ thống. Không xóa. S13 `Contains("admin")` của GameScr chat thường giữ nguyên; đã so sánh toàn thân handler với `0934c02`. Các ô nhập mod/private không dùng tab World.

**Các mục dọn cũ đã không còn ở HEAD:** MainMod.smethod_15, using trùng trong MainMod, ServerNotify.gk, import Functions ở ServerNotify. Không phát sinh thay đổi các mục này trong đợt này.

**Ngoài phạm vi, chưa xóa:** các field/callback rỗng không liên quan world chat và cảnh báo cũ ở MainMod, Panel, GameScr; helper UTF toàn game vẫn dùng codec cũ, world chat dùng codec riêng. Không áp commit UI batch C.

## 5. Bug gặp và xử lý

- Callback OK, Enter và bàn phím trước đây gọi thẳng listener, có thể bypass nhánh world: gom routing và test actual ChatTextField.
- `startChat(IChatable, string)` trước đây không gán lại listener: giờ luôn gán đúng listener khi mở form, tránh dùng listener của lần nhập mod trước.
- Xóa text trong keyboard handle trước khi đóng có thể làm mất bản nháp khi trở lại: world popup đóng/giải phóng handle, giữ text của TField; test decline/native callback.
- RESULT đến muộn có thể xóa bản nháp khác có cùng nội dung: theo dõi quyền sở hữu bản nháp; đổi về Thường hoặc mở form mới không cho receipt world xóa text đó.
- Đổi phiên đăng nhập: reset bản nháp/ownership/mode world, tránh nội dung world sót lại ở tab Thường (ChatTextField.cs:41).
- Popup callback sau khi rời màn chơi: không mở lại form trên màn login/khác.
- Policy đổi giá sau popup: không gửi với giá mới chưa được đồng ý; giữ bản nháp và báo xác nhận lại.
- Test invitation thiếu dependency ticker sau đổi store: bổ sung source vào runner; hồi quy chạy lại đạt.

## 6. Build và test

- C# client: **236 source, PASS, 0 lỗi**, output/log `Build/world-chat-rework/`.
- Java server: **551 source, PASS, 0 lỗi**, release 17, output/log `build/world-chat-rework/`. Còn note unchecked/unsafe operations.
- Test **320 assertions PASS**: 181 world server + 49 world store/protocol/composer/ticker + 24 actual ChatTextField + 42 registry + 6 escrow + 18 invitation client. Không giữ số lượng test cũ bằng các assertion history đã vô hiệu.
- Cảnh báo C# liên quan reader cũ: CS0675 tại myReader.cs:106 và field không dùng trong test compile; không có lỗi compile WorldChat/ChatTextField.
- APK: **Succeeded, errors=0**. Artifact `Build/world-chat-p3/NRO_WORLD_CHAT_P3.apk` (53.873.259 byte), SHA-256 `c3180e39200fd7052fbd7356b82f64f7a3854d24571b559d52d26b63b11386aa`, kết quả `Build/world-chat-p3/result.txt`, log `Build/world-chat-rework/unity-apk.log`. Dùng menu Tools/World chat/Build Android sau Refresh, không đổi cấu hình project. Lần đầu Cancelled ở Processing Mapping File; lần build lại với bản sửa đổi phiên đã thành công.

## 7. Ma trận test tay

| Case | Kỳ vọng |
|---|---|
| R / gõ để mở Chat | Tiêu đề Chat như cũ; chọn Thường mặc định; không có nhãn phí |
| Chuyển Thường ↔ Thế giới khi có text | Text còn nguyên; tab hoạt động, OK/Đóng theo form chung |
| Thường → OK / Enter / keyboard done | Gửi theo listener cũ, không hỏi phí |
| Bấm thanh đang chạy / menu Chat thế giới | Mở cùng form, chọn Thế giới |
| Thế giới → OK → Từ chối | Popup đúng phí snapshot; trở lại form với nguyên nội dung; không gửi/trừ |
| Thế giới → OK → Đồng ý; bấm liên tục | Chỉ gửi một request, thu đúng một lần; bản nháp chỉ xóa sau thành công |
| Chỉ thiếu gem / chỉ thiếu vàng / thiếu cả hai | Báo rõ, không broadcast/trừ |
| Tin rỗng, toàn whitespace, 100/101 ký tự và emoji | Rỗng/quá dài bị từ chối; Unicode đếm code point; server kiểm tra lại |
| Gửi trước 5 giây / sau 5 giây / relog trong cooldown | Đúng cooldown, không bypass bằng relog |
| Tin ngắn và dài | Đều chạy từ phải sang trái đúng một lượt rồi biến mất; không đứng yên/lặp |
| Một tin chạy + bốn tin mới tới | Tin đang chạy giữ nguyên; ba tin chờ mới nhất chạy lần lượt, bỏ tin chờ cũ nhất |
| Hai client gửi đồng thời | Cùng thứ tự live, mỗi người thấy cả tin mình, mỗi tin được thu đúng một lần |
| Login sau khi đã có tin trước đó / restart | Chỉ policy; không hiện lại tin cũ |
| Android: nhập, chuyển tab, done, từ chối rồi chạm lại input | Không mất text/nhảy listener, bàn phím mở lại được, không gửi ngoài consent |
| Mất kết nối trước gửi / sau submit / mất RESULT | Báo lỗi hoặc kết quả chưa rõ; không tự gửi lại; không tự mở chat trên login |
| Chat thường chứa admin | S13 vẫn như cũ, dành riêng UI batch C |
| Chat world chứa admin, qua OK/Enter/native | Chỉ gửi world sau consent; không bật/tắt VIP hay chạy lệnh mod |
| Ô nhập mod và tin nhắn riêng | Không đổi thành form world; giữ callback hiện có |

## 8. Việc còn dở / giới hạn

Chưa test tích hợp trên hai thiết bị thật hoặc bàn phím Android thật trong đợt này; các test form dùng class sản phẩm với renderer/keyboard/transport fixture. Không dựng ảnh mô phỏng để thay kiểm tra thiết bị.

Phí được commit khi ít nhất một outgoing queue online nhận tin, không dựa trên ACK thiết bị. Không còn lịch sử nên không khôi phục tin trong lúc offline hoặc xác minh lại RESULT thất lạc. Ticker tiếp tục cập nhật theo game tick khi form/panel che nó, giống cơ chế dòng chữ gốc.

Không có câu hỏi cần chốt thêm để triển khai các mục đã yêu cầu. S13 chat thường và phần UI batch C giữ nguyên ngoài phạm vi.
