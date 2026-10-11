# 5. Bạn bè / bang hội / giao dịch / chat / PK

Danh mục phía client; hiệu lực quyền, giá và an toàn giao dịch trên server **chưa xác minh**. [Quy ước](README.md).

## Bản đồ

| Panel/popup | Mở vào | Thoát |
|---|---|---|
| Menu người chơi | Chọn nhân vật → playerMenu; action11111 (`GameScr.cs:2158`, `:6911`) → Panel type10 (`Panel.cs:1061`) | Chọn lệnh đặt isSelectPlayerMenu/hide (`Panel.cs:7347`); gửi lệnh theo lifecycle cần test runtime |
| Bạn bè | Tool → Tài khoản → friend(0), chờ (`Panel.cs:10336`) → type11 (`:1069`) | X/hide về Account (`Panel.cs:6917`) |
| Kẻ thù | Account → enemy(0) (`Panel.cs:10340`) → type16 (`:1078`) | X/hide về Account |
| Lịch sử chat/thông báo | Account → type8 (`Panel.cs:10344`, `:1169`), hoặc InfoMe | X/hide về Account |
| Bang hội | Main tab3 (`Panel.cs:1376`, `:1904`); có/không bang thay nội dung (`:8095`) | Tab khác/X; tìm kiếm/thành viên/tin nhắn là các chế độ cùng panel |
| Chọn biểu tượng bang | Tạo bang → onChatFromMe (`Panel.cs:9431`); TabClanIcon.show (`TabClanIcon.cs:100`) | Close → hide animation (`TabClanIcon.cs:274`, `:119`) |
| Giao dịch | Mời → giaodich(0); chấp nhận → giaodich(1) (`GameScr.cs:6931`, `:6940`) → Panel type13 (`Panel.cs:2094`) | X/bấm ngoài khi chưa accept gửi giaodich(3) hủy (`Panel.cs:6872`) |
| Lời mời giao dịch/bang/PK | PopUpYesNo (`GameScr.cs:6728`, `:6755`, `:2150`) | Đồng ý hoặc hết countdown; nút Từ chối không được paint/hit, xem S34 |
| Top/thách đấu | Panel type15 (`Panel.cs:1087`) | hide gửi sendThachDau(-1) (`Panel.cs:6876`) |
| Đổi cờ | Tool getFlag(0) (`Panel.cs:7683`) → type18 (`:1028`) | X về main; nút Trở lại trong submenu đóng socket (S38) |

## Bạn bè / chat

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| S01 | Mở menu người chơi | OK | Lấy menu/description server và show panel (`GameScr.cs:6911`) |
| S02 | Kết bạn từ người chơi | OK | friend(1,charId) (`GameScr.cs:6925`) |
| S03 | Danh sách bạn bè | OK | friend(0,-1), showWait (`Panel.cs:10336`) |
| S04 | Chat bạn bè | OK | 8001 mở form với tên, gửi chatPlayer theo currInfoItem (`Panel.cs:8978`, `:9484`) |
| S05 | Xóa bạn | OK | friend(2,charId) trực tiếp (`Panel.cs:8968`); không thấy xác nhận client tại handler |
| S06 | Đi đến bạn | OK | gotoPlayer(charId) (`Panel.cs:8973`); điều kiện/phí server chưa xác minh |
| S07 | Danh sách kẻ thù | OK | enemy(0,-1), showWait (`Panel.cs:10340`) |
| S08 | Trả thù | OK | enemy(1,charId), hideNow (`Panel.cs:9262`) |
| S09 | Xóa kẻ thù | OK | enemy(2,charId), showWait (`Panel.cs:9268`) |
| S10 | Chat từ lịch sử | OK | Menu 8001 và addLogMessage (`Panel.cs:8073`, `:8088`) |
| S11 | Kết bạn từ lịch sử | OK | 8003 → friend(1,charId) (`Panel.cs:8962`) |
| S12 | Chat thế giới | OK | Kiểm ≥5 ngọc, nhãn có phí, mở input (`Panel.cs:7697`); chatGlobal (`:9475`). Phí thực tế server chưa xác minh |
| S13 | Gửi chat thường có chữ “admin” | BROKEN | GameScr.onChatFromMe dùng Contains("admin") để bật menu VIP rồi xóa text (`GameScr.cs:6614`); câu chat bình thường chứa chuỗi này không được gửi (`:6626`) |
| S14 | Gửi chat | OK | Action8000 có throttle1 giây, callback parent và clear (`ChatTextField.cs:305`); nhánh native justReturn gọi trực tiếp không qua throttle này (`:248`) |
| S15 | Xóa/Đóng chat | UNCLEAR | Cùng nút8001: còn chữ thì xóa một ký tự qua tfChat.clear, hết chữ mới đóng (`ChatTextField.cs:320`, `TField.cs:301`); nhãn lúc gửi đổi sang Close (`ChatTextField.cs:315`) |

