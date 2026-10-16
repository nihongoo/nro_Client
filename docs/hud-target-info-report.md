# Cụm 6 — Thẻ mục tiêu và danh sách Người/Vật phẩm

Ngày 16/10/2026. Branch giữ nguyên `feat/hud-target-mobile`, base `origin/develop` 015c672. Hai phần đầu commit riêng, các chỉnh sửa theo ảnh test được commit bổ sung. User đã duyệt commit/push/tạo PR ngày16/10/2026; không sửa server, không merge/stash. Các thay đổi logo, Packages, ProjectSettings, UserSettings có sẵn được giữ ngoài commit.

## Phần 1 — Thẻ mục tiêu mobile

- `Assets/Scripts/Hud/TargetMobileHud.cs:114` Register/Bounds/PaintPanel/BuildPanel: một provider `target-mobile.bounds`, Center/Top, order10, **blocksPointer=false**. Rect con chỉ là các lát nội bộ của provider, không đăng ký thêm. Kẹp đối xứng giữa top-info và hàng nút phải, không đẩy thẻ xuống che list. HP ưu tiên192px, tối thiểu160px; không đủ khoảng trống thì chỉ còn map title.
- Nền tab hai bậc: thân trên212px + vai cong14px mỗi bên, mép HP cách thân10px; bậc dưới cố định theo viewport (thân trên−52px, tối thiểu140px khi màn hẹp), tên/pill thay đổi nội dung trong khung này. Tên quá dài dùng vùng hiển thị có cuộn; màn hẹp giảm bậc thụt từ26 xuống20px mỗi bên để vẫn giữ bậc dưới140px. Hàng dưới cách HP3.5px. Cao58px thiết kế khi font native cho phép; viewport rất thấp phải tăng đến chiều cao chứa đủ ba hàng font nguyên cỡ, không cắt/scale font.
- Đường bao polygon có vai Bezier lõm, gờ nối ngắn, tất cả góc chuyển bậc/đáy dùng đường cong quadratic bo7px; sai số lấy mẫu đường cong <=0.75 pixel thật, coverage theo signed distance khử răng cưa1px. Nền #2b1a0a alpha88%, viền #f2b45a1.5px, không kẻ ngang mép trên. Texture RGBA32 đúng pixel đích qua ScreenPixels, không scale sprite nhỏ; cache một texture theo kích thước trên/dưới, height/step/bo/zoom và kích thước pixel safe-area. Hủy texture trước khi dựng lại, dọn khi đổi map/viewport/session qua MobileCombatPointers.Cancel; không tạo texture/array/path/key-string mỗi frame.
- Giữ nguyên painter HP đỏ/track, fraction/căn giữa `HP + số`, font game và marquee từng vùng. Tên khu dùng font thường kem mờ. Chưa có mục tiêu/NPC/item: chỉ map title nhỏ có bóng tối, không nền và không chặn pointer. Char còn sống: tên/HP/sức mạnh thật (thiếu dữ liệu tạm `SM --` cho người chơi); Mob không có field sức mạnh nên ẩn pill. Boss dạng Char ID âm cũng ẩn pill nếu chưa có số. Mục tiêu chết/status chết/ẩn/ra khỏi vector map hoặc game bỏ focus thì thẻ ẩn; không tự xóa focus, không đặt phạm vi hoặc timeout mới.
- `Assets/Scripts/GameScr.cs:655`: đăng ký/paint thẻ; VS head cũ chỉ nhường khi thẻ mobile đang hiện, countdown đặt dưới thẻ. PC giữ cũ. `Assets/Scripts/Hud/MapListsHud.cs:144`: map title nhường cho thẻ đang hiện, fallback mobile có bóng tối.
- `Assets/Scripts/GameCanvas.cs:600`, `Controller.cs` case−79: giữ phần đang làm dở lấy sức mạnh qua API sẵn `Service.getPlayerMenu(id)` cho Char chưa biết; kiểm ID/object/owner/map/zone, giới hạn750ms/yêu cầu,2lần/đối tượng (retry5s),32entry. Không mở menu; không đổi handler PC/server.

Đã xóa: bộ nền Seam/tai thỏ/vát cũ và nhánh fallback hộp bo, các rect map/HP/name/power đăng ký riêng, capture chặn pointer thẻ, nhánh viền hộp cũ không còn dùng. Giữ mask HP/pill để không thay style thanh. Ngoài phạm vi không xóa: head/VS PC, tên/HP trên đầu nhân vật, logic focus và dữ liệu sức mạnh server không gửi.

