# Stage 3 — World chat ở thanh dưới

Ngày: 2026-10-13. Client và server đều dùng branch `feat/world-chat-p3`, tạo trực tiếp trong repo gốc từ `origin/develop` sau fetch. Không clone, không tạo worktree, không áp stash, không push/merge.

## 1. Kết quả và commit

World chat dùng riêng thanh vàng dưới cùng. Thanh giữ tin mới nhất, đứng yên nếu ngắn và chạy chữ nếu dài. Bấm thanh hoặc mục Chat thế giới trong menu để mở lịch sử và ô nhập. Người chơi xác nhận phí trước mỗi lần gửi; tin world không còn đi qua popup InfoMe ở cạnh nút chat/R.

| Repo | Commit | Nội dung |
|---|---|---|
| Client | c3191bb | Store/protocol, thanh dưới, lịch sử/nhập tin/xác nhận phí; thay và xóa luồng world cũ |
| Client | c77ecde | Test snapshot Java → C#, state client, regression lời mời và entry build APK |
| Client | 77ef933 | Xóa nốt discriminator màu world chat trong Info/InfoItem/Panel, chuẩn hóa metadata |
| Client | 47f0caa | Sửa codec modified UTF đúng Java, gồm ký tự ngoài BMP, kiểm tra byte hai chiều |
| Server | 86b2cc8 | Policy chung, lịch sử RAM, validate/thu phí/broadcast; thay hàng đợi cũ và tách tin bot/minigame |
| Server | 1fd0907 | 100 kiểm tra tự động về phí, replay, concurrency, cooldown, retention và protocol |
| Server | 1914cd7 | Fixture Java modified UTF có surrogate pair để kiểm tra codec client |

Commit báo cáo được tạo sau các commit trên; xem `git log origin/develop..HEAD` ở mỗi repo để có danh sách đầy đủ.

Nền client: `e9f1721`; nền server: `335d8a1`. Hai phía Stage 3 phải được merge/phát hành cùng nhau. APK cũ không có giao diện/protocol world chat mới; không giữ đường gửi -71 cũ để tránh bypass chính sách.

## 2. Luồng mới và file:line

Đường dẫn tính từ root của repo tương ứng.

