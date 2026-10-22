# Tổng hợp UI/UX client

Snapshot `develop` tại `67a32b7`, nhánh `audit/ui-ux`, worktree riêng. Chỉ tạo báo cáo trong `docs/ui-audit/`; không sửa code/build/DB/push. [Danh mục báo cáo và quy ước](README.md).

## Phạm vi / bảng đếm

Đã lập bản đồ 16 subclass mScreen/Dialog trong [SCREEN_INDEX](SCREEN_INDEX.csv), các loại Panel/overlay và **329 mẫu hành động logic**. [CONTROL_INDEX](CONTROL_INDEX.csv) ghi 450 điểm tạo Command, không đồng nghĩa 450 nút: có lệnh nội bộ, bản sao động, callback và code legacy. Bảng dưới đếm dòng hành động đã mô tả, không khẳng định mọi constructor là một nút độc lập đã chứng nhận. Chức năng server, IME/bố cục thật, các vòng worker bot chưa kiểm hết được ghi riêng ở từng nhóm.

| Nhóm | OK | DEAD | DUPLICATE | BROKEN | UNCLEAR | UNREACHABLE | Chưa xác minh riêng | Tổng mẫu |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1. Login/server | 25 | 1 | 0 | 5 | 2 | 6 | 0 | 39 |
| 2. HUD | 24 | 1 | 0 | 2 | 0 | 4 | 0 | 31 |
| 3. Nhân vật/túi/kỹ năng | 30 | 1 | 0 | 6 | 3 | 0 | 0 | 40 |
| 4. NPC/shop | 39 | 0 | 0 | 2 | 2 | 0 | 0 | 43 |
| 5. Xã hội | 38 | 0 | 0 | 3 | 4 | 1 | 1 | 47 |
| 6. Cài đặt/popup + mod | 102 | 2 | 0 | 9 | 11 | 5 | 0 | 129 |
| **Tổng** | **258** | **5** | **0** | **27** | **22** | **16** | **1** | **329** |

OK chỉ xác nhận tác dụng của handler phía client đã đọc. Chưa có bằng chứng nút trùng **thừa** để xếp DUPLICATE; hai đường mở cùng panel có thể phục vụ ngữ cảnh khác nhau. S43 (gỡ item giao dịch) có handler nhưng caller UI chưa xác minh. Những mục khác đã phân loại vẫn có thể cần kiểm tra server/runtime như ghi tại dòng tương ứng.

## Top 15 vấn đề

Ưu tiên theo hậu quả có thể gây ra và đường dùng phổ biến trong giao diện; chưa có telemetry tần suất. Công sửa là ước lượng tĩnh, chưa bao gồm thay đổi hợp đồng server.

| # | Vấn đề / ảnh hưởng | Bằng chứng | Mức độ | Công |
|---|---|---|---|---|
| 1 | Hủy tải tài nguyên xóa toàn RMS: mất thông tin đăng nhập/IP local, không hỏi | `ServerListScreen.cs:813`, `:821`, `:827`; `Rms.cs:208` (L22) | Cao | Nhỏ |
| 2 | Đổi x1/x2 PC cũng clearAll rồi thoát; lời hỏi không nói xóa local | `Panel.cs:10160`, `:8874` (G04) | Cao | Nhỏ |
| 3 | Password vẫn lowercase/trim khi savePass; các đường lưu/gửi không thống nhất | `LoginScr.cs:660`, `:666`, `:596`, `:1148` (L08/L12). Server chấp nhận ra sao chưa xác minh | Cao | Vừa |
| 4 | Auto vứt: điều kiện bảo vệ ID455/194 dùng OR nên luôn đúng; worker gửi bước bỏ trực tiếp | `Panel.cs:7610`; `Mod.CuongLe/AutoVutDo.cs:254` (C18). Mất vật phẩm thực tế/server bảo vệ chưa xác minh | Cao | Nhỏ |
| 5 | Dùng item rương gán command lấy nguồn BAG_BODY/index túi, khác nguồn người chơi chọn | `Panel.cs:8798`, `:11050`, `:9034` (C11). Kết quả server chưa xác minh | Cao | Vừa |
| 6 | “Trở lại” menu đổi cờ chỉ đóng socket, gây mất kết nối | `Panel.cs:8068`, `:9392` (S38) | Cao | Nhỏ |
| 7 | Ký gửi số lượng int.Parse không catch, thiếu chặn ≤0 | `Panel.cs:9595`, `:9606` (N22/N23). Hậu quả exception cấp UI chưa test | Cao | Nhỏ |
| 8 | Native chat/input ≥40 ký tự không đồng bộ về text game, dù max80/500 | `TField.cs:744`, `:79`; `ChatTextField.cs:67` (G24) | TB | Vừa |
| 9 | Auto Skill cho chọn slot trống rồi dereference template | `Mod.CuongLe/AutoSkill.cs:130`, `:169`, `:230` (phụ lục6b) | TB | Nhỏ |
| 10 | Auto item/delay skill nhận0/âm, không có giới hạn tối thiểu; có thể gọi quá thường xuyên | `Mod.CuongLe/AutoItem.cs:101`, `:72`; `Mod.CuongLe/AutoSkill.cs:89` (C16/6b). Tần suất thực chưa đo | TB | Nhỏ |
| 11 | VIP Save/Reset có phần dưới bị bộ lọc vùng cha coi là chạm ngoài rồi đóng | `Mod.CuongLe/UnifiedAutoMenucs.cs:59`, `:243`, `:322`; `Mod.CuongLe/MainMod.cs:1416` (H25/H26) | TB | Nhỏ |
| 12 | Lời mời có cmdNo nhưng không vẽ/xử lý touch; muốn từ chối phải đợi hết hạn | `PopUpYesNo.cs:28`, `:50`, `:86`; `GameScr.cs:3427` (S34) | TB | Nhỏ |
| 13 | Native NumberPad nhưng hướng dẫn nhập nhiều ID bằng '_' | `Mod.CuongLe/AutoVutDo.cs:43`, `:53`, `:138`; `TField.cs:848` (C21/C22). Bàn phím cụ thể chưa test | TB | Nhỏ |
| 14 | Menu chết có lựa chọn110381 không được dispatch | `GameScr.cs:6722`, `:6815`; tìm toàn source không có handler ID110381 (H15) | TB | Nhỏ |
| 15 | Mở link ChatPopup trên mobile đồng thời gọi thoát game | `ChatPopup.cs:712`, `:725`; `GameMidlet.cs:78` (G20) | TB | Nhỏ |

