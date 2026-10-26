$ErrorActionPreference='Stop'
$workspace=Split-Path $PSScriptRoot -Parent
$testRoot=Join-Path $workspace 'build\setup-qa'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$target=Join-Path $testRoot 'Папка с пробелами'
$registry='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{86B09527-052F-4531-AE74-4ECCF313C6DC}_is1'
if(Test-Path $registry){throw 'Existing installation found; QA stopped to protect it'}
$setup=Join-Path $workspace 'dist\ArcheAgeDPS-Setup.exe'
$args=@('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS','/TASKS=""',('/DIR="'+$target+'"'),('/LOG="'+(Join-Path $testRoot 'install.log')+'"'))
$p=Start-Process $setup -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
if($p.ExitCode -ne 0){throw "Install failed: $($p.ExitCode)"}
if((Get-FileHash (Join-Path $target 'ArcheAgeDPS.exe')).Hash -ne (Get-FileHash (Join-Path $workspace 'dist\ArcheAgeDPS.exe')).Hash){throw 'Payload mismatch'}
if(-not(Test-Path $registry)){throw 'Uninstall registration missing'}
Set-Content (Join-Path $target 'keep-user-file.txt') 'preserve'
$p=Start-Process $setup -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
if($p.ExitCode -ne 0){throw 'Upgrade failed'}
$p=Start-Process (Join-Path $target 'unins000.exe') -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -Wait -PassThru -WindowStyle Hidden
if($p.ExitCode -ne 0){throw 'Uninstall failed'}
if(Test-Path (Join-Path $target 'ArcheAgeDPS.exe')){throw 'Application was not removed'}
if(Test-Path $registry){throw 'Uninstall registry entry was not removed'}
if(-not(Test-Path (Join-Path $target 'keep-user-file.txt'))){throw 'User file was removed'}
Write-Output 'PASS: custom Unicode folder, payload, registration, update, uninstall, user file preservation'
