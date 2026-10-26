param([ValidateRange(1,1800)][int]$Seconds = 900)
$ErrorActionPreference = 'Stop'
$game = Get-Process archeage -ErrorAction Stop
$connections = @(Get-NetTCPConnection -State Established | Where-Object OwningProcess -In $game.Id)
if (!$connections.Count) { throw 'No active ArcheAge TCP connections.' }
$clauses = @($connections | ForEach-Object {
    '(tcp and host {0} and host {1} and port {2} and port {3})' -f $_.LocalAddress,$_.RemoteAddress,$_.LocalPort,$_.RemotePort
})
$folder = Join-Path $PSScriptRoot ('captures\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$filterPath = Join-Path $folder 'filter.txt'
($clauses -join ' or ') | Set-Content -LiteralPath $filterPath -Encoding Ascii
$connections | Select-Object LocalAddress,LocalPort,RemoteAddress,RemotePort,OwningProcess | ConvertTo-Json | Set-Content (Join-Path $folder 'connections.json')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe = Join-Path $PSScriptRoot 'Capture.exe'
if (!(Test-Path $exe) -or (Get-Item $exe).LastWriteTime -lt (Get-Item (Join-Path $PSScriptRoot 'Capture.cs')).LastWriteTime) {
    & $compiler /nologo /platform:x64 /target:exe "/out:$exe" (Join-Path $PSScriptRoot 'Capture.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
}
$captureProcess = Start-Process -FilePath $exe -ArgumentList @(('"'+$folder+'"'),('"'+$filterPath+'"'),$Seconds) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $folder 'capture.log') -RedirectStandardError (Join-Path $folder 'error.log')
$folder | Set-Content (Join-Path $PSScriptRoot 'active-capture.txt')
Write-Output "Capture PID: $($captureProcess.Id)"
Write-Output "Folder: $folder"
Write-Output 'To stop: create an empty file named STOP in this folder.'
