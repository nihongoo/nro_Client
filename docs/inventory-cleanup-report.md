# Dọn sau merge kho đồ — kết quả 18/10/2026

Thực hiện theo danh sách user duyệt. Client: `D:/project_Unity/client_Nro/Mod-nro-unity`. Server chỉ đọc ở `D:/dowload/SRC NRO NEW 03/SRC`; không sửa/build server. Không build Unity/APK, không stash, không push code hoặc merge commit; chỉ fast-forward develop và push --delete đúng10 remote được duyệt.

## 1. Branch/git đã làm

Fetch và xác minh origin/develop = **3075840** (#24); chuyển khỏi feat/inventory-detail, fast-forward local develop từ **f4e58a9 → 3075840**. Không xung đột. Tám file đang có thay đổi (logo, hai Packages, ProjectSettings, hai UserSettings, guest-account-design.md và báo cáo này) được kiểm tra SHA256 giữ nguyên từng byte qua switch/fast-forward. Sau đó tạo **chore/inventory-cleanup** từ3075840; không tạo clone/worktree phụ.

Đã xoá bằng `git branch -D` đúng11 local; `git push origin --delete` đúng10 remote. Danh sách xác nhận:

| Local đã xoá / tip trước xoá | Remote cùng tên đã xoá |
| --- | --- |
| feat/inventory-frame /46b4403 | Có |
| feat/inventory-bag /4c648ea | Không có remote từ trước, không gửi lệnh xoá tên này |
| feat/inventory-detail /065ca6d | Có |
| feat/hud-top-info /42ac840 | Có |
| feat/hud-top-actions /31d344f | Có |
| feat/hud-notification-panel /0e1e715 | Có |
| feat/hud-lists /d666199 | Có |
| feat/hud-combat-mobile /203f4fe | Có |
| feat/hud-combat-pc /d09d6f2 | Có |
| feat/hud-target-mobile /3f3de89 | Có |
| feat/hud-target-info-pc /fc012ea | Có |

Giữ develop/master và nguyên5 nhánh dưới. origin/HEAD vẫn trỏ origin/master; không sửa default branch. Giữ nguyên4 stash và worktree phụ.

## 2. So sánh nội dung5 nhánh với develop3075840 — không dùng git cherry

Đã đọc `git diff <merge-base> <branch>` để xác định phần nhánh thêm, rồi `git diff <branch> develop -- <file>` và source develop để phân biệt thiếu thật với viết lại. Toàn file khác không đồng nghĩa tính năng chưa có; chuỗi hunk không khớp cũng không dùng làm kết luận tự động. Bảng này thay kết luận patch-id của báo cáo khảo sát trước.

| Nhánh giữ / tip | Kết luận | Dẫn chứng diff nội dung và đề nghị |
| --- | --- | --- |
| feature/custom-server-address /ed03f4f | **Một phần theo toàn nội dung nhánh; chức năng Đổi IP đã đủ và được mở rộng** | Nhánh đổi action2 thành mở CustomServerDialog và thay nút Reset bằng Đổi IP. Develop hiện CustomServerAddress.cs:26–34 dùng action3 mở dialog, giữ action2 Reset; dòng178 có Đổi IP và179 có Về mặc định, thêm hit-test mobile. Không áp lại patch cũ làm mất nút thứ3. File docs/cleanup-change-ip-report.md của nhánh chưa có trong develop. Giữ, có thể đề nghị đóng nhánh sau khi user xác nhận tài liệu không cần |
| fix/dead-state-hud /e79d4cb | **Chưa có phần sửa của nhánh** | Develop không có Hud/DeadStateHud.cs/meta, không có DeadStateHud.Register/SyncCommand/SyncMenu hay guard !DeadStateHud.IsDead trong CombatMobileHud/CombatPcHud. Các diff GameScr/Menu/mScreen vẫn còn thiếu; không nhầm guard statusMe khi dùng skill với việc ẩn HUD. Giữ để xem xét PR riêng |
| fix/ulti-skills /177e808 | **Chưa có phần sửa của nhánh** | Develop Char.cs không có finishTuSatCharge/isTuSatCharge; diff còn cả ba hunk sửa kết thúc tụ/tách cooldown và Tests/Skills/run.py của nhánh. Giữ, chưa tuyên bố fix này đã lên develop chỉ vì runtime trước đây từng test |
| fix/login-batch3a /25b2d68 | **Một phần, nhiều phần đã viết lại** | GameCanvas.onSessionReplaced và các5 block thêm của nhánh đã có trong develop; Controller xử lý thay phiên/wait và MainMod launcher dùng đúng credentials, queue UI, timeout đã có dạng mới. Nhưng bounded state machine AccountManagerStep/Retry với trần3 lần, TryParse/server>=1 của nhánh chưa có nguyên chức năng: develop MainMod.Login vẫn vòng while và countdown retry. Không cherry-pick toàn nhánh lên lớp manualLoginRequired hiện tại; giữ, tách phần thiếu để review riêng |
| fix/ui-batch-c /e0407a4 | **Một phần, có phần thay thế bằng HUD mới** | S13 /admin đã xử lý bằng GameScr.cs:5883 `command == "admin"` sau parser lệnh, không còn Contains substring. S34 Từ chối đã có renderer/hit-test riêng trong PopUpYesNo.cs:49–83 (HudLayout/NotificationStore), không áp lại patch layout cũ. H25/H26 vẫn thiếu: UnifiedAutoMenucs.PanelHeight dùng15, paint/input header22, nhánh dùng HeaderHeight22 thống nhất. G20 còn khác: ChatPopup.perform develop vẫn notifyDestroyed cho mobile khi mở link; nhánh bỏ chỗ này. Giữ worktree/nhánh, chỉ đề nghị port phần còn thiếu riêng |

Vị trí trong bảng là file dưới `D:/project_Unity/client_Nro/Mod-nro-unity/Assets/Scripts/`, trừ tên file chỉ có ở tip nhánh được ghi rõ. Bản nhánh không có nghĩa phải phục hồi toàn bộ file cũ đè develop.

## 3. Stash và worktree — không apply/pop/drop hoặc xoá

So sánh `git diff stash^1 stash` để lấy thay đổi của stash và diff từng file stash↔develop; không coi toàn bộ snapshot cũ là phần stash muốn giữ.

| Stash / hash | Kết luận với develop | Nội dung/đề nghị |
| --- | --- | --- |
| stash@{0} /d1a9a4a | **Một phần** | CustomServerAddress đã có đủ hành vi Đổi IP bằng action3 thay cho action2 của stash; layout default-2022.dwlt còn khác, chỉ1/14 block thêm trùng trực tiếp. Giữ đến khi user xác nhận không cần layout cá nhân; không apply toàn bộ để lùi giao diện |
| stash@{1} /a9a997d | **Đã có đủ hành vi sửa, bằng code mới** | Chỉ có2 hunk CustomServerAddress đổi nút/handler; develop có Đổi IP + CloseConnection + CustomServerDialog.show, đồng thời giữ Reset. CustomServerDialog.show gán currentDialog nên không cần gọi endDlg như patch cũ. Có thể đề nghị bỏ stash sau duyệt; lần này giữ nguyên |
| stash@{2} /dcbb14c | **Chưa có trong develop** | Manifest/lock develop thiếu com.coplaydev.unity-mcp, com.unity.device-simulator.devices và entry newtonsoft-json bổ sung. Các giá trị bổ sung này hiện **đã trùng trong working tree cá nhân**, không phải đã commit develop. Giữ, không đưa package cá nhân vào cleanup |
| stash@{3} /5922203 | **Một phần** | AutoMap.cs và Npc.cs trùng toàn file develop; ba block vẽ nền GameCanvas và template danh sách server đã có. Endpoint LAN cũ khác endpoint hiện tại; Boss.cs cũ đã bị thay khỏi source, đường boss mới không được khôi phục mù quáng; isHuntingBoss/UI nút cũ không còn trong DoHoa hiện tại. MainMod có3/5 block thêm trùng, các cấu hình scene/project/editor/layout còn khác. Giữ bản backup hỗn hợp; nếu cần chỉ trích từng hunk còn giá trị, không pop cả stash |

Các tỷ lệ block là hỗ trợ đối chiếu text, không phải tỷ lệ chức năng. Không ghi thông tin credential hoặc giá trị cấu hình bí mật vào báo cáo.

Worktree chính hiện ở chore/inventory-cleanup, giữ thay đổi cá nhân. Worktree phụ **D:/project_Unity/client_Nro/Mod-nro-unity-ui-batch-c**, branch fix/ui-batch-c/e0407a4, vẫn sạch cả tracked/untracked; chính là nội dung một phần ở hàng tương ứng mục2. Không còn căn cứ gọi4 commit là hoàn toàn mới hoặc đã có hết chỉ dựa squash; vẫn giữ nguyên worktree và nhánh.

## 4. Hai commit code dọn đã duyệt

**Commit1: 0652ce4 — chore(inventory): remove unused legacy fields and stubs.** Chỉ sửa Panel.cs: bỏ subArray; mainTabPos/shopTabPos/boxTabPos/hasUseBag/currentButtonPress/screenTab6/itemInvenNew/imgStar9; bỏ điều kiện đọc itemInvenNew và phép load imgStar9; bỏ8 local flag…flag8 và4 if(1==0) rỗng trong setTextColor, giữ hàm/ánh xạ màu. Trước xoá đã đối chiếu tham chiếu; sau commit quét toàn Assets/Scripts và Assets serialized không còn các tên xoá.

**Commit2: chore(inventory): remove disabled inventory branches.** Bỏ13 IfStatement còn lại chứa isnewInventory bằng Roslyn syntax spans (nhánh true luôn false), giữ nguyên Statement của else thành scope để không đổi phạm vi local; nhánh thứ14 ở vị trí gốc2773 đã bỏ cùng itemInvenNew trong commit1. Vị trí gốc trong báo cáo duyệt:2682/2733/2773/2855/2883/2933/3541/6188/7025/8996/9008/9029/10731/10752. Bỏ eBanner cùng nhánh tạo/update bị tắt; bỏ field isnewInventory và phép gán false trong InventoryFrame.SyncInventoryFrame. Không xoá cả handler. Có7 dòng trắng do bỏ nhánh được làm sạch whitespace.

Vị trí source sau dọn: [Panel.setTextColor:10453](D:/project_Unity/client_Nro/Mod-nro-unity/Assets/Scripts/Panel.cs:10453); [InventoryFrame.SyncInventoryFrame:218](D:/project_Unity/client_Nro/Mod-nro-unity/Assets/Scripts/Hud/InventoryFrame.cs:218). Không xoá sprite nào. Các hàm doFireInventory/paintInventory/paintDetail/addItemDetail được so với develop để bảo đảm giữ nguyên nội dung; luongkhoa và tất cả sprite/field được yêu cầu giữ không bị sửa.

Kiểm tra không build: sau mỗi commit quét tên xoá trên toàn Assets scripts/serialized; phân tích ngữ nghĩa255 file C# không Emit; mô hình bố cục1.658 kiểm tra qua; ba nhóm fixture registry/input/Porata phân tích ngữ nghĩa qua, **không chạy assertion runtime C#**. Không chạy Unity hoặc tạo DLL/exe/APK. Cần user test kho, rương/combine/trade, chọn/di chuyển bàn phím, mặc/tháo/dùng sau dọn.

Code thừa phát hiện thêm, **chưa xoá ngoài duyệt**: `Panel.sellectInventory` tại Panel.cs:483 nay chỉ còn khai báo sau khi nhánh cũ bị bỏ; giữ lại để user quyết. setTextColor ở nơi khác/khối if(1==0) ngoài hàm được duyệt không dọn lan. Không sửa code tài khoản khách hoặc server trong lần này; guest-account-design.md giữ untracked riêng.

## 5. Khảo sát Set1/Set2 và MOVEFORPET — chỉ đọc

Các file client bên dưới ở `D:/project_Unity/client_Nro/Mod-nro-unity/Assets/Scripts/`; server ở `D:/dowload/SRC NRO NEW 03/SRC/src/nro/models/`.

| Đường | Packet và xử lý thực tế | Có luôn dùng được không? |
| --- | --- | --- |
| Thêm/xoá Set1/Set2 | Panel.doFireInventory:7429–7449 tạo Command AutoItem id4/5/6/7. AutoItem.perform:169–180 chỉ Add/Remove chuỗi Item.getFullName vào static List<string> set1/set2; **không gửi packet set**, không có handler server để lưu set | Không: panel chi tiết mới InventoryDetailPanel.BuildInventoryDetail:96 chỉ lấy AutoItem id1/2 cho đồ tiêu hao, không render id4–7 cho trang bị; menu legacy còn có đường tạo. Danh sách chỉ RAM, không thấy persist; tên+Lv/nhãn màu không phải ID món duy nhất |
| Mặc Set | MainMod.cs:1052–1061 bắt SHIFT_A/SHIFT_Z → AutoItem.useSet(0/1):277 → worker ChangeSet:296, duyệt túi ngược, getFullName khớp thì Service.getItem(4,index):309, Sleep100ms | Không: phải có set trong RAM và món đang trong túi; không bảo đảm đủ5 món, không phân biệt bản sao cùng tên, không nguyên tử/không có rollback. isChangingSet kiểm tra/gán ở worker không atomic và không có finally. Thông báo “Đã mặc” của hotkey không chờ ACK thành công |
| Packet mặc từng món | Service.cs:673 getItem gửi **-40, byte type=4, byte index túi**. Server Controller.java:557 chặn TransactionService.check rồi UseItem.getItem:90 đọc type/index; type4 → InventoryService.itemBagToBody:441 → putItemBody:333; trả bag/body và point/appearance | Server vẫn kiểm tra loại đồ hợp lệ, hệ, sức mạnh (option21 có thể ghi đè yêu cầu), điều kiện đồ đặc biệt. sendItemBags:607 gọi sortItems:612 sau từng món: index/túi có thể đổi trong lúc worker dùng mảng cũ. Sleep100ms không thay ACK; lag có thể mặc thiếu/sai thứ tự. Type/index byte có giới hạn, không coi là batch set ổn định |
| MOVEFORPET | Panel.doFireInventory:7411/7418 tạo Command id2005; Panel.perform:8874 dùng BAG_PET=6 (gán10677), Service.getItem → **-40, type6, index túi**. Server UseItem.getItem type6 → InventoryService.itemBagToPetBody:481 | Không: cần pet và power≥1.500.000; putItemBody kiểm tra loại/hệ/power của pet. Pet không được mount23/24/bông tai27; type11/25 còn hạn chế pet.type. Không đủ điều kiện trả thông báo/giữ món. C mới không render id2005 nên backend còn nhưng không có nút trong panel chi tiết này |

Server -40 không có một opcode Set1/Set2 riêng; mỗi món là một thao tác mặc độc lập. Không thấy guard chết riêng trong UseItem.getItem/InventoryService.itemBagToBody ở đường đọc này; không suy ra mọi trạng thái đều hợp lệ vì còn guard giao dịch và điều kiện trang bị. Không thử gửi packet hoặc sửa behavior.

Đề nghị cho phase2 set sau: xác định lưu set ở server/account hay client; dùng identity/index được xác nhận lại theo snapshot, tránh nhận diện chỉ getFullName; tuần tự theo ACK hoặc thiết kế batch server có validation rõ, thông báo kết quả từng món. Phải chọn scope set người chơi/đệ tử và xử lý đồ thiếu/trùng/túi đầy, chưa triển khai trong cleanup.

## 6. Trạng thái cuối

Chỉ commit source cleanup và báo cáo này, không stage logo/Packages/ProjectSettings/UserSettings hay guest-account-design.md. Giữ cả5 nhánh còn lại,4 stash,worktree phụ. Không push nhánh chore, không mở/merge PR mới, không build. Kết quả so sánh không phải phê duyệt xoá tiếp; chờ user quyết riêng những nhánh/stash còn lại.
