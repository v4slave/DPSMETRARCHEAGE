"""Download public official skill metadata and sprites; never send game data."""
from pathlib import Path
import urllib.request
import xml.etree.ElementTree as ET
import re
from concurrent.futures import ThreadPoolExecutor
from PIL import Image
ROOT=Path('assets'); ROOT.mkdir(exist_ok=True)
BASE='https://aa.cdn.gmru.net/static/aa.mail.ru/'
def fetch(url,path):
    with urllib.request.urlopen(url,timeout=45) as r: path.write_bytes(r.read())
def branch(n):
    xml=ROOT/f'branch-{n}.xml'; sprite=ROOT/f'branch-{n}.png'
    fetch(f'https://archeage.ru/static/aa.mail.ru/calculator/skills/6/skills_{n}.xml?v=2',xml)
    fetch(BASE+f'img/main/content/calculator/active_skills/data_clid_{n}.png?5',sprite)
    tree=ET.parse(xml); image=Image.open(sprite); rows=[]
    levels=[1,3,10,15,20,25,30,35,40,45,50,55]
    basic={int(s.findtext('level')):s for s in tree.findall('.//skill') if s.findtext('type')=='1'}
    for s in tree.findall('.//skill'):
        if s.findtext('type') not in ('1','4'):continue
        level=int(s.findtext('level')); sid=int(s.findtext('skillid')); name=s.findtext('name').replace('\t',' ').replace('\n',' ')
        if level not in levels or level not in basic:continue
        index=levels.index(level); x=2+52*(index%4);y=4+52*(index//4)
        icon=f'branch-{n}-level-{level}.png'
        image.crop((x,y,x+40,y+40)).save(ROOT/icon)
        rows.append(f'{sid}\t{name}\t{icon}\t{n}\t{basic[level].findtext("skillid")}')
    return rows
if __name__=='__main__':
    with ThreadPoolExecutor(max_workers=4) as pool: branches=list(pool.map(branch,range(1,15)))
    rows=[r for b in branches for r in b]
    (ROOT/'skills.tsv').write_text('\n'.join(rows)+'\n',encoding='utf-8')
    print(f'{len(branches)} branches; {len(rows)} skill IDs; icons saved locally')