| Bước | Vị trí | Hành vi |
|---|---|---|
| Policy server | server `src/nro/models/services/WorldChatPolicy.java:5` | Nguồn duy nhất: 10 gem thường + 10.000.000 vàng; 100 Unicode code point; cooldown 5 giây; giữ 30 tin |
| Snapshot khi vào game | server `src/nro/models/server/Controller.java:944` | Gửi policy, cooldown còn lại và lịch sử RAM trong sendInfo |
| Snapshot chủ động | server `src/nro/models/server/Controller.java:341`, `src/nro/models/services/WorldChatService.java:66` | Client có thể xin lại khi mở lần đầu, mất gói hoặc timeout |
| Decode/tách kênh | client `Assets/Scripts/Notifications/NotificationProtocol.cs:20`, `Assets/Scripts/WorldChat/WorldChatProtocol.cs:11` | Dispatch action world riêng, không đưa world vào NotificationStore/badge |
| Codec UTF world | client `Assets/Scripts/WorldChat/WorldChatProtocol.cs:60`, `:73`, `Assets/Scripts/Service.cs:2166` | Encode/decode modified UTF đúng Java; giữ nguyên surrogate pair, không sửa UTF helper toàn game |
| Lưu lịch sử/policy | client `Assets/Scripts/WorldChat/WorldChatStore.cs:44` | Thay snapshot nguyên khối, bỏ snapshot cũ, giữ thứ tự và retention server cung cấp |
| Thanh tin mới nhất | client `Assets/Scripts/WorldChat/WorldChatHud.cs:28`, `:51` | Chỉ chọn entry cuối cùng; reset chạy chữ khi có tin mới hoặc đổi chiều rộng |
| Hook HUD | client `Assets/Scripts/GameCanvas.cs:596`, `:2277` | Input thanh trước input gameplay; paint sau HUD game |
| Mở lịch sử | client `Assets/Scripts/WorldChat/WorldChatHud.cs:18`, `Assets/Scripts/Panel.cs:7699`, `:7750` | Mở dialog chứa lịch sử cuộn, phí, input, nút gửi/đóng |
| Layout và cuộn | client `Assets/Scripts/WorldChat/WorldChatDialog.cs:26`, `:98`, `:122` | Wrap tên/nội dung theo viewport, tự theo tin mới khi đang ở cuối, giữ vị trí nếu đang xem tin cũ |
| Validate/hiện phí | client `Assets/Scripts/WorldChat/WorldChatDialog.cs:62`, `:69`, `Assets/Scripts/WorldChat/WorldChatStore.cs:34`, `:88` | Lấy số phí từ snapshot; kiểm tra text, pending/cooldown và số dư cache; popup xác nhận trước khi tạo request |
| Gửi sau xác nhận | client `Assets/Scripts/WorldChat/WorldChatDialog.cs:77`, `Assets/Scripts/Service.cs:2159` | Chỉ callback xác nhận mới gửi request ID + text; hủy không gửi/trừ phí |
| Server nhận | server `src/nro/models/server/Controller.java:342` | Nhận -109/version 2/action 6; không nhận lời khai phí/số dư từ client |
| Kiểm tra lại | server `src/nro/models/services/WorldChatService.java:94` | Phiên online hiện tại, request ID, text, độ dài, ký tự điều khiển, bảo trì, cooldown, gem/vàng; admin đi cùng luồng |
| Thu phí/broadcast | server `src/nro/models/services/WorldChatService.java:115`, `:132`, `:137` | Lock toàn kênh + inventory, serialize trước thu phí, reserve đúng một lần, enqueue tới các phiên online |
| Xác nhận enqueue | server `src/nro/models/network/Session.java:105`, `src/nro/models/network/Sender.java:80` | API trả bool khi queue outgoing nhận gói, thay vì dựa vào API void có thể bỏ gói âm thầm |
| Commit hoặc hoàn phí | server `src/nro/models/services/WorldChatService.java:143` | Không queue nào nhận: hoàn cả gem/vàng, không thêm history/cooldown. Có queue nhận: commit ID/history, giữ 30 tin, cập nhật tiền và RESULT |
| Chặn replay | server `src/nro/models/network/MySession.java:31`, `src/nro/models/services/WorldChatService.java:97` | High-water request ID theo connection; ID đã xử lý không được chạy lại kể cả sau cooldown |
| Cooldown qua relog | server `src/nro/models/services/WorldChatService.java:22`, `:47`, `:151` | Deadline monotonic theo character ID; phiên mới vẫn bị giới hạn nếu tin trước chưa đủ 5 giây |
| Kết quả/timeout client | client `Assets/Scripts/WorldChat/WorldChatStore.cs:68`, `:75` | Chỉ nhận RESULT khớp pending; timeout 10 giây xin snapshot, không tự gửi lại gây trừ phí lặp |
| Reset khi login | client `Assets/Scripts/Service.cs:858`, `Assets/Scripts/WorldChat/WorldChatHud.cs:14` | Xóa policy/history/pending của phiên trước; bộ đếm request tiếp tục tăng |

## 3. Protocol

Dùng lại opcode **-109, version 2**, thêm action **4/5/6/7**. Không tạo opcode mới: kênh này đã có routing, snapshot và session initialization phù hợp. Các action notification **0/1/2/3** và payload P1/2a giữ nguyên; world có sequence/store riêng nên không ảnh hưởng badge chuông hoặc invitation.

Các số dùng Java DataOutputStream big-endian và reader tương ứng phía client. UTF là **Java modified UTF-8** của DataInputStream.readUTF/DataOutputStream.writeUTF. WorldChatProtocol.WriteText/ReadText phía client có codec riêng tương thích byte với Java, gồm surrogate pair; không dùng writer/reader UTF-8 thường của các luồng game cũ. Độ dài tin đếm Unicode code point ở cả hai phía, không phải số byte UTF hay số UTF-16 code unit. Tin được trim; tin toàn khoảng trắng, quá 100 code point hoặc có ký tự điều khiển/xuống dòng bị từ chối.

### Client → server

| Action | Payload sau opcode |
|---|---|
| 4: xin snapshot | byte version=2, byte action=4 |
| 6: gửi tin | byte version=2, byte action=6, int64 requestId dương tăng dần, UTF text |

### Server → client

Header chung world: `byte version=2, byte action, int64 sequence, int64 serverTimeMs`.

