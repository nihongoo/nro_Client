# Giai đoạn 2a — Lời mời trong trung tâm thông báo

Client: `D:/project_Unity/client_Nro/Mod-nro-unity`.
Server: `D:/dowload/SRC NRO NEW 03/SRC`.
Cả hai đang ở `feat/invitation-p2a`, tạo trực tiếp từ `feat/notification-p1`, không clone/worktree mới.
**Phải merge và phát hành client + server cùng nhau. Giao thức -109 version 2 không hỗ trợ APK cũ. Không push, không merge trong đợt này.**

## 1. Commit và workspace

Kiểm tra ban đầu bằng local branches, remote-tracking branches và `git ls-remote --heads origin feat/invitation-p2a`: branch chưa tồn tại ở cả hai repo, chưa có commit 2a để tiếp tục.

Client:

| Commit | Nội dung |
|---|---|
| `dc6d44e` | Decode lời mời riêng tư, theo dõi thời hạn/trả lời; bỏ ClanObject cũ |
| `9951482` | Bỏ painter rỗng MainMod và using trùng |
| `e0bd11f` | Popup mới nhất, tab Lời mời có hành động; bỏ callback/opcode nhận lời mời cũ |
| `7e86a61` | Khôi phục popup/badge qua snapshot; giữ chuông dùng được ở viewport nhỏ; dọn dispatch cũ |
| `82255c4` | Test store/gói Java và entry build APK bằng Unity |

Server:

| Commit | Nội dung |
|---|---|
| `56a2d77` | Registry RAM, hết hạn/hủy, trả lời qua ID cho bốn loại |
| `a826cf0` | Khởi tạo tiền cược trước khi bắt đầu trận |
| `851b9e9` | Bỏ welcome bytes và import Functions không dùng |
| `5b43ff5` | Quyết toán tiền cược một lần; dọn state giao dịch cũ |
| `b1bf8c4` | Test lifecycle, concurrency, quyền nhận, gói tin và cược |
| `c94ffa1` | Bỏ import cùng package và override dispose chỉ gọi base |

Commit báo cáo được tạo sau các commit trên; xem `git log feat/notification-p1..HEAD` để có cả commit tài liệu.

Thay đổi cũ được giữ trong stash, không discard:

- Client: `On feat/notification-p1: pre-invitation-p2a client local server address and editor layout`.
- Server: `On feat/notification-p1: pre-invitation-p2a server IDE files`.
- Client layout phát sinh từ Unity sau build: `On feat/invitation-p2a: invitation-p2a editor-generated layout after build`.

Không sửa/commit `Config.properties`, `Packages/manifest.json`, `Packages/packages-lock.json`. Hai packages cài tay `com.coplaydev.unity-mcp`, `com.unity.device-simulator.devices` được giữ nguyên. Các file này đã dirty trước khi làm; kiểm tra SHA256 cuối để phân biệt với thay đổi của 2a. Địa chỉ server local cũ nằm trong stash client; cần áp lại cấu hình phù hợp khi test LAN.

## 2. Luồng mới và vị trí source

`File:line` phía server tính từ root server; phía client tính từ root client.

