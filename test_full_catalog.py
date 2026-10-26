import json,re,html
from pathlib import Path
from PIL import Image
rows={int(a[0]):a for a in (line.split('\t') for line in Path('assets/skills.tsv').read_text(encoding='utf8').splitlines())}
source=json.loads(Path('captures/codex-all-skills.json').read_text(encoding='utf8'))['data']
for item in source:
    row=rows[int(item[0])]
    expected=html.unescape(re.sub('<[^>]+>','',item[2])).strip()
    assert row[1]==expected,(item[0],row[1],expected)
    image=Image.open(Path('assets')/row[2]);image.verify()
report=json.loads(Path('captures/icon-extraction.json').read_text())
assert report['converted']==2508 and not report['errors']
names=dict(line.split('\t',1) for line in Path('assets/npc-names.tsv').read_text(encoding='utf8').splitlines())
assert names['7512']=='Легкий манекен' and names['8029']=='Мертвый всадник'
assert len(names)>17000
print(f'PASS: all {len(source)} published Codex skill rows, exact names and client icons; 2508 client icons; {len(names)} NPC names.')
effects={int(a[0]):a for a in (line.split('\t') for line in Path('assets/effects.tsv').read_text(encoding='utf8').splitlines())}
expected_effects={-int(r[0]):html.unescape(re.sub('<[^>]+>','',r[2])).strip() for r in json.loads(Path('captures/codex-all-buffs.json').read_text(encoding='utf8'))['data']}
assert len(effects)==len(expected_effects)==31014
for sid,name in expected_effects.items():
    assert effects[sid][1]==name,(sid,name,effects[sid][1])
    assert (Path('assets')/effects[sid][2]).is_file()
assert effects[-23162][1]=='Электрошок'
npc_source=json.loads(Path('captures/codex-all-npcs.json').read_text(encoding='utf8'))['data']
for row in npc_source:
    assert names[str(row[0])]==html.unescape(re.sub('<[^>]+>','',row[2])).strip()
print('PASS: every published NPC ID and all 31014 effect IDs are present; screenshot electroshock 23162 resolved.')

