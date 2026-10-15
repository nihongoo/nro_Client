# Cụm 5b — HUD chiến đấu PC (15/10/2026)

## Git

Fetch origin: PR #19 đã squash vào `origin/develop` **6c0d26f**. Tree của develop, local và remote `feat/hud-combat-mobile` cùng **43e2bc91781bc8f83a79f89c0fb4b4881906e2d4**. Tạo `feat/hud-combat-pc` từ develop này; mang theo các file local đã có, không stash/commit/push/merge/xóa gì.

Ứng viên xóa **local + remote**, chỉ liệt kê để user duyệt; đã đối chiếu tree từng branch với commit squash trong develop:

| Branch | Commit develop có cùng tree |
|---|---|
| feat/hud-top-info | ae61050 / PR15 |
| feat/hud-top-actions | a4d8c3e / PR16 |
| feat/hud-notification-panel | 97468bd / PR17 |
| feat/hud-lists | ba1cc48 / PR18 |
| feat/hud-combat-mobile | 6c0d26f / PR19 |

Giữ nguyên develop/master/feature/custom-server-address/fix/login-batch3a/fix/ui-batch-c và worktree ui-batch-c. Đã in lại stash list; cả bốn stash client vẫn thuộc danh sách **giữ nguyên**, không có stash đề nghị drop:

- `d1a9a4a242b8bdce69c9f7645adcd2282d9df34a` — pre-invitation-p2a local address/layout.
- `a9a997d0a600ec9062acb7c7653783aaba17cf20` — change-ip-btn.
- `dcbb14c83bf39b0950f7ec31d93179cc0a642d6b` — local-packages-unity-mcp.
- `5922203f9a36a4d24d4b2cacf1479ab13726da02` — backup-before-custom-server.

Giữ stash server `6121b4b` theo quyết định cũ, không thao tác repo server. Nếu về sau được duyệt drop sẽ dùng hash và in lại list. Logo/Packages/ProjectSettings/UserSettings dirty từ trước vẫn giữ ngoài phạm vi.

## Đã làm / đã xóa