## Phần 2 — Số liệu và nguyên nhân danh sách thiếu hàng

Nguồn: MapListsHud.ListBox.RowHeight/HeaderHeight/HeaderBounds/BottomLimit/BodyBounds, CombatMobileHud.Register/DesignUnit, TopInfoHud.ResolveBounds, TopActionsHud.ChatBounds, MotherCanvas.checkZoomLevel. Đơn vị bảng dưới là **logical**; đổi sang pixel canvas bằng nhân zoom. N >=5, panel mở, combat hiện, analog mobile bật, PC analog tắt, không item ngữ cảnh. 20:9 chọn rõ **2400×1080** (20:9 chỉ là tỉ lệ, không xác định resolution/zoom).

**Đây là phép tính tĩnh, không phải số đo Unity đang chạy.** Bảng dùng tình huống font thường F=11, đậm B=10 logical. Asset FontSys chelthm/barmeneb hhea + fontSize cho x2/x4 xấp xỉ các giá trị này, nhưng GUIStyle.CalcSize thực chỉ có khi user chạy; không thể khẳng định font height/safe-area của ảnh lỗi khi chưa có số runtime. Width budget top-info được đối chiếu advance của asset font; thay F/B thực vào công thức dưới nếu khác. Zoom2 cho844×390/1280×720, zoom4 cho2400×1080 theo MotherCanvas hiện có; không đổi thuật toán.

### (a) Số hàng tối đa

Trước và sau sửa: cả mobile/PC, cả Người/Vật phẩm đều tối đa **5 hàng nguyên**, **6 slot renderer** để có thêm một hàng khi scroll lệch hàng. Trước sửa trần pixel còn giảm số hàng thực; không phải code chỉ cho phép1 hàng. Sau sửa `rows=min(5,N,floor(max(0,available)/RowHeight))`. N=0 ẩn cả header/body; N ít thì body đúng N hàng, phần dư cuộn và có rail.

### (b), (c) Kích thước và chỗ trống cụ thể

`U(p)=max(1,Round(p/zoom))`, `Gap=U(4)`; header `max(U(24),B+U(8))`; hàng item `max(U(32),F+U(4))`, hàng người `max(U(28),F+U(4))`. Top thân = header.yMax+Gap. Giới hạn đáy = min(h−13−Gap, obstacle.yMin−Gap) **chỉ với obstacle giao ngang chiều rộng panel và còn nằm dưới top**; available = đáy−top. Khi một obstacle che ngay top, available=0. World bar vẫn dự phòng13logical như cũ.

Cũ: obstacle lấy mọi rect chặn input order30, gồm joystick-touch từ `y=(h/2)>>1` (~h/4), trong khi vòng joystick vẽ ở244/390 chiều cao. Header cũng bị kẹp bởi vùng này. Thân cũ = `floor(max(0,min(U(160 hoặc140),available))/RowHeight)*RowHeight`, rồi giới hạnmin(5,N)hàng. Mới: chỉ lấy bounds **đang vẽ**; joystick dùng vòng thật, PC analog tùy chọn dùng extent hai ảnh thật từ GamePad.HudVisualBounds; không đổi vùng bắt joystick và vùng tránh world chat. Bỏ trần160/140, giữ max5. Giữ vị trí/chiều rộng/header/chiều cao từng hàng/style cũ.

| Khung / list | w×h logical; zoom | Header / hàng | Top thân cũ → mới | Available cũ → mới | Thân cũ → mới | Hàng cũ → mới |
|---|---|---|---|---|---|---|
| Mobile844×390 / item | 422×195;2 | 14 /16 | Ẩn →56 | 0 →64 | 0 →64 | 0 →4 |
| Mobile844×390 / người | 422×195;2 | 14 /14 | 54 →54 | 25.5 →25.5 | 14 →14 | 1 →1 |
| Mobile2400×1080 / item | 600×270;4 | 12 /12 | 51 →51 | 15 →116.92 | 12 →60 | 1 →5 |
| Mobile2400×1080 / người | 600×270;4 | 12 /12 | 50 →50 | 61.85 →61.85 | 24 →60 | 2 →5 |
| PC1280×720 / item | 640×360;2 | 14 /16 | 56 →56 | 289 →289 | 80 →80 | 5 →5 |
| PC1280×720 / người | 640×360;2 | 14 /14 | 54 →54 | 291 →291 | 70 →70 | 5 →5 |

