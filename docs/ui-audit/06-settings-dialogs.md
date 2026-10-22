# 6. Cài đặt / xác nhận / popup chung

[Quy ước](README.md), [phụ lục menu mod](06b-mod-menus.md). OK chỉ xác nhận handler client; chưa test runtime/server.

## Bản đồ

| Màn/panel | Mở vào | Thoát |
|---|---|---|
| Chức năng | HUD Menu → tab chức năng; `Panel.cs:7658` | X/bấm ngoài → hide (`Panel.cs:6856`) |
| Tùy chọn, type19 | Chức năng → `Panel.cs:7733`, `:10090` | hide về main (`Panel.cs:6912`) |
| Tài khoản, type20 | Chức năng → `Panel.cs:7730`, `:10183` | hide về main; màn con về Account (`Panel.cs:6917`) |
| Thông tin, type23/24 | Tool → `Panel.cs:7852`; dòng → `:7128`, `:7828` | Type24 về danh sách; type23 về main (`Panel.cs:6895`) |
| Auto, type22 | Account → `Panel.cs:10380`, `:1285` | X/hide; nội dung/handler rỗng |
| MsgDlg OK/Yes-No/chờ | `GameCanvas.cs:2351`, `:2358`; `MsgDlg.cs:49` | Command của caller; endDlg (`GameCanvas.cs:2341`) |
| InputDlg | `InputDlg.cs:25` nhận nhãn, callback, kiểu input | Close/OK tùy callback; hide (`InputDlg.cs:61`) |
| ChatTextField | Chat/menu nhập liệu → startChat2 (`ChatTextField.cs:206`) | Gửi/xóa/close (`ChatTextField.cs:301`, `:278`) |
| InfoDlg chờ | showWait (`InfoDlg.cs:24`) | Hết số tick tự hide hoặc caller hide (`InfoDlg.cs:59`) |
| Menu chọn lệnh | startAt; điều khiển chung (`Menu.cs:194`) | Đóng nếu không disableClose/RongThanMenu (`Menu.cs:256`); chọn thực thi Command (`:525`) |
| Popup hội thoại/link | `ChatPopup.cs:712` | Close/Next tùy loại; link có tác dụng phụ thoát mobile |
| Panel thông tin/khóa inventory legacy | `Panel.cs:863`, `:1177` | Không thấy caller; không nhầm với lịch sử chat type8 có entry thật |
| ClientInput đa ô | Packet-125 (`Assets.src.f/Controller2.cs:775`) → `Assets.src.g/ClientInput.cs:85` | Close/OK về GameScr (`Assets.src.g/ClientInput.cs:177`) |

## Hành động

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| G01 | Aura | OK | `Panel.cs:10148` → toggle/lưu RMS (`SoundMn.cs:390`) |
| G02 | Aura2 | OK | `Panel.cs:10151` → toggle/lưu RMS (`SoundMn.cs:405`) |
| G03 | Âm thanh | OK | `Panel.cs:10154` → load/close sound + RMS (`SoundMn.cs:313`) |
| G04 | Đổi x1/x2 PC | BROKEN | Yes/No ở `Panel.cs:10160`; Yes170391 gọi Rms.clearAll rồi exit (`:8874`). Đổi kích thước đồng thời xóa dữ liệu local; lời hỏi tại callsite không nêu phạm vi xóa |
| G05 | Đồ họa thấp mobile | OK | CaseSizeScr toggle lowGraphic + báo restart (`SoundMn.cs:375`) |
| G06 | Analog mobile / chat server non-touch | OK | CaseAnalog chọn analog/chatVip theo isTouch, lưu RMS (`SoundMn.cs:360`, `:343`, `:329`) |
| G07 | Không đổi kích thước | OK | 4005 endDlg (`Panel.cs:9176`) |
| G08 | X panel | OK | 1003 → hide (`Panel.cs:9013`); input bị khóa khi InfoDlg/ghép chưa xong (`:1838`) |
| G09 | Softkey/bấm ngoài panel | OK | hide; cấp cha phụ thuộc type (`Panel.cs:1836`, `:1878`, `:6856`) |
| G10 | Dòng thông tin game | OK | Lưu dấu đã đọc rồi mở type24 (`Panel.cs:7128`) |
| G11 | Auto dùng ngọc (Account) | DEAD | Có entry `Panel.cs:10380`; paintAuto (`:4313`), updateKeyAuto (`:2021`), doFireAuto (`:7139`) đều rỗng |
| G12 | Xóa tài khoản | UNCLEAR | IsDelAcc bật mới thêm nút (`SoundMn.cs:222`); sendDelAcc trực tiếp (`Panel.cs:7664`), không hỏi tại client. Xác nhận/quy tắc server chưa xác minh |
| G13 | OK đóng thông báo | OK | 8882 bỏ currentDialog, hideInfo, dừng autoConnect (`GameCanvas.cs:2761`) |
| G14 | Hủy dialog chờ chung | UNCLEAR | CANCEL8882 (`GameCanvas.cs:2358`) chỉ chạy nhánh G13; không hủy request/socket. Auth có deadline riêng, không kết luận treo vô hạn |
| G15 | Yes/No chung | OK | Dialog.update dispatch Command giữa/trái/phải (`Dialog.cs:51`); nghiệp vụ ở callback caller |
| G16 | InputDlg gửi | OK | Callback caller cung cấp (`InputDlg.cs:25`); update TField/Dialog (`:48`). Validate tùy caller |
| G17 | InputDlg đóng | OK | Close8882/endDlg (`InputDlg.cs:30`, `:61`, `GameCanvas.cs:2761`) |
| G18 | Menu chọn dòng | OK | performSelect → Command.performAction (`Menu.cs:525`, `Command.cs:106`); Menu.perform rỗng không chứng minh mọi menu DEAD |
| G19 | Menu đóng | OK | Đóng khi được phép (`Menu.cs:256`); có menu cố ý disableClose |
| G20 | Link trong ChatPopup | BROKEN | platformRequest rồi notifyDestroyed nếu !Main.isPC (`ChatPopup.cs:712`, `:725`); notifyDestroyed gọi Main.exit (`GameMidlet.cs:78`). Mobile mở link đồng thời thoát game; trình duyệt thật chưa test |
| G21 | Close/Next ChatPopup | OK | 1001 reset chat/unlock; 8000 chuyển trang (`ChatPopup.cs:731`, `:746`) |
| G22 | Panel thông tin legacy | UNREACHABLE | setTypeInfomatioin (`Panel.cs:863`); tìm toàn Assets/Scripts không thấy caller |
| G23 | Panel khóa inventory legacy | UNREACHABLE | setTypeLockInventory (`Panel.cs:1177`); không thấy caller; không kết luận tính năng khóa tài khoản khác bị thiếu |
| G24 | Native keyboard nhập ≥40 ký tự | BROKEN | Ngoài loginInput, kb.text phải Length<40 mới setText (`TField.cs:744`), dù max mặc định500 (`:79`), ChatTextField max80 (`ChatTextField.cs:67`). Text game nhận ngừng đồng bộ ở ngưỡng40; cần test IME thật |
| G25 | ClientInput gửi | OK | Kiểm tra từng ô không rỗng rồi sendClientInput, về game (`Assets.src.g/ClientInput.cs:188`, `:196`). Validate mẫu server chưa xác minh |
| G26 | ClientInput đóng | OK | Action1 về GameScr, clear instance (`Assets.src.g/ClientInput.cs:179`) |
| G27 | ClientInput focus/nhập | OK | Keyboard chuyển focus (`Assets.src.g/ClientInput.cs:125`); touch dựa TField.update/setTextBox (`TField.cs:778`, `:788`). Không kết luận touch chết từ nhánh updateKey lồng trong phím lên/xuống |

