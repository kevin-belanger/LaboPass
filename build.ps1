$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceProject = Join-Path $root "source\LaboPass.csproj"
$publishDir = Join-Path $root "source\bin\portable-win-x64"
$exeSource = Join-Path $publishDir "LaboPass.exe"
$exeDestination = Join-Path $root "LaboPass.exe"
$vaultPath = Join-Path $root "vault.json"

if (-not (Test-Path -LiteralPath $sourceProject)) {
    throw "Projet introuvable: $sourceProject"
}

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

dotnet publish $sourceProject -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o $publishDir

Copy-Item -LiteralPath $exeSource -Destination $exeDestination -Force

if (-not (Test-Path -LiteralPath $vaultPath)) {
    "[]" | Set-Content -LiteralPath $vaultPath -Encoding UTF8
}

Write-Host "Publication terminee: $exeDestination"
