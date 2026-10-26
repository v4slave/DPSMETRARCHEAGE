"""Resolve extra skill IDs actually observed in local captures."""
import csv,re,html,urllib.request,json,io
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
from PIL import Image
ROOT=Path('assets')
CLIENT_INDEX={e['name'].lower():e for e in json.loads(Path('captures/gamepak-index.json').read_text())}
def resolve(sid):
    with urllib.request.urlopen(f'https://archeagecodex.com/ru/skill/{sid}/',timeout=20) as r:s=r.read().decode('utf8')
    name=re.search(r'<title>\s*(.*?)\s*-\s*Archeage Codex\s*</title>',s,re.I)
    icon=re.search(r'class="itemwhite-icon"[^>]*>\s*<img\s+src="[^"]*/items/([^"]+)\.png"',s,re.I)
    if not name:return None
    relative='client-icons/unknown.png'
    if icon:
        key=icon[1].replace('\\','/')
        if re.fullmatch('[A-Za-z0-9_/-]+',key) and '..' not in key:
            candidate='client-icons/'+key+'.png';dest=ROOT/candidate
            entry=CLIENT_INDEX.get(('game/ui/icon/'+key+'.dds').lower())
            if not dest.exists() and entry:
                with open(r'D:\VK Play\ArcheAge\game_pak','rb') as archive:
                    archive.seek(entry['offset']);im=Image.open(io.BytesIO(archive.read(entry['size'])));dest.parent.mkdir(parents=True,exist_ok=True);im.convert('RGBA').save(dest)
            if dest.exists():relative=candidate
    return [str(sid),html.unescape(name[1]),relative,'Codex',str(sid)]
if __name__=='__main__':
    known={int(line.split('\t')[0]) for line in (ROOT/'skills.tsv').read_text(encoding='utf8').splitlines()}
    wanted=set()
    for p in Path('captures').glob('*/candidate-damage.csv'):
        with p.open(encoding='utf-8-sig') as f:
            for row in csv.DictReader(f):
                sid=int(row.get('ability') or 0)
                if sid and sid not in known:wanted.add(sid)
    out=[]
    with ThreadPoolExecutor(max_workers=3) as pool:
        for row in pool.map(resolve,sorted(wanted)):
            if row:out.append(row)
    (ROOT/'extra-skills.tsv').write_text('\n'.join('\t'.join(r) for r in out)+'\n',encoding='utf8')
    print('Extra observed IDs:',len(out),'of',len(wanted));print([(r[0],r[1]) for r in out])
