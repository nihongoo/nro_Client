# 4. NPC / hội thoại / nhiệm vụ / shop

Tách hội thoại/nhiệm vụ, mua bán và chức năng phụ. Menu NPC lấy từ server: xác nhận được dispatch client, **chưa xác minh mọi lựa chọn nghiệp vụ server**. Xem [README](README.md).

## Bản đồ

| Màn/panel | Mở vào | Thoát |
|---|---|---|
| Menu NPC | Chọn/tương tác NPC; dữ liệu menu → `GameScr.cs:1492` | Chọn command → confirmMenu; Close Menu chung (`Menu.cs:256`) |
| Hội thoại nhiều trang | `ChatPopup.cs:797`, `:849`; nút cmdNextLine | Next đến trang cuối bỏ popup/unlock (`ChatPopup.cs:746`) |
| Nhiệm vụ / bản đồ hành tinh | Main panel tab0 (`Panel.cs:1889`) → nút map (`:2509`) | Map hide → main (`Panel.cs:6927`) |
| Shop thường / học kỹ năng ở shop / nhận thưởng | Dữ liệu server → setTypeShop (`Panel.cs:1185`) | X/hide; trả preview trang bị (`Panel.cs:6864`) |
| Ký gửi | Shop typeShop=2; panel phụ type17 (`Panel.cs:913`) | hide panel, đóng input dùng ChatTextField |
| Ghép/nâng cấp | Server → `Controller.cs:887`; `Panel.cs:1228` | X nếu không bị chờ/animation khóa (`Panel.cs:1838`); hiệu ứng hoàn tất đặt isDoneCombine (`:9877`, `:9920`) |
| Thành tựu | `Controller.cs:1136` → `Panel.cs:891` | X/hide |
| Khu vực / dịch chuyển | Tool openUIZone (`Panel.cs:7694`), phản hồi `GameScr.cs:6676`; Panel mapTrans `Panel.cs:855` | Chọn đánh dấu isChangeZone rồi hide (`Panel.cs:8759`); gửi ở quá trình đóng, cần kiểm tra callback runtime |
| Radar | Tool → doRada (`Panel.cs:7644`), RadarScr `:729` | Nút thứ 3 → GameScr (`RadarScr.cs:508`) |
| Đập bóng / quay số | Tool openMenu(54) (`Panel.cs:7678`); Controller2 nhận → SetCrackBallScr (`Assets.src.f/Controller2.cs:1043`) | Nút kỹ năng thứ2 → GameScr (`CrackBallScr.cs:536`) |
| Di chuyển bằng tàu | Controller2 `Assets.src.f/Controller2.cs:852` → `TransportScr.cs:66` | Đủ tiến độ gửi transportNow; chờ server (`TransportScr.cs:168`) |
| Nạp thẻ | Account panel (`Panel.cs:10354`); cũng server `Controller.cs:5069` | Đóng/Gửi đều quay game và clear form (`MoneyCharge.cs:218`) |

## A. Hội thoại / nhiệm vụ

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| N01 | Chọn dòng menu NPC động | OK | Command 11057 → confirmMenu(npcId, menuSelectedItem); item NPC dùng pickItem (`GameScr.cs:1492`, `:6896`); nghiệp vụ từng NPC chưa xác minh |
| N02 | Next/đóng hội thoại cuối | OK | 8000 tăng currentLine, dựng trang tiếp; cuối unlock Char (`ChatPopup.cs:746`) |
| N03 | Xem nhiệm vụ | OK | setTabTask + scrollQuest (`Panel.cs:3608`, `:2472`) |
| N04 | Bản đồ từ nhiệm vụ | OK | Touch 70×30, waitToPerform → setTypeMap (`Panel.cs:2503`, `:8350`) |
| N05 | Nhận thành tựu | OK | Chỉ gửi khi hoàn thành/chưa nhận; touch yêu cầu chạm mép phải 40 đơn vị (`Panel.cs:7502`) |
| N06 | Chọn khu/dịch chuyển | OK | doFireZone đặt isChangeZone và hide (`Panel.cs:8759`); phản hồi và điểm đến thực tế chưa xác minh |