## Vấn đề xuyên suốt

| Chủ đề | Bằng chứng / giới hạn |
|---|---|
| Touch | List24 đơn vị, tab W/7 (`Panel.cs:803`, `:826`); VIP checkbox18/hàng20 (`Mod.CuongLe/UnifiedAutoMenucs.cs:25`). Đơn vị game, chưa đo dp/ngón tay thật; không khẳng định mọi nút quá nhỏ |
| Back | Panel có cấp cha (`Panel.cs:6895`); close dialog còn tắt autoConnect (`GameCanvas.cs:2761`); Back đổi cờ đóng socket (S38). MyKeyMap không map Escape (`MyKeyMap.cs:10`, `:64`), Main chỉ gửi key khác0 (`Main.cs:337`); Android gesture Back chưa xác minh |
| Loading | pleasewait hạn5 giây (`MsgDlg.cs:26`); InfoDlg hạn theo tick, tự ẩn không báo retry (`InfoDlg.cs:24`, `:59`); startWaitDlg() chỉ đặt isLoadingMap (`GameCanvas.cs:2374`). Cần thử mất packet; chưa chứng minh request thực tế treo |
| Chuỗi/font | “Trờ ra” (`ChooseCharScr.cs:132`); “null” slot kỹ năng (`Mod.CuongLe/AutoSkill.cs:130`); Save/Reset VIP (`Mod.CuongLe/UnifiedAutoMenucs.cs:231`); slang Bò Mộng (`Mod.CuongLe/AutoboMong.cs:126`). Glyph/font APK chưa xác minh |
| Nhập liệu | Native numeric dùng NumberPad (`TField.cs:848`); xung đột '_' với nhiều ID xem C21/C22. Login/custom endpoint opt-in loginInput (`CustomServerDialog.cs:24`); IME thật chưa test |
| Xác nhận | Đổi TK không hỏi (L10); xóa local chủ động có hỏi (L23); Hủy tài nguyên lại xóa không hỏi (L22). Đuổi/rời bang, xóa account cần đọc server trước thêm hỏi |
| Dialog dài | setInfo tính h theo số dòng, không thấy cap/scroll (`MsgDlg.cs:49`); tràn trên màn nhỏ chưa xác minh |
| Safe area | Đã có Android Screen.safeArea + systemBars, cache0.5s, tránh inset hai lần (`ScaleGUI.cs:20`, `:42`, `:57`); touch dùng cùng viewport (`:84`). Không ghi thiếu hỗ trợ Android15 |
| Xoay/gập | Init WIDTH/HEIGHT (`ScaleGUI.cs:90`, `Main.cs:191`), scale x/y riêng (`ScaleGUI.cs:111`). Thay tỷ lệ có nguy cơ kéo giãn; reflow/hinge/đa cửa sổ chưa xác minh |
| Hiệu năng | new Command trong ServerScr.update (`ServerScr.cs:93`), splitFontArray trong dialog.paint (`CustomServerDialog.cs:53`), StringBuilder lọc password mỗi update (`TField.cs:727`). Allocation có bằng chứng, mức GC/FPS chưa đo |

Test tay: Android15 gesture Back từng tầng; native chat39/40/41 ký tự; dialog dài; lời mời Yes/No; xoay/gập/đa cửa sổ/thanh home; chạm sát mép và VIP Save/Reset; mất mạng lúc shop/ghép và response đến muộn. Hành động xóa/auto vứt chỉ thử bằng dữ liệu thử.