- `Assets/Scripts/Hud/CombatPcHud.cs:27`: đăng ký một lần các rect `pc-combat.*`, order30, chỉ hiện khi `!HudLayout.IsMobileLayout`; Center/Bottom, PixelsToLogical/RegisterDesignAnchored. ĐÁNH 64 tại288/622; chiêu52 tại366+58*i/628; item44 tại848+50*i/632. Không responsive-scale, nâng cụm hay thêm clip riêng. Vẽ/hover/bấm đều dùng rect registry, modal order100 thắng.
- `CombatPcHud.cs:67`: nền/viền/bo theo thiết kế, chữ ĐÁNH và font game giữ nguyên; chiêu được chọn có viền vàng. Icon/cooldown dùng tùy chọn khung bo của cache 5a (`CircularCombatIcon.cs:38`); minimal cover resample một lần vào đúng pixel đích rồi vẽ1:1, không kéo texture nhỏ mỗi frame. Bán kính trong =12px trừ viền2px. Cache32 icon/8 mask, key có bán kính để không lẫn vòng tròn; Destroy khi evict/đổi map/viewport/session, temporary render/readback giải phóng trong finally. Skill.paint/công thức cooldown không sửa.
- `CombatPcPointer.cs:22`, `Main.cs:334`, `GameScr.cs:2543`: capture chuột trái từ nút đầu tiên suốt nhấn–kéo–thả; chỉ kích hoạt nếu HitTest lúc thả vẫn đúng nút, skill/item assignment không đổi. Không phát world pointer cho chuỗi này; kéo từ map vào nút không kích hoạt/bấm xuyên. Hủy khi modal/resize/map/nhân vật/màn hình/pause; bàn phím đi nguyên đường cũ.
- `MobileQuickItems.cs:15/55/72`: dùng cùng templates[0..2], RMS host/port/charID, resolve lại arrItemBag mỗi lần vẽ/dùng như5a. Mở menu Gắn ô nhanh1/2/3 và Bỏ gán sẵn có cho PC; không tạo nguồn riêng. Item chưa gán/hết hàng không icon/badge/nhãn; dùng Service.useItem gốc và cooldown10s đã có ở5a. Helper `GameScr.cs:3714` chia sẻ lastUsePotion cho đậu từ ô nhanh PC; không sửa thân doUseHP/AutoPean.
- `CombatMobileHud.cs:167`: chỉ mở helper badge cho PC và thêm offset tùy chọn; PC truyền3px mỗi trục, mobile vẫn mặc định6px/4px. Chung skin tròn/đệm/font/căn giữa/99+, không thay painter/layout/input mobile. CircularCombatIcon giữ đường tròn mặc định Corner=0, chỉ PC chọn khung bo. GamePad/MobileCombatPointers/Skill/MyKeyMap/MotherCanvas không sửa.
- `GameScr.cs:655/1918/4120/5470`: xóa adapter legacy.skill/potion/potion-alt/context-item/attack/focus/joystick, LegacySkillBounds, toàn bộ đặt/vẽ/hitbox PC cũ, setTouchBtn/updateGamePad/paintGamePad và field tọa độ không còn dùng. setSkillBarPosition giữ API SoundMn gọi nhưng PC chỉ invalidate registry; mobile vẫn return ngay. xHP/yHP còn cho Hint.cs: chỉ nhận rect item1 trong painter PC, không dùng đặt/hit-test nút.
- Gỡ nút đổi mục tiêu PC cả vẽ/hitbox/registry; cmdFocus cũng null trên PC non-touch (`GameScr.cs:1008`) để Char/Controller không gắn lại softkey hình cũ. Logic còn ở `GameScr.cs:4297/4302/6089` (Tab/key16, key13/F2, command11001), cùng Char.findNextFocusByKey. Không sửa logic nhắm/auto/phím.
- Analog PC giữ chính sách cũ: chỉ hiện khi isAnalog!=0; cập nhật gamePad cũ khi touch-control bật, vùng rộng đăng ký `pc-analog.touch` **trước** các nút để không nuốt nút. Không thêm joystick khi analog tắt. Nút ngữ cảnh ngọc rồng/capsule193/194 giữ action cũ qua UseMobileHudContext (`GameScr.cs:6247`); rect phụ44 tại948/578, chỉ hiện khi flag tương ứng bật, không nâng cụm chính.
- World chat dùng registry tránh control như cũ (`WorldChatLayout.cs:11`). PC chuẩn zoom1/2: nút thấp nhất cách đáy34px, chat bar cao13 logical (13/26px), không chồng; không đổi thuật toán zoom hay thanh mobile.

## Ô → phím thật

Nguồn: MyKeyMap.cs:58/62 (Enter/Tab), GameCanvas.cs:1877/1894/1973 (map phím), GameScr.cs:2691 (keySkill), Mod.CuongLe/MainMod.cs:885 (F). Chữ kem #fff3da góc trên trái, inset3px, font tahoma_7_whiteSmall có sẵn, bóng tối1 pixel thật, không nền/scale/phím mới (`CombatPcHud.cs:117/128`). Badge quantity riêng ở dưới phải; chữ phím không có hitbox.

| Ô | Nguồn PC | Chữ phím (QWERTY, isMoveNumberPad=true) |
|---|---|---|
| ĐÁNH | keyPressed[25] qua Return/-5 | Enter |
| Chiêu1 | keySkill[0] | 1 |
| Chiêu2 | keySkill[1] | 2 |
| Chiêu3 | keySkill[2] | 3 |
| Chiêu4 | keySkill[3] | 4 |
| Chiêu5 | keySkill[4] | 5 |
| Chiêu6 | keySkill[5] | 6 |
| Chiêu7 | keySkill[6] | 7 |
| Chiêu8 | keySkill[7] | 8 |
| Item1/2/3 | templates[0/1/2], chưa có phím riêng | Không hiện |

keySkill[8/9] còn nguyên, vẫn dùng phím9/0 hoặc gán vào1–8 qua action11121. Space vẫn dùng đậu bằng doUseHP, F vẫn dùng item921/454 theo logic cũ, Tab vẫn đổi mục tiêu; không giả các phím này là phím của ô item. Khi isMoveNumberPad=false không hiện nhãn số. Nhánh non-QWERTY cũ map7/8 cho hai ô đầu; nhãn phản ánh7/8. Lỗi cũ phím9 non-QWERTY truy cập keySkill[21] trên PC chỉ ghi nhận ngoài phạm vi, không sửa/không quảng bá phím đó.

## Cửa sổ hẹp / còn lại ngoài phạm vi