## Bang hội

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| S16 | Tìm bang | OK | searchClan mở input (`Panel.cs:7881`), callback searchClan(text) (`:9419`) |
| S17 | Tạo bang / nhập tên | OK | creatClan + getClan(1…), nhận tên mở icon picker (`Panel.cs:8115`, `:9431`) |
| S18 | Chọn icon tạo/đổi bang | OK | getClan(2,id,text) nếu chưa bang, getClan(4,…) nếu có (`TabClanIcon.cs:282`) |
| S19 | Đóng icon picker | OK | hide animation (`TabClanIcon.cs:274`) |
| S20 | Xin vào bang | OK | 4000 → clanMessage(2,null,clan.ID) (`Panel.cs:9157`); một đường có xác nhận (`:8132`), menu khác gửi thẳng (`:8141`) |
| S21 | Xem thành viên | OK | clanMember(clan.ID) + showWait (`Panel.cs:9166`) |
| S22 | Chat bang | OK | Input chatClan (`Panel.cs:7899`) → clanMessage(0,text,-1) (`:9425`) |
| S23 | Xin đậu trong bang | OK | clanMessage(1,null,-1) (`Panel.cs:8181`) |
| S24 | Cho đậu | OK | clanDonate(currMess.id) (`Panel.cs:8314`) |
| S25 | Duyệt xin vào bang | OK | joinClan(id,1) (`Panel.cs:8323`) |
| S26 | Từ chối xin vào bang | OK | joinClan(id,0) (`Panel.cs:8327`) |
| S27 | Nhường bang chủ | UNCLEAR | Menu5001 (`Panel.cs:8286`) → clanRemote(ID,0) trực tiếp (`:9190`), không xác nhận client; xác nhận server chưa xác minh |
| S28 | Bổ nhiệm phó bang | OK | 5002 → clanRemote(ID,1) (`Panel.cs:9194`) |
| S29 | Hạ quyền | OK | 5003 → clanRemote(ID,2) (`Panel.cs:9199`) |
| S30 | Đuổi thành viên | UNCLEAR | 5004 → clanRemote(ID,-1) trực tiếp (`Panel.cs:9204`); không xác nhận client, server chưa xác minh |
| S31 | Rời bang | UNCLEAR | leaveClan() trực tiếp (`Panel.cs:8206`); tác động/confirmation server chưa xác minh |
| S32 | Đổi khẩu hiệu | OK | Entry (`Panel.cs:8214`, `:8221`); callback cuối chặn trống, getClan(4,imgID,text), đóng (`Panel.cs:9618`, `:9624`). Phản hồi server chưa xác minh |
| S33 | Đồng ý lời mời bang/PK | OK | clanInvite(1…) (`GameScr.cs:7007`) hoặc player_vs_player(1…) (`:6826`, `:6849`) |
| S34 | Từ chối lời mời overlay PopUpYesNo | UNREACHABLE | cmdNo được lưu (`PopUpYesNo.cs:28`) nhưng paint chỉ cmdYes (`:50`), touch chỉ cmdYes (`GameScr.cs:3427`); không thấy cmdNo.performAction toàn source. Hết hạn chỉ bỏ popup (`PopUpYesNo.cs:86`), không gửi reject. Handler reject bang có thật (`GameScr.cs:7015`) |

