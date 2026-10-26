"""Package the executable's data only; raw captures and research files are excluded."""
from pathlib import Path
import zipfile
root=Path(__file__).parent
(root/'build').mkdir(exist_ok=True);(root/'dist').mkdir(exist_ok=True)
with zipfile.ZipFile(root/'build'/'Assets.zip','w',zipfile.ZIP_DEFLATED,compresslevel=7) as out:
    for p in sorted((root/'assets').rglob('*')):
        if p.is_file():out.write(p,p.relative_to(root).as_posix())
print('Embedded assets:',(root/'build'/'Assets.zip').stat().st_size,'bytes')
