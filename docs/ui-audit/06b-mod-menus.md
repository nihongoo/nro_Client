# 6b. Menu mod — phụ lục nhóm6

OK xác nhận mở menu/toggle/lưu/feedback trong handler. Vòng automation, tài nguyên/tiền, kết quả server **chưa xác minh**. Tham chiếu file mod thuộc `Assets/Scripts/Mod.CuongLe/`.

## Bản đồ

| Khu vực | Mở vào | Thoát |
|---|---|---|
| Menu mod gốc/Khác | Icon HUD → MainMod.ShowMenu (`MainMod.cs:1391`, `:1125`); Khác :1139 | Menu đóng chung (`Menu.cs:256`) |
| Auto Map/đậu/nhặt/train/skill/chat/chỉ số/Mục tiêu | Command gốc → MainMod.perform (`MainMod.cs:707`) | Đóng menu; input ChatTextField; toggle bot vẫn chạy sau khi đóng |
| Auto Pro/boss/nâng cao/thông tin | MainMod action34 (`MainMod.cs:857`); `ModProCuongLe.cs:72`, `:1191`, `:141`, `:165` | Menu đóng; bot cần nút tắt riêng |
| Yardat/Bò Mộng/Auto vứt | Auto Pro → `ModProCuongLe.cs:121`, `:289`, `:271` | Menu/Input; Auto vứt nhóm3 |
| VIP inline | HUD icon (`MainMod.cs:1399`) | Chạm ngoài; chi tiết H20–H27 nhóm2 |
| Tab mod Panel | Main tab mod (`Panel.cs:8346`, `:8402`) | X/đổi tab |
| Đồ họa/ép ngọc legacy | DoHoa.ShowMenuDoHoa / AutoEpNro.ShowMenu | Không thấy caller, UNREACHABLE dưới đây |

## Hành động

