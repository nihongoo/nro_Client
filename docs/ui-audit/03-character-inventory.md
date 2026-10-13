# 3. Nhân vật / túi / trang bị / kỹ năng

Chia ba phần do dùng chung Panel lớn. Quy ước [README](README.md); xác minh tĩnh client, không test UI/server.

## Bản đồ

| Màn/panel | Mở vào | Thoát |
|---|---|---|
| Main panel: nhiệm vụ, hành trang, kỹ năng, bang, mod, rương, chức năng | HUD Menu → `GameScr.cs:6670`, `Panel.cs:1364`; route tab `Panel.cs:1887`, `:8346` | X/softkey/bấm ngoài → hide, có thể về cấp cha (`Panel.cs:1836`, `:6856`) |
| Hành trang + trang bị | Main tab 1 (`Panel.cs:1368`); chọn item → `:7517` | Menu đóng; X panel dùng chung |
| Rương + túi phụ | Phản hồi server `Controller.cs:1985` → `Panel.cs:1194`; cũng main tab 5 (`Panel.cs:1391`) | hide; panel2 bị xóa (`Panel.cs:6933`) |
| Kỹ năng / tiềm năng | Main tab 2 (`Panel.cs:1372`); `Panel.cs:8503` | Đóng submenu hoặc panel |
| Đệ tử: trang bị/trạng thái/túi | Tool → petInfo (`Panel.cs:7874`); `Panel.cs:1314` thiết lập type 21 | hide về main (`Panel.cs:6912`) |
| Nội tại/kỹ năng đặc biệt | Server → `Controller.cs:206` → `Panel.cs:10391` type 25 | X panel; thao tác dòng gọi hàm rỗng (`Panel.cs:7111`, `:7124`) |
| Auto dùng item / set đồ | Context menu item (`Panel.cs:7574`, `:7600`) → ChatTextField/AutoItem | Nhập xong tự đóng; `Mod.CuongLe/AutoItem.cs:184` |
| Auto vứt | Menu mod gọi ShowMenu; định nghĩa `Mod.CuongLe/AutoVutDo.cs:27`; thêm trực tiếp từ túi `Panel.cs:7614` | Menu dùng chung; nhập xong reset chat (`AutoVutDo.cs:112`) |

