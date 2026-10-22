# Sửa đường đổi IP khi lỗi kết nối — 15/10/2026

- Nguyên nhân xác nhận qua code: popup lỗi endpoint chỉ có Thử lại/Về mặc định, không mở được form nhập địa chỉ mới; lỗi đăng nhập dùng server mặc định còn đi qua popup OK riêng. Chưa có bằng chứng chạy máy để khẳng định đây là nguyên nhân duy nhất của tình trạng user báo kẹt.
- `Assets/Scripts/CustomServerAddress.cs`: thêm Đổi IP ngay trên popup lỗi; đóng cả hai session, xóa queue/cờ chờ/loading trước khi mở form. Bắt lỗi kết nối chính ở màn trước khi vào game cho cả địa chỉ mặc định và địa chỉ tự nhập; deadline kết nối 15 giây cũng áp dụng cho địa chỉ mặc định. Giữ nguyên xử lý mất kết nối khi đã vào GameScr/CreateCharScr và trường hợp bị thay phiên đăng nhập.
- `Assets/Scripts/LoginScr.cs`: lỗi/timeout đăng nhập đều dùng popup phục hồi có Đổi IP.
- `Assets/Scripts/CustomServerDialog.cs`: tái sử dụng form nhập IP/cổng, validate và lưu RMS hiện có; Lưu kết nối lại bằng địa chỉ mới; Hủy từ popup lỗi trở lại popup lỗi để vẫn có đường Thử lại/Đổi IP/Về mặc định. Chỉ reset endpoint khi người chơi chọn Về mặc định, không xóa tài khoản hay dữ liệu game.
- Ba nút popup touch đặt cách nhau theo kích thước vẽ thực 76 logical pixels; màn hẹp xếp dọc. Không sửa bố trí chung của MsgDlg hay HUD chiến đấu/PC.

Kiểm tra tĩnh: rà call path lỗi → popup → action 3 → đóng kết nối → form → Save/Reconnect; action ID đều dương; vùng bấm đồng bộ kích thước nút và không chồng nhau. `git diff --check` cho các file sửa.

Test tay cần user chạy, tất cả **chưa chạy**:

| Ca | Kết quả cần thấy |
|---|---|
| IP sai/cổng đóng hoặc timeout, cả mặc định và custom | Hết trạng thái chờ; popup có Thử lại / Đổi IP / Về mặc định |
| Đổi IP → nhập IP/cổng đúng → Lưu | Kết nối địa chỉ mới; có thể đăng nhập; mở lại game giữ địa chỉ đã lưu |
| Đổi IP → Hủy | Trở lại popup lỗi, cả ba lựa chọn còn bấm được |
| Nhập IP/cổng không hợp lệ | Báo lỗi trong form, tiếp tục sửa được |
| Lỗi kết nối đến muộn khi đang nhập | Không thay mất form hoặc tự kết nối lại địa chỉ cũ |
| Thử lại vẫn sai nhiều lần, sau đó Đổi IP | Không kẹt chờ; đổi địa chỉ vẫn được |
| Về mặc định | Xóa riêng endpoint custom, thử lại địa chỉ mặc định; lỗi tiếp vẫn đổi IP được |
| Android Simulator 16:9, 20:9, màn hẹp và S22 | Ba nút không chồng vùng bấm; bàn phím nhập/sửa/đóng hoạt động |

Chưa compile, chạy client, build Unity/APK. Không sửa server; không push/merge/stash.