Cụm chính span704px (288–992). Ngưỡng đủ toàn bộ khung là **GameCanvas.w*zoom >=704** (logical >=704/zoom); tính cả badge cuối nhô3px thì cần **>=710px**. Dưới ngưỡng vẫn Center/Bottom, giữ kích thước và khoảng cách, không fit-scale/clip nội bộ; phần vượt viewport không thể hiện ngoài cửa sổ. Ở1280×720 đúng tọa độ thiết kế;1920×1080 dịch+320 ngang/+360 dọc. Viewport hẹp chồng các HUD khác tùy rect thực; registry vẫn quyết định order.

Không xóa ngoài phạm vi: asset nút cũ imgSkill/imgFire/imgFocus/imgHP còn load (một số dùng ở CrackBallScr/mobile); legacy.screen-menu/target-menu/overlay thuộc cụm khác; đường non-QWERTY lỗi nêu trên; Hint.cs:471/534 vẫn hướng dẫn đậu kiểu cũ dù ô1 có thể gán item khác. Không tự đổi tutorial/phím hay source gameplay để xử lý các phần này.

## Test tay — tất cả CHƯA CHẠY

| Môi trường / ca | Cần xác nhận |
|---|---|
| PC1280×720,1920×1080; resize rộng/hẹp qua704/710px | Đúng neo/size/lề đáy, không nâng cụm, ngưỡng overflow đúng; không đăng ký trùng |
| Chuột lần lượt chiêu1–8; phím1–8 và9/0 | Đúng chiêu, viền chọn đúng, hồi chiêu khớp khung bo; phím giữ nguyên |
| ĐÁNH bằng chuột/Enter; Tab/F/Space | Hành vi cũ; không còn nút đổi mục tiêu; analog0 ẩn joystick, analog1 vẫn theo cấu hình cũ |
| Gán/bỏ gán cả ba item, đổi stack/số lượng/dùng hết | Đúng icon/số lượng, badge1/99/99+ cân và nhô3px; ô trống/hết hàng không icon/badge; không nhãn tên, không chữ phím bịa |
| Item cooldown, trùng item nhiều ô/đậu nhiều cấp | Chung timer như5a, hết10s dùng lại được, quantity do server cập nhật |
| Icon sáng, hover/giữ/kéo/thả trong-ngoài nút | Chữ kem/bóng đọc rõ, không che quá khoảng1/3 icon; hover nhẹ; chỉ click đúng capture mới dùng |
| Modal/panel/thông báo mở giữa nhấn và thả | Không bấm xuyên/không dùng skill/item, đóng overlay không phát lại click cũ |
| Bấm nút khi map có người/item; chat đáy đang chạy | Không chạy tới điểm bấm, không chọn người/item/gửi chat xuyên; chat không chồng cụm |
| Đổi map/relog/resize/pause | Không action cũ/đăng ký trùng/rò cache, gán item đúng nhân vật |
| Android Device Simulator16:9/20:9, tai thỏ trái/phải; S22 | HUD5a giữ bố trí/icon tròn/item nổi/badge/joystick+chiêu multi-touch/cooldown, không hiện HUDPC |

Đã kiểm tra tĩnh: diff --check; delimiter; so thân hàm gameplay và nhánh phím với HEAD; so các entry mobile; tính neo/kích thước/overflow. **Chưa compile, chạy game/Simulator, build Unity/APK.** Không sửa server, không commit/push/merge/stash/cleanup Git.

## Chỉnh theo ảnh test — 15/10/2026

Nút ĐÁNH PC dùng cặp asset nắm đấm GameScr.imgFire0/imgFire1 giống mobile, bỏ nền vuông/chữ ĐÁNH vẽ tay. Giữ rect64×64, nhãn phím Enter nhỏ và hành vi click/phím; hover/giữ dùng asset trạng thái nhấn. Copy Bilinear riêng cache tối đa hai texture, Destroy khi source đổi hoặc cache PC được dọn; không sửa asset/painter mobile.

Badge PC nhô **6px ngang / 12px dọc** thay cho3px/3px, giảm che icon. Mép phải badge ở đúng mép trái ô tiếp theo (gap6px), tránh đè sang icon bên cạnh; kích thước/font/padding/99+ giữ nguyên. Tính cả badge cuối, ngưỡng đủ khung/badge cập nhật thành **716px** (logical716/zoom). Badge thấp nhất còn cách đáy32px, chat bar PC zoom2 cao26px nên còn khoảng6px.