## A. Túi / trang bị / rương

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| C01 | Tab hành trang | OK | setTabInventory và updateKeyInventory (`Panel.cs:1368`, `:1896`); chọn item mở menu + chi tiết (`:7517`, `:7634`) |
| C02 | Mặc trang bị | OK | Action 2000 getItem(BAG_BODY, index) (`Panel.cs:9034`) |
| C03 | Tháo trang bị | OK | Action 2002 getItem(BODY_BAG, index) (`Panel.cs:9069`) |
| C04 | Dùng vật phẩm | OK | Action 2001 useItem theo body/bag; capsule tự đóng panel (`Panel.cs:9044`) |
| C05 | Bỏ vật phẩm thường | OK | Action 2003 chỉ gửi yêu cầu useItem(1…) (`Panel.cs:9080`); không kết luận xóa ngay: callback itemRequest mở xác nhận (`:8849`), OK mới useItem(2/3…) (`:9087`). Server có luôn gửi xác nhận: chưa xác minh |
| C06 | Đồng ý bỏ item | OK | Dialog giữ where/index, gửi bước xác nhận rồi endDlg (`Panel.cs:8849`, `:9087`) |
| C07 | Hủy bỏ item | OK | Action 4005 chỉ endDlg (`Panel.cs:9176`) |
| C08 | Chuyển item túi → rương | OK | Action 1001 getItem(BAG_BOX…) (`Panel.cs:9008`) |
| C09 | Chuyển trang bị → rương | OK | Action 1002 getItem(BODY_BOX…) (`Panel.cs:9017`) |
| C10 | Lấy item từ rương | OK | Action 1000 getItem(BOX_BAG…) (`Panel.cs:9004`); nguồn rương `:8774`, main tab rương `:11025` |
| C11 | Dùng item ngay trong rương | BROKEN | Item không phải trang bị vẫn được gán action 2000 (`Panel.cs:8798`, `:11050`), nhưng 2000 lấy index từ túi và gửi BAG_BODY (`:9034`), khác nguồn BOX. Có thể bị server từ chối hoặc tác động item túi cùng index; kết quả server chưa xác minh |
| C12 | Thêm Set 1 | OK | AutoItem action 4 thêm getFullName vào list RAM (`Panel.cs:7581`, `AutoItem.cs:169`) |
| C13 | Thêm Set 2 | OK | Action 5 thêm list RAM (`AutoItem.cs:172`) |
| C14 | Xóa Set 1 | OK | Action 6 Remove tên (`AutoItem.cs:175`) |
| C15 | Xóa Set 2 | OK | Action 7 Remove tên (`AutoItem.cs:178`); ghi nhớ qua restart chưa thấy tại handler này |
| C16 | Auto sử dụng item / nhập delay | BROKEN | Mở nhập (`AutoItem.cs:215`); int.Parse rồi lưu trực tiếp, thiếu >0 (`:101`). Update so thời gian với delay*1000, delay 0/âm gây gửi liên tục theo update (`:72`) |
| C17 | Ngưng Auto sử dụng | OK | RemoveItemAuto tìm theo template id (`AutoItem.cs:163`, `:203`) |
| C18 | Thêm item vào danh sách vứt | BROKEN | Điều kiện loại id 455/194 dùng OR nên luôn đúng (`Panel.cs:7610`); handler thêm list (`AutoVutDo.cs:99`, `:325`), loop gửi useItem(2…) không kiểm tra lại hai ID (`:254`). Chưa xác minh server bảo vệ các item này |
| C19 | Xóa item khỏi danh sách vứt | OK | Action 8 remove và thông báo (`AutoVutDo.cs:104`, `:330`) |
| C20 | Bật/tắt Auto vứt | UNCLEAR | Toggle rồi chạy worker gửi useItem(2…), không xác nhận lần bật (`AutoVutDo.cs:79`, `:254`); tác dụng tiêu hủy cần cảnh báo rõ, kết quả server chưa xác minh |
| C21 | Thêm nhiều ID vứt bằng ô nhập | BROKEN | Nhãn cho nhập `_` giữa ID nhưng đặt INPUT_TYPE_NUMERIC (`AutoVutDo.cs:43`); parser lại Split('_') (`:138`). Native NumberPad ở `TField.cs:848` không bảo đảm có `_`; bàn phím cụ thể chưa xác minh |
| C22 | Xóa nhiều ID vứt bằng ô nhập | BROKEN | Cùng xung đột numeric / `_`: `AutoVutDo.cs:53`, `:193` |
| C23 | Xóa tất cả danh sách vứt | OK | Chỉ Clear list + thông báo, không trực tiếp xóa đồ (`AutoVutDo.cs:49`); chưa lưu RMS đến khi bấm Lưu |
| C24 | Xem danh sách vứt | OK | Popup danh sách hoặc thông báo trống (`AutoVutDo.cs:59`); ID không có template có thể lỗi GetNameItem không null-guard (`:294`), dữ liệu gây lỗi thực tế chưa xác minh |
| C25 | Lưu danh sách vứt | OK | Join IDs rồi saveRMSString (`AutoVutDo.cs:87`) |