Giải số:844 item cũ joystick-touch bắt đầu48, đáy46, header minTop40 cao14 không lọt nên ẩn; mới vòng joystick bắt đầu122, đáy120, body56 →64 còn trống.844 người vẫn chạm bounds chiêu8 ở81.5, đáy79.5, body54 →25.5: **không đủ3 hàng42**, không ép chồng chiêu.2400 item cũ joystick-touch y67 →đáy66−body51=15; mới joystick y168.92 →đáy167.92−51=116.92.2400 người chiêu8 y112.85 →đáy111.85−body50=61.85, đủ5×12 nhưng cap cũ U(140)=35 chỉ cho2. PC hai cột không giao cụm chiến đấu giữa đáy nên giới hạn world bar y347−Gap2=345; body56/54 →289/291.

Kích thước pixel thân mới tương ứng:844 item128px/người28px;2400 cả hai240px;PC item160px/người140px. Kích thước header pixel lần lượt28/48/28px. Nếu vẫn muốn844 người có3 hàng phải giảm/move hàng nút phải hoặc cụm chiêu/font, trái phạm vi giữ vị trí/kích thước lần này; hiện một hàng + scroll là giới hạn hình học thật, được ghi lại thay vì che điều khiển.

### (d) Nguyên nhân gốc và sửa cuộn

Hai lỗi thật trong source: **nhầm rect bắt joystick rộng với vòng hình vẽ** khi giới hạn chiều cao; **cap160/140px cố định** trong khi hàng còn có min-height theo font native. Tình huống2400 ở bảng tái hiện chính xác item1hàng dù N>=3 và vùng hình vẽ còn dư. Ảnh user không kèm resolution/zoom/F/B nên không gán khẳng định một bộ số cho mọi thiết bị; bảng844 còn cho thấy cũ có thể ẩn cả header item. Không thấy lỗi slot vẽ chồng: full.y=body.y+index*RowHeight−Scroll, hit/icon/text đều clip với body; First=Scroll/RowHeight, max6 slot. Header N lấy chính drops.Count/people.Count sau cùng bộ lọc dùng cho renderer, không lấy tổng map.

- HudLayout.CopyVisualControls chỉ dùng cho list; API obstacle input/world-chat cũ giữ nguyên. GamePad chỉ thêm getter đọc bounds ảnh, không đổi update/paint/movement.
- MapListsHud.BodyBounds bỏ cap sai, giữ tối đa5; rail có sẵn vẫn hiện khi N*row>body.height. SetScroll clamp lại sau refresh/count/resize. Giữ bộ lọc ±120 ngang, −90/+60 dọc theo tọa độ thế giới, trạng thái ẩn và lọc vệ tinh mobile; không nới phạm vi để tăng N. Hàng item focus nay tô theo itemFocus như hàng người.
- MobileCombatPointers.Feed: gesture bắt đầu trên list giữ stream đến thả dù đi ra joystick/skill hoặc ra khỏi list; không chuyển owner sang controls/map. Modal/loading/map change/cancel hủy capture, không phát lại release. MapListsHud.Consume chặn GameScr.updateKey trước gameplay; lăn chuột chỉ nhận khi HitTest vẫn là list trên cùng và chuột trong body. Sự kiện cuộn được consume, không chạy tới điểm bấm hoặc chọn mục tiêu xuyên.

Đã xóa cap chiều cao160/140 và việc dùng broad touch zone trong tính chiều cao list. Ngoài phạm vi giữ nguyên: toàn bộ world chat obstacle policy, vùng bắt joystick, nguồn/bộ lọc MainMod cho caller khác, style và vị trí panel, mục tiêu/nhặt đồ khi bấm hàng như cũ. Không cần thêm UI cuộn vì rail mảnh đã có.

## Kiểm tra

Đã kiểm tra tĩnh delimiter/encoding/diff; containment hàng nội dung trong tab;30 tổ hợp số lượng/bố cục và các offset cuộn; tái hiện hai phản ví dụ cap/broad zone bằng công thức. So thân doFire/doUseHP/doSelectSkill/doUseSkill/checkAuto/checkClick/checkDrag/updateKey và GamePad.update/paint/disableCheckDrag/disableClickMove với commit phần1: không đổi. Preview hình học offline không phải ảnh chạy Unity. Sau chỉnh theo mockup mới, font nguyên cỡ F11/B10 cần thẻ khoảng61px thiết kế ở2400×1080 và78.5px ở844×390; không ép chiều cao làm cắt hàng chữ. **Chưa compile/chạy client, Device Simulator, build Unity/APK.**

