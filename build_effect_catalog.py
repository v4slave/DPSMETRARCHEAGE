"""Build the entire published effect catalogue and extract referenced local icons."""
import json,re,html,io
from pathlib import Path
from PIL import Image
def plain(s):return html.unescape(re.sub('<[^>]+>','',s)).replace('\t',' ').replace('\r',' ').replace('\n',' ').strip()
if __name__=='__main__':
    entries=json.loads(Path('captures/gamepak-index.json').read_text());index={e['name'].lower():e for e in entries}
    fallback=Image.new('RGBA',(40,40),(39,45,57,255));fallback.save('assets/client-icons/unknown.png')
    rows=[];missing=set();copied=0
    data=json.loads(Path('captures/codex-all-buffs.json').read_text(encoding='utf8'))['data']
    with open(r'D:\VK Play\ArcheAge\game_pak','rb') as archive:
        for r in data:
            sid=int(r[0]);m=re.search(r'src="/items/([^\"]+)\.png"',r[1]);relative='client-icons/unknown.png'
            if m:
                icon=m[1].replace('\\','/');entry=index.get(('game/ui/icon/'+icon+'.dds').lower());dest=Path('assets/client-icons')/(icon+'.png')
                if entry:
                    if not dest.exists():
                        archive.seek(entry['offset']);im=Image.open(io.BytesIO(archive.read(entry['size'])));dest.parent.mkdir(parents=True,exist_ok=True);im.convert('RGBA').save(dest);copied+=1
                    relative=dest.relative_to('assets').as_posix()
                else:missing.add(icon)
            rows.append([str(-sid),plain(r[2]),relative,'effect',str(sid)])
    Path('assets/effects.tsv').write_text('\n'.join('\t'.join(r) for r in rows)+'\n',encoding='utf8')
    report=dict(rows=len(rows),unique_ids=len({r[0] for r in rows}),extra_local_icons=copied,missing_icon_names=sorted(missing))
    Path('captures/effects-coverage.json').write_text(json.dumps(report,ensure_ascii=True,indent=2));print({k:v for k,v in report.items() if k!='missing_icon_names'});print('Missing icon references:',len(missing))
