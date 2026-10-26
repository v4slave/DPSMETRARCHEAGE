$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build-Release.ps1')
$exe = Join-Path $PSScriptRoot 'dist\ArcheAgeDPS.exe'
Start-Process -FilePath $exe
