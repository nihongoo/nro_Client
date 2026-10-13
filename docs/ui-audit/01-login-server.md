# 1. Login / chọn server / menu ngoài game

Quy ước và giới hạn xác minh: [README](README.md). Mỗi dòng mã `L…` là một hành động được đếm; các biến thể dùng cùng handler ghi chung một dòng.

## Bản đồ màn hình

| Màn hình | Nơi định nghĩa / mở vào | Thoát ra |
|---|---|---|
| Splash | `SplashScr.cs:23`; `GameMidlet.cs:50` khởi tạo | Tự sang chọn server (`SplashScr.cs:70`) hoặc màn Chơi (`SplashScr.cs:85`); sau 150 tick gọi xử lý mất kết nối nếu chưa nối (`SplashScr.cs:79`) |
| Chọn server NRO | `ServerScr.cs:33`; Splash hoặc nút Server (`ServerListScreen.cs:989`) | Chọn NRO → màn Chơi (`ServerScr.cs:189`); Nhập IP → dialog (`ServerScr.cs:180`) |
| Màn Chơi TK X / Chơi mới | `ServerListScreen.cs:725`; chọn server (`ServerScr.cs:194`) | Chơi → Login và gửi auth; Đổi TK → form nhập; Server → ServerScr (`ServerListScreen.cs:848`, `:904`, `:989`) |
| Nhập IP server | `CustomServerDialog.cs:11`; nút ở ServerScr | Lưu/mặc định đóng rồi reconnect; Hủy đóng và phục hồi cờ auto-connect (`CustomServerDialog.cs:80`, `:90`) |
| Login / sửa tài khoản | `LoginScr.cs:437`, `:450`; `GameCanvas.cs:1000` | Nhập tài khoản mới → Lưu → màn Chơi (`LoginScr.cs:1133`); login thành công → tạo/chọn nhân vật/game theo phản hồi |
| Tạo nhân vật | `CreateCharScr.cs:40`; phản hồi server (`Controller.cs:1701`) | Tạo gửi request; Back đóng socket về màn Chơi, nhánh guest có xác nhận (`CreateCharScr.cs:569`) |
| Chọn nhân vật | `ChooseCharScr.cs:33`; `Controller.cs:4353` | Vào game gửi finishUpdate; Trở ra reset về màn Chơi (`ChooseCharScr.cs:135`) |
| Menu ngoài game | `LoginScr.cs:483`; nút Menu → action 2003 (`:1126`) | Chọn lệnh hoặc cơ chế đóng Menu dùng chung; xem nhóm 6 |
| Bổ sung thông tin cá nhân, RegisterScreen legacy | Packet42 (`Assets.src.f/Controller2.cs:128`, `:136`) → `Assets.src.g/RegisterScreen.cs:301` | Thoát đóng session → màn server (`Assets.src.g/RegisterScreen.cs:889`); không phải tạo tài khoản web |

## Nút và hành động