| Bước | Source | Hành vi |
|---|---|---|
| Gửi PK thách đấu | server `src/nro/models/matches/PVPService.java:95` | Chọn mức cược hợp lệ, kiểm tra vàng; đưa số cược vào từng lời mời |
| Gửi PK luyện tập | server `src/nro/models/matches/PVPService.java:129` | Đưa lời mời luyện tập vào registry |
| Gửi giao dịch | server `src/nro/models/services_func/TransactionService.java:171` | Đưa lời mời người chơi vào registry; bot shop giữ luồng riêng |
| Gửi vào bang | server `src/nro/models/services/ClanService.java:275` | Kiểm tra quyền người mời, nhiệm vụ, bang hiện tại, sao đen, chỗ trống |
| Tạo registry | server `src/nro/models/services/NotificationService.java:62` | ID tăng dần chung với P1; lưu tham chiếu Player của sender/recipient, zone gốc, loại, payload, expiresAt = now + 30000; đối chiếu Player với phiên online hiện tại khi xử lý |
| Gửi riêng tư | server `src/nro/models/services/NotificationService.java:215` | UPSERT loại Invitation chỉ gửi người nhận; snapshot cũng lọc đúng người nhận |
| Decode | client `Assets/Scripts/Notifications/NotificationProtocol.cs:63` | Đọc ID, loại, updatedAt, subtype, senderId, expiresAt, text |
| Store/popup | client `Assets/Scripts/Notifications/NotificationStore.cs:61`, `:104` | Giữ mọi lời mời còn sống; chỉ dựng popup của ID mới nhất; không đưa lời mời cũ trở lại popup sau khi xử lý lời mới |
| Popup nhanh | client `Assets/Scripts/PopUpYesNo.cs:14`, `:42`, `:65` | Vẫn là PopUpYesNo có text và countdown bên trái nút R; vẽ ✓/✗ bằng pixel để không phụ thuộc glyph Unicode của font sprite |
| Input/paint toàn cục | client `Assets/Scripts/GameCanvas.cs:594`, `:2293` | Update/input trước panel/combat; paint popup sau các panel/overlay thông thường |
| Tab Lời mời | client `Assets/Scripts/Notifications/NotificationCenter.cs:12`, `:191`, `:203` | Danh sách lời mời sống, thời gian còn lại, ✓/✗ từng dòng, cuộn; đang trả lời thì khóa thao tác dòng |
| Gửi câu trả lời | client `Assets/Scripts/Notifications/NotificationStore.cs:94`; `Assets/Scripts/Service.cs:2121` | Chặn bấm lặp trong client; gửi ID + accept/decline; sau 3 giây chưa có kết quả thì xin snapshot |
| Nhận câu trả lời | server `src/nro/models/server/Controller.java:336`; `src/nro/models/services/NotificationService.java:110` | Kiểm tra version/action, ID thuộc đúng người nhận, còn hạn, đúng phiên online, chưa đổi zone; consume trước khi chạy nghiệp vụ |
| Thực hiện PK | server `src/nro/models/matches/PVPService.java:151` | Kiểm tra lại cùng zone, còn sống, không ở trận/giao dịch khác, đủ vàng cược |
| Thực hiện giao dịch | server `src/nro/models/services_func/TransactionService.java:178` | Kiểm tra lại zone, chết, kích hoạt, bảo vệ tài khoản, tradeWVP, PvP, maintenance, trade hiện tại |
| Thực hiện vào bang | server `src/nro/models/services/ClanService.java:302` | Kiểm tra lại đúng bang được mời, quyền người mời, nhiệm vụ, sao đen, bang hiện tại, chỗ trống |
| Từ chối/hết hạn | server `src/nro/models/services/NotificationService.java:90`, `:95` | Xóa registry, gửi REMOVE người nhận, thông báo ngắn người gửi nếu còn online; worker quét mỗi 250 ms |
| Đổi map/logout | server `src/nro/models/map/service/ChangeMapService.java:779`; `NotificationService.java:104` | Hủy lời mời liên quan trước khi rời zone; quay lại zone cũ không khôi phục ID đã hủy; worker còn kiểm tra offline/phiên thay thế |

Badge chuông = tin Server chưa đọc + lời mời chưa đọc. Mở tab Server chỉ đánh dấu Server đã đọc; mở Lời mời chỉ đánh dấu lời mời đã đọc. Boss không góp badge ở chuông và không có badge ở tab Boss. Hết hạn/trả lời/remove cũng gỡ badge tương ứng. Snapshot không đánh dấu lại tin đã đọc thành chưa đọc và vẫn phát hiện lời mời mới chưa nhận qua UPSERT.

