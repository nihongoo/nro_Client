"""Source contracts and layout model checks for C. No compiler, build, or Unity."""
from pathlib import Path
import math
import re

ROOT = Path(__file__).resolve().parents[2]
frame = (ROOT / 'Assets/Scripts/Hud/InventoryFrame.cs').read_text(encoding='utf-8-sig')
detail = (ROOT / 'Assets/Scripts/Hud/InventoryDetailPanel.cs').read_text(encoding='utf-8-sig')
bag = (ROOT / 'Assets/Scripts/Hud/InventoryBagPanel.cs').read_text(encoding='utf-8-sig')
panel = (ROOT / 'Assets/Scripts/Panel.cs').read_text(encoding='utf-8-sig')
checks = 0


def check(condition, message):
    global checks
    checks += 1
    assert condition, message


def native_button(a):
    return (round(a[0]),round(a[1]),math.floor(a[2]),24)


def overlap(a, b):
    return min(a[0]+a[2], b[0]+b[2]) > max(a[0], b[0]) and min(a[1]+a[3], b[1]+b[3]) > max(a[1], b[1])


check('DetailActions' not in frame + bag + detail and '"Thao tác"' not in detail + bag, 'old action-strip retained')
check('inventoryEquipmentDoubleTap.Tap' in bag and 'perform(2001, item)' in bag, 'Porata direct-use lost')
check('OpenInventoryItemDetail(item)' in bag and 'BuildInventoryDetail()' in bag, 'selection not opening C detail')
check('Service.' not in detail and 'Service.' not in bag, 'C added item business packets')
check('action.performAction()' in detail and 'doFireInventory(true)' in detail, 'C not reusing original Command')
check('command.actionListener is AutoItem' in detail and 'command.actionListener is AutoVutDo' in detail, 'auto handlers replaced')
check('command?.actionListener is MobileQuickItems' in detail, 'quick assignment handler replaced')
check('item.quantity =' not in detail and 'option.param =' not in detail, 'view mutates item data')
check('getCompare(' not in detail and 'addItemDetail(item)' not in bag, 'C uses legacy mutating comparison')
check('inventoryAppearanceFromSettings = false; SelectInventoryFrameTab(4)' in panel and 'setTabBox(); Hud.HudLayout.Invalidate()' in panel, 'old function entries missing')
check('case 5: paintBox(g)' in frame, 'old chest renderer missing')
check('InventoryEquipmentCollapsed' not in frame and 'EquipmentToggle' not in frame, 'removed collapse control still exists')
check('86 + slot / 3 * (size + 9)' in frame, 'equipment model origin changed')
equipment_method = frame[frame.index('internal static Rect EquipmentBounds'):frame.index('internal static float EquipmentBottom')]
check('InventoryFrameScroll' in equipment_method, 'equipment no longer shares bag scroll')
check('FilterBounds(0).yMax + 6 + index / InventoryBag.Columns' in frame, 'bag model origin changed')
check('if (frameGesture) GameCanvas.isPointerJustDown = false' in frame, 'legacy tab pointer regression')
check('if (inventoryDragging) {' in frame, 'shared drag does not update scroll')
check('Command.paintOngMau' in detail and 'Command.btn0left' in detail and 'Command.btn1left' in detail, 'confirmation button assets not reused')
check('mFont.tahoma_7b_dark' in detail and 'mFont.tahoma_7b_green2' in detail, 'confirmation button fonts missing')
check('Nâng cấp / Gắn sao: đến NPC như trước' in detail and '6000' not in detail, 'inventory bypasses NPC combine')
check('IsInventorySetOption(id) && (item.template.type < 0 || item.template.type > 4)' in detail, 'set section applied to non-set gear')
check('delta > 0 ? mFont.tahoma_7_green2 : delta < 0 ? mFont.tahoma_7_red' in detail, 'comparison direction colors absent')
check('if (Char.myCharz().statusMe == 14)' in detail and 'SyncInventoryBag();' in detail, 'dead/stale action guards missing')

# Formula model matches source constants above. Font-dependent left-panel minimum
# uses a range of plausible native-font widths; actual semantic analysis is separate.
for mobile, w, h in [(True,422,195),(True,585,270),(True,844,390),(True,1560,720),
                     (False,640,360),(False,960,540),(False,1280,720),(False,1280,390)]:
    unit = h/(390 if mobile else 720)
    cell = (40 if mobile else 64)*unit
    bag_cell = (44 if mobile else 56)*unit
    gap = max(6,12*unit)
    for money_width in (150,175,212):
        root_width=max((300 if mobile else 380)*unit,money_width)
        dw=min((250 if mobile else 320)*unit,max(0,w-root_width-8))
        left=(0,0,root_width,h); right=(w-dw,0,dw,h); footer=(1,h-20,root_width-2,19)
        check(not overlap(left,right),'detail overlaps bag/money panel')
        check(not overlap(footer,right),'detail overlaps currency footer')
        equip_bottom=86+2*(cell+9)+cell+10
        filter_y=equip_bottom+16
        first_row=filter_y+22+6
        bag_top=73
        bag_height=h-20-bag_top-2
        check(bag_height>=bag_cell,'shared viewport cannot expose a bag row')
        for count in (0,30,35,36,50,80,255):
            rows=max(7,math.ceil(count/5))
            last=first_row+(rows-1)*(bag_cell+gap)+bag_cell
            limit=max(0,int(last+5-(bag_top+bag_height)))
            check(last-limit<=bag_top+bag_height,'last row cannot scroll into viewport')
            check(first_row-limit<first_row,'bag content did not move with shared scroll')
        for bottom_count in (1,2,3):
            rows=(bottom_count+1)//2
            lower=(w-dw+6,h/2+5,max(0,dw-12),max(0,h/2-13-rows*26))
            buttons=[]
            for i in range(bottom_count):
                button=(w-dw+6+(i%2)*((dw-16)/2+4),h-5-rows*26+(i//2)*26,max(0,dw-12 if bottom_count==1 else (dw-16)/2),24)
                button=native_button(button)
                check(button[1]+button[3]<=h-5,'action not anchored to bottom')
                check(not overlap(button,lower),'description intersects footer button')
                check(all(not overlap(button,old) for old in buttons),'footer buttons overlap')
                buttons.append(button)
            for quick,discard in ((False,False),(True,False),(True,True)):
                controls=(26 if quick else 0)+(26 if discard else 0)
                upper=(w-dw+6,27,max(0,dw-12),max(0,h/2-30-controls))
                if quick:
                    quick_buttons=[native_button((w-dw+6+i*((dw-16)/3+2),h/2-controls,max(0,(dw-16)/3),24)) for i in range(3)]
                    check(all(not overlap(b,upper) for b in quick_buttons),'quick buttons overlap scrolling info')
                if discard:
                    toggle=native_button((w-dw+6,h/2-26,max(0,dw-12),24))
                    check(not overlap(toggle,upper),'discard toggle overlaps scrolling info')
                    check(all(not overlap(toggle,b) for b in quick_buttons),'discard toggle overlaps assignments')

print(f'PASS: {checks} C source/layout-model checks; no build/Unity/compiler')