Các lỗi khác trong báo cáo: tiền giao dịch âm thiếu guard (S44); giá cộng KI dùng HP (C34); auto train nhãn trạng thái đảo; chuyển mode nhặt lại tắt; Yardat khởi động thread trước toggle; Lưu Auto Skill không lưu; Auto dùng ngọc/nội tại có handler rỗng. Không suy ra exploit, crash toàn game hay mất dữ liệu server khi chưa xác minh.

## Batch sửa đề xuất

Các batch chạm Panel.cs phải làm tuần tự trên cùng nền đã merge; tránh mở nhánh sửa Panel song song.

| Thứ tự | Khu vực / mục tiêu | File chính | Cách hạn chế chồng file |
|---|---|---|---|
| A | Login/local: tách Hủy khỏi xóa RMS; thống nhất password sau đối chiếu server | ServerListScreen.cs, LoginScr.cs | Chưa đụng Panel/worker |
| B | An toàn thao tác Panel: x1/x2, Back đổi cờ, nguồn item rương, validate ký gửi/giao dịch, giá KI | Panel.cs | Gom toàn bộ sửa Panel thành một batch, commit nhỏ theo lỗi |
| C | HUD/popup: vùng chạm VIP, nút chết, từ chối lời mời, link mobile | GameScr.cs, PopUpYesNo.cs, ChatPopup.cs, Mod.CuongLe/UnifiedAutoMenucs.cs, MainMod.cs | Tách khỏi Panel; gom touch/dispatch dùng chung để review |
| D | Nhập liệu chung: native ≥40, Back/focus, các lựa chọn bàn phím | TField.cs, MyKeyMap.cs, Main.cs; AutoVutDo.cs cho '_' | Sau khi chốt quy tắc input; test mọi caller, không đổi mặc định tùy tiện |
| E | Mod: delay/range/null, Auto Skill Lưu, nhãn mode, Yardat race; làm rõ nguy cơ auto vứt/bán | Mod.CuongLe/AutoItem.cs, AutoSkill.cs, AutoTrain.cs, AutoPick.cs, Yardat.cs, AutoVutDo.cs | Mỗi menu một commit; thay đổi entry Panel nếu cần thì xếp sau B |
| F | Tinh chỉnh sau test thật: kích thước chạm, số bước, chuỗi/font, xoay/gập, allocation paint | ScaleGUI.cs và file khu vực đã đo | Chỉ sửa sau có số đo/ảnh tái hiện; không refactor UI toàn cục từ phỏng đoán |

## Những gì còn phải xác minh

| Việc | Lý do / điểm bắt đầu |
|---|---|
| Menu NPC, xóa tài khoản, giá/phí, xác nhận và giao dịch nguyên tử server | Client chỉ gửi request; audit này không đọc backend. `GameScr.cs:1492`, `Panel.cs:7664`, `:8955` |
| Gỡ item giao dịch S43 | Có7001 (`Panel.cs:8944`), các menu đã đọc chưa tìm được entry tương ứng |
| Bàn phím/touch mobile | Không chạy UI test; các vấn đề kích thước dùng đơn vị game, chưa quy thành dp. `TField.cs:825`, `Panel.cs:803` |
| Android15/tai thỏ/gập/Back | Đã có safe area; cần thử gesture/rotation/đa cửa sổ/hinge. `ScaleGUI.cs:20`, `:111`; `MyKeyMap.cs:64` |
| Loading và request đến muộn | Auth có deadline riêng; các thao tác khác dùng InfoDlg/countdown chung, chưa test mất packet. `LoginScr.cs:198`, `InfoDlg.cs:59` |
| Worker bot và hiệu năng | Audit chỉ đọc dispatch UI và một số đoạn trực tiếp chứng minh lỗi; chưa chạy bot/profile. `Mod.CuongLe/AutoItem.cs:72`, `ServerScr.cs:93` |

Không build hoặc UI test theo yêu cầu. Đã kiểm tra tất cả tham chiếu file:line đầy đủ có file thật và nằm trong phạm vi dòng của snapshot; các đoạn gây lỗi chính được đối chiếu handler. Báo cáo chưa commit, không push.