| ID | Hành động | Loại | Handler / kết luận |
|---|---|---|---|
| M001 | Mở Auto Map | OK | MainMod.cs:712: gọi ShowMenu; menu gốc :1125/:1139 |
| M002 | Mở Auto Pro | OK | MainMod.cs:857: gọi ShowMenu; menu gốc :1125/:1139 |
| M003 | Mở Auto Skill | OK | MainMod.cs:715: gọi ShowMenu; menu gốc :1125/:1139 |
| M004 | Mở Auto đậu | OK | MainMod.cs:718: gọi ShowMenu; menu gốc :1125/:1139 |
| M005 | Mở Auto nhặt | OK | MainMod.cs:721: gọi ShowMenu; menu gốc :1125/:1139 |
| M006 | Mở Auto train | OK | MainMod.cs:724: gọi ShowMenu; menu gốc :1125/:1139 |
| M007 | Mở Mục tiêu | OK | MainMod.cs:825: gọi ShowMenu; menu gốc :1125/:1139 |
| M008 | Mở Khác | OK | MainMod.cs:733: gọi ShowMenu; menu gốc :1125/:1139 |
| M009 | Mở Auto chỉ số | OK | MainMod.cs:730: gọi ShowMenu; menu gốc :1125/:1139 |
| M010 | Mở Auto chat | OK | MainMod.cs:727: gọi ShowMenu; menu gốc :1125/:1139 |
| M011 | Facebook / Web DataNRO | OK | MainMod.cs:860, :863: platformRequest; trình duyệt thật chưa xác minh |
| M012 | Auto Map: Load Map → hành tinh | OK | AutoMap.cs:414; menu :462/:478/:488. Di chuyển server chưa xác minh |
| M013 | Auto Map: Ăn đùi gà | OK | AutoMap.cs:417; menu :462/:478/:488. Di chuyển server chưa xác minh |
| M014 | Auto Map: Thu đậu | OK | AutoMap.cs:425; menu :462/:478/:488. Di chuyển server chưa xác minh |
| M015 | Auto Map: Dùng capsule | OK | AutoMap.cs:433; menu :462/:478/:488. Di chuyển server chưa xác minh |
| M016 | Auto Map: Lưu cài đặt | OK | AutoMap.cs:441; menu :462/:478/:488. Di chuyển server chưa xác minh |
| M017 | Auto Map: Chọn hành tinh | OK | AutoMap.cs:450; menu :462/:478/:488. Di chuyển server chưa xác minh |
| M018 | Auto Map: Chọn map đích | OK | AutoMap.cs:453; menu :462/:478/:488. Di chuyển server chưa xác minh |
| M019 | Auto Map: Dừng | OK | AutoMap.cs:457; menu :462/:478/:488. Di chuyển server chưa xác minh |
| M020 | Auto đậu: Xin đậu | OK | AutoPean.cs:171: toggle/feedback/RMS; menu :274 |
| M021 | Auto đậu: Cho đậu | OK | AutoPean.cs:179: toggle/feedback/RMS; menu :274 |
| M022 | Auto đậu: Thu đậu | OK | AutoPean.cs:187: toggle/feedback/RMS; menu :274 |
| M023 | Auto đậu: Lưu | OK | AutoPean.cs:263: toggle/feedback/RMS; menu :274 |
| M024 | Auto đậu: ngưỡng HP | UNCLEAR | AutoPean.cs:195: nếu ngưỡng khác0, bấm đặt0; lần sau mới mở nhập. Nhãn :281–:284 không mô tả thao tác tắt; callback chưa xác minh đầy đủ |
| M025 | Auto đậu: ngưỡng %HP | UNCLEAR | AutoPean.cs:212: nếu ngưỡng khác0, bấm đặt0; lần sau mới mở nhập. Nhãn :281–:284 không mô tả thao tác tắt; callback chưa xác minh đầy đủ |
| M026 | Auto đậu: ngưỡng MP | UNCLEAR | AutoPean.cs:229: nếu ngưỡng khác0, bấm đặt0; lần sau mới mở nhập. Nhãn :281–:284 không mô tả thao tác tắt; callback chưa xác minh đầy đủ |
| M027 | Auto chat: map | OK | AutoChat.cs:205: đang bật thì dừng, chưa bật mở nhập; menu :280. Phí/giới hạn server chưa xác minh |
| M028 | Auto chat: thế giới | OK | AutoChat.cs:222: đang bật thì dừng, chưa bật mở nhập; menu :280. Phí/giới hạn server chưa xác minh |
| M029 | Auto chat: spam thế giới | OK | AutoChat.cs:239: đang bật thì dừng, chưa bật mở nhập; menu :280. Phí/giới hạn server chưa xác minh |
| M030 | Auto chat: inbox | OK | AutoChat.cs:252: đang bật thì dừng, chưa bật mở nhập; menu :280. Phí/giới hạn server chưa xác minh |
| M031 | Auto chat: Lưu | OK | AutoChat.cs:269: toggle chế độ lưu tự động/SaveData, không phải lưu một lần |
| M032 | Auto nhặt: nhặt thường | OK | AutoPick.cs:119; menu :172; hiệu lực worker/server chưa xác minh |
| M033 | Auto nhặt: nhặt tất cả | OK | AutoPick.cs:125; menu :172; hiệu lực worker/server chưa xác minh |
| M034 | Auto nhặt: dịch tới item | OK | AutoPick.cs:134; menu :172; hiệu lực worker/server chưa xác minh |
| M035 | Auto nhặt: xem danh sách | OK | AutoPick.cs:143; menu :172; hiệu lực worker/server chưa xác minh |
| M036 | Auto nhặt: xóa danh sách | OK | AutoPick.cs:154; menu :172; hiệu lực worker/server chưa xác minh |
| M037 | Auto nhặt: thêm item focus | OK | AutoPick.cs:165; menu :172; hiệu lực worker/server chưa xác minh |
| M038 | Auto nhặt: chuyển theo danh sách | BROKEN | AutoPick.cs:129: toggle isAutoPick; nếu nhặt thường ON thì chọn theo danh sách lại OFF; hai mode tại :175/:177 |
| M039 | Auto nhặt: nhập khoảng cách / ID | UNCLEAR | AutoPick.cs:80, :93: TryParse không chặn âm/ID không tồn tại; input :138/:158; hậu quả worker chưa xác minh |
| M040 | Auto train: loại quái | OK | AutoTrain.cs:222; menu :332/:376; kết quả automation chưa xác minh |
| M041 | Auto train: tất cả quái | OK | AutoTrain.cs:236; menu :332/:376; kết quả automation chưa xác minh |
| M042 | Auto train: danh sách vị trí | OK | AutoTrain.cs:240; menu :332/:376; kết quả automation chưa xác minh |
| M043 | Auto train: xóa danh sách | OK | AutoTrain.cs:250; menu :332/:376; kết quả automation chưa xác minh |
| M044 | Auto train: thêm quái focus | OK | AutoTrain.cs:261; menu :332/:376; kết quả automation chưa xác minh |
| M045 | Auto train: dừng | OK | AutoTrain.cs:266; menu :332/:376; kết quả automation chưa xác minh |
| M046 | Auto train: Goback khu | OK | AutoTrain.cs:279; menu :332/:376; kết quả automation chưa xác minh |
| M047 | Auto train: Goback tọa độ | OK | AutoTrain.cs:293; menu :332/:376; kết quả automation chưa xác minh |
| M048 | Auto train: ngưỡng MP | OK | AutoTrain.cs:303; menu :332/:376; kết quả automation chưa xác minh |
| M049 | Auto train: đổi set Xayda | OK | AutoTrain.cs:318; menu :332/:376; kết quả automation chưa xác minh |
| M050 | Auto train: Fake TDLT | OK | AutoTrain.cs:326; menu :332/:376; kết quả automation chưa xác minh |
| M051 | Auto train: Né siêu quái | BROKEN | AutoTrain.cs:243: toggle cờ nhưng feedback :244 và nhãn :364 đảo ON/OFF |
| M052 | Auto chỉ số: bật tự động | OK | AutoPoint.cs:177; menu :230; callback parse/catch :116. Range/server cần kiểm tra tiếp |
| M053 | Auto chỉ số: ngưỡng sức đánh | OK | AutoPoint.cs:181; menu :230; callback parse/catch :116. Range/server cần kiểm tra tiếp |
| M054 | Auto chỉ số: ngưỡng HP | OK | AutoPoint.cs:186; menu :230; callback parse/catch :116. Range/server cần kiểm tra tiếp |
| M055 | Auto chỉ số: ngưỡng MP | OK | AutoPoint.cs:191; menu :230; callback parse/catch :116. Range/server cần kiểm tra tiếp |
| M056 | Auto chỉ số: Cộng / Cộng tới mức | UNREACHABLE | AutoPoint.cs:240 dựng menu, callback :205/:212; tìm toàn Assets/Scripts không thấy caller ShowMenuAddPoint; khác cộng1/10/100 Panel |
| M057 | Auto skill: tấn công | OK | AutoSkill.cs:116; menu :199/:225; hiệu lực skill server chưa xác minh |
| M058 | Auto skill: đổi mục tiêu | OK | AutoSkill.cs:143; menu :199/:225; hiệu lực skill server chưa xác minh |
| M059 | Auto skill: xóa danh sách | OK | AutoSkill.cs:151; menu :199/:225; hiệu lực skill server chưa xác minh |
| M060 | Auto skill: bỏ mục tiêu focus | OK | AutoSkill.cs:157; menu :199/:225; hiệu lực skill server chưa xác minh |
| M061 | Auto skill: thêm mục tiêu focus | OK | AutoSkill.cs:164; menu :199/:225; hiệu lực skill server chưa xác minh |
| M062 | Auto skill: bật skill đã có | OK | AutoSkill.cs:174; menu :199/:225; hiệu lực skill server chưa xác minh |
| M063 | Auto skill: chọn slot trống | BROKEN | AutoSkill.cs:130 vẫn tạo dòng null; action10 :169 mở menu dereference keySkill[skillIndex].template.name tại :230, không null guard |
| M064 | Auto skill: Lưu | DEAD | AutoSkill.cs:205 tạo action5; :139–:140 chỉ báo Không Hỗ Trợ Lưu Cài Đặt Ở Mục Này |
| M065 | Auto skill: delay | BROKEN | AutoSkill.cs:89–:91 parse rồi bật auto, không giới hạn >0; input :183; tần suất gửi chưa đo |
| M066 | Auto skill: Đóng băng | UNCLEAR | AutoSkill.cs:230 nhãn Đóng Băng; :191–:193 thực tế sửa cooldown/manaUse local về0; hiệu lực server chưa xác minh |
| M067 | Mục tiêu: bỏ NPC | OK | MucTieu.cs:37: toggle/feedback; menu :52 |
| M068 | Mục tiêu: bỏ quái | OK | MucTieu.cs:41: toggle/feedback; menu :52 |
| M069 | Mục tiêu: bỏ người | OK | MucTieu.cs:45: toggle/feedback; menu :52 |
| M070 | Yardat: đổi mục tiêu | BROKEN | Yardat.cs:37: Start thread trước toggle :38; worker while(cờ) :63 có thể thoát sớm. Race tĩnh, tái hiện chưa xác minh |
| M071 | Yardat: bông tai/hợp thể | BROKEN | Yardat.cs:43–:44 cùng race; :75 dùng FindItemBag(454).indexUI không guard item thiếu |
| M072 | Yardat: đệ về nhà | OK | Yardat.cs:49 toggle, guard không đệ :52; Update :99 gửi trạng thái |
| M073 | Bò Mộng: bật/dừng | OK | AutoboMong.cs:74; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M074 | Bò Mộng: độ khó | OK | AutoboMong.cs:98; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M075 | Bò Mộng: bỏ NV vàng | OK | AutoboMong.cs:115; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M076 | Bò Mộng: bỏ NV người | OK | AutoboMong.cs:129; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M077 | Bò Mộng: bỏ NV quái | OK | AutoboMong.cs:140; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M078 | Bò Mộng: lưu | OK | AutoboMong.cs:151; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M079 | Bò Mộng: kiểu kiếm vàng | OK | AutoboMong.cs:159; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M080 | Bò Mộng: map kiếm vàng | OK | AutoboMong.cs:170; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M081 | Bò Mộng: hướng dẫn | OK | AutoboMong.cs:126; menu :178; chỉ xác nhận thao tác UI, chuỗi nhiệm vụ/server chưa xác minh |
| M082 | Auto Pro boss: Dò boss | OK | ModProCuongLe.cs:97; menu :1191; worker/chiến đấu chưa UI test |
| M083 | Auto Pro boss: Gim boss | OK | ModProCuongLe.cs:125; menu :1191; worker/chiến đấu chưa UI test |
| M084 | Auto Pro boss: Whis | OK | ModProCuongLe.cs:130; menu :1191; worker/chiến đấu chưa UI test |
| M085 | Auto Pro boss: Farm Napa | OK | ModProCuongLe.cs:302; menu :1191; worker/chiến đấu chưa UI test |
| M086 | Auto Pro boss: Dịch theo boss | OK | ModProCuongLe.cs:153; menu :1191; worker/chiến đấu chưa UI test |
| M087 | Auto Pro boss: Dịch + đánh boss | OK | ModProCuongLe.cs:218; menu :1191; worker/chiến đấu chưa UI test |
| M088 | Auto Pro: Boss trứng Mabu | UNCLEAR | ModProCuongLe.cs:1198 nhãn Boss trứng mabu; :291–:296 báo tìm Hirde. Quan hệ hai tên chưa xác minh |
| M089 | Auto Pro nâng cao: hồi phục Namek | OK | ModProCuongLe.cs:88; menu :141; toggle/worker. Tự sát/bán đồ không hỏi tại handler; hậu quả/tiêu chí đồ rác chưa xác minh |
| M090 | Auto Pro nâng cao: cờ đen | OK | ModProCuongLe.cs:92; menu :141; toggle/worker. Tự sát/bán đồ không hỏi tại handler; hậu quả/tiêu chí đồ rác chưa xác minh |
| M091 | Auto Pro nâng cao: up KOK | OK | ModProCuongLe.cs:159; menu :141; toggle/worker. Tự sát/bán đồ không hỏi tại handler; hậu quả/tiêu chí đồ rác chưa xác minh |
| M092 | Auto Pro nâng cao: đánh người cờ đen | OK | ModProCuongLe.cs:201; menu :141; toggle/worker. Tự sát/bán đồ không hỏi tại handler; hậu quả/tiêu chí đồ rác chưa xác minh |
| M093 | Auto Pro nâng cao: tự sát | UNCLEAR | ModProCuongLe.cs:246; menu :141; toggle/worker. Tự sát/bán đồ không hỏi tại handler; hậu quả/tiêu chí đồ rác chưa xác minh |
| M094 | Auto Pro nâng cao: đệ tự nói | OK | ModProCuongLe.cs:250; menu :141; toggle/worker. Tự sát/bán đồ không hỏi tại handler; hậu quả/tiêu chí đồ rác chưa xác minh |
| M095 | Auto Pro nâng cao: bán đồ rác | UNCLEAR | ModProCuongLe.cs:254; menu :141; toggle/worker. Tự sát/bán đồ không hỏi tại handler; hậu quả/tiêu chí đồ rác chưa xác minh |
| M096 | Auto Pro nâng cao: cất đồ VIP | OK | ModProCuongLe.cs:262; menu :141; toggle/worker. Tự sát/bán đồ không hỏi tại handler; hậu quả/tiêu chí đồ rác chưa xác minh |
| M097 | Auto Pro: lớp thông tin char/đệ/vàng/set | OK | ModProCuongLe.cs:173, :184, :236, :274: toggle loại trừ lớp khác; guard không có đệ; menu :165 |
| M098 | Auto Pro: mở Auto vứt/Bò Mộng/Yardat | OK | ModProCuongLe.cs:271, :289, :121; menu :72. Chi tiết vứt C18–C25, không đếm lại |
| M099 | Đồ họa mod legacy | UNREACHABLE | DoHoa.cs:90 dựng5 command; :63 toggle/RMS; tìm toàn source không thấy caller ShowMenuDoHoa. Toggle tương tự có ở Panel :8402 |
| M100 | Ép ngọc rồng legacy | UNREACHABLE | AutoEpNro.cs:20 dựng6 command, không có caller ShowMenu từ file khác; :32 action1–5 return, action6 worker; không gọi DEAD cho UI chưa có đường vào |
| M101 | Tab mod: boss/char/lưới/set/vàng/nhân vật | OK | Panel.cs:8402–:8454: toggle/RMS; entry tab :8346; một mẫu toggle hiển thị |
| M102 | Tab mod: captcha | UNCLEAR | Panel.cs:8455–:8483: file tương đối Data/keyAPI.ini, hướng dẫn QLTK mục khác; launcher/mobile/API chưa xác minh, không đọc key |

## Điểm chung / giới hạn

| Chủ đề | Bằng chứng / giới hạn |
|---|---|
| Lưu cấu hình | Nhiều menu có cờ Lưu, VIP có Save riêng; lưu bộ dữ liệu khác nhau nên chưa kết luận DUPLICATE (`AutoMap.cs:441`, `AutoPean.cs:263`, `AutoChat.cs:269`, `UnifiedAutoMenucs.cs:350`) |
| Nhãn | Goback/Fake TDLT/KOK không có giải thích tại menu (`AutoTrain.cs:365`, `:379`, `ModProCuongLe.cs:145`); cần xác nhận nhóm người dùng trước đổi tên |
| Kiểm thử còn thiếu | Bật/tắt liên tục, đổi map/đổi TK khi bot chạy, thiếu item/skill, nhập0/âm, restore RMS. Không suy an toàn tiền/vật phẩm chỉ từ toggle hoặc chứng nhận worker thread qua audit UI |
