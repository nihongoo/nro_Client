# HUD cụm 0 — nền layout dùng chung

Ngày 14/10/2026. Repo gốc `D:/project_Unity/client_Nro/Mod-nro-unity`. Branch `feat/hud-foundation`, tạo từ `origin/develop` **f42ac041a00a89eb1cabe6cf2ae31786c8bfa4ce**.

Commit code: `0a50e1b` — feat(hud): add shared anchors rect registry and platform layout flag. Commit báo cáo đi ngay sau; xem git log của branch.

## 1. Git và phạm vi

Đã fetch ở bước 0. Stage 3 đã vào develop qua squash PR #13. Theo chỉ dẫn mới của người dùng, bỏ điều kiện branch Đổi IP; không đổi branch feature/custom-server-address, không cherry-pick.

Không build Unity/APK, không compile C#, không chạy test có compile, không sửa server. Không push/merge/stash. Giữ font, thuật toán zoom MotherCanvas và giá trị/ý nghĩa Main.isPC, GameCanvas.isTouch. Không thêm UI/skin mới. Các file dirty local có sẵn được giữ nguyên và không đưa vào commit.

## 2. API

Class `Hud.HudLayout`, file `Assets/Scripts/Hud/HudLayout.cs`:

| API | File:line | Vai trò |
|---|---|---|
| IsMobileLayout / DesignReference | :36 / :38 | Getter theo Android/iOS; reference mobile 844×390 hoặc PC 1280×720 |
| PixelsToLogical(float/Vector2) | :43 | Pixel thật chia zoom hiện tại, không đổi zoom |
| FitDesignSize | :47 | Tùy chọn fit đều theo reference và viewport logic, tách riêng khỏi quy đổi pixel chính xác |
| Anchor | :52 | Neo trái/giữa/phải và trên/giữa/dưới, size/margin là đơn vị logic |
| Inset / ScreenBounds | :60 / :39 | Vùng nội dung và toàn viewport logic; dùng được cho panel cao toàn màn và overlay |
| Register | :65 | Đăng ký một lần: tên, provider Rect, thứ tự vẽ, visible predicate, có chặn pointer không |
| RegisterAnchored | :73 | Đăng ký neo với kích thước/lề logic cố định |
| RegisterDesignAnchored | :78 | Đăng ký neo bằng pixel thiết kế, quy đổi lại lazily khi đổi zoom |
| BeginFrame | :83 | Tăng generation cache trước xử lý UI; gọi tại GameCanvas.cs:593 |
| GetRect / TryGetRect | :114 / :120 | Cùng rect cho vẽ, hover, hit test, vị trí thành phần phụ thuộc |
| ForEachBackToFront | :128 | Duyệt thứ tự vẽ tăng dần |
| HitTest / IsTopmost | :137 / :147 | Duyệt ngược đúng danh sách vẽ, trả tên thành phần chặn trên cùng |
| CopyObstaclesAbove | :149 | Cấp vùng tránh của các thành phần phía trên một thành phần |

Tên trùng hoặc dependency rect vòng bị báo lỗi rõ ràng. Rect được tính một lần trong một generation, không tính lại riêng cho mỗi renderer/hitbox. Cache tăng mỗi frame; thay đổi GameCanvas.w/h, zoom, nền tảng hoặc offset keyboard cũng làm cache mất hiệu lực.

### Thêm một thành phần

Ví dụ cho cụm sau, chỉ định neo + kích thước + đăng ký rect **một lần khi khởi tạo owner**, không gọi Register trong Update/Paint:

```csharp
HudLayout.RegisterDesignAnchored(
    "hud.avatar", HorizontalAnchor.Left, VerticalAnchor.Top,
    new Vector2(42, 42), new Vector2(16, 16),
    order: 60, visible: () => ownerIsVisible);
```

Renderer lấy `HudLayout.GetRect("hud.avatar")`; input chỉ chạy khi `HudLayout.IsTopmost("hud.avatar", pointer)`. Không tự tính lại tọa độ/hitbox thứ hai. Renderer nhiều thành phần dùng `ForEachBackToFront`, xử lý tên thuộc renderer đó; các entry `legacy.*` chỉ là adapter tránh va chạm/capture, không vẽ lại chúng.

Chữ trang trí đăng ký `blocksPointer: false`. Hàng nút hoặc skill vòng cung: tính vị trí trong provider duy nhất của từng nút, đăng ký một tên/nút; mọi consumer dùng rect đã resolve. Panel full-height: provider neo Right/Top với size `(widthLogic, GameCanvas.h)`; overlay dùng ScreenBounds. Dropdown thu gọn: visible/size provider đọc trạng thái owner, lớp layout không tự quản lý nội dung hay toggle gameplay.

Margin trái/phải/trên/dưới dương đi vào trong màn hình; margin ở giữa là offset có dấu từ tâm. Không tự scale font, clip hoặc thay vị trí gameplay khi một thành phần lớn hơn viewport; owner cụm sau chịu trách nhiệm giới hạn kích thước nội dung.

## 3. Pixel thiết kế, zoom và safe area

