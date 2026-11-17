; JF Tools (C# Addin)
; Inno Setup 6
;
; Antes de compilar, rode build.ps1 na raiz do repositório. Ele gera nesta pasta:
;   R2022\JFTools.dll    (.NET Framework 4.8 → Revit 2022, 2023, 2024)
;   R2025\JFTools.dll    (.NET 8             → Revit 2025, 2026)
;   R2027\JFTools.dll    (.NET 10            → Revit 2027)
;   export_profiles.txt
; icon.ico já está nesta pasta.

#ifndef AppVersion
  #define AppVersion "2.0.0"
#endif

[Setup]
AppName=JF Tools
AppVersion={#AppVersion}
AppPublisher=Eng. Júlio Ferreira
AppPublisherURL=https://eng-julioferreira.com.br
AppSupportURL=https://github.com/Eng-JulioFerreira/jftools/issues
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
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
SetupIconFile=icon.ico
UninstallDisplayName=JF Tools
UninstallDisplayIcon={app}\icon.ico
CreateUninstallRegKey=yes
LicenseFile=..\LICENSE

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"

[Messages]
WelcomeLabel1=Bem-vindo ao instalador do JF Tools
FinishedLabel=Instalação concluída! Reinicie o Revit e procure a aba JFTools.

[InstallDelete]
; Versão 1.x instalava a DLL na raiz da pasta
Type: files; Name: "{app}\JFTools.dll"
Type: files; Name: "{app}\export_profiles.txt"

[Files]
; Uma DLL por faixa de Revit (instaladas em C:\ProgramData\JFTools\R20xx\)
Source: "R2022\JFTools.dll";   DestDir: "{app}\R2022"; Flags: ignoreversion
Source: "R2025\JFTools.dll";   DestDir: "{app}\R2025"; Flags: ignoreversion
Source: "R2027\JFTools.dll";   DestDir: "{app}\R2027"; Flags: ignoreversion
; O plugin copia os perfis iniciais da pasta da própria DLL
Source: "export_profiles.txt"; DestDir: "{app}\R2022"; Flags: ignoreversion
Source: "export_profiles.txt"; DestDir: "{app}\R2025"; Flags: ignoreversion
Source: "export_profiles.txt"; DestDir: "{app}\R2027"; Flags: ignoreversion
Source: "icon.ico";            DestDir: "{app}";       Flags: ignoreversion

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

function AddinsBase(): string;
begin
  Result := GetUserAppData() + '\Autodesk\Revit\Addins';
end;

// Which build serves each Revit version ('' = not supported).
function BuildFolderFor(Year: string): string;
var
  Y: Integer;
begin
  Result := '';
  Y := StrToIntDef(Year, 0);
  if (Y >= 2022) and (Y <= 2024) then Result := 'R2022'
  else if (Y >= 2025) and (Y <= 2026) then Result := 'R2025'
  else if Y = 2027 then Result := 'R2027';
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

// ── Install / remove .addin files ─────────────────────────────────────────

procedure InstallAddinFiles();
var
  FindRec: TFindRec;
  Folder: string;
begin
  if not DirExists(AddinsBase()) then Exit;
  if FindFirst(AddinsBase() + '\*', FindRec) then
  begin
    try
      repeat
        if FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then
        begin
          Folder := BuildFolderFor(FindRec.Name);
          if Folder <> '' then
            SaveStringToFile(AddinsBase() + '\' + FindRec.Name + '\JFTools.addin',
              BuildAddinContent(ExpandConstant('{app}') + '\' + Folder + '\JFTools.dll'), False);
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

procedure RemoveAddinFiles();
var
  FindRec: TFindRec;
begin
  if not DirExists(AddinsBase()) then Exit;
  if FindFirst(AddinsBase() + '\*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and
           (FindRec.Name <> '.') and (FindRec.Name <> '..') then
          DeleteFile(AddinsBase() + '\' + FindRec.Name + '\JFTools.addin');
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
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