Gửi trùng cùng sender/recipient/type khi lời trước còn sống bị từ chối, không ghi đè số cược/bang hay gia hạn vô hạn. Tối đa 50 lời mời sống/người nhận. PK/giao dịch giữ các giới hạn bận của game; popup đang có không bị ẩn khi đánh quái, giao đấu hoặc mở panel. Chấp nhận khi trạng thái nghiệp vụ không còn phù hợp sẽ báo lỗi và kết thúc lời mời, không thực hiện hành động.

### Giao thức

Dùng lại opcode `-109`; nâng version từ 1 lên 2 vì có thêm record/action, không cần opcode mới.

- Server → client: header `byte version=2, byte action, long sequence, long serverTime`.
- Action: UPSERT=0, REMOVE=1, SNAPSHOT=2 như P1.
- Record chung: `long id, byte kind, long updatedAt`; kind Boss=0, Server=1, Invitation=2.
- Record Invitation: `byte subtype, int senderId, long expiresAt, UTF text`; subtype challenge=0, practice=1, trade=2, clan=3.
- REMOVE: `byte kind, long id`. SNAPSHOT: `unsigned short count, records`.
- Client → server: `byte version=2, byte action=3, long invitationId, byte answer` (0 từ chối, 1 chấp nhận). Không nhận senderId, số cược hoặc clanId do client tự khai cho thao tác chấp nhận.
- Xin snapshot: `byte version=2, byte action=2`.
- Sequence là toàn cục; gói riêng tư làm xuất hiện khoảng trống sequence ở người khác là bình thường, client chỉ kiểm tra thứ tự tăng.

## 3. Code cũ đã bỏ và phần còn lại

Đã bỏ/thay thế:

- Server: packet mời PK `-59`, packet mời bang `-57` với mã cố định `758435`, action invite `-86/0` gửi người nhận; action accept cũ của ba opcode không còn dispatch chấp nhận.
- Server: `acceptPVP`, `acceptPVP2`, `acceptJoinClan` cũ được thay bằng nghiệp vụ chỉ gọi sau registry; bỏ `IDMark.goldThachDau`, `playerTradeId` và các reset trong Trade.dispose.
- `IDMark.idPlayThachDau` đổi thành `challengeTargetId`, chỉ giữ người được chọn khi mở menu cược; không dùng để xác minh/chấp nhận lời mời nữa.
- Client: receiver lời mời `-59`, `-57`, `-86/0`; `GameScr.clanInvite`, `player_vs_player`, `giaodich`, `startYesNoPopUp` cũ; callback accept/close 2000, 2005, 2006, 2009, 11114, 12002, 12003.
- Client: xóa `ClanObject.cs` và `.meta` vì chỉ phục vụ callback mời bang đã bỏ; Service chỉ còn `sendClanInvitation(playerID)`, không còn phần ghi clanId/code để accept.
- Client: bỏ đường bắt touch chỉ ✓ trong GameScr và countdown cũ dựa độ dài text/log mỗi frame; popup/tab dùng cùng ID, thời hạn server và đường trả lời.
- `MainMod.smethod_15()` rỗng, bốn using trùng trong MainMod, `ServerNotify.gk`, import Functions không dùng: đã chết từ P1, không phải mới chết bởi 2a; đã xác nhận không có nơi dùng và dọn luôn phần liên quan thông báo.
- ThachDau: bỏ import cùng package và override dispose chỉ gọi base. `finish`/`update` rỗng vẫn phải giữ vì hợp đồng abstract/interface của PVP, không phải handler lời mời bị bỏ quên.

Ngoài phạm vi, ghi nhận nhưng không xóa:

