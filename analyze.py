"""Offline protocol research. Results are candidates, not verified damage events."""
import argparse
import collections
import datetime
import json
from pathlib import Path
import socket
import struct
import zlib


def packets(path):
    with open(path, 'rb') as f:
        header = f.read(24)
        if len(header) != 24 or header[:4] != b'\xd4\xc3\xb2\xa1':
            raise ValueError('Unsupported PCAP')
        link = struct.unpack_from('<I', header, 20)[0]
        while record := f.read(16):
            if len(record) != 16:
                raise ValueError('Truncated record')
            sec, us, cap, orig = struct.unpack('<IIII', record)
            if cap > 65535:
                raise ValueError('Oversized record')
            b = f.read(cap)
            if len(b) != cap:
                raise ValueError('Truncated packet')
            ip = {0: 4, 1: 14, 12: 0}.get(link)
            if ip is None:
                raise ValueError(f'Unsupported link {link}')
            if len(b) < ip+20 or b[ip] >> 4 != 4 or b[ip+9] != 6:
                continue
            if struct.unpack_from('!H', b, ip+6)[0] & 0x3fff:
                raise ValueError('IP fragmentation requires reassembly')
            tcp = ip+(b[ip] & 15)*4
            if len(b) < tcp+20:
                continue
            start = tcp+(b[tcp+12] >> 4)*4
            end = min(len(b), ip+struct.unpack_from('!H', b, ip+2)[0])
            if start >= end:
                continue
            src, dst = socket.inet_ntoa(b[ip+12:ip+16]), socket.inet_ntoa(b[ip+16:ip+20])
            sport, dport, seq = struct.unpack_from('!HHI', b, tcp)
            yield (src, sport, dst, dport), seq, sec+us/1e6, b[start:end]


def streams(records):
    """Offline sequence ordering, retransmission removal, explicit gap boundaries.

    Relative sequence numbers support one wrap within short captures (<2 GiB).
    Conflicting retransmissions fail rather than manufacture a stream.
    """
    flows = collections.defaultdict(list)
    for key, seq, ts, data in records:
        flows[key].append((seq, ts, data))
    for key, parts in flows.items():
        base = parts[0][0]
        parts = sorted((((seq-base+2**31) % 2**32-2**31, ts, data) for seq, ts, data in parts))
        out = bytearray(); stamps = []; next_seq = None; begin = None
        for seq, ts, data in parts:
            if next_seq is None or seq > next_seq:
                if out:
                    yield key, bytes(out), stamps, True
                out = bytearray(); stamps = []; begin = seq; next_seq = seq
            overlap = min(len(data), next_seq-seq)
            if overlap and out[seq-begin:seq-begin+overlap] != data[:overlap]:
                raise ValueError('Conflicting TCP retransmission')
            data = data[overlap:]
            if data:
                out.extend(data); stamps.append((len(out), ts)); next_seq += len(data)
        if out:
            yield key, bytes(out), stamps, False


def frames(data):
    pos = 0
    while pos+4 <= len(data):
        size = int.from_bytes(data[pos:pos+2], 'little')+2
        if size < 4 or data[pos+2] != 0xdd:
            raise ValueError(f'Unaligned frame at {pos}: {data[pos:pos+16].hex()}')
        if pos+size > len(data):
            return
        yield pos, data[pos:pos+size]
        pos += size


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('paths', nargs='+', type=Path)
    ap.add_argument('--damage', type=int, default=146115)
    args = ap.parse_args()
    patterns = {f'uint{width*8}': args.damage.to_bytes(width,'little') for width in (3,4)}
    for path in args.paths:
        results = []; stats = collections.Counter(); examples = []
        for key, data, stamps, gap in streams(packets(path)):
            if key[1] not in (1239,1250):
                continue
            stats['tcp_gaps'] += int(gap)
            try:
                for offset, frame in frames(data):
                    ts = next(ts for end,ts in stamps if end > offset)
                    level = frame[3]; stats[f'level_{level}'] += 1
                    content = frame
                    if level == 4:
                        try:
                            content = zlib.decompress(frame[6:], -15)
                            stats['inflated'] += 1
                            if len(examples)<4: examples.append(content[:100].hex())
                        except zlib.error:
                            stats['inflate_failed'] += 1
                    matches = {label: content.find(pat) for label,pat in patterns.items() if pat in content}
                    if matches:
                        results.append(dict(time=datetime.datetime.fromtimestamp(ts,datetime.timezone.utc).isoformat(),
                                            flow=key, level=level, matches=matches, hex=content.hex()))
            except ValueError as e:
                stats['framing_errors'] += 1
                print(str(e))
        report = dict(file=str(path),stats=dict(stats),matches=results,inflated_examples=examples)
        path.with_suffix('.analysis.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
        print(json.dumps(report,indent=2))


if __name__ == '__main__':
    main()
