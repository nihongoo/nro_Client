import sys,time,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent));from review_command import call
profiles=[('16-9',65,960,540),('18-9',31,960,480),('20-9',40,960,432),('tablet',5,800,600),('notch',13,960,444)]
results=[]
for name,index,w,h in profiles:
 call('clear-fixture');call('device',index=index,width=w,height=h);time.sleep(2);r=call('reflow');time.sleep(2)
 call('capture',path=str(Path('docs/world-chat-ui-review/'+name+'-after.png').resolve()))
 v=call('validate');call('fixture');sc=call('validate-scroll');call('history');time.sleep(1)
 call('capture',path=str(Path('docs/world-chat-ui-review/'+name+'-history.png').resolve()));call('close-history');ticker=call('ticker')
 call('analog',index=1);av=call('validate');call('clear-fixture');call('fixture');call('history');time.sleep(1)
 call('capture',path=str(Path('docs/world-chat-ui-review/'+name+'-analog-history.png').resolve()));call('close-history');call('analog',index=0)
 results.append(dict(profile=name,requested=f'{w}x{h}',state=r['detail'],validation=v['detail'],scrollAndClose=sc['detail'],ticker=ticker['detail'],analog=av['detail']))
 Path('docs/world-chat-ui-review/after-state.json').write_text(json.dumps(results,indent=2))