| Action | Payload sau header |
|---|---|
| 4: snapshot | int32 gemFee, int64 goldFee, uint16 maxCharacters, int32 cooldownMs, byte historyLimit, int64 nextAllowedAtMs của người nhận, uint16 count, Entry[count] |
| 5: append | Entry |
| 7: kết quả | int64 requestId, bool accepted, UTF message, int64 nextAllowedAtMs của người gửi |

Entry: `int64 id, int64 sentAtMs, int32 senderId, UTF senderName, UTF text`. ID bằng sequence của lần broadcast được commit. Snapshot xếp cũ → mới, tối đa 30. Không tăng sequence vì lỗi validate hoặc không enqueue được. Client phát hiện gap thì xin snapshot; decode snapshot xong mới thay store.

## 4. Thu phí và concurrency

- Server giữ một lock cho submit/snapshot/history. Các tin gửi đồng thời được tuần tự hóa; lock inventory bao quanh kiểm tra và trừ/hoàn phí. Không có miễn phí hoặc bypass cooldown dành cho admin.
- Serialize toàn bộ packet trước khi trừ tiền. Reserve 10 gem và 10.000.000 vàng; nếu không queue online nào nhận thì hoàn nguyên bằng cộng lại đúng hai khoản. Chưa thành công không cập nhật lịch sử/cooldown.
- Một khi ít nhất một outgoing queue online nhận gói, broadcast được commit; lỗi ở một recipient không khiến tin đã phát bị thu/hoàn lần hai. Tiếp tục thử mọi recipient còn lại.
- Broadcast ở đây là enqueue trên server. TCP có thể ngắt sau enqueue; không có cam kết mọi thiết bị đã đọc/hiển thị tin và không dùng client ACK để quyết định phí.
- Cooldown dùng nanoTime, không lệ thuộc việc chỉnh đồng hồ hệ thống. Lịch sử/cooldown đều trong RAM; restart làm mất chúng như yêu cầu.
- Gem là `inventory.gem`/`Char.luong`, không cộng ruby/ngọc khóa để giả đủ tiền. Báo riêng thiếu gem, thiếu vàng hoặc thiếu cả hai.

## 5. Đã xóa/thay thế

Server:

- Xóa ChatGlobalService cũ: waitingChat/listChatting, worker chạy mỗi giây, API chat1/chatVip/ThôngBáo*, overload SystemColor, luật sức mạnh/30 giây và logic phí ngọc cũ.
- Bỏ opcode gửi -71 và constant CHAT_VIP=93 không còn dùng.
- Bỏ field lastTimeChatGlobal cũ trong IDMark.
- Bỏ broadcast quảng cáo shop bot; giữ chat bot cục bộ trong map và giao dịch bot.
- Thông báo thắng minigame chuyển sang NotificationService/category minigame ở tab Server, không giả thành tin world của người chơi và không tự thu phí người thắng.
- Bỏ import/comment callback ChatGlobalService chết trong Mob, NangCapVatPham, PhaLeHoaTrangBi, UseItem.

Client:

- Bỏ Service.chatGlobal/-71, callback nhập world chat cũ trong Panel, kiểm tra/label 5 ngọc trong T1/T2/T3/mResources.
- Bỏ vChatVip, ticker queue cũ, handler opcode 93, class ChatVip không dùng, preference/toggle ẩn ticker serverchat cũ.
- Bỏ đường world chat opcode 92 → InfoMe và cờ phân biệt màu isChatServer trong Info/InfoItem/Panel. Opcode 92 còn phục vụ tin nhắn riêng.
- Bỏ hiệu ứng FireWorkEff và cờ isFireWorks chỉ được ticker quảng cáo cũ kích hoạt; giữ hiệu ứng/nền Rồng Thần.
- Bỏ AutoChat world và spam world, menu/callback/RMS load-save liên quan. Giữ auto chat cục bộ và inbox; world mới chỉ gửi qua xác nhận phí.
- Menu Chat thế giới cũ chuyển sang dialog lịch sử mới; không giữ hai đường gửi song song.

### Còn lại, ngoài phạm vi

- `Panel.isViewChatServer` tên cũ vẫn là toggle hiện tin nhắn riêng trong Info/Panel; có hành vi đang dùng, không phải đường world chat mới.
- `AutoChat.tempChatInboxContent` chưa dùng và các phần auto inbox/local không thuộc world chat; chỉ liệt kê.
- GameScr.onChatFromMe vẫn kiểm tra chuỗi chứa admin cho chat cục bộ/VIP; commit sửa exact /admin ở branch UI batch C vẫn chờ quyết định. WorldChatDialog không gọi đường này.
- Không áp các commit UI batch C, không đổi worktree/branch audit hoặc các stash còn giữ từ đợt trước.
- myReader/myWriter UTF của các luồng cũ ngoài world chat vẫn dùng UTF-8 thường, có thể không tương thích ký tự ngoài BMP với Java writeUTF/readUTF. Đợt này sửa codec riêng cho world, chưa thay protocol toàn bộ game.