## B. Shop / ký gửi / nâng cấp

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| N07 | Xem item shop | OK | Xác định tab/item rồi dựng menu + detail (`Panel.cs:7356`) |
| N08 | Mua bằng xu / nhận miễn phí | OK | 3000 → buyItem(0, templateId,0) (`Panel.cs:9121`); phí/nhận đúng item chưa xác minh server |
| N09 | Mua bằng ngọc | OK | 3001 → pleasewait 5 giây, buyItem(1…) (`Panel.cs:9127`, `MsgDlg.cs:26`) |
| N10 | Học bằng tiềm năng ở shop | OK | 3004 → buyItem(3…) (`Panel.cs:9146`); khác nút hướng dẫn Học ở tab kỹ năng |
| N11 | Mua giá đặc biệt | OK | 3005 → buyItem(3…) (`Panel.cs:9151`); giao thức trùng mode 3 chưa đủ để kết luận trùng tính năng |
| N12 | Nhận một phần thưởng | OK | 30001 → buyItem(0,selected,0) (`Panel.cs:9106`), chỉ trong loại shop phù hợp |
| N13 | Xóa phần thưởng | UNCLEAR | 30002 → buyItem(1,selected,0) ngay (`Panel.cs:9111`); không thấy xác nhận tại handler; có mất thưởng không chưa xác minh server |
| N14 | Nhận tất cả | OK | 30003 → buyItem(2,selected,0) (`Panel.cs:9116`) |
| N15 | Mua xu shop type8 | OK | Nhãn 30001 (`Panel.cs:7393`) dùng handler selected (`:9106`); hợp đồng server type8 chưa xác minh |
| N16 | Mua ngọc shop type8 | OK | Nhãn 30002 (`Panel.cs:7399`), handler `:9111`; server phân biệt shop nhận thưởng/type8 chưa xác minh |
| N17 | Bán item | OK | 3002 gửi saleItem bước0 (`Panel.cs:9133`); callback saleRequest hỏi trước bước1 (`:8859`, `:9140`) |
| N18 | Đồng ý bán | OK | 3003 endDlg, saleItem(1,type,id) (`Panel.cs:9140`) |
| N19 | Hủy bán | OK | 4005 endDlg (`Panel.cs:9176`) |
| N20 | Ký gửi xu / nhập giá | OK | 10012 mở numeric field (`Panel.cs:9274`), parse có catch và gửi kigui (`:9561`); giá âm/0 chưa chặn tại handler, server xử lý chưa xác minh |
| N21 | Ký gửi ngọc / xác nhận | OK | Nhánh 10013 đặt trạng thái nhập (`Panel.cs:9312`); doNotiRuby parse và Yes/No (`:10425`), 11000/11001 gửi kigui (`:9396`) |
| N22 | Số lượng ký gửi xu | BROKEN | int.Parse không catch, chỉ kiểm tra >quantity, thiếu <=0 (`Panel.cs:9595`); overflow/chuỗi không hợp lệ có thể ném exception tại handler; hậu quả vòng UI chưa xác minh |
| N23 | Số lượng ký gửi ngọc | BROKEN | Cùng lỗi ở `Panel.cs:9606`; không kết luận UI crash toàn game nếu lớp trên bắt exception |
| N24 | Hủy ký gửi | OK | kigui(1,itemId…) + wait (`Panel.cs:9349`) |
| N25 | Nhận tiền ký gửi | OK | kigui(2…) + wait (`Panel.cs:9355`) |
| N26 | Mua ký gửi bằng xu | OK | kigui(3,itemId,0,buyCoin…) (`Panel.cs:9361`) |
| N27 | Mua ký gửi bằng ngọc | OK | kigui(3,itemId,1,buyGold…) (`Panel.cs:9367`) |
| N28 | Đẩy top ký gửi | OK | kigui(5…) + wait (`Panel.cs:9373`); chi phí thực tế chưa xác minh |
| N29 | Thêm nguyên liệu ghép | OK | 6000 chống trùng templateId, đánh dấu rồi thêm (`Panel.cs:8897`); có giới hạn “không hai món cùng template”, đúng nghiệp vụ không chưa xác minh |
| N30 | Gỡ nguyên liệu ghép | OK | 6001 bỏ đánh dấu/xóa list (`Panel.cs:8887`) |
| N31 | Thực hiện ghép | OK | Chặn list rỗng, combine(1,list), showWait (`Panel.cs:7308`); xác nhận giá/quy tắc server chưa xác minh |