Test tay bổ sung **chưa chạy**: nắm đấm thường/hover/giữ/thả và Enter vẫn đánh đúng; badge1/99/99+ ở ba ô ít che icon, không đè ô bên cạnh/chat đáy khi resize. Chưa compile/chạy/build; không commit/push/merge/stash.

## Viền chiêu được chọn rõ hơn — 15/10/2026

CombatPcHud.PaintSelectedSkill thay viền vàng3px cũ bằng viền kem sáng4px, vòng vàng đậm2px ngoài khung và mép nâu tối tương phản ở ngoài cùng3px. Luôn hiện theo skill==Char.myCharz().myskill, vẽ sau icon/cooldown, không nhấp nháy; giữ icon/rect/hitbox và toàn bộ mobile. Vòng ngoài chỉ trang trí, dùng cache khung bo sẵn có, không có entry pointer riêng.

Test tay chưa chạy: chọn lần lượt1–8 rồi thả chuột vẫn thấy rõ đúng ô chọn trên icon sáng/tối, trong cooldown và khi hover ô khác; vòng ngoài không che icon ô kế bên. Diff check tĩnh đạt; chưa compile/chạy/build, chưa commit/push/merge/stash.

## Căn nhãn phím và tăng tương phản — 15/10/2026

Theo ảnh test mới, nhãn PC chuyển từ chữ nổi/bóng đơn sang chữ kem trên nền nâu tối gần đặc, bo4 pixel. Cùng inset5px từ góc trên trái rect; các số1–8 dùng chung chiều rộng đo theo chữ số rộng nhất, cùng chiều cao/baseline/padding. Enter dùng cùng lề/chiều cao và chiều rộng vừa nội dung. Nền và chữ native cùng một rect pixel đã snap, bỏ lệch do mFont x--/yAdd; dùng chính font game, fontSize0 (cỡ gốc), không scale. Zoom1 giữ bitmap font hiện có. Nền nhỏ chỉ quanh nhãn, không có entry/hitbox riêng; metric/style/texture nền được cache và dọn cùng cache PC, không thay gameplay/mobile. Đây là điều chỉnh nền nhãn theo yêu cầu dễ đọc mới, thay thiết kế chữ không nền ban đầu.

Test tay chưa chạy: so hàng chữ1–8 và Enter, icon sáng/tối và viền chọn/cooldown; nhãn cân/không bị cắt, nền chỉ che diện tích nhỏ, resize zoom1/2 vẫn đọc rõ. Kiểm tra tĩnh delimiter/diff đạt; chưa compile/chạy/build, chưa commit/push/merge/stash.

## Nhãn phím tối, không nền — 15/10/2026

Theo yêu cầu mới nhất, bỏ hoàn toàn nền bo tối và cache texture nền nhãn PC. Chữ dùng **#28180c** (RGB40/24/12, cùng màu gốc PanelColor của danh sách vật phẩm, alpha255), áp dụng cả GUI font và bitmap zoom1. Giữ cùng lề/khung căn chữ/baseline và font game, không bóng/nền, không thay mobile/phím/hitbox. Test tay đọc nhãn trên icon sáng/tối chưa chạy; kiểm tra diff tĩnh đạt. Chưa compile/chạy/build, chưa commit/push/merge/stash.

## Nhãn sát góc / ENT — 15/10/2026

PC KeyLabel căn UpperLeft trực tiếp hộp glyph, bỏ phần đệm/metric căn giữa còn lại từ nền nhãn cũ. Lề ngang2px/dọc1px từ rect, cùng vị trí tương đối cho mọi ô; bitmap zoom1 dùng cùng lề. Nhãn Enter rút thành **ENT** (viết tắt của Enter), không đổi phím thật. Giữ màu#28180c, font/cỡ gốc và không nền; mobile/input/hitbox không sửa. Test tay mới chưa chạy; chưa compile/chạy/build, chưa commit/push/merge/stash.

## Tùy chọn bật/tắt nhãn — 15/10/2026

