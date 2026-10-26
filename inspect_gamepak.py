"""Read-only ArcheAge archive index inspector. Never opens archive for writing.
Format reference: ZeromusXYZ/AAEmu-Packer (v0.7.4), Common/AAPak.cs.
"""
import argparse,json,struct
from pathlib import Path
from cryptography.hazmat.primitives.ciphers import Cipher,algorithms,modes
KEY=bytes.fromhex('321f2aeeaa584ab49a6c9e09d59e9c6f')
def decrypt(b):
    d=Cipher(algorithms.AES(KEY),modes.CBC(bytes(16))).decryptor()
    return d.update(b)+d.finalize()
def index(path):
    with path.open('rb') as f:
        f.seek(-512,2);size=f.tell()+512;header=decrypt(f.read(512))
        if header[:4]!=b'WIBO':raise ValueError('Archive index format is not classic WIBO: '+header[:16].hex())
        count,deleted=struct.unpack_from('<II',header,8)
        if count+deleted>3000000:raise ValueError('Invalid index size')
        start=(size-512-(count+deleted)*336)//512*512;f.seek(start)
        entries=[]
        for i in range(count+deleted):
            b=decrypt(f.read(336));name=b[:264].split(b'\0',1)[0].decode('utf8','replace');off,length=struct.unpack_from('<QQ',b,264)
            if i<count and off+length<=start:entries.append(dict(name=name,offset=off,size=length))
        return entries
if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('archive',type=Path);args=ap.parse_args()
    entries=index(args.archive);Path('captures/gamepak-index.json').write_text(json.dumps(entries,ensure_ascii=True))
    selected=[e for e in entries if any(x in e['name'].lower() for x in ['compact.sqlite','game.sqlite','localization','localize','icon_skill_wild32','icon_skill_madness'])]
    print('Index entries:',len(entries));print(json.dumps(selected[:55],indent=2))
