import json,time,uuid,sys
from pathlib import Path
def call(action,**kwargs):
 id=str(uuid.uuid4());p=Path('Logs/world-chat-ui-command.json');p.write_text(json.dumps(dict(id=id,action=action,**kwargs)))
 for _ in range(60):
  time.sleep(.5)
  try:r=json.loads(Path('Logs/world-chat-ui-result.json').read_text())
  except:continue
  if r.get('id')==id:
   if r['status']!='ok':raise RuntimeError(r['detail'])
   print(action+': '+r['detail'].split('\n')[0],flush=True);return r
 raise RuntimeError('timeout '+action)
if __name__=='__main__':call(sys.argv[1],**json.loads(sys.argv[2] if len(sys.argv)>2 else '{}'))
