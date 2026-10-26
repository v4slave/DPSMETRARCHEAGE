param([switch]$SkipAppBuild, [string]$CompilerPath)
$ErrorActionPreference = 'Stop'
if (-not $SkipAppBuild) { & (Join-Path $PSScriptRoot 'Build-Release.ps1') }
if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'dist\ArcheAgeDPS.exe'))) { throw 'Build the application first.' }
if (-not $CompilerPath) {
    $candidates = @(
        (Join-Path $PSScriptRoot 'build\inno\ISCC.exe'),
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $CompilerPath) { throw 'Install Inno Setup 6.7+ from https://jrsoftware.org/isdl.php or pass -CompilerPath.' }
& $CompilerPath (Join-Path $PSScriptRoot 'ArcheAgeDPS.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed' }
Write-Output (Join-Path $PSScriptRoot 'dist\ArcheAgeDPS-Setup.exe')