- `MainMod.cs:183`, `:185`, `:219`: `pathfile`, `resetLoadMapFile`, `checkSkill` còn warning field chỉ gán/không dùng; liên quan bot/map legacy, không thuộc lời mời.
- `GameScr.cs:6781`: case 2001 chỉ đóng dialog, chưa tìm thấy caller GameScr tương ứng trong source hiện tại; không gộp các actionListener riêng của Login/Register có cùng số action.
- Luồng kết bạn, tin bang xin vào/xin đậu, trả thù và bot shop nằm ngoài registry 2a theo phạm vi đã chốt. Giao thức giao dịch sau khi đã mở trade (`-86/1,2,...`) vẫn cần cho gameplay, không phải handler nhận lời mời cũ còn chạy song song.

## 4. Bug và validation

Đã sửa:

- Lời mời cũ ghi đè người gửi/số cược qua IDMark; mỗi ID nay giữ payload riêng, chấp nhận lời cũ không lấy dữ liệu của lời mới.
- Chấp nhận bang bằng clanId/code cố định không chứng minh có lời mời sống: phải có registry ID thuộc đúng người nhận.
- Chấp nhận lặp/trễ/offline/phiên thay thế/đổi map: consume một lần dưới cùng lock, kiểm tra lifecycle trước nghiệp vụ.
- `PVP` gọi virtual `start()` từ constructor trước khi `ThachDau` gán số cược: thêm đường defer start cho ThachDau. Đồng thời bỏ trừ lần hai ở người thua và hoàn escrow của người thắng + lãi 80%. Ví dụ cược 1 triệu, hai người ban đầu 2 triệu: vào trận mỗi người còn 1 triệu; kết thúc thắng 2,8 triệu, thua 1 triệu. Giữ lãi ròng/thua ròng và phí 20% như thông báo cũ.
- Trade map dùng HashMap trong khi worker duyệt và các session mutate: đổi sang ConcurrentHashMap. Thêm guard null trade khi maintenance ở LOCK/ACCEPT và null zone khi gửi lời mời.
- Chọn mức cược ngoài mảng: kiểm tra index trước khi truy cập.
- Snapshot/resync không được làm mất badge lời mời mới hoặc bật lại badge đã đọc; session mới xóa popup cũ.
- Chuông bị popup phủ trên viewport nhỏ: dành vùng popup cố định từ đầu, không dịch chuông khi lời mời xuất hiện.
- Build config cũ trỏ scene `Assets/NROL.unity` không tồn tại: entry build 2a chỉ rõ `Assets/Scenes/SampleScene.unity` chứa Main. Không sửa scene list/settings gốc hoặc package.

Chưa sửa, ngoài phạm vi bị loại trừ: `ClanService.acceptAskJoinClan` đọc `cmg.playerId` trước null-check `cmg`; có thể lỗi nếu clan message đã mất. Đây là **xin vào bang**, không phải mời vào bang; chỉ ghi nhận để xử lý riêng.

### Kết quả build/test

- **Client C# compile: PASS** bằng MSBuild, không có lỗi compile mới.
- **Client Android APK: PASS**, Unity `2022.3.62f1`, IL2CPP, `Succeeded, errors=0`.
- APK: `D:/project_Unity/client_Nro/Mod-nro-unity/Build/invitation-p2a/NRO_INVITATION_P2A.apk`, 40.342.771 bytes; SHA256 `5F976AD087F158D9606481F589F4A2BF5909DD4773A00465A16443ADAEDE8618`.
- Log APK: `D:/project_Unity/client_Nro/Mod-nro-unity/Logs/invitation-p2a-final-build.log`. Hai lượt cũ từ editor đã fail do scene cũ/bản editor assembly chưa reload; lượt batch cuối dùng scene đúng và đã thành công.
- **Server: PASS**, `compile.bat --no-pause` compile toàn bộ 550 file Java. Runtime đã chọn: `build/server/releases/20261013-000457-024cdf1b79dc43528ab72131d0492f90/classes`.
- **Server entry point: PASS**, `run.bat --check` (Java dry-run, không start server/DB).
- Còn warning compiler có sẵn về field không dùng/API obsolete/unchecked Java; không coi warning là runtime đã được xác minh.
- Workspace cuối: client chỉ còn hai package files dirty từ trước; server chỉ còn `Config.properties` dirty từ trước. Source 2a và báo cáo đều commit, không include các file protected.

