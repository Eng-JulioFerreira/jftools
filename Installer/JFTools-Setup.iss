; JF Tools (C# Addin)
; Inno Setup 6
;
; Coloque na mesma pasta que este .iss:
;   JFTools.dll          (compilado do Visual Studio - bin\Release\)
;   export_profiles.txt
;   icon.ico

[Setup]
AppName=JF Tools
AppVersion=1.0
AppPublisher=JF Tools
AppId={{B8F3A1C2-7D4E-4A9B-8C6D-1E2F3A4B5C6D}
DefaultDirName={commonappdata}\JFTools
DefaultGroupName=JF Tools
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=JFTools-Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern
SetupIconFile=icon.ico
UninstallDisplayName=JF Tools
UninstallDisplayIcon={app}\icon.ico
CreateUninstallRegKey=yes

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"

[Messages]
WelcomeLabel1=Bem-vindo ao instalador do JF Tools
FinishedLabel=Instalação concluída! Reinicie o Revit e procure a aba JFTools.

[Files]
; DLL do plugin e dependências (instaladas em C:\ProgramData\JFTools\)
Source: "JFTools.dll";          DestDir: "{app}"; Flags: ignoreversion
Source: "export_profiles.txt";  DestDir: "{app}"; Flags: ignoreversion
Source: "icon.ico";             DestDir: "{app}"; Flags: ignoreversion

[UninstallDelete]
; Remover pasta do app
Type: filesandordirs; Name: "{app}"

[Code]

// ── Helpers ───────────────────────────────────────────────────────────────

function GetUserAppData(): string;
// Returns the actual logged-in user's APPDATA even when running elevated.
var
  Buf: string;
begin
  // USERPROFILE is reliable under UAC elevation
  Buf := GetEnv('USERPROFILE');
  if Buf <> '' then
    Result := Buf + '\AppData\Roaming'
  else
    Result := ExpandConstant('{userappdata}');
end;

function BuildAddinContent(DllPath: string): string;
begin
  Result :=
    '<?xml version="1.0" encoding="utf-8"?>' + #13#10 +
    '<RevitAddIns>' + #13#10 +
    '  <AddIn Type="Application">' + #13#10 +
    '    <Name>JF Tools Ribbon</Name>' + #13#10 +
    '    <Assembly>' + DllPath + '</Assembly>' + #13#10 +
    '    <FullClassName>JFTools.JFToolsApp</FullClassName>' + #13#10 +
    '    <ClientId>B8F3A1C2-7D4E-4A9B-8C6D-1E2F3A4B5C7E</ClientId>' + #13#10 +
    '    <VendorId>JFTools</VendorId>' + #13#10 +
    '    <VendorDescription>JF Tools</VendorDescription>' + #13#10 +
    '  </AddIn>' + #13#10 +
    '</RevitAddIns>';
end;

// ── Find all installed Revit Addins folders ───────────────────────────────

procedure GetRevitAddinsDirs(var Dirs: TStringList);
var
  AddinsBase: string;
  FindRec: TFindRec;
  SubDir: string;
begin
  AddinsBase := GetUserAppData() + '\Autodesk\Revit\Addins';
  if not DirExists(AddinsBase) then Exit;

  if FindFirst(AddinsBase + '\*', FindRec) then
  begin
    try
      repeat
        // Só versões em .NET Framework 4.8 (2022–2024). O Revit 2025+ usa .NET 8
        // e não carrega esta DLL; registrar nele causaria erro ao abrir o Revit.
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and
           ((FindRec.Name = '2022') or (FindRec.Name = '2023') or (FindRec.Name = '2024')) then
        begin
          SubDir := AddinsBase + '\' + FindRec.Name;
          Dirs.Add(SubDir);
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

// ── Install / remove .addin files ─────────────────────────────────────────

procedure InstallAddinFiles();
var
  Dirs: TStringList;
  DllPath, Content: string;
  i: Integer;
begin
  DllPath := ExpandConstant('{app}') + '\JFTools.dll';
  Content  := BuildAddinContent(DllPath);

  Dirs := TStringList.Create();
  try
    GetRevitAddinsDirs(Dirs);
    for i := 0 to Dirs.Count - 1 do
    begin
      if DirExists(Dirs[i]) then
        SaveStringToFile(Dirs[i] + '\JFTools.addin', Content, False);
    end;
  finally
    Dirs.Free();
  end;
end;

procedure RemoveAddinFiles();
var
  Dirs: TStringList;
  i: Integer;
begin
  Dirs := TStringList.Create();
  try
    GetRevitAddinsDirs(Dirs);
    for i := 0 to Dirs.Count - 1 do
      DeleteFile(Dirs[i] + '\JFTools.addin');
  finally
    Dirs.Free();
  end;
end;

// ── Inno Setup hooks ──────────────────────────────────────────────────────

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    InstallAddinFiles();
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RemoveAddinFiles();
end;