Thêm dòng **Nhãn phím HUD PC** có checkbox ở cuối bảng Cấu hình/cài đặt gốc (`SoundMn.cs:259`, `Panel.cs:10059`). Mặc định bật, click đổi trạng thái và cập nhật dòng ngay; lưu RMS riêng `hud-pc-key-labels` (0=tắt,1=bật, chưa lưu=-1 vẫn bật). Getter load một lần trong CombatPcHud.cs:11, KeyLabel:178 chỉ bỏ vẽ chữ khi tắt; nút/icon/cooldown/badge/hitbox/phím thật không đổi. Dòng mới chỉ xuất hiện khi !IsMobileLayout, không dịch index các tùy chọn cũ; nhánh mobile giữ nguyên danh sách/hành vi.

Test tay chưa chạy: mở Cấu hình, bỏ chọn rồi về game không còn nhãn1–8/ENT; phím/click vẫn dùng được; mở lại game giữ tắt; bật lại hiện đúng; bật/tắt âm thanh/hào quang không mất dòng mới/không đổi trạng thái nhãn; Android Simulator không có dòng này và HUD5a giữ nguyên. Đã kiểm tra tĩnh code path/RMS default/diff/delimiter; chưa compile/chạy/build, chưa commit/push/merge/stash.

## Giữ thứ tự chiêu / số ô trống — 15/10/2026

Nguyên nhân xác nhận trong source: AutoSkill static constructor đặt isLoadKeySkill=true (AutoSkill.cs:475); Update mỗi20tick gọi LoadKeySkills, hàm này gán keySkill[i] theo nClass.skillTemplates[i] rồi saveKeySkillToRMS từng vòng. Vì vậy mở lại client có thể ghi đè thứ tự người chơi vừa lưu bằng thứ tự hệ và gửi nó về server. Sửa AutoSkill.cs:70: PC không chạy bước tự xếp/gửi này; nhánh mobile vẫn chạy y như cũ, các phần auto đánh/dùng chiêu/target không thay.

Luồng gán/lưu thật giữ nguyên: Panel.cs:9169 action9003 → doSetKeySkill → GameScr.cs:6171 action11121 ghi keySkill[index] → saveKeySkillToRMS:1380 → Service.changeOnKeyScr(-113). Tên hàm có RMS nhưng thực tế **không ghi RMS client**; loadSkillShortcut:1223 trống, dữ liệu được Controller.cs:5936 nhận từ gói KSkill rồi onKSkill:1250 khôi phục đúng index bằng template.id. Không thêm thứ tự mặc định mới hay cache Skill object. Server chỉ đọc để đối chiếu: models/server/Controller.java:355 nhận10ô; models/skill/PlayerSkill.java:37 gửi KSkill/OSkill; models/database/PlayerDAO.java:724 đưa skillShortCut vào dữ liệu lưu. Không sửa server/protocol/onKSkill/luồng bắn chiêu.

Asset gốc đã xem `Assets/Resources/res/x2/mainImage/myTexture2dskill.png`: chỉ là khung trống, không có chữ số; painter gốc vẽ khung này và menu gán dùng tên Vào phím1..0 (T1.cs:200), không có asset số ô riêng tại luồng đó. CombatPcHud.PaintEmptySkill (chỉ skill==null) vẽ **số1–8 ở giữa ô bằng font game**, bóng tối để đọc rõ. Số là thứ tự ô, luôn hiện cả khi tùy chọn Nhãn phím HUD PC tắt; nhãn phím góc vẫn theo tùy chọn. Ô đã gán không có số giữa; không thay icon/hitbox/action hoặc item placeholder/mobile.

Test tay mới **chưa chạy**: xếp thứ tự bất kỳ có ô trống, thoát game/mở lại và relog cùng nhân vật giữ đúng10index (HUD hiển thị8ô); phím1–8/9/0 vẫn đúng chiêu; nâng cấp chiêu không mất vị trí; đổi nhân vật dùng dữ liệu server của nhân vật đó; ô trống hiện đúng số thứ tự1–8 khi nhãn phím bật/tắt, gán vào thì số giữa biến mất; Android giữ nguyên luồng cũ. Đã kiểm tra tĩnh guard PC/mobile, chuỗi lưu/nhận và diff/delimiter. Chưa compile/chạy/build, chưa commit/push/merge/stash.

## Gỡ menu gốc PC, giữ thứ tự mobile và gỡ item từng ô — 15/10/2026

Theo yêu cầu mở rộng mới, bỏ nút menu gốc cả PC/mobile: Char.task gán left=null; xóa legacy.screen-menu, hover và flare menu cũ trong GameScr. Giữ cmdMenu/command11000 và đường mở menu bằng avatar (TopInfoHud), không gỡ menu ngữ cảnh Char.cmdMenu khác.