## 6. Bug đã sửa

1. Phí cũ kiểm tra >=5 ngọc nhưng chỉ trừ 1, label 5 và có overload bỏ phí: thay bằng policy chung + server snapshot, áp dụng admin.
2. Cooldown cũ 30 giây nhưng báo 5 giây: thay bằng 5 giây monotonic và truyền deadline về client.
3. Hàng đợi world cũ dùng ArrayList/LinkedList không khóa giữa worker/người gửi, có thể race và thu tiền trước khi tin được phát: bỏ worker, commit enqueue/tiền/history dưới lock, test gửi 64 request đồng thời.
4. Tin bot/minigame chen vào world và có thể bị coi là người chơi gửi: tách khỏi kênh world.
5. Tin quá dài bị cắt âm thầm và có thể cắt đôi surrogate: từ chối quá giới hạn, đếm code point nhất quán.
6. Gói tin nhắn riêng opcode 92 gọi endDlg và có thể đóng khung world/confirmation đang mở: bỏ thao tác đóng dialog khi nhận tin riêng.
7. AutoChat.onChatFromMe gọi text.Equals trước kiểm tra null: sửa kiểm tra null an toàn trong file đang dọn.
8. Java modified UTF và UTF-8 thường của client khác nhau với ký tự ngoài BMP: thêm codec world riêng, fixture đọc emoji và kiểm tra bytes client writer giống hệt DataOutputStream.writeUTF của Java.

## 7. Build và test tự động

- Compile đầy đủ server: **551 file Java**, thành công bằng `scripts/build-server.ps1`. Không khởi động server trong đợt này. Release build: `20261013-125500-4f59af3933784c838dbb6080b93b3ff5`.
- World server: **100 assertions** qua, chạy production WorldChatService/Policy với session/clock fixture; gồm admin, thiếu từng loại tiền/cả hai, empty/Unicode/control, replay, offline/replacement, bảo trì, relog, refund/partial queue failure, 64 request đồng thời, 30 tin và Java wire snapshot.
- World client: **32 assertions** qua, đọc packet Java thật; policy, UTF tiếng Việt/emoji, kiểm tra byte writer giống Java, history/order/latest, malformed snapshot, badge isolation, stale/gap/replay, pending/result/cooldown/timeout, session reset.
- Regression P2a: **42 registry + 6 escrow + 18 client assertions**, tất cả qua.
- APK cuối **Unity 2022.3.62f1 IL2CPP: Succeeded, errors=0**, gồm cleanup Info và codec modified UTF. Log: `Logs/world-chat-p3-utf-build.log:1181`.
- APK: `D:/project_Unity/client_Nro/Mod-nro-unity/Build/world-chat-p3/NRO_WORLD_CHAT_P3.apk`, 53.863.981 bytes, source code client `47f0caa`.
- SHA256 APK: `CD1DA9BE2E4EA559CEED35FC5F10F293BAD2DA07762DF4717E67C51E69CE5A0F`.
- Tổng cộng **198 assertions** qua: world 100 server + 32 client; regression 42 registry + 6 escrow + 18 client.
- Chưa xác minh trực quan/thao tác trên thiết bị thật; ma trận test tay bên dưới vẫn cần chạy.

Lệnh:

```powershell
# Server
python tests/world_chat/run.py
.\scripts\build-server.ps1
# Client, dùng fixture Java vừa sinh
.\Tests\WorldChat\run.ps1 -JavaPacket 'D:\dowload\SRC NRO NEW 03\SRC\build\world-chat-snapshot.bin'
.\Tests\Invitations\run.ps1 -JavaPacket 'D:\dowload\SRC NRO NEW 03\SRC\build\invitation-protocol-fixture.bin'
# Unity CLI: -executeMethod InvitationBuild.BuildWorldChatAndroid
```

Test server cũ dùng thư mục temp hệ thống bị sandbox Windows từ chối; đã chạy lại với temp nằm dưới build/test-workspaces. Không đổi fixture/logic kiểm tra cũ.