- `PixelsToLogical(pixel) = pixel / max(1, mGraphics.zoomLevel)`.
- PC reference 1280×720 ở zoom 2 = 640×360 logic. Icon 42px = 21 logic; lề 16px = 8 logic.
- Mobile reference 844×390 ở zoom 2 = 422×195 logic. Kích thước reference là pixel thật, không bị coi là logic ngay từ đầu.
- `FitDesignSize` là cách fit đều tùy chọn cho toàn reference ở tỉ lệ khác: scale = min(logicWidth/referenceWidth, logicHeight/referenceHeight). Không dùng scale X/Y khác nhau, không sửa MotherCanvas. Chưa áp fit này lên các pilot vì phải giữ hình cũ.
- ScreenBounds dùng GameCanvas.w/h. ScaleGUI tiếp tục ánh xạ viewport safe area và pointer; **không trừ inset lần hai** trong HudLayout.
- Giữ toàn bộ ScaleGUI và MotherCanvas. Hạn chế đã có: nhánh UseSafeArea trong ScaleGUI hiện áp dụng Android/Editor, chưa bao gồm iOS player. Cờ layout iOS vẫn là mobile; không khẳng định đã sửa safe area iOS trong cụm này.

## 4. PC/mobile và các cờ cũ

IsMobileLayout dùng `UnityEngine.Device.Application.platform`, getter không có setter. Android/iOS = mobile, nền tảng khác = PC. API Device của Unity hỗ trợ nền tảng mô phỏng trong Device Simulator; Game view Editor thông thường vẫn PC. Không thêm checkbox Editor, RMS/preference, công tắc mod hoặc tham số override.

Tham chiếu API Unity 2022.3: [Device.Application.platform](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Device.Application-platform.html).

Các nơi dùng lẫn lộn giữ nguyên, chỉ báo cáo:

- Main.cs:98 nhận diện mobile, nhưng Main.cs:200 lại ép isPC=true; còn ảnh hưởng typeClient và fullscreen.
- GameCanvas.cs:416 ép isTouch=true, không đồng nghĩa thiết bị đang là mobile.
- GameScr.setSkillBarPosition (hiện khoảng :2002) chọn keySkill/onScreenSkill theo isTouch; paintSelectedSkill lại còn chọn theo Main.isPC.
- GameScr.paintTouchControl chọn ảnh chat theo Main.isPC; GamePad còn phụ thuộc isAnalog, không chỉ nền tảng.
- MotherCanvas.checkZoomLevel có nhánh Main.isPC rồi lại nhánh Application.platform Android/iOS. Không thay thuật toán này.

Cờ mới chưa thay các điều kiện legacy nói trên; các cụm giao diện PC/mobile sau dùng IsMobileLayout thay vì suy ra từ isPC/isTouch.

## 5. Registry mở rộng từ CollectChatObstacles và thứ tự vẽ/bấm

`GameScr.RegisterHudRects` (:671) đăng ký một lần các rect trước đây dựng trong CollectChatObstacles: chat, chuông, menu, skill, potion, context item, đánh, focus và vùng joystick. Với skill legacy, adapter lấy bao hợp của rect ảnh và rect touch cũ để giữ tránh vùng bấm lớn. Đây là adapter vùng tránh, chưa đổi renderer/handler chiến đấu.

`GameScr.CollectChatObstacles` (:713) nay gọi CopyObstaclesAbove; WorldChatLayout vẫn đi qua entry point này. Không còn một danh sách công thức obstacle song song. List tạm trong WorldChatLayout chỉ nhận bản sao rect từ registry.

Thứ tự hiện tại dùng cho các pilot và bridge:

| Order | Thành phần |
|---|---|
| 10 | Chuông — đang vẽ sớm trong GameScr.Paint |
| 20 | Thanh world chat |
| 30 | Skill, potion, đánh/focus/joystick legacy |
| 40 | Nút chat R |
| 50 | Menu/soft control legacy |
| 100 | Capture modal legacy: form, panel, menu, dialog, chat popup, thông báo, menu VIP |

Vẽ thấp → cao, hit test cao → thấp. Cùng order: đăng ký trước vẽ trước; đăng ký sau nhận bấm trước. Thành phần ẩn hoặc không chặn pointer không chiếm lượt input. Không gắn xử lý click vào một thứ tự quét khác; dispatch theo tên HitTest trả về.

Bridge overlay giữ quy tắc capture hiện có, gồm cả vùng ngoài hộp/panel; nó **không vẽ thêm overlay tối** trong cụm 0. Popup lời mời và các modal cũ vẫn giữ handler/paint ưu tiên hiện có. Không tuyên bố toàn bộ legacy HUD đã được chuyển sang renderer registry: chỉ ba pilot dưới đây chuyển cả nguồn rect và arbitration; combat và các modal sẽ chuyển ở cụm của chúng.

## 6. Ba vị trí đã chuyển thử