SHA256 protected files trước và sau làm/build giống nhau:

| File | SHA256 |
|---|---|
| Client `Packages/manifest.json` | `AC85B03C96B57121E4A910F13488D9CC743D3B34B8C3833A68F9B9B4F43741E1` |
| Client `Packages/packages-lock.json` | `33E418C1639338E404BB1B396030D523FBF7DDC6ABFE8F651FA587A5EEA6DCD2` |
| Server `Config.properties` | `2BFA840AB14ECC000D25FCF7D472578702E2812C778B7F4F934E1F798DEA6818` |

Test tự động:

- Server `python tests/invitations/run.py`: **42 assertions registry + 6 assertions cược PASS**. Compile NotificationService và PVP/ThachDau thật với fixture network/gameplay trong RAM; kiểm tra bốn loại, riêng tư, sai người nhận, reject, timeout worker, accept trễ/lặp, nhiều lời, offline, đổi zone, leave/return, phiên thay thế, 16 thread accept cùng ID, tiền cược khi chết/bỏ chạy.
- Client `Tests/Invitations/run.ps1 -JavaPacket <server>/build/invitation-protocol-fixture.bin`: **18 assertions PASS**. Compile store/protocol/models/myReader thật với fixture UI/session; kiểm tra gói nhị phân tạo bởi Java có tiếng Việt, badge, popup mới nhất, double submit, remove, resync, local expiry, gửi lỗi, reset session, packet thiếu.
- Test fixture không chạy DB/server/game và không chứng nhận UI cảm ứng hoặc nghiệp vụ clan/trade trên dữ liệu thật. Chưa test runtime trên điện thoại.

## 5. Ma trận test tay

Chuẩn bị A (người gửi), B (người nhận), C/D để gửi đồng thời. PK/giao dịch cùng zone; bang có quyền mời và đủ điều kiện nhiệm vụ. Mỗi case accept/reject nên chạy một lượt từ popup và một lượt từ dòng tab. Test PvE khi đang đánh quái; test thêm khi lời mời đã có rồi mới vào PvP để kiểm tra popup vẫn hiện và guard bận khi accept.

| Loại | Chấp nhận | Từ chối | Hết hạn | Nhiều lời cùng lúc | Đang combat/panel khác | Người gửi offline |
|---|---|---|---|---|---|---|
| PK thách đấu | Đúng đối thủ và đúng mức cược; trừ mỗi người một lần; thử chết và bỏ chạy, đối chiếu ví dụ tiền ở trên | Không mở trận/không trừ tiền; A nhận toast từ chối | Sau 30s popup/tab tự mất, A nhận hết hạn, bấm cũ không mở trận | A/C/D gửi mức cược khác nhau; popup chỉ mới nhất; nhận dòng cũ phải giữ đúng người/cược | Đánh quái hoặc mở túi/VIP: text/countdown/✓/✗ vẫn hiện; nhận khi đang bận PvP bị chặn | A thoát sau khi gửi: dòng/popup mất; B bấm ID cũ không mở trận/trừ tiền |
| PK luyện tập | Đúng đối thủ, mở luyện tập, không trừ vàng | Không mở trận; A nhận từ chối | 30s tự mất; A nhận hết hạn | Trộn luyện tập + thách đấu + giao dịch; nhận đúng subtype của dòng cũ | Popup vẫn hiện và nút bấm dùng được; không click xuyên vào skill/panel | A offline: xóa và không mở trận |
| Giao dịch | Mở tab trade hai phía đúng người; bấm lặp không tạo trade thứ hai | Không mở trade; A nhận từ chối | 30s tự mất; A nhận hết hạn | A/C/D mời B (tôn trọng cooldown cũ); chỉ lời mới nhất ở popup; nhận dòng cũ đúng người | Đánh quái/mở túi/VIP: popup vẫn hiện; ✓/✗ không kích hoạt panel phía sau | A offline: không mở trade, không giữ registry cũ |
| Mời vào bang | Vào đúng bang trong lời mời; thông tin bang/flag cập nhật; không thêm thành viên hai lần | B không vào bang; A nhận từ chối | 30s tự mất; A nhận hết hạn | Hai bang khác nhau gửi; nhận lời cũ vào đúng bang đó, các lời còn lại không cho vào bang thứ hai | Popup vẫn hiện khi đánh quái/mở panel, ✗ gửi server thật | A offline: lời mời hủy, B không vào bang bằng ID cũ |