## 8. Ma trận test tay

| Case | Kết quả mong đợi |
|---|---|
| Vào game chưa có tin | Thanh dưới có gợi ý mở chat; dialog nhận policy và lịch sử rỗng |
| Có nhiều tin trước khi login | Snapshot có tối đa 30 tin gần nhất đúng thứ tự; thanh chỉ hiện tin cuối |
| Tin ngắn | Chữ đứng yên, không chạy khỏi thanh |
| Tin dài/tên dài, màn hình ngang/dọc | Thanh chạy chữ, dialog wrap và cuộn; input/nút không đè lịch sử |
| Tin mới trong lúc thanh đang chạy tin cũ | Chuyển ngay sang tin mới, reset vị trí chạy; không xếp hàng phát tin cũ |
| Bấm thanh/menu world | Mở cùng dialog lịch sử và input; không mở InfoMe world cạnh R |
| Đủ tiền, bấm Gửi | Hiện phí đúng 10 gem + 10.000.000 vàng; chưa xác nhận chưa gửi/trừ |
| Hủy popup phí | Không gửi, không trừ; giữ nội dung đang nhập |
| Xác nhận phí | Hai client nhận cùng một tin; trừ đúng một lần, input chỉ xóa sau kết quả thành công |
| Admin gửi | Cùng phí/cooldown, không miễn phí |
| Thiếu gem / thiếu vàng / thiếu cả hai | Báo đúng loại thiếu, không phát tin/không trừ gì |
| Chỉ đủ ruby/ngọc khóa nhưng thiếu gem thường | Không giả đủ gem; báo thiếu gem |
| Bấm Gửi liên tục/Enter + touch | Client pending chặn lặp; server không thu/broadcast hai lần |
| Gửi lại trong 5 giây / đúng sau 5 giây | Bị từ chối / được gửi, phí chỉ trừ lần thành công |
| Disconnect/relog trong 5 giây | Cooldown theo character còn hiệu lực |
| Tin rỗng/toàn khoảng trắng | Không gửi/trừ |
| 100 ký tự / 101 ký tự, gồm Unicode | 100 được gửi; 101 bị từ chối, không cắt âm thầm |
| Xuống dòng/ký tự điều khiển qua packet tự tạo | Server từ chối, không trừ |
| Gửi request ID cũ sau cooldown | Không broadcast/trừ lại |
| Hai người gửi đồng thời | Cùng thứ tự trên hai client, mỗi người bị trừ đúng phí của tin đã phát |
| Tiền thay đổi sau popup xác nhận | Server kiểm tra lại số dư thật; thiếu thì từ chối không trừ |
| Recipient ngắt giữa lúc gửi | Các recipient còn online vẫn nhận; không thu/hoàn lặp |
| Mọi outgoing queue không nhận | Không history/cooldown, hoàn đủ cả hai khoản (có test tự động) |
| Mất RESULT | Sau 10 giây client xin snapshot; không tự gửi lại/trừ thêm; kiểm tra lịch sử trước khi gửi lại |
| Cuộn lên xem tin cũ rồi có tin mới | Giữ vị trí xem cũ; đang ở cuối thì theo tin mới |
| Có tin nhắn riêng khi đang nhập/xác nhận world | Không đóng khung world/confirmation; private chat vẫn hoạt động |
| Bot shop/minigame/welcome hoạt động | Không chen vào thanh world; minigame/welcome ở Server tab, bot map giữ luồng cục bộ |
| Restart server | Lịch sử RAM trống; phiên mới nhận snapshot mới, không lẫn dữ liệu phiên trước |
| P1/2a: boss/server/invitation/combat | Badge và lời mời vẫn đúng; world không tăng badge chuông |

## 9. Workspace và việc còn lại

- Giữ thay đổi local có sẵn: logo App.png, hai file Packages, ProjectSettings.asset, layout Unity; server Config.properties và .idea/. Không đưa vào commit Stage 3.
- Không sửa ba file được bảo vệ, không áp/drop stash nào. Branch Đổi IP và UI batch C giữ nguyên.
- Kiểm tra cuối xác nhận SHA256 ba file được bảo vệ vẫn khớp bản trước khi làm; toàn bộ 6 stash client và 1 stash server giữ nguyên hash/thứ tự.
- Không push/merge. Cần test tay bằng client/server cùng branch trước khi phát hành.