| Test tay — tất cả chưa chạy | Cần xác nhận |
|---|---|
| Mobile844×390; Android16:9/20:9, tai thỏ trái/phải; máy thật | Tab hai bậc cong mượt, đủ thông tin, không che avatar/nút phải; màn hẹp giảm HP tới160px hoặc chỉ còn tên khu |
| Quái/boss/người chơi, tên dài, sức mạnh ẩn/hiện | Đúng dữ liệu, bậc dưới ôm nội dung, tên/số dài cuộn đủ, HP giữ style/căn giữa |
| NPC/chưa có mục tiêu/mục tiêu chết/mất/bỏ focus | Chỉ tên khu, tab biến mất ngay theo trạng thái; không xóa focus hoặc thêm timer |
| Chuyển mục tiêu liên tục, đổi map/relog/resize | Không nhầm phản hồi sức mạnh, không đăng ký trùng, texture cũ được hủy |
| Bấm xuyên thẻ, joystick+chiêu, modal | Thẻ không chặn pointer; controls/modal vẫn quyết định input theo registry |
| PC1280×720 | HUD mục tiêu và gameplay giữ nguyên |
| Cả hai list0/1/3/5/10 trên mobile844×390,20:9 và PC1280×720 | Header N khớp bộ lọc;0 ẩn,1 đúng1;>=3 hiện đủ nếu available>=3*row; mục dư có rail |
| Kéo ngón tay/lăn chuột, scroll đến đầu/cuối và lệch nửa hàng | Thấy đủ mục, clamp đúng, phần hàng clip không chồng; không chạy/chọn map/nhặt đồ ngoài ý muốn |
| Kéo từ list ra joystick/skill, thả ngoài list; modal mở giữa kéo | Không đổi owner, không mất release/capture kẹt; modal chặn wheel và click xuyên |
| Mục vào/ra phạm vi, item bị nhặt, focus đang chọn, resize/relog | N cập nhật trước input/paint, scroll clamp; tô đúng hàng focus; không đổi lọc khoảng cách |
| PC bật analog có sẵn | List tránh đúng ảnh joystick thực, không bị broad hitbox ép cao1hàng; logic di chuyển giữ nguyên |

Nguồn sửa phần2 (file:line):
- `Assets/Scripts/Hud/HudLayout.cs:164`.
- `Assets/Scripts/Hud/MapListsHud.cs:113`.
- `Assets/Scripts/Hud/MapListsHud.cs:249`.
- `Assets/Scripts/Hud/MobileCombatPointers.cs:118`.
- `Assets/Scripts/GamePad.cs:299`.
- `Assets/Scripts/Hud/TargetMobileHud.cs:294`.
- `Assets/Scripts/Controller.cs:1079`.

## Chỉnh theo mockup target_card_stepped_tab_tight.png — 16/10/2026

Tham chiếu trực tiếp ảnh thiết kế mới `D:/KamuiSpace/case CRUD/target_card_stepped_tab_tight.png`, không lấy ảnh test làm hình chuẩn. TargetMobileHud.BuildPanel thay vai cong kết thúc bằng cạnh đứng bằng **vai cong nối tiếp tuyến vào cạnh vát dài**, đi xuống gờ **ngang** giữa hai bậc; bậc dưới tiếp tục hơi nghiêng rồi bo vào đáy. Gờ không còn là đoạn nối chéo; góc bo7px, nền/viền và cache một texture pixel thật giữ nguyên. Xóa đường bao cũ ngay trong builder, không thêm sprite hay bộ nền thứ hai.

Bounds chừa đủ chiều cao font thường cho pill sức mạnh (trước lấy theo font tên đậm rồi trừ2px nên có thể cắt chữ), tính cả lề chữ tên vào chiều rộng nội dung. Nếu tên ngắn thì ô sức mạnh được dùng phần rộng còn lại thay vì luôn bị chặn ở55%. Chữ dài vẫn cuộn; thêm1logical pixel lề glyph ở hai đầu clip để đầu/cuối chuỗi không dính mép khi dừng. HP giữ painter, màu, kích thước ưu tiên và chữ căn giữa. Không sửa danh sách, gameplay, input hoặc PC trong lần này.

