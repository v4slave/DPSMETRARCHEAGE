import json,re,html
from pathlib import Path
ROOT=Path('assets')
def plain(s):return html.unescape(re.sub('<[^>]+>','',s)).replace('\t',' ').replace('\n',' ').strip()
def merge():
    rows={int(a[0]):a for a in (line.split('\t') for line in (ROOT/'skills.tsv').read_text(encoding='utf-8-sig').splitlines())}
    data=json.loads(Path('captures/codex-all-skills.json').read_text(encoding='utf-8'))['data']
    missing=[]
    for row in data:
        sid=int(row[0]);name=plain(row[2]);m=re.search(r'src="/items/([^"/]+)\.png"',row[1]);
        if not m:raise ValueError(f'Missing icon for {sid}')
        icon='client-icons/'+m[1]+'.png'
        if not (ROOT/icon).exists():missing.append((sid,icon));continue
        rows[sid]=[str(sid),name,icon,plain(row[4]),str(sid)]
    (ROOT/'skills.tsv').write_text('\n'.join('\t'.join(rows[k]) for k in sorted(rows))+'\n',encoding='utf-8')
    npcs=json.loads(Path('captures/codex-all-npcs.json').read_text(encoding='utf-8'))['data']
    names={}
    for row in npcs:
        text=plain(row[2])
        if text:names[int(row[0])]=text
    (ROOT/'npc-names.tsv').write_text('\n'.join(str(k)+'\t'+v for k,v in sorted(names.items()))+'\n',encoding='utf-8')
    for sid,alias in [(44196,'quickshot'),(44207,'grenade')]:
        (ROOT/(alias+'.png')).write_bytes((ROOT/rows[sid][2]).read_bytes())
    print('Skill IDs:',len(rows),'Codex rows:',len(data),'missing local icons:',missing)
    print('NPC names:',len(names),'examples:',[(k,names.get(k)) for k in [7512,8029,19040,19041]])
if __name__=='__main__':merge()
