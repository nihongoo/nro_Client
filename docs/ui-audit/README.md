# Khảo sát UI/UX client

Snapshot: `develop` / `origin/develop` tại `67a32b7`, nhánh `audit/ui-ux`. Checkout riêng: `D:\project_Unity\client_Nro\Mod-nro-unity-ui-audit`; giữ nguyên checkout Unity đang dùng và thay đổi local ở đó.

Chỉ đọc source; không chạy game/build/UI test/DB, không sửa code, không push. OK nghĩa là handler client có tác dụng rõ và chưa thấy lỗi trong phần đã đọc; **không chứng nhận runtime hay backend**. Các mục cần server, dữ liệu động, IME/bố cục thật được ghi **chưa xác minh**.

`File.cs:line` tính theo snapshot trên. File không ghi thư mục nằm tại `Assets/Scripts/`; file mod tại `Assets/Scripts/Mod.CuongLe/`. Tham chiếu `:line` tiếp theo dùng cùng file gần nhất. Bảng đếm **hành động logic**, các bản sao theo item/server/kỹ năng đếm một mẫu; không phải số nút runtime. Biến thể cùng handler có thể gộp một dòng. Hành động thiếu bằng chứng không ép vào sáu loại.

| Nhóm | Báo cáo |
|---|---|
| 1 | [Login/chọn server/menu ngoài game](01-login-server.md) |
| 2 | [HUD](02-hud.md) |
| 3 | [Nhân vật/túi/trang bị/kỹ năng](03-character-inventory.md) |
| 4 | [NPC/hội thoại/nhiệm vụ/shop](04-npc-shop.md) |
| 5 | [Xã hội](05-social.md) |
| 6 | [Cài đặt/popup chung](06-settings-dialogs.md) |
| 6 bổ sung | [Menu mod](06b-mod-menus.md) |
| Tổng hợp | [SUMMARY](SUMMARY.md) |

[CONTROL_INDEX.csv](CONTROL_INDEX.csv) liệt kê điểm tạo Command để truy vết, không tự phân loại từ constructor. Nút touch gọi trực tiếp có trong báo cáo. [SCREEN_INDEX.csv](SCREEN_INDEX.csv) đối chiếu tất cả subclass mScreen/Dialog tìm thấy. Các kết luận trong bảng đã đọc handler; backend và vòng worker bot chưa kiểm hết được ghi rõ ở từng khu vực.