Đã đối chiếu preview hình học offline với mockup và kiểm tra12 tổ hợp chiều rộng/tỉ lệ pixel: vai nối tiếp tuyến, gờ ngang thực, vùng nội dung nằm trong nền; delimiter/diff tĩnh. Test tay bổ sung **chưa chạy**: so đúng hai cạnh vát/gờ/bo đáy theo ảnh2; tên/sức mạnh ngắn/dài, đầu/cuối marquee không cắt chữ, pill không cắt trên/dưới; quái không power, đổi mục tiêu/resize/tai thỏ; HP và list giữ nguyên. **Chưa compile/chạy/build.**

## Căn giữa tên, cuộn một chiều và giảm khựng khi đổi mục tiêu — 16/10/2026

- Khi không có pill sức mạnh (Mob/boss không có số), tên dùng cả hàng dưới và căn giữa. Tên dài vượt ô vẫn chạy để đọc đủ. Khi có pill thì tên giữ phần bên trái. Không đổi nguồn sức mạnh, request−79 hoặc logic focus.
- Marquee đổi từ qua lại thành **chỉ chạy sang trái**, bản sao cùng chuỗi đi vào từ phải sau khoảng cách ngắn; nghỉ1s ở đầu mỗi vòng, không đảo hướng. Clip/lề glyph giữ riêng từng ô. Giữ4 state có sẵn, chỉ reset thời gian khi đổi mục tiêu/text/layout/hiện lại; không clear rồi cấp phát state mới mỗi lần chọn. Chiều rộng font cache theo text/font/zoom; HP/sức mạnh/map-khu chỉ format khi giá trị đổi.
- Nguyên nhân tốn CPU thấy trong source: lowerWidth/power pill trước phụ thuộc số ký tự, đổi mục tiêu làm key nền đổi và Destroy/BuildPanel/Apply ngay trong painter; builder cũ duyệt **mọi cạnh cho mọi pixel**, có sqrt mỗi cạnh. Cache mask HP/pill đầy còn gọi ClearShapes làm hủy cả nền ngoài ý muốn. Không thể khẳng định toàn bộ mức tụt60→20FPS chỉ từ các nguyên nhân này khi chưa chạy Profiler.
- Theo yêu cầu cho phép cố định cụm: nền hai bậc và chiều rộng pill nay chỉ theo viewport/font/zoom, **không theo tên, HP, sức mạnh hoặc loại mục tiêu**. Vẫn giữ hình dáng mockup mới; bậc dưới160px thông thường,140px khi hẹp. Chỉ rect chia tên/pill thay đổi khi pill ẩn/hiện; nền giữ nguyên. Không dựng lại texture khi chuyển mục tiêu trong cùng viewport sau lần dựng đầu. Viewport/zoom/safe-area/session thay đổi vẫn dựng/dọn đúng như trước.
- Builder mới raster bằng scanline, tính khoảng cách trong dải hẹp quanh mỗi cạnh; mỗi pixel cuối chỉ lấy sqrt một lần. Giữ coverage signed-distance/alpha88%/viền1.5px và texture đúng pixel thật, không đổi hình bằng sprite phóng to. Mask HP/pill dùng ShapeKey struct thay chuỗi key cấp phát mỗi frame; eviction cache này chỉ dọn mask riêng, không đụng panelShape.

Kiểm tra tĩnh:4 cấu hình mask (120×31 hai bậc dưới khác nhau,104×31,240×62) so **từng pixel RGBA** giữa mô hình thuật toán cũ và mới, không sai khác. Số phép tính khoảng cách cạnh ở240×62 giảm2,336,160→8,767 (không phải benchmark FPS); scanline chỉ duyệt cạnh theo hàng thay vì theo từng pixel. Kiểm tra offset cuộn đơn điệu, bản sao nối vòng cùng vị trí, kích thước nền không phụ thuộc chuỗi và delimiter/diff. Không thay danh sách/PC/gameplay trong lượt này.

