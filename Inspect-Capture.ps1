param([string]$Path, [int]$Damage = 146115)
$ErrorActionPreference = 'Stop'
function Read-BE16([byte[]]$b,[int]$o) { return ([int]$b[$o] * 256 + [int]$b[$o+1]) }
$stream = [IO.File]::OpenRead((Resolve-Path $Path))
$reader = [IO.BinaryReader]::new($stream)
$rows = [Collections.Generic.List[object]]::new()
try {
    if ($reader.ReadUInt32() -ne 2712847316) { throw 'Only little-endian microsecond PCAP supported' }
    $stream.Position = 20
    $link = $reader.ReadUInt32()
    while ($stream.Position + 16 -le $stream.Length) {
        $sec = $reader.ReadUInt32(); $usec = $reader.ReadUInt32()
        $caplen = $reader.ReadUInt32(); $original = $reader.ReadUInt32()
        if ($caplen -gt 65535 -or $stream.Position + $caplen -gt $stream.Length) { throw 'Invalid/truncated record' }
        $b = $reader.ReadBytes($caplen)
        $ip = switch($link) { 1 {14} 0 {4} 12 {0} default {throw "Unsupported link $link"} }
        if ($b.Length -lt $ip+20 -or ($b[$ip] -shr 4) -ne 4 -or $b[$ip+9] -ne 6) { continue }
        $tcp = $ip + (($b[$ip] -band 15)*4)
        if ($b.Length -lt $tcp+20) { continue }
        $payload = $tcp + (($b[$tcp+12] -shr 4)*4)
        $ipEnd = [Math]::Min($b.Length, $ip+(Read-BE16 $b ($ip+2)))
        if ($payload -ge $ipEnd) { continue }
        $hex = [BitConverter]::ToString($b,$payload,$ipEnd-$payload).Replace('-','')
        $patterns = @([BitConverter]::ToString([BitConverter]::GetBytes($Damage)).Replace('-',''),[BitConverter]::ToString([BitConverter]::GetBytes(-$Damage)).Replace('-',''))
        $hits = @($patterns | Where-Object { $hex.Contains($_) }) -join ','
        $rows.Add([pscustomobject]@{
            Utc=[DateTimeOffset]::FromUnixTimeSeconds($sec).AddTicks([long]$usec*10).ToString('o')
            Source=($b[($ip+12)..($ip+15)] -join '.')
            SourcePort=(Read-BE16 $b $tcp)
            DestinationPort=(Read-BE16 $b ($tcp+2))
            Bytes=($ipEnd-$payload)
            Sequence=([uint64]$b[$tcp+4]*16777216+[uint64]$b[$tcp+5]*65536+[uint64]$b[$tcp+6]*256+[uint64]$b[$tcp+7])
            DamageMatch=$hits
            Prefix=$hex.Substring(0,[Math]::Min(96,$hex.Length))
        })
    }
} finally { $reader.Dispose() }
$rows | Export-Csv ($Path+'.csv') -NoTypeInformation
$rows | Group-Object SourcePort,DestinationPort | Select-Object Name,Count | Format-Table
$rows | Where-Object DamageMatch | Format-Table
$rows | Select-Object -First 8 | Format-Table Utc,SourcePort,Bytes,Prefix
Write-Output "TCP payload records: $($rows.Count)"