| ID | Nút/hành động | Loại | Handler thực tế / kết luận |
|---|---|---|---|
| L01 | Chọn NRO | OK | Chọn index, lưu `svselect`, đóng socket cũ, sang màn Chơi: `ServerScr.cs:189`, `ServerListScreen.cs:427` |
| L02 | Nhập IP server | OK | Mở dialog nếu không đang login: `ServerScr.cs:180` |
| L03 | Lưu IP/cổng | OK | Validate IPv4/hostname, port 1–65535; lưu RMS, đóng rồi reconnect: `CustomServerDialog.cs:99`, `CustomServerAddress.cs:178`, `:140` |
| L04 | Hủy nhập IP | OK | Đóng keyboard/dialog, phục hồi trạng thái auto-connect trước đó: `CustomServerDialog.cs:80`, `:90` |
| L05 | Về mặc định trong dialog | OK | Xóa riêng endpoint RMS và reconnect: `CustomServerDialog.cs:106`, `CustomServerAddress.cs:52` |
| L06 | Thử lại khi lỗi endpoint | OK | Gọi Reconnect, có deadline 15 giây: `CustomServerAddress.cs:23`, `:113`, `:173` |
| L07 | Về mặc định khi lỗi endpoint | OK | Reset rồi reconnect: `CustomServerAddress.cs:27`, `:174` |
| L08 | Chơi TK X / Chơi tiếp | BROKEN | Đọc acc/pass RMS rồi auth (`ServerListScreen.cs:848`, `LoginScr.cs:591`); `savePass()` sau auth đổi password sang lowercase/trim (`LoginScr.cs:660`), khác đường Lưu tài khoản mới giữ nguyên (`:1148`). Việc server chấp nhận password đã đổi: chưa xác minh trong audit client |
| L09 | Chơi mới/guest | OK | `doGuestLogin` và chờ: `ServerListScreen.cs:869`, `:938`; khả năng server cấp guest: chưa xác minh |
| L10 | Đổi tài khoản | OK | Không xác nhận; khóa auto-login, đóng session, mở form trống, không xóa credential cũ: `ServerListScreen.cs:904`, `LoginScr.cs:1211`, `GameCanvas.cs:1000` |
| L11 | Lưu tài khoản mới | OK | Validate rỗng; lưu username trim/password nguyên; về màn Chơi, chưa login: `LoginScr.cs:1133` |
| L12 | OK đăng nhập thường | BROKEN | Có guard/đợi; vẫn trim password khi lưu/gửi: `LoginScr.cs:1154`, `:596`. Password có khoảng trắng đầu/cuối bị đổi trước auth |
| L13 | Hiện/Ẩn password | OK | Đổi chế độ che, reset text và mở lại bàn phím nếu cần: `LoginScr.cs:1175` |
| L14 | Next username | OK | Chuyển focus password và native keyboard: `LoginScr.cs:1162` |
| L15 | Done password | OK | Gọi cùng action OK (hoặc đăng ký trong mode legacy): `LoginScr.cs:1166` |
| L16 | Menu login | OK | Tạo các mục tùy cấu hình, gồm đổi server: `LoginScr.cs:483`, `:1126` |
| L17 | Server từ Menu/màn Chơi | OK | Mở chọn server; `LoginScr.cs:1092`, `ServerListScreen.cs:989` |
| L18 | Tùy chọn ngoài game | BROKEN | Handler mở menu đồ họa (`ServerListScreen.cs:912`), nhưng touch bị bỏ qua lúc testConnect -1/0 vì không nằm whitelist (`:587`); chỉ áp dụng khi nút có trong `cmd` (`:208`, `:262`) |
| L19 | Đồ họa thấp | OK | Lưu lowGraphic=1 rồi yêu cầu khởi động lại: `ServerListScreen.cs:928` |
| L20 | Đồ họa cao | OK | Lưu lowGraphic=0 rồi yêu cầu khởi động lại: `ServerListScreen.cs:933` |
| L21 | Tải tài nguyên | OK | Gửi getResource(1), đặt trạng thái nhận/tải: `ServerListScreen.cs:824`; hoàn tất dữ liệu thực tế chưa xác minh |
| L22 | Hủy tải tài nguyên | BROKEN | Nút Hủy action 4 (`ServerListScreen.cs:827`) chạy chung nhánh 1/4 và `Rms.clearAll()` (`:813`, `:821`). Hàm xóa mọi file local trong persistentDataPath (`Rms.cs:208`), gồm acc/pass/endpoint đã lưu; không có xác nhận riêng tại nhánh này |
| L23 | Xóa dữ liệu chủ động | OK | Hỏi xác nhận rồi mới clearAll: `ServerListScreen.cs:973`, `:979`; phạm vi xóa rộng cần nêu rõ cho người dùng |
| L24 | Hotline (clientType 1/3/5/6) | DEAD | Nút touch dispatch (`LoginScr.cs:932`); action13 gọi các hàm rỗng `mSystem.cs:264`, `:268`, `:272`. Type2 chỉ break (`LoginScr.cs:1068`), type7 không có nhánh. Không đồng nhất clientType với nền tảng Unity: mặc định type4 (`mSystem.cs:17`), có thể bị RMS đổi (`GameCanvas.cs:444`) |
| L25 | Đăng ký web | UNREACHABLE | Menu chỉ thêm khi URL hợp lệ (`LoginScr.cs:486`); `GameMidlet.RegistrationUrl` đang rỗng (`GameMidlet.cs:9`). Handler mở web có thật (`LoginScr.cs:1206`); ẩn theo cấu hình, không kết luận DEAD |
| L26 | Quên mật khẩu web | UNREACHABLE | Cùng điều kiện URL; config rỗng (`GameMidlet.cs:10`), handler `LoginScr.cs:1100` |
| L27 | Website tài khoản | UNREACHABLE | Config rỗng (`GameMidlet.cs:11`), menu/handler `LoginScr.cs:494`, `:1103` |
| L28 | FAQ legacy | UNREACHABLE | `LoginScr.cs:513` có handler connect/wait; tìm tham chiếu toàn `Assets/Scripts` chỉ thấy định nghĩa tại LoginScr (bản khác nằm RegisterScreen), không thấy caller vào hàm LoginScr |
| L29 | Chọn server legacy Bokken/Shuriken/Tessen | UNREACHABLE | `LoginScr.cs:526` tạo menu legacy, không thấy caller; action đổi server hiện dùng màn ServerList (`:1092`) |
| L30 | Tạo nhân vật | OK | Chặn tên rỗng/<5/>10 rồi gửi createChar: `CreateCharScr.cs:573`; ký tự a–z/0–9 có trong hướng dẫn nhưng handler client chưa lọc ký tự (`:579`, `:590`), server trả gì chưa xác minh |
| L31 | Back tạo nhân vật | OK | Có đóng socket; guest dùng xác nhận, account thường quay thẳng: `CreateCharScr.cs:592` |
| L32 | Chọn tóc | OK | Touch cập nhật indexHair trong giới hạn; đổi map/preview: `CreateCharScr.cs:390` |
| L33 | Chọn nhân vật | OK | Touch cập nhật focus rồi Vào game gửi playerID: `ChooseCharScr.cs:43`, `:135`; danh sách rỗng từ server chưa xác minh |
| L34 | Trở ra chọn nhân vật | UNCLEAR | Nhãn viết sai “Trờ ra” (`ChooseCharScr.cs:132`); handler reset màn Chơi có tác dụng (`:146`) |
| L35 | Hotline (clientType 4, mặc định) | BROKEN | Action gọi `callHotlinePC`, mở URL placeholder `http://abc.com` thay vì hỗ trợ (`LoginScr.cs:1065`, `mSystem.cs:259`) |