Test tay bổ sung **chưa chạy**: quái không SM tên giữa; người có SM tên/pill đúng; tên dài chạy trái liên tục qua ít nhất3vòng, không lắc/đảo hướng/cắt đầu cuối; liên tục chọn100lần quái↔người/tên dài↔ngắn trên Simulator16:9/20:9 và S22, xem FPS/CPU/GC trước-sau, xác nhận BuildPanel/newTexture2D không xuất hiện theo mỗi lần chọn sau warm-up; đổi map/relog/resize/tai thỏ/ẩn hiện modal không rò texture. **Chưa compile/chạy/build.**
- `Assets/Scripts/Hud/TargetMobileHud.cs:120` — Bounds.
- `Assets/Scripts/Hud/TargetMobileHud.cs:183` — Text.
- `Assets/Scripts/Hud/TargetMobileHud.cs:294` — BuildPanel.
- `Assets/Scripts/Hud/TargetMobileHud.cs:401` — ClearButtonShapes.

## Chạm mục tiêu bị vùng joystick rộng nuốt — 16/10/2026

Nguồn thật: CombatMobileHud.JoystickTouch phủ rộng tới khoảngh/4 và một phần ba màn hình trái. MobileCombatPointers.Feed trước đây gán mọi press ở đó cho stick, không phát pointerPressed/Released vào GameCanvas để GameScr.checkClick chọn mục tiêu. GamePad.update cần độ kéo lớn hơn4logical mới có hướng di chuyển; chạm đơn có delta0 nên không dịch núm, mobile painter còn giữ vòng nền cố định, dẫn tới cảm giác vùng chết. Không kết luận thêm lỗi kéo trên thiết bị khi chưa chạy.

Sửa mobile: nếu HitTest là vùng bắt joystick rộng, điểm nằm **ngoài rect vòng joystick đang vẽ**, controls đang cho nhận input và có đối tượng ở hitbox map gốc, pointer được giao cho map. GameScr.HasMobilePointerTarget chỉ gọi findClickToItem với point+camera, không đổi focus/di chuyển/gửi service; cùng logic hitbox nới30logical, trạng thái ẩn và loại trừ quái triệu hồi của chính mình. Áp dụng cả Mob/Char/NPC/item theo bộ chọn gốc. Bỏ chọn quái/người/NPC trong cài đặt và các điều kiện cast/khóa gameplay vẫn được hàm gốc giữ nguyên.

Capture.MapTargetThroughJoystick giữ owner map trọn press/hold/release, bỏ qua broad zone ở các bước sau (không re-probe đối tượng đang di chuyển/chết). Đi vào **vòng joystick thật**, nút chiêu/item/ĐÁNH hoặc modal thì hủy thao tác như quy tắc chống xuyên cũ, không đổi owner giữa chừng. Press trên vòng joystick vẫn ưu tiên stick dù có quái phía dưới; press vùng trống vẫn giữ vùng joystick rộng. Giữ joystick bằng ngón1 và chọn quái bằng ngón2 ngoài vòng được phép; world pointer thứ2 bị cách ly như cũ. Không sửa GamePad.update/paint, nguồn phím, xử lý chọn/đánh/double-click/di chuyển hoặc PC.

Kiểm tra tĩnh:9 tình huống owner và5 tình huống hold/release trong mô hình routing; delimiter/diff; so nguyên thân findClickToItem/findClickToMOB/checkClick/checkSingleClickEarly/checkDoubleClick/checkClickMoveTo/doDoubleClickToObj/updateKey vớiHEAD không đổi. **Chưa compile/chạy/build.**

Test tay bổ sung chưa chạy: chạm đúng quái trong vùng khoanh ảnh user thấy focus/thẻ; chạm NPC/người/item ngoài vòng; giữ-kéo vòng joystick và kéo vùng trống như cũ; joystick+chọn quái/chiêu bằng ngón khác; kéo pointer chọn quái vào vòng/chiêu rồi thả không chọn xuyên; mục tiêu chết/rời vị trí giữa press-release theo hàm gốc; modal/loading/map change giữa gesture không phát release cũ; kiểm tra Android16:9/20:9/tai thỏ/S22 và PC không hồi quy.
- `Assets/Scripts/Hud/MobileCombatPointers.cs:86`.
- `Assets/Scripts/Hud/MobileCombatPointers.cs:125`.
- `Assets/Scripts/GameScr.cs:3303`.

## Xác nhận trước PR — 16/10/2026

User đã test lại sửa chạm chọn quái trong vùng joystick rộng, xác nhận “ổn” và yêu cầu tạo PR. Đây là xác nhận của user cho lỗi vừa sửa, không thay thế toàn bộ ma trận test. Agent chỉ kiểm tra tĩnh, không compile/chạy/build Unity/APK. Các thay đổi logo/Packages/ProjectSettings/UserSettings có sẵn giữ ngoài commit/PR.
