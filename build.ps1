<#
  Compila o JF Tools para todas as versões do Revit e prepara o instalador.

  Uso:
    .\build.ps1                     # usa o 'dotnet' do PATH
    .\build.ps1 -Dotnet C:\x\dotnet.exe

  Requer o SDK do .NET 10 (compila net48, net8.0-windows e net10.0-windows).
  Se o Inno Setup 6 estiver instalado, também gera Installer\Output\JFTools-Setup.exe.
#>
param(
    [string]$Dotnet = 'dotnet',
    [string]$Configuration = 'Release',
    [string]$Version = '2.0.0',   # versão da DLL e do instalador (o GitHub Actions passa a da tag)
    [string]$Iscc = ''            # caminho do ISCC.exe, se não estiver no local padrão
)

$ErrorActionPreference = 'Stop'
$root      = $PSScriptRoot
$project   = Join-Path $root 'JFTools\JFTools.csproj'
$installer = Join-Path $root 'Installer'

# Uma compilação por faixa de Revit (ver JFTools.csproj)
$builds = [ordered]@{
    '2022' = 'Revit 2022, 2023, 2024 (.NET Framework 4.8)'
    '2025' = 'Revit 2025, 2026 (.NET 8)'
    '2027' = 'Revit 2027 (.NET 10)'
}

foreach ($v in $builds.Keys) {
    Write-Host "==> R$v  $($builds[$v])" -ForegroundColor Cyan
    & $Dotnet build $project -c $Configuration "-p:RevitVersion=$v" "-p:Version=$Version" -v minimal -nologo
    if ($LASTEXITCODE -ne 0) { throw "Falha ao compilar R$v" }

    $dest = Join-Path $installer "R$v"
    New-Item -ItemType Directory -Force $dest | Out-Null
    Copy-Item (Join-Path $root "JFTools\bin\$Configuration\R$v\JFTools.dll") $dest -Force
}
Copy-Item (Join-Path $root 'JFTools\export_profiles.txt') $installer -Force

$iscc = @(
    $Iscc,
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if ($iscc) {
    Write-Host '==> Gerando instalador' -ForegroundColor Cyan
    & $iscc "/DAppVersion=$Version" (Join-Path $installer 'JFTools-Setup.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar o instalador' }
    Write-Host "Pronto: $installer\Output\JFTools-Setup.exe" -ForegroundColor Green
} else {
    Write-Host 'Inno Setup 6 não encontrado: DLLs prontas em Installer\R20xx. Compile Installer\JFTools-Setup.iss manualmente.' -ForegroundColor Yellow
}