| Pilot | Vẽ / bấm / vùng tránh | Hình và hành vi giữ lại |
|---|---|---|
| Thanh world chat | WorldChatLayout.cs:9/:14; WorldChatHud.cs:40; key world-chat.bar | Neo đáy trái, cao 13 logic, rộng viewport. Bỏ 2px cũ nằm ngoài màn; phần nhìn thấy giữ nguyên màu/font. Ticker/queue/phí không đổi. |
| Nút chat R | GameScr.ChatButtonBounds :663; hover :4243, touch :4301, paint :5727 | Giữ 34×34 logic, lề phải 6/trên 2; màn <=240 giữ lề phải 1/trên 5. Rect gồm offset keyboard như ảnh vẽ. Xóa xC/yC và phép đặt tọa độ riêng cũ. |
| Chuông | NotificationCenter.cs:55/:62/:109; NotificationHudLayout.cs:14/:60 | Giữ thuật toán tìm vùng trống/DPI cũ, đăng ký provider duy nhất; paint/input/obscured check đọc cùng rect registry. Badge và tab không đổi. |

Bell xử lý input trước world chat nhưng chỉ nhận nếu HitTest xác nhận nó là trên cùng; do đó thứ tự gọi cũ không làm nút phía dưới nuốt input của pilot vẽ phía trên.

## 7. Đã xóa / còn lại

Đã thay/xóa trong phạm vi:

- Hai field xC/yC của chat, nhóm gán vị trí và công thức chat hitbox/center bị lặp.
- Các phép dựng rect rời trong CollectChatObstacles; chuyển thành provider đăng ký một lần.
- Công thức bar rect riêng và magic height 15 vượt màn 2px ở painter; vẽ/clip/hit dùng rect 13px nhìn thấy.
- Guard hiển thị world bar trùng ở painter chuyển thành IsVisible dùng chung với registry.

Chỉ liệt kê, **không xóa** theo yêu cầu:

- MainMod.paintListCharsInMap và lời gọi bị comment; listCharsInMap/controlTeleChar vẫn dùng, không coi cả nhóm là code chết.
- MainMod.smethod_7/smethod_8 rỗng.
- GameScr.getInjure rỗng.
- Nhánh G mở đồ họa không tới được vì nhánh G giao dịch return trước.

Ngoài phạm vi: cờ isPC/isTouch cũ, safe area iOS, các callback/painter chiến đấu, header/font mới, UI các panel Vật phẩm/Người và toàn bộ branch Đổi IP.

## 8. Kiểm tra và ma trận test tay

Chỉ rà source/tham chiếu, dependency rect, diff whitespace và hash các file được bảo vệ. **Không compile, không chạy Unity/Device Simulator, không build APK** theo yêu cầu. Tất cả case dưới đây chưa chạy trong đợt này, do người dùng build và test.

| Case | Kỳ vọng |
|---|---|
| PC 16:9 1280×720 | IsMobileLayout=false; quy đổi reference/zoom đúng; R/chuông/bar giữ hình/vị trí; click và hover khớp |
| Mobile 16:9 trong Device Simulator Android | IsMobileLayout=true, không chỉnh công tắc; ba pilot hoạt động, legacy HUD chưa đổi skin |
| Mobile 20:9 | Bar phủ đúng bề rộng logic, R neo phải, chuông không lấn điều khiển; không stretch méo icon |
| Tai thỏ / gesture bar / safe area | Không trừ inset hai lần; vùng bấm trùng hình trong viewport hiện có; thử tai thỏ trái/phải |
| Device Simulator iOS | Flag mobile đúng; ghi nhận hạn chế safe area iOS player cũ nếu test thiết bị thật |
| Resize cửa sổ PC rộng/hẹp | Cache rect cập nhật theo GameCanvas.w/h; R/bar neo lại, không còn xC/yC cũ giữ tọa độ |
| Thay đổi zoom theo cơ chế có sẵn | PixelsToLogical và RegisterDesignAnchored quy đổi lại; không dùng size pixel như logic; font/zoom game vẫn do code cũ |
| Click R, chuông, thanh đang có tin | Mỗi click chỉ vào thành phần trên cùng; không đồng thời di chuyển nhân vật |
| Mở/đóng thông báo và form Chat | Capture modal giữ nguyên; tab/OK/Đóng vẫn dùng được qua nhiều frame nhấn–giữ–thả |
| Lời mời đến, mở menu VIP/panel/dialog | Popup lời mời giữ ưu tiên; pilot bên dưới không bắt click xuyên overlay |
| Skill/joystick/potion sát thanh dưới | Bar nhường vùng tránh của điều khiển; không gửi chat khi bấm skill/đánh/đậu |
| Đổi map/relog, có/không có tin world | Không đăng ký trùng tên; bar không có tin không chiếm pointer |
| Kiểm tra foundation trong cụm kế tiếp | Register một entry; painter lấy rect/duyệt tăng order; HitTest chọn entry cuối cùng có chứa điểm; hidden/decor không chặn |

Giới hạn kiểm chứng: chưa xác nhận compile/runtime. Không thêm menu chẩn đoán, công tắc layout hay scene test phụ. Runtime resize/zoom của các điều khiển legacy chưa chuyển vẫn chịu hành vi cũ; chỉ cache và ba pilot của nền mới được cập nhật theo viewport.