Case bổ sung bắt buộc:

1. Một trong hai đổi zone/map rồi quay lại trước 30s: ID cũ vẫn bị hủy, kể cả mời bang.
2. B offline rồi đăng nhập lại; A đăng nhập phiên mới cùng account: không khôi phục lời mời của phiên cũ.
3. Giữ cùng ID và gửi 2 accept hoặc accept/decline đồng thời từ popup/tab: tối đa một tác dụng.
4. Gửi ID không tồn tại, ID của người khác, answer ngoài 0/1; gửi các accept cũ `-59`, `-57`, `-86/1`: không thực hiện nghiệp vụ.
5. Sau khi nhận lời: tiêu bớt vàng, chết, bật bảo vệ tài khoản, mở trade khác, người mời mất quyền bang/đổi bang, bang đầy hoặc B đã vào bang khác: accept phải bị chặn theo điều kiện tương ứng.
6. Chuông/tab Boss không có badge Boss; đọc Server không xóa badge Lời mời và ngược lại. Hết hạn giảm badge; resync không làm tin đã đọc thành mới.
7. Kích thước hẹp/rộng, zoom x1/x2, cuộn danh sách dài, dòng nhiều chữ, mép clip, nút R, popup khi đang mở trung tâm; xác minh nút không chồng vùng điều khiển.
8. Lời mới đến khi tab đang mở; xử lý lời mới nhất xong thì lời cũ chỉ ở tab. Gửi lại cùng sender/type trước khi hết hạn không tạo bản sao/gia hạn.
9. Test regression xin vào bang/xin đậu ở tin bang, kết bạn, bot shop và giao dịch sau khi mở: giữ luồng hiện có.
10. Restart server: registry RAM mất như đã chốt, session client mới lấy snapshot không có lời mời cũ.

## 6. Việc còn lại, rủi ro, quyết định

- Cần chạy ma trận runtime trên server test và thiết bị thật; các kết quả compile/test fixture không thay thế bước này.
- RAM registry không bền qua restart; toast từ chối/hết hạn chỉ gửi nếu người gửi còn online. Logout/đổi map hủy lời mời ngay, không chờ đủ 30s; mời bang cũng hủy nếu một phía đổi zone.
- Khi accept hợp lệ nhưng nghiệp vụ đã bận/thiếu điều kiện, lời mời vẫn kết thúc và người nhận được thông báo lỗi; cần gửi lời mới để thử lại.
- Không thay đổi quy tắc xin vào bang/xin đậu, kết bạn, bot hoặc các hệ thống khác để đồng bộ toàn bộ concurrency của server. Cần test với DB thật, đặc biệt bang đầy do luồng xin vào chạy đồng thời và giao dịch/tiền cập nhật từ hệ thống khác.
- Chưa có câu hỏi thiết kế bắt buộc còn mở trong phạm vi 2a. Giới hạn 50 lời mời/người và chặn bản sao cùng sender/type là giới hạn vận hành đã triển khai, có thể điều chỉnh nếu cần.
- **Client/server branch này phải merge cùng nhau sau test. Không push hoặc merge đã được thực hiện.**