## PK / giao dịch

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| S35 | Thách đấu/PK từ menu người chơi | OK | player_vs_player(0,3/4,charFocusId) (`GameScr.cs:6840`, `:6845`) |
| S36 | Thách đấu từ Top | OK | 9999 → sendThachDau(topInfo.pId) (`Panel.cs:8869`). Guard doFireTop dùng selected>=-1 (`:7195`), có nguy cơ index -1; đường kích hoạt thực tế chưa xác minh |
| S37 | Chọn/đổi cờ | OK | 10030 → getFlag(1,selected), hideNow (`Panel.cs:9387`) |
| S38 | “Trở lại” menu đổi cờ | BROKEN | Tạo với mResources.BACK/action10031 (`Panel.cs:8068`), handler chỉ Session_ME.close (`:9392`); thao tác quay lại làm ngắt kết nối |
| S39 | Mời giao dịch | OK | giaodich(0,targetId…) (`GameScr.cs:6931`) |
| S40 | Đồng ý mời giao dịch | OK | giaodich(1,targetId…) (`GameScr.cs:6940`) |
| S41 | Đưa item vào giao dịch | OK | 7000 chống trùng/đã khóa, hỏi số lượng nếu >1, giaodich(2…) (`Panel.cs:8915`) |
| S42 | Nhập số lượng giao dịch | OK | Parse catch, chặn <=0 và >quantity (`Panel.cs:9501`) |
| S43 | Gỡ item khỏi giao dịch | chưa xác minh | Handler7001 có remove + giaodich(4…) (`Panel.cs:8944`), nhưng các menu item đã đọc chỉ thêm Close/8000 (`:7290`); cần xác định caller/UI thật trước khi gọi OK hoặc UNREACHABLE |
| S44 | Nhập tiền giao dịch | BROKEN | Parse có catch và chặn >xu, thiếu num2<0 (`Panel.cs:9533`); vẫn set moneyGD/gửi số âm (`:9553`). Chưa xác minh server có từ chối, không suy ra exploit/mất tiền |
| S45 | Khóa giao dịch | OK | Toggle isLock, giaodich(5…), mở khóa gửi hủy (`Panel.cs:7254`) |
| S46 | Chấp nhận giao dịch cuối | OK | Chỉ khi cả hai khóa, mở Yes/No (`Panel.cs:7275`); Yes7002 giaodich(7…), isAccept=true, hide (`:8955`) |
| S47 | Hủy giao dịch bằng đóng panel | OK | hide khi type13 chưa accept gửi giaodich(3…) (`Panel.cs:6872`); không khẳng định server rollback an toàn nếu chưa đọc server |

## Điểm chung

| Chủ đề | Bằng chứng / giới hạn |
|---|---|
| Confirmation | Giao dịch cuối có xác nhận; nhường bang/đuổi/rời không thấy xác nhận phía client tại handler. Đề xuất thêm theo hậu quả thực, cần đọc server trước tránh hỏi hai lần |
| PopUpYesNo | Có Reject handler nhưng UI chỉ vẽ Yes; countdown ít nhất15 giây (`PopUpYesNo.cs:36`). Người chơi mobile phải chờ nếu không đồng ý; không phải dialog Yes/No dùng chung |
| Routing dựa chuỗi | Panel.onChatFromMe phân nhánh bằng chuỗi hiển thị, kể cả số khoảng trắng khác nhau (`Panel.cs:9413`, `:9573`, `:9590`); rủi ro khi sửa ngôn ngữ, chưa chứng minh lỗi locale hiện tại |
| Số bước | Bạn bè: Menu → Chức năng → Tài khoản → Bạn bè → dòng → Chat (`GameScr.cs:6670`, `Panel.cs:7730`, `:10336`, `:8047`); ứng viên giảm bước, chưa tự kết luận hai entry là thừa |
