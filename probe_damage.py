"""Experimental pattern inspection; not a production combat decoder."""
from analyze import packets, streams, frames
from pathlib import Path
from collections import Counter
import datetime
import json
import zlib

events = []
for path in Path('captures').glob('*/adapter-5.pcap'):
    levels = {}
    for key, data, stamps, gap in streams(packets(path)):
        if key[1] != 1239:
            continue
        for off, frame in frames(data):
            level = frame[3]
            levels.setdefault(level, frame[:60].hex())
            if level == 4:
                body = zlib.decompress(frame[6:], -15)
            elif level == 1:
                body = frame[4:]
            else:
                continue
            start = 0
            while (pos := body.find(b'\x25\x02', start)) >= 0:
                start = pos+2
                b = body[pos:]
                if len(b) < 34 or b[2] != 1 or b[17] != 0 or b[18:21] != b[21:24]:
                    continue
                widths = [((b[28] >> (i*2)) & 3)+1 for i in range(3)]
                cursor = 29; amounts=[]
                for width in widths:
                    amounts.append(int.from_bytes(b[cursor:cursor+width], 'little')); cursor += width
                ts = next(ts for end,ts in stamps if end > off)
                events.append(dict(time=datetime.datetime.fromtimestamp(ts,datetime.timezone.utc).isoformat(),
                                   source=int.from_bytes(b[18:21],'little'), target=int.from_bytes(b[24:27],'little'),
                                   skill_candidate=int.from_bytes(b[9:13],'little'), amount=amounts[0],
                                   amounts=amounts, packet=b.hex(), level=level, file=str(path)))
    print(path, 'level examples', json.dumps(levels))
Path('captures/candidate-events.json').write_text(json.dumps(events,indent=2),encoding='utf-8')
for e in events:
    print(e['time'],e['source'],e['target'],e['skill_candidate'],e['amount'],e['level'])
print('events',len(events),'sources',Counter(e['source'] for e in events))
