from pathlib import Path
from analyze import packets,streams,frames
from protocol_research import unpack
import json,collections
folder=Path('identity-full-capture.txt').read_text().strip(); rows=[]
for key,data,stamps,gap in streams(packets(Path(folder)/'adapter-5.pcap')):
 if key[1]!=1239:continue
 try:
  for pos,fr in frames(data):
   try:b=unpack(fr)
   except Exception:continue
   t=next((t for end,t in stamps if end>pos),0)
   rows.append(dict(flow=key,time=t,level=fr[3],hex=b.hex()))
   if b'\x03\x00Dor' in b:print('NAME',t,fr[3],len(b),b[:150].hex(), 'offset',b.find(b'\x03\x00Dor'))
 except Exception as e:print('ERR',key,str(e))
Path('captures/identity-full-frames.json').write_text(json.dumps(rows))
print('FRAMES',len(rows));print(collections.Counter(int.from_bytes(bytes.fromhex(x['hex'])[2:4],'little') for x in rows).most_common(20))