Lỗi tự xếp thứ tự chiêu cũng có trên mobile: xóa hoàn toàn cờ isLoadKeySkill, bước Update và hàm LoadKeySkills tự gán theo nClass rồi gửi lưu lúc mở app. Hai HUD giữ thứ tự nhận từ KSkill/OSkill và luồng gán/lưu server hiện có; không sửa server/protocol/logic đánh/auto dùng chiêu. Phần này thay giới hạn chỉ sửa PC ở cập nhật trước theo yêu cầu mới của user. Thứ tự đã bị ghi đè trước đây cần gán lại một lần.

Ba ô item trống/hết hàng hiện số1/2/3 **từ trái sang phải**, bằng font game ở giữa khung, không phụ thuộc nhãn phím. Menu gán ghi thêm vị trí trái/giữa/phải. PC chuột phải vào ô đã gán; mobile giữ ô0,6s trong cùng rect, lệch không quá8designpx → mở menu game có Gỡ ô nhanh N và Hủy. Chuột trái/tap ngắn vẫn dùng item như cũ. Giữ mở menu hủy capture/queue, không dùng item khi thả và modal dừng joystick. Gỡ chỉ templates[slot]=-1 và lưu RMS/invalidate, không gọi Service/useItem, không xóa quantity/timer. Menu kiểm tra owner và template để không gỡ nhầm sau khi đổi nhân vật/gán lại; cùng item ở nhiều ô chỉ gỡ ô chọn. Menu Bỏ gán ô nhanh trong túi cũ vẫn dùng được.

Nguồn sửa: Char.cs (gán left); GameScr.cs (registry/painter menu cũ); Mod.CuongLe/AutoSkill.cs (khởi tạo thứ tự); Hud/MobileQuickItems.cs (ShowSlotMenu/RemoveSlot/Persist), CombatPcPointer.cs (chuột phải), MobileCombatPointers.cs (hold), CombatPcHud.cs/CombatMobileHud.cs (số ô item).

Test tay mới **chưa chạy**: avatar mở menu, không còn nút/flare/hitbox menu cũ; xếp chiêu PC/mobile rồi thoát/mở/relog giữ đúng; gỡ từng ô1/2/3, trùng item nhiều ô chỉ mất ô chọn; Hủy không thay gán; giữ/thả không gửi dùng item/trừ quantity, tap ngắn vẫn dùng; gỡ/gán lại không reset cooldown; hết hàng vẫn gỡ được; placeholder1–3 đúng trái→phải sau gỡ/mở lại; modal/joystick+tap không hồi quy. Kiểm tra tĩnh delimiter/diff và so AutoSkill chỉ bỏ bước tự xếp; chưa compile/chạy/build, chưa commit/push/merge/stash.

## Menu item đã gán / placeholder chiêu mobile — 15/10/2026

Menu trong túi giờ đọc templates đang lưu trước khi tạo lệnh: item đã có ở bất kỳ ô nhanh nào chỉ thêm **Bỏ gán ô nhanh**, không thêm Gắn ô1/2/3; item chưa gán chỉ có ba lệnh Gắn ô, không có lệnh bỏ gán. Handler cũng chặn gán một template đã gán, tránh menu cũ bypass; muốn đổi ô phải gỡ trước. Item trùng nhiều ô từ bản cũ vẫn được bỏ gán hết qua menu túi; thao tác gỡ riêng từng ô trên HUD giữ nguyên. Không sửa dùng item/cooldown/quantity.

CombatMobileHud.PaintEmptySlot dùng chung font/căn giữa cho **chiêu trống1–8** và item trống1–3; số thứ tự độc lập tùy chọn nhãn phím PC, chỉ hiện khi ô không có skill/item. Không đổi rect, icon/cooldown hoặc input.

Test tay bổ sung chưa chạy: item chưa gán → menu có ba lựa chọn; gán rồi mở lại → chỉ Bỏ gán; gỡ rồi gán ô khác được, không tạo trùng qua menu cũ; mobile mọi ô chiêu trống đúng số1–8, gán chiêu số giữa biến mất. Kiểm tra tĩnh diff/delimiter; chưa compile/chạy/build, chưa commit/push/merge/stash.