## C. Màn phụ

| ID | Hành động | Loại | Handler / bằng chứng |
|---|---|---|---|
| N32 | Radar: chọn thẻ | OK | Touch 30×30 → doClickItem (`RadarScr.cs:410`, `:557`) |
| N33 | Radar: trang trước/sau | OK | Touch arrow20×20 → doClickArrow (`RadarScr.cs:415`, `:515`) |
| N34 | Radar: đổi bộ lọc | OK | doClickUse(0) → doChangeUI (`RadarScr.cs:495`, `:388`) |
| N35 | Radar: dùng thẻ | OK | SendRada(1,id) nếu có focus (`RadarScr.cs:502`) |
| N36 | Radar: đóng | OK | GameScr.switchToMe (`RadarScr.cs:508`) |
| N37 | Đập bóng: chọn bóng | OK | Kiểm tiền/vé trước đổi index/yTo (`CrackBallScr.cs:497`) |
| N38 | Đập bóng: đánh/tiếp tục | OK | Chuyển step, animation; giai đoạn phù hợp SendCrackBall (`CrackBallScr.cs:516`) |
| N39 | Đập bóng: thoát | OK | Reset isRongThanXuatHien và về GameScr (`CrackBallScr.cs:536`); `perform()` rỗng (`:228`) không chứng minh DEAD vì touch gọi trực tiếp doClickSkill |
| N40 | Tàu: Nhanh hơn | OK | Hỏi Yes/No, Yes bật isSpeed nếu còn ngọc (`TransportScr.cs:181`); phí cụ thể chưa xác minh |
| N41 | Tàu: Hủy tăng tốc | OK | Action3 endDlg (`TransportScr.cs:193`) |
| N42 | Nạp thẻ: Gửi | UNCLEAR | Validate rỗng, gửi rồi đóng/xóa form ngay, chưa nhận thành công (`MoneyCharge.cs:225`); response server có thể hiện thông báo, chưa xác minh nên không kết luận mất tiền/thẻ |
| N43 | Nạp thẻ: Đóng | OK | Quay GameScr và clearScreen (`MoneyCharge.cs:220`) |

## Điểm chung

| Chủ đề | Bằng chứng / giới hạn |
|---|---|
| Chờ chung | InfoDlg.showWait khóa input 1000 update rồi tự ẩn (`InfoDlg.cs:24`, `:59`); không tương đương deadline request có thông báo retry. Không ghi “treo vô hạn” chỉ vì đang chờ |
| Server menu | Nhãn/ID NPC đến từ dữ liệu runtime (`GameScr.cs:1492`). Chưa có snapshot packet/server menu đầy đủ để kết luận nút nào dẫn chức năng server thiếu |
| Nhập số | N22/N23 là thiếu guard tại handler, không cần chạy server để chứng minh; port custom server đã có TryParse/range, nên không gộp là tất cả input đều lỗi |
| Touch | Radar nút và mũi tên 20×20 (`RadarScr.cs:415`, `:441`); bóng30×30 (`CrackBallScr.cs:478`); cần đo độ dễ bấm trên máy thật |
| Hiệu năng | Ghép có animation/Effect mới định kỳ (`Panel.cs:9766`), không coi hoạt ảnh là redraw thừa; mức FPS/GC chưa xác minh |