## B. Kỹ năng / tiềm năng

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| C26 | Tab kỹ năng / xem chi tiết | OK | setTabSkill (`Panel.cs:1372`), doFireSkill + addSkillDetail (`:8503`, `:8541`) |
| C27 | Học kỹ năng từ tab | UNCLEAR | Nhãn Học tạo action 9004 (`Panel.cs:8538`), handler chỉ hiện hướng dẫn học/mua, không gửi request (`:9250`); đây là hướng dẫn, không kết luận server thiếu |
| C28 | Nâng cấp kỹ năng từ tab | UNCLEAR | Action 9002 chỉ hiện thông báo/hướng dẫn (`Panel.cs:8531`, `:9227`), không nâng cấp trực tiếp; nên ghi rõ “Cách nâng cấp” |
| C29 | Gán ô kỹ năng mobile | OK | 9003 → doSetOnScreenSkill →11120 lưu RMS, gỡ slot trùng (`Panel.cs:9239`, `GameScr.cs:1368`, `:6958`) |
| C30 | Gán phím kỹ năng PC | OK | doSetKeySkill →11121 lưu RMS (`Panel.cs:9245`, `GameScr.cs:1389`, `:6974`) |
| C31 | Cộng 1 đơn vị chỉ số | OK | upPotential(selected,1), endDlg và chờ (`Panel.cs:9209`); dòng KI có lỗi giá hiển thị riêng C34 |
| C32 | Cộng 10 đơn vị chỉ số | OK | upPotential(selected,10) (`Panel.cs:9215`) |
| C33 | Cộng 100 đơn vị chỉ số | OK | upPotential(selected,100) (`Panel.cs:9221`) |
| C34 | Giá cộng KI khi đủ 10 nhưng chưa đủ 100 | BROKEN | Điều kiện dùng cMPGoc (`Panel.cs:8593`), hai nhãn giá lại dùng cHPGoc (`:8596`, `:8597`). Khi HP khác MP, giá người chơi đọc sai; nhánh ≥100 dùng cMPGoc (`:8604`) |
| C35 | Chọn dòng nội tại/kỹ năng đặc biệt | DEAD | Route type 25 gọi doSpeacialSkill rỗng (`Panel.cs:7111`, `:7124`); màn có đường vào từ Controller (`Controller.cs:206`) |

## C. Đệ tử

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| C36 | Mở đệ tử | OK | petInfo + showWait (`Panel.cs:7874`); hiển thị sau server phản hồi, chưa xác minh nội dung runtime |
| C37 | Trang bị cho đệ tử | OK | Action 2005 getItem(BAG_PET…) (`Panel.cs:9095`) |
| C38 | Tháo đồ đệ tử | OK | Action 2006 getItem(PET_BAG…) (`Panel.cs:9100`), lệnh tạo ở `:7156` |
| C39 | Trạng thái theo/bảo vệ/tấn công/về nhà/hợp thể | OK | Các lựa chọn dùng petStatus(selected), cập nhật local nếu <4 (`Panel.cs:7175`); đếm một mẫu hành động sinh theo strStatus (`:1328`) |
| C40 | Hợp thể vĩnh viễn | OK | selected=5 mở Yes/No (`Panel.cs:7181`), Yes petStatus(5) (`GameScr.cs:7054`); có xác nhận phù hợp |

## Điểm chung

| Chủ đề | Bằng chứng / điều cần xác minh |
|---|---|
| Vùng chạm/list | Hàng chuẩn 24 đơn vị (`Panel.cs:803`), tab W/7 (`:826`); phải đo trên điện thoại trước kết luận nhỏ hơn chuẩn dp |
| Luồng thường dùng | Dùng đồ cần Menu → tab túi → item → Dùng (`GameScr.cs:6670`, `Panel.cs:8346`, `:7517`); chưa có số đo tần suất để tự coi tất cả menu là thừa |
| Rương trùng đường vào | Main tab 5 và NPC cùng dẫn vào dữ liệu rương (`Panel.cs:8377`, `Controller.cs:1985`); là hai ngữ cảnh khác, chưa kết luận DUPLICATE thừa |
| Chi tiết/GC | Mỗi lần chọn item tạo vector/menu (`Panel.cs:7531`), không phải mỗi frame. Không quy chụp thành lỗi paint |
| An toàn | Bỏ đồ thường và bán có callback xác nhận (`Panel.cs:8849`, `:8859`); auto vứt gửi bước 2 trực tiếp (`AutoVutDo.cs:269`). Không đồng nhất hai luồng |
