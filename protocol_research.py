"""Offline inspection of observed server frames, including level-5 checksum."""
import zlib
def unpack(frame):
    b=frame[4:]
    if frame[3]==4:return zlib.decompress(b[2:],-15)
    if frame[3]!=5:return b
    state=len(b)^522286496; out=bytearray(len(b)); boundary=len(b)//4*4
    for i in list(range(boundary-1,-1,-1))+list(range(boundary,len(b))):
        state=(state+3132373)&0xffffffff
        key=(state>>16)&247
        out[i]=b[i]^(key or 254)
    checksum=0
    for v in out[1:]:checksum=(checksum*19+v)&255
    if not out or checksum!=out[0]:raise ValueError('Level 5 checksum mismatch')
    return bytes(out)
