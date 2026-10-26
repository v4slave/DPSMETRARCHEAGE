#define AppName "ArcheAge DPS"
#define AppVersion "1.0.0"
[Setup]
AppId={{86B09527-052F-4531-AE74-4ECCF313C6DC}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=ArcheAge DPS
DefaultDirName={localappdata}\Programs\ArcheAgeDPS
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=no
DisableWelcomePage=no
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=dist
OutputBaseFilename=ArcheAgeDPS-Setup
SetupIconFile=assets\application.ico
UninstallDisplayIcon={app}\ArcheAgeDPS.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupMutex=ArcheAgeDPSSetup
UninstallDisplayName={#AppName}
VersionInfoDescription=Установка ArcheAge DPS meter
[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Ярлыки:"; Flags: checkedonce
[Files]
Source: "dist\ArcheAgeDPS.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "installer\Verify-Npcap.ps1"; Flags: dontcopy
[Icons]
Name: "{group}\ArcheAge DPS"; Filename: "{app}\ArcheAgeDPS.exe"
Name: "{group}\Удалить ArcheAge DPS"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ArcheAge DPS"; Filename: "{app}\ArcheAgeDPS.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\ArcheAgeDPS.exe"; Description: "Запустить ArcheAge DPS"; Flags: nowait postinstall skipifsilent
[Code]
var
  DownloadPage: TDownloadWizardPage;
  NeedsRestart: Boolean;

function HasNpcap: Boolean;
begin
  Result := FileExists(ExpandConstant('{sys}\Npcap\wpcap.dll'));
end;

procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel2.Caption :=
    'Мастер установит ArcheAge DPS meter на ваш компьютер.' + #13#10#13#10 +
    'Вы сможете выбрать папку установки и создать ярлык.' + #13#10#13#10 +
    'Если Npcap отсутствует, мастер скачает его с npcap.com. Пройдите официальный мастер Npcap и подтвердите запрос администратора.' + #13#10#13#10 +
    'Перед обновлением закройте DPS meter.';
  DownloadPage := CreateDownloadPage('Загрузка Npcap',
    'Загрузка драйвера захвата трафика с официального сайта', nil);
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo,
  MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := MemoDirInfo + NewLine + NewLine + MemoTasksInfo;
  if not HasNpcap then
    Result := Result + NewLine + NewLine +
      'Дополнительно: скачать и установить Npcap 1.90 с npcap.com.';
end;

function PrepareToInstall(var NeedsRestartFlag: Boolean): String;
var
  ResultCode: Integer;
  Params: String;
begin
  Result := '';
  if HasNpcap then Exit;
  if WizardSilent then begin
    Result := 'Для тихой установки требуется заранее установить Npcap.';
    Exit;
  end;
  DownloadPage.Clear;
  DownloadPage.Add('https://npcap.com/dist/npcap-1.90.exe', 'npcap-1.90.exe', '');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
      ExtractTemporaryFile('Verify-Npcap.ps1');
      Params := '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' +
        ExpandConstant('{tmp}\Verify-Npcap.ps1') + '" -InstallerPath "' +
        ExpandConstant('{tmp}\npcap-1.90.exe') + '"';
      if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
        Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        RaiseException('Не удалось запустить проверку Npcap.');
      if ResultCode = 3010 then NeedsRestart := True
      else if ResultCode <> 0 then
        RaiseException('Npcap не установлен: проверка подписи, отмена или ошибка мастера. Код: ' + IntToStr(ResultCode));
      if not HasNpcap then
        RaiseException('Npcap не найден. Завершите его установку и повторите попытку.');
    except
      Result := GetExceptionMessage;
    end;
  finally
    DownloadPage.Hide;
  end;
end;

function NeedRestart: Boolean;
begin
  Result := NeedsRestart;
end;