| L36 | Gửi form thông tin cá nhân | UNCLEAR | OK2008 kiểm tra9 ô không rỗng rồi charInfo; dùng thông báo OK “Vui lòng chờ” thay spinner (`Assets.src.g/RegisterScreen.cs:918`, `:924`). Ngày/CMND không validate tại handler; yêu cầu server chưa xác minh |
| L37 | Thoát form thông tin cá nhân | OK | Action1003 đóng session, về server screen (`Assets.src.g/RegisterScreen.cs:889`) |
| L38 | Chọn hành tinh tạo nhân vật | OK | Touch clamp indexGender, doChangeMap (`CreateCharScr.cs:370`, `:385`) |
| L39 | Checkbox Ghi nhớ legacy | UNREACHABLE | cmdCheck chỉ khai báo/khởi tạo (`LoginScr.cs:26`, `:351`), không thấy paint/hit/caller command toàn source. Handler2001 toggle có thật (`:1113`); isCheck vẫn điều khiển savePass (`:662`) |

## Điểm chung / giới hạn

| Chủ đề | Bằng chứng / kết quả |
|---|---|
| Deadline login | 15 giây connect và 20 giây auth/load; đường timeout hiện tiếng Việt, reset cờ: `LoginScr.cs:198`. Đây là điểm đã có, không ghi là thiếu |
| Địa chỉ đang dùng | Label có endpoint và fallback nếu có; hostname dài cuộn để đọc: `CustomServerAddress.cs:78` |
| Nhập liệu | Custom endpoint dùng tùy chọn loginInput + Next/Done (`CustomServerDialog.cs:24`); Login khóa thao tác khi đang chờ (`LoginScr.cs:942`). Native Android chưa chạy thử |
| Ghi nhớ | RMS lưu acc/pass dạng UTF ra file local (`Rms.cs:77`, `:181`); vấn đề đổi password ở L08/L12 là source hiện tại, không suy từ lịch sử batch |
| Đồ họa/frame | ServerScr tạo `new Command` mỗi update khi điều khiển bàn phím (`ServerScr.cs:93`); dialog endpoint splitFontArray mỗi paint khi có lỗi (`CustomServerDialog.cs:53`). Chi phí GC/FPS chưa đo |
| Số thao tác | Đã có acc: bấm Chơi → request auth; đổi acc: Đổi TK → nhập → Lưu → Chơi (`ServerListScreen.cs:904`, `LoginScr.cs:1133`, `ServerListScreen.cs:848`); chưa tính các bước server yêu cầu tạo/chọn nhân vật |
