$ErrorActionPreference = 'Stop'
$pythonRuntime = 'C:\Users\PGGPS\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
& $pythonRuntime (Join-Path $PSScriptRoot 'build_release.py')
if($LASTEXITCODE -ne 0){throw 'Asset packaging failed'}
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot 'dist\ArcheAgeDPS.exe'
$sources=@('Capture.cs','Meter.cs','SelfIdentity.cs','GameConnections.cs','Dashboard.cs','TargetNames.cs','Startup.cs','AppPaths.cs','Theme.cs','WindowFrame.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& $compiler /nologo /platform:x64 /target:winexe /main:Meter /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll "/win32icon:$(Join-Path $PSScriptRoot 'assets\application.ico')" "/resource:$(Join-Path $PSScriptRoot 'build\Assets.zip'),Assets.zip" "/out:$output" $sources
if($LASTEXITCODE -ne 0){throw 'Release build failed'}
Write-Output $output

