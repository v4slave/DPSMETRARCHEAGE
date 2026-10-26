"""Extract icon copies and database bytes from a read-only game_pak."""
import json,io
from pathlib import Path
from PIL import Image
if __name__=='__main__':
    entries=json.loads(Path('captures/gamepak-index.json').read_text());out=Path('assets/client-icons');out.mkdir(exist_ok=True)
    count=0;errors=[]
    with Path(r'D:\VK Play\ArcheAge\game_pak').open('rb') as archive:
        for e in entries:
            if e['name'].startswith('game/ui/icon/icon_skill') and e['name'].endswith('.dds'):
                archive.seek(e['offset']);raw=archive.read(e['size'])
                try:
                    image=Image.open(io.BytesIO(raw));image.convert('RGBA').save(out/(Path(e['name']).stem+'.png'));count+=1
                except Exception as ex:errors.append((e['name'],str(ex)))
            if e['name']=='game/db/compact.sqlite':
                archive.seek(e['offset']);db=archive.read(e['size']);Path('captures/client-compact.sqlite').write_bytes(db);print('Database signature:',db[:24].hex())
    Path('captures/icon-extraction.json').write_text(json.dumps(dict(converted=count,errors=errors),indent=2));print('Icons converted:',count,'errors:',len(errors))
